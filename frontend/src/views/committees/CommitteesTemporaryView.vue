<script setup lang="ts">
/**
 * CommitteesTemporaryView — Temporary Committees Management Page.
 *
 * TASK-902: Displays a paginated, filterable list of temporary committees.
 * Temporary committees are linked to specific competitions and have phase ranges.
 * Data is fetched dynamically from the API (no mock data).
 *
 * Features:
 * - Paginated committee list with search & status filter
 * - Create / Edit / View committee dialogs
 * - Member management (add / remove)
 * - Competition linking and phase range selection
 * - Status change (Suspend / Reactivate / Dissolve)
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  fetchCommittees,
  fetchCommitteeById,
  createCommittee,
  updateCommittee,
  changeCommitteeStatus,
  addCommitteeMember,
  removeCommitteeMember,
} from '@/services/committeeService'
import type {
  CommitteeListItem,
  CommitteeDetail,
  CreateCommitteeRequest,
  UpdateCommitteeRequest,
  ChangeCommitteeStatusRequest,
  AddCommitteeMemberRequest,
  CommitteeMember,
} from '@/types/committee'
import {
  CommitteeType,
  CommitteeStatus,
  CommitteeMemberRole,
  CompetitionPhase,
} from '@/types/committee'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const { formatDate } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const committees = ref<CommitteeListItem[]>([])
const selectedCommittee = ref<CommitteeDetail | null>(null)
const isLoading = ref(false)
const error = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const searchQuery = ref('')
const statusFilter = ref<CommitteeStatus | ''>('')

/* Dialogs */
const showCreateDialog = ref(false)
const showDetailDialog = ref(false)
const showStatusDialog = ref(false)
const showAddMemberDialog = ref(false)
const isSubmitting = ref(false)

/* Create / Edit form */
const formData = ref<CreateCommitteeRequest>({
  nameAr: '',
  nameEn: '',
  type: CommitteeType.BookletPreparation,
  isPermanent: false,
  description: '',
  startDate: '',
  endDate: '',
  competitionId: '',
  activeFromPhase: CompetitionPhase.BookletPreparation,
  activeToPhase: CompetitionPhase.ContractSigning,
})

/* Status change form */
const statusChangeData = ref<ChangeCommitteeStatusRequest>({
  newStatus: CommitteeStatus.Suspended,
  reason: '',
})

/* Add member form */
const memberFormData = ref<AddCommitteeMemberRequest>({
  userId: '',
  userFullName: '',
  role: CommitteeMemberRole.Member,
  activeFromPhase: CompetitionPhase.BookletPreparation,
  activeToPhase: CompetitionPhase.ContractSigning,
})

const isEditMode = ref(false)
const editingCommitteeId = ref('')

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
const isRtl = computed(() => locale.value === 'ar')

const statusOptions = computed(() => [
  { value: '', label: t('committees.statuses.all') },
  { value: CommitteeStatus.Active, label: t('committees.statuses.active') },
  { value: CommitteeStatus.Suspended, label: t('committees.statuses.suspended') },
  { value: CommitteeStatus.Dissolved, label: t('committees.statuses.dissolved') },
  { value: CommitteeStatus.Expired, label: t('committees.statuses.expired') },
])

const typeOptions = computed(() => [
  { value: CommitteeType.BookletPreparation, label: t('committees.types.bookletPreparation') },
  { value: CommitteeType.InquiryReview, label: t('committees.types.inquiryReview') },
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
    [CommitteeStatus.Active]: {
      label: t('committees.statuses.active'),
      bgClass: 'bg-success/10',
      textClass: 'text-success',
    },
    [CommitteeStatus.Suspended]: {
      label: t('committees.statuses.suspended'),
      bgClass: 'bg-warning/10',
      textClass: 'text-warning',
    },
    [CommitteeStatus.Dissolved]: {
      label: t('committees.statuses.dissolved'),
      bgClass: 'bg-danger/10',
      textClass: 'text-danger',
    },
    [CommitteeStatus.Expired]: {
      label: t('committees.statuses.expired'),
      bgClass: 'bg-surface-muted',
      textClass: 'text-tertiary',
    },
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

function getPhaseName(phase: CompetitionPhase | null): string {
  if (phase === null) return '-'
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

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadCommittees() {
  isLoading.value = true
  error.value = ''
  try {
    const result = await fetchCommittees({
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
      isPermanent: false,
      status: statusFilter.value !== '' ? statusFilter.value : undefined,
      search: searchQuery.value || undefined,
    })
    committees.value = result.items
    totalCount.value = result.totalCount
  } catch (err) {
    /* Graceful degradation: show empty state when API is unavailable */
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
  } catch {
    error.value = t('committees.errors.loadDetailFailed')
  }
}

async function handleCreateCommittee() {
  isSubmitting.value = true
  try {
    if (isEditMode.value && editingCommitteeId.value) {
      const updateData: UpdateCommitteeRequest = {
        nameAr: formData.value.nameAr,
        nameEn: formData.value.nameEn,
        description: formData.value.description,
      }
      await updateCommittee(editingCommitteeId.value, updateData)
    } else {
      await createCommittee({ ...formData.value, isPermanent: false })
    }
    showCreateDialog.value = false
    resetForm()
    await loadCommittees()
  } catch {
    error.value = isEditMode.value
      ? t('committees.errors.updateFailed')
      : t('committees.errors.createFailed')
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
  } catch {
    error.value = t('committees.errors.statusChangeFailed')
  } finally {
    isSubmitting.value = false
  }
}

async function handleAddMember() {
  if (!selectedCommittee.value) return
  isSubmitting.value = true
  try {
    await addCommitteeMember(selectedCommittee.value.id, memberFormData.value)
    showAddMemberDialog.value = false
    memberFormData.value = {
      userId: '',
      userFullName: '',
      role: CommitteeMemberRole.Member,
      activeFromPhase: CompetitionPhase.BookletPreparation,
      activeToPhase: CompetitionPhase.ContractSigning,
    }
    await loadCommitteeDetail(selectedCommittee.value.id)
  } catch {
    error.value = t('committees.errors.addMemberFailed')
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
    isPermanent: false,
    description: committee.description || '',
    startDate: committee.startDate,
    endDate: committee.endDate,
    competitionId: committee.competitionId || '',
    activeFromPhase: committee.activeFromPhase || CompetitionPhase.BookletPreparation,
    activeToPhase: committee.activeToPhase || CompetitionPhase.ContractSigning,
  }
  showCreateDialog.value = true
}

function openStatusDialog() {
  statusChangeData.value = { newStatus: CommitteeStatus.Suspended, reason: '' }
  showStatusDialog.value = true
}

function resetForm() {
  formData.value = {
    nameAr: '',
    nameEn: '',
    type: CommitteeType.BookletPreparation,
    isPermanent: false,
    description: '',
    startDate: '',
    endDate: '',
    competitionId: '',
    activeFromPhase: CompetitionPhase.BookletPreparation,
    activeToPhase: CompetitionPhase.ContractSigning,
  }
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

watch(statusFilter, () => {
  currentPage.value = 1
  loadCommittees()
})

watch(currentPage, () => {
  loadCommittees()
})

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  loadCommittees()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('committees.temporaryTitle') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('committees.temporarySubtitle') }}
        </p>
      </div>
      <button
        class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark"
        @click="openCreateDialog"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ t('committees.actions.create') }}
      </button>
    </div>

    <!-- Error Banner -->
    <div
      v-if="error"
      class="flex items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-4"
    >
      <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
      <p class="flex-1 text-sm text-danger">{{ error }}</p>
      <button
        class="text-xs font-medium text-danger hover:underline"
        @click="error = ''"
      >
        {{ t('common.close') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="flex flex-col gap-4 rounded-lg border border-surface-dim bg-white p-4 sm:flex-row sm:items-center">
      <div class="relative flex-1">
        <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary"></i>
        <input
          v-model="searchQuery"
          type="text"
          :placeholder="t('committees.searchPlaceholder')"
          class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-10 pe-4 text-sm text-secondary placeholder:text-tertiary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>
      <select
        v-model="statusFilter"
        class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option
          v-for="option in statusOptions"
          :key="String(option.value)"
          :value="option.value"
        >
          {{ option.label }}
        </option>
      </select>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-16">
      <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="committees.length === 0"
      class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16"
    >
      <i class="pi pi-users text-5xl text-surface-dim"></i>
      <p class="mt-4 text-sm text-tertiary">{{ t('committees.emptyStateTemporary') }}</p>
      <button
        class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark"
        @click="openCreateDialog"
      >
        {{ t('committees.actions.create') }}
      </button>
    </div>

    <!-- Committee Table -->
    <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-surface-dim bg-surface-ground">
              <th class="px-4 py-3 text-start font-semibold text-secondary">
                {{ t('committees.table.name') }}
              </th>
              <th class="px-4 py-3 text-start font-semibold text-secondary">
                {{ t('committees.table.type') }}
              </th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">
                {{ t('committees.table.members') }}
              </th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">
                {{ t('committees.table.status') }}
              </th>
              <th class="px-4 py-3 text-start font-semibold text-secondary">
                {{ t('committees.table.startDate') }}
              </th>
              <th class="px-4 py-3 text-start font-semibold text-secondary">
                {{ t('committees.table.competition') }}
              </th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">
                {{ t('committees.table.actions') }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="committee in committees"
              :key="committee.id"
              class="border-b border-surface-dim last:border-b-0 transition-colors hover:bg-surface-ground/50"
            >
              <td class="px-4 py-3 font-medium text-secondary">
                {{ getCommitteeName(committee) }}
              </td>
              <td class="px-4 py-3 text-tertiary">
                {{ getTypeName(committee.type) }}
              </td>
              <td class="px-4 py-3 text-center text-secondary">
                {{ committee.activeMemberCount }}
              </td>
              <td class="px-4 py-3 text-center">
                <span
                  :class="[
                    getStatusBadge(committee.status).bgClass,
                    getStatusBadge(committee.status).textClass,
                  ]"
                  class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium"
                >
                  {{ getStatusBadge(committee.status).label }}
                </span>
              </td>
              <td class="px-4 py-3 text-tertiary">
                {{ formatDate(committee.startDate) }}
              </td>
              <td class="px-4 py-3 text-tertiary text-xs">
                {{ committee.competitionId ? committee.competitionId.substring(0, 8) + '...' : '-' }}
              </td>
              <td class="px-4 py-3 text-center">
                <button
                  class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary"
                  :title="t('common.edit')"
                  @click="loadCommitteeDetail(committee.id)"
                >
                  <i class="pi pi-eye text-sm"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div
        v-if="totalPages > 1"
        class="flex items-center justify-between border-t border-surface-dim px-4 py-3"
      >
        <p class="text-xs text-tertiary">
          {{ t('committees.pagination.showing', { from: (currentPage - 1) * pageSize + 1, to: Math.min(currentPage * pageSize, totalCount), total: totalCount }) }}
        </p>
        <div class="flex items-center gap-1">
          <button
            class="rounded-lg p-2 text-sm text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40"
            :disabled="currentPage === 1"
            @click="goToPage(currentPage - 1)"
          >
            <i class="pi pi-chevron-left text-xs" :class="{ 'pi-chevron-right': isRtl }"></i>
          </button>
          <template v-for="page in totalPages" :key="page">
            <button
              v-if="page <= 5 || page === totalPages || Math.abs(page - currentPage) <= 1"
              class="min-w-[2rem] rounded-lg px-2 py-1 text-sm transition-colors"
              :class="page === currentPage ? 'bg-primary text-white' : 'text-tertiary hover:bg-surface-ground'"
              @click="goToPage(page)"
            >
              {{ page }}
            </button>
            <span
              v-else-if="page === 6 || page === totalPages - 1"
              class="px-1 text-tertiary"
            >
              ...
            </span>
          </template>
          <button
            class="rounded-lg p-2 text-sm text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40"
            :disabled="currentPage === totalPages"
            @click="goToPage(currentPage + 1)"
          >
            <i class="pi pi-chevron-right text-xs" :class="{ 'pi-chevron-left': isRtl }"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================ -->
    <!--  Create / Edit Committee Dialog                               -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showCreateDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showCreateDialog = false"
      >
        <div class="mx-4 w-full max-w-lg rounded-xl bg-white p-6 shadow-xl max-h-[90vh] overflow-y-auto">
          <div class="mb-6 flex items-center justify-between">
            <h2 class="text-lg font-bold text-secondary">
              {{ isEditMode ? t('committees.dialog.editTitle') : t('committees.dialog.createTemporaryTitle') }}
            </h2>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showCreateDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>

          <form class="space-y-4" @submit.prevent="handleCreateCommittee">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.nameAr') }}
              </label>
              <input
                v-model="formData.nameAr"
                type="text"
                required
                dir="rtl"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.nameEn') }}
              </label>
              <input
                v-model="formData.nameEn"
                type="text"
                required
                dir="ltr"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.type') }}
              </label>
              <select
                v-model="formData.type"
                :disabled="isEditMode"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground disabled:text-tertiary"
              >
                <option
                  v-for="opt in typeOptions"
                  :key="opt.value"
                  :value="opt.value"
                >
                  {{ opt.label }}
                </option>
              </select>
            </div>

            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.competitionId') }}
              </label>
              <input
                v-model="formData.competitionId"
                type="text"
                :disabled="isEditMode"
                :placeholder="t('committees.form.competitionIdPlaceholder')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground"
              />
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('committees.form.activeFromPhase') }}
                </label>
                <select
                  v-model="formData.activeFromPhase"
                  :disabled="isEditMode"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground"
                >
                  <option
                    v-for="opt in phaseOptions"
                    :key="opt.value"
                    :value="opt.value"
                  >
                    {{ opt.label }}
                  </option>
                </select>
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('committees.form.activeToPhase') }}
                </label>
                <select
                  v-model="formData.activeToPhase"
                  :disabled="isEditMode"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground"
                >
                  <option
                    v-for="opt in phaseOptions"
                    :key="opt.value"
                    :value="opt.value"
                  >
                    {{ opt.label }}
                  </option>
                </select>
              </div>
            </div>

            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.description') }}
              </label>
              <textarea
                v-model="formData.description"
                rows="3"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('committees.form.startDate') }}
                </label>
                <input
                  v-model="formData.startDate"
                  type="date"
                  required
                  :disabled="isEditMode"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground"
                />
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('committees.form.endDate') }}
                </label>
                <input
                  v-model="formData.endDate"
                  type="date"
                  required
                  :disabled="isEditMode"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-ground"
                />
              </div>
            </div>

            <div class="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-medium text-secondary hover:bg-surface-ground"
                @click="showCreateDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="isSubmitting"
                class="rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-dark disabled:opacity-50"
              >
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
      <div
        v-if="showDetailDialog && selectedCommittee"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showDetailDialog = false"
      >
        <div class="mx-4 w-full max-w-2xl rounded-xl bg-white shadow-xl max-h-[90vh] overflow-y-auto">
          <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-dim bg-white px-6 py-4">
            <h2 class="text-lg font-bold text-secondary">
              {{ getCommitteeName(selectedCommittee) }}
            </h2>
            <div class="flex items-center gap-2">
              <button
                class="rounded-lg border border-surface-dim px-3 py-1.5 text-xs font-medium text-secondary hover:bg-surface-ground"
                @click="openEditDialog(selectedCommittee!)"
              >
                <i class="pi pi-pencil me-1 text-xs"></i>
                {{ t('common.edit') }}
              </button>
              <button
                class="rounded-lg border border-warning/30 px-3 py-1.5 text-xs font-medium text-warning hover:bg-warning/5"
                @click="openStatusDialog"
              >
                <i class="pi pi-cog me-1 text-xs"></i>
                {{ t('committees.actions.changeStatus') }}
              </button>
              <button
                class="rounded-lg p-1.5 text-tertiary hover:bg-surface-ground"
                @click="showDetailDialog = false"
              >
                <i class="pi pi-times"></i>
              </button>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4 p-6 sm:grid-cols-3">
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.type') }}</p>
              <p class="mt-1 text-sm font-medium text-secondary">
                {{ getTypeName(selectedCommittee.type) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.status') }}</p>
              <span
                :class="[
                  getStatusBadge(selectedCommittee.status).bgClass,
                  getStatusBadge(selectedCommittee.status).textClass,
                ]"
                class="mt-1 inline-block rounded-full px-2.5 py-0.5 text-xs font-medium"
              >
                {{ getStatusBadge(selectedCommittee.status).label }}
              </span>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.detail.phaseRange') }}</p>
              <p class="mt-1 text-sm text-secondary">
                {{ getPhaseName(selectedCommittee.activeFromPhase) }} — {{ getPhaseName(selectedCommittee.activeToPhase) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.startDate') }}</p>
              <p class="mt-1 text-sm text-secondary">
                {{ formatDate(selectedCommittee.startDate) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.detail.endDate') }}</p>
              <p class="mt-1 text-sm text-secondary">
                {{ formatDate(selectedCommittee.endDate) }}
              </p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.form.description') }}</p>
              <p class="mt-1 text-sm text-secondary">
                {{ selectedCommittee.description || t('committees.detail.noDescription') }}
              </p>
            </div>
          </div>

          <!-- Members Section -->
          <div class="border-t border-surface-dim px-6 py-4">
            <div class="mb-4 flex items-center justify-between">
              <h3 class="text-sm font-bold text-secondary">
                {{ t('committees.detail.members') }}
                <span class="ms-1 text-xs text-tertiary">
                  ({{ selectedCommittee.members.filter(m => m.isActive).length }})
                </span>
              </h3>
              <button
                class="flex items-center gap-1 rounded-lg bg-primary/10 px-3 py-1.5 text-xs font-medium text-primary hover:bg-primary/20"
                @click="showAddMemberDialog = true"
              >
                <i class="pi pi-user-plus text-xs"></i>
                {{ t('committees.actions.addMember') }}
              </button>
            </div>

            <div v-if="selectedCommittee.members.length > 0" class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead>
                  <tr class="border-b border-surface-dim bg-surface-ground">
                    <th class="px-3 py-2 text-start text-xs font-semibold text-tertiary">
                      {{ t('committees.memberTable.name') }}
                    </th>
                    <th class="px-3 py-2 text-center text-xs font-semibold text-tertiary">
                      {{ t('committees.memberTable.role') }}
                    </th>
                    <th class="px-3 py-2 text-center text-xs font-semibold text-tertiary">
                      {{ t('committees.memberTable.status') }}
                    </th>
                    <th class="px-3 py-2 text-center text-xs font-semibold text-tertiary">
                      {{ t('committees.memberTable.actions') }}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="member in selectedCommittee.members"
                    :key="member.id"
                    class="border-b border-surface-dim last:border-b-0"
                  >
                    <td class="px-3 py-2 text-secondary">{{ member.userFullName }}</td>
                    <td class="px-3 py-2 text-center text-tertiary">
                      {{ getRoleName(member.role) }}
                    </td>
                    <td class="px-3 py-2 text-center">
                      <span
                        :class="member.isActive ? 'bg-success/10 text-success' : 'bg-surface-muted text-tertiary'"
                        class="inline-block rounded-full px-2 py-0.5 text-xs font-medium"
                      >
                        {{ member.isActive ? t('committees.memberStatus.active') : t('committees.memberStatus.removed') }}
                      </span>
                    </td>
                    <td class="px-3 py-2 text-center">
                      <button
                        v-if="member.isActive"
                        class="rounded-lg p-1 text-danger/70 hover:bg-danger/5 hover:text-danger"
                        :title="t('committees.actions.removeMember')"
                        @click="handleRemoveMember(member)"
                      >
                        <i class="pi pi-user-minus text-xs"></i>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <p v-else class="py-4 text-center text-sm text-tertiary">
              {{ t('committees.detail.noMembers') }}
            </p>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!--  Change Status Dialog                                         -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showStatusDialog"
        class="fixed inset-0 z-[60] flex items-center justify-center bg-black/40"
        @click.self="showStatusDialog = false"
      >
        <div class="mx-4 w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
          <h2 class="mb-4 text-lg font-bold text-secondary">
            {{ t('committees.dialog.changeStatusTitle') }}
          </h2>
          <form class="space-y-4" @submit.prevent="handleChangeStatus">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.newStatus') }}
              </label>
              <select
                v-model="statusChangeData.newStatus"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option
                  v-for="opt in statusChangeOptions"
                  :key="opt.value"
                  :value="opt.value"
                >
                  {{ opt.label }}
                </option>
              </select>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.reason') }}
              </label>
              <textarea
                v-model="statusChangeData.reason"
                rows="3"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              ></textarea>
            </div>
            <div class="flex items-center justify-end gap-3">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-medium text-secondary hover:bg-surface-ground"
                @click="showStatusDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="isSubmitting"
                class="rounded-lg bg-warning px-4 py-2.5 text-sm font-medium text-white hover:bg-warning/90 disabled:opacity-50"
              >
                {{ t('common.confirm') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!--  Add Member Dialog                                            -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showAddMemberDialog"
        class="fixed inset-0 z-[60] flex items-center justify-center bg-black/40"
        @click.self="showAddMemberDialog = false"
      >
        <div class="mx-4 w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
          <h2 class="mb-4 text-lg font-bold text-secondary">
            {{ t('committees.dialog.addMemberTitle') }}
          </h2>
          <form class="space-y-4" @submit.prevent="handleAddMember">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.memberName') }}
              </label>
              <input
                v-model="memberFormData.userFullName"
                type="text"
                required
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.memberId') }}
              </label>
              <input
                v-model="memberFormData.userId"
                type="text"
                required
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">
                {{ t('committees.form.memberRole') }}
              </label>
              <select
                v-model="memberFormData.role"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option
                  v-for="opt in memberRoleOptions"
                  :key="opt.value"
                  :value="opt.value"
                >
                  {{ opt.label }}
                </option>
              </select>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('committees.form.activeFromPhase') }}
                </label>
                <select
                  v-model="memberFormData.activeFromPhase"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                >
                  <option
                    v-for="opt in phaseOptions"
                    :key="opt.value"
                    :value="opt.value"
                  >
                    {{ opt.label }}
                  </option>
                </select>
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('committees.form.activeToPhase') }}
                </label>
                <select
                  v-model="memberFormData.activeToPhase"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                >
                  <option
                    v-for="opt in phaseOptions"
                    :key="opt.value"
                    :value="opt.value"
                  >
                    {{ opt.label }}
                  </option>
                </select>
              </div>
            </div>
            <div class="flex items-center justify-end gap-3">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-medium text-secondary hover:bg-surface-ground"
                @click="showAddMemberDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="isSubmitting"
                class="rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-dark disabled:opacity-50"
              >
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner me-1 text-xs"></i>
                {{ t('committees.actions.addMember') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>
