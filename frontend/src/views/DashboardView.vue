<script setup lang="ts">
/**
 * DashboardView - Main Dashboard Page
 *
 * TASK-503: Implements the main dashboard as specified in PRD v6.1
 * Sections 13.1, 13.2, and 15.1.
 *
 * Features:
 * - Role-based widget visibility
 * - KPI statistics cards
 * - Active competitions with progress tracking
 * - Pending tasks with SLA countdown
 * - Notifications with read/unread status
 * - Active committees overview
 * - Recent activity timeline
 * - Interactive performance charts (Chart.js)
 * - Quick action buttons
 * - Auto-refresh every 30 seconds
 * - RTL/LTR support
 * - English numerals exclusively
 *
 * All data is fetched dynamically from APIs.
 * NO mock/hardcoded data is used.
 */
import { onMounted, onUnmounted, defineAsyncComponent } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useFormatters } from '@/composables/useFormatters'
import { storeToRefs } from 'pinia'

/* Eagerly loaded lightweight components */
import StatsCards from '@/components/dashboard/StatsCards.vue'
import QuickActions from '@/components/dashboard/QuickActions.vue'
import PendingTasksList from '@/components/dashboard/PendingTasksList.vue'

/* Lazy-loaded heavier components for performance */
const ActiveCompetitionsList = defineAsyncComponent(
  () => import('@/components/dashboard/ActiveCompetitionsList.vue'),
)
const NotificationsList = defineAsyncComponent(
  () => import('@/components/dashboard/NotificationsList.vue'),
)
const ActiveCommittees = defineAsyncComponent(
  () => import('@/components/dashboard/ActiveCommittees.vue'),
)
const RecentActivityList = defineAsyncComponent(
  () => import('@/components/dashboard/RecentActivityList.vue'),
)
const PerformanceCharts = defineAsyncComponent(
  () => import('@/components/dashboard/PerformanceCharts.vue'),
)

const { t } = useI18n()
const { formatDateTime } = useFormatters()

const dashboardStore = useDashboardStore()
const {
  isLoading,
  isRefreshing,
  error,
  lastUpdated,
  stats,
  activeCompetitions,
  pendingTasks,
  notifications,
  activeCommittees,
  recentActivities,
  performanceMetrics,
  dashboardConfig,
} = storeToRefs(dashboardStore)

/* Auto-refresh interval (30 seconds per PRD requirement) */
const AUTO_REFRESH_INTERVAL = 30_000
let refreshTimer: ReturnType<typeof setInterval> | null = null

onMounted(async () => {
  await dashboardStore.loadDashboard()

  /* Start auto-refresh */
  refreshTimer = setInterval(() => {
    dashboardStore.refreshDashboard()
  }, AUTO_REFRESH_INTERVAL)
})

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
})

function handleMarkNotificationRead(notificationId: string): void {
  dashboardStore.markNotificationRead(notificationId)
}

async function handleManualRefresh(): Promise<void> {
  await dashboardStore.refreshDashboard()
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('nav.dashboard') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('dashboard.subtitle') }}
        </p>
      </div>

      <div class="flex items-center gap-3">
        <!-- Last updated indicator -->
        <span v-if="lastUpdated" class="text-xs text-surface-dim">
          {{ t('dashboard.lastUpdated') }}: {{ formatDateTime(lastUpdated.toISOString()) }}
        </span>

        <!-- Refresh button -->
        <button
          class="flex items-center gap-1.5 rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm font-medium text-secondary transition-all duration-200 hover:border-primary/30 hover:shadow-sm disabled:opacity-50"
          :disabled="isRefreshing"
          @click="handleManualRefresh"
        >
          <i
            class="pi pi-refresh text-xs"
            :class="{ 'animate-spin': isRefreshing }"
          ></i>
          {{ t('dashboard.refresh') }}
        </button>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="error"
      class="flex items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-4"
    >
      <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
      <div class="flex-1">
        <p class="text-sm font-medium text-danger">
          {{ t('dashboard.errorTitle') }}
        </p>
        <p class="mt-0.5 text-xs text-danger/80">
          {{ t('dashboard.errorMessage') }}
        </p>
      </div>
      <button
        class="rounded-lg border border-danger/20 px-3 py-1.5 text-xs font-medium text-danger hover:bg-danger/10 transition-colors"
        @click="handleManualRefresh"
      >
        {{ t('dashboard.retry') }}
      </button>
    </div>

    <!-- KPI Statistics Cards -->
    <StatsCards :stats="stats" :is-loading="isLoading" />

    <!-- Quick Actions -->
    <QuickActions />

    <!-- Main Content Grid -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <!-- Left Column: Tasks + Competitions (2/3 width) -->
      <div class="space-y-6 lg:col-span-2">
        <!-- Pending Tasks -->
        <PendingTasksList
          v-if="dashboardConfig.showTasks"
          :tasks="pendingTasks"
          :is-loading="isLoading"
        />

        <!-- Active Competitions -->
        <ActiveCompetitionsList
          v-if="dashboardConfig.showCompetitions"
          :competitions="activeCompetitions"
          :is-loading="isLoading"
        />
      </div>

      <!-- Right Column: Notifications + Committees + Activity (1/3 width) -->
      <div class="space-y-6">
        <!-- Notifications -->
        <NotificationsList
          v-if="dashboardConfig.showNotifications"
          :notifications="notifications"
          :is-loading="isLoading"
          @mark-read="handleMarkNotificationRead"
        />

        <!-- Active Committees -->
        <ActiveCommittees
          v-if="dashboardConfig.showCommittees"
          :committees="activeCommittees"
          :is-loading="isLoading"
        />

        <!-- Recent Activity -->
        <RecentActivityList
          v-if="dashboardConfig.showRecentActivity"
          :activities="recentActivities"
          :is-loading="isLoading"
        />
      </div>
    </div>

    <!-- Performance Charts (Full Width) -->
    <PerformanceCharts
      v-if="dashboardConfig.showPerformanceMetrics"
      :metrics="performanceMetrics"
      :is-loading="isLoading"
    />
  </div>
</template>
