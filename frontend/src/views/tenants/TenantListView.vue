<script setup lang="ts">
/**
 * Tenant List View — Super Admin Portal.
 *
 * Displays a paginated, filterable list of all government entities (tenants).
 * Data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useTenantStore } from '@/stores/tenant'
import { TenantStatus } from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const tenantStore = useTenantStore()

const {
  tenants,
  totalCount,
  totalPages,
  currentPage,
  pageSize,
  searchQuery,
  statusFilter,
  isLoading,
  error,
} = storeToRefs(tenantStore)

/** Status badge configuration */
function getStatusBadge(status: TenantStatus) {
  const config: Record<
    number,
    { labelKey: string; bgClass: string; textClass: string; icon: string }
  > = {
    [TenantStatus.PendingProvisioning]: {
      labelKey: 'tenants.statuses.pendingProvisioning',
      bgClass: 'bg-amber-50',
      textClass: 'text-amber-700',
      icon: 'pi pi-clock',
    },
    [TenantStatus.Active]: {
      labelKey: 'tenants.statuses.active',
      bgClass: 'bg-emerald-50',
      textClass: 'text-emerald-700',
      icon: 'pi pi-check-circle',
    },
    [TenantStatus.Suspended]: {
      labelKey: 'tenants.statuses.suspended',
      bgClass: 'bg-orange-50',
      textClass: 'text-orange-700',
      icon: 'pi pi-pause-circle',
    },
    [TenantStatus.Cancelled]: {
      labelKey: 'tenants.statuses.cancelled',
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
      icon: 'pi pi-times-circle',
    },
    [TenantStatus.Archived]: {
      labelKey: 'tenants.statuses.archived',
      bgClass: 'bg-slate-100',
      textClass: 'text-slate-600',
      icon: 'pi pi-inbox',
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
  { value: null, labelKey: 'tenants.filters.allStatuses' },
  { value: TenantStatus.PendingProvisioning, labelKey: 'tenants.statuses.pendingProvisioning' },
  { value: TenantStatus.Active, labelKey: 'tenants.statuses.active' },
  { value: TenantStatus.Suspended, labelKey: 'tenants.statuses.suspended' },
  { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
  { value: TenantStatus.Archived, labelKey: 'tenants.statuses.archived' },
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

/** Get tenant display name based on locale */
function getTenantName(item: { nameAr: string; nameEn: string }): string {
  return locale.value === 'ar' ? item.nameAr : item.nameEn
}

/** Calculate days until subscription expiry */
function getDaysUntilExpiry(dateStr: string | null): number | null {
  if (!dateStr) return null
  const now = new Date()
  const expiry = new Date(dateStr)
  const diff = expiry.getTime() - now.getTime()
  return Math.ceil(diff / (1000 * 60 * 60 * 24))
}

/** Get expiry badge style */
function getExpiryBadge(dateStr: string | null): {
  text: string
  bgClass: string
  textClass: string
} {
  const days = getDaysUntilExpiry(dateStr)
  if (days === null)
    return { text: '-', bgClass: '', textClass: 'text-tertiary' }
  if (days <= 0)
    return {
      text: t('tenants.expiry.expired'),
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
    }
  if (days <= 14)
    return {
      text: `${days} ${t('tenants.expiry.days')}`,
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
    }
  if (days <= 30)
    return {
      text: `${days} ${t('tenants.expiry.days')}`,
      bgClass: 'bg-orange-50',
      textClass: 'text-orange-700',
    }
  if (days <= 60)
    return {
      text: `${days} ${t('tenants.expiry.days')}`,
      bgClass: 'bg-amber-50',
      textClass: 'text-amber-700',
    }
  return {
    text: `${days} ${t('tenants.expiry.days')}`,
    bgClass: 'bg-emerald-50',
    textClass: 'text-emerald-700',
  }
}

/** Navigation */
function goToCreate() {
  router.push({ name: 'TenantCreate' })
}

function goToDetail(id: string) {
  router.push({ name: 'TenantDetail', params: { id } })
}

/** Watch filters */
watch([searchQuery, statusFilter], () => {
  tenantStore.resetAndReload()
})

watch(currentPage, () => {
  tenantStore.loadTenants()
})

onMounted(() => {
  tenantStore.loadTenants()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8 flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-secondary">
            {{ t('tenants.titles.list') }}
          </h1>
          <p class="mt-1 text-sm text-tertiary">
            {{ t('tenants.titles.listSubtitle') }}
          </p>
        </div>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-bold text-white shadow-md transition-all hover:bg-primary-dark hover:shadow-lg"
          @click="goToCreate"
        >
          <i class="pi pi-plus text-sm"></i>
          {{ t('tenants.actions.createNew') }}
        </button>
      </div>

      <!-- Filters -->
      <div
        class="mb-6 flex flex-wrap items-center gap-4 rounded-lg border border-surface-dim bg-white p-4"
      >
        <!-- Search -->
        <div class="relative min-w-[250px] flex-1">
          <i
            class="pi pi-search pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-sm text-tertiary"
          ></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('tenants.placeholders.search')"
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

        <!-- Count badge -->
        <span class="text-sm text-tertiary">
          {{ t('tenants.labels.totalTenants') }}: {{ totalCount }}
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
        <p class="text-sm text-danger">{{ error }}</p>
        <button
          type="button"
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="tenantStore.loadTenants()"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Empty state -->
      <div
        v-else-if="tenants.length === 0"
        class="rounded-lg border-2 border-dashed border-surface-dim p-16 text-center"
      >
        <i class="pi pi-building mb-4 text-5xl text-tertiary"></i>
        <h3 class="text-lg font-bold text-secondary">
          {{ t('tenants.messages.noTenants') }}
        </h3>
        <p class="mt-2 text-sm text-tertiary">
          {{ t('tenants.messages.noTenantsDesc') }}
        </p>
        <button
          type="button"
          class="mt-6 rounded-lg bg-primary px-6 py-2.5 text-sm font-bold text-white hover:bg-primary-dark"
          @click="goToCreate"
        >
          <i class="pi pi-plus me-2 text-sm"></i>
          {{ t('tenants.actions.createFirst') }}
        </button>
      </div>

      <!-- Table -->
      <div
        v-else
        class="overflow-hidden rounded-lg border border-surface-dim bg-white shadow-sm"
      >
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="border-b border-surface-dim bg-surface-muted">
              <tr>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.name') }}
                </th>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.identifier') }}
                </th>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.subdomain') }}
                </th>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.status') }}
                </th>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.provisioned') }}
                </th>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.subscriptionExpiry') }}
                </th>
                <th
                  class="px-4 py-3 text-start text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.createdAt') }}
                </th>
                <th
                  class="px-4 py-3 text-center text-xs font-medium text-tertiary"
                >
                  {{ t('tenants.tableHeaders.actions') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in tenants"
                :key="item.id"
                class="border-b border-surface-dim transition-colors hover:bg-surface-muted/50"
              >
                <!-- Name -->
                <td class="px-4 py-3">
                  <button
                    type="button"
                    class="text-sm font-medium text-primary hover:underline"
                    @click="goToDetail(item.id)"
                  >
                    {{ getTenantName(item) }}
                  </button>
                </td>

                <!-- Identifier -->
                <td class="px-4 py-3 text-sm text-secondary">
                  <code
                    class="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-mono"
                  >
                    {{ item.identifier }}
                  </code>
                </td>

                <!-- Subdomain -->
                <td class="px-4 py-3 text-sm text-tertiary" dir="ltr">
                  {{ item.subdomain }}.tendex.ai
                </td>

                <!-- Status -->
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium"
                    :class="[
                      getStatusBadge(item.status).bgClass,
                      getStatusBadge(item.status).textClass,
                    ]"
                  >
                    <i
                      :class="getStatusBadge(item.status).icon"
                      class="text-[10px]"
                    ></i>
                    {{ t(getStatusBadge(item.status).labelKey) }}
                  </span>
                </td>

                <!-- Provisioned -->
                <td class="px-4 py-3 text-center">
                  <i
                    v-if="item.isProvisioned"
                    class="pi pi-check-circle text-emerald-600"
                  ></i>
                  <i v-else class="pi pi-minus-circle text-slate-400"></i>
                </td>

                <!-- Subscription Expiry -->
                <td class="px-4 py-3">
                  <span
                    v-if="item.subscriptionExpiresAt"
                    class="inline-flex rounded-full px-2 py-0.5 text-xs font-medium"
                    :class="[
                      getExpiryBadge(item.subscriptionExpiresAt).bgClass,
                      getExpiryBadge(item.subscriptionExpiresAt).textClass,
                    ]"
                  >
                    {{ getExpiryBadge(item.subscriptionExpiresAt).text }}
                  </span>
                  <span v-else class="text-xs text-tertiary">-</span>
                </td>

                <!-- Created At -->
                <td class="px-4 py-3 text-sm text-tertiary">
                  {{ formatDate(item.createdAt) }}
                </td>

                <!-- Actions -->
                <td class="px-4 py-3 text-center">
                  <div class="flex items-center justify-center gap-1">
                    <button
                      type="button"
                      class="flex h-8 w-8 items-center justify-center rounded-lg transition-colors hover:bg-surface-muted"
                      :title="t('common.edit')"
                      @click="goToDetail(item.id)"
                    >
                      <i class="pi pi-eye text-sm text-primary"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div
          class="flex items-center justify-between border-t border-surface-dim px-4 py-3"
        >
          <span class="text-sm text-tertiary">
            {{ t('tenants.pagination.showing') }} {{ tenants.length }}
            {{ t('tenants.pagination.of') }} {{ totalCount }}
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
            <span class="text-sm text-secondary">
              {{ currentPage }} / {{ totalPages || 1 }}
            </span>
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
