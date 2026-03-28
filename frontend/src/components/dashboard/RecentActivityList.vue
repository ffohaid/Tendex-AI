<script setup lang="ts">
/**
 * RecentActivityList Component
 *
 * Displays a timeline of recent actions on the platform.
 * Shows user actions like logins, approvals, and offer submissions.
 */
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { RecentActivity } from '@/types/dashboard'

defineProps<{
  activities: RecentActivity[]
  isLoading: boolean
}>()

const { locale } = useI18n()
const { t } = useI18n()
const { formatRelativeTime } = useFormatters()

function getEntityIcon(entityType: string): string {
  const map: Record<string, string> = {
    competition: 'pi-briefcase',
    offer: 'pi-file',
    committee: 'pi-users',
    evaluation: 'pi-star',
    approval: 'pi-check-circle',
    user: 'pi-user',
    inquiry: 'pi-comments',
  }
  return map[entityType] ?? 'pi-circle'
}

function getEntityColor(entityType: string): string {
  const map: Record<string, string> = {
    competition: 'border-primary bg-primary/10 text-primary',
    offer: 'border-info bg-info/10 text-info',
    committee: 'border-tertiary bg-tertiary/10 text-tertiary',
    evaluation: 'border-warning bg-warning/10 text-warning',
    approval: 'border-success bg-success/10 text-success',
    user: 'border-secondary bg-secondary/10 text-secondary',
    inquiry: 'border-info bg-info/10 text-info',
  }
  return map[entityType] ?? 'border-surface-dim bg-surface-muted text-tertiary'
}

function getLocalizedAction(activity: RecentActivity): string {
  return locale.value === 'ar' ? activity.actionAr : activity.actionEn
}

function getLocalizedUserName(activity: RecentActivity): string {
  return locale.value === 'ar' ? activity.userNameAr : activity.userNameEn
}
</script>

<template>
  <div class="card">
    <div class="mb-4">
      <h3 class="text-lg font-semibold text-secondary">
        {{ t('dashboard.recentActivity') }}
      </h3>
    </div>

    <!-- Loading skeleton -->
    <div v-if="isLoading" class="space-y-4">
      <div v-for="i in 5" :key="i" class="animate-pulse flex items-start gap-3">
        <div class="h-8 w-8 rounded-full bg-surface-dim"></div>
        <div class="flex-1">
          <div class="mb-1 h-3 w-3/4 rounded bg-surface-dim"></div>
          <div class="h-2 w-1/4 rounded bg-surface-muted"></div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="activities.length === 0"
      class="flex flex-col items-center justify-center py-8 text-center"
    >
      <i class="pi pi-history mb-3 text-4xl text-surface-dim"></i>
      <p class="text-sm text-tertiary">{{ t('dashboard.noActivity') }}</p>
    </div>

    <!-- Activity timeline -->
    <div v-else class="relative max-h-[350px] overflow-y-auto pe-1">
      <div class="absolute start-4 top-0 bottom-0 w-px bg-surface-dim"></div>

      <div
        v-for="activity in activities"
        :key="activity.id"
        class="relative flex items-start gap-3 pb-4 ps-0"
      >
        <!-- Timeline dot -->
        <div
          class="relative z-10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full border"
          :class="getEntityColor(activity.entityType)"
        >
          <i class="pi text-xs" :class="getEntityIcon(activity.entityType)"></i>
        </div>

        <!-- Content -->
        <div class="min-w-0 flex-1 pt-0.5">
          <p class="text-sm text-secondary">
            <span class="font-medium">{{ getLocalizedUserName(activity) }}</span>
            <span class="mx-1 text-tertiary">{{ getLocalizedAction(activity) }}</span>
          </p>
          <p class="mt-0.5 text-[10px] text-surface-dim">
            {{ formatRelativeTime(activity.timestamp) }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
