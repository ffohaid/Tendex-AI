<script setup lang="ts">
/**
 * Tenant Usage Statistics Table — Operator Dashboard.
 *
 * Displays a paginated, searchable table of per-tenant usage statistics
 * including feature flags, AI configs, and audit log entries.
 *
 * All data from API — NO mock data.
 */
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useOperatorDashboardStore } from '@/stores/operatorDashboard'

const { t, locale } = useI18n()
const store = useOperatorDashboardStore()
const {
  tenantUsageItems,
  tenantUsageTotalCount,
  tenantUsageTotalPages,
  tenantUsageCurrentPage,
  tenantUsagePageSize,
  isLoadingTenantUsage,
} = storeToRefs(store)

const searchInput = ref('')
let searchTimeout: ReturnType<typeof setTimeout> | null = null

/** Debounced search. */
function onSearchInput(): void {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    store.searchTenantUsage(searchInput.value)
  }, 400)
}

/** Page change handler. */
function onPageChange(page: number): void {
  store.changeTenantUsagePage(page)
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  try {
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  } catch {
    return dateStr
  }
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('en-US').format(value)
}

function getStatusBadgeClass(status: string): string {
  const map: Record<string, string> = {
    Active: 'bg-emerald-100 text-emerald-800',
    Suspended: 'bg-red-100 text-red-800',
    PendingProvisioning: 'bg-amber-100 text-amber-800',
    EnvironmentSetup: 'bg-blue-100 text-blue-800',
    Training: 'bg-purple-100 text-purple-800',
    FinalAcceptance: 'bg-cyan-100 text-cyan-800',
    RenewalWindow: 'bg-orange-100 text-orange-800',
    Cancelled: 'bg-gray-100 text-gray-800',
    Archived: 'bg-slate-100 text-slate-800',
  }
  return map[status] ?? 'bg-gray-100 text-gray-800'
}

/** Generate page numbers for pagination. */
function getPageNumbers(): number[] {
  const total = tenantUsageTotalPages.value
  const current = tenantUsageCurrentPage.value
  const pages: number[] = []
  const maxVisible = 5

  if (total <= maxVisible) {
    for (let i = 1; i <= total; i++) pages.push(i)
  } else {
    const start = Math.max(1, current - 2)
    const end = Math.min(total, start + maxVisible - 1)
    for (let i = start; i <= end; i++) pages.push(i)
  }
  return pages
}
</script>

<template>
  <div class="rounded-lg border border-surface-dim bg-white p-6">
    <div class="mb-4 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <h3 class="text-lg font-semibold text-secondary">
        {{ t('operatorDashboard.tenantUsage.title') }}
      </h3>

      <!-- Search -->
      <div class="relative">
        <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-tertiary"></i>
        <input
          v-model="searchInput"
          type="text"
          :placeholder="t('operatorDashboard.tenantUsage.searchPlaceholder')"
          class="w-full rounded-lg border border-surface-dim bg-white py-2 pe-4 ps-10 text-sm text-secondary placeholder-tertiary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary sm:w-64"
          @input="onSearchInput"
        />
      </div>
    </div>

    <!-- Skeleton -->
    <div v-if="isLoadingTenantUsage" class="animate-pulse space-y-3">
      <div v-for="i in 5" :key="i" class="flex items-center gap-4 rounded-lg border border-surface-dim p-3">
        <div class="h-4 w-32 rounded bg-surface-dim"></div>
        <div class="h-4 w-20 rounded bg-surface-dim"></div>
        <div class="h-4 w-16 rounded bg-surface-dim"></div>
        <div class="h-4 w-16 rounded bg-surface-dim"></div>
      </div>
    </div>

    <!-- No data -->
    <div v-else-if="tenantUsageItems.length === 0" class="flex h-40 items-center justify-center text-tertiary">
      <div class="text-center">
        <i class="pi pi-table mb-2 text-3xl"></i>
        <p>{{ t('operatorDashboard.noData') }}</p>
      </div>
    </div>

    <!-- Table -->
    <div v-else>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-surface-dim">
              <th class="pb-3 pe-4 text-start font-medium text-tertiary">
                {{ t('operatorDashboard.tenantUsage.entity') }}
              </th>
              <th class="pb-3 pe-4 text-start font-medium text-tertiary">
                {{ t('operatorDashboard.tenantUsage.status') }}
              </th>
              <th class="pb-3 pe-4 text-center font-medium text-tertiary">
                {{ t('operatorDashboard.tenantUsage.featureFlags') }}
              </th>
              <th class="pb-3 pe-4 text-center font-medium text-tertiary">
                {{ t('operatorDashboard.tenantUsage.aiConfigs') }}
              </th>
              <th class="pb-3 pe-4 text-center font-medium text-tertiary">
                {{ t('operatorDashboard.tenantUsage.auditLogs') }}
              </th>
              <th class="pb-3 text-start font-medium text-tertiary">
                {{ t('operatorDashboard.tenantUsage.subscriptionExpiry') }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in tenantUsageItems"
              :key="item.tenantId"
              class="border-b border-surface-dim last:border-0 hover:bg-surface-muted/50"
            >
              <td class="py-3 pe-4">
                <p class="font-medium text-secondary">
                  {{ locale === 'ar' ? item.tenantNameAr : item.tenantNameEn }}
                </p>
                <p class="text-xs text-tertiary">{{ item.tenantIdentifier }}</p>
              </td>
              <td class="py-3 pe-4">
                <span :class="['rounded-full px-2 py-1 text-xs font-medium', getStatusBadgeClass(item.statusName)]">
                  {{ t(`operatorDashboard.tenantStatus.${item.statusName}`, item.statusName) }}
                </span>
              </td>
              <td class="py-3 pe-4 text-center">
                <span class="font-medium text-secondary">{{ item.activeFeatureFlags }}</span>
                <span class="text-tertiary"> / {{ item.totalFeatureFlags }}</span>
              </td>
              <td class="py-3 pe-4 text-center font-medium text-secondary">
                {{ item.aiConfigurationsCount }}
              </td>
              <td class="py-3 pe-4 text-center font-medium text-secondary">
                {{ formatNumber(item.auditLogEntriesCount) }}
              </td>
              <td class="py-3 text-secondary">
                {{ formatDate(item.subscriptionExpiresAt) }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="tenantUsageTotalPages > 1" class="mt-4 flex items-center justify-between">
        <p class="text-xs text-tertiary">
          {{ t('operatorDashboard.tenantUsage.showing') }}
          {{ (tenantUsageCurrentPage - 1) * tenantUsagePageSize + 1 }}-{{ Math.min(tenantUsageCurrentPage * tenantUsagePageSize, tenantUsageTotalCount) }}
          {{ t('operatorDashboard.tenantUsage.of') }}
          {{ tenantUsageTotalCount }}
        </p>
        <div class="flex gap-1">
          <button
            :disabled="tenantUsageCurrentPage <= 1"
            class="rounded px-3 py-1 text-sm text-secondary hover:bg-surface-muted disabled:opacity-40"
            @click="onPageChange(tenantUsageCurrentPage - 1)"
          >
            <i class="pi pi-chevron-left text-xs"></i>
          </button>
          <button
            v-for="page in getPageNumbers()"
            :key="page"
            :class="[
              'rounded px-3 py-1 text-sm',
              page === tenantUsageCurrentPage
                ? 'bg-primary text-white'
                : 'text-secondary hover:bg-surface-muted',
            ]"
            @click="onPageChange(page)"
          >
            {{ page }}
          </button>
          <button
            :disabled="tenantUsageCurrentPage >= tenantUsageTotalPages"
            class="rounded px-3 py-1 text-sm text-secondary hover:bg-surface-muted disabled:opacity-40"
            @click="onPageChange(tenantUsageCurrentPage + 1)"
          >
            <i class="pi pi-chevron-right text-xs"></i>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
