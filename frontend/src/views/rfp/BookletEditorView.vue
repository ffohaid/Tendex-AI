<script setup lang="ts">
/**
 * BookletEditorView - Smart Booklet Editor with EXPRO Color Coding
 *
 * Features:
 * - Color-coded content blocks per EXPRO usage guide:
 *   - Black (Fixed): Cannot be modified
 *   - Green (Editable): Can be modified within regulatory bounds
 *   - Red (Example): Should be replaced with actual content
 *   - Blue (Guidance): Must be removed from published version
 * - AI-powered text generation for editable/example blocks
 * - Bracket placeholder detection and filling
 * - Section navigation sidebar
 * - Auto-save functionality
 * - Export to final version (removes blue guidance blocks)
 */
import { ref, computed, onMounted, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { httpGet, httpPost, httpPut } from '@/services/http'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()

// ═══════════════════════════════════════════════════════════
//  Types
// ═══════════════════════════════════════════════════════════

interface BookletSection {
  id: string
  titleAr: string
  titleEn: string
  sectionType: string
  contentHtml: string
  isMandatory: boolean
  isFromTemplate: boolean
  defaultTextColor: string
  sortOrder: number
}

interface Competition {
  id: string
  projectNameAr: string
  projectNameEn: string
  status: string
  sections: BookletSection[]
}

type ColorType = 'fixed' | 'editable' | 'example' | 'guidance'

interface EditableBlock {
  id: string
  sectionId: string
  originalContent: string
  editedContent: string
  colorType: ColorType
  isHeading: boolean
  hasBracketPlaceholders: boolean
  isEditable: boolean
  isModified: boolean
}

// ═══════════════════════════════════════════════════════════
//  State
// ═══════════════════════════════════════════════════════════

const competition = ref<Competition | null>(null)
const isLoading = ref(true)
const isSaving = ref(false)
const saveStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')
const activeSectionId = ref<string | null>(null)
const showGuidance = ref(true)
const showAiPanel = ref(false)
const aiTargetBlockId = ref<string | null>(null)
const aiPrompt = ref('')
const aiResult = ref('')
const aiIsGenerating = ref(false)
const aiError = ref('')
const editableBlocks = ref<Map<string, EditableBlock>>(new Map())


// ═══════════════════════════════════════════════════════════
//  Computed
// ═══════════════════════════════════════════════════════════

const sections = computed(() => competition.value?.sections || [])



const sectionProgress = computed(() => {
  const total = sections.value.length
  if (total === 0) return 0
  const completed = sections.value.filter(s => {
    const blocks = getBlocksForSection(s.id)
    if (blocks.length === 0) return true
    const editableBlks = blocks.filter(b => b.isEditable || b.hasBracketPlaceholders)
    if (editableBlks.length === 0) return true
    return editableBlks.every(b => b.isModified)
  }).length
  return Math.round((completed / total) * 100)
})

const pendingExampleBlocks = computed(() => {
  let count = 0
  editableBlocks.value.forEach(block => {
    if (block.colorType === 'example' && !block.isModified) count++
  })
  return count
})

const pendingBracketBlocks = computed(() => {
  let count = 0
  editableBlocks.value.forEach(block => {
    if (block.hasBracketPlaceholders && !block.isModified) count++
  })
  return count
})

// ═══════════════════════════════════════════════════════════
//  Methods
// ═══════════════════════════════════════════════════════════

function getBlocksForSection(sectionId: string): EditableBlock[] {
  const blocks: EditableBlock[] = []
  editableBlocks.value.forEach(block => {
    if (block.sectionId === sectionId) blocks.push(block)
  })
  return blocks.sort((a, b) => a.id.localeCompare(b.id))
}

function parseContentToBlocks(section: BookletSection): void {
  if (!section.contentHtml) return

  // Parse the HTML content and extract color-coded blocks
  const parser = new DOMParser()
  const doc = parser.parseFromString(section.contentHtml, 'text/html')
  const elements = doc.body.children

  let blockIndex = 0
  for (const el of elements) {
    blockIndex++
    const blockId = `${section.id}-block-${blockIndex}`

    // Determine color type from the element's spans
    const spans = el.querySelectorAll('[data-color-type]')
    let colorType: ColorType = 'fixed'

    if (spans.length > 0) {
      const firstSpanColor = spans[0].getAttribute('data-color-type')
      if (firstSpanColor === 'green') colorType = 'editable'
      else if (firstSpanColor === 'red') colorType = 'example'
      else if (firstSpanColor === 'blue') colorType = 'guidance'
    } else {
      // Check CSS classes
      const html = el.outerHTML
      if (html.includes('expro-editable')) colorType = 'editable'
      else if (html.includes('expro-example')) colorType = 'example'
      else if (html.includes('expro-guidance')) colorType = 'guidance'
    }

    const text = el.textContent || ''
    const isHeading = el.tagName === 'H3' || el.tagName === 'H2' || el.tagName === 'H1'
    const hasBrackets = /\[.*?\]/.test(text)
    const isEditable = colorType === 'editable' || colorType === 'example'

    editableBlocks.value.set(blockId, {
      id: blockId,
      sectionId: section.id,
      originalContent: text,
      editedContent: text,
      colorType,
      isHeading,
      hasBracketPlaceholders: hasBrackets,
      isEditable,
      isModified: false
    })
  }
}

async function loadCompetition(): Promise<void> {
  isLoading.value = true
  try {
    const id = route.params.id as string
    const data = await httpGet<Competition>(`/v1/competitions/${id}`)
    competition.value = data

    // Parse all sections into editable blocks
    editableBlocks.value.clear()
    for (const section of data.sections) {
      parseContentToBlocks(section)
    }

    // Set active section to first
    if (data.sections.length > 0) {
      activeSectionId.value = data.sections[0].id
    }
  } catch (err) {
    console.error('Failed to load competition', err)
  } finally {
    isLoading.value = false
  }
}

function updateBlockContent(blockId: string, newContent: string): void {
  const block = editableBlocks.value.get(blockId)
  if (!block) return

  block.editedContent = newContent
  block.isModified = newContent !== block.originalContent
}

function scrollToSection(sectionId: string): void {
  activeSectionId.value = sectionId
  nextTick(() => {
    const el = document.getElementById(`section-${sectionId}`)
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  })
}

async function saveChanges(): Promise<void> {
  if (!competition.value) return

  isSaving.value = true
  saveStatus.value = 'saving'
  try {
    // Collect modified sections
    const modifiedSections: { id: string; contentHtml: string }[] = []

    for (const section of sections.value) {
      const blocks = getBlocksForSection(section.id)
      const hasModified = blocks.some(b => b.isModified)
      if (!hasModified) continue

      // Rebuild HTML from blocks
      const htmlParts = blocks.map(block => {
        const tag = block.isHeading ? 'h3' : 'p'
        const cssClass = block.colorType === 'fixed' ? 'expro-fixed'
          : block.colorType === 'editable' ? 'expro-editable'
          : block.colorType === 'example' ? 'expro-example'
          : 'expro-guidance'

        const content = block.editedContent
        return `<${tag} dir="rtl"><span class="${cssClass}" data-color-type="${block.colorType}">${content}</span></${tag}>`
      })

      modifiedSections.push({
        id: section.id,
        contentHtml: htmlParts.join('\n')
      })
    }

    // Save each modified section
    for (const ms of modifiedSections) {
      await httpPut(`/v1/competitions/${competition.value.id}/sections/${ms.id}`, {
        contentHtml: ms.contentHtml
      })
    }

    saveStatus.value = 'saved'
    setTimeout(() => { saveStatus.value = 'idle' }, 3000)
  } catch (err) {
    console.error('Failed to save', err)
    saveStatus.value = 'error'
  } finally {
    isSaving.value = false
  }
}

// ═══════════════════════════════════════════════════════════
//  AI Integration
// ═══════════════════════════════════════════════════════════

function openAiPanel(blockId: string): void {
  aiTargetBlockId.value = blockId
  aiPrompt.value = ''
  aiResult.value = ''
  aiError.value = ''
  showAiPanel.value = true
}

async function generateWithAi(action: string): Promise<void> {
  if (!competition.value || !aiTargetBlockId.value) return

  const block = editableBlocks.value.get(aiTargetBlockId.value)
  if (!block) return

  aiIsGenerating.value = true
  aiError.value = ''
  aiResult.value = ''

  try {
    const response = await httpPost<{ text: string }>('/v1/ai/text/assist', {
      action,
      currentText: block.editedContent,
      fieldName: block.isHeading ? 'عنوان القسم' : 'محتوى القسم',
      fieldPurpose: block.colorType === 'example'
        ? 'استبدال النص التوضيحي بمحتوى فعلي مناسب للمشروع'
        : 'تحسين وتعديل النص ضمن حدود النظام',
      projectName: competition.value.projectNameAr,
      projectDescription: '',
      competitionType: 'public_tender',
      customPrompt: aiPrompt.value || undefined,
      language: 'ar'
    })

    aiResult.value = response.text
  } catch (err: unknown) {
    aiError.value = err instanceof Error ? err.message : 'حدث خطأ أثناء التوليد'
  } finally {
    aiIsGenerating.value = false
  }
}

function applyAiResult(): void {
  if (!aiTargetBlockId.value || !aiResult.value) return

  updateBlockContent(aiTargetBlockId.value, aiResult.value)
  showAiPanel.value = false
}

function getColorBadge(colorType: ColorType): { label: string; class: string; icon: string } {
  switch (colorType) {
    case 'fixed':
      return {
        label: locale.value === 'ar' ? 'ثابت' : 'Fixed',
        class: 'bg-gray-100 text-gray-700 border-gray-200',
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
        label: locale.value === 'ar' ? 'إرشادات (تُحذف)' : 'Guidance (remove)',
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
  loadCompetition()
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
            {{ competition?.projectNameAr || (locale === 'ar' ? 'محرر الكراسة' : 'Booklet Editor') }}
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
              {{ locale === 'ar' ? 'ثابت' : 'Fixed' }}
            </span>
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-green-300 bg-green-500"></span>
              {{ locale === 'ar' ? 'قابل للتعديل' : 'Editable' }}
            </span>
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-red-300 bg-red-500"></span>
              {{ locale === 'ar' ? 'مثال' : 'Example' }}
            </span>
            <span class="flex items-center gap-1.5 text-xs text-secondary-500">
              <span class="inline-block h-3 w-3 rounded border border-blue-300 bg-blue-500"></span>
              {{ locale === 'ar' ? 'إرشادات' : 'Guidance' }}
            </span>
          </div>

          <!-- Sections -->
          <div v-for="section in sections" :key="section.id" :id="`section-${section.id}`" class="mb-10">
            <!-- Section Header -->
            <div class="mb-4 border-b-2 border-primary/20 pb-3">
              <h2 class="text-xl font-bold text-secondary-800">{{ section.titleAr }}</h2>
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

                    <!-- AI Assist Button -->
                    <div class="mt-2 flex items-center gap-2">
                      <button
                        class="inline-flex items-center gap-1 rounded-lg bg-gradient-to-l from-purple-500 to-purple-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition-all hover:shadow-md"
                        @click="openAiPanel(block.id)"
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
          <div class="fixed inset-0 bg-black/30" @click="showAiPanel = false"></div>

          <!-- Panel -->
          <div class="relative ms-auto w-full max-w-md bg-white shadow-2xl">
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
                <!-- Quick Actions -->
                <div class="mb-4">
                  <h4 class="mb-2 text-xs font-semibold uppercase tracking-wider text-secondary-400">
                    {{ locale === 'ar' ? 'إجراءات سريعة' : 'Quick Actions' }}
                  </h4>
                  <div class="grid grid-cols-2 gap-2">
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click="generateWithAi('generate')"
                    >
                      <i class="pi pi-pencil me-1"></i>
                      {{ locale === 'ar' ? 'توليد محتوى' : 'Generate' }}
                    </button>
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click="generateWithAi('improve')"
                    >
                      <i class="pi pi-star me-1"></i>
                      {{ locale === 'ar' ? 'تحسين' : 'Improve' }}
                    </button>
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click="generateWithAi('expand')"
                    >
                      <i class="pi pi-arrows-alt me-1"></i>
                      {{ locale === 'ar' ? 'توسيع' : 'Expand' }}
                    </button>
                    <button
                      class="rounded-lg border border-secondary-200 px-3 py-2 text-xs font-medium text-secondary-600 transition-all hover:border-purple-200 hover:bg-purple-50 hover:text-purple-700 disabled:opacity-50"
                      :disabled="aiIsGenerating"
                      @click="generateWithAi('formalize')"
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
                  ></textarea>
                  <button
                    class="mt-2 w-full rounded-xl bg-gradient-to-l from-purple-500 to-purple-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:shadow-md disabled:opacity-50"
                    :disabled="aiIsGenerating || !aiPrompt.trim()"
                    @click="generateWithAi('custom')"
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
                      @click="applyAiResult"
                    >
                      <i class="pi pi-check me-1 text-xs"></i>
                      {{ locale === 'ar' ? 'تطبيق النتيجة' : 'Apply Result' }}
                    </button>
                    <button
                      class="rounded-xl border border-secondary-200 px-4 py-2.5 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                      @click="aiResult = ''"
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
