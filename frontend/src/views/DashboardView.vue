<script setup lang="ts">
/**
 * DashboardView - Modern Dashboard (TASK-1001 Overhaul)
 *
 * Features:
 * - Gradient welcome header with user greeting
 * - Animated KPI statistics cards
 * - Active competitions with progress tracking
 * - Pending tasks with SLA countdown
 * - Notifications with read/unread status
 * - Active committees overview (My Committees tab)
 * - Recent activity timeline
 * - Interactive performance charts (Chart.js)
 * - Quick action buttons
 * - Auto-refresh every 30 seconds
 * - RTL/LTR support
 * - English numerals exclusively
 * - Skeleton loading states
 * - Fade-in animations
 *
 * All data is fetched dynamically from APIs.
 * NO mock/hardcoded data is used.
 */
import { onMounted, onUnmounted, ref, defineAsyncComponent } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
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
const authStore = useAuthStore()

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

const activeTab = ref<'tasks' | 'committees'>('tasks')

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

/** Get greeting based on time of day */
function getGreeting(): string {
  const hour = new Date().getHours()
  if (hour < 12) return t('dashboard.greetingMorning')
  if (hour < 17) return t('dashboard.greetingAfternoon')
  return t('dashboard.greetingEvening')
}
</script>

<template>
  <div class="space-y-6">
    <!-- Welcome Header with Gradient -->
    <div class="animate-fade-in overflow-hidden rounded-2xl bg-gradient-to-r from-primary to-primary-light p-6 text-white shadow-elevated">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 class="text-2xl font-bold">
            {{ getGreeting() }}
          </h1>
          <p class="mt-1 text-sm text-white/80">
            {{ t('dashboard.subtitle') }}
          </p>
        </div>
        <div class="flex items-center gap-3">
          <!-- Last updated indicator -->
          <span v-if="lastUpdated" class="text-xs text-white/60">
            {{ t('dashboard.lastUpdated') }}: {{ formatDateTime(lastUpdated.toISOString()) }}
          </span>
          <!-- Refresh button -->
          <button
            class="flex items-center gap-1.5 rounded-xl border border-white/20 bg-white/10 px-4 py-2.5 text-sm font-medium text-white backdrop-blur-sm transition-all duration-200 hover:bg-white/20 disabled:opacity-50"
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
    </div>

    <!-- Error Banner -->
    <div
      v-if="error"
      class="animate-fade-in flex items-center gap-3 rounded-2xl border border-danger/20 bg-danger-50 p-4"
    >
      <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-danger/10">
        <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
      </div>
      <div class="flex-1">
        <p class="text-sm font-semibold text-danger">
          {{ t('dashboard.errorTitle') }}
        </p>
        <p class="mt-0.5 text-xs text-danger/80">
          {{ t('dashboard.errorMessage') }}
        </p>
      </div>
      <button
        class="btn-outline btn-sm border-danger text-danger hover:bg-danger hover:text-white"
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
      <!-- Left Column: Tasks/Committees + Competitions (2/3 width) -->
      <div class="space-y-6 lg:col-span-2">
        <!-- Tab switcher: Tasks / My Committees -->
        <div class="card !p-0 overflow-hidden">
          <div class="flex border-b border-secondary-100">
            <button
              class="flex-1 px-6 py-3.5 text-sm font-semibold transition-colors"
              :class="activeTab === 'tasks'
                ? 'text-primary border-b-2 border-primary bg-primary-50/50'
                : 'text-secondary-500 hover:text-secondary-700 hover:bg-surface-subtle'"
              @click="activeTab = 'tasks'"
            >
              <i class="pi pi-list-check me-2"></i>
              {{ t('dashboard.tabs.pendingTasks') }}
              <span
                v-if="pendingTasks.length > 0"
                class="ms-2 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-[10px] font-bold text-white"
              >
                {{ pendingTasks.length }}
              </span>
            </button>
            <button
              class="flex-1 px-6 py-3.5 text-sm font-semibold transition-colors"
              :class="activeTab === 'committees'
                ? 'text-primary border-b-2 border-primary bg-primary-50/50'
                : 'text-secondary-500 hover:text-secondary-700 hover:bg-surface-subtle'"
              @click="activeTab = 'committees'"
            >
              <i class="pi pi-users me-2"></i>
              {{ t('dashboard.tabs.myCommittees') }}
              <span
                v-if="activeCommittees.length > 0"
                class="ms-2 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-info px-1.5 text-[10px] font-bold text-white"
              >
                {{ activeCommittees.length }}
              </span>
            </button>
          </div>
          <div class="p-6">
            <!-- Pending Tasks -->
            <PendingTasksList
              v-if="activeTab === 'tasks'"
              :tasks="pendingTasks"
              :is-loading="isLoading"
            />
            <!-- My Committees -->
            <ActiveCommittees
              v-if="activeTab === 'committees'"
              :committees="activeCommittees"
              :is-loading="isLoading"
            />
          </div>
        </div>

        <!-- Active Competitions -->
        <ActiveCompetitionsList
          v-if="dashboardConfig.showCompetitions"
          :competitions="activeCompetitions"
          :is-loading="isLoading"
        />
      </div>

      <!-- Right Column: Notifications + Activity (1/3 width) -->
      <div class="space-y-6">
        <!-- Notifications -->
        <NotificationsList
          v-if="dashboardConfig.showNotifications"
          :notifications="notifications"
          :is-loading="isLoading"
          @mark-read="handleMarkNotificationRead"
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
