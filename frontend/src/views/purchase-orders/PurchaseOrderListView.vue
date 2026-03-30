<script setup lang="ts">
/**
 * Purchase Order List View — Super Admin Portal.
 *
 * Displays a paginated, filterable list of all purchase orders.
 * Data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { usePurchaseOrderStore } from '@/stores/purchaseOrder'
import { useTenantStore } from '@/stores/tenant'
import { PoStatus } from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const poStore = usePurchaseOrderStore()
const tenantStore = useTenantStore()

const {
  purchaseOrders,
  totalCount,
  totalPages,
  currentPage,
  searchQuery,
  statusFilter,
  tenantFilter,
  isLoading,
  error,
} = storeToRefs(poStore)

const { selectorOptions } = storeToRefs(tenantStore)

/** PO Status badge configuration */
function getPoStatusBadge(status: PoStatus) {
  const config: Record<
    number,
    { labelKey: string; bgClass: string; textClass: string; icon: string }
  > = {
    [PoStatus.Received]: {
      labelKey: 'purchaseOrders.statuses.received',
      bgClass: 'bg-blue-50',
      textClass: 'text-blue-700',
      icon: 'pi pi-inbox',
    },
    [PoStatus.EnvironmentSetup]: {
      labelKey: 'purchaseOrders.statuses.environmentSetup',
      bgClass: 'bg-indigo-50',
      textClass: 'text-indigo-700',
      icon: 'pi pi-cog',
    },
    [PoStatus.Training]: {
      labelKey: 'purchaseOrders.statuses.training',
      bgClass: 'bg-purple-50',
      textClass: 'text-purple-700',
      icon: 'pi pi-users',
    },
    [PoStatus.FinalAcceptance]: {
      labelKey: 'purchaseOrders.statuses.finalAcceptance',
      bgClass: 'bg-cyan-50',
      textClass: 'text-cyan-700',
      icon: 'pi pi-verified',
    },
    [PoStatus.ActiveOperation]: {
      labelKey: 'purchaseOrders.statuses.activeOperation',
      bgClass: 'bg-emerald-50',
      textClass: 'text-emerald-700',
      icon: 'pi pi-check-circle',
    },
    [PoStatus.RenewalWindow]: {
      labelKey: 'purchaseOrders.statuses.renewalWindow',
      bgClass: 'bg-amber-50',
      textClass: 'text-amber-700',
      icon: 'pi pi-clock',
    },
    [PoStatus.Renewed]: {
      labelKey: 'purchaseOrders.statuses.renewed',
      bgClass: 'bg-teal-50',
      textClass: 'text-teal-700',
      icon: 'pi pi-refresh',
    },
    [PoStatus.Cancelled]: {
      labelKey: 'purchaseOrders.statuses.cancelled',
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
      icon: 'pi pi-times-circle',
    },
  }
  return (
    config[status] || {
      labelKey: 'common.noData',
      bgClass: 'bg-slate-100',
      textClass: 'text-slate-600',
      icon: 'pi pi-question-circle',
    }
  )
}

/** Status filter options */
const statusFilterOptions = [
  { value: null, labelKey: 'purchaseOrders.filters.allStatuses' },
  { value: PoStatus.Received, labelKey: 'purchaseOrders.statuses.received' },
  { value: PoStatus.EnvironmentSetup, labelKey: 'purchaseOrders.statuses.environmentSetup' },
  { value: PoStatus.Training, labelKey: 'purchaseOrders.statuses.training' },
  { value: PoStatus.FinalAcceptance, labelKey: 'purchaseOrders.statuses.finalAcceptance' },
  { value: PoStatus.ActiveOperation, labelKey: 'purchaseOrders.statuses.activeOperation' },
  { value: PoStatus.RenewalWindow, labelKey: 'purchaseOrders.statuses.renewalWindow' },
  { value: PoStatus.Renewed, labelKey: 'purchaseOrders.statuses.renewed' },
  { value: PoStatus.Cancelled, labelKey: 'purchaseOrders.statuses.cancelled' },
]

/** Format date */
function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-SA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

/** Format currency */
function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-SA', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount) + ' \uFDFC'
}

/** Get tenant name based on locale */
function getTenantName(item: { tenantNameAr: string; tenantNameEn: string }): string {
  return locale.value === 'ar' ? item.tenantNameAr : item.tenantNameEn
}

/** Get expiry badge */
function getExpiryBadge(days: number | null): { text: string; bgClass: string; textClass: string } {
  if (days === null) return { text: '-', bgClass: '', textClass: 'text-tertiary' }
  if (days <= 0) return { text: t('purchaseOrders.expiry.expired'), bgClass: 'bg-red-50', textClass: 'text-red-700' }
  if (days <= 14) return { text: `${days} ${t('purchaseOrders.expiry.days')}`, bgClass: 'bg-red-50', textClass: 'text-red-700' }
  if (days <= 30) return { text: `${days} ${t('purchaseOrders.expiry.days')}`, bgClass: 'bg-orange-50', textClass: 'text-orange-700' }
  if (days <= 60) return { text: `${days} ${t('purchaseOrders.expiry.days')}`, bgClass: 'bg-amber-50', textClass: 'text-amber-700' }
  return { text: `${days} ${t('purchaseOrders.expiry.days')}`, bgClass: 'bg-emerald-50', textClass: 'text-emerald-700' }
}

/** Plan label */
function getPlanLabel(plan: string): string {
  return t(`purchaseOrders.plans.${plan}`)
}

/** Navigation */
function goToCreate() {
  router.push({ name: 'PurchaseOrderCreate' })
}

function goToDetail(id: string) {
  router.push({ name: 'PurchaseOrderDetail', params: { id } })
}

/** Watch filters */
watch([searchQuery, statusFilter, tenantFilter], () => {
  poStore.resetAndReload()
})

watch(currentPage, () => {
  poStore.loadPurchaseOrders()
})

onMounted(() => {
  poStore.loadPurchaseOrders()
  tenantStore.loadSelectorOptions()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8 flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-secondary">
            {{ t('purchaseOrders.titles.list') }}
          </h1>
          <p class="mt-1 text-sm text-tertiary">
            {{ t('purchaseOrders.titles.listSubtitle') }}
          </p>
        </div>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-bold text-white shadow-md transition-all hover:bg-primary-dark hover:shadow-lg"
          @click="goToCreate"
        >
          <i class="pi pi-plus text-sm"></i>
          {{ t('purchaseOrders.actions.createNew') }}
        </button>
      </div>

      <!-- Filters -->
      <div class="mb-6 flex flex-wrap items-center gap-4 rounded-lg border border-surface-dim bg-white p-4">
        <!-- Search -->
        <div class="relative min-w-[200px] flex-1">
          <i class="pi pi-search pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-sm text-tertiary"></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('purchaseOrders.placeholders.search')"
            class="w-full rounded-lg border border-surface-dim py-2 pe-4 ps-10 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          />
        </div>

        <!-- Status filter -->
        <select
          v-model="statusFilter"
          class="rounded-lg border border-surface-dim px-4 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
        >
          <option
            v-for="option in statusFilterOptions"
            :key="String(option.value)"
            :value="option.value"
          >
            {{ t(option.labelKey) }}
          </option>
        </select>

        <!-- Tenant filter -->
        <select
          v-model="tenantFilter"
          class="rounded-lg border border-surface-dim px-4 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
        >
          <option value="">{{ t('purchaseOrders.filters.allTenants') }}</option>
          <option
            v-for="tenant in selectorOptions"
            :key="tenant.id"
            :value="tenant.id"
          >
            {{ locale === 'ar' ? tenant.nameAr : tenant.nameEn }}
          </option>
        </select>

        <!-- Count -->
        <span class="text-sm text-tertiary">
          {{ t('purchaseOrders.labels.total') }}: {{ totalCount }}
        </span>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="rounded-lg border border-danger/20 bg-danger/5 p-8 text-center"
      >
        <i class="pi pi-exclamation-triangle mb-3 text-3xl text-danger"></i>
        <p class="text-sm text-danger">{{ t(error) }}</p>
        <button
          type="button"
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="poStore.loadPurchaseOrders()"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Empty state -->
      <div
        v-else-if="purchaseOrders.length === 0"
        class="rounded-lg border-2 border-dashed border-surface-dim p-16 text-center"
      >
        <i class="pi pi-file mb-4 text-5xl text-tertiary"></i>
        <h3 class="text-lg font-bold text-secondary">
          {{ t('purchaseOrders.messages.noOrders') }}
        </h3>
        <p class="mt-2 text-sm text-tertiary">
          {{ t('purchaseOrders.messages.noOrdersDesc') }}
        </p>
        <button
          type="button"
          class="mt-6 rounded-lg bg-primary px-6 py-2.5 text-sm font-bold text-white hover:bg-primary-dark"
          @click="goToCreate"
        >
          <i class="pi pi-plus me-2 text-sm"></i>
          {{ t('purchaseOrders.actions.createFirst') }}
        </button>
      </div>

      <!-- Table -->
      <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white shadow-sm">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="border-b border-surface-dim bg-surface-muted">
              <tr>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.poNumber') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.tenant') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.plan') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.amount') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.status') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.subscriptionEnd') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.daysRemaining') }}</th>
                <th class="px-4 py-3 text-center text-xs font-medium text-tertiary">{{ t('purchaseOrders.tableHeaders.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in purchaseOrders"
                :key="item.id"
                class="border-b border-surface-dim transition-colors hover:bg-surface-muted/50"
              >
                <!-- PO Number -->
                <td class="px-4 py-3">
                  <button
                    type="button"
                    class="text-sm font-medium text-primary hover:underline"
                    @click="goToDetail(item.id)"
                  >
                    {{ item.poNumber }}
                  </button>
                </td>

                <!-- Tenant -->
                <td class="px-4 py-3 text-sm text-secondary">
                  {{ getTenantName(item) }}
                </td>

                <!-- Plan -->
                <td class="px-4 py-3">
                  <span class="inline-flex rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                    {{ getPlanLabel(item.subscriptionPlan) }}
                  </span>
                </td>

                <!-- Amount -->
                <td class="px-4 py-3 text-sm font-medium text-secondary" dir="ltr">
                  {{ formatAmount(item.totalAmount) }}
                </td>

                <!-- Status -->
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium"
                    :class="[getPoStatusBadge(item.status).bgClass, getPoStatusBadge(item.status).textClass]"
                  >
                    <i :class="getPoStatusBadge(item.status).icon" class="text-[10px]"></i>
                    {{ t(getPoStatusBadge(item.status).labelKey) }}
                  </span>
                </td>

                <!-- Subscription End -->
                <td class="px-4 py-3 text-sm text-tertiary">
                  {{ formatDate(item.subscriptionEndDate) }}
                </td>

                <!-- Days Remaining -->
                <td class="px-4 py-3">
                  <span
                    v-if="item.daysUntilExpiry !== null"
                    class="inline-flex rounded-full px-2 py-0.5 text-xs font-medium"
                    :class="[getExpiryBadge(item.daysUntilExpiry).bgClass, getExpiryBadge(item.daysUntilExpiry).textClass]"
                  >
                    {{ getExpiryBadge(item.daysUntilExpiry).text }}
                  </span>
                  <span v-else class="text-xs text-tertiary">-</span>
                </td>

                <!-- Actions -->
                <td class="px-4 py-3 text-center">
                  <button
                    type="button"
                    class="flex h-8 w-8 items-center justify-center rounded-lg transition-colors hover:bg-surface-muted"
                    :title="t('common.edit')"
                    @click="goToDetail(item.id)"
                  >
                    <i class="pi pi-eye text-sm text-primary"></i>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="flex items-center justify-between border-t border-surface-dim px-4 py-3">
          <span class="text-sm text-tertiary">
            {{ t('purchaseOrders.pagination.showing') }} {{ purchaseOrders.length }}
            {{ t('purchaseOrders.pagination.of') }} {{ totalCount }}
          </span>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-3 py-1.5 text-sm transition-colors hover:bg-surface-muted disabled:opacity-50"
              :disabled="currentPage <= 1"
              @click="currentPage--"
            >
              {{ t('common.previous') }}
            </button>
            <span class="text-sm text-secondary">{{ currentPage }} / {{ totalPages || 1 }}</span>
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-3 py-1.5 text-sm transition-colors hover:bg-surface-muted disabled:opacity-50"
              :disabled="currentPage >= totalPages"
              @click="currentPage++"
            >
              {{ t('common.next') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
