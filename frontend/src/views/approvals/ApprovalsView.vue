<script setup lang="ts">
/**
 * ApprovalsView — Unified Task Center / Approvals Page.
 *
 * TASK-902: Displays pending tasks and approval workflows for the current user.
 * Data is fetched dynamically from the API (no mock data).
 *
 * Features:
 * - Paginated pending tasks list
 * - Filter by type and priority
 * - SLA countdown with visual indicators
 * - Direct action links
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed, watch, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { fetchPendingTasks } from '@/services/approvalService'
import type { PendingTask } from '@/types/committee'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const router = useRouter()
const { formatDateTime } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const tasks = ref<PendingTask[]>([])
const isLoading = ref(false)
const error = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const typeFilter = ref('')
const priorityFilter = ref('')

/* Auto-refresh timer */
let refreshTimer: ReturnType<typeof setInterval> | null = null
const AUTO_REFRESH_INTERVAL = 30_000

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
const isRtl = computed(() => locale.value === 'ar')

const typeOptions = computed(() => [
  { value: '', label: t('approvals.types.all') },
  { value: 'rfp_approval', label: t('approvals.types.rfpApproval') },
  { value: 'evaluation_review', label: t('approvals.types.evaluationReview') },
  { value: 'committee_assignment', label: t('approvals.types.committeeAssignment') },
  { value: 'contract_approval', label: t('approvals.types.contractApproval') },
])

const priorityOptions = computed(() => [
  { value: '', label: t('approvals.priorities.all') },
  { value: 'critical', label: t('approvals.priorities.critical') },
  { value: 'high', label: t('approvals.priorities.high') },
  { value: 'medium', label: t('approvals.priorities.medium') },
  { value: 'low', label: t('approvals.priorities.low') },
])

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getPriorityBadge(priority: string) {
  const config: Record<string, { label: string; bgClass: string; textClass: string }> = {
    critical: {
      label: t('approvals.priorities.critical'),
      bgClass: 'bg-danger/10',
      textClass: 'text-danger',
    },
    high: {
      label: t('approvals.priorities.high'),
      bgClass: 'bg-warning/10',
      textClass: 'text-warning',
    },
    medium: {
      label: t('approvals.priorities.medium'),
      bgClass: 'bg-info/10',
      textClass: 'text-info',
    },
    low: {
      label: t('approvals.priorities.low'),
      bgClass: 'bg-surface-muted',
      textClass: 'text-tertiary',
    },
  }
  return config[priority] || config.medium
}

function getSlaStatusBadge(slaStatus: string) {
  const config: Record<string, { label: string; bgClass: string; textClass: string }> = {
    on_track: {
      label: t('approvals.sla.onTrack'),
      bgClass: 'bg-success/10',
      textClass: 'text-success',
    },
    at_risk: {
      label: t('approvals.sla.atRisk'),
      bgClass: 'bg-warning/10',
      textClass: 'text-warning',
    },
    overdue: {
      label: t('approvals.sla.overdue'),
      bgClass: 'bg-danger/10',
      textClass: 'text-danger',
    },
  }
  return config[slaStatus] || config.on_track
}

function getTaskTitle(task: PendingTask): string {
  return locale.value === 'ar' ? task.titleAr : task.titleEn
}

function getCompetitionTitle(task: PendingTask): string {
  return locale.value === 'ar' ? task.competitionTitleAr : task.competitionTitleEn
}

function formatRemainingTime(seconds: number): string {
  if (seconds <= 0) return t('approvals.sla.expired')
  const days = Math.floor(seconds / 86400)
  const hours = Math.floor((seconds % 86400) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)

  if (days > 0) {
    return t('approvals.sla.remaining', { days, hours })
  }
  if (hours > 0) {
    return t('approvals.sla.remainingHours', { hours, minutes })
  }
  return t('approvals.sla.remainingMinutes', { minutes })
}

function getTypeName(type: string): string {
  const map: Record<string, string> = {
    rfp_approval: t('approvals.types.rfpApproval'),
    evaluation_review: t('approvals.types.evaluationReview'),
    committee_assignment: t('approvals.types.committeeAssignment'),
    contract_approval: t('approvals.types.contractApproval'),
  }
  return map[type] || type
}

function navigateToAction(task: PendingTask) {
  if (task.actionUrl) {
    router.push(task.actionUrl)
  }
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadTasks() {
  isLoading.value = true
  error.value = ''
  try {
    const result = await fetchPendingTasks({
      page: currentPage.value,
      pageSize: pageSize.value,
      type: typeFilter.value || undefined,
      priority: priorityFilter.value || undefined,
    })
    tasks.value = result.items
    totalCount.value = result.totalCount
  } catch (err) {
    /* Graceful degradation */ console.warn('[Approvals] API unavailable:', err)
    tasks.value = []
  } finally {
    isLoading.value = false
  }
}

async function refreshTasks() {
  try {
    const result = await fetchPendingTasks({
      page: currentPage.value,
      pageSize: pageSize.value,
      type: typeFilter.value || undefined,
      priority: priorityFilter.value || undefined,
    })
    tasks.value = result.items
    totalCount.value = result.totalCount
  } catch {
    /* Silent refresh failure */
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
watch([typeFilter, priorityFilter], () => {
  currentPage.value = 1
  loadTasks()
})

watch(currentPage, () => {
  loadTasks()
})

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  loadTasks()
  refreshTimer = setInterval(refreshTasks, AUTO_REFRESH_INTERVAL)
})

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('approvals.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('approvals.subtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <span class="rounded-lg bg-primary/10 px-3 py-1.5 text-sm font-medium text-primary">
          {{ t('approvals.pendingCount', { count: totalCount }) }}
        </span>
        <button
          class="flex items-center gap-1.5 rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm font-medium text-secondary transition-all hover:border-primary/30 hover:shadow-sm"
          @click="loadTasks"
        >
          <i class="pi pi-refresh text-xs"></i>
          {{ t('common.retry') }}
        </button>
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
      <select
        v-model="typeFilter"
        class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option
          v-for="option in typeOptions"
          :key="option.value"
          :value="option.value"
        >
          {{ option.label }}
        </option>
      </select>

      <select
        v-model="priorityFilter"
        class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option
          v-for="option in priorityOptions"
          :key="option.value"
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
      v-else-if="tasks.length === 0"
      class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16"
    >
      <i class="pi pi-check-circle text-5xl text-success/40"></i>
      <p class="mt-4 text-sm font-medium text-secondary">{{ t('approvals.emptyTitle') }}</p>
      <p class="mt-1 text-xs text-tertiary">{{ t('approvals.emptySubtitle') }}</p>
    </div>

    <!-- Tasks List -->
    <div v-else class="space-y-3">
      <div
        v-for="task in tasks"
        :key="task.id"
        class="group rounded-lg border border-surface-dim bg-white p-4 transition-all hover:border-primary/20 hover:shadow-sm"
      >
        <div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <!-- Left: Task Info -->
          <div class="flex-1 space-y-2">
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="text-sm font-semibold text-secondary">
                {{ getTaskTitle(task) }}
              </h3>
              <span
                :class="[
                  getPriorityBadge(task.priority).bgClass,
                  getPriorityBadge(task.priority).textClass,
                ]"
                class="inline-block rounded-full px-2 py-0.5 text-[10px] font-medium"
              >
                {{ getPriorityBadge(task.priority).label }}
              </span>
              <span
                :class="[
                  getSlaStatusBadge(task.slaStatus).bgClass,
                  getSlaStatusBadge(task.slaStatus).textClass,
                ]"
                class="inline-block rounded-full px-2 py-0.5 text-[10px] font-medium"
              >
                {{ getSlaStatusBadge(task.slaStatus).label }}
              </span>
            </div>

            <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-tertiary">
              <span class="flex items-center gap-1">
                <i class="pi pi-folder text-[10px]"></i>
                {{ getCompetitionTitle(task) }}
              </span>
              <span class="flex items-center gap-1">
                <i class="pi pi-hashtag text-[10px]"></i>
                {{ task.competitionReferenceNumber }}
              </span>
              <span class="flex items-center gap-1">
                <i class="pi pi-tag text-[10px]"></i>
                {{ getTypeName(task.type) }}
              </span>
            </div>

            <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-tertiary">
              <span class="flex items-center gap-1">
                <i class="pi pi-calendar text-[10px]"></i>
                {{ t('approvals.assignedAt') }}: {{ formatDateTime(task.assignedAt) }}
              </span>
              <span class="flex items-center gap-1">
                <i class="pi pi-clock text-[10px]"></i>
                {{ t('approvals.deadline') }}: {{ formatDateTime(task.slaDeadline) }}
              </span>
              <span
                class="flex items-center gap-1 font-medium"
                :class="task.remainingTimeSeconds <= 0 ? 'text-danger' : task.remainingTimeSeconds < 86400 ? 'text-warning' : 'text-success'"
              >
                <i class="pi pi-stopwatch text-[10px]"></i>
                {{ formatRemainingTime(task.remainingTimeSeconds) }}
              </span>
            </div>
          </div>

          <!-- Right: Action Button -->
          <div class="flex items-center gap-2">
            <span class="text-xs text-tertiary">
              {{ task.actionRequired }}
            </span>
            <button
              class="flex items-center gap-1.5 rounded-lg bg-primary px-4 py-2 text-xs font-medium text-white transition-all hover:bg-primary-dark"
              @click="navigateToAction(task)"
            >
              <i class="pi pi-arrow-right text-[10px]" :class="{ 'pi-arrow-left': isRtl }"></i>
              {{ t('approvals.actions.review') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div
      v-if="totalPages > 1"
      class="flex items-center justify-between rounded-lg border border-surface-dim bg-white px-4 py-3"
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
</template>
