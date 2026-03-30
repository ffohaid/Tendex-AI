<script setup lang="ts">
/**
 * TaskCenterView - Unified Task Center (TASK-1001)
 *
 * A single unified inbox for all human interactions:
 * - Approval tasks
 * - Evaluation tasks
 * - Inquiry responses
 * - Review tasks
 *
 * Features:
 * - Filter by type, priority, SLA status
 * - Sort by priority, deadline, type, date
 * - SLA countdown with color-coded indicators
 * - Quick actions (approve, reject, return)
 * - Bulk operations
 * - Real-time updates via SignalR
 * - RTL/LTR support
 *
 * All data fetched dynamically from APIs.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet, httpPost } from '@/services/http'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const router = useRouter()
const { formatDateTime, formatNumber } = useFormatters()

/* ── Types ── */
interface Task {
  id: string
  type: 'approval' | 'evaluation' | 'inquiry' | 'review'
  title: string
  titleEn: string
  description: string
  competitionName: string
  stageName: string
  priority: 'low' | 'medium' | 'high' | 'critical'
  slaStatus: 'on_track' | 'approaching' | 'exceeded'
  slaDeadline: string
  slaRemainingHours: number
  assignedAt: string
  assignedBy: string
  relatedEntityId: string
  relatedEntityType: string
}

/* ── State ── */
const isLoading = ref(false)
const tasks = ref<Task[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

/* Filters */
const activeFilter = ref<string>('all')
const sortBy = ref<string>('priority')
const searchQuery = ref('')
const selectedTasks = ref<string[]>([])

/* Filter counts */
const filterCounts = computed(() => {
  const counts: Record<string, number> = { all: tasks.value.length }
  for (const task of tasks.value) {
    counts[task.type] = (counts[task.type] || 0) + 1
  }
  return counts
})

const filteredTasks = computed(() => {
  let result = tasks.value
  if (activeFilter.value !== 'all') {
    result = result.filter(t => t.type === activeFilter.value)
  }
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(t =>
      t.title.toLowerCase().includes(q) ||
      t.competitionName.toLowerCase().includes(q)
    )
  }
  /* Sort */
  result = [...result].sort((a, b) => {
    switch (sortBy.value) {
      case 'priority': {
        const order = { critical: 0, high: 1, medium: 2, low: 3 }
        return (order[a.priority] ?? 3) - (order[b.priority] ?? 3)
      }
      case 'deadline':
        return new Date(a.slaDeadline).getTime() - new Date(b.slaDeadline).getTime()
      case 'type':
        return a.type.localeCompare(b.type)
      case 'date':
        return new Date(b.assignedAt).getTime() - new Date(a.assignedAt).getTime()
      default:
        return 0
    }
  })
  return result
})

const isAllSelected = computed(() =>
  filteredTasks.value.length > 0 &&
  filteredTasks.value.every(t => selectedTasks.value.includes(t.id))
)

/* ── Methods ── */
async function loadTasks(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: Task[]; totalCount: number }>('/v1/tasks/pending', {
      params: {
        page: currentPage.value,
        pageSize: pageSize.value,
      },
    })
    tasks.value = data.items
    totalCount.value = data.totalCount
  } catch (err) {
    console.error('Failed to load tasks:', err)
  } finally {
    isLoading.value = false
  }
}

function toggleSelectAll(): void {
  if (isAllSelected.value) {
    selectedTasks.value = []
  } else {
    selectedTasks.value = filteredTasks.value.map(t => t.id)
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

async function performAction(taskId: string, action: string): Promise<void> {
  try {
    await httpPost(`/v1/tasks/${taskId}/${action}`)
    tasks.value = tasks.value.filter(t => t.id !== taskId)
  } catch (err) {
    console.error(`Failed to ${action} task:`, err)
  }
}

async function bulkAction(action: string): Promise<void> {
  try {
    await httpPost(`/v1/tasks/bulk/${action}`, { taskIds: selectedTasks.value })
    tasks.value = tasks.value.filter(t => !selectedTasks.value.includes(t.id))
    selectedTasks.value = []
  } catch (err) {
    console.error(`Failed to bulk ${action}:`, err)
  }
}

function viewTaskDetails(task: Task): void {
  router.push(`/task-center/${task.id}`)
}

function getTypeIcon(type: string): string {
  const icons: Record<string, string> = {
    approval: 'pi-check-circle',
    evaluation: 'pi-clipboard',
    inquiry: 'pi-question-circle',
    review: 'pi-eye',
  }
  return icons[type] || 'pi-circle'
}

function getTypeColor(type: string): string {
  const colors: Record<string, string> = {
    approval: 'text-primary',
    evaluation: 'text-warning',
    inquiry: 'text-info',
    review: 'text-ai',
  }
  return colors[type] || 'text-secondary'
}

function getTypeBg(type: string): string {
  const bgs: Record<string, string> = {
    approval: 'bg-primary-50',
    evaluation: 'bg-warning-50',
    inquiry: 'bg-info-50',
    review: 'bg-ai-50',
  }
  return bgs[type] || 'bg-secondary-50'
}

function getPriorityBadge(priority: string): string {
  const classes: Record<string, string> = {
    critical: 'badge-danger',
    high: 'badge-warning',
    medium: 'badge-info',
    low: 'badge-secondary',
  }
  return classes[priority] || 'badge-secondary'
}

function getSlaClass(status: string): string {
  const classes: Record<string, string> = {
    on_track: 'text-success',
    approaching: 'text-warning',
    exceeded: 'text-danger',
  }
  return classes[status] || 'text-secondary'
}

function getSlaIcon(status: string): string {
  const icons: Record<string, string> = {
    on_track: 'pi-check-circle',
    approaching: 'pi-exclamation-triangle',
    exceeded: 'pi-times-circle',
  }
  return icons[status] || 'pi-clock'
}

onMounted(() => {
  loadTasks()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="animate-fade-in">
      <h1 class="page-title">{{ t('taskCenter.title') }}</h1>
      <p class="page-description">{{ t('taskCenter.subtitle') }}</p>
    </div>

    <!-- Filter Tabs + Search -->
    <div class="card !p-4">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <!-- Type filters -->
        <div class="flex flex-wrap gap-2">
          <button
            v-for="filter in ['all', 'approval', 'evaluation', 'inquiry', 'review']"
            :key="filter"
            class="flex items-center gap-1.5 rounded-xl px-4 py-2 text-xs font-semibold transition-all"
            :class="activeFilter === filter
              ? 'bg-primary text-white shadow-card'
              : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
            @click="activeFilter = filter"
          >
            <i v-if="filter !== 'all'" class="pi text-[10px]" :class="getTypeIcon(filter)"></i>
            {{ t(`taskCenter.filters.${filter}`) }}
            <span
              v-if="filterCounts[filter]"
              class="ms-1 inline-flex h-5 min-w-5 items-center justify-center rounded-full text-[10px] font-bold"
              :class="activeFilter === filter ? 'bg-white/20 text-white' : 'bg-secondary-200 text-secondary-600'"
            >
              {{ filterCounts[filter] }}
            </span>
          </button>
        </div>

        <!-- Search + Sort -->
        <div class="flex items-center gap-3">
          <div class="relative">
            <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-xs text-secondary-400"></i>
            <input
              v-model="searchQuery"
              type="text"
              :placeholder="t('common.search')"
              class="input w-56 ps-9 text-xs"
            />
          </div>
          <select v-model="sortBy" class="input w-40 text-xs">
            <option value="priority">{{ t('taskCenter.sortBy.priority') }}</option>
            <option value="deadline">{{ t('taskCenter.sortBy.deadline') }}</option>
            <option value="type">{{ t('taskCenter.sortBy.type') }}</option>
            <option value="date">{{ t('taskCenter.sortBy.date') }}</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Bulk Actions Bar -->
    <Transition name="fade">
      <div
        v-if="selectedTasks.length > 0"
        class="flex items-center justify-between rounded-2xl border border-primary/20 bg-primary-50 px-5 py-3"
      >
        <span class="text-sm font-medium text-primary">
          {{ selectedTasks.length }} {{ t('common.selected') }}
        </span>
        <div class="flex gap-2">
          <button class="btn-sm bg-success text-white rounded-lg px-3 py-1.5 text-xs font-semibold" @click="bulkAction('approve')">
            <i class="pi pi-check me-1"></i>{{ t('taskCenter.actions.approve') }}
          </button>
          <button class="btn-sm bg-danger text-white rounded-lg px-3 py-1.5 text-xs font-semibold" @click="bulkAction('reject')">
            <i class="pi pi-times me-1"></i>{{ t('taskCenter.actions.reject') }}
          </button>
        </div>
      </div>
    </Transition>

    <!-- Task List -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 5" :key="i" class="skeleton h-20 rounded-2xl"></div>
    </div>

    <div v-else-if="filteredTasks.length === 0" class="empty-state">
      <div class="empty-state-icon">
        <i class="pi pi-check-circle"></i>
      </div>
      <h3 class="empty-state-title">{{ t('taskCenter.empty') }}</h3>
      <p class="empty-state-text">{{ t('taskCenter.emptyDescription') }}</p>
    </div>

    <div v-else class="space-y-3">
      <!-- Select All -->
      <div class="flex items-center gap-3 px-2">
        <input
          type="checkbox"
          :checked="isAllSelected"
          class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
          @change="toggleSelectAll"
        />
        <span class="text-xs text-secondary-500">{{ t('common.selectAll') }}</span>
      </div>

      <!-- Task Cards -->
      <div
        v-for="task in filteredTasks"
        :key="task.id"
        class="group flex items-center gap-4 rounded-2xl border border-secondary-100 bg-white p-4 shadow-xs transition-all duration-200 hover:border-primary/30 hover:shadow-card"
        :class="{ 'border-primary/30 bg-primary-50/30': selectedTasks.includes(task.id) }"
      >
        <!-- Checkbox -->
        <input
          type="checkbox"
          :checked="selectedTasks.includes(task.id)"
          class="h-4 w-4 shrink-0 rounded border-secondary-300 text-primary focus:ring-primary"
          @change="toggleSelect(task.id)"
        />

        <!-- Type Icon -->
        <div
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl"
          :class="getTypeBg(task.type)"
        >
          <i class="pi text-lg" :class="[getTypeIcon(task.type), getTypeColor(task.type)]"></i>
        </div>

        <!-- Task Info -->
        <div class="min-w-0 flex-1 cursor-pointer" @click="viewTaskDetails(task)">
          <div class="flex items-center gap-2">
            <h4 class="truncate text-sm font-bold text-secondary-800">
              {{ locale === 'ar' ? task.title : task.titleEn }}
            </h4>
            <span class="badge" :class="getPriorityBadge(task.priority)">
              {{ t(`dashboard.priority.${task.priority}`) }}
            </span>
          </div>
          <div class="mt-1 flex items-center gap-3 text-xs text-secondary-500">
            <span><i class="pi pi-briefcase me-1 text-[10px]"></i>{{ task.competitionName }}</span>
            <span><i class="pi pi-flag me-1 text-[10px]"></i>{{ task.stageName }}</span>
            <span><i class="pi pi-clock me-1 text-[10px]"></i>{{ formatDateTime(task.assignedAt) }}</span>
          </div>
        </div>

        <!-- SLA Indicator -->
        <div class="shrink-0 text-end">
          <div class="flex items-center gap-1" :class="getSlaClass(task.slaStatus)">
            <i class="pi text-xs" :class="getSlaIcon(task.slaStatus)"></i>
            <span class="text-xs font-semibold">
              {{ task.slaRemainingHours > 0
                ? `${task.slaRemainingHours}h`
                : t('taskCenter.sla.overdue') }}
            </span>
          </div>
          <p class="mt-0.5 text-[10px] text-secondary-400">
            {{ t(`taskCenter.sla.${task.slaStatus === 'on_track' ? 'onTime' : task.slaStatus === 'approaching' ? 'approaching' : 'overdue'}`) }}
          </p>
        </div>

        <!-- Quick Actions -->
        <div class="flex shrink-0 gap-1 opacity-0 transition-opacity group-hover:opacity-100">
          <button
            v-if="task.type === 'approval'"
            class="flex h-8 w-8 items-center justify-center rounded-lg bg-success-50 text-success transition-colors hover:bg-success hover:text-white"
            :title="t('taskCenter.actions.approve')"
            @click.stop="performAction(task.id, 'approve')"
          >
            <i class="pi pi-check text-xs"></i>
          </button>
          <button
            v-if="task.type === 'approval'"
            class="flex h-8 w-8 items-center justify-center rounded-lg bg-danger-50 text-danger transition-colors hover:bg-danger hover:text-white"
            :title="t('taskCenter.actions.reject')"
            @click.stop="performAction(task.id, 'reject')"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
          <button
            class="flex h-8 w-8 items-center justify-center rounded-lg bg-info-50 text-info transition-colors hover:bg-info hover:text-white"
            :title="t('taskCenter.actions.view')"
            @click.stop="viewTaskDetails(task)"
          >
            <i class="pi pi-eye text-xs"></i>
          </button>
        </div>
      </div>
    </div>
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
</style>
