<script setup lang="ts">
/**
 * Expiring Subscriptions Table — Operator Dashboard.
 *
 * Displays a table of tenants with subscriptions expiring within 90 days.
 * Color-coded severity based on days until expiry.
 *
 * All data from API — NO mock data.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ExpiringSubscriptionDto } from '@/types/operatorDashboard'

const props = defineProps<{
  subscriptions: ExpiringSubscriptionDto[]
  isLoading: boolean
}>()

const { t, locale } = useI18n()

function getSeverityClass(days: number): string {
  if (days <= 7) return 'bg-red-100 text-red-800'
  if (days <= 30) return 'bg-amber-100 text-amber-800'
  if (days <= 60) return 'bg-yellow-100 text-yellow-800'
  return 'bg-blue-100 text-blue-800'
}

function getSeverityLabel(days: number): string {
  if (days <= 7) return t('operatorDashboard.expiring.critical')
  if (days <= 30) return t('operatorDashboard.expiring.warning')
  if (days <= 60) return t('operatorDashboard.expiring.approaching')
  return t('operatorDashboard.expiring.info')
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

const tenantName = (sub: ExpiringSubscriptionDto): string => {
  return locale.value === 'ar' ? sub.tenantNameAr : sub.tenantNameEn
}

const hasData = computed(() => props.subscriptions.length > 0)
</script>

<template>
  <div class="rounded-lg border border-surface-dim bg-white p-6">
    <div class="mb-4 flex items-center justify-between">
      <h3 class="text-lg font-semibold text-secondary">
        {{ t('operatorDashboard.expiring.title') }}
      </h3>
      <span v-if="hasData && !isLoading" class="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-800">
        {{ subscriptions.length }} {{ t('operatorDashboard.expiring.count') }}
      </span>
    </div>

    <!-- Skeleton -->
    <div v-if="isLoading" class="animate-pulse space-y-3">
      <div v-for="i in 3" :key="i" class="flex items-center justify-between rounded-lg border border-surface-dim p-3">
        <div class="space-y-1">
          <div class="h-4 w-32 rounded bg-surface-dim"></div>
          <div class="h-3 w-20 rounded bg-surface-dim"></div>
        </div>
        <div class="h-6 w-20 rounded-full bg-surface-dim"></div>
      </div>
    </div>

    <!-- No data -->
    <div v-else-if="!hasData" class="flex h-40 items-center justify-center text-tertiary">
      <div class="text-center">
        <i class="pi pi-check-circle mb-2 text-3xl text-emerald-500"></i>
        <p>{{ t('operatorDashboard.expiring.noExpiring') }}</p>
      </div>
    </div>

    <!-- Table -->
    <div v-else class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-surface-dim text-start">
            <th class="pb-2 pe-4 text-start font-medium text-tertiary">
              {{ t('operatorDashboard.expiring.entity') }}
            </th>
            <th class="pb-2 pe-4 text-start font-medium text-tertiary">
              {{ t('operatorDashboard.expiring.expiryDate') }}
            </th>
            <th class="pb-2 pe-4 text-start font-medium text-tertiary">
              {{ t('operatorDashboard.expiring.daysLeft') }}
            </th>
            <th class="pb-2 text-start font-medium text-tertiary">
              {{ t('operatorDashboard.expiring.severity') }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="sub in subscriptions"
            :key="sub.tenantId"
            class="border-b border-surface-dim last:border-0"
          >
            <td class="py-3 pe-4">
              <p class="font-medium text-secondary">{{ tenantName(sub) }}</p>
              <p class="text-xs text-tertiary">{{ sub.tenantIdentifier }}</p>
            </td>
            <td class="py-3 pe-4 text-secondary">
              {{ formatDate(sub.subscriptionExpiresAt) }}
            </td>
            <td class="py-3 pe-4 font-medium text-secondary">
              {{ sub.daysUntilExpiry }}
            </td>
            <td class="py-3">
              <span :class="['rounded-full px-2 py-1 text-xs font-medium', getSeverityClass(sub.daysUntilExpiry)]">
                {{ getSeverityLabel(sub.daysUntilExpiry) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
