<script setup lang="ts">
/**
 * CommitteesPermanentView — Permanent Committees Management Page.
 *
 * Enhanced with:
 * - Statistics dashboard cards
 * - AI-powered committee analysis panel
 * - Enhanced table with workload/days-remaining indicators
 * - Competition name display
 * - Improved member management
 * - Full RTL/LTR support with Tailwind logical properties
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
  fetchCommitteeStatistics,
  fetchCommitteeAiAnalysis,
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
} from '@/types/committee'
import {
  CommitteeType,
  CommitteeStatus,
  CommitteeMemberRole,
} from '@/types/committee'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
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

/* Create / Edit form */
const formData = ref<CreateCommitteeRequest>({
  nameAr: '',
  nameEn: '',
  type: CommitteeType.TechnicalEvaluation,
  isPermanent: true,
  description: '',
  startDate: '',
  endDate: '',
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
  { value: CommitteeType.TechnicalEvaluation, label: t('committees.types.technicalEvaluation') },
  { value: CommitteeType.FinancialEvaluation, label: t('committees.types.financialEvaluation') },
  { value: CommitteeType.OtherPermanent, label: t('committees.types.otherPermanent') },
])

const typeFilterOptions = computed(() => [
  { value: '', label: t('committees.filters.allTypes') },
  ...typeOptions.value,
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

function getDaysRemainingClass(days: number): string {
  if (days <= 0) return 'text-danger'
  if (days <= 14) return 'text-warning'
  return 'text-success'
}

function getWorkloadClass(score: number): string {
  if (score >= 80) return 'bg-danger'
  if (score >= 50) return 'bg-warning'
  return 'bg-success'
}

function getHealthScoreClass(score: number): string {
  if (score >= 80) return 'text-success'
  if (score >= 50) return 'text-warning'
  return 'text-danger'
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
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
      type: typeFilter.value !== '' ? typeFilter.value : undefined,
      status: statusFilter.value !== '' ? statusFilter.value : undefined,
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
      await createCommittee({ ...formData.value, isPermanent: true })
    }
    showCreateDialog.value = false
    resetForm()
    await loadCommittees()
    await loadStatistics()
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
    await loadStatistics()
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
    memberFormData.value = { userId: '', userFullName: '', role: CommitteeMemberRole.Member }
    await loadCommitteeDetail(selectedCommittee.value.id)
    await loadStatistics()
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
    description: committee.description || '',
    startDate: committee.startDate,
    endDate: committee.endDate,
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
    type: CommitteeType.TechnicalEvaluation,
    isPermanent: true,
    description: '',
    startDate: '',
    endDate: '',
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

watch(typeFilter, () => {
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
  loadStatistics()
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
        class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark"
        @click="openCreateDialog"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ t('committees.actions.create') }}
      </button>
    </div>

    <!-- ============================================================ -->
    <!--  Statistics Dashboard                                         -->
    <!-- ============================================================ -->
    <div v-if="isLoadingStats" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <div v-for="i in 5" :key="i" class="animate-pulse rounded-lg border border-surface-dim bg-white p-4">
        <div class="h-3 w-20 rounded bg-surface-dim"></div>
        <div class="mt-3 h-7 w-12 rounded bg-surface-dim"></div>
      </div>
    </div>
    <div v-else-if="statistics" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <!-- Total -->
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10">
            <i class="pi pi-users text-sm text-primary"></i>
          </div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.total') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-secondary">{{ statistics.totalCommittees }}</p>
      </div>
      <!-- Active -->
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-success/10">
            <i class="pi pi-check-circle text-sm text-success"></i>
          </div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.active') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-success">{{ statistics.activeCommittees }}</p>
      </div>
      <!-- Expiring Soon -->
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-warning/10">
            <i class="pi pi-clock text-sm text-warning"></i>
          </div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.expiringSoon') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-warning">{{ statistics.committeesExpiringSoon }}</p>
      </div>
      <!-- No Chair -->
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-danger/10">
            <i class="pi pi-exclamation-triangle text-sm text-danger"></i>
          </div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.noChair') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-danger">{{ statistics.committeesWithNoChair }}</p>
      </div>
      <!-- Avg Members -->
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-info/10">
            <i class="pi pi-chart-bar text-sm text-info"></i>
          </div>
          <p class="text-xs text-tertiary">{{ t('committees.stats.avgMembers') }}</p>
        </div>
        <p class="mt-2 text-2xl font-bold text-secondary">{{ statistics.averageMembers }}</p>
      </div>
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
      <!-- Search -->
      <div class="relative flex-1">
        <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary"></i>
        <input
          v-model="searchQuery"
          type="text"
          :placeholder="t('committees.searchPlaceholder')"
          class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-10 pe-4 text-sm text-secondary placeholder:text-tertiary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>

      <!-- Type Filter -->
      <select
        v-model="typeFilter"
        class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option
          v-for="option in typeFilterOptions"
          :key="String(option.value)"
          :value="option.value"
        >
          {{ option.label }}
        </option>
      </select>

      <!-- Status Filter -->
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
      <p class="mt-4 text-sm text-tertiary">{{ t('committees.emptyState') }}</p>
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
              <th class="px-4 py-3 text-center font-semibold text-secondary">
                {{ t('committees.table.daysRemaining') }}
              </th>
              <th class="px-4 py-3 text-center font-semibold text-secondary">
                {{ t('committees.table.workload') }}
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
              <td class="px-4 py-3">
                <p class="font-medium text-secondary">{{ getCommitteeName(committee) }}</p>
                <p v-if="committee.competitionNameAr || committee.competitionNameEn" class="mt-0.5 text-xs text-tertiary">
                  {{ locale === 'ar' ? committee.competitionNameAr : committee.competitionNameEn }}
                </p>
              </td>
              <td class="px-4 py-3 text-tertiary">
                {{ getTypeName(committee.type) }}
              </td>
              <td class="px-4 py-3 text-center text-secondary">
                <span class="inline-flex items-center gap-1">
                  <i class="pi pi-user text-xs text-tertiary"></i>
                  {{ committee.activeMemberCount }}
                </span>
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
              <td class="px-4 py-3 text-center">
                <span
                  :class="getDaysRemainingClass(committee.daysRemaining)"
                  class="text-sm font-semibold"
                >
                  {{ committee.daysRemaining > 0 ? committee.daysRemaining : 0 }}
                </span>
                <span class="ms-0.5 text-xs text-tertiary">{{ t('committees.table.days') }}</span>
              </td>
              <td class="px-4 py-3 text-center">
                <div class="mx-auto w-16">
                  <div class="h-1.5 w-full overflow-hidden rounded-full bg-surface-dim">
                    <div
                      :class="getWorkloadClass(committee.workloadScore)"
                      class="h-full rounded-full transition-all"
                      :style="{ width: Math.min(committee.workloadScore, 100) + '%' }"
                    ></div>
                  </div>
                  <p class="mt-0.5 text-xs text-tertiary">{{ committee.workloadScore }}%</p>
                </div>
              </td>
              <td class="px-4 py-3 text-center">
                <div class="flex items-center justify-center gap-1">
                  <button
                    class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary"
                    :title="t('common.view')"
                    @click="loadCommitteeDetail(committee.id)"
                  >
                    <i class="pi pi-eye text-sm"></i>
                  </button>
                  <button
                    class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-primary/10 hover:text-primary"
                    :title="t('committees.actions.aiAnalysis')"
                    @click="loadCommitteeDetail(committee.id); loadAiAnalysis(committee.id)"
                  >
                    <i class="pi pi-sparkles text-sm"></i>
                  </button>
                </div>
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
            <i class="pi text-xs" :class="isRtl ? 'pi-chevron-right' : 'pi-chevron-left'"></i>
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
            <i class="pi text-xs" :class="isRtl ? 'pi-chevron-left' : 'pi-chevron-right'"></i>
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
        <div class="mx-4 w-full max-w-lg rounded-xl bg-white p-6 shadow-xl">
          <div class="mb-6 flex items-center justify-between">
            <h2 class="text-lg font-bold text-secondary">
              {{ isEditMode ? t('committees.dialog.editTitle') : t('committees.dialog.createTitle') }}
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
        <div class="mx-4 w-full max-w-3xl rounded-xl bg-white shadow-xl max-h-[90vh] overflow-y-auto">
          <!-- Header -->
          <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-dim bg-white px-6 py-4">
            <div>
              <h2 class="text-lg font-bold text-secondary">
                {{ getCommitteeName(selectedCommittee) }}
              </h2>
              <p v-if="selectedCommittee.competitionNameAr || selectedCommittee.competitionNameEn" class="mt-0.5 text-xs text-tertiary">
                {{ locale === 'ar' ? selectedCommittee.competitionNameAr : selectedCommittee.competitionNameEn }}
              </p>
            </div>
            <div class="flex items-center gap-2">
              <button
                class="rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary hover:bg-primary/5"
                @click="loadAiAnalysis(selectedCommittee!.id)"
              >
                <i class="pi pi-sparkles me-1 text-xs"></i>
                {{ t('committees.actions.aiAnalysis') }}
              </button>
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

          <!-- Info Grid -->
          <div class="grid grid-cols-2 gap-4 p-6 sm:grid-cols-4">
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
              <p class="text-xs text-tertiary">{{ t('committees.table.daysRemaining') }}</p>
              <p class="mt-1 text-sm font-semibold" :class="getDaysRemainingClass(selectedCommittee.daysRemaining)">
                {{ selectedCommittee.daysRemaining > 0 ? selectedCommittee.daysRemaining : 0 }} {{ t('committees.table.days') }}
              </p>
            </div>
            <div>
              <p class="text-xs text-tertiary">{{ t('committees.table.workload') }}</p>
              <div class="mt-1">
                <div class="h-2 w-full overflow-hidden rounded-full bg-surface-dim">
                  <div
                    :class="getWorkloadClass(selectedCommittee.workloadScore)"
                    class="h-full rounded-full transition-all"
                    :style="{ width: Math.min(selectedCommittee.workloadScore, 100) + '%' }"
                  ></div>
                </div>
                <p class="mt-0.5 text-xs text-tertiary">{{ selectedCommittee.workloadScore }}%</p>
              </div>
            </div>
            <div class="col-span-2">
              <p class="text-xs text-tertiary">{{ t('committees.form.description') }}</p>
              <p class="mt-1 text-sm text-secondary">
                {{ selectedCommittee.description || t('committees.detail.noDescription') }}
              </p>
            </div>
          </div>

          <!-- ============================================================ -->
          <!--  AI Analysis Panel                                           -->
          <!-- ============================================================ -->
          <div v-if="showAiPanel" class="border-t border-surface-dim px-6 py-4">
            <div class="mb-4 flex items-center justify-between">
              <h3 class="flex items-center gap-2 text-sm font-bold text-secondary">
                <i class="pi pi-sparkles text-primary"></i>
                {{ t('committees.ai.title') }}
              </h3>
              <button
                class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
                @click="showAiPanel = false"
              >
                <i class="pi pi-times text-xs"></i>
              </button>
            </div>

            <!-- Loading -->
            <div v-if="isLoadingAi" class="flex items-center justify-center py-8">
              <div class="text-center">
                <i class="pi pi-spin pi-spinner text-2xl text-primary"></i>
                <p class="mt-2 text-xs text-tertiary">{{ t('committees.ai.analyzing') }}</p>
              </div>
            </div>

            <!-- AI Results -->
            <div v-else-if="aiAnalysis" class="space-y-4">
              <!-- Health Score -->
              <div class="flex items-center gap-4 rounded-lg bg-surface-ground p-4">
                <div class="relative flex h-16 w-16 items-center justify-center">
                  <svg class="h-16 w-16 -rotate-90" viewBox="0 0 36 36">
                    <path
                      class="text-surface-dim"
                      stroke="currentColor"
                      stroke-width="3"
                      fill="none"
                      d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                    />
                    <path
                      :class="getHealthScoreClass(aiAnalysis.insight.healthScore)"
                      stroke="currentColor"
                      stroke-width="3"
                      fill="none"
                      :stroke-dasharray="`${aiAnalysis.insight.healthScore}, 100`"
                      d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                    />
                  </svg>
                  <span class="absolute text-sm font-bold" :class="getHealthScoreClass(aiAnalysis.insight.healthScore)">
                    {{ aiAnalysis.insight.healthScore }}%
                  </span>
                </div>
                <div class="flex-1">
                  <p class="text-sm font-semibold text-secondary">{{ aiAnalysis.insight.healthLabel }}</p>
                  <p class="mt-1 text-xs text-tertiary leading-relaxed">{{ aiAnalysis.insight.summary }}</p>
                </div>
              </div>

              <!-- Recommendations -->
              <div v-if="aiAnalysis.insight.recommendations.length > 0">
                <h4 class="mb-2 text-xs font-semibold text-secondary">{{ t('committees.ai.recommendations') }}</h4>
                <ul class="space-y-1.5">
                  <li
                    v-for="(rec, idx) in aiAnalysis.insight.recommendations"
                    :key="idx"
                    class="flex items-start gap-2 rounded-lg bg-primary/5 p-2.5 text-xs text-secondary"
                  >
                    <i class="pi pi-check-circle mt-0.5 text-xs text-primary"></i>
                    <span>{{ rec }}</span>
                  </li>
                </ul>
              </div>

              <!-- Risks -->
              <div v-if="aiAnalysis.insight.risks.length > 0">
                <h4 class="mb-2 text-xs font-semibold text-secondary">{{ t('committees.ai.risks') }}</h4>
                <ul class="space-y-1.5">
                  <li
                    v-for="(risk, idx) in aiAnalysis.insight.risks"
                    :key="idx"
                    class="flex items-start gap-2 rounded-lg bg-danger/5 p-2.5 text-xs text-secondary"
                  >
                    <i class="pi pi-exclamation-triangle mt-0.5 text-xs text-danger"></i>
                    <span>{{ risk }}</span>
                  </li>
                </ul>
              </div>

              <!-- Detailed Recommendations -->
              <div v-if="aiAnalysis.recommendations.length > 0">
                <h4 class="mb-2 text-xs font-semibold text-secondary">{{ t('committees.ai.detailedRecommendations') }}</h4>
                <div class="space-y-2">
                  <div
                    v-for="(rec, idx) in aiAnalysis.recommendations"
                    :key="idx"
                    class="rounded-lg border border-surface-dim p-3"
                  >
                    <div class="flex items-center justify-between">
                      <p class="text-xs font-semibold text-secondary">{{ rec.title }}</p>
                      <div class="flex items-center gap-2">
                        <span
                          class="rounded-full px-2 py-0.5 text-[10px] font-medium"
                          :class="rec.impact === 'High' ? 'bg-danger/10 text-danger' : rec.impact === 'Medium' ? 'bg-warning/10 text-warning' : 'bg-info/10 text-info'"
                        >
                          {{ rec.impact }}
                        </span>
                        <span class="text-[10px] text-tertiary">{{ rec.confidence }}%</span>
                      </div>
                    </div>
                    <p class="mt-1 text-xs text-tertiary leading-relaxed">{{ rec.description }}</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- No AI Data -->
            <div v-else class="py-8 text-center">
              <i class="pi pi-info-circle text-2xl text-tertiary"></i>
              <p class="mt-2 text-xs text-tertiary">{{ t('committees.ai.noData') }}</p>
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

            <!-- Members Table -->
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
                      {{ t('committees.memberTable.assignedAt') }}
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
                    <td class="px-3 py-2">
                      <div class="flex items-center gap-2">
                        <div class="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                          {{ member.userFullName.charAt(0) }}
                        </div>
                        <span class="text-secondary">{{ member.userFullName }}</span>
                      </div>
                    </td>
                    <td class="px-3 py-2 text-center">
                      <span
                        class="inline-block rounded-full px-2 py-0.5 text-xs font-medium"
                        :class="member.role === CommitteeMemberRole.Chair ? 'bg-primary/10 text-primary' : 'bg-surface-muted text-tertiary'"
                      >
                        {{ getRoleName(member.role) }}
                      </span>
                    </td>
                    <td class="px-3 py-2 text-center">
                      <span
                        :class="member.isActive ? 'bg-success/10 text-success' : 'bg-surface-muted text-tertiary'"
                        class="inline-block rounded-full px-2 py-0.5 text-xs font-medium"
                      >
                        {{ member.isActive ? t('committees.memberStatus.active') : t('committees.memberStatus.removed') }}
                      </span>
                    </td>
                    <td class="px-3 py-2 text-center text-xs text-tertiary">
                      {{ formatDate(member.assignedAt) }}
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
