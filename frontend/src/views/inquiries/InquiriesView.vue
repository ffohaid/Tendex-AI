<script setup lang="ts">
/**
 * InquiriesView — Comprehensive Inquiries Management Page.
 *
 * Features:
 * - Statistics dashboard (total, new, in-progress, pending approval, approved, overdue)
 * - Paginated inquiry list with filters (status, category, priority, competition, search)
 * - Create new inquiry / Bulk import from Etimad
 * - Inquiry detail with full workflow: assign, AI answer, submit, approve/reject, close
 * - AI-powered answer generation with confidence score
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import {
  fetchInquiries,
  fetchInquiryStats,
  fetchInquiryById,
  createInquiry,
  assignInquiry,
  submitAnswer,
  approveInquiry,
  rejectInquiry,
  closeInquiry,
  generateAiAnswer,
  bulkImportInquiries,
} from '@/services/inquiryService'
import { fetchCommittees } from '@/services/committeeService'
import { fetchRfpList } from '@/services/rfpService'
import type {
  Inquiry,
  InquiryFilters,
  InquiryStats,
  InquiryResponse,
} from '@/types/inquiry'

const { t, locale } = useI18n()
const { formatDateTime, formatNumber } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const isLoading = ref(false)
const error = ref('')
const inquiries = ref<Inquiry[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
const isRtl = computed(() => locale.value === 'ar')

const stats = ref<InquiryStats>({
  total: 0,
  new: 0,
  inProgress: 0,
  pendingApproval: 0,
  approved: 0,
  rejected: 0,
  overdue: 0,
  averageResponseTimeHours: 0,
})

/* Filters */
const filters = ref<InquiryFilters>({
  status: '',
  category: '',
  priority: '',
  competitionId: '',
  search: '',
})

/* Active Tab */
const activeTab = ref<'list' | 'create' | 'import'>('list')

/* Detail Panel */
const showDetail = ref(false)
const selectedInquiry = ref<Inquiry | null>(null)
const detailLoading = ref(false)
const detailTab = ref<'info' | 'responses' | 'assign'>('info')

/* Create Form */
const createForm = ref({
  competitionId: '',
  questionText: '',
  category: 'General',
  priority: 'Medium',
  supplierName: '',
  etimadReferenceNumber: '',
  internalNotes: '',
})
const isCreating = ref(false)

/* Bulk Import */
const bulkImportForm = ref({
  competitionId: '',
  rawText: '',
})
const parsedInquiries = ref<Array<{ questionText: string; supplierName: string }>>([])
const isImporting = ref(false)

/* Assignment */
const assignForm = ref({ userId: '', userName: '', committeeId: '' })
const isAssigning = ref(false)
const committees = ref<Array<{ id: string; nameAr: string; nameEn: string; members: Array<{ userId: string; userFullName: string }> }>>([])

/* Answer */
const answerText = ref('')
const isSubmittingAnswer = ref(false)

/* AI Answer */
const aiContext = ref('')
const isGeneratingAi = ref(false)
const aiResult = ref<{ answerText: string; confidenceScore: number; modelUsed: string; sources: string | null; responseId: string } | null>(null)

/* Approve/Reject */
const isApproving = ref(false)
const isRejecting = ref(false)
const rejectReason = ref('')
const showRejectDialog = ref(false)

/* Close */
const isClosing = ref(false)

/* Competitions for dropdown */
const competitions = ref<Array<{ id: string; projectName: string; referenceNumber: string }>>([])

/* ------------------------------------------------------------------ */
/*  Stats Cards Configuration                                          */
/* ------------------------------------------------------------------ */
const statsCards = computed(() => [
  { key: 'total', icon: 'pi-comments', color: 'text-primary', bgColor: 'bg-primary/10', value: formatNumber(stats.value.total), label: 'إجمالي الاستفسارات' },
  { key: 'new', icon: 'pi-plus-circle', color: 'text-blue-500', bgColor: 'bg-blue-500/10', value: formatNumber(stats.value.new), label: 'جديد' },
  { key: 'inProgress', icon: 'pi-spinner', color: 'text-yellow-600', bgColor: 'bg-yellow-500/10', value: formatNumber(stats.value.inProgress), label: 'قيد الإجابة' },
  { key: 'pendingApproval', icon: 'pi-clock', color: 'text-orange-500', bgColor: 'bg-orange-500/10', value: formatNumber(stats.value.pendingApproval), label: 'بانتظار الاعتماد' },
  { key: 'approved', icon: 'pi-check-circle', color: 'text-green-600', bgColor: 'bg-green-500/10', value: formatNumber(stats.value.approved), label: 'معتمد' },
  { key: 'overdue', icon: 'pi-exclamation-triangle', color: 'text-red-600', bgColor: 'bg-red-500/10', value: formatNumber(stats.value.overdue), label: 'متأخر' },
])

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getStatusConfig(status: string) {
  const config: Record<string, { bgClass: string; textClass: string; label: string }> = {
    New: { bgClass: 'bg-blue-500/10', textClass: 'text-blue-600', label: 'جديد' },
    InProgress: { bgClass: 'bg-yellow-500/10', textClass: 'text-yellow-600', label: 'قيد الإجابة' },
    PendingApproval: { bgClass: 'bg-orange-500/10', textClass: 'text-orange-600', label: 'بانتظار الاعتماد' },
    Approved: { bgClass: 'bg-green-500/10', textClass: 'text-green-600', label: 'معتمد' },
    Rejected: { bgClass: 'bg-red-500/10', textClass: 'text-red-600', label: 'مرفوض' },
    Closed: { bgClass: 'bg-gray-200', textClass: 'text-gray-600', label: 'مغلق' },
  }
  return config[status] || config.New
}

function getCategoryConfig(category: string) {
  const config: Record<string, { bgClass: string; textClass: string; label: string }> = {
    General: { bgClass: 'bg-gray-100', textClass: 'text-gray-600', label: 'عام' },
    Technical: { bgClass: 'bg-indigo-500/10', textClass: 'text-indigo-600', label: 'فني' },
    Financial: { bgClass: 'bg-emerald-500/10', textClass: 'text-emerald-600', label: 'مالي' },
    Administrative: { bgClass: 'bg-sky-500/10', textClass: 'text-sky-600', label: 'إداري' },
    Legal: { bgClass: 'bg-purple-500/10', textClass: 'text-purple-600', label: 'قانوني' },
  }
  return config[category] || config.General
}

function getPriorityConfig(priority: string) {
  const config: Record<string, { bgClass: string; textClass: string; label: string }> = {
    Critical: { bgClass: 'bg-red-500/10', textClass: 'text-red-600', label: 'حرج' },
    High: { bgClass: 'bg-orange-500/10', textClass: 'text-orange-600', label: 'عالي' },
    Medium: { bgClass: 'bg-yellow-500/10', textClass: 'text-yellow-600', label: 'متوسط' },
    Low: { bgClass: 'bg-gray-100', textClass: 'text-gray-600', label: 'منخفض' },
  }
  return config[priority] || config.Medium
}

/* ------------------------------------------------------------------ */
/*  Data Loading                                                       */
/* ------------------------------------------------------------------ */
async function loadInquiries(): Promise<void> {
  isLoading.value = true
  error.value = ''
  try {
    const [inquiriesResult, statsResult] = await Promise.all([
      fetchInquiries(currentPage.value, pageSize.value, filters.value),
      fetchInquiryStats(),
    ])
    inquiries.value = inquiriesResult.items
    totalCount.value = inquiriesResult.totalCount
    stats.value = statsResult
  } catch (err: unknown) {
    console.warn('[InquiriesView] API unavailable:', err)
    inquiries.value = []
    error.value = err instanceof Error ? err.message : 'حدث خطأ أثناء تحميل الاستفسارات'
  } finally {
    isLoading.value = false
  }
}

async function loadCompetitions(): Promise<void> {
  try {
    const result = await fetchRfpList({ page: 1, pageSize: 100 })
    if (result.success && result.data) {
      competitions.value = result.data.items.map((c: Record<string, unknown>) => ({
        id: String(c.id || ''),
        projectName: String(c.projectName || c.projectNameAr || ''),
        referenceNumber: String(c.referenceNumber || ''),
      }))
    }
  } catch {
    console.warn('[InquiriesView] Failed to load competitions')
  }
}

async function loadCommittees(): Promise<void> {
  try {
    const result = await fetchCommittees({ page: 1, pageSize: 50 })
    if (result?.items) {
      committees.value = result.items.map((c: Record<string, unknown>) => ({
        id: String(c.id || ''),
        nameAr: String(c.nameAr || c.name || ''),
        nameEn: String(c.nameEn || c.name || ''),
        members: Array.isArray(c.members) ? c.members : [],
      }))
    }
  } catch {
    console.warn('[InquiriesView] Failed to load committees')
  }
}

function applyFilters(): void {
  currentPage.value = 1
  loadInquiries()
}

function resetFilters(): void {
  filters.value = { status: '', category: '', priority: '', competitionId: '', search: '' }
  currentPage.value = 1
  loadInquiries()
}

function goToPage(page: number): void {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    loadInquiries()
  }
}

/* ------------------------------------------------------------------ */
/*  Detail                                                             */
/* ------------------------------------------------------------------ */
async function openDetail(inquiry: Inquiry): Promise<void> {
  showDetail.value = true
  detailTab.value = 'info'
  detailLoading.value = true
  answerText.value = ''
  aiResult.value = null
  aiContext.value = ''
  rejectReason.value = ''
  showRejectDialog.value = false
  try {
    selectedInquiry.value = await fetchInquiryById(inquiry.id)
  } catch {
    selectedInquiry.value = inquiry
  } finally {
    detailLoading.value = false
  }
}

function closeDetail(): void {
  showDetail.value = false
  selectedInquiry.value = null
}

/* ------------------------------------------------------------------ */
/*  Create Inquiry                                                     */
/* ------------------------------------------------------------------ */
async function handleCreate(): Promise<void> {
  if (!createForm.value.competitionId || !createForm.value.questionText.trim()) return
  isCreating.value = true
  try {
    await createInquiry({
      competitionId: createForm.value.competitionId,
      questionText: createForm.value.questionText,
      category: createForm.value.category,
      priority: createForm.value.priority,
      supplierName: createForm.value.supplierName || undefined,
      etimadReferenceNumber: createForm.value.etimadReferenceNumber || undefined,
      internalNotes: createForm.value.internalNotes || undefined,
    })
    createForm.value = { competitionId: '', questionText: '', category: 'General', priority: 'Medium', supplierName: '', etimadReferenceNumber: '', internalNotes: '' }
    activeTab.value = 'list'
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to create inquiry:', err)
    error.value = 'فشل في إنشاء الاستفسار'
  } finally {
    isCreating.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Bulk Import                                                        */
/* ------------------------------------------------------------------ */
function parseBulkText(): void {
  const lines = bulkImportForm.value.rawText.split('\n').filter(l => l.trim())
  parsedInquiries.value = lines.map(line => {
    const parts = line.split('|').map(p => p.trim())
    return {
      questionText: parts[0] || line.trim(),
      supplierName: parts[1] || '',
    }
  })
}

watch(() => bulkImportForm.value.rawText, parseBulkText)

async function handleBulkImport(): Promise<void> {
  if (!bulkImportForm.value.competitionId || parsedInquiries.value.length === 0) return
  isImporting.value = true
  try {
    await bulkImportInquiries({
      competitionId: bulkImportForm.value.competitionId,
      inquiries: parsedInquiries.value.map(q => ({
        questionText: q.questionText,
        supplierName: q.supplierName || undefined,
        category: 'General',
        priority: 'Medium',
      })),
    })
    bulkImportForm.value = { competitionId: '', rawText: '' }
    parsedInquiries.value = []
    activeTab.value = 'list'
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to bulk import:', err)
    error.value = 'فشل في الاستيراد المجمّع'
  } finally {
    isImporting.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Assign                                                             */
/* ------------------------------------------------------------------ */
async function handleAssign(): Promise<void> {
  if (!selectedInquiry.value) return
  isAssigning.value = true
  try {
    await assignInquiry(selectedInquiry.value.id, {
      userId: assignForm.value.userId || undefined,
      userName: assignForm.value.userName || undefined,
      committeeId: assignForm.value.committeeId || undefined,
    })
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    assignForm.value = { userId: '', userName: '', committeeId: '' }
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to assign:', err)
  } finally {
    isAssigning.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  AI Answer Generation                                               */
/* ------------------------------------------------------------------ */
async function handleGenerateAi(): Promise<void> {
  if (!selectedInquiry.value) return
  isGeneratingAi.value = true
  aiResult.value = null
  try {
    const result = await generateAiAnswer(selectedInquiry.value.id, {
      additionalContext: aiContext.value || undefined,
      useRag: true,
    })
    aiResult.value = result
    answerText.value = result.answerText
    // Reload to get updated responses
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
  } catch (err) {
    console.error('[InquiriesView] AI generation failed:', err)
    error.value = 'فشل في توليد الإجابة بالذكاء الاصطناعي'
  } finally {
    isGeneratingAi.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Submit Answer                                                      */
/* ------------------------------------------------------------------ */
async function handleSubmitAnswer(): Promise<void> {
  if (!selectedInquiry.value || !answerText.value.trim()) return
  isSubmittingAnswer.value = true
  try {
    await submitAnswer(selectedInquiry.value.id, {
      answerText: answerText.value,
      isAiAssisted: aiResult.value !== null,
    })
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    answerText.value = ''
    aiResult.value = null
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to submit answer:', err)
  } finally {
    isSubmittingAnswer.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Approve / Reject                                                   */
/* ------------------------------------------------------------------ */
async function handleApprove(): Promise<void> {
  if (!selectedInquiry.value) return
  isApproving.value = true
  try {
    await approveInquiry(selectedInquiry.value.id)
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to approve:', err)
  } finally {
    isApproving.value = false
  }
}

async function handleReject(): Promise<void> {
  if (!selectedInquiry.value || !rejectReason.value.trim()) return
  isRejecting.value = true
  try {
    await rejectInquiry(selectedInquiry.value.id, rejectReason.value)
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    showRejectDialog.value = false
    rejectReason.value = ''
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to reject:', err)
  } finally {
    isRejecting.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Close Inquiry                                                      */
/* ------------------------------------------------------------------ */
async function handleClose(): Promise<void> {
  if (!selectedInquiry.value) return
  isClosing.value = true
  try {
    await closeInquiry(selectedInquiry.value.id)
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to close:', err)
  } finally {
    isClosing.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Select AI Response                                                 */
/* ------------------------------------------------------------------ */
function selectResponse(response: InquiryResponse): void {
  answerText.value = response.answerText
}

/* ------------------------------------------------------------------ */
/*  Init                                                               */
/* ------------------------------------------------------------------ */
onMounted(async () => {
  await Promise.all([loadInquiries(), loadCompetitions(), loadCommittees()])
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">الاستفسارات</h1>
        <p class="mt-1 text-sm text-tertiary">إدارة استفسارات الموردين على كراسات الشروط والمواصفات</p>
      </div>
      <div class="flex items-center gap-2">
        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary/90"
          @click="activeTab = 'create'"
        >
          <i class="pi pi-plus" />
          إضافة استفسار
        </button>
        <button
          class="flex items-center gap-2 rounded-lg border border-primary/30 bg-primary/5 px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary/10"
          @click="activeTab = 'import'"
        >
          <i class="pi pi-upload" />
          استيراد من اعتماد
        </button>
        <button
          class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
          :disabled="isLoading"
          @click="loadInquiries"
        >
          <i class="pi" :class="isLoading ? 'pi-spinner pi-spin' : 'pi-refresh'" />
          تحديث
        </button>
      </div>
    </div>

    <!-- Error Alert -->
    <div v-if="error" class="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-4">
      <i class="pi pi-exclamation-triangle text-red-600" />
      <div class="flex-1">
        <p class="text-sm font-medium text-red-700">{{ error }}</p>
      </div>
      <button class="rounded-lg px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-100" @click="error = ''">
        إغلاق
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-6">
      <div v-for="card in statsCards" :key="card.key" class="card flex items-center gap-3 !p-4">
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg" :class="card.bgColor">
          <i class="pi text-base" :class="[card.icon, card.color]" />
        </div>
        <div>
          <p class="text-[11px] text-tertiary">{{ card.label }}</p>
          <p class="text-lg font-bold text-secondary">{{ card.value }}</p>
        </div>
      </div>
    </div>

    <!-- ═══════════════════ CREATE TAB ═══════════════════ -->
    <div v-if="activeTab === 'create'" class="card">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-lg font-bold text-secondary">
          <i class="pi pi-plus-circle me-2 text-primary" />
          إضافة استفسار جديد
        </h2>
        <button class="rounded-lg px-3 py-1.5 text-sm text-tertiary hover:bg-surface-muted" @click="activeTab = 'list'">
          <i class="pi pi-times" />
        </button>
      </div>
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">المنافسة *</label>
          <select v-model="createForm.competitionId" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">اختر المنافسة...</option>
            <option v-for="comp in competitions" :key="comp.id" :value="comp.id">{{ comp.projectName }} ({{ comp.referenceNumber }})</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">اسم المورد</label>
          <input v-model="createForm.supplierName" type="text" placeholder="اسم المورد المستفسر..." class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">التصنيف</label>
          <select v-model="createForm.category" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="General">عام</option>
            <option value="Technical">فني</option>
            <option value="Financial">مالي</option>
            <option value="Administrative">إداري</option>
            <option value="Legal">قانوني</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">الأولوية</label>
          <select v-model="createForm.priority" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="Low">منخفض</option>
            <option value="Medium">متوسط</option>
            <option value="High">عالي</option>
            <option value="Critical">حرج</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">رقم مرجع اعتماد</label>
          <input v-model="createForm.etimadReferenceNumber" type="text" placeholder="رقم الاستفسار في منصة اعتماد..." class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="md:col-span-2">
          <label class="mb-1 block text-xs font-medium text-tertiary">نص الاستفسار *</label>
          <textarea v-model="createForm.questionText" rows="4" placeholder="اكتب نص استفسار المورد هنا..." class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="md:col-span-2">
          <label class="mb-1 block text-xs font-medium text-tertiary">ملاحظات داخلية</label>
          <textarea v-model="createForm.internalNotes" rows="2" placeholder="ملاحظات داخلية للفريق..." class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
      <div class="mt-4 flex justify-end gap-2">
        <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="activeTab = 'list'">إلغاء</button>
        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          :disabled="!createForm.competitionId || !createForm.questionText.trim() || isCreating"
          @click="handleCreate"
        >
          <i class="pi" :class="isCreating ? 'pi-spinner pi-spin' : 'pi-check'" />
          حفظ الاستفسار
        </button>
      </div>
    </div>

    <!-- ═══════════════════ BULK IMPORT TAB ═══════════════════ -->
    <div v-if="activeTab === 'import'" class="card">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-lg font-bold text-secondary">
          <i class="pi pi-upload me-2 text-primary" />
          استيراد مجمّع من اعتماد
        </h2>
        <button class="rounded-lg px-3 py-1.5 text-sm text-tertiary hover:bg-surface-muted" @click="activeTab = 'list'">
          <i class="pi pi-times" />
        </button>
      </div>
      <p class="mb-4 text-sm text-tertiary">
        الصق استفسارات الموردين المنسوخة من منصة اعتماد. كل سطر يمثل استفساراً واحداً.
        يمكنك استخدام الفاصل | لفصل نص الاستفسار عن اسم المورد (مثال: نص الاستفسار | اسم المورد).
      </p>
      <div class="grid grid-cols-1 gap-4">
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">المنافسة *</label>
          <select v-model="bulkImportForm.competitionId" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">اختر المنافسة...</option>
            <option v-for="comp in competitions" :key="comp.id" :value="comp.id">{{ comp.projectName }} ({{ comp.referenceNumber }})</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">الاستفسارات (سطر لكل استفسار) *</label>
          <textarea v-model="bulkImportForm.rawText" rows="8" placeholder="ما هي مدة تنفيذ المشروع؟ | شركة الأفق&#10;هل يمكن تقديم العرض بالدولار؟ | شركة التقنية&#10;ما هي متطلبات الضمان البنكي؟" class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary font-mono focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
      <!-- Preview -->
      <div v-if="parsedInquiries.length > 0" class="mt-4">
        <p class="mb-2 text-xs font-medium text-tertiary">معاينة ({{ parsedInquiries.length }} استفسار)</p>
        <div class="max-h-48 overflow-y-auto rounded-lg border border-surface-dim">
          <table class="w-full text-sm">
            <thead class="bg-surface-muted">
              <tr>
                <th class="px-3 py-2 text-start text-xs font-medium text-tertiary">#</th>
                <th class="px-3 py-2 text-start text-xs font-medium text-tertiary">نص الاستفسار</th>
                <th class="px-3 py-2 text-start text-xs font-medium text-tertiary">المورد</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(q, i) in parsedInquiries" :key="i" class="border-t border-surface-dim">
                <td class="px-3 py-2 text-tertiary">{{ i + 1 }}</td>
                <td class="px-3 py-2 text-secondary">{{ q.questionText }}</td>
                <td class="px-3 py-2 text-tertiary">{{ q.supplierName || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      <div class="mt-4 flex justify-end gap-2">
        <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="activeTab = 'list'">إلغاء</button>
        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          :disabled="!bulkImportForm.competitionId || parsedInquiries.length === 0 || isImporting"
          @click="handleBulkImport"
        >
          <i class="pi" :class="isImporting ? 'pi-spinner pi-spin' : 'pi-upload'" />
          استيراد {{ parsedInquiries.length }} استفسار
        </button>
      </div>
    </div>

    <!-- ═══════════════════ LIST TAB ═══════════════════ -->
    <template v-if="activeTab === 'list'">
      <!-- Filters -->
      <div class="card">
        <div class="flex flex-wrap items-end gap-4">
          <div class="flex-1 min-w-[200px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">بحث</label>
            <div class="relative">
              <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary" />
              <input v-model="filters.search" type="text" placeholder="البحث في الاستفسارات..." class="w-full rounded-lg border border-surface-dim bg-white py-2 pe-3 ps-9 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" @keyup.enter="applyFilters" />
            </div>
          </div>
          <div class="min-w-[140px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">الحالة</label>
            <select v-model="filters.status" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">جميع الحالات</option>
              <option value="New">جديد</option>
              <option value="InProgress">قيد الإجابة</option>
              <option value="PendingApproval">بانتظار الاعتماد</option>
              <option value="Approved">معتمد</option>
              <option value="Rejected">مرفوض</option>
              <option value="Closed">مغلق</option>
            </select>
          </div>
          <div class="min-w-[130px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">التصنيف</label>
            <select v-model="filters.category" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">جميع التصنيفات</option>
              <option value="General">عام</option>
              <option value="Technical">فني</option>
              <option value="Financial">مالي</option>
              <option value="Administrative">إداري</option>
              <option value="Legal">قانوني</option>
            </select>
          </div>
          <div class="min-w-[130px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">الأولوية</label>
            <select v-model="filters.priority" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">جميع الأولويات</option>
              <option value="Critical">حرج</option>
              <option value="High">عالي</option>
              <option value="Medium">متوسط</option>
              <option value="Low">منخفض</option>
            </select>
          </div>
          <div class="flex gap-2">
            <button class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90" @click="applyFilters">
              <i class="pi pi-search me-1" /> بحث
            </button>
            <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="resetFilters">
              <i class="pi pi-filter-slash me-1" /> إعادة تعيين
            </button>
          </div>
        </div>
      </div>

      <!-- Inquiries List -->
      <div class="card">
        <!-- Loading -->
        <div v-if="isLoading" class="space-y-3">
          <div v-for="i in 5" :key="i" class="h-20 animate-pulse rounded-lg bg-surface-muted" />
        </div>

        <!-- Empty -->
        <div v-else-if="inquiries.length === 0" class="flex flex-col items-center justify-center py-16">
          <i class="pi pi-comments text-5xl text-surface-dim" />
          <p class="mt-4 text-base font-medium text-secondary">لا توجد استفسارات</p>
          <p class="mt-1 text-sm text-tertiary">لم يتم العثور على أي استفسارات تطابق معايير البحث</p>
          <button class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90" @click="activeTab = 'create'">
            <i class="pi pi-plus me-1" /> إضافة استفسار جديد
          </button>
        </div>

        <!-- Items -->
        <div v-else class="space-y-3">
          <div
            v-for="inquiry in inquiries"
            :key="inquiry.id"
            class="cursor-pointer rounded-xl border border-surface-dim p-4 transition-all hover:border-primary/30 hover:shadow-sm"
            @click="openDetail(inquiry)"
          >
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <h4 class="text-sm font-semibold text-secondary line-clamp-2">{{ inquiry.questionText }}</h4>
                  <span class="shrink-0 text-xs text-tertiary">#{{ inquiry.referenceNumber }}</span>
                </div>
                <div class="mt-1.5 flex flex-wrap items-center gap-3 text-xs text-tertiary">
                  <span v-if="inquiry.competitionName">
                    <i class="pi pi-file-edit me-1" />{{ inquiry.competitionName }}
                  </span>
                  <span v-if="inquiry.supplierName">
                    <i class="pi pi-building me-1" />{{ inquiry.supplierName }}
                  </span>
                  <span v-if="inquiry.assignedToUserName">
                    <i class="pi pi-user me-1" />{{ inquiry.assignedToUserName }}
                  </span>
                  <span>
                    <i class="pi pi-calendar me-1" />{{ formatDateTime(inquiry.createdAt) }}
                  </span>
                  <span v-if="inquiry.etimadReferenceNumber" class="text-primary/70">
                    <i class="pi pi-external-link me-1" />اعتماد: {{ inquiry.etimadReferenceNumber }}
                  </span>
                </div>
              </div>
              <div class="flex flex-col items-end gap-1.5">
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getStatusConfig(inquiry.status).bgClass, getStatusConfig(inquiry.status).textClass]">
                  {{ getStatusConfig(inquiry.status).label }}
                </span>
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getCategoryConfig(inquiry.category).bgClass, getCategoryConfig(inquiry.category).textClass]">
                  {{ getCategoryConfig(inquiry.category).label }}
                </span>
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getPriorityConfig(inquiry.priority).bgClass, getPriorityConfig(inquiry.priority).textClass]">
                  {{ getPriorityConfig(inquiry.priority).label }}
                </span>
                <span v-if="inquiry.isOverdue" class="flex items-center gap-1 text-xs font-medium text-red-600">
                  <i class="pi pi-exclamation-circle text-xs" /> متأخر
                </span>
                <span v-if="inquiry.isAiAssisted" class="flex items-center gap-1 text-xs font-medium text-purple-600">
                  <i class="pi pi-sparkles text-xs" /> AI
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="mt-4 flex items-center justify-between border-t border-surface-dim pt-4">
          <p class="text-xs text-tertiary">
            عرض {{ (currentPage - 1) * pageSize + 1 }} إلى {{ Math.min(currentPage * pageSize, totalCount) }} من {{ totalCount }}
          </p>
          <div class="flex items-center gap-1">
            <button class="rounded-lg px-3 py-1.5 text-sm text-secondary hover:bg-surface-muted disabled:opacity-50" :disabled="currentPage <= 1" @click="goToPage(currentPage - 1)">
              <i class="pi pi-chevron-right" />
            </button>
            <template v-for="page in totalPages" :key="page">
              <button
                v-if="page === 1 || page === totalPages || (page >= currentPage - 1 && page <= currentPage + 1)"
                class="min-w-[2rem] rounded-lg px-2 py-1.5 text-sm font-medium transition-colors"
                :class="page === currentPage ? 'bg-primary text-white' : 'text-secondary hover:bg-surface-muted'"
                @click="goToPage(page)"
              >{{ page }}</button>
              <span v-else-if="page === currentPage - 2 || page === currentPage + 2" class="px-1 text-tertiary">...</span>
            </template>
            <button class="rounded-lg px-3 py-1.5 text-sm text-secondary hover:bg-surface-muted disabled:opacity-50" :disabled="currentPage >= totalPages" @click="goToPage(currentPage + 1)">
              <i class="pi pi-chevron-left" />
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══════════════════ DETAIL PANEL ═══════════════════ -->
    <Teleport to="body">
      <div v-if="showDetail && selectedInquiry" class="fixed inset-0 z-50 flex items-start justify-center bg-black/50 p-4 pt-8 overflow-y-auto" @click.self="closeDetail">
        <div class="w-full max-w-4xl overflow-hidden rounded-2xl bg-white shadow-xl my-4">
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-surface-dim bg-gradient-to-l from-primary/5 to-white px-6 py-4">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getStatusConfig(selectedInquiry.status).bgClass, getStatusConfig(selectedInquiry.status).textClass]">
                  {{ getStatusConfig(selectedInquiry.status).label }}
                </span>
                <span class="text-xs text-tertiary">#{{ selectedInquiry.referenceNumber }}</span>
              </div>
              <p v-if="selectedInquiry.competitionName" class="mt-1 text-xs text-tertiary">
                <i class="pi pi-file-edit me-1" />{{ selectedInquiry.competitionName }}
              </p>
            </div>
            <button class="rounded-lg p-2 text-tertiary hover:bg-surface-muted hover:text-secondary" @click="closeDetail">
              <i class="pi pi-times" />
            </button>
          </div>

          <!-- Tabs -->
          <div class="flex border-b border-surface-dim bg-surface-muted/30">
            <button
              v-for="tab in [
                { key: 'info', label: 'تفاصيل الاستفسار', icon: 'pi-info-circle' },
                { key: 'responses', label: 'الإجابات والذكاء الاصطناعي', icon: 'pi-sparkles' },
                { key: 'assign', label: 'الإسناد', icon: 'pi-users' },
              ]"
              :key="tab.key"
              class="flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors border-b-2"
              :class="detailTab === tab.key ? 'border-primary text-primary' : 'border-transparent text-tertiary hover:text-secondary'"
              @click="detailTab = tab.key as 'info' | 'responses' | 'assign'"
            >
              <i class="pi text-sm" :class="tab.icon" />
              {{ tab.label }}
            </button>
          </div>

          <!-- Loading -->
          <div v-if="detailLoading" class="flex items-center justify-center py-16">
            <i class="pi pi-spinner pi-spin text-2xl text-primary" />
          </div>

          <!-- Tab: Info -->
          <div v-else-if="detailTab === 'info'" class="px-6 py-5 space-y-5">
            <!-- Meta Grid -->
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div>
                <p class="text-xs text-tertiary">المورد</p>
                <p class="text-sm font-medium text-secondary">{{ selectedInquiry.supplierName || '-' }}</p>
              </div>
              <div>
                <p class="text-xs text-tertiary">التصنيف</p>
                <span class="rounded-full px-2 py-0.5 text-xs font-medium" :class="[getCategoryConfig(selectedInquiry.category).bgClass, getCategoryConfig(selectedInquiry.category).textClass]">
                  {{ getCategoryConfig(selectedInquiry.category).label }}
                </span>
              </div>
              <div>
                <p class="text-xs text-tertiary">الأولوية</p>
                <span class="rounded-full px-2 py-0.5 text-xs font-medium" :class="[getPriorityConfig(selectedInquiry.priority).bgClass, getPriorityConfig(selectedInquiry.priority).textClass]">
                  {{ getPriorityConfig(selectedInquiry.priority).label }}
                </span>
              </div>
              <div>
                <p class="text-xs text-tertiary">تاريخ الإنشاء</p>
                <p class="text-sm font-medium text-secondary">{{ formatDateTime(selectedInquiry.createdAt) }}</p>
              </div>
              <div v-if="selectedInquiry.etimadReferenceNumber">
                <p class="text-xs text-tertiary">مرجع اعتماد</p>
                <p class="text-sm font-medium text-primary">{{ selectedInquiry.etimadReferenceNumber }}</p>
              </div>
              <div v-if="selectedInquiry.assignedToUserName">
                <p class="text-xs text-tertiary">مُسند إلى</p>
                <p class="text-sm font-medium text-secondary">{{ selectedInquiry.assignedToUserName }}</p>
              </div>
              <div v-if="selectedInquiry.slaDeadline">
                <p class="text-xs text-tertiary">الموعد النهائي</p>
                <p class="text-sm font-medium" :class="selectedInquiry.isOverdue ? 'text-red-600' : 'text-secondary'">
                  {{ formatDateTime(selectedInquiry.slaDeadline) }}
                  <span v-if="selectedInquiry.isOverdue" class="text-xs">(متأخر)</span>
                </p>
              </div>
              <div v-if="selectedInquiry.isExportedToEtimad">
                <p class="text-xs text-tertiary">التصدير لاعتماد</p>
                <p class="text-sm font-medium text-green-600">
                  <i class="pi pi-check-circle me-1" />تم التصدير {{ formatDateTime(selectedInquiry.exportedToEtimadAt) }}
                </p>
              </div>
            </div>

            <!-- Question -->
            <div>
              <p class="mb-1.5 text-xs font-semibold text-tertiary">نص الاستفسار</p>
              <div class="rounded-lg bg-blue-50 border border-blue-100 p-4 text-sm leading-relaxed text-secondary whitespace-pre-wrap">
                {{ selectedInquiry.questionText }}
              </div>
            </div>

            <!-- Approved Answer -->
            <div v-if="selectedInquiry.approvedAnswer">
              <p class="mb-1.5 text-xs font-semibold text-tertiary">الإجابة المعتمدة</p>
              <div class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm leading-relaxed text-secondary whitespace-pre-wrap">
                {{ selectedInquiry.approvedAnswer }}
              </div>
              <p class="mt-1 text-xs text-tertiary">
                اعتمد بواسطة: {{ selectedInquiry.approvedBy }} — {{ formatDateTime(selectedInquiry.approvedAt) }}
              </p>
            </div>

            <!-- Rejection Reason -->
            <div v-if="selectedInquiry.rejectionReason">
              <p class="mb-1.5 text-xs font-semibold text-tertiary">سبب الرفض</p>
              <div class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm leading-relaxed text-red-700">
                {{ selectedInquiry.rejectionReason }}
              </div>
            </div>

            <!-- Internal Notes -->
            <div v-if="selectedInquiry.internalNotes">
              <p class="mb-1.5 text-xs font-semibold text-tertiary">ملاحظات داخلية</p>
              <div class="rounded-lg bg-yellow-50 border border-yellow-100 p-4 text-sm leading-relaxed text-secondary">
                {{ selectedInquiry.internalNotes }}
              </div>
            </div>
          </div>

          <!-- Tab: Responses & AI -->
          <div v-else-if="detailTab === 'responses'" class="px-6 py-5 space-y-5">
            <!-- AI Generation Section -->
            <div v-if="selectedInquiry.status === 'New' || selectedInquiry.status === 'InProgress' || selectedInquiry.status === 'Rejected'" class="rounded-xl border border-purple-200 bg-gradient-to-l from-purple-50 to-white p-5">
              <div class="flex items-center gap-2 mb-3">
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/10">
                  <i class="pi pi-sparkles text-purple-600" />
                </div>
                <div>
                  <h4 class="text-sm font-bold text-secondary">المساعد الذكي للاستفسارات</h4>
                  <p class="text-xs text-tertiary">توليد إجابة احترافية بالذكاء الاصطناعي بناءً على سياق الكراسة والمنافسة</p>
                </div>
              </div>
              <div class="mb-3">
                <label class="mb-1 block text-xs font-medium text-tertiary">سياق إضافي (اختياري)</label>
                <textarea v-model="aiContext" rows="2" placeholder="أضف أي معلومات إضافية لتحسين جودة الإجابة..." class="w-full rounded-lg border border-purple-200 bg-white p-3 text-sm text-secondary focus:border-purple-400 focus:outline-none focus:ring-1 focus:ring-purple-400" />
              </div>
              <button
                class="flex items-center gap-2 rounded-lg bg-purple-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-purple-700 disabled:opacity-50"
                :disabled="isGeneratingAi"
                @click="handleGenerateAi"
              >
                <i class="pi" :class="isGeneratingAi ? 'pi-spinner pi-spin' : 'pi-sparkles'" />
                {{ isGeneratingAi ? 'جاري التوليد...' : 'توليد إجابة بالذكاء الاصطناعي' }}
              </button>

              <!-- AI Result -->
              <div v-if="aiResult" class="mt-4 rounded-lg border border-purple-200 bg-white p-4">
                <div class="flex items-center justify-between mb-2">
                  <span class="text-xs font-medium text-purple-600">
                    <i class="pi pi-sparkles me-1" />إجابة الذكاء الاصطناعي
                  </span>
                  <div class="flex items-center gap-3">
                    <span class="text-xs text-tertiary">الثقة: {{ aiResult.confidenceScore }}%</span>
                    <span class="text-xs text-tertiary">النموذج: {{ aiResult.modelUsed }}</span>
                  </div>
                </div>
                <div class="rounded-lg bg-purple-50 p-3 text-sm leading-relaxed text-secondary whitespace-pre-wrap">
                  {{ aiResult.answerText }}
                </div>
                <div v-if="aiResult.sources" class="mt-2 text-xs text-tertiary">
                  <i class="pi pi-book me-1" />المصادر: {{ aiResult.sources }}
                </div>
              </div>
            </div>

            <!-- Previous Responses -->
            <div v-if="selectedInquiry.responses && selectedInquiry.responses.length > 0">
              <h4 class="mb-3 text-sm font-bold text-secondary">المسودات والإجابات السابقة ({{ selectedInquiry.responses.length }})</h4>
              <div class="space-y-3">
                <div
                  v-for="response in selectedInquiry.responses"
                  :key="response.id"
                  class="rounded-lg border p-4 transition-all"
                  :class="response.isSelected ? 'border-green-300 bg-green-50' : 'border-surface-dim hover:border-primary/30'"
                >
                  <div class="flex items-center justify-between mb-2">
                    <div class="flex items-center gap-2">
                      <span v-if="response.isAiGenerated" class="rounded-full bg-purple-100 px-2 py-0.5 text-xs font-medium text-purple-600">
                        <i class="pi pi-sparkles me-1" />AI
                      </span>
                      <span v-else class="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-600">
                        <i class="pi pi-user me-1" />يدوي
                      </span>
                      <span v-if="response.isSelected" class="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-600">
                        <i class="pi pi-check me-1" />مختار
                      </span>
                      <span v-if="response.aiConfidenceScore" class="text-xs text-tertiary">
                        الثقة: {{ response.aiConfidenceScore }}%
                      </span>
                    </div>
                    <span class="text-xs text-tertiary">{{ formatDateTime(response.createdAt) }}</span>
                  </div>
                  <p class="text-sm leading-relaxed text-secondary whitespace-pre-wrap">{{ response.answerText }}</p>
                  <div class="mt-2 flex justify-end">
                    <button
                      v-if="selectedInquiry.status !== 'Approved' && selectedInquiry.status !== 'Closed'"
                      class="rounded-lg px-3 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                      @click="selectResponse(response)"
                    >
                      <i class="pi pi-copy me-1" />استخدام هذه الإجابة
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Manual Answer Form -->
            <div v-if="selectedInquiry.status === 'New' || selectedInquiry.status === 'InProgress' || selectedInquiry.status === 'Rejected'">
              <h4 class="mb-2 text-sm font-bold text-secondary">تقديم الإجابة للاعتماد</h4>
              <textarea v-model="answerText" rows="5" placeholder="اكتب الإجابة أو عدّل إجابة الذكاء الاصطناعي..." class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
              <div class="mt-2 flex justify-end">
                <button
                  class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
                  :disabled="!answerText.trim() || isSubmittingAnswer"
                  @click="handleSubmitAnswer"
                >
                  <i class="pi" :class="isSubmittingAnswer ? 'pi-spinner pi-spin' : 'pi-send'" />
                  تقديم للاعتماد
                </button>
              </div>
            </div>

            <!-- Approval Actions -->
            <div v-if="selectedInquiry.status === 'PendingApproval'" class="rounded-xl border border-orange-200 bg-orange-50 p-5">
              <h4 class="mb-3 text-sm font-bold text-secondary">
                <i class="pi pi-check-square me-2 text-orange-600" />
                إجراء الاعتماد
              </h4>
              <p class="mb-4 text-sm text-tertiary">هذا الاستفسار بانتظار اعتمادك. يمكنك اعتماد الإجابة أو رفضها مع ذكر السبب.</p>
              <div class="flex gap-3">
                <button
                  class="flex items-center gap-2 rounded-lg bg-green-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                  :disabled="isApproving"
                  @click="handleApprove"
                >
                  <i class="pi" :class="isApproving ? 'pi-spinner pi-spin' : 'pi-check'" />
                  اعتماد الإجابة
                </button>
                <button
                  class="flex items-center gap-2 rounded-lg bg-red-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-red-700"
                  @click="showRejectDialog = true"
                >
                  <i class="pi pi-times" />
                  رفض
                </button>
              </div>
              <!-- Reject Dialog -->
              <div v-if="showRejectDialog" class="mt-4 rounded-lg border border-red-200 bg-white p-4">
                <label class="mb-1 block text-xs font-medium text-red-600">سبب الرفض *</label>
                <textarea v-model="rejectReason" rows="3" placeholder="اذكر سبب رفض الإجابة..." class="w-full rounded-lg border border-red-200 bg-white p-3 text-sm text-secondary focus:border-red-400 focus:outline-none focus:ring-1 focus:ring-red-400" />
                <div class="mt-2 flex justify-end gap-2">
                  <button class="rounded-lg px-3 py-1.5 text-sm text-tertiary hover:bg-surface-muted" @click="showRejectDialog = false">إلغاء</button>
                  <button
                    class="flex items-center gap-2 rounded-lg bg-red-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
                    :disabled="!rejectReason.trim() || isRejecting"
                    @click="handleReject"
                  >
                    <i class="pi" :class="isRejecting ? 'pi-spinner pi-spin' : 'pi-times'" />
                    تأكيد الرفض
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab: Assign -->
          <div v-else-if="detailTab === 'assign'" class="px-6 py-5 space-y-5">
            <div v-if="selectedInquiry.assignedToUserName" class="rounded-lg border border-green-200 bg-green-50 p-4">
              <p class="text-sm font-medium text-green-700">
                <i class="pi pi-user me-2" />مُسند حالياً إلى: {{ selectedInquiry.assignedToUserName }}
              </p>
            </div>

            <div class="rounded-xl border border-surface-dim p-5">
              <h4 class="mb-4 text-sm font-bold text-secondary">
                <i class="pi pi-users me-2 text-primary" />
                إسناد الاستفسار
              </h4>

              <!-- Assign to Committee -->
              <div class="mb-4">
                <label class="mb-1 block text-xs font-medium text-tertiary">إسناد للجنة</label>
                <select v-model="assignForm.committeeId" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                  <option value="">اختر اللجنة...</option>
                  <option v-for="committee in committees" :key="committee.id" :value="committee.id">
                    {{ isRtl ? committee.nameAr : committee.nameEn }}
                  </option>
                </select>
              </div>

              <!-- Or assign to specific user -->
              <div class="mb-4">
                <label class="mb-1 block text-xs font-medium text-tertiary">أو إسناد لمستخدم محدد</label>
                <input v-model="assignForm.userName" type="text" placeholder="اسم المستخدم..." class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
              </div>

              <div class="flex justify-end">
                <button
                  class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
                  :disabled="(!assignForm.committeeId && !assignForm.userName) || isAssigning"
                  @click="handleAssign"
                >
                  <i class="pi" :class="isAssigning ? 'pi-spinner pi-spin' : 'pi-user-plus'" />
                  إسناد
                </button>
              </div>
            </div>
          </div>

          <!-- Footer Actions -->
          <div class="flex items-center justify-between border-t border-surface-dim px-6 py-4 bg-surface-muted/30">
            <div>
              <button
                v-if="selectedInquiry && selectedInquiry.status !== 'Closed' && selectedInquiry.status !== 'Approved'"
                class="flex items-center gap-2 rounded-lg border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                :disabled="isClosing"
                @click="handleClose"
              >
                <i class="pi" :class="isClosing ? 'pi-spinner pi-spin' : 'pi-ban'" />
                إغلاق الاستفسار
              </button>
            </div>
            <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="closeDetail">
              إغلاق
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
