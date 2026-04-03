<script setup lang="ts">
/**
 * Operator Dashboard View — Super Admin Portal (TASK-602).
 *
 * Comprehensive dashboard for the platform operator showing:
 * - KPI summary cards (tenant counts, subscriptions, features, AI, audit)
 * - Tenant status distribution chart (doughnut)
 * - Monthly tenant registration trend (bar chart)
 * - System health panel
 * - Expiring subscriptions table
 * - Resource consumption trends (audit activity, feature adoption, AI usage)
 * - Per-tenant usage statistics table with pagination and search
 * - Quick actions panel
 *
 * All data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, ref, defineAsyncComponent } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useOperatorDashboardStore } from '@/stores/operatorDashboard'
import KpiSummaryCards from '@/components/operator/dashboard/KpiSummaryCards.vue'

/* TASK-703: Lazy-load chart and table components for better initial load performance */
const TenantStatusChart = defineAsyncComponent(
  () => import('@/components/operator/dashboard/TenantStatusChart.vue'),
)
const MonthlyRegistrationsChart = defineAsyncComponent(
  () => import('@/components/operator/dashboard/MonthlyRegistrationsChart.vue'),
)
const SystemHealthPanel = defineAsyncComponent(
  () => import('@/components/operator/dashboard/SystemHealthPanel.vue'),
)
const ExpiringSubscriptionsTable = defineAsyncComponent(
  () => import('@/components/operator/dashboard/ExpiringSubscriptionsTable.vue'),
)
const ResourceTrendsCharts = defineAsyncComponent(
  () => import('@/components/operator/dashboard/ResourceTrendsCharts.vue'),
)
const TenantUsageTable = defineAsyncComponent(
  () => import('@/components/operator/dashboard/TenantUsageTable.vue'),
)

const { t } = useI18n()
const router = useRouter()
const store = useOperatorDashboardStore()

const {
  summary,
  systemHealth,
  resourceTrends,
  isLoadingSummary,
  isLoadingHealth,
  isLoadingTrends,
} = storeToRefs(store)

/** Auto-refresh interval in milliseconds (5 minutes). */
const REFRESH_INTERVAL = 5 * 60 * 1000
let refreshTimer: ReturnType<typeof setInterval> | null = null

/** Last refresh timestamp. */
const lastRefreshed = ref<Date | null>(null)

/** Navigation helpers. */
function goToTenants(): void {
  router.push({ name: 'TenantList' })
}

function goToPurchaseOrders(): void {
  router.push({ name: 'PurchaseOrderList' })
}

function goToCreateTenant(): void {
  router.push({ name: 'TenantCreate' })
}

function goToCreatePo(): void {
  router.push({ name: 'PurchaseOrderCreate' })
}

function goToSupportTickets(): void {
  router.push({ name: 'OperatorSupportTickets' })
}

function goToAuditLog(): void {
  router.push({ name: 'OperatorAuditLog' })
}

function goToAiSettings(): void {
  router.push({ name: 'OperatorAiSettings' })
}

function goToSystemHealth(): void {
  router.push({ name: 'OperatorSystemHealth' })
}

/** Manual refresh handler. */
async function handleRefresh(): Promise<void> {
  await store.loadAll()
  lastRefreshed.value = new Date()
}

function formatLastRefreshed(): string {
  if (!lastRefreshed.value) return '-'
  return lastRefreshed.value.toLocaleTimeString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

onMounted(async () => {
  await store.loadAll()
  lastRefreshed.value = new Date()

  // Set up auto-refresh
  refreshTimer = setInterval(async () => {
    await store.loadAll()
    lastRefreshed.value = new Date()
  }, REFRESH_INTERVAL)
})

// Clean up on unmount
import { onUnmounted } from 'vue'
onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 class="text-2xl font-bold text-secondary">
            {{ t('operatorDashboard.title') }}
          </h1>
          <p class="mt-1 text-sm text-tertiary">
            {{ t('operatorDashboard.subtitle') }}
          </p>
        </div>
        <div class="flex items-center gap-3">
          <span class="text-xs text-tertiary">
            {{ t('operatorDashboard.lastRefreshed') }}: {{ formatLastRefreshed() }}
          </span>
          <button
            type="button"
            class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
            :disabled="store.isLoading"
            @click="handleRefresh"
          >
            <i :class="['pi pi-refresh text-sm', { 'animate-spin': store.isLoading }]"></i>
            {{ t('operatorDashboard.refresh') }}
          </button>
        </div>
      </div>

      <!-- Error Banner -->
      <div
        v-if="store.error"
        class="mb-6 flex items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-4"
      >
        <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
        <div class="flex-1">
          <p class="text-sm font-medium text-danger">{{ store.error }}</p>
        </div>
        <button
          class="rounded-lg border border-danger/20 px-3 py-1.5 text-xs font-medium text-danger hover:bg-danger/10 transition-colors"
          @click="handleRefresh"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- KPI Summary Cards -->
      <div class="mb-8">
        <KpiSummaryCards :summary="summary" :is-loading="isLoadingSummary" />
      </div>

      <!-- Charts Row: Status Distribution + Monthly Registrations -->
      <div class="mb-8 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <TenantStatusChart
          :distribution="summary?.tenantStatusDistribution ?? []"
          :is-loading="isLoadingSummary"
        />
        <MonthlyRegistrationsChart
          :data="summary?.monthlyTenantRegistrations ?? []"
          :is-loading="isLoadingSummary"
        />
      </div>

      <!-- System Health + Expiring Subscriptions -->
      <div class="mb-8 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <SystemHealthPanel
          :health="systemHealth"
          :is-loading="isLoadingHealth"
        />
        <div class="lg:col-span-2">
          <ExpiringSubscriptionsTable
            :subscriptions="summary?.expiringSubscriptions ?? []"
            :is-loading="isLoadingSummary"
          />
        </div>
      </div>

      <!-- Resource Consumption Trends -->
      <div class="mb-8">
        <ResourceTrendsCharts
          :trends="resourceTrends"
          :is-loading="isLoadingTrends"
        />
      </div>

      <!-- Tenant Usage Statistics Table -->
      <div class="mb-8">
        <TenantUsageTable />
      </div>

      <!-- Quick Actions -->
      <div class="rounded-lg border border-surface-dim bg-white p-6">
        <h2 class="mb-4 text-lg font-semibold text-secondary">
          {{ t('operatorDashboard.quickActions.title') }}
        </h2>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToCreateTenant"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <i class="pi pi-building text-sm text-primary"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">
                {{ t('operatorDashboard.quickActions.addTenant') }}
              </p>
              <p class="text-xs text-tertiary">
                {{ t('operatorDashboard.quickActions.addTenantDesc') }}
              </p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToCreatePo"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-50">
              <i class="pi pi-file text-sm text-emerald-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">
                {{ t('operatorDashboard.quickActions.addPO') }}
              </p>
              <p class="text-xs text-tertiary">
                {{ t('operatorDashboard.quickActions.addPODesc') }}
              </p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToTenants"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50">
              <i class="pi pi-list text-sm text-blue-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">
                {{ t('operatorDashboard.quickActions.viewTenants') }}
              </p>
              <p class="text-xs text-tertiary">
                {{ t('operatorDashboard.quickActions.viewTenantsDesc') }}
              </p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToPurchaseOrders"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-50">
              <i class="pi pi-chart-bar text-sm text-purple-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">
                {{ t('operatorDashboard.quickActions.viewPOs') }}
              </p>
              <p class="text-xs text-tertiary">
                {{ t('operatorDashboard.quickActions.viewPOsDesc') }}
              </p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToSupportTickets"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-orange-50">
              <i class="pi pi-ticket text-sm text-orange-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">الدعم الفني</p>
              <p class="text-xs text-tertiary">إدارة تذاكر الدعم الفني</p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToAuditLog"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-50">
              <i class="pi pi-history text-sm text-amber-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">سجل التدقيق</p>
              <p class="text-xs text-tertiary">مراجعة جميع أنشطة المنصة</p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToAiSettings"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-violet-50">
              <i class="pi pi-sparkles text-sm text-violet-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">إعدادات الذكاء الاصطناعي</p>
              <p class="text-xs text-tertiary">تكوين مزودي الذكاء الاصطناعي</p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-lg border border-surface-dim p-4 text-start transition-colors hover:bg-surface-muted"
            @click="goToSystemHealth"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-teal-50">
              <i class="pi pi-server text-sm text-teal-600"></i>
            </div>
            <div>
              <p class="text-sm font-medium text-secondary">صحة النظام</p>
              <p class="text-xs text-tertiary">مراقبة حالة الخدمات</p>
            </div>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
