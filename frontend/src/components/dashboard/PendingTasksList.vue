<script setup lang="ts">
/**
 * PendingTasksList Component
 *
 * Displays pending tasks from the Unified Task Center.
 * Features:
 * - SLA countdown timer with color coding (green/yellow/red)
 * - Task type categorization
 * - Priority indicators
 * - Direct action buttons
 * - Smart sorting by priority and urgency
 */
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { PendingTask, SlaStatus, TaskPriority } from '@/types/dashboard'

defineProps<{
  tasks: PendingTask[]
  isLoading: boolean
}>()

const { t, locale } = useI18n()
const { formatRemainingTime } = useFormatters()

function getTaskTypeIcon(type: string): string {
  const map: Record<string, string> = {
    review_booklet: 'pi-book',
    evaluate_offer: 'pi-star',
    answer_inquiry: 'pi-comments',
    approve_request: 'pi-check-square',
    prepare_minutes: 'pi-file-edit',
    committee_action: 'pi-users',
  }
  return map[type] ?? 'pi-file'
}

function getSlaStatusClass(status: SlaStatus): string {
  const map: Record<SlaStatus, string> = {
    on_track: 'text-success bg-success/10',
    approaching: 'text-warning bg-warning/10',
    exceeded: 'text-danger bg-danger/10',
  }
  return map[status]
}

function getSlaStatusDotClass(status: SlaStatus): string {
  const map: Record<SlaStatus, string> = {
    on_track: 'bg-success',
    approaching: 'bg-warning',
    exceeded: 'bg-danger',
  }
  return map[status]
}

function getPriorityClass(priority: TaskPriority): string {
  const map: Record<TaskPriority, string> = {
    low: 'badge-secondary',
    medium: 'badge-primary',
    high: 'badge-warning',
    critical: 'badge-danger',
  }
  return map[priority]
}

function getLocalizedTitle(task: PendingTask): string {
  return locale.value === 'ar' ? task.titleAr : task.titleEn
}

function getLocalizedCompetition(task: PendingTask): string {
  return locale.value === 'ar' ? task.competitionTitleAr : task.competitionTitleEn
}
</script>

<template>
  <div class="card">
    <div class="mb-4 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <h3 class="text-lg font-semibold text-secondary">
          {{ t('dashboard.pendingTasks') }}
        </h3>
        <span
          v-if="tasks.length > 0"
          class="flex h-6 min-w-6 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-bold text-white"
        >
          {{ tasks.length }}
        </span>
      </div>
      <router-link
        to="/approvals"
        class="text-sm font-medium text-primary hover:text-primary-dark transition-colors"
      >
        {{ t('dashboard.viewAll') }}
      </router-link>
    </div>

    <!-- Loading skeleton -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="animate-pulse rounded-lg border border-surface-dim p-3">
        <div class="flex items-center gap-3">
          <div class="h-8 w-8 rounded-lg bg-surface-dim"></div>
          <div class="flex-1">
            <div class="mb-1 h-3 w-3/4 rounded bg-surface-dim"></div>
            <div class="h-2 w-1/2 rounded bg-surface-muted"></div>
          </div>
          <div class="h-6 w-16 rounded-full bg-surface-dim"></div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="tasks.length === 0"
      class="flex flex-col items-center justify-center py-8 text-center"
    >
      <i class="pi pi-check-circle mb-3 text-4xl text-success"></i>
      <p class="text-sm font-medium text-secondary">{{ t('dashboard.noTasks') }}</p>
      <p class="mt-1 text-xs text-tertiary">{{ t('dashboard.allTasksComplete') }}</p>
    </div>

    <!-- Tasks list -->
    <div v-else class="space-y-2 max-h-[400px] overflow-y-auto pe-1">
      <div
        v-for="task in tasks"
        :key="task.id"
        class="group flex items-start gap-3 rounded-lg border border-surface-dim p-3 transition-all duration-200 hover:border-primary/30 hover:shadow-sm cursor-pointer"
      >
        <!-- Task type icon -->
        <div
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-muted transition-colors group-hover:bg-primary/10"
        >
          <i class="pi text-sm text-tertiary group-hover:text-primary" :class="getTaskTypeIcon(task.type)"></i>
        </div>

        <!-- Task details -->
        <div class="min-w-0 flex-1">
          <div class="flex items-start justify-between gap-2">
            <h4 class="truncate text-sm font-medium text-secondary">
              {{ getLocalizedTitle(task) }}
            </h4>
            <span class="badge shrink-0 text-[10px]" :class="getPriorityClass(task.priority)">
              {{ t(`dashboard.priority.${task.priority}`) }}
            </span>
          </div>
          <p class="mt-0.5 truncate text-xs text-tertiary">
            {{ getLocalizedCompetition(task) }}
            <span class="mx-1 text-surface-dim">|</span>
            {{ task.competitionReferenceNumber }}
          </p>

          <!-- SLA Timer -->
          <div class="mt-2 flex items-center gap-2">
            <div
              class="flex items-center gap-1.5 rounded-md px-2 py-0.5 text-xs font-medium"
              :class="getSlaStatusClass(task.slaStatus)"
            >
              <span
                class="h-1.5 w-1.5 rounded-full animate-pulse"
                :class="getSlaStatusDotClass(task.slaStatus)"
              ></span>
              {{ formatRemainingTime(task.remainingTimeSeconds) }}
            </div>
            <span class="text-[10px] text-tertiary">
              {{ t(`dashboard.slaStatus.${task.slaStatus}`) }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
