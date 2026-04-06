<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost, httpPut } from '@/services/http'
import RichTextEditor from '@/components/common/RichTextEditor.vue'
import {
  initiateWorkflow,
  getWorkflowStatus,
  approveStep,
  rejectStep,
  type ApprovalWorkflowStatusResult,
  ApprovalStepStatus,
} from '@/services/workflowService'

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

// Approval Workflow state
const approvalStatus = ref<ApprovalWorkflowStatusResult | null>(null)
const isSubmittingForApproval = ref(false)
const showApprovalModal = ref(false)
const showApprovalTimeline = ref(false)
const approvalError = ref('')
const approvalSuccess = ref('')
const rejectComment = ref('')
const showRejectModal = ref(false)
const rejectStepId = ref('')
const approveComment = ref('')
const showApproveModal = ref(false)
const approveStepId = ref('')

// Competition status (fetched from API)
const competitionStatus = ref<number>(0) // 0=Draft, 1=UnderPreparation, 2=PendingApproval, 3=Approved

const isReadOnly = computed(() => competitionStatus.value >= 2) // PendingApproval or Approved
const canSubmitForApproval = computed(() => competitionStatus.value <= 1)
// Reserved for future use:
// const isApprovalPhase = computed(() => competitionStatus.value === 2)
// const isApproved = computed(() => competitionStatus.value === 3)

const statusLabel = computed(() => {
  const labels: Record<number, { ar: string; en: string; class: string }> = {
    0: { ar: 'مسودة', en: 'Draft', class: 'bg-gray-100 text-gray-600 border-gray-200' },
    1: { ar: 'قيد الإعداد', en: 'Under Preparation', class: 'bg-amber-50 text-amber-700 border-amber-200' },
    2: { ar: 'بانتظار الاعتماد', en: 'Pending Approval', class: 'bg-blue-50 text-blue-700 border-blue-200' },
    3: { ar: 'معتمدة', en: 'Approved', class: 'bg-green-50 text-green-700 border-green-200' },
  }
  return labels[competitionStatus.value] || labels[0]
})

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

// ─── Approval Workflow Methods ──────────────────────────
async function loadCompetitionStatus() {
  try {
    const data = await httpGet<{ status: number }>(`/v1/competitions/${competitionId.value}`)
    competitionStatus.value = data.status
  } catch {
    // Default to UnderPreparation if API not available
    competitionStatus.value = 1
  }
}

async function loadApprovalStatus() {
  try {
    const result = await getWorkflowStatus(
      competitionId.value,
      1, // UnderPreparation
      2, // PendingApproval
    )
    approvalStatus.value = result
  } catch {
    approvalStatus.value = null
  }
}

async function submitForApproval() {
  isSubmittingForApproval.value = true
  approvalError.value = ''
  approvalSuccess.value = ''
  try {
    // Save changes first
    await saveChanges()

    // If competition is in Draft status, transition to UnderPreparation first
    if (competitionStatus.value === 0) {
      await httpPost(`/v1/competitions/${competitionId.value}/status`, {
        newStatus: 1, // UnderPreparation
        reason: null,
      })
      competitionStatus.value = 1
    }

    // Now initiate the approval workflow from UnderPreparation to PendingApproval
    await initiateWorkflow({
      competitionId: competitionId.value,
      fromStatus: 1, // UnderPreparation
      toStatus: 2, // PendingApproval
    })

    approvalSuccess.value = locale.value === 'ar'
      ? '\u062a\u0645 \u062a\u0642\u062f\u064a\u0645 \u0627\u0644\u0643\u0631\u0627\u0633\u0629 \u0644\u0644\u0627\u0639\u062a\u0645\u0627\u062f \u0628\u0646\u062c\u0627\u062d'
      : 'Booklet submitted for approval successfully'
    showApprovalModal.value = false
    competitionStatus.value = 2
    await loadApprovalStatus()
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    approvalError.value = locale.value === 'ar'
      ? `\u0641\u0634\u0644 \u0641\u064a \u062a\u0642\u062f\u064a\u0645 \u0627\u0644\u0643\u0631\u0627\u0633\u0629: ${msg}`
      : `Failed to submit: ${msg}`
  } finally {
    isSubmittingForApproval.value = false
  }
}

async function handleApproveStep() {
  if (!approveStepId.value) return
  try {
    const result = await approveStep(approveStepId.value, { comment: approveComment.value || null })
    showApproveModal.value = false
    approveComment.value = ''
    approveStepId.value = ''
    approvalSuccess.value = locale.value === 'ar' ? '\u062a\u0645 \u0627\u0644\u0627\u0639\u062a\u0645\u0627\u062f \u0628\u0646\u062c\u0627\u062d' : 'Approved successfully'
    if (result.isWorkflowCompleted) {
      competitionStatus.value = result.toStatus
    }
    await loadApprovalStatus()
    setTimeout(() => { approvalSuccess.value = '' }, 5000)
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    approvalError.value = locale.value === 'ar' ? `\u0641\u0634\u0644 \u0641\u064a \u0627\u0644\u0627\u0639\u062a\u0645\u0627\u062f: ${msg}` : `Approval failed: ${msg}`
  }
}

async function handleRejectStep() {
  if (!rejectStepId.value || !rejectComment.value.trim()) return
  try {
    await rejectStep(rejectStepId.value, { reason: rejectComment.value })
    showRejectModal.value = false
    rejectComment.value = ''
    rejectStepId.value = ''
    approvalSuccess.value = locale.value === 'ar' ? '\u062a\u0645 \u0627\u0644\u0631\u0641\u0636 \u0648\u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u0643\u0631\u0627\u0633\u0629 \u0644\u0644\u062a\u0639\u062f\u064a\u0644' : 'Rejected and returned for revision'
    competitionStatus.value = 1 // Back to UnderPreparation
    await loadApprovalStatus()
    setTimeout(() => { approvalSuccess.value = '' }, 5000)
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    approvalError.value = locale.value === 'ar' ? `\u0641\u0634\u0644 \u0641\u064a \u0627\u0644\u0631\u0641\u0636: ${msg}` : `Rejection failed: ${msg}`
  }
}

function openApproveModal(stepId: string) {
  approveStepId.value = stepId
  approveComment.value = ''
  showApproveModal.value = true
}

function openRejectModal(stepId: string) {
  rejectStepId.value = stepId
  rejectComment.value = ''
  showRejectModal.value = true
}

function getStepStatusBadge(status: ApprovalStepStatus) {
  const badges: Record<number, { ar: string; en: string; class: string; icon: string }> = {
    [ApprovalStepStatus.Pending]: { ar: '\u0628\u0627\u0646\u062a\u0638\u0627\u0631', en: 'Pending', class: 'bg-gray-100 text-gray-600', icon: 'pi-clock' },
    [ApprovalStepStatus.InProgress]: { ar: '\u062c\u0627\u0631\u064a', en: 'In Progress', class: 'bg-blue-100 text-blue-700', icon: 'pi-spinner' },
    [ApprovalStepStatus.Approved]: { ar: '\u0645\u0639\u062a\u0645\u062f', en: 'Approved', class: 'bg-green-100 text-green-700', icon: 'pi-check' },
    [ApprovalStepStatus.Rejected]: { ar: '\u0645\u0631\u0641\u0648\u0636', en: 'Rejected', class: 'bg-red-100 text-red-700', icon: 'pi-times' },
    [ApprovalStepStatus.Skipped]: { ar: '\u062a\u0645 \u062a\u062e\u0637\u064a\u0647', en: 'Skipped', class: 'bg-gray-50 text-gray-400', icon: 'pi-forward' },
  }
  return badges[status] || badges[ApprovalStepStatus.Pending]
}

onMounted(async () => {
  await loadBookletData()
  await loadCompetitionStatus()
  if (competitionStatus.value >= 2) {
    await loadApprovalStatus()
  }
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
        <!-- Status Badge -->
        <span
          class="inline-flex items-center rounded-md border px-2.5 py-1 text-xs font-semibold"
          :class="statusLabel.class"
        >
          {{ locale === 'ar' ? statusLabel.ar : statusLabel.en }}
        </span>

        <!-- Toggle Guidance -->
        <button
          class="rounded-lg border px-3 py-1.5 text-xs font-medium transition-all"
          :class="showGuidance ? 'border-blue-200 bg-blue-50 text-blue-700' : 'border-secondary-200 text-secondary-500'"
          @click="showGuidance = !showGuidance"
        >
          <i class="pi pi-info-circle me-1"></i>
          {{ locale === 'ar' ? (showGuidance ? 'إخفاء الإرشادات' : 'إظهار الإرشادات') : (showGuidance ? 'Hide Guidance' : 'Show Guidance') }}
        </button>

        <!-- Approval Timeline Toggle -->
        <button
          v-if="approvalStatus?.hasWorkflow"
          class="rounded-lg border px-3 py-1.5 text-xs font-medium transition-all"
          :class="showApprovalTimeline ? 'border-primary/30 bg-primary/5 text-primary' : 'border-secondary-200 text-secondary-500'"
          @click="showApprovalTimeline = !showApprovalTimeline"
        >
          <i class="pi pi-sitemap me-1"></i>
          {{ locale === 'ar' ? 'مسار الاعتماد' : 'Approval Path' }}
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
          v-if="!isReadOnly"
          class="rounded-xl bg-primary px-4 py-2 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 disabled:opacity-50"
          :disabled="isSaving"
          @click="saveChanges"
        >
          <i class="pi pi-save me-1 text-xs"></i>
          {{ locale === 'ar' ? 'حفظ التعديلات' : 'Save Changes' }}
        </button>

        <!-- Submit for Approval Button -->
        <button
          v-if="canSubmitForApproval"
          class="rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-all hover:bg-green-700 disabled:opacity-50"
          :disabled="isSubmittingForApproval"
          @click="showApprovalModal = true"
        >
          <i class="pi pi-send me-1 text-xs"></i>
          {{ locale === 'ar' ? 'تقديم للاعتماد' : 'Submit for Approval' }}
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
                      <RichTextEditor
                        :model-value="block.editedContent"
                        :editable="!isReadOnly"
                        :placeholder="locale === 'ar' ? 'أدخل المحتوى هنا...' : 'Enter content here...'"
                        dir="rtl"
                        min-height="120px"
                        max-height="400px"
                        @update:model-value="(val: string) => updateBlockContent(block.id, val)"
                      />
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

    <!-- ═══ Approval Timeline Panel ═══ -->
    <Teleport to="body">
      <Transition name="slide-in">
        <div
          v-if="showApprovalTimeline && approvalStatus?.hasWorkflow"
          class="fixed inset-0 z-50 flex"
        >
          <div class="flex-1 bg-black/30" @click="showApprovalTimeline = false"></div>
          <div class="w-[400px] overflow-y-auto bg-white shadow-2xl">
            <div class="sticky top-0 z-10 flex items-center justify-between border-b border-secondary-100 bg-white px-6 py-4">
              <h3 class="text-base font-bold text-secondary-800">
                <i class="pi pi-sitemap me-2 text-primary"></i>
                {{ locale === 'ar' ? 'مسار الاعتماد' : 'Approval Path' }}
              </h3>
              <button class="rounded-lg p-1.5 text-secondary-400 hover:bg-secondary-100" @click="showApprovalTimeline = false">
                <i class="pi pi-times text-sm"></i>
              </button>
            </div>

            <!-- Progress Summary -->
            <div class="border-b border-secondary-100 px-6 py-4">
              <div class="flex items-center justify-between text-xs text-secondary-500">
                <span>{{ locale === 'ar' ? 'التقدم' : 'Progress' }}</span>
                <span class="font-semibold text-primary">{{ approvalStatus?.completedSteps || 0 }}/{{ approvalStatus?.totalSteps || 0 }}</span>
              </div>
              <div class="mt-2 h-2 overflow-hidden rounded-full bg-secondary-200">
                <div
                  class="h-full rounded-full transition-all duration-500"
                  :class="approvalStatus?.isRejected ? 'bg-red-500' : approvalStatus?.isCompleted ? 'bg-green-500' : 'bg-primary'"
                  :style="{ width: `${approvalStatus?.totalSteps ? (approvalStatus.completedSteps / approvalStatus.totalSteps) * 100 : 0}%` }"
                ></div>
              </div>
            </div>

            <!-- Steps Timeline -->
            <div class="px-6 py-4">
              <div class="space-y-0">
                <div
                  v-for="(step, idx) in (approvalStatus?.steps || [])"
                  :key="step.stepId"
                  class="relative"
                >
                  <!-- Connector line -->
                  <div
                    v-if="idx < (approvalStatus?.steps?.length || 0) - 1"
                    class="absolute start-4 top-10 h-full w-0.5"
                    :class="step.status === 2 ? 'bg-green-300' : step.status === 3 ? 'bg-red-300' : 'bg-secondary-200'"
                  ></div>

                  <div class="relative flex gap-3 pb-6">
                    <!-- Status Icon -->
                    <div
                      class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
                      :class="{
                        'bg-green-100 text-green-600': step.status === 2,
                        'bg-red-100 text-red-600': step.status === 3,
                        'bg-blue-100 text-blue-600': step.status === 1,
                        'bg-secondary-100 text-secondary-400': step.status === 0 || step.status === 4,
                      }"
                    >
                      <i class="pi text-xs" :class="getStepStatusBadge(step.status).icon"></i>
                    </div>

                    <!-- Step Info -->
                    <div class="flex-1">
                      <div class="flex items-center justify-between">
                        <h4 class="text-sm font-semibold text-secondary-800">
                          {{ locale === 'ar' ? step.stepNameAr : step.stepNameEn }}
                        </h4>
                        <span
                          class="rounded px-1.5 py-0.5 text-[10px] font-medium"
                          :class="getStepStatusBadge(step.status).class"
                        >
                          {{ locale === 'ar' ? getStepStatusBadge(step.status).ar : getStepStatusBadge(step.status).en }}
                        </span>
                      </div>

                      <!-- SLA Info -->
                      <div v-if="step.slaDeadline" class="mt-1 text-[10px]" :class="step.isSlaExceeded ? 'text-red-500' : 'text-secondary-400'">
                        <i class="pi pi-clock me-1"></i>
                        {{ step.isSlaExceeded
                          ? (locale === 'ar' ? 'تجاوز المهلة الزمنية' : 'SLA Exceeded')
                          : (locale === 'ar' ? 'المهلة: ' : 'Deadline: ') + new Date(step.slaDeadline).toLocaleDateString(locale === 'ar' ? 'ar-SA' : 'en-US')
                        }}
                      </div>

                      <!-- Comment -->
                      <p v-if="step.comment" class="mt-1 text-[10px] text-secondary-500">
                        <i class="pi pi-comment me-1"></i>{{ step.comment }}
                      </p>

                      <!-- Action Buttons for current step -->
                      <div v-if="step.status === 1" class="mt-2 flex gap-2">
                        <button
                          class="rounded-lg bg-green-600 px-3 py-1 text-xs font-medium text-white hover:bg-green-700"
                          @click="openApproveModal(step.stepId)"
                        >
                          <i class="pi pi-check me-1 text-[10px]"></i>
                          {{ locale === 'ar' ? 'اعتماد' : 'Approve' }}
                        </button>
                        <button
                          class="rounded-lg bg-red-50 px-3 py-1 text-xs font-medium text-red-600 hover:bg-red-100"
                          @click="openRejectModal(step.stepId)"
                        >
                          <i class="pi pi-times me-1 text-[10px]"></i>
                          {{ locale === 'ar' ? 'رفض' : 'Reject' }}
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══ Submit for Approval Confirmation Modal ═══ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showApprovalModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
            <div class="mb-4 flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-green-100">
                <i class="pi pi-send text-lg text-green-600"></i>
              </div>
              <div>
                <h3 class="text-base font-bold text-secondary-800">
                  {{ locale === 'ar' ? 'تقديم الكراسة للاعتماد' : 'Submit for Approval' }}
                </h3>
                <p class="text-xs text-secondary-500">
                  {{ locale === 'ar'
                    ? 'سيتم قفل الكراسة للتعديل وإرسالها للمعتمدين'
                    : 'The booklet will be locked and sent to approvers' }}
                </p>
              </div>
            </div>

            <div v-if="approvalError" class="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">
              <i class="pi pi-exclamation-triangle me-1"></i>{{ approvalError }}
            </div>

            <div class="rounded-lg bg-amber-50 p-3 text-sm text-amber-700">
              <i class="pi pi-info-circle me-1"></i>
              {{ locale === 'ar'
                ? 'بعد التقديم، لن تتمكن من تعديل الكراسة حتى يتم الاعتماد أو الرفض.'
                : 'After submission, you cannot edit the booklet until it is approved or rejected.' }}
            </div>

            <div class="mt-6 flex justify-end gap-3">
              <button
                class="rounded-lg border border-secondary-200 px-4 py-2 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                @click="showApprovalModal = false"
              >
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white hover:bg-green-700 disabled:opacity-50"
                :disabled="isSubmittingForApproval"
                @click="submitForApproval"
              >
                <i v-if="isSubmittingForApproval" class="pi pi-spin pi-spinner me-1 text-xs"></i>
                <i v-else class="pi pi-send me-1 text-xs"></i>
                {{ isSubmittingForApproval
                  ? (locale === 'ar' ? 'جاري التقديم...' : 'Submitting...')
                  : (locale === 'ar' ? 'تأكيد التقديم' : 'Confirm Submit') }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══ Approve Step Modal ═══ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showApproveModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
            <div class="mb-4 flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-green-100">
                <i class="pi pi-check text-lg text-green-600"></i>
              </div>
              <h3 class="text-base font-bold text-secondary-800">
                {{ locale === 'ar' ? 'اعتماد الخطوة' : 'Approve Step' }}
              </h3>
            </div>
            <div>
              <label class="mb-1 block text-xs font-semibold text-secondary-600">
                {{ locale === 'ar' ? 'ملاحظات (اختياري)' : 'Comments (optional)' }}
              </label>
              <textarea
                v-model="approveComment"
                rows="3"
                class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                :placeholder="locale === 'ar' ? 'أضف ملاحظاتك هنا...' : 'Add your comments here...'"
              ></textarea>
            </div>
            <div class="mt-4 flex justify-end gap-3">
              <button class="rounded-lg border border-secondary-200 px-4 py-2 text-sm font-medium text-secondary-600 hover:bg-secondary-50" @click="showApproveModal = false">
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white hover:bg-green-700" @click="handleApproveStep">
                <i class="pi pi-check me-1 text-xs"></i>
                {{ locale === 'ar' ? 'تأكيد الاعتماد' : 'Confirm Approval' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══ Reject Step Modal ═══ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showRejectModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
            <div class="mb-4 flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-red-100">
                <i class="pi pi-times text-lg text-red-600"></i>
              </div>
              <h3 class="text-base font-bold text-secondary-800">
                {{ locale === 'ar' ? 'رفض الخطوة' : 'Reject Step' }}
              </h3>
            </div>
            <div>
              <label class="mb-1 block text-xs font-semibold text-secondary-600">
                {{ locale === 'ar' ? 'سبب الرفض' : 'Rejection Reason' }}
                <span class="text-red-500">*</span>
              </label>
              <textarea
                v-model="rejectComment"
                rows="3"
                class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                :placeholder="locale === 'ar' ? 'اذكر سبب الرفض بالتفصيل...' : 'Describe the reason for rejection...'"
              ></textarea>
            </div>
            <div class="mt-4 flex justify-end gap-3">
              <button class="rounded-lg border border-secondary-200 px-4 py-2 text-sm font-medium text-secondary-600 hover:bg-secondary-50" @click="showRejectModal = false">
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50"
                :disabled="!rejectComment.trim()"
                @click="handleRejectStep"
              >
                <i class="pi pi-times me-1 text-xs"></i>
                {{ locale === 'ar' ? 'تأكيد الرفض' : 'Confirm Rejection' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══ Approval Success/Error Banners ═══ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="approvalSuccess" class="fixed start-1/2 top-4 z-[60] -translate-x-1/2">
          <div class="flex items-center gap-3 rounded-xl border border-green-200 bg-green-50 px-5 py-3 shadow-lg">
            <i class="pi pi-check-circle text-lg text-green-600"></i>
            <p class="text-sm font-medium text-green-700">{{ approvalSuccess }}</p>
            <button class="text-xs text-green-600 hover:underline" @click="approvalSuccess = ''">
              <i class="pi pi-times"></i>
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <Transition name="fade">
        <div v-if="approvalError" class="fixed start-1/2 top-4 z-[60] -translate-x-1/2">
          <div class="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-5 py-3 shadow-lg">
            <i class="pi pi-exclamation-triangle text-lg text-red-600"></i>
            <p class="text-sm font-medium text-red-700">{{ approvalError }}</p>
            <button class="text-xs text-red-600 hover:underline" @click="approvalError = ''">
              <i class="pi pi-times"></i>
            </button>
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

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
