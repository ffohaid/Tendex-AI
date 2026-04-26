<script setup lang="ts">
/**
 * TaskCenterView - Unified Task Center (مركز المهام الموحد)
 *
 * A single unified inbox for all human interactions:
 * - Approval tasks (competitions & inquiries)
 * - Evaluation tasks (technical & financial)
 * - Inquiry response tasks
 * - Review/committee tasks
 *
 * Features:
 * - Statistics dashboard header with KPI cards
 * - Filter by type, priority, SLA status, source
 * - Sort by priority, deadline, type, date, SLA
 * - SLA countdown with color-coded indicators
 * - AI-powered recommendations for each task
 * - Quick actions (navigate to task)
 * - Bulk selection
 * - Search across all fields
 * - RTL/LTR support
 *
 * All data fetched dynamically from APIs — NO mock data.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { resolveTaskActionUrl } from '@/utils/taskNavigation'
import { useFormatters } from '@/composables/useFormatters'
import {
  fetchTasks,
  type TaskItem,
  type TaskCenterStats,
  type TaskCenterPagedResult,
  type TaskCenterFilters,
} from '@/services/taskCenterService'

const { t } = useI18n()
const router = useRouter()
const { formatDateTime, formatRemainingTime, formatNumber, formatPercentage, isArabic } = useFormatters()

/* ── State ── */
const isLoading = ref(false)
const tasks = ref<TaskItem[]>([])
const totalCount = ref(0)
const statistics = ref<TaskCenterStats | null>(null)
const currentPage = ref(1)
const pageSize = ref(20)
const errorMessage = ref('')

/* Filters */
const activeTypeFilter = ref<string>('all')
const activePriorityFilter = ref<string>('')
const activeSlaFilter = ref<string>('')
const activeSourceFilter = ref<string>('')
const sortBy = ref<string>('priority')
const searchQuery = ref('')
const selectedTasks = ref<string[]>([])
const showAiPanel = ref(false)
const selectedTaskForAi = ref<TaskItem | null>(null)

/* Computed */
const isAllSelected = computed(() =>
  tasks.value.length > 0 &&
  tasks.value.every(t => selectedTasks.value.includes(t.id))
)

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))

/* ── Methods ── */
async function loadTasks(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const filters: TaskCenterFilters = {
      type: activeTypeFilter.value !== 'all' ? activeTypeFilter.value : undefined,
      priority: activePriorityFilter.value || undefined,
      slaStatus: activeSlaFilter.value || undefined,
      source: activeSourceFilter.value || undefined,
      search: searchQuery.value || undefined,
      sortBy: sortBy.value,
    }
    const data: TaskCenterPagedResult = await fetchTasks(
      currentPage.value,
      pageSize.value,
      filters,
    )
    tasks.value = data.items
    totalCount.value = data.totalCount
    if (data.statistics) {
      statistics.value = data.statistics
    }
  } catch (err: any) {
    console.error('Failed to load tasks:', err)
    errorMessage.value = err?.message || 'Failed to load tasks'
  } finally {
    isLoading.value = false
  }
}

function setTypeFilter(filter: string): void {
  activeTypeFilter.value = filter
  currentPage.value = 1
  loadTasks()
}

function setPriorityFilter(priority: string): void {
  activePriorityFilter.value = activePriorityFilter.value === priority ? '' : priority
  currentPage.value = 1
  loadTasks()
}

function setSlaFilter(sla: string): void {
  activeSlaFilter.value = activeSlaFilter.value === sla ? '' : sla
  currentPage.value = 1
  loadTasks()
}

function setSourceFilter(source: string): void {
  activeSourceFilter.value = activeSourceFilter.value === source ? '' : source
  currentPage.value = 1
  loadTasks()
}

function onSortChange(): void {
  currentPage.value = 1
  loadTasks()
}

function onSearch(): void {
  currentPage.value = 1
  loadTasks()
}

function goToPage(page: number): void {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    loadTasks()
  }
}

function toggleSelectAll(): void {
  if (isAllSelected.value) {
    selectedTasks.value = []
  } else {
    selectedTasks.value = tasks.value.map(t => t.id)
  }
}

function toggleSelect(taskId: string): void {
  const idx = selectedTasks.value.indexOf(taskId)
  if (idx >= 0) {
    selectedTasks.value.splice(idx, 1)
  } else {
    selectedTasks.value.push(taskId)
  }
}

function navigateToTask(task: TaskItem): void {
  const targetUrl = resolveTaskActionUrl(task)
  if (!targetUrl) return

  router.push(targetUrl)
}

function showAiRecommendation(task: TaskItem): void {
  selectedTaskForAi.value = task
  showAiPanel.value = true
}

function closeAiPanel(): void {
  showAiPanel.value = false
  selectedTaskForAi.value = null
}

function getTaskTitle(task: TaskItem): string {
  return isArabic.value ? task.titleAr : task.titleEn
}

function getTaskDescription(task: TaskItem): string {
  return isArabic.value ? task.descriptionAr : task.descriptionEn
}

function getCompetitionTitle(task: TaskItem): string {
  return isArabic.value ? task.competitionTitleAr : task.competitionTitleEn
}

function getAiRecommendation(task: TaskItem): string {
  return isArabic.value
    ? (task.aiRecommendationAr || '')
    : (task.aiRecommendationEn || '')
}

function getTypeIcon(type: string): string {
  const icons: Record<string, string> = {
    approve_request: 'pi-check-circle',
    approve_inquiry_response: 'pi-check-circle',
    evaluate_offer: 'pi-chart-bar',
    answer_inquiry: 'pi-question-circle',
    committee_action: 'pi-users',
  }
  return icons[type] || 'pi-circle'
}

function getTypeColor(type: string): string {
  const colors: Record<string, string> = {
    approve_request: 'text-emerald-600',
    approve_inquiry_response: 'text-emerald-600',
    evaluate_offer: 'text-amber-600',
    answer_inquiry: 'text-blue-600',
    committee_action: 'text-purple-600',
  }
  return colors[type] || 'text-gray-600'
}

function getTypeBg(type: string): string {
  const bgs: Record<string, string> = {
    approve_request: 'bg-emerald-50',
    approve_inquiry_response: 'bg-emerald-50',
    evaluate_offer: 'bg-amber-50',
    answer_inquiry: 'bg-blue-50',
    committee_action: 'bg-purple-50',
  }
  return bgs[type] || 'bg-gray-50'
}

function getSourceIcon(source: string): string {
  return source === 'competition' ? 'pi-briefcase' : 'pi-question-circle'
}

function getSourceLabel(source: string): string {
  if (source === 'competition') {
    return isArabic.value ? 'منافسة' : 'Competition'
  }
  return isArabic.value ? 'استفسار' : 'Inquiry'
}

function getPriorityBadgeClass(priority: string): string {
  const classes: Record<string, string> = {
    critical: 'bg-red-100 text-red-700 border-red-200',
    high: 'bg-orange-100 text-orange-700 border-orange-200',
    medium: 'bg-blue-100 text-blue-700 border-blue-200',
    low: 'bg-gray-100 text-gray-600 border-gray-200',
  }
  return classes[priority] || 'bg-gray-100 text-gray-600 border-gray-200'
}

function getSlaClass(status: string): string {
  const classes: Record<string, string> = {
    on_track: 'text-emerald-600',
    approaching: 'text-amber-600',
    exceeded: 'text-red-600',
  }
  return classes[status] || 'text-gray-500'
}

function getSlaBgClass(status: string): string {
  const classes: Record<string, string> = {
    on_track: 'bg-emerald-50',
    approaching: 'bg-amber-50',
    exceeded: 'bg-red-50',
  }
  return classes[status] || 'bg-gray-50'
}

function getSlaIcon(status: string): string {
  const icons: Record<string, string> = {
    on_track: 'pi-check-circle',
    approaching: 'pi-exclamation-triangle',
    exceeded: 'pi-times-circle',
  }
  return icons[status] || 'pi-clock'
}

/* Debounced search */
let searchTimeout: ReturnType<typeof setTimeout> | null = null
watch(searchQuery, () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    onSearch()
  }, 400)
})

onMounted(() => {
  loadTasks()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="animate-fade-in">
      <h1 class="text-2xl font-bold text-gray-900">{{ t('taskCenter.title') }}</h1>
      <p class="mt-1 text-sm text-gray-500">{{ t('taskCenter.subtitle') }}</p>
    </div>

    <!-- Statistics Cards -->
    <div v-if="statistics" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <!-- Total Pending -->
      <div class="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50">
            <i class="pi pi-list text-blue-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-gray-900">{{ formatNumber(statistics.totalPending) }}</p>
            <p class="text-xs text-gray-500">{{ t('taskCenter.stats.totalPending') }}</p>
          </div>
        </div>
      </div>

      <!-- Critical Tasks -->
      <div class="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-red-50">
            <i class="pi pi-exclamation-circle text-red-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-red-600">{{ formatNumber(statistics.criticalTasks) }}</p>
            <p class="text-xs text-gray-500">{{ t('taskCenter.stats.critical') }}</p>
          </div>
        </div>
      </div>

      <!-- Overdue Tasks -->
      <div class="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-orange-50">
            <i class="pi pi-clock text-orange-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-orange-600">{{ formatNumber(statistics.overdueTasks) }}</p>
            <p class="text-xs text-gray-500">{{ t('taskCenter.stats.overdue') }}</p>
          </div>
        </div>
      </div>

      <!-- Inquiry Tasks -->
      <div class="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-50">
            <i class="pi pi-question-circle text-purple-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-gray-900">{{ formatNumber(statistics.inquiryTasks) }}</p>
            <p class="text-xs text-gray-500">{{ t('taskCenter.stats.inquiries') }}</p>
          </div>
        </div>
      </div>

      <!-- SLA Compliance -->
      <div class="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50">
            <i class="pi pi-chart-line text-emerald-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-emerald-600">{{ formatPercentage(statistics.averageSlaCompliancePercent) }}</p>
            <p class="text-xs text-gray-500">{{ t('taskCenter.stats.slaCompliance') }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Filter Bar -->
    <div class="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <!-- Type Filters -->
        <div class="flex flex-wrap gap-2">
          <button
            v-for="filter in [
              { key: 'all', icon: '', label: t('taskCenter.filters.all') },
              { key: 'approval', icon: 'pi-check-circle', label: t('taskCenter.filters.approval') },
              { key: 'evaluation', icon: 'pi-chart-bar', label: t('taskCenter.filters.evaluation') },
              { key: 'inquiry', icon: 'pi-question-circle', label: t('taskCenter.filters.inquiry') },
              { key: 'review', icon: 'pi-users', label: t('taskCenter.filters.review') },
            ]"
            :key="filter.key"
            class="flex items-center gap-1.5 rounded-xl px-4 py-2 text-xs font-semibold transition-all"
            :class="activeTypeFilter === filter.key
              ? 'bg-blue-600 text-white shadow-md'
              : 'bg-gray-50 text-gray-600 hover:bg-gray-100'"
            @click="setTypeFilter(filter.key)"
          >
            <i v-if="filter.icon" class="pi text-[10px]" :class="filter.icon"></i>
            {{ filter.label }}
            <span
              v-if="statistics && filter.key !== 'all'"
              class="ms-1 inline-flex h-5 min-w-5 items-center justify-center rounded-full text-[10px] font-bold"
              :class="activeTypeFilter === filter.key ? 'bg-white/20 text-white' : 'bg-gray-200 text-gray-600'"
            >
              {{ filter.key === 'approval' ? statistics.approvalTasks
                : filter.key === 'evaluation' ? statistics.evaluationTasks
                : filter.key === 'inquiry' ? statistics.inquiryTasks
                : statistics.reviewTasks }}
            </span>
            <span
              v-else-if="statistics && filter.key === 'all'"
              class="ms-1 inline-flex h-5 min-w-5 items-center justify-center rounded-full text-[10px] font-bold"
              :class="activeTypeFilter === 'all' ? 'bg-white/20 text-white' : 'bg-gray-200 text-gray-600'"
            >
              {{ statistics.totalPending }}
            </span>
          </button>
        </div>

        <!-- Search + Sort -->
        <div class="flex items-center gap-3">
          <div class="relative">
            <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-xs text-gray-400"></i>
            <input
              v-model="searchQuery"
              type="text"
              :placeholder="t('common.search')"
              class="w-56 rounded-xl border border-gray-200 bg-gray-50 py-2 ps-9 pe-3 text-xs text-gray-700 outline-none transition-colors focus:border-blue-400 focus:bg-white focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <select
            v-model="sortBy"
            class="rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-xs text-gray-700 outline-none focus:border-blue-400 focus:ring-2 focus:ring-blue-100"
            @change="onSortChange"
          >
            <option value="priority">{{ t('taskCenter.sortBy.priority') }}</option>
            <option value="deadline">{{ t('taskCenter.sortBy.deadline') }}</option>
            <option value="type">{{ t('taskCenter.sortBy.type') }}</option>
            <option value="date">{{ t('taskCenter.sortBy.date') }}</option>
            <option value="sla">{{ t('taskCenter.sortBy.sla') }}</option>
          </select>
        </div>
      </div>

      <!-- Secondary Filters (Source, Priority, SLA) -->
      <div class="mt-3 flex flex-wrap gap-2 border-t border-gray-100 pt-3">
        <!-- Source Filters -->
        <button
          v-for="src in ['competition', 'inquiry']"
          :key="src"
          class="flex items-center gap-1 rounded-lg px-3 py-1.5 text-[11px] font-medium transition-all"
          :class="activeSourceFilter === src
            ? 'bg-blue-100 text-blue-700 border border-blue-200'
            : 'bg-gray-50 text-gray-500 border border-gray-100 hover:bg-gray-100'"
          @click="setSourceFilter(src)"
        >
          <i class="pi text-[10px]" :class="getSourceIcon(src)"></i>
          {{ getSourceLabel(src) }}
        </button>

        <span class="mx-1 text-gray-300">|</span>

        <!-- Priority Filters -->
        <button
          v-for="p in ['critical', 'high', 'medium', 'low']"
          :key="p"
          class="rounded-lg px-3 py-1.5 text-[11px] font-medium transition-all border"
          :class="activePriorityFilter === p
            ? getPriorityBadgeClass(p)
            : 'bg-gray-50 text-gray-500 border-gray-100 hover:bg-gray-100'"
          @click="setPriorityFilter(p)"
        >
          {{ t(`taskCenter.priority.${p}`) }}
        </button>

        <span class="mx-1 text-gray-300">|</span>

        <!-- SLA Filters -->
        <button
          v-for="sla in ['on_track', 'approaching', 'exceeded']"
          :key="sla"
          class="flex items-center gap-1 rounded-lg px-3 py-1.5 text-[11px] font-medium transition-all border"
          :class="activeSlaFilter === sla
            ? getSlaBgClass(sla) + ' ' + getSlaClass(sla) + ' border-current'
            : 'bg-gray-50 text-gray-500 border-gray-100 hover:bg-gray-100'"
          @click="setSlaFilter(sla)"
        >
          <i class="pi text-[9px]" :class="getSlaIcon(sla)"></i>
          {{ t(`taskCenter.sla.${sla}`) }}
        </button>
      </div>
    </div>

    <!-- Bulk Actions Bar -->
    <Transition name="fade">
      <div
        v-if="selectedTasks.length > 0"
        class="flex items-center justify-between rounded-2xl border border-blue-200 bg-blue-50 px-5 py-3"
      >
        <span class="text-sm font-medium text-blue-700">
          {{ formatNumber(selectedTasks.length) }} {{ t('common.selected') }}
        </span>
        <button
          class="rounded-lg bg-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-300"
          @click="selectedTasks = []"
        >
          {{ t('taskCenter.actions.clearSelection') }}
        </button>
      </div>
    </Transition>

    <!-- Loading State -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 5" :key="i" class="h-24 animate-pulse rounded-2xl bg-gray-100"></div>
    </div>

    <!-- Error State -->
    <div v-else-if="errorMessage" class="rounded-2xl border border-red-200 bg-red-50 p-6 text-center">
      <i class="pi pi-exclamation-triangle mb-2 text-3xl text-red-400"></i>
      <p class="text-sm text-red-600">{{ errorMessage }}</p>
      <button
        class="mt-3 rounded-lg bg-red-100 px-4 py-2 text-xs font-semibold text-red-700 hover:bg-red-200"
        @click="loadTasks"
      >
        {{ t('common.retry') }}
      </button>
    </div>

    <!-- Empty State -->
    <div v-else-if="tasks.length === 0" class="rounded-2xl border border-gray-100 bg-white p-12 text-center shadow-sm">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50">
        <i class="pi pi-check-circle text-3xl text-emerald-500"></i>
      </div>
      <h3 class="text-lg font-bold text-gray-800">{{ t('taskCenter.empty') }}</h3>
      <p class="mt-2 text-sm text-gray-500">{{ t('taskCenter.emptyDescription') }}</p>
    </div>

    <!-- Task List -->
    <div v-else class="space-y-3">
      <!-- Select All -->
      <div class="flex items-center gap-3 px-2">
        <input
          type="checkbox"
          :checked="isAllSelected"
          class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          @change="toggleSelectAll"
        />
        <span class="text-xs text-gray-500">
          {{ t('common.selectAll') }}
          <span class="text-gray-400">({{ formatNumber(totalCount) }} {{ t('taskCenter.stats.totalPending') }})</span>
        </span>
      </div>

      <!-- Task Cards -->
      <div
        v-for="task in tasks"
        :key="task.id"
        class="group flex items-start gap-4 rounded-2xl border border-gray-100 bg-white p-4 shadow-sm transition-all duration-200 hover:border-blue-200 hover:shadow-md"
        :class="{
          'border-blue-200 bg-blue-50/30': selectedTasks.includes(task.id),
          'border-s-4 border-s-red-500': task.slaStatus === 'exceeded',
          'border-s-4 border-s-amber-400': task.slaStatus === 'approaching',
        }"
      >
        <!-- Checkbox -->
        <input
          type="checkbox"
          :checked="selectedTasks.includes(task.id)"
          class="mt-1 h-4 w-4 shrink-0 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          @change="toggleSelect(task.id)"
        />

        <!-- Type Icon -->
        <div
          class="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-xl"
          :class="getTypeBg(task.type)"
        >
          <i class="pi text-lg" :class="[getTypeIcon(task.type), getTypeColor(task.type)]"></i>
        </div>

        <!-- Task Info -->
        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2">
            <h4
              class="cursor-pointer truncate text-sm font-bold text-gray-800 hover:text-blue-600"
              @click="navigateToTask(task)"
            >
              {{ getTaskTitle(task) }}
            </h4>
            <span
              class="inline-flex items-center rounded-md border px-2 py-0.5 text-[10px] font-bold"
              :class="getPriorityBadgeClass(task.priority)"
            >
              {{ t(`taskCenter.priority.${task.priority}`) }}
            </span>
            <span
              class="inline-flex items-center gap-1 rounded-md bg-gray-100 px-2 py-0.5 text-[10px] font-medium text-gray-500"
            >
              <i class="pi text-[8px]" :class="getSourceIcon(task.sourceType)"></i>
              {{ getSourceLabel(task.sourceType) }}
            </span>
          </div>

          <!-- Description -->
          <p class="mt-1 line-clamp-1 text-xs text-gray-500">
            {{ getTaskDescription(task) }}
          </p>

          <!-- Meta Info -->
          <div class="mt-2 flex flex-wrap items-center gap-3 text-[11px] text-gray-400">
            <span class="flex items-center gap-1">
              <i class="pi pi-briefcase text-[10px]"></i>
              {{ getCompetitionTitle(task) }}
            </span>
            <span class="flex items-center gap-1">
              <i class="pi pi-hashtag text-[10px]"></i>
              {{ task.competitionReferenceNumber }}
            </span>
            <span class="flex items-center gap-1">
              <i class="pi pi-calendar text-[10px]"></i>
              {{ formatDateTime(task.assignedAt) }}
            </span>
          </div>

          <!-- AI Recommendation (inline) -->
          <div
            v-if="task.aiRecommendationAr || task.aiRecommendationEn"
            class="mt-2 flex items-start gap-2 rounded-lg bg-indigo-50 px-3 py-2"
          >
            <i class="pi pi-sparkles mt-0.5 text-xs text-indigo-500"></i>
            <p class="text-[11px] leading-relaxed text-indigo-700">
              {{ getAiRecommendation(task) }}
            </p>
            <span
              v-if="task.aiConfidence"
              class="ms-auto shrink-0 rounded-full bg-indigo-100 px-2 py-0.5 text-[9px] font-bold text-indigo-600"
            >
              {{ formatPercentage(task.aiConfidence * 100, 0) }}
            </span>
          </div>
        </div>

        <!-- SLA Indicator -->
        <div class="shrink-0 text-end">
          <div
            class="inline-flex items-center gap-1 rounded-lg px-2.5 py-1"
            :class="getSlaBgClass(task.slaStatus)"
          >
            <i class="pi text-xs" :class="[getSlaIcon(task.slaStatus), getSlaClass(task.slaStatus)]"></i>
            <span class="text-xs font-bold" :class="getSlaClass(task.slaStatus)">
              {{ formatRemainingTime(task.remainingTimeSeconds) }}
            </span>
          </div>
          <p class="mt-1 text-[10px] text-gray-400">
            {{ t(`taskCenter.sla.${task.slaStatus}`) }}
          </p>
        </div>

        <!-- Quick Actions -->
        <div class="flex shrink-0 flex-col gap-1 opacity-0 transition-opacity group-hover:opacity-100">
          <button
            class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-50 text-blue-600 transition-colors hover:bg-blue-600 hover:text-white"
            :title="t('taskCenter.actions.view')"
            @click.stop="navigateToTask(task)"
          >
            <i class="pi pi-arrow-right text-xs" :class="{ 'pi-arrow-left': isArabic }"></i>
          </button>
          <button
            v-if="task.aiRecommendationAr"
            class="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600 transition-colors hover:bg-indigo-600 hover:text-white"
            :title="t('taskCenter.actions.aiRecommendation')"
            @click.stop="showAiRecommendation(task)"
          >
            <i class="pi pi-sparkles text-xs"></i>
          </button>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div
      v-if="totalPages > 1"
      class="flex items-center justify-between rounded-2xl border border-gray-100 bg-white px-5 py-3 shadow-sm"
    >
      <p class="text-xs text-gray-500">
        {{ t('common.showing') }}
        {{ formatNumber((currentPage - 1) * pageSize + 1) }}
        -
        {{ formatNumber(Math.min(currentPage * pageSize, totalCount)) }}
        {{ t('common.of') }}
        {{ formatNumber(totalCount) }}
      </p>
      <div class="flex gap-1">
        <button
          class="rounded-lg px-3 py-1.5 text-xs font-medium transition-colors"
          :class="currentPage === 1 ? 'text-gray-300 cursor-not-allowed' : 'text-gray-600 hover:bg-gray-100'"
          :disabled="currentPage === 1"
          @click="goToPage(currentPage - 1)"
        >
          <i class="pi text-[10px]" :class="isArabic ? 'pi-chevron-right' : 'pi-chevron-left'"></i>
        </button>
        <template v-for="page in totalPages" :key="page">
          <button
            v-if="page === 1 || page === totalPages || Math.abs(page - currentPage) <= 1"
            class="rounded-lg px-3 py-1.5 text-xs font-medium transition-colors"
            :class="page === currentPage
              ? 'bg-blue-600 text-white'
              : 'text-gray-600 hover:bg-gray-100'"
            @click="goToPage(page)"
          >
            {{ formatNumber(page) }}
          </button>
          <span
            v-else-if="Math.abs(page - currentPage) === 2"
            class="px-1 text-gray-400"
          >...</span>
        </template>
        <button
          class="rounded-lg px-3 py-1.5 text-xs font-medium transition-colors"
          :class="currentPage === totalPages ? 'text-gray-300 cursor-not-allowed' : 'text-gray-600 hover:bg-gray-100'"
          :disabled="currentPage === totalPages"
          @click="goToPage(currentPage + 1)"
        >
          <i class="pi text-[10px]" :class="isArabic ? 'pi-chevron-left' : 'pi-chevron-right'"></i>
        </button>
      </div>
    </div>

    <!-- AI Recommendation Panel (Slide-over) -->
    <Transition name="slide">
      <div
        v-if="showAiPanel && selectedTaskForAi"
        class="fixed inset-y-0 end-0 z-50 w-96 border-s border-gray-200 bg-white shadow-2xl"
      >
        <div class="flex h-full flex-col">
          <!-- Panel Header -->
          <div class="flex items-center justify-between border-b border-gray-100 px-6 py-4">
            <div class="flex items-center gap-2">
              <i class="pi pi-sparkles text-indigo-600"></i>
              <h3 class="text-sm font-bold text-gray-800">{{ t('taskCenter.ai.title') }}</h3>
            </div>
            <button
              class="flex h-8 w-8 items-center justify-center rounded-lg hover:bg-gray-100"
              @click="closeAiPanel"
            >
              <i class="pi pi-times text-xs text-gray-400"></i>
            </button>
          </div>

          <!-- Panel Content -->
          <div class="flex-1 overflow-y-auto p-6">
            <!-- Task Info -->
            <div class="mb-6 rounded-xl bg-gray-50 p-4">
              <h4 class="text-sm font-bold text-gray-800">{{ getTaskTitle(selectedTaskForAi) }}</h4>
              <p class="mt-1 text-xs text-gray-500">{{ getCompetitionTitle(selectedTaskForAi) }}</p>
            </div>

            <!-- AI Recommendation -->
            <div class="mb-6">
              <h5 class="mb-2 flex items-center gap-2 text-xs font-bold text-gray-700">
                <i class="pi pi-lightbulb text-amber-500"></i>
                {{ t('taskCenter.ai.recommendation') }}
              </h5>
              <div class="rounded-xl border border-indigo-100 bg-indigo-50 p-4">
                <p class="text-sm leading-relaxed text-indigo-800">
                  {{ getAiRecommendation(selectedTaskForAi) }}
                </p>
                <div v-if="selectedTaskForAi.aiConfidence" class="mt-3 flex items-center gap-2">
                  <div class="h-1.5 flex-1 overflow-hidden rounded-full bg-indigo-200">
                    <div
                      class="h-full rounded-full bg-indigo-600 transition-all"
                      :style="{ width: `${(selectedTaskForAi.aiConfidence || 0) * 100}%` }"
                    ></div>
                  </div>
                  <span class="text-[10px] font-bold text-indigo-600">
                    {{ formatPercentage((selectedTaskForAi.aiConfidence || 0) * 100, 0) }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Task Details -->
            <div class="space-y-3">
              <div class="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2">
                <span class="text-xs text-gray-500">{{ t('taskCenter.ai.priority') }}</span>
                <span
                  class="rounded-md border px-2 py-0.5 text-[10px] font-bold"
                  :class="getPriorityBadgeClass(selectedTaskForAi.priority)"
                >
                  {{ t(`taskCenter.priority.${selectedTaskForAi.priority}`) }}
                </span>
              </div>
              <div class="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2">
                <span class="text-xs text-gray-500">{{ t('taskCenter.ai.slaStatus') }}</span>
                <span class="text-xs font-bold" :class="getSlaClass(selectedTaskForAi.slaStatus)">
                  {{ t(`taskCenter.sla.${selectedTaskForAi.slaStatus}`) }}
                </span>
              </div>
              <div class="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2">
                <span class="text-xs text-gray-500">{{ t('taskCenter.ai.remainingTime') }}</span>
                <span class="text-xs font-bold text-gray-700">
                  {{ formatRemainingTime(selectedTaskForAi.remainingTimeSeconds) }}
                </span>
              </div>
              <div class="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2">
                <span class="text-xs text-gray-500">{{ t('taskCenter.ai.source') }}</span>
                <span class="text-xs font-medium text-gray-700">
                  {{ getSourceLabel(selectedTaskForAi.sourceType) }}
                </span>
              </div>
            </div>
          </div>

          <!-- Panel Footer -->
          <div class="border-t border-gray-100 px-6 py-4">
            <button
              class="w-full rounded-xl bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-blue-700"
              @click="navigateToTask(selectedTaskForAi); closeAiPanel()"
            >
              <i class="pi pi-arrow-right me-2 text-xs" :class="{ 'pi-arrow-left': isArabic }"></i>
              {{ t('taskCenter.actions.goToTask') }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Overlay for AI Panel -->
    <Transition name="fade">
      <div
        v-if="showAiPanel"
        class="fixed inset-0 z-40 bg-black/20"
        @click="closeAiPanel"
      ></div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: all 0.3s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

.slide-enter-active,
.slide-leave-active {
  transition: transform 0.3s ease;
}
.slide-enter-from,
.slide-leave-to {
  transform: translateX(100%);
}
[dir="rtl"] .slide-enter-from,
[dir="rtl"] .slide-leave-to {
  transform: translateX(-100%);
}

.line-clamp-1 {
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.animate-fade-in {
  animation: fadeIn 0.3s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
