<script setup lang="ts">
/**
 * Operator Dashboard View — Super Admin Portal.
 *
 * Main dashboard for the platform operator showing:
 * - KPI statistics cards
 * - Renewal alerts widget
 * - Quick actions
 *
 * Data is fetched dynamically from the API — NO mock data.
 */
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { usePurchaseOrderStore } from '@/stores/purchaseOrder'
import { useTenantStore } from '@/stores/tenant'
import RenewalAlertsWidget from '@/components/operator/RenewalAlertsWidget.vue'

const { t } = useI18n()
const router = useRouter()
const poStore = usePurchaseOrderStore()
const tenantStore = useTenantStore()

const { statistics } = storeToRefs(poStore)
const { totalCount: tenantCount } = storeToRefs(tenantStore)

/** Navigation */
function goToTenants() {
  router.push({ name: 'TenantList' })
}

function goToPurchaseOrders() {
  router.push({ name: 'PurchaseOrderList' })
}

function goToCreateTenant() {
  router.push({ name: 'TenantCreate' })
}

function goToCreatePo() {
  router.push({ name: 'PurchaseOrderCreate' })
}

onMounted(() => {
  poStore.loadStatistics()
  poStore.loadRenewalAlerts(60)
  tenantStore.loadTenants()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('operatorDashboard.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('operatorDashboard.subtitle') }}
        </p>
      </div>

      <!-- KPI Cards -->
      <div class="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <!-- Total Tenants -->
        <div
          class="cursor-pointer rounded-lg border border-surface-dim bg-white p-5 transition-shadow hover:shadow-md"
          @click="goToTenants"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm text-tertiary">{{ t('operatorDashboard.kpi.totalTenants') }}</p>
              <p class="mt-1 text-2xl font-bold text-secondary">{{ tenantCount }}</p>
            </div>
            <div class="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
              <i class="pi pi-building text-xl text-primary"></i>
            </div>
          </div>
        </div>

        <!-- Active POs -->
        <div
          class="cursor-pointer rounded-lg border border-surface-dim bg-white p-5 transition-shadow hover:shadow-md"
          @click="goToPurchaseOrders"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm text-tertiary">{{ t('operatorDashboard.kpi.activePOs') }}</p>
              <p class="mt-1 text-2xl font-bold text-emerald-600">{{ statistics?.totalActive || 0 }}</p>
            </div>
            <div class="flex h-12 w-12 items-center justify-center rounded-lg bg-emerald-50">
              <i class="pi pi-check-circle text-xl text-emerald-600"></i>
            </div>
          </div>
        </div>

        <!-- Pending POs -->
        <div
          class="cursor-pointer rounded-lg border border-surface-dim bg-white p-5 transition-shadow hover:shadow-md"
          @click="goToPurchaseOrders"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm text-tertiary">{{ t('operatorDashboard.kpi.pendingPOs') }}</p>
              <p class="mt-1 text-2xl font-bold text-amber-600">{{ statistics?.totalPending || 0 }}</p>
            </div>
            <div class="flex h-12 w-12 items-center justify-center rounded-lg bg-amber-50">
              <i class="pi pi-clock text-xl text-amber-600"></i>
            </div>
          </div>
        </div>

        <!-- Renewing -->
        <div
          class="cursor-pointer rounded-lg border border-surface-dim bg-white p-5 transition-shadow hover:shadow-md"
          @click="goToPurchaseOrders"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm text-tertiary">{{ t('operatorDashboard.kpi.renewingPOs') }}</p>
              <p class="mt-1 text-2xl font-bold text-orange-600">{{ statistics?.totalRenewing || 0 }}</p>
            </div>
            <div class="flex h-12 w-12 items-center justify-center rounded-lg bg-orange-50">
              <i class="pi pi-refresh text-xl text-orange-600"></i>
            </div>
          </div>
        </div>
      </div>

      <!-- Main content grid -->
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <!-- Renewal Alerts (2/3 width) -->
        <div class="lg:col-span-2">
          <RenewalAlertsWidget />
        </div>

        <!-- Quick Actions (1/3 width) -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-4 text-lg font-semibold text-secondary">
            {{ t('operatorDashboard.quickActions.title') }}
          </h2>
          <div class="space-y-3">
            <button
              type="button"
              class="flex w-full items-center gap-3 rounded-lg border border-surface-dim p-3 text-start transition-colors hover:bg-surface-muted"
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
              class="flex w-full items-center gap-3 rounded-lg border border-surface-dim p-3 text-start transition-colors hover:bg-surface-muted"
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
              class="flex w-full items-center gap-3 rounded-lg border border-surface-dim p-3 text-start transition-colors hover:bg-surface-muted"
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
              class="flex w-full items-center gap-3 rounded-lg border border-surface-dim p-3 text-start transition-colors hover:bg-surface-muted"
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
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
