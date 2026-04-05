<script setup lang="ts">
/**
 * CommitteesPermanentView — Permanent Committees Management Page.
 *
 * Redesigned with:
 * - Scope model: Comprehensive / SpecificPhasesAllCompetitions / SpecificPhasesSpecificCompetitions
 * - Dynamic user search for adding members (only registered platform users)
 * - Multi-competition linking
 * - Multi-phase selection via checkboxes (discrete phases, not range)
 * - Full RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import {
  fetchCommittees,
  fetchCommitteeById,
  createCommittee,
  updateCommittee,
  changeCommitteeStatus,
  addCommitteeMember,
  removeCommitteeMember,
  fetchCommitteeStatistics,
  fetchCommitteeAiAnalysis,
  searchEligibleUsers,
} from '@/services/committeeService'
import type {
  CommitteeListItem,
  CommitteeDetail,
  CreateCommitteeRequest,
  UpdateCommitteeRequest,
  ChangeCommitteeStatusRequest,
  AddCommitteeMemberRequest,
  CommitteeMember,
  CommitteeStatistics,
  CommitteeAiAnalysisResponse,
  EligibleUser,
} from '@/types/committee'
import {
  CommitteeType,
  CommitteeStatus,
  CommitteeMemberRole,
  CommitteeScopeType,
  CompetitionPhase,
} from '@/types/committee'
import { useFormatters } from '@/composables/useFormatters'
import { fetchRfpList } from '@/services/rfpService'
import type { RfpListItem } from '@/types/rfp'

const { t, locale } = useI18n()
const authStore = useAuthStore()

const canCreateCommittee = computed(() => authStore.hasPermission('committees.create'))
const canEditCommittee = computed(() => authStore.hasPermission('committees.edit'))
const { formatDate } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const committees = ref<CommitteeListItem[]>([])
const selectedCommittee = ref<CommitteeDetail | null>(null)
const statistics = ref<CommitteeStatistics | null>(null)
const aiAnalysis = ref<CommitteeAiAnalysisResponse | null>(null)
const isLoading = ref(false)
const isLoadingStats = ref(false)
const isLoadingAi = ref(false)
const competitions = ref<RfpListItem[]>([])
const isLoadingCompetitions = ref(false)
const error = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const searchQuery = ref('')
const statusFilter = ref<CommitteeStatus | ''>('')
const typeFilter = ref<CommitteeType | ''>('')

/* Dialogs */
const showCreateDialog = ref(false)
const showDetailDialog = ref(false)
const showStatusDialog = ref(false)
const showAddMemberDialog = ref(false)
const showAiPanel = ref(false)
const isSubmitting = ref(false)

/* Create / Edit form — uses phases[] instead of activeFromPhase/activeToPhase */
const formData = ref<CreateCommitteeRequest>({
  nameAr: '',
  nameEn: '',
  type: CommitteeType.TechnicalEvaluation,
  isPermanent: true,
  scopeType: CommitteeScopeType.Comprehensive,
  description: '',
  startDate: '',
  endDate: '',
  competitionIds: [],
  phases: [],
})

/* Status change form */
const statusChangeData = ref<ChangeCommitteeStatusRequest>({
  newStatus: CommitteeStatus.Suspended,
  reason: '',
})

/* Add member form — simplified: only userId and role */
const memberFormData = ref<AddCommitteeMemberRequest>({
  userId: '',
  role: CommitteeMemberRole.Member,
})

/* User search for add member */
const userSearchQuery = ref('')
const eligibleUsers = ref<EligibleUser[]>([])
const isSearchingUsers = ref(false)
const selectedUser = ref<EligibleUser | null>(null)

const isEditMode = ref(false)
const editingCommitteeId = ref('')

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
const isRtl = computed(() => locale.value === 'ar')

const showPhaseFields = computed(() =>
  formData.value.scopeType === CommitteeScopeType.SpecificPhasesAllCompetitions ||
  formData.value.scopeType === CommitteeScopeType.SpecificPhasesSpecificCompetitions
)

const showCompetitionFields = computed(() =>
  formData.value.scopeType === CommitteeScopeType.SpecificPhasesSpecificCompetitions
)

const statusOptions = computed(() => [
  { value: '', label: t('committees.statuses.all') },
  { value: CommitteeStatus.Active, label: t('committees.statuses.active') },
  { value: CommitteeStatus.Suspended, label: t('committees.statuses.suspended') },
  { value: CommitteeStatus.Dissolved, label: t('committees.statuses.dissolved') },
  { value: CommitteeStatus.Expired, label: t('committees.statuses.expired') },
])

const typeOptions = computed(() => [
  { value: CommitteeType.TechnicalEvaluation, label: t('committees.types.technicalEvaluation') },
  { value: CommitteeType.FinancialEvaluation, label: t('committees.types.financialEvaluation') },
  { value: CommitteeType.OtherPermanent, label: t('committees.types.otherPermanent') },
])

const typeFilterOptions = computed(() => [
  { value: '', label: t('committees.filters.allTypes') },
  ...typeOptions.value,
])

const scopeTypeOptions = computed(() => [
  { value: CommitteeScopeType.Comprehensive, label: t('committees.scopeTypes.comprehensive') },
  { value: CommitteeScopeType.SpecificPhasesAllCompetitions, label: t('committees.scopeTypes.specificPhasesAll') },
  { value: CommitteeScopeType.SpecificPhasesSpecificCompetitions, label: t('committees.scopeTypes.specificPhasesSpecific') },
])

const phaseOptions = computed(() => [
  { value: CompetitionPhase.BookletPreparation, label: t('committees.phases.bookletPreparation') },
  { value: CompetitionPhase.BookletApproval, label: t('committees.phases.bookletApproval') },
  { value: CompetitionPhase.BookletPublishing, label: t('committees.phases.bookletPublishing') },
  { value: CompetitionPhase.OfferReception, label: t('committees.phases.offerReception') },
  { value: CompetitionPhase.TechnicalAnalysis, label: t('committees.phases.technicalAnalysis') },
  { value: CompetitionPhase.FinancialAnalysis, label: t('committees.phases.financialAnalysis') },
  { value: CompetitionPhase.AwardNotification, label: t('committees.phases.awardNotification') },
  { value: CompetitionPhase.ContractApproval, label: t('committees.phases.contractApproval') },
  { value: CompetitionPhase.ContractSigning, label: t('committees.phases.contractSigning') },
])

const memberRoleOptions = computed(() => [
  { value: CommitteeMemberRole.Chair, label: t('committees.roles.chair') },
  { value: CommitteeMemberRole.Member, label: t('committees.roles.member') },
  { value: CommitteeMemberRole.Secretary, label: t('committees.roles.secretary') },
])

const statusChangeOptions = computed(() => [
  { value: CommitteeStatus.Suspended, label: t('committees.actions.suspend') },
  { value: CommitteeStatus.Active, label: t('committees.actions.reactivate') },
  { value: CommitteeStatus.Dissolved, label: t('committees.actions.dissolve') },
])

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getStatusBadge(status: CommitteeStatus) {
  const config: Record<number, { label: string; bgClass: string; textClass: string }> = {
    [CommitteeStatus.Active]: { label: t('committees.statuses.active'), bgClass: 'bg-success/10', textClass: 'text-success' },
    [CommitteeStatus.Suspended]: { label: t('committees.statuses.suspended'), bgClass: 'bg-warning/10', textClass: 'text-warning' },
    [CommitteeStatus.Dissolved]: { label: t('committees.statuses.dissolved'), bgClass: 'bg-danger/10', textClass: 'text-danger' },
    [CommitteeStatus.Expired]: { label: t('committees.statuses.expired'), bgClass: 'bg-surface-muted', textClass: 'text-tertiary' },
  }
  return config[status] || config[CommitteeStatus.Active]
}

function getTypeName(type: CommitteeType): string {
  const map: Record<number, string> = {
    [CommitteeType.TechnicalEvaluation]: t('committees.types.technicalEvaluation'),
    [CommitteeType.FinancialEvaluation]: t('committees.types.financialEvaluation'),
    [CommitteeType.BookletPreparation]: t('committees.types.bookletPreparation'),
    [CommitteeType.InquiryReview]: t('committees.types.inquiryReview'),
    [CommitteeType.OtherPermanent]: t('committees.types.otherPermanent'),
  }
  return map[type] || ''
}

function getScopeTypeName(scopeType: number): string {
  const map: Record<number, string> = {
    [CommitteeScopeType.Comprehensive]: t('committees.scopeTypes.comprehensive'),
    [CommitteeScopeType.SpecificPhasesAllCompetitions]: t('committees.scopeTypes.specificPhasesAll'),
    [CommitteeScopeType.SpecificPhasesSpecificCompetitions]: t('committees.scopeTypes.specificPhasesSpecific'),
  }
  return map[scopeType] || ''
}

function getPhaseName(phase: CompetitionPhase | null | undefined): string {
  if (phase === null || phase === undefined) return '-'
  const map: Record<number, string> = {
    [CompetitionPhase.BookletPreparation]: t('committees.phases.bookletPreparation'),
    [CompetitionPhase.BookletApproval]: t('committees.phases.bookletApproval'),
    [CompetitionPhase.BookletPublishing]: t('committees.phases.bookletPublishing'),
    [CompetitionPhase.OfferReception]: t('committees.phases.offerReception'),
    [CompetitionPhase.TechnicalAnalysis]: t('committees.phases.technicalAnalysis'),
    [CompetitionPhase.FinancialAnalysis]: t('committees.phases.financialAnalysis'),
    [CompetitionPhase.AwardNotification]: t('committees.phases.awardNotification'),
    [CompetitionPhase.ContractApproval]: t('committees.phases.contractApproval'),
    [CompetitionPhase.ContractSigning]: t('committees.phases.contractSigning'),
  }
  return map[phase] || '-'
}

function getRoleName(role: CommitteeMemberRole): string {
  const map: Record<number, string> = {
    [CommitteeMemberRole.Chair]: t('committees.roles.chair'),
    [CommitteeMemberRole.Member]: t('committees.roles.member'),
    [CommitteeMemberRole.Secretary]: t('committees.roles.secretary'),
  }
  return map[role] || ''
}

function getCommitteeName(item: CommitteeListItem | CommitteeDetail): string {
  return locale.value === 'ar' ? item.nameAr : item.nameEn
}

function getHealthScoreClass(score: number): string {
  if (score >= 80) return 'text-success'
  if (score >= 50) return 'text-warning'
  return 'text-danger'
}

/* ------------------------------------------------------------------ */
/*  Phase Toggle Helpers                                               */
/* ------------------------------------------------------------------ */
function togglePhase(phase: CompetitionPhase) {
  const phases = formData.value.phases || []
  const idx = phases.indexOf(phase)
  if (idx >= 0) {
    phases.splice(idx, 1)
  } else {
    phases.push(phase)
  }
  formData.value.phases = [...phases]
}

function isPhaseSelected(phase: CompetitionPhase): boolean {
  return (formData.value.phases || []).includes(phase)
}

function selectAllPhases() {
  formData.value.phases = phaseOptions.value.map(o => o.value)
}

function deselectAllPhases() {
  formData.value.phases = []
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadCompetitions() {
  isLoadingCompetitions.value = true
  try {
    const result = await fetchRfpList({ page: 1, pageSize: 100 })
    if (result.success) {
      competitions.value = result.data.items
    }
  } catch (err) {
    console.warn('[Committees] Failed to load competitions:', err)
  } finally {
    isLoadingCompetitions.value = false
  }
}

async function loadStatistics() {
  isLoadingStats.value = true
  try {
    statistics.value = await fetchCommitteeStatistics(true)
  } catch (err) {
    console.warn('[Committees] Statistics API unavailable:', err)
    statistics.value = null
  } finally {
    isLoadingStats.value = false
  }
}

async function loadCommittees() {
  isLoading.value = true
  error.value = ''
  try {
    const result = await fetchCommittees({
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
      isPermanent: true,
      type: typeFilter.value !== '' ? (typeFilter.value as CommitteeType) : undefined,
      status: statusFilter.value !== '' ? (statusFilter.value as CommitteeStatus) : undefined,
      search: searchQuery.value || undefined,
    })
    committees.value = result.items
    totalCount.value = result.totalCount
  } catch (err) {
    console.warn('[Committees] API unavailable:', err)
    committees.value = []
  } finally {
    isLoading.value = false
  }
}

async function loadCommitteeDetail(id: string) {
  try {
    selectedCommittee.value = await fetchCommitteeById(id)
    showDetailDialog.value = true
    aiAnalysis.value = null
    showAiPanel.value = false
  } catch {
    error.value = t('committees.errors.loadDetailFailed')
  }
}

async function loadAiAnalysis(committeeId: string) {
  isLoadingAi.value = true
  showAiPanel.value = true
  try {
    aiAnalysis.value = await fetchCommitteeAiAnalysis(committeeId)
  } catch (err) {
    console.warn('[Committees] AI Analysis unavailable:', err)
    aiAnalysis.value = null
  } finally {
    isLoadingAi.value = false
  }
}

async function handleSearchUsers() {
  if (!selectedCommittee.value || userSearchQuery.value.length < 2) {
    eligibleUsers.value = []
    return
  }
  isSearchingUsers.value = true
  try {
    eligibleUsers.value = await searchEligibleUsers(
      selectedCommittee.value.id,
      userSearchQuery.value,
      memberFormData.value.role,
    )
  } catch (err) {
    console.warn('[Committees] User search failed:', err)
    eligibleUsers.value = []
  } finally {
    isSearchingUsers.value = false
  }
}

function selectUser(user: EligibleUser) {
  selectedUser.value = user
  memberFormData.value.userId = user.id
  userSearchQuery.value = user.fullName
  eligibleUsers.value = []
}

async function handleCreateCommittee() {
  isSubmitting.value = true
  try {
    if (isEditMode.value && editingCommitteeId.value) {
      const updateData: UpdateCommitteeRequest = {
        nameAr: formData.value.nameAr,
        nameEn: formData.value.nameEn,
        description: formData.value.description,
        scopeType: formData.value.scopeType,
        phases: showPhaseFields.value ? formData.value.phases : undefined,
        competitionIds: showCompetitionFields.value ? formData.value.competitionIds : undefined,
      }
      await updateCommittee(editingCommitteeId.value, updateData)
    } else {
      const payload: CreateCommitteeRequest = {
        ...formData.value,
        isPermanent: true,
        startDate: formData.value.startDate ? new Date(formData.value.startDate).toISOString() : '',
        endDate: formData.value.endDate ? new Date(formData.value.endDate).toISOString() : '',
        phases: showPhaseFields.value ? formData.value.phases : undefined,
        competitionIds: showCompetitionFields.value ? formData.value.competitionIds : undefined,
      }
      await createCommittee(payload)
    }
    showCreateDialog.value = false
    resetForm()
    await loadCommittees()
    await loadStatistics()
  } catch (err: any) {
    const apiMsg = err?.response?.data?.detail || err?.response?.data?.title || err?.response?.data
    error.value = typeof apiMsg === 'string' ? apiMsg : (isEditMode.value
      ? t('committees.errors.updateFailed')
      : t('committees.errors.createFailed'))
  } finally {
    isSubmitting.value = false
  }
}

async function handleChangeStatus() {
  if (!selectedCommittee.value) return
  isSubmitting.value = true
  try {
    await changeCommitteeStatus(selectedCommittee.value.id, statusChangeData.value)
    showStatusDialog.value = false
    showDetailDialog.value = false
    await loadCommittees()
    await loadStatistics()
  } catch {
    error.value = t('committees.errors.statusChangeFailed')
  } finally {
    isSubmitting.value = false
  }
}

async function handleAddMember() {
  if (!selectedCommittee.value) {
    error.value = t('committees.errors.addMemberFailed')
    return
  }
  if (!memberFormData.value.userId) {
    error.value = t('committees.errors.selectUserRequired')
    return
  }
  isSubmitting.value = true
  try {
    await addCommitteeMember(selectedCommittee.value.id, {
      userId: memberFormData.value.userId,
      role: memberFormData.value.role,
    })
    showAddMemberDialog.value = false
    resetMemberForm()
    await loadCommitteeDetail(selectedCommittee.value.id)
    await loadStatistics()
  } catch (err: any) {
    const apiMsg = err?.response?.data?.detail || err?.response?.data?.title || err?.response?.data
    error.value = typeof apiMsg === 'string' ? apiMsg : t('committees.errors.addMemberFailed')
  } finally {
    isSubmitting.value = false
  }
}

async function handleRemoveMember(member: CommitteeMember) {
  if (!selectedCommittee.value) return
  const reason = prompt(t('committees.prompts.removalReason'))
  if (!reason) return
  try {
    await removeCommitteeMember(selectedCommittee.value.id, member.userId, reason)
    await loadCommitteeDetail(selectedCommittee.value.id)
    await loadStatistics()
  } catch {
    error.value = t('committees.errors.removeMemberFailed')
  }
}

/* ------------------------------------------------------------------ */
/*  Form Helpers                                                       */
/* ------------------------------------------------------------------ */
function openCreateDialog() {
  isEditMode.value = false
  editingCommitteeId.value = ''
  resetForm()
  showCreateDialog.value = true
}

function openEditDialog(committee: CommitteeDetail) {
  isEditMode.value = true
  editingCommitteeId.value = committee.id
  formData.value = {
    nameAr: committee.nameAr,
    nameEn: committee.nameEn,
    type: committee.type,
    isPermanent: true,
    scopeType: committee.scopeType,
    description: committee.description || '',
    startDate: committee.startDate,
    endDate: committee.endDate,
    competitionIds: committee.competitions?.map(c => c.competitionId) || [],
    phases: committee.phases || [],
  }
  showCreateDialog.value = true
}

function openStatusDialog() {
  statusChangeData.value = { newStatus: CommitteeStatus.Suspended, reason: '' }
  showStatusDialog.value = true
}

function openAddMemberDialog() {
  resetMemberForm()
  showAddMemberDialog.value = true
}

function resetForm() {
  formData.value = {
    nameAr: '',
    nameEn: '',
    type: CommitteeType.TechnicalEvaluation,
    isPermanent: true,
    scopeType: CommitteeScopeType.Comprehensive,
    description: '',
    startDate: '',
    endDate: '',
    competitionIds: [],
    phases: [],
  }
}

function resetMemberForm() {
  memberFormData.value = {
    userId: '',
    role: CommitteeMemberRole.Member,
  }
  userSearchQuery.value = ''
  selectedUser.value = null
  eligibleUsers.value = []
}

function toggleCompetition(compId: string) {
  const ids = formData.value.competitionIds || []
  const idx = ids.indexOf(compId)
  if (idx >= 0) {
    ids.splice(idx, 1)
  } else {
    ids.push(compId)
  }
  formData.value.competitionIds = [...ids]
}

function isCompetitionSelected(compId: string): boolean {
  return (formData.value.competitionIds || []).includes(compId)
}

/* ------------------------------------------------------------------ */
/*  Pagination                                                         */
/* ------------------------------------------------------------------ */
function goToPage(page: number) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
  }
}

/* ------------------------------------------------------------------ */
/*  Watchers                                                           */
/* ------------------------------------------------------------------ */
let searchTimeout: ReturnType<typeof setTimeout> | null = null
watch(searchQuery, () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    loadCommittees()
  }, 400)
})

let userSearchTimeout: ReturnType<typeof setTimeout> | null = null
watch(userSearchQuery, () => {
  if (userSearchTimeout) clearTimeout(userSearchTimeout)
  if (selectedUser.value && userSearchQuery.value === selectedUser.value.fullName) return
  selectedUser.value = null
  memberFormData.value.userId = ''
  userSearchTimeout = setTimeout(() => {
    handleSearchUsers()
  }, 400)
})

watch(statusFilter, () => { currentPage.value = 1; loadCommittees() })
watch(typeFilter, () => { currentPage.value = 1; loadCommittees() })
watch(currentPage, () => { loadCommittees() })

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  loadCommittees()
  loadStatistics()
  loadCompetitions()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('committees.permanentTitle') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('committees.permanentSubtitle') }}
        </p>
      </div>
      <button
        v-if="canCreateCommittee"
        class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark"
        @click="openCreateDialog"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ t('committees.actions.create') }}
      </button>
    </div>

    <!-- Statistics Dashboard -->
    <div v-if="isLoadingStats" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <div v-for="i in 5" :key="i" class="animate-pulse rounded-lg border border-surface-dim bg-white p-4">
        <div class="h-3 w-20 rounded bg-surface-dim"></div>
        <div class="mt-3 h-7 w-12 rounded bg-surface-dim"></div>
      </div>
    </div>
    <div v-else-if="statistics" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10"><i class="pi pi-users text-sm text-primary"></i></div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.total') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-secondary">{{ statistics.totalCommittees }}</p>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-success/10"><i class="pi pi-check-circle text-sm text-success"></i></div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.active') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-success">{{ statistics.activeCommittees }}</p>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-warning/10"><i class="pi pi-clock text-sm text-warning"></i></div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.expiringSoon') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-warning">{{ statistics.committeesExpiringSoon }}</p>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-danger/10"><i class="pi pi-exclamation-triangle text-sm text-danger"></i></div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.noChair') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-danger">{{ statistics.committeesWithNoChair }}</p>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-info/10"><i class="pi pi-chart-bar text-sm text-info"></i></div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.avgMembers') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-secondary">{{ statistics.averageMembers }}</p>
      </div>
    </div>

    <!-- Error Banner -->
    <div v-if="error" class="flex items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-4">
      <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
      <p class="flex-1 text-sm text-danger">{{ error }}</p>
      <button class="text-xs font-medium text-danger hover:underline" @click="error = ''">{{ t('common.close') }}</button>
    </div>

    <!-- Filters -->
    <div class="flex flex-col gap-4 rounded-lg border border-surface-dim bg-white p-4 sm:flex-row sm:items-center">
      <div class="relative flex-1">
        <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary"></i>
        <input v-model="searchQuery" type="text" :placeholder="t('committees.searchPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-10 pe-4 text-sm text-secondary placeholder:text-tertiary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>
      <select v-model="typeFilter" class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
        <option v-for="option in typeFilterOptions" :key="String(option.value)" :value="option.value">{{ option.label }}</option>
      </select>
      <select v-model="statusFilter" class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
        <option v-for="option in statusOptions" :key="String(option.value)" :value="option.value">{{ option.label }}</option>
      </select>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-16">
      <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
    </div>

    <!-- Empty State -->
    <div v-else-if="committees.length === 0" class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16">
      <i class="pi pi-users text-5xl text-surface-dim"></i>
      <p class="mt-4 text-sm text-tertiary">{{ t('committees.emptyState') }}</p>
      <button class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark" @click="openCreateDialog">
        {{ t('committees.actions.create') }}
      </button>
    </div>

    <!-- Committee Table -->
    <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-surface-dim bg-surface-ground">
              <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('committees.table.name') }}</th>
              <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('committees.table.type') }}</th>
              <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('committees.table.scope') }}</th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('committees.table.members') }}</th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('committees.table.status') }}</th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('committees.table.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="committee in committees" :key="committee.id" class="border-b border-surface-dim last:border-b-0 transition-colors hover:bg-surface-ground/50">
              <td class="px-4 py-3">
                <p class="font-medium text-secondary">{{ getCommitteeName(committee) }}</p>
                <p v-if="committee.competitions && committee.competitions.length > 0" class="mt-0.5 text-xs text-tertiary">
                  {{ committee.competitions.length }} {{ t('committees.table.linkedCompetitions') }}
                </p>
              </td>
              <td class="px-4 py-3 text-tertiary">{{ getTypeName(committee.type) }}</td>
              <td class="px-4 py-3 text-xs text-tertiary">
                <span class="inline-block rounded-full px-2 py-0.5 text-xs font-medium bg-primary/10 text-primary">
                  {{ getScopeTypeName(committee.scopeType) }}
                </span>
              </td>
              <td class="px-4 py-3 text-center text-secondary">
                <span class="inline-flex items-center gap-1">
                  <i class="pi pi-user text-xs text-tertiary"></i>
                  {{ committee.activeMemberCount }}
                </span>
              </td>
              <td class="px-4 py-3 text-center">
                <span :class="[getStatusBadge(committee.status).bgClass, getStatusBadge(committee.status).textClass]" class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">
                  {{ getStatusBadge(committee.status).label }}
                </span>
              </td>
              <td class="px-4 py-3 text-center">
                <div class="flex items-center justify-center gap-1">
                  <button class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary" :title="t('common.view')" @click="loadCommitteeDetail(committee.id)">
                    <i class="pi pi-eye text-sm"></i>
                  </button>
                  <button class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-primary/10 hover:text-primary" :title="t('committees.actions.aiAnalysis')" @click="loadCommitteeDetail(committee.id); loadAiAnalysis(committee.id)">
                    <i class="pi pi-sparkles text-sm"></i>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between border-t border-surface-dim px-4 py-3">
        <p class="text-xs text-tertiary">
          {{ t('committees.pagination.showing', { from: (currentPage - 1) * pageSize + 1, to: Math.min(currentPage * pageSize, totalCount), total: totalCount }) }}
        </p>
        <div class="flex items-center gap-1">
          <button class="rounded-lg p-2 text-sm text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40" :disabled="currentPage === 1" @click="goToPage(currentPage - 1)">
            <i class="pi text-xs" :class="isRtl ? 'pi-chevron-right' : 'pi-chevron-left'"></i>
          </button>
          <template v-for="page in totalPages" :key="page">
            <button v-if="page <= 5 || page === totalPages || Math.abs(page - currentPage) <= 1" class="min-w-[2rem] rounded-lg px-2 py-1 text-sm transition-colors" :class="page === currentPage ? 'bg-primary text-white' : 'text-tertiary hover:bg-surface-ground'" @click="goToPage(page)">
              {{ page }}
            </button>
            <span v-else-if="page === 6 || page === totalPages - 1" class="px-1 text-tertiary">...</span>
          </template>
          <button class="rounded-lg p-2 text-sm text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40" :disabled="currentPage === totalPages" @click="goToPage(currentPage + 1)">
            <i class="pi text-xs" :class="isRtl ? 'pi-chevron-left' : 'pi-chevron-right'"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================ -->
    <!--  Create / Edit Committee Dialog                               -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div v-if="showCreateDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showCreateDialog = false">
        <div class="mx-4 w-full max-w-lg rounded-xl bg-white p-6 shadow-xl max-h-[90vh] overflow-y-auto">
          <div class="mb-6 flex items-center justify-between">
            <h2 class="text-lg font-bold text-secondary">
              {{ isEditMode ? t('committees.dialog.editTitle') : t('committees.dialog.createPermTitle') }}
            </h2>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showCreateDialog = false">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <form class="space-y-4" @submit.prevent="handleCreateCommittee">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.nameAr') }}</label>
              <input v-model="formData.nameAr" type="text" required dir="rtl" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.nameEn') }}</label>
              <input v-model="formData.nameEn" type="text" required dir="ltr" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.type') }}</label>
              <select v-model="formData.type" :disabled="isEditMode" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground disabled:text-tertiary">
                <option v-for="opt in typeOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
              </select>
            </div>

            <!-- Scope Type -->
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.scopeType') }}</label>
              <select v-model="formData.scopeType" :disabled="isEditMode" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground disabled:text-tertiary">
                <option v-for="opt in scopeTypeOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
              </select>
              <p class="mt-1 text-xs text-tertiary">
                <template v-if="formData.scopeType === CommitteeScopeType.Comprehensive">{{ t('committees.scopeHints.comprehensive') }}</template>
                <template v-else-if="formData.scopeType === CommitteeScopeType.SpecificPhasesAllCompetitions">{{ t('committees.scopeHints.specificPhasesAll') }}</template>
                <template v-else>{{ t('committees.scopeHints.specificPhasesSpecific') }}</template>
              </p>
            </div>

            <!-- Phase Selection via Checkboxes (conditional) -->
            <div v-if="showPhaseFields">
              <div class="mb-2 flex items-center justify-between">
                <label class="block text-sm font-medium text-secondary">{{ t('committees.form.selectPhases') }}</label>
                <div class="flex items-center gap-2">
                  <button type="button" class="text-xs font-medium text-primary hover:underline" @click="selectAllPhases">{{ t('committees.form.selectAll') }}</button>
                  <span class="text-xs text-tertiary">|</span>
                  <button type="button" class="text-xs font-medium text-tertiary hover:underline" @click="deselectAllPhases">{{ t('committees.form.deselectAll') }}</button>
                </div>
              </div>
              <div class="rounded-lg border border-surface-dim p-3 space-y-1.5">
                <label
                  v-for="opt in phaseOptions"
                  :key="opt.value"
                  class="flex cursor-pointer items-center gap-2.5 rounded-lg px-2 py-1.5 text-sm transition-colors hover:bg-surface-ground"
                  :class="{ 'bg-primary/5': isPhaseSelected(opt.value) }"
                >
                  <input
                    type="checkbox"
                    :checked="isPhaseSelected(opt.value)"
                    class="rounded border-surface-dim text-primary focus:ring-primary"
                    @change="togglePhase(opt.value)"
                  />
                  <span class="text-secondary">{{ opt.label }}</span>
                </label>
              </div>
              <p v-if="(formData.phases || []).length > 0" class="mt-1.5 text-xs text-primary">
                {{ t('committees.form.selectedPhasesCount', { count: (formData.phases || []).length }) }}
              </p>
              <p v-else class="mt-1.5 text-xs text-danger">
                {{ t('committees.form.selectAtLeastOnePhase') }}
              </p>
            </div>

            <!-- Competition Selection (conditional) -->
            <div v-if="showCompetitionFields">
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.selectCompetitions') }}</label>
              <div v-if="isLoadingCompetitions" class="flex items-center gap-2 py-2 text-xs text-tertiary">
                <i class="pi pi-spin pi-spinner text-xs"></i>
                {{ t('common.loading') }}
              </div>
              <div v-else class="max-h-40 overflow-y-auto rounded-lg border border-surface-dim p-2 space-y-1">
                <label v-for="comp in competitions" :key="comp.id" class="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1.5 text-sm transition-colors hover:bg-surface-ground" :class="{ 'bg-primary/5': isCompetitionSelected(comp.id) }">
                  <input type="checkbox" :checked="isCompetitionSelected(comp.id)" class="rounded border-surface-dim text-primary focus:ring-primary" @change="toggleCompetition(comp.id)" />
                  <span class="text-secondary">{{ comp.projectName }}</span>
                  <span class="text-xs text-tertiary">({{ comp.referenceNumber }})</span>
                </label>
                <p v-if="competitions.length === 0" class="py-2 text-center text-xs text-tertiary">{{ t('committees.form.noCompetitions') }}</p>
              </div>
              <p v-if="(formData.competitionIds || []).length > 0" class="mt-1 text-xs text-primary">
                {{ t('committees.form.selectedCount', { count: (formData.competitionIds || []).length }) }}
              </p>
            </div>

            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.description') }}</label>
              <textarea v-model="formData.description" rows="3" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.startDate') }}</label>
                <input v-model="formData.startDate" type="date" :disabled="isEditMode" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground" />
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.endDate') }}</label>
                <input v-model="formData.endDate" type="date" :disabled="isEditMode" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground" />
              </div>
            </div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button type="button" class="rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-medium text-secondary hover:bg-surface-ground" @click="showCreateDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting || (showPhaseFields && (!formData.phases || formData.phases.length === 0))" class="rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-dark disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner me-1 text-xs"></i>
                {{ isEditMode ? t('common.save') : t('common.create') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!--  Committee Detail Dialog                                      -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div v-if="showDetailDialog && selectedCommittee" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showDetailDialog = false">
        <div class="mx-4 w-full max-w-3xl rounded-xl bg-white shadow-xl max-h-[90vh] overflow-y-auto">
          <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-dim bg-white px-6 py-4">
            <div>
              <h2 class="text-lg font-bold text-secondary">{{ getCommitteeName(selectedCommittee) }}</h2>
              <p class="mt-0.5 text-xs text-tertiary">{{ getScopeTypeName(selectedCommittee.scopeType) }}</p>
            </div>
            <div class="flex items-center gap-2">
              <button class="rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary hover:bg-primary/5" @click="loadAiAnalysis(selectedCommittee!.id)">
                <i class="pi pi-sparkles me-1 text-xs"></i>{{ t('committees.actions.aiAnalysis') }}
              </button>
              <button v-if="canEditCommittee" class="rounded-lg border border-surface-dim px-3 py-1.5 text-xs font-medium text-secondary hover:bg-surface-ground" @click="openEditDialog(selectedCommittee!)">
                <i class="pi pi-pencil me-1 text-xs"></i>{{ t('common.edit') }}
              </button>
              <button v-if="canEditCommittee" class="rounded-lg border border-warning/30 px-3 py-1.5 text-xs font-medium text-warning hover:bg-warning/5" @click="openStatusDialog">
                <i class="pi pi-cog me-1 text-xs"></i>{{ t('committees.actions.changeStatus') }}
              </button>
              <button class="rounded-lg p-1.5 text-tertiary hover:bg-surface-ground" @click="showDetailDialog = false">
                <i class="pi pi-times"></i>
              </button>
            </div>
          </div>

          <!-- Info Grid -->
          <div class="grid grid-cols-2 gap-4 p-6 sm:grid-cols-4">
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.type') }}</p>
              <p class="mt-1 text-sm font-medium text-secondary">{{ getTypeName(selectedCommittee.type) }}</p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.status') }}</p>
              <span :class="[getStatusBadge(selectedCommittee.status).bgClass, getStatusBadge(selectedCommittee.status).textClass]" class="mt-1 inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">
                {{ getStatusBadge(selectedCommittee.status).label }}
              </span>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.startDate') }}</p>
              <p class="mt-1 text-sm text-secondary">{{ formatDate(selectedCommittee.startDate) }}</p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.detail.endDate') }}</p>
              <p class="mt-1 text-sm text-secondary">{{ formatDate(selectedCommittee.endDate) }}</p>
            </div>
            <div v-if="selectedCommittee.phases && selectedCommittee.phases.length > 0" class="col-span-2">
              <p class="text-xs text-tertiary">{{ t('committees.table.phases') }}</p>
              <div class="mt-1 flex flex-wrap gap-1">
                <span
                  v-for="phase in selectedCommittee.phases"
                  :key="phase"
                  class="inline-block rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary"
                >
                  {{ getPhaseName(phase) }}
                </span>
              </div>
            </div>
            <div class="col-span-2">
              <p class="text-xs text-tertiary">{{ t('committees.form.description') }}</p>
              <p class="mt-1 text-sm text-secondary">{{ selectedCommittee.description || t('committees.detail.noDescription') }}</p>
            </div>
          </div>

          <!-- Linked Competitions -->
          <div v-if="selectedCommittee.competitions && selectedCommittee.competitions.length > 0" class="border-t border-surface-dim px-6 py-4">
            <h3 class="mb-3 text-sm font-bold text-secondary">{{ t('committees.detail.linkedCompetitions') }}</h3>
            <div class="flex flex-wrap gap-2">
              <span v-for="comp in selectedCommittee.competitions" :key="comp.id" class="inline-flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
                <i class="pi pi-briefcase text-xs"></i>
                {{ comp.competitionNameAr || comp.competitionId.substring(0, 8) }}
              </span>
            </div>
          </div>

          <!-- AI Analysis Panel -->
          <div v-if="showAiPanel" class="border-t border-surface-dim px-6 py-4">
            <div class="mb-4 flex items-center justify-between">
              <h3 class="flex items-center gap-2 text-sm font-bold text-secondary">
                <i class="pi pi-sparkles text-primary"></i>{{ t('committees.ai.title') }}
              </h3>
              <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showAiPanel = false">
                <i class="pi pi-times text-xs"></i>
              </button>
            </div>
            <div v-if="isLoadingAi" class="flex items-center justify-center py-8">
              <div class="text-center">
                <i class="pi pi-spin pi-spinner text-2xl text-primary"></i>
                <p class="mt-2 text-xs text-tertiary">{{ t('committees.ai.analyzing') }}</p>
              </div>
            </div>
            <div v-else-if="aiAnalysis" class="space-y-4">
              <div class="flex items-center gap-4 rounded-lg bg-surface-ground p-4">
                <div class="relative flex h-16 w-16 items-center justify-center">
                  <svg class="h-16 w-16 -rotate-90" viewBox="0 0 36 36">
                    <path class="text-surface-dim" stroke="currentColor" stroke-width="3" fill="none" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                    <path :class="getHealthScoreClass(aiAnalysis.insight.healthScore)" stroke="currentColor" stroke-width="3" fill="none" :stroke-dasharray="`${aiAnalysis.insight.healthScore}, 100`" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                  </svg>
                  <span class="absolute text-sm font-bold" :class="getHealthScoreClass(aiAnalysis.insight.healthScore)">{{ aiAnalysis.insight.healthScore }}%</span>
                </div>
                <div class="flex-1">
                  <p class="text-sm font-semibold text-secondary">{{ aiAnalysis.insight.healthLabel }}</p>
                  <p class="mt-1 text-xs text-tertiary leading-relaxed">{{ aiAnalysis.insight.summary }}</p>
                </div>
              </div>
              <div v-if="aiAnalysis.insight.recommendations.length > 0">
                <h4 class="mb-2 text-xs font-semibold text-secondary">{{ t('committees.ai.recommendations') }}</h4>
                <ul class="space-y-1.5">
                  <li v-for="(rec, idx) in aiAnalysis.insight.recommendations" :key="idx" class="flex items-start gap-2 rounded-lg bg-success/5 px-3 py-2 text-xs text-secondary">
                    <i class="pi pi-check-circle mt-0.5 text-success"></i><span>{{ rec }}</span>
                  </li>
                </ul>
              </div>
              <div v-if="aiAnalysis.insight.risks.length > 0">
                <h4 class="mb-2 text-xs font-semibold text-secondary">{{ t('committees.ai.risks') }}</h4>
                <ul class="space-y-1.5">
                  <li v-for="(risk, idx) in aiAnalysis.insight.risks" :key="idx" class="flex items-start gap-2 rounded-lg bg-danger/5 px-3 py-2 text-xs text-secondary">
                    <i class="pi pi-exclamation-triangle mt-0.5 text-danger"></i><span>{{ risk }}</span>
                  </li>
                </ul>
              </div>
            </div>
            <div v-else class="py-4 text-center text-xs text-tertiary">{{ t('committees.ai.unavailable') }}</div>
          </div>

          <!-- Members Section -->
          <div class="border-t border-surface-dim px-6 py-4">
            <div class="mb-4 flex items-center justify-between">
              <h3 class="text-sm font-bold text-secondary">{{ t('committees.detail.members') }}</h3>
              <button class="flex items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary-dark" @click="openAddMemberDialog">
                <i class="pi pi-user-plus text-xs"></i>{{ t('committees.actions.addMember') }}
              </button>
            </div>
            <div v-if="selectedCommittee.members.length === 0" class="py-6 text-center text-xs text-tertiary">
              {{ t('committees.detail.noMembers') }}
            </div>
            <div v-else class="space-y-2">
              <div v-for="member in selectedCommittee.members" :key="member.id" class="flex items-center justify-between rounded-lg border border-surface-dim p-3" :class="{ 'opacity-50': !member.isActive }">
                <div class="flex items-center gap-3">
                  <div class="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-sm font-bold text-primary">
                    {{ member.userFullName.charAt(0) }}
                  </div>
                  <div>
                    <p class="text-sm font-medium text-secondary">{{ member.userFullName }}</p>
                    <div class="flex items-center gap-2 mt-0.5">
                      <span class="text-xs text-tertiary">{{ getRoleName(member.role) }}</span>
                      <span v-if="!member.isActive" class="text-xs text-danger">{{ t('committees.detail.removed') }}</span>
                    </div>
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <span class="text-xs text-tertiary">{{ formatDate(member.assignedAt) }}</span>
                  <button v-if="member.isActive" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-danger/10 hover:text-danger" :title="t('committees.actions.removeMember')" @click="handleRemoveMember(member)">
                    <i class="pi pi-user-minus text-xs"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!--  Add Member Dialog (Dynamic User Search)                      -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div v-if="showAddMemberDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showAddMemberDialog = false">
        <div class="mx-4 w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
          <div class="mb-6 flex items-center justify-between">
            <h2 class="text-lg font-bold text-secondary">{{ t('committees.dialog.addMemberTitle') }}</h2>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showAddMemberDialog = false">
              <i class="pi pi-times"></i>
            </button>
          </div>
          <form class="space-y-4" @submit.prevent="handleAddMember">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.role') }}</label>
              <select v-model="memberFormData.role" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                <option v-for="opt in memberRoleOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
              </select>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.searchUser') }}</label>
              <div class="relative">
                <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary"></i>
                <input v-model="userSearchQuery" type="text" :placeholder="t('committees.form.searchUserPlaceholder')" class="w-full rounded-lg border border-surface-dim py-2.5 ps-10 pe-4 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" :class="{ 'border-success ring-1 ring-success': selectedUser }" />
                <i v-if="isSearchingUsers" class="pi pi-spin pi-spinner absolute end-3 top-1/2 -translate-y-1/2 text-sm text-tertiary"></i>
                <i v-else-if="selectedUser" class="pi pi-check-circle absolute end-3 top-1/2 -translate-y-1/2 text-sm text-success"></i>
              </div>
              <div v-if="eligibleUsers.length > 0 && !selectedUser" class="mt-1 max-h-48 overflow-y-auto rounded-lg border border-surface-dim bg-white shadow-lg">
                <button v-for="user in eligibleUsers" :key="user.id" type="button" class="flex w-full items-center gap-3 px-3 py-2.5 text-start transition-colors hover:bg-surface-ground" @click="selectUser(user)">
                  <div class="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">{{ user.fullName.charAt(0) }}</div>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-secondary truncate">{{ user.fullName }}</p>
                    <p class="text-xs text-tertiary truncate">{{ user.email }}</p>
                  </div>
                  <div class="flex flex-wrap gap-1">
                    <span v-for="role in user.roles" :key="role.normalizedName" class="rounded-full bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                      {{ locale === 'ar' ? role.nameAr : role.nameEn }}
                    </span>
                  </div>
                </button>
              </div>
              <p v-if="userSearchQuery.length >= 2 && eligibleUsers.length === 0 && !isSearchingUsers && !selectedUser" class="mt-1 text-xs text-tertiary">{{ t('committees.form.noUsersFound') }}</p>
              <div v-if="selectedUser" class="mt-2 flex items-center gap-2 rounded-lg bg-success/5 border border-success/20 px-3 py-2">
                <div class="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">{{ selectedUser.fullName.charAt(0) }}</div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-secondary truncate">{{ selectedUser.fullName }}</p>
                  <p class="text-xs text-tertiary truncate">{{ selectedUser.email }}</p>
                </div>
                <button type="button" class="rounded p-1 text-tertiary hover:text-danger" @click="resetMemberForm">
                  <i class="pi pi-times text-xs"></i>
                </button>
              </div>
            </div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button type="button" class="rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-medium text-secondary hover:bg-surface-ground" @click="showAddMemberDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting || !selectedUser" class="rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-dark disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner me-1 text-xs"></i>
                {{ t('committees.actions.addMember') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!--  Change Status Dialog                                         -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div v-if="showStatusDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showStatusDialog = false">
        <div class="mx-4 w-full max-w-sm rounded-xl bg-white p-6 shadow-xl">
          <div class="mb-6 flex items-center justify-between">
            <h2 class="text-lg font-bold text-secondary">{{ t('committees.dialog.changeStatusTitle') }}</h2>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showStatusDialog = false">
              <i class="pi pi-times"></i>
            </button>
          </div>
          <form class="space-y-4" @submit.prevent="handleChangeStatus">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.newStatus') }}</label>
              <select v-model="statusChangeData.newStatus" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                <option v-for="opt in statusChangeOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
              </select>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('committees.form.reason') }}</label>
              <textarea v-model="statusChangeData.reason" rows="3" class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
            </div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button type="button" class="rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-medium text-secondary hover:bg-surface-ground" @click="showStatusDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="rounded-lg bg-warning px-4 py-2.5 text-sm font-medium text-white hover:bg-warning/90 disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner me-1 text-xs"></i>
                {{ t('committees.actions.confirm') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>
