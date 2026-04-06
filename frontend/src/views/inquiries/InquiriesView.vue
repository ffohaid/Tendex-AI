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
 * - Full i18n support via vue-i18n
 * - English numerals exclusively
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
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
import { httpGet } from '@/services/http'
import RichTextEditor from '@/components/common/RichTextEditor.vue'
import type {
  Inquiry,
  InquiryFilters,
  InquiryStats,
  InquiryResponse,
} from '@/types/inquiry'

const { t, locale } = useI18n()
const authStore = useAuthStore()

const canCreateInquiry = computed(() => authStore.hasPermission('competitions.create'))
// canEditInquiry available for future use
// const canEditInquiry = computed(() => authStore.hasPermission('competitions.edit'))
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
const assignMode = ref<'committee' | 'user'>('committee')
const assignForm = ref({ userId: '', userName: '', committeeId: '' })
const isAssigning = ref(false)
const committees = ref<Array<{ id: string; nameAr: string; nameEn: string; members: Array<{ userId: string; userFullName: string }> }>>([]) 

/* User Search for Assignment */
const userSearchQuery = ref('')
const userSearchResults = ref<Array<{ id: string; fullName: string; email: string; roleName: string }>>([]) 
const isSearchingUsers = ref(false)
const showUserDropdown = ref(false)
let userSearchTimeout: ReturnType<typeof setTimeout> | null = null

/* Answer */
const answerText = ref('')
const isSubmittingAnswer = ref(false)
const isDirectApproving = ref(false)

/* Response Mode: manual = write reply directly, ai = generate via AI */
const responseMode = ref<'manual' | 'ai'>('manual')
const isEditingAiResponse = ref(false)

/* Permission: can the user directly approve without sending for approval? */
const canDirectApprove = computed(() => authStore.hasPermission('inquiries.manage') || authStore.hasPermission('approvals.approve'))

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
  { key: 'total', icon: 'pi-comments', color: 'text-primary', bgColor: 'bg-primary/10', value: formatNumber(stats.value.total), labelKey: 'inquiries.stats.total' },
  { key: 'new', icon: 'pi-plus-circle', color: 'text-blue-500', bgColor: 'bg-blue-500/10', value: formatNumber(stats.value.new), labelKey: 'inquiries.stats.new' },
  { key: 'inProgress', icon: 'pi-spinner', color: 'text-yellow-600', bgColor: 'bg-yellow-500/10', value: formatNumber(stats.value.inProgress), labelKey: 'inquiries.stats.inProgress' },
  { key: 'pendingApproval', icon: 'pi-clock', color: 'text-orange-500', bgColor: 'bg-orange-500/10', value: formatNumber(stats.value.pendingApproval), labelKey: 'inquiries.stats.pendingApproval' },
  { key: 'approved', icon: 'pi-check-circle', color: 'text-green-600', bgColor: 'bg-green-500/10', value: formatNumber(stats.value.approved), labelKey: 'inquiries.stats.approved' },
  { key: 'overdue', icon: 'pi-exclamation-triangle', color: 'text-red-600', bgColor: 'bg-red-500/10', value: formatNumber(stats.value.overdue), labelKey: 'inquiries.stats.overdue' },
])

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getStatusConfig(status: string) {
  const config: Record<string, { bgClass: string; textClass: string; labelKey: string }> = {
    New: { bgClass: 'bg-blue-500/10', textClass: 'text-blue-600', labelKey: 'inquiries.status.New' },
    InProgress: { bgClass: 'bg-yellow-500/10', textClass: 'text-yellow-600', labelKey: 'inquiries.status.InProgress' },
    PendingApproval: { bgClass: 'bg-orange-500/10', textClass: 'text-orange-600', labelKey: 'inquiries.status.PendingApproval' },
    Approved: { bgClass: 'bg-green-500/10', textClass: 'text-green-600', labelKey: 'inquiries.status.Approved' },
    Rejected: { bgClass: 'bg-red-500/10', textClass: 'text-red-600', labelKey: 'inquiries.status.Rejected' },
    Closed: { bgClass: 'bg-gray-200', textClass: 'text-gray-600', labelKey: 'inquiries.status.Closed' },
  }
  return config[status] || config.New
}

function getCategoryConfig(category: string) {
  const config: Record<string, { bgClass: string; textClass: string; labelKey: string }> = {
    General: { bgClass: 'bg-gray-100', textClass: 'text-gray-600', labelKey: 'inquiries.category.General' },
    Technical: { bgClass: 'bg-indigo-500/10', textClass: 'text-indigo-600', labelKey: 'inquiries.category.Technical' },
    Financial: { bgClass: 'bg-emerald-500/10', textClass: 'text-emerald-600', labelKey: 'inquiries.category.Financial' },
    Administrative: { bgClass: 'bg-sky-500/10', textClass: 'text-sky-600', labelKey: 'inquiries.category.Administrative' },
    Legal: { bgClass: 'bg-purple-500/10', textClass: 'text-purple-600', labelKey: 'inquiries.category.Legal' },
  }
  return config[category] || config.General
}

function getPriorityConfig(priority: string) {
  const config: Record<string, { bgClass: string; textClass: string; labelKey: string }> = {
    Critical: { bgClass: 'bg-red-500/10', textClass: 'text-red-600', labelKey: 'inquiries.priority.Critical' },
    High: { bgClass: 'bg-orange-500/10', textClass: 'text-orange-600', labelKey: 'inquiries.priority.High' },
    Medium: { bgClass: 'bg-yellow-500/10', textClass: 'text-yellow-600', labelKey: 'inquiries.priority.Medium' },
    Low: { bgClass: 'bg-gray-100', textClass: 'text-gray-600', labelKey: 'inquiries.priority.Low' },
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
    error.value = err instanceof Error ? err.message : t('inquiries.errorLoading')
  } finally {
    isLoading.value = false
  }
}

async function loadCompetitions(): Promise<void> {
  try {
    const result = await fetchRfpList({ page: 1, pageSize: 100 })
    if (result.success && result.data) {
      competitions.value = result.data.items.map((c) => ({
        id: c.id,
        projectName: c.projectName || '',
        referenceNumber: c.referenceNumber || '',
      }))
    }
  } catch {
    console.warn('[InquiriesView] Failed to load competitions')
  }
}

async function loadCommittees(): Promise<void> {
  try {
    const result = await fetchCommittees({ pageNumber: 1, pageSize: 50 })
    if (result?.items) {
      committees.value = result.items.map((c) => ({
        id: c.id,
        nameAr: c.nameAr || '',
        nameEn: c.nameEn || '',
        members: [],
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
  responseMode.value = 'manual'
  isEditingAiResponse.value = false
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
    error.value = t('inquiries.errorLoading')
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
    error.value = t('inquiries.errorLoading')
  } finally {
    isImporting.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  User Search for Assignment                                         */
/* ------------------------------------------------------------------ */
function handleUserSearch(query: string): void {
  userSearchQuery.value = query
  if (userSearchTimeout) clearTimeout(userSearchTimeout)
  if (!query || query.trim().length < 2) {
    userSearchResults.value = []
    showUserDropdown.value = false
    return
  }
  userSearchTimeout = setTimeout(async () => {
    isSearchingUsers.value = true
    try {
      const result = await httpGet<{ items: Array<{ id: string; firstName: string; lastName: string; email: string; roles: Array<{ nameAr: string; nameEn: string }> }> }>(
        `/v1/users?page=1&pageSize=10&search=${encodeURIComponent(query.trim())}`
      )
      userSearchResults.value = (result.items || []).map(u => ({
        id: u.id,
        fullName: `${u.firstName} ${u.lastName}`,
        email: u.email,
        roleName: u.roles?.[0]?.nameAr || '',
      }))
      showUserDropdown.value = userSearchResults.value.length > 0
    } catch (err) {
      console.warn('[InquiriesView] User search failed:', err)
      userSearchResults.value = []
      showUserDropdown.value = false
    } finally {
      isSearchingUsers.value = false
    }
  }, 300)
}

function selectUser(user: { id: string; fullName: string; email: string }): void {
  assignForm.value.userId = user.id
  assignForm.value.userName = user.fullName
  userSearchQuery.value = `${user.fullName} (${user.email})`
  showUserDropdown.value = false
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
    userSearchQuery.value = ''
    userSearchResults.value = []
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
  } catch (err: any) {
    console.error('[InquiriesView] AI generation failed:', err)
    const apiMessage = err?.response?.data?.message || err?.response?.data?.detail || ''
    error.value = apiMessage || t('inquiries.detail.ai.error')
  } finally {
    isGeneratingAi.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Submit Answer (for approval)                                       */
/* ------------------------------------------------------------------ */
async function handleSubmitAnswer(): Promise<void> {
  if (!selectedInquiry.value || !answerText.value.trim()) return
  isSubmittingAnswer.value = true
  try {
    await submitAnswer(selectedInquiry.value.id, {
      answerText: answerText.value,
      isAiAssisted: aiResult.value !== null || isEditingAiResponse.value,
    })
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    answerText.value = ''
    aiResult.value = null
    isEditingAiResponse.value = false
    responseMode.value = 'manual'
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to submit answer:', err)
  } finally {
    isSubmittingAnswer.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Direct Approve Answer (submit + approve in one step)               */
/* ------------------------------------------------------------------ */
async function handleDirectApprove(): Promise<void> {
  if (!selectedInquiry.value || !answerText.value.trim()) return
  isDirectApproving.value = true
  try {
    // Step 1: Submit the answer
    await submitAnswer(selectedInquiry.value.id, {
      answerText: answerText.value,
      isAiAssisted: aiResult.value !== null || isEditingAiResponse.value,
    })
    // Step 2: Approve immediately
    await approveInquiry(selectedInquiry.value.id)
    selectedInquiry.value = await fetchInquiryById(selectedInquiry.value.id)
    answerText.value = ''
    aiResult.value = null
    isEditingAiResponse.value = false
    responseMode.value = 'manual'
    await loadInquiries()
  } catch (err) {
    console.error('[InquiriesView] Failed to direct approve:', err)
  } finally {
    isDirectApproving.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Edit AI Response in Rich Editor                                    */
/* ------------------------------------------------------------------ */
function editAiResponseInEditor(): void {
  if (aiResult.value) {
    answerText.value = aiResult.value.answerText
    isEditingAiResponse.value = true
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
        <h1 class="text-2xl font-bold text-secondary">{{ t('inquiries.title') }}</h1>
        <p class="mt-1 text-sm text-tertiary">{{ t('inquiries.subtitle') }}</p>
      </div>
      <div class="flex items-center gap-2">
        <button
          v-if="canCreateInquiry"
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary/90"
          @click="activeTab = 'create'"
        >
          <i class="pi pi-plus" />
          {{ t('inquiries.addInquiry') }}
        </button>
        <button
          v-if="canCreateInquiry"
          class="flex items-center gap-2 rounded-lg border border-primary/30 bg-primary/5 px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary/10"
          @click="activeTab = 'import'"
        >
          <i class="pi pi-upload" />
          {{ t('inquiries.importFromEtimad') }}
        </button>
        <button
          class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
          :disabled="isLoading"
          @click="loadInquiries"
        >
          <i class="pi" :class="isLoading ? 'pi-spinner pi-spin' : 'pi-refresh'" />
          {{ t('inquiries.refresh') }}
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
        {{ t('inquiries.close') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-6">
      <div v-for="card in statsCards" :key="card.key" class="card flex items-center gap-3 !p-4">
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg" :class="card.bgColor">
          <i class="pi text-base" :class="[card.icon, card.color]" />
        </div>
        <div>
          <p class="text-[11px] text-tertiary">{{ t(card.labelKey) }}</p>
          <p class="text-lg font-bold text-secondary">{{ card.value }}</p>
        </div>
      </div>
    </div>

    <!-- ═══════════════════ CREATE TAB ═══════════════════ -->
    <div v-if="activeTab === 'create'" class="card">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-lg font-bold text-secondary">
          <i class="pi pi-plus-circle me-2 text-primary" />
          {{ t('inquiries.create.title') }}
        </h2>
        <button class="rounded-lg px-3 py-1.5 text-sm text-tertiary hover:bg-surface-muted" @click="activeTab = 'list'">
          <i class="pi pi-times" />
        </button>
      </div>
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.competition') }} *</label>
          <select v-model="createForm.competitionId" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('inquiries.create.competitionPlaceholder') }}</option>
            <option v-for="comp in competitions" :key="comp.id" :value="comp.id">{{ comp.projectName }} ({{ comp.referenceNumber }})</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.supplierName') }}</label>
          <input v-model="createForm.supplierName" type="text" :placeholder="t('inquiries.create.supplierNamePlaceholder')" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.category') }}</label>
          <select v-model="createForm.category" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="General">{{ t('inquiries.category.General') }}</option>
            <option value="Technical">{{ t('inquiries.category.Technical') }}</option>
            <option value="Financial">{{ t('inquiries.category.Financial') }}</option>
            <option value="Administrative">{{ t('inquiries.category.Administrative') }}</option>
            <option value="Legal">{{ t('inquiries.category.Legal') }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.priority') }}</label>
          <select v-model="createForm.priority" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="Low">{{ t('inquiries.priority.Low') }}</option>
            <option value="Medium">{{ t('inquiries.priority.Medium') }}</option>
            <option value="High">{{ t('inquiries.priority.High') }}</option>
            <option value="Critical">{{ t('inquiries.priority.Critical') }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.etimadRef') }}</label>
          <input v-model="createForm.etimadReferenceNumber" type="text" :placeholder="t('inquiries.create.etimadRefPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="md:col-span-2">
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.questionText') }} *</label>
          <textarea v-model="createForm.questionText" rows="4" :placeholder="t('inquiries.create.questionTextPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div class="md:col-span-2">
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.create.internalNotes') }}</label>
          <textarea v-model="createForm.internalNotes" rows="2" :placeholder="t('inquiries.create.internalNotesPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
      <div class="mt-4 flex justify-end gap-2">
        <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="activeTab = 'list'">{{ t('inquiries.create.cancel') }}</button>
        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          :disabled="!createForm.competitionId || !createForm.questionText.trim() || isCreating"
          @click="handleCreate"
        >
          <i class="pi" :class="isCreating ? 'pi-spinner pi-spin' : 'pi-check'" />
          {{ t('inquiries.create.save') }}
        </button>
      </div>
    </div>

    <!-- ═══════════════════ BULK IMPORT TAB ═══════════════════ -->
    <div v-if="activeTab === 'import'" class="card">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-lg font-bold text-secondary">
          <i class="pi pi-upload me-2 text-primary" />
          {{ t('inquiries.bulkImport.title') }}
        </h2>
        <button class="rounded-lg px-3 py-1.5 text-sm text-tertiary hover:bg-surface-muted" @click="activeTab = 'list'">
          <i class="pi pi-times" />
        </button>
      </div>
      <p class="mb-4 text-sm text-tertiary">
        {{ t('inquiries.bulkImport.description') }}
      </p>
      <div class="grid grid-cols-1 gap-4">
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.bulkImport.competition') }} *</label>
          <select v-model="bulkImportForm.competitionId" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="">{{ t('inquiries.bulkImport.competitionPlaceholder') }}</option>
            <option v-for="comp in competitions" :key="comp.id" :value="comp.id">{{ comp.projectName }} ({{ comp.referenceNumber }})</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.bulkImport.textLabel') }} *</label>
          <textarea v-model="bulkImportForm.rawText" rows="8" :placeholder="t('inquiries.bulkImport.textPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary font-mono focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>
      <!-- Preview -->
      <div v-if="parsedInquiries.length > 0" class="mt-4">
        <p class="mb-2 text-xs font-medium text-tertiary">{{ t('inquiries.bulkImport.previewCount', { count: parsedInquiries.length }) }}</p>
        <div class="max-h-48 overflow-y-auto rounded-lg border border-surface-dim">
          <table class="w-full text-sm">
            <thead class="bg-surface-muted">
              <tr>
                <th class="px-3 py-2 text-start text-xs font-medium text-tertiary">#</th>
                <th class="px-3 py-2 text-start text-xs font-medium text-tertiary">{{ t('inquiries.bulkImport.questionText') }}</th>
                <th class="px-3 py-2 text-start text-xs font-medium text-tertiary">{{ t('inquiries.bulkImport.supplier') }}</th>
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
        <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="activeTab = 'list'">{{ t('inquiries.bulkImport.cancel') }}</button>
        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          :disabled="!bulkImportForm.competitionId || parsedInquiries.length === 0 || isImporting"
          @click="handleBulkImport"
        >
          <i class="pi" :class="isImporting ? 'pi-spinner pi-spin' : 'pi-upload'" />
          {{ t('inquiries.bulkImport.import', { count: parsedInquiries.length }) }}
        </button>
      </div>
    </div>

    <!-- ═══════════════════ LIST TAB ═══════════════════ -->
    <template v-if="activeTab === 'list'">
      <!-- Filters -->
      <div class="card">
        <div class="flex flex-wrap items-end gap-4">
          <div class="flex-1 min-w-[200px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.filters.search') }}</label>
            <div class="relative">
              <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary" />
              <input v-model="filters.search" type="text" :placeholder="t('inquiries.filters.searchPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-white py-2 pe-3 ps-9 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" @keyup.enter="applyFilters" />
            </div>
          </div>
          <div class="min-w-[140px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.filters.status') }}</label>
            <select v-model="filters.status" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('inquiries.filters.allStatuses') }}</option>
              <option value="New">{{ t('inquiries.status.New') }}</option>
              <option value="InProgress">{{ t('inquiries.status.InProgress') }}</option>
              <option value="PendingApproval">{{ t('inquiries.status.PendingApproval') }}</option>
              <option value="Approved">{{ t('inquiries.status.Approved') }}</option>
              <option value="Rejected">{{ t('inquiries.status.Rejected') }}</option>
              <option value="Closed">{{ t('inquiries.status.Closed') }}</option>
            </select>
          </div>
          <div class="min-w-[130px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.filters.category') }}</label>
            <select v-model="filters.category" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('inquiries.filters.allCategories') }}</option>
              <option value="General">{{ t('inquiries.category.General') }}</option>
              <option value="Technical">{{ t('inquiries.category.Technical') }}</option>
              <option value="Financial">{{ t('inquiries.category.Financial') }}</option>
              <option value="Administrative">{{ t('inquiries.category.Administrative') }}</option>
              <option value="Legal">{{ t('inquiries.category.Legal') }}</option>
            </select>
          </div>
          <div class="min-w-[130px]">
            <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.filters.priority') }}</label>
            <select v-model="filters.priority" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
              <option value="">{{ t('inquiries.filters.allPriorities') }}</option>
              <option value="Critical">{{ t('inquiries.priority.Critical') }}</option>
              <option value="High">{{ t('inquiries.priority.High') }}</option>
              <option value="Medium">{{ t('inquiries.priority.Medium') }}</option>
              <option value="Low">{{ t('inquiries.priority.Low') }}</option>
            </select>
          </div>
          <div class="flex gap-2">
            <button class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90" @click="applyFilters">
              <i class="pi pi-search me-1" /> {{ t('inquiries.filters.apply') }}
            </button>
            <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="resetFilters">
              <i class="pi pi-filter-slash me-1" /> {{ t('inquiries.filters.reset') }}
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
          <p class="mt-4 text-base font-medium text-secondary">{{ t('inquiries.empty.title') }}</p>
          <p class="mt-1 text-sm text-tertiary">{{ t('inquiries.empty.subtitle') }}</p>
          <button class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90" @click="activeTab = 'create'">
            <i class="pi pi-plus me-1" /> {{ t('inquiries.empty.addNew') }}
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
                    <i class="pi pi-external-link me-1" />{{ t('inquiries.detail.etimadRef') }}: {{ inquiry.etimadReferenceNumber }}
                  </span>
                </div>
              </div>
              <div class="flex flex-col items-end gap-1.5">
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getStatusConfig(inquiry.status).bgClass, getStatusConfig(inquiry.status).textClass]">
                  {{ t(getStatusConfig(inquiry.status).labelKey) }}
                </span>
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getCategoryConfig(inquiry.category).bgClass, getCategoryConfig(inquiry.category).textClass]">
                  {{ t(getCategoryConfig(inquiry.category).labelKey) }}
                </span>
                <span class="rounded-full px-2.5 py-0.5 text-xs font-medium" :class="[getPriorityConfig(inquiry.priority).bgClass, getPriorityConfig(inquiry.priority).textClass]">
                  {{ t(getPriorityConfig(inquiry.priority).labelKey) }}
                </span>
                <span v-if="inquiry.isOverdue" class="flex items-center gap-1 text-xs font-medium text-red-600">
                  <i class="pi pi-exclamation-circle text-xs" /> {{ t('inquiries.stats.overdue') }}
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
            {{ t('inquiries.pagination.showing', { from: (currentPage - 1) * pageSize + 1, to: Math.min(currentPage * pageSize, totalCount), total: totalCount }) }}
          </p>
          <div class="flex items-center gap-1">
            <button class="rounded-lg px-3 py-1.5 text-sm text-secondary hover:bg-surface-muted disabled:opacity-50" :disabled="currentPage <= 1" @click="goToPage(currentPage - 1)">
              <i class="pi" :class="isRtl ? 'pi-chevron-right' : 'pi-chevron-left'" />
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
              <i class="pi" :class="isRtl ? 'pi-chevron-left' : 'pi-chevron-right'" />
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
                  {{ t(getStatusConfig(selectedInquiry.status).labelKey) }}
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
                { key: 'info', labelKey: 'inquiries.detail.tabs.info', icon: 'pi-info-circle' },
                { key: 'responses', labelKey: 'inquiries.detail.tabs.responses', icon: 'pi-sparkles' },
                { key: 'assign', labelKey: 'inquiries.detail.tabs.assign', icon: 'pi-users' },
              ]"
              :key="tab.key"
              class="flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors border-b-2"
              :class="detailTab === tab.key ? 'border-primary text-primary' : 'border-transparent text-tertiary hover:text-secondary'"
              @click="detailTab = tab.key as 'info' | 'responses' | 'assign'"
            >
              <i class="pi text-sm" :class="tab.icon" />
              {{ t(tab.labelKey) }}
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
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.supplier') }}</p>
                <p class="text-sm font-medium text-secondary">{{ selectedInquiry.supplierName || '-' }}</p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.category') }}</p>
                <span class="rounded-full px-2 py-0.5 text-xs font-medium" :class="[getCategoryConfig(selectedInquiry.category).bgClass, getCategoryConfig(selectedInquiry.category).textClass]">
                  {{ t(getCategoryConfig(selectedInquiry.category).labelKey) }}
                </span>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.priority') }}</p>
                <span class="rounded-full px-2 py-0.5 text-xs font-medium" :class="[getPriorityConfig(selectedInquiry.priority).bgClass, getPriorityConfig(selectedInquiry.priority).textClass]">
                  {{ t(getPriorityConfig(selectedInquiry.priority).labelKey) }}
                </span>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.createdAt') }}</p>
                <p class="text-sm font-medium text-secondary">{{ formatDateTime(selectedInquiry.createdAt) }}</p>
              </div>
              <div v-if="selectedInquiry.etimadReferenceNumber">
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.etimadRef') }}</p>
                <p class="text-sm font-medium text-primary">{{ selectedInquiry.etimadReferenceNumber }}</p>
              </div>
              <div v-if="selectedInquiry.assignedToUserName">
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.assignment.assignedTo') }}</p>
                <p class="text-sm font-medium text-secondary">{{ selectedInquiry.assignedToUserName }}</p>
              </div>
              <div v-if="selectedInquiry.slaDeadline">
                <p class="text-xs text-tertiary">SLA</p>
                <p class="text-sm font-medium" :class="selectedInquiry.isOverdue ? 'text-red-600' : 'text-secondary'">
                  {{ formatDateTime(selectedInquiry.slaDeadline) }}
                  <span v-if="selectedInquiry.isOverdue" class="text-xs">({{ t('inquiries.stats.overdue') }})</span>
                </p>
              </div>
              <div v-if="selectedInquiry.isExportedToEtimad">
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.etimadRef') }}</p>
                <p class="text-sm font-medium text-green-600">
                  <i class="pi pi-check-circle me-1" />{{ formatDateTime(selectedInquiry.exportedToEtimadAt) }}
                </p>
              </div>
            </div>

            <!-- Question -->
            <div>
              <p class="mb-1.5 text-xs font-semibold text-tertiary">{{ t('inquiries.detail.question') }}</p>
              <div class="rounded-lg bg-blue-50 border border-blue-100 p-4 text-sm leading-relaxed text-secondary whitespace-pre-wrap">
                {{ selectedInquiry.questionText }}
              </div>
            </div>

            <!-- Approved Answer -->
            <div v-if="selectedInquiry.approvedAnswer">
              <p class="mb-1.5 text-xs font-semibold text-tertiary">{{ t('inquiries.detail.answer.title') }}</p>
              <div class="rounded-lg border border-green-200 bg-green-50 p-4 text-sm leading-relaxed text-secondary whitespace-pre-wrap">
                {{ selectedInquiry.approvedAnswer }}
              </div>
              <p class="mt-1 text-xs text-tertiary">
                {{ selectedInquiry.approvedBy }} — {{ formatDateTime(selectedInquiry.approvedAt) }}
              </p>
            </div>

            <!-- Rejection Reason -->
            <div v-if="selectedInquiry.rejectionReason">
              <p class="mb-1.5 text-xs font-semibold text-tertiary">{{ t('inquiries.detail.reject.reason') }}</p>
              <div class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm leading-relaxed text-red-700">
                {{ selectedInquiry.rejectionReason }}
              </div>
            </div>

            <!-- Internal Notes -->
            <div v-if="selectedInquiry.internalNotes">
              <p class="mb-1.5 text-xs font-semibold text-tertiary">{{ t('inquiries.detail.internalNotes') }}</p>
              <div class="rounded-lg bg-yellow-50 border border-yellow-100 p-4 text-sm leading-relaxed text-secondary">
                {{ selectedInquiry.internalNotes }}
              </div>
            </div>
          </div>

          <!-- Tab: Responses & AI -->
          <div v-else-if="detailTab === 'responses'" class="px-6 py-5 space-y-5">

            <!-- ═══ Response Mode Selector (only when inquiry is actionable) ═══ -->
            <div v-if="selectedInquiry.status === 'New' || selectedInquiry.status === 'InProgress' || selectedInquiry.status === 'Rejected'">

              <!-- Mode Toggle -->
              <div class="mb-5 flex rounded-xl border border-surface-dim bg-surface-muted/50 p-1">
                <button
                  type="button"
                  class="flex flex-1 items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-medium transition-all"
                  :class="responseMode === 'manual' ? 'bg-white text-primary shadow-sm' : 'text-tertiary hover:text-secondary'"
                  @click="responseMode = 'manual'"
                >
                  <i class="pi pi-pencil" />
                  {{ locale === 'ar' ? 'كتابة الرد على الاستفسار' : 'Write Reply Manually' }}
                </button>
                <button
                  type="button"
                  class="flex flex-1 items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-medium transition-all"
                  :class="responseMode === 'ai' ? 'bg-white text-purple-600 shadow-sm' : 'text-tertiary hover:text-secondary'"
                  @click="responseMode = 'ai'"
                >
                  <i class="pi pi-sparkles" />
                  {{ locale === 'ar' ? 'كتابة الرد من خلال الذكاء الاصطناعي' : 'Generate Reply with AI' }}
                </button>
              </div>

              <!-- ═══ MODE 1: Manual Reply ═══ -->
              <div v-if="responseMode === 'manual'" class="space-y-4">
                <div class="rounded-xl border border-primary/20 bg-gradient-to-l from-primary/5 to-white p-5">
                  <div class="flex items-center gap-2 mb-3">
                    <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10">
                      <i class="pi pi-pencil text-primary" />
                    </div>
                    <div>
                      <h4 class="text-sm font-bold text-secondary">{{ locale === 'ar' ? 'كتابة الرد على الاستفسار' : 'Write Reply' }}</h4>
                      <p class="text-xs text-tertiary">{{ locale === 'ar' ? 'اكتب نص الرد باستخدام المحرر المتقدم ثم اختر إرسال للاعتماد أو الاعتماد المباشر' : 'Write your reply using the advanced editor then choose to submit for approval or approve directly' }}</p>
                    </div>
                  </div>
                  <RichTextEditor
                    :model-value="answerText"
                    :placeholder="locale === 'ar' ? 'اكتب نص الرد هنا...' : 'Write your reply here...'"
                    dir="auto"
                    min-height="180px"
                    max-height="400px"
                    @update:model-value="(val: string) => answerText = val"
                  />
                  <div class="mt-4 flex flex-wrap items-center justify-end gap-3">
                    <button
                      class="flex items-center gap-2 rounded-lg border border-primary/30 bg-white px-5 py-2.5 text-sm font-medium text-primary hover:bg-primary/5 disabled:opacity-50"
                      :disabled="!answerText.trim() || answerText === '<p></p>' || isSubmittingAnswer || isDirectApproving"
                      @click="handleSubmitAnswer"
                    >
                      <i class="pi" :class="isSubmittingAnswer ? 'pi-spinner pi-spin' : 'pi-send'" />
                      {{ locale === 'ar' ? 'إرسال للاعتماد' : 'Submit for Approval' }}
                    </button>
                    <button
                      v-if="canDirectApprove"
                      class="flex items-center gap-2 rounded-lg bg-green-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                      :disabled="!answerText.trim() || answerText === '<p></p>' || isSubmittingAnswer || isDirectApproving"
                      @click="handleDirectApprove"
                    >
                      <i class="pi" :class="isDirectApproving ? 'pi-spinner pi-spin' : 'pi-check-circle'" />
                      {{ locale === 'ar' ? 'اعتماد مباشر' : 'Approve Directly' }}
                    </button>
                  </div>
                </div>
              </div>

              <!-- ═══ MODE 2: AI-Assisted Reply ═══ -->
              <div v-if="responseMode === 'ai'" class="space-y-4">

                <!-- Step 1: Generate AI Response -->
                <div v-if="!isEditingAiResponse" class="rounded-xl border border-purple-200 bg-gradient-to-l from-purple-50 to-white p-5">
                  <div class="flex items-center gap-2 mb-3">
                    <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/10">
                      <i class="pi pi-sparkles text-purple-600" />
                    </div>
                    <div>
                      <h4 class="text-sm font-bold text-secondary">{{ t('inquiries.detail.ai.title') }}</h4>
                      <p class="text-xs text-tertiary">{{ locale === 'ar' ? 'سيتم توليد رد باستخدام الذكاء الاصطناعي بناءً على قاعدة المعرفة' : 'AI will generate a reply based on the knowledge base' }}</p>
                    </div>
                  </div>
                  <div class="mb-3">
                    <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.detail.ai.contextLabel') }}</label>
                    <textarea v-model="aiContext" rows="2" :placeholder="t('inquiries.detail.ai.contextPlaceholder')" class="w-full rounded-lg border border-purple-200 bg-white p-3 text-sm text-secondary focus:border-purple-400 focus:outline-none focus:ring-1 focus:ring-purple-400" />
                  </div>
                  <button
                    class="flex items-center gap-2 rounded-lg bg-purple-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-purple-700 disabled:opacity-50"
                    :disabled="isGeneratingAi"
                    @click="handleGenerateAi"
                  >
                    <i class="pi" :class="isGeneratingAi ? 'pi-spinner pi-spin' : 'pi-sparkles'" />
                    {{ isGeneratingAi ? t('inquiries.detail.ai.generating') : t('inquiries.detail.ai.generate') }}
                  </button>

                  <!-- AI Result Preview -->
                  <div v-if="aiResult" class="mt-4 rounded-lg border border-purple-200 bg-white p-4">
                    <div class="flex items-center justify-between mb-2">
                      <span class="text-xs font-medium text-purple-600">
                        <i class="pi pi-sparkles me-1" />{{ t('inquiries.detail.ai.result') }}
                      </span>
                      <div class="flex items-center gap-3">
                        <span class="text-xs text-tertiary">{{ t('inquiries.detail.ai.confidence') }}: {{ aiResult.confidenceScore }}%</span>
                        <span class="text-xs text-tertiary">{{ t('inquiries.detail.ai.model') }}: {{ aiResult.modelUsed }}</span>
                      </div>
                    </div>
                    <div class="rounded-lg bg-purple-50 p-3 text-sm leading-relaxed text-secondary whitespace-pre-wrap">
                      {{ aiResult.answerText }}
                    </div>
                    <div v-if="aiResult.sources" class="mt-2 text-xs text-tertiary">
                      <i class="pi pi-book me-1" />{{ t('inquiries.detail.ai.sources') }}: {{ aiResult.sources }}
                    </div>
                    <div class="mt-4 flex flex-wrap items-center gap-3">
                      <button
                        class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white hover:bg-primary/90"
                        @click="editAiResponseInEditor"
                      >
                        <i class="pi pi-file-edit" />
                        {{ locale === 'ar' ? 'تعديل الرد في المحرر المتقدم' : 'Edit Reply in Advanced Editor' }}
                      </button>
                      <button
                        class="flex items-center gap-2 rounded-lg border border-purple-300 bg-white px-5 py-2.5 text-sm font-medium text-purple-600 hover:bg-purple-50"
                        @click="handleGenerateAi"
                      >
                        <i class="pi pi-refresh" />
                        {{ locale === 'ar' ? 'إعادة التوليد' : 'Regenerate' }}
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Step 2: Edit AI Response in Rich Editor -->
                <div v-if="isEditingAiResponse" class="rounded-xl border border-purple-200 bg-gradient-to-l from-purple-50 to-white p-5">
                  <div class="flex items-center gap-2 mb-3">
                    <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/10">
                      <i class="pi pi-file-edit text-purple-600" />
                    </div>
                    <div>
                      <h4 class="text-sm font-bold text-secondary">{{ locale === 'ar' ? 'تعديل رد الذكاء الاصطناعي' : 'Edit AI Response' }}</h4>
                      <p class="text-xs text-tertiary">{{ locale === 'ar' ? 'قم بمراجعة وتعديل النص المولّد ثم اختر الإجراء المناسب' : 'Review and edit the generated text then choose the appropriate action' }}</p>
                    </div>
                  </div>
                  <div v-if="aiResult" class="mb-3 flex items-center gap-3 rounded-lg bg-purple-100/50 px-3 py-2">
                    <span class="text-xs font-medium text-purple-600">
                      <i class="pi pi-sparkles me-1" />{{ t('inquiries.detail.ai.confidence') }}: {{ aiResult.confidenceScore }}%
                    </span>
                    <span class="text-xs text-tertiary">{{ t('inquiries.detail.ai.model') }}: {{ aiResult.modelUsed }}</span>
                  </div>
                  <RichTextEditor
                    :model-value="answerText"
                    :placeholder="locale === 'ar' ? 'عدّل نص الرد هنا...' : 'Edit reply text here...'"
                    dir="auto"
                    min-height="200px"
                    max-height="400px"
                    @update:model-value="(val: string) => answerText = val"
                  />
                  <div class="mt-4 flex flex-wrap items-center justify-between gap-3">
                    <button
                      class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-tertiary hover:bg-surface-muted"
                      @click="isEditingAiResponse = false; answerText = ''"
                    >
                      <i class="pi pi-arrow-right" />
                      {{ locale === 'ar' ? 'العودة للتوليد' : 'Back to Generation' }}
                    </button>
                    <div class="flex flex-wrap items-center gap-3">
                      <button
                        class="flex items-center gap-2 rounded-lg border border-primary/30 bg-white px-5 py-2.5 text-sm font-medium text-primary hover:bg-primary/5 disabled:opacity-50"
                        :disabled="!answerText.trim() || answerText === '<p></p>' || isSubmittingAnswer || isDirectApproving"
                        @click="handleSubmitAnswer"
                      >
                        <i class="pi" :class="isSubmittingAnswer ? 'pi-spinner pi-spin' : 'pi-send'" />
                        {{ locale === 'ar' ? 'إرسال للاعتماد' : 'Submit for Approval' }}
                      </button>
                      <button
                        v-if="canDirectApprove"
                        class="flex items-center gap-2 rounded-lg bg-green-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                        :disabled="!answerText.trim() || answerText === '<p></p>' || isSubmittingAnswer || isDirectApproving"
                        @click="handleDirectApprove"
                      >
                        <i class="pi" :class="isDirectApproving ? 'pi-spinner pi-spin' : 'pi-check-circle'" />
                        {{ locale === 'ar' ? 'اعتماد مباشر' : 'Approve Directly' }}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- ═══ Previous Responses ═══ -->
            <div v-if="selectedInquiry.responses && selectedInquiry.responses.length > 0">
              <h4 class="mb-3 text-sm font-bold text-secondary">{{ t('inquiries.detail.responses.title') }} ({{ selectedInquiry.responses.length }})</h4>
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
                        <i class="pi pi-sparkles me-1" />{{ t('inquiries.detail.responses.aiGenerated') }}
                      </span>
                      <span v-else class="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-600">
                        <i class="pi pi-user me-1" />{{ t('inquiries.detail.responses.manual') }}
                      </span>
                      <span v-if="response.isSelected" class="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-600">
                        <i class="pi pi-check me-1" />{{ t('inquiries.detail.answer.title') }}
                      </span>
                      <span v-if="response.aiConfidenceScore" class="text-xs text-tertiary">
                        {{ t('inquiries.detail.responses.confidence') }}: {{ response.aiConfidenceScore }}%
                      </span>
                    </div>
                    <span class="text-xs text-tertiary">{{ formatDateTime(response.createdAt) }}</span>
                  </div>
                  <div class="text-sm leading-relaxed text-secondary" v-html="response.answerText" />
                  <div class="mt-2 flex justify-end">
                    <button
                      v-if="selectedInquiry.status !== 'Approved' && selectedInquiry.status !== 'Closed'"
                      class="rounded-lg px-3 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                      @click="selectResponse(response)"
                    >
                      <i class="pi pi-copy me-1" />{{ t('inquiries.detail.responses.useThis') }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- ═══ Approval Actions (when pending approval) ═══ -->
            <div v-if="selectedInquiry.status === 'PendingApproval'" class="rounded-xl border border-orange-200 bg-orange-50 p-5">
              <h4 class="mb-3 text-sm font-bold text-secondary">
                <i class="pi pi-check-square me-2 text-orange-600" />
                {{ locale === 'ar' ? 'اعتماد الرد' : 'Approve Response' }}
              </h4>
              <p class="mb-4 text-xs text-tertiary">{{ locale === 'ar' ? 'تم إرسال الرد للاعتماد. يمكنك الموافقة أو الرفض مع ذكر السبب.' : 'The response has been submitted for approval. You can approve or reject with a reason.' }}</p>
              <div class="flex gap-3">
                <button
                  class="flex items-center gap-2 rounded-lg bg-green-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                  :disabled="isApproving"
                  @click="handleApprove"
                >
                  <i class="pi" :class="isApproving ? 'pi-spinner pi-spin' : 'pi-check'" />
                  {{ t('inquiries.detail.answer.approve') }}
                </button>
                <button
                  class="flex items-center gap-2 rounded-lg bg-red-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-red-700"
                  @click="showRejectDialog = true"
                >
                  <i class="pi pi-times" />
                  {{ t('inquiries.detail.answer.reject') }}
                </button>
              </div>
              <!-- Reject Dialog -->
              <div v-if="showRejectDialog" class="mt-4 rounded-lg border border-red-200 bg-white p-4">
                <label class="mb-1 block text-xs font-medium text-red-600">{{ t('inquiries.detail.reject.reason') }} *</label>
                <textarea v-model="rejectReason" rows="3" :placeholder="t('inquiries.detail.reject.reasonPlaceholder')" class="w-full rounded-lg border border-red-200 bg-white p-3 text-sm text-secondary focus:border-red-400 focus:outline-none focus:ring-1 focus:ring-red-400" />
                <div class="mt-2 flex justify-end gap-2">
                  <button class="rounded-lg px-3 py-1.5 text-sm text-tertiary hover:bg-surface-muted" @click="showRejectDialog = false">{{ t('inquiries.detail.reject.cancel') }}</button>
                  <button
                    class="flex items-center gap-2 rounded-lg bg-red-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
                    :disabled="!rejectReason.trim() || isRejecting"
                    @click="handleReject"
                  >
                    <i class="pi" :class="isRejecting ? 'pi-spinner pi-spin' : 'pi-times'" />
                    {{ t('inquiries.detail.reject.confirm') }}
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab: Assign -->
          <div v-else-if="detailTab === 'assign'" class="px-6 py-5 space-y-5">
            <!-- Current Assignment Status -->
            <div v-if="selectedInquiry.assignedToUserName || selectedInquiry.assignedToCommitteeId" class="rounded-lg border border-green-200 bg-green-50 p-4">
              <p class="text-sm font-medium text-green-700">
                <i class="pi pi-user me-2" />{{ t('inquiries.detail.assignment.assignedTo') }}: {{ selectedInquiry.assignedToUserName || (locale === 'ar' ? 'لجنة' : 'Committee') }}
              </p>
            </div>

            <div class="rounded-xl border border-surface-dim p-5">
              <h4 class="mb-4 text-sm font-bold text-secondary">
                <i class="pi pi-users me-2 text-primary" />
                {{ t('inquiries.detail.assignment.title') }}
              </h4>

              <!-- Assignment Mode Toggle -->
              <div class="mb-5 flex rounded-lg border border-surface-dim bg-surface-muted/50 p-1">
                <button
                  type="button"
                  class="flex-1 rounded-md px-4 py-2 text-sm font-medium transition-all"
                  :class="assignMode === 'committee' ? 'bg-white text-primary shadow-sm' : 'text-tertiary hover:text-secondary'"
                  @click="assignMode = 'committee'; assignForm.userId = ''; assignForm.userName = ''; userSearchQuery = ''"
                >
                  <i class="pi pi-sitemap me-2" />
                  {{ locale === 'ar' ? 'إسناد للجنة' : 'Assign to Committee' }}
                </button>
                <button
                  type="button"
                  class="flex-1 rounded-md px-4 py-2 text-sm font-medium transition-all"
                  :class="assignMode === 'user' ? 'bg-white text-primary shadow-sm' : 'text-tertiary hover:text-secondary'"
                  @click="assignMode = 'user'; assignForm.committeeId = ''"
                >
                  <i class="pi pi-user me-2" />
                  {{ locale === 'ar' ? 'إسناد لشخص' : 'Assign to Person' }}
                </button>
              </div>

              <!-- Assign to Committee -->
              <div v-if="assignMode === 'committee'" class="mb-4">
                <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('inquiries.detail.assignment.committee') }}</label>
                <select v-model="assignForm.committeeId" class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                  <option value="">{{ t('inquiries.detail.assignment.selectCommittee') }}</option>
                  <option v-for="committee in committees" :key="committee.id" :value="committee.id">
                    {{ isRtl ? committee.nameAr : committee.nameEn }}
                  </option>
                </select>
              </div>

              <!-- Assign to Specific User (Dynamic Search) -->
              <div v-if="assignMode === 'user'" class="mb-4">
                <label class="mb-1 block text-xs font-medium text-tertiary">
                  {{ locale === 'ar' ? 'ابحث عن الشخص بالاسم أو البريد الإلكتروني' : 'Search by name or email' }}
                </label>
                <div class="relative">
                  <div class="relative">
                    <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary" />
                    <input
                      :value="userSearchQuery"
                      type="text"
                      class="w-full rounded-lg border border-surface-dim bg-white pe-3 ps-9 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                      :placeholder="locale === 'ar' ? 'اكتب اسم الشخص أو بريده الإلكتروني...' : 'Type name or email...'"
                      @input="handleUserSearch(($event.target as HTMLInputElement).value)"
                      @focus="showUserDropdown = userSearchResults.length > 0"
                    />
                    <i v-if="isSearchingUsers" class="pi pi-spinner pi-spin absolute end-3 top-1/2 -translate-y-1/2 text-sm text-primary" />
                  </div>
                  <!-- Search Results Dropdown -->
                  <div
                    v-if="showUserDropdown && userSearchResults.length > 0"
                    class="absolute z-50 mt-1 w-full rounded-lg border border-surface-dim bg-white shadow-lg max-h-48 overflow-y-auto"
                  >
                    <button
                      v-for="user in userSearchResults"
                      :key="user.id"
                      type="button"
                      class="flex w-full items-center gap-3 px-4 py-2.5 text-start hover:bg-primary/5 transition-colors"
                      @click="selectUser(user)"
                    >
                      <div class="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                        {{ user.fullName.charAt(0) }}
                      </div>
                      <div class="flex-1 min-w-0">
                        <p class="text-sm font-medium text-secondary truncate">{{ user.fullName }}</p>
                        <p class="text-xs text-tertiary truncate">{{ user.email }}</p>
                      </div>
                      <span v-if="user.roleName" class="rounded-full bg-surface-muted px-2 py-0.5 text-xs text-tertiary">{{ user.roleName }}</span>
                    </button>
                  </div>
                </div>
                <!-- Selected User Display -->
                <div v-if="assignForm.userId" class="mt-2 flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 px-3 py-2">
                  <i class="pi pi-check-circle text-green-600 text-sm" />
                  <span class="text-sm text-green-700">{{ assignForm.userName }}</span>
                  <button type="button" class="ms-auto text-xs text-red-500 hover:text-red-700" @click="assignForm.userId = ''; assignForm.userName = ''; userSearchQuery = ''">
                    <i class="pi pi-times" />
                  </button>
                </div>
              </div>

              <div class="flex justify-end">
                <button
                  class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
                  :disabled="(assignMode === 'committee' ? !assignForm.committeeId : !assignForm.userId) || isAssigning"
                  @click="handleAssign"
                >
                  <i class="pi" :class="isAssigning ? 'pi-spinner pi-spin' : 'pi-user-plus'" />
                  {{ isAssigning ? t('inquiries.detail.assignment.assigning') : t('inquiries.detail.assignment.assign') }}
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
                {{ t('inquiries.detail.answer.close') }}
              </button>
            </div>
            <button class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted" @click="closeDetail">
              {{ t('inquiries.close') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
