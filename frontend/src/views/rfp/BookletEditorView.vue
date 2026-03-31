<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost, httpPut } from '@/services/http'

// ─── Types ──────────────────────────────────────────────
interface BookletBlock {
  id: string
  sortOrder: number
  originalContent: string
  editedContent: string
  contentHtml: string
  colorType: 'fixed' | 'editable' | 'example' | 'guidance'
  isHeading: boolean
  hasBracketPlaceholders: boolean
  isEditable: boolean
  isModified: boolean
}

interface BookletSection {
  id: string
  competitionSectionId: string | null
  titleAr: string
  sortOrder: number
  isMainSection: boolean
  blocks: BookletBlock[]
}

interface BookletEditorData {
  competitionId: string
  templateId: string
  templateNameAr: string
  projectNameAr: string
  projectNameEn: string
  sections: {
    id: string
    competitionSectionId: string | null
    titleAr: string
    sortOrder: number
    isMainSection: boolean
    blocks: {
      id: string
      sortOrder: number
      originalContent: string
      contentHtml: string
      colorType: string
      isHeading: boolean
      hasBracketPlaceholders: boolean
      isEditable: boolean
    }[]
  }[]
}

// ─── Composables ────────────────────────────────────────
const route = useRoute()
const router = useRouter()
const { locale } = useI18n()

// ─── State ──────────────────────────────────────────────
const competitionId = computed(() => route.params.id as string)
const isLoading = ref(true)
const loadError = ref('')
const projectNameAr = ref('')
const projectNameEn = ref('')
const templateNameAr = ref('')
const sections = ref<BookletSection[]>([])
const activeSectionId = ref('')
const showGuidance = ref(true)

// Save state
const isSaving = ref(false)
const saveStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')

// AI Panel state
const showAiPanel = ref(false)
const aiActiveBlockId = ref('')
const aiPrompt = ref('')
const aiResult = ref('')
const aiError = ref('')
const aiIsGenerating = ref(false)

// ─── Computed ───────────────────────────────────────────
const sectionProgress = computed(() => {
  const allBlocks = sections.value.flatMap(s => s.blocks)
  const editableBlocks = allBlocks.filter(b => b.colorType === 'editable' || b.colorType === 'example')
  if (editableBlocks.length === 0) return 100
  const modified = editableBlocks.filter(b => b.isModified).length
  return Math.round((modified / editableBlocks.length) * 100)
})

const pendingExampleBlocks = computed(() => {
  return sections.value.flatMap(s => s.blocks)
    .filter(b => b.colorType === 'example' && !b.isModified).length
})

const pendingBracketBlocks = computed(() => {
  return sections.value.flatMap(s => s.blocks)
    .filter(b => b.hasBracketPlaceholders && !b.isModified).length
})

// ─── Methods ────────────────────────────────────────────
async function loadBookletData() {
  isLoading.value = true
  loadError.value = ''
  try {
    const data = await httpGet<BookletEditorData>(
      `/v1/booklet-templates/competition/${competitionId.value}/blocks`
    )
    projectNameAr.value = data.projectNameAr
    projectNameEn.value = data.projectNameEn
    templateNameAr.value = data.templateNameAr

    sections.value = data.sections.map(s => ({
      id: s.id,
      competitionSectionId: s.competitionSectionId,
      titleAr: s.titleAr,
      sortOrder: s.sortOrder,
      isMainSection: s.isMainSection,
      blocks: s.blocks.map(b => ({
        id: b.id,
        sortOrder: b.sortOrder,
        originalContent: b.originalContent,
        editedContent: b.originalContent,
        contentHtml: b.contentHtml,
        colorType: (b.colorType as BookletBlock['colorType']) || 'fixed',
        isHeading: b.isHeading,
        hasBracketPlaceholders: b.hasBracketPlaceholders,
        isEditable: b.isEditable,
        isModified: false
      }))
    }))

    if (sections.value.length > 0) {
      activeSectionId.value = sections.value[0].id
    }
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    loadError.value = msg
    console.error('Failed to load booklet data:', err)
  } finally {
    isLoading.value = false
  }
}

function getBlocksForSection(sectionId: string): BookletBlock[] {
  const section = sections.value.find(s => s.id === sectionId)
  return section?.blocks ?? []
}

function scrollToSection(sectionId: string) {
  activeSectionId.value = sectionId
  const el = document.getElementById(`section-${sectionId}`)
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

function updateBlockContent(blockId: string, newContent: string) {
  for (const section of sections.value) {
    const block = section.blocks.find(b => b.id === blockId)
    if (block) {
      block.editedContent = newContent
      block.isModified = newContent !== block.originalContent
      break
    }
  }
}

async function saveChanges() {
  isSaving.value = true
  saveStatus.value = 'saving'
  try {
    const payload = {
      sections: sections.value
        .filter(s => s.competitionSectionId)
        .map(s => ({
          competitionSectionId: s.competitionSectionId,
          blocks: s.blocks.map(b => ({
            blockId: b.id,
            sortOrder: b.sortOrder,
            editedContent: b.editedContent,
            colorType: b.colorType,
            isHeading: b.isHeading
          }))
        }))
    }
    await httpPut(`/v1/booklet-templates/competition/${competitionId.value}/blocks`, payload)
    saveStatus.value = 'saved'
    setTimeout(() => { saveStatus.value = 'idle' }, 3000)
  } catch (err) {
    console.error('Save failed:', err)
    saveStatus.value = 'error'
    setTimeout(() => { saveStatus.value = 'idle' }, 5000)
  } finally {
    isSaving.value = false
  }
}

// ─── AI Panel ───────────────────────────────────────────
function openAiPanel(blockId: string) {
  aiActiveBlockId.value = blockId
  aiResult.value = ''
  aiError.value = ''
  aiPrompt.value = ''
  showAiPanel.value = true
}

function getActiveBlock(): BookletBlock | null {
  for (const section of sections.value) {
    const block = section.blocks.find(b => b.id === aiActiveBlockId.value)
    if (block) return block
  }
  return null
}

function getActiveSectionTitle(): string {
  for (const section of sections.value) {
    if (section.blocks.some(b => b.id === aiActiveBlockId.value)) {
      return section.titleAr
    }
  }
  return ''
}

async function generateWithAi(action: string) {
  const block = getActiveBlock()
  if (!block) return

  aiIsGenerating.value = true
  aiError.value = ''
  aiResult.value = ''

  try {
    const response = await httpPost<{ isSuccess: boolean; generatedText: string; errorMessage?: string }>('/v1/ai/text/assist', {
      action,
      currentText: block.editedContent,
      fieldName: getActiveSectionTitle(),
      fieldPurpose: block.colorType === 'example'
        ? 'استبدال نص المثال بمحتوى حقيقي مناسب لكراسة شروط ومواصفات حكومية'
        : 'تحرير محتوى قسم في كراسة شروط ومواصفات حكومية',
      projectName: projectNameAr.value,
      projectDescription: '',
      competitionType: 'PublicTender',
      additionalContext: `القالب: ${templateNameAr.value}`,
      customPrompt: action === 'custom' ? aiPrompt.value : undefined,
      language: 'ar'
    })
    if (response.isSuccess && response.generatedText) {
      aiResult.value = response.generatedText
    } else {
      aiError.value = response.errorMessage || (locale.value === 'ar' ? 'فشل في توليد المحتوى' : 'Failed to generate content')
    }
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    aiError.value = locale.value === 'ar'
      ? `فشل في توليد المحتوى: ${msg}`
      : `Failed to generate: ${msg}`
  } finally {
    aiIsGenerating.value = false
  }
}

function applyAiResult() {
  if (!aiResult.value || !aiActiveBlockId.value) return
  updateBlockContent(aiActiveBlockId.value, aiResult.value)
  showAiPanel.value = false
  aiResult.value = ''
}

// ─── Color helpers ──────────────────────────────────────
type ColorType = 'fixed' | 'editable' | 'example' | 'guidance'

function getColorBadge(colorType: ColorType) {
  switch (colorType) {
    case 'fixed':
      return {
        label: locale.value === 'ar' ? 'نصوص ثابتة (لا يجوز التعديل)' : 'Fixed (do not edit)',
        class: 'bg-gray-100 text-gray-600 border-gray-200',
        icon: 'pi-lock'
      }
    case 'editable':
      return {
        label: locale.value === 'ar' ? 'قابل للتعديل' : 'Editable',
        class: 'bg-green-50 text-green-700 border-green-200',
        icon: 'pi-pencil'
      }
    case 'example':
      return {
        label: locale.value === 'ar' ? 'مثال (يجب استبداله)' : 'Example (replace)',
        class: 'bg-red-50 text-red-700 border-red-200',
        icon: 'pi-exclamation-triangle'
      }
    case 'guidance':
      return {
        label: locale.value === 'ar' ? 'إرشادات (تُحذف من النسخة النهائية)' : 'Guidance (remove)',
        class: 'bg-blue-50 text-blue-700 border-blue-200',
        icon: 'pi-info-circle'
      }
  }
}

function getBlockBorderClass(colorType: ColorType): string {
  switch (colorType) {
    case 'fixed': return 'border-s-4 border-s-gray-400 bg-gray-50/50'
    case 'editable': return 'border-s-4 border-s-green-500 bg-green-50/30'
    case 'example': return 'border-s-4 border-s-red-500 bg-red-50/30'
    case 'guidance': return 'border-s-4 border-s-blue-500 bg-blue-50/30'
  }
}

onMounted(() => {
  loadBookletData()
})
</script>

<template>
  <div class="flex h-[calc(100vh-4rem)] flex-col">
    <!-- Top Bar -->
    <div class="flex items-center justify-between border-b border-secondary-100 bg-white px-6 py-3">
      <div class="flex items-center gap-4">
        <button
          class="rounded-lg p-2 text-secondary-500 hover:bg-secondary-100"
          @click="router.push({ name: 'BookletTemplates' })"
        >
          <i class="pi pi-arrow-right"></i>
        </button>
        <div>
          <h1 class="text-lg font-bold text-secondary-800">
            {{ projectNameAr || (locale === 'ar' ? 'محرر الكراسة' : 'Booklet Editor') }}
          </h1>
          <div class="flex items-center gap-3 text-xs text-secondary-500">
            <span>{{ sections.length }} {{ locale === 'ar' ? 'قسم' : 'sections' }}</span>
            <span class="text-secondary-300">|</span>
            <span>{{ sectionProgress }}% {{ locale === 'ar' ? 'مكتمل' : 'complete' }}</span>
            <span v-if="pendingExampleBlocks > 0" class="text-red-500">
              {{ pendingExampleBlocks }} {{ locale === 'ar' ? 'مثال يحتاج استبدال' : 'examples to replace' }}
            </span>
            <span v-if="pendingBracketBlocks > 0" class="text-amber-500">
              {{ pendingBracketBlocks }} {{ locale === 'ar' ? 'حقل يحتاج ملء' : 'fields to fill' }}
            </span>
          </div>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <!-- Toggle Guidance -->
        <button
          class="rounded-lg border px-3 py-1.5 text-xs font-medium transition-all"
          :class="showGuidance ? 'border-blue-200 bg-blue-50 text-blue-700' : 'border-secondary-200 text-secondary-500'"
          @click="showGuidance = !showGuidance"
        >
          <i class="pi pi-info-circle me-1"></i>
          {{ locale === 'ar' ? (showGuidance ? 'إخفاء الإرشادات' : 'إظهار الإرشادات') : (showGuidance ? 'Hide Guidance' : 'Show Guidance') }}
        </button>

        <!-- Save Status -->
        <span v-if="saveStatus === 'saving'" class="text-xs text-amber-500">
          <i class="pi pi-spin pi-spinner me-1"></i>{{ locale === 'ar' ? 'جاري الحفظ...' : 'Saving...' }}
        </span>
        <span v-else-if="saveStatus === 'saved'" class="text-xs text-green-500">
          <i class="pi pi-check me-1"></i>{{ locale === 'ar' ? 'تم الحفظ' : 'Saved' }}
        </span>
        <span v-else-if="saveStatus === 'error'" class="text-xs text-red-500">
          <i class="pi pi-exclamation-triangle me-1"></i>{{ locale === 'ar' ? 'خطأ في الحفظ' : 'Save Error' }}
        </span>

        <!-- Save Button -->
        <button
          class="rounded-xl bg-primary px-4 py-2 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 disabled:opacity-50"
          :disabled="isSaving"
          @click="saveChanges"
        >
          <i class="pi pi-save me-1 text-xs"></i>
          {{ locale === 'ar' ? 'حفظ التعديلات' : 'Save Changes' }}
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex flex-1 items-center justify-center">
      <div class="text-center">
        <i class="pi pi-spin pi-spinner text-4xl text-primary"></i>
        <p class="mt-3 text-sm text-secondary-500">{{ locale === 'ar' ? 'جاري تحميل الكراسة...' : 'Loading booklet...' }}</p>
      </div>
    </div>

    <!-- Load Error -->
    <div v-else-if="loadError" class="flex flex-1 items-center justify-center">
      <div class="text-center">
        <i class="pi pi-exclamation-triangle text-4xl text-red-400"></i>
        <p class="mt-3 text-sm text-red-500">{{ loadError }}</p>
        <button class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white" @click="loadBookletData">
          {{ locale === 'ar' ? 'إعادة المحاولة' : 'Retry' }}
        </button>
      </div>
    </div>

    <!-- Editor Layout -->
    <div v-else class="flex flex-1 overflow-hidden">
      <!-- Sidebar - Section Navigation -->
      <div class="w-72 shrink-0 overflow-y-auto border-e border-secondary-100 bg-secondary-50/50">
        <div class="p-4">
          <h3 class="mb-3 text-xs font-semibold uppercase tracking-wider text-secondary-400">
            {{ locale === 'ar' ? 'أقسام الكراسة' : 'Booklet Sections' }}
          </h3>

          <!-- Progress Bar -->
          <div class="mb-4 rounded-lg bg-white p-3 shadow-sm">
            <div class="flex items-center justify-between text-xs text-secondary-500">
              <span>{{ locale === 'ar' ? 'التقدم' : 'Progress' }}</span>
              <span class="font-semibold text-primary">{{ sectionProgress }}%</span>
            </div>
            <div class="mt-2 h-2 overflow-hidden rounded-full bg-secondary-200">
              <div
                class="h-full rounded-full bg-primary transition-all duration-500"
                :style="{ width: `${sectionProgress}%` }"
              ></div>
            </div>
          </div>

          <!-- Section List -->
          <div class="space-y-1">
            <button
              v-for="section in sections"
              :key="section.id"
              class="w-full rounded-lg px-3 py-2.5 text-start text-sm transition-all"
              :class="activeSectionId === section.id
                ? 'bg-primary/10 font-semibold text-primary'
                : 'text-secondary-600 hover:bg-white hover:shadow-sm'"
              @click="scrollToSection(section.id)"
            >
              <div class="flex items-center gap-2">
                <span class="shrink-0 text-xs text-secondary-400">{{ section.sortOrder }}</span>
                <span class="line-clamp-2">{{ section.titleAr }}</span>
              </div>
            </button>
          </div>
        </div>
      </div>

      <!-- Main Editor Area -->
      <div class="flex-1 overflow-y-auto bg-white">
        <div class="mx-auto max-w-4xl px-8 py-6">
          <!-- Color Legend (compact) -->
          <div class="mb-6 flex flex-wrap gap-3 rounded-xl bg-secondary-50 p-3">
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-gray-300 bg-gray-400"></span>
              {{ locale === 'ar' ? 'ثابت (لا يُعدّل)' : 'Fixed' }}
            </span>
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-green-300 bg-green-500"></span>
              {{ locale === 'ar' ? 'قابل للتعديل' : 'Editable' }}
            </span>
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-red-300 bg-red-500"></span>
              {{ locale === 'ar' ? 'مثال يجب استبداله' : 'Example' }}
            </span>
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-blue-300 bg-blue-500"></span>
              {{ locale === 'ar' ? 'إرشادات (تُحذف)' : 'Guidance' }}
            </span>
          </div>

          <!-- Sections -->
          <div v-for="section in sections" :key="section.id" :id="`section-${section.id}`" class="mb-10">
            <!-- Section Header -->
            <div class="mb-4 border-b-2 border-primary/20 pb-3">
              <h2 class="text-xl font-bold text-secondary-800">{{ section.titleAr }}</h2>
              <div class="mt-1 flex gap-2 text-xs text-secondary-400">
                <span>{{ section.blocks.length }} {{ locale === 'ar' ? 'كتلة' : 'blocks' }}</span>
                <span v-if="section.blocks.filter(b => b.isEditable).length > 0" class="text-green-500">
                  {{ section.blocks.filter(b => b.isEditable).length }} {{ locale === 'ar' ? 'قابلة للتعديل' : 'editable' }}
                </span>
              </div>
            </div>

            <!-- Content Blocks -->
            <div class="space-y-3">
              <template v-for="block in getBlocksForSection(section.id)" :key="block.id">
                <!-- Skip guidance blocks if hidden -->
                <div
                  v-if="block.colorType !== 'guidance' || showGuidance"
                  class="group relative rounded-lg p-4 transition-all"
                  :class="getBlockBorderClass(block.colorType)"
                >
                  <!-- Color Badge -->
                  <div class="mb-2 flex items-center justify-between">
                    <span
                      class="inline-flex items-center gap-1 rounded-md border px-2 py-0.5 text-xs font-medium"
                      :class="getColorBadge(block.colorType).class"
                    >
                      <i :class="`pi ${getColorBadge(block.colorType).icon}`" class="text-[10px]"></i>
                      {{ getColorBadge(block.colorType).label }}
                    </span>

                    <!-- Bracket placeholder indicator -->
                    <span v-if="block.hasBracketPlaceholders && !block.isModified" class="text-xs text-amber-500">
                      <i class="pi pi-exclamation-circle me-1"></i>
                      {{ locale === 'ar' ? 'يحتوي على حقول يجب ملؤها [...]' : 'Contains fields to fill [...]' }}
                    </span>

                    <!-- Modified indicator -->
                    <span v-if="block.isModified" class="text-xs text-green-500">
                      <i class="pi pi-check-circle me-1"></i>
                      {{ locale === 'ar' ? 'تم التعديل' : 'Modified' }}
                    </span>
                  </div>

                  <!-- Fixed Block (read-only) -->
                  <div v-if="block.colorType === 'fixed'" class="text-sm leading-relaxed text-secondary-700">
                    <component :is="block.isHeading ? 'h3' : 'p'" :class="block.isHeading ? 'font-bold text-base' : ''">
                      {{ block.editedContent }}
                    </component>
                  </div>

                  <!-- Guidance Block (read-only, info style) -->
                  <div v-else-if="block.colorType === 'guidance'" class="text-sm leading-relaxed text-blue-600 italic">
                    <div class="flex items-start gap-2">
                      <i class="pi pi-info-circle mt-0.5 shrink-0"></i>
                      <p>{{ block.editedContent }}</p>
                    </div>
                  </div>

                  <!-- Editable / Example Block -->
                  <div v-else>
                    <component :is="block.isHeading ? 'h3' : 'div'" :class="block.isHeading ? 'font-bold text-base' : ''">
                      <textarea
                        :value="block.editedContent"
                        class="w-full resize-y rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm leading-relaxed outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
                        :class="block.colorType === 'example' ? 'border-red-200 focus:border-red-400 focus:ring-red-200/20' : ''"
                        :rows="Math.max(3, Math.ceil(block.editedContent.length / 80))"
                        @input="(e) => updateBlockContent(block.id, (e.target as HTMLTextAreaElement).value)"
                      ></textarea>
                    </component>

                    <!-- Example warning -->
                    <div v-if="block.colorType === 'example' && !block.isModified" class="mt-1 text-xs text-red-500">
                      <i class="pi pi-exclamation-triangle me-1"></i>
                      {{ locale === 'ar' ? 'هذا نص استرشادي يجب استبداله بالمحتوى الفعلي' : 'This is example text that must be replaced' }}
                    </div>

                    <!-- AI Assist + Restore Buttons -->
                    <div class="mt-2 flex items-center gap-2">
                      <button
                        class="inline-flex items-center gap-1 rounded-lg bg-gradient-to-l from-purple-500 to-purple-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition-all hover:shadow-md"
                        @click.stop="openAiPanel(block.id)"
                      >
                        <i class="pi pi-sparkles text-[10px]"></i>
                        {{ locale === 'ar' ? 'مساعد الذكاء الاصطناعي' : 'AI Assistant' }}
                      </button>
                      <button
                        v-if="block.isModified"
                        class="inline-flex items-center gap-1 rounded-lg border border-secondary-200 px-3 py-1.5 text-xs font-medium text-secondary-500 hover:bg-secondary-50"
                        @click="updateBlockContent(block.id, block.originalContent)"
                      >
                        <i class="pi pi-undo text-[10px]"></i>
                        {{ locale === 'ar' ? 'استعادة الأصلي' : 'Restore Original' }}
                      </button>
                    </div>
                  </div>
                </div>
              </template>

              <!-- Empty section -->
              <div v-if="getBlocksForSection(section.id).length === 0" class="rounded-lg border-2 border-dashed border-secondary-200 p-8 text-center">
                <i class="pi pi-file-edit text-3xl text-secondary-300"></i>
                <p class="mt-2 text-sm text-secondary-400">
                  {{ locale === 'ar' ? 'لا يوجد محتوى في هذا القسم' : 'No content in this section' }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- AI Panel (Slide-over) -->
    <Teleport to="body">
      <Transition name="slide-in">
        <div v-if="showAiPanel" class="fixed inset-y-0 end-0 z-50 flex">
          <!-- Backdrop -->
          <div class="fixed inset-0 bg-black/30" @click.self="showAiPanel = false"></div>

          <!-- Panel -->
          <div class="relative ms-auto w-full max-w-md bg-white shadow-2xl" @click.stop>
            <div class="flex h-full flex-col">
              <!-- Panel Header -->
              <div class="flex items-center justify-between border-b border-secondary-100 bg-gradient-to-l from-purple-50 to-white p-5">
                <div>
                  <h3 class="text-base font-bold text-secondary-800">
                    <i class="pi pi-sparkles me-2 text-purple-500"></i>
                    {{ locale === 'ar' ? 'مساعد الذكاء الاصطناعي' : 'AI Assistant' }}
                  </h3>
                  <p class="mt-1 text-xs text-secondary-500">
                    {{ locale === 'ar' ? 'استخدم الذكاء الاصطناعي لتوليد أو تحسين المحتوى' : 'Use AI to generate or improve content' }}
                  </p>
                </div>
                <button class="rounded-lg p-2 text-secondary-400 hover:bg-secondary-100" @click="showAiPanel = false">
                  <i class="pi pi-times"></i>
                </button>
              </div>

              <!-- Panel Body -->
              <div class="flex-1 overflow-y-auto p-5">
                <!-- Current Block Preview -->
                <div v-if="getActiveBlock()" class="mb-4 rounded-lg border border-secondary-200 bg-secondary-50 p-3">
                  <h4 class="mb-1 text-xs font-semibold text-secondary-400">
                    {{ locale === 'ar' ? 'النص الحالي' : 'Current Text' }}
                  </h4>
                  <p class="line-clamp-4 text-xs text-secondary-600">{{ getActiveBlock()?.editedContent }}</p>
                </div>

                <!-- Quick Actions -->
                <div class="mb-4">
                  <h4 class="mb-2 text-xs font-semibold uppercase tracking-wider text-secondary-400">
                    {{ locale === 'ar' ? 'إجراءات سريعة' : 'Quick Actions' }}
                  </h4>
                  <div class="grid grid-cols-2 gap-2">
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click.stop="generateWithAi('generate')"
                    >
                      <i class="pi pi-pencil me-1"></i>
                      {{ locale === 'ar' ? 'توليد محتوى' : 'Generate' }}
                    </button>
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click.stop="generateWithAi('improve')"
                    >
                      <i class="pi pi-star me-1"></i>
                      {{ locale === 'ar' ? 'تحسين' : 'Improve' }}
                    </button>
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click.stop="generateWithAi('expand')"
                    >
                      <i class="pi pi-arrows-alt me-1"></i>
                      {{ locale === 'ar' ? 'توسيع' : 'Expand' }}
                    </button>
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click.stop="generateWithAi('formalize')"
                    >
                      <i class="pi pi-building me-1"></i>
                      {{ locale === 'ar' ? 'صياغة رسمية' : 'Formalize' }}
                    </button>
                  </div>
                </div>

                <!-- Custom Prompt -->
                <div class="mb-4">
                  <h4 class="mb-2 text-xs font-semibold uppercase tracking-wider text-secondary-400">
                    {{ locale === 'ar' ? 'تعليمات مخصصة' : 'Custom Instructions' }}
                  </h4>
                  <textarea
                    v-model="aiPrompt"
                    rows="3"
                    :placeholder="locale === 'ar' ? 'أدخل تعليمات مخصصة للذكاء الاصطناعي...' : 'Enter custom AI instructions...'"
                    class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-purple-400 focus:ring-2 focus:ring-purple-200/20"
                    @click.stop
                  ></textarea>
                  <button
                    class="mt-2 w-full rounded-xl bg-gradient-to-l from-purple-500 to-purple-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:shadow-md disabled:opacity-50"
                    :disabled="aiIsGenerating || !aiPrompt.trim()"
                    @click.stop="generateWithAi('custom')"
                  >
                    <i v-if="aiIsGenerating" class="pi pi-spin pi-spinner me-2 text-xs"></i>
                    <i v-else class="pi pi-sparkles me-2 text-xs"></i>
                    {{ aiIsGenerating
                      ? (locale === 'ar' ? 'جاري التوليد...' : 'Generating...')
                      : (locale === 'ar' ? 'توليد بتعليمات مخصصة' : 'Generate with Custom Instructions') }}
                  </button>
                </div>

                <!-- Loading -->
                <div v-if="aiIsGenerating" class="flex items-center justify-center py-8">
                  <div class="text-center">
                    <i class="pi pi-spin pi-spinner text-2xl text-purple-500"></i>
                    <p class="mt-2 text-xs text-secondary-500">{{ locale === 'ar' ? 'جاري توليد المحتوى...' : 'Generating content...' }}</p>
                  </div>
                </div>

                <!-- Error -->
                <div v-if="aiError" class="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">
                  <i class="pi pi-exclamation-triangle me-1"></i>{{ aiError }}
                </div>

                <!-- Result -->
                <div v-if="aiResult && !aiIsGenerating" class="mb-4">
                  <h4 class="mb-2 text-xs font-semibold uppercase tracking-wider text-secondary-400">
                    {{ locale === 'ar' ? 'النتيجة' : 'Result' }}
                  </h4>
                  <div class="rounded-xl border border-purple-200 bg-purple-50/50 p-4">
                    <p class="whitespace-pre-wrap text-sm leading-relaxed text-secondary-700">{{ aiResult }}</p>
                  </div>
                  <div class="mt-3 flex gap-2">
                    <button
                      class="flex-1 rounded-xl bg-purple-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-purple-700"
                      @click.stop="applyAiResult"
                    >
                      <i class="pi pi-check me-1 text-xs"></i>
                      {{ locale === 'ar' ? 'تطبيق النتيجة' : 'Apply Result' }}
                    </button>
                    <button
                      class="rounded-xl border border-secondary-200 px-4 py-2.5 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                      @click.stop="aiResult = ''"
                    >
                      {{ locale === 'ar' ? 'تجاهل' : 'Discard' }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.slide-in-enter-active,
.slide-in-leave-active {
  transition: all 0.3s ease;
}
.slide-in-enter-from,
.slide-in-leave-to {
  opacity: 0;
}
.slide-in-enter-from > div:last-child,
.slide-in-leave-to > div:last-child {
  transform: translateX(100%);
}
</style>
