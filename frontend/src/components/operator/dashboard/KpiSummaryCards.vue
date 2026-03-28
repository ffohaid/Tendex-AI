<script setup lang="ts">
/**
 * KPI Summary Cards Component — Operator Dashboard.
 *
 * Displays high-level KPI cards for the Super Admin dashboard:
 * - Total tenants, active tenants, suspended, pending provisioning
 * - Active subscriptions, feature flags, AI configs, audit logs
 *
 * All data is fetched dynamically from the API — NO mock data.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { OperatorDashboardSummaryDto } from '@/types/operatorDashboard'

const props = defineProps<{
  summary: OperatorDashboardSummaryDto | null
  isLoading: boolean
}>()

const { t, locale } = useI18n()
const router = useRouter()

interface KpiCard {
  key: string
  value: number
  icon: string
  iconBg: string
  iconColor: string
  valueColor: string
  route?: string
}

const kpiCards = computed<KpiCard[]>(() => {
  const s = props.summary
  return [
    {
      key: 'totalTenants',
      value: s?.totalTenants ?? 0,
      icon: 'pi pi-building',
      iconBg: 'bg-primary/10',
      iconColor: 'text-primary',
      valueColor: 'text-secondary',
      route: 'TenantList',
    },
    {
      key: 'activeTenants',
      value: s?.activeTenants ?? 0,
      icon: 'pi pi-check-circle',
      iconBg: 'bg-emerald-50',
      iconColor: 'text-emerald-600',
      valueColor: 'text-emerald-600',
    },
    {
      key: 'suspendedTenants',
      value: s?.suspendedTenants ?? 0,
      icon: 'pi pi-ban',
      iconBg: 'bg-red-50',
      iconColor: 'text-red-600',
      valueColor: 'text-red-600',
    },
    {
      key: 'pendingProvisioning',
      value: s?.pendingProvisioningTenants ?? 0,
      icon: 'pi pi-clock',
      iconBg: 'bg-amber-50',
      iconColor: 'text-amber-600',
      valueColor: 'text-amber-600',
    },
    {
      key: 'activeSubscriptions',
      value: s?.totalActiveSubscriptions ?? 0,
      icon: 'pi pi-credit-card',
      iconBg: 'bg-blue-50',
      iconColor: 'text-blue-600',
      valueColor: 'text-blue-600',
    },
    {
      key: 'featureFlags',
      value: s?.totalFeatureFlags ?? 0,
      icon: 'pi pi-flag',
      iconBg: 'bg-purple-50',
      iconColor: 'text-purple-600',
      valueColor: 'text-purple-600',
    },
    {
      key: 'aiConfigurations',
      value: s?.totalAiConfigurations ?? 0,
      icon: 'pi pi-microchip-ai',
      iconBg: 'bg-indigo-50',
      iconColor: 'text-indigo-600',
      valueColor: 'text-indigo-600',
    },
    {
      key: 'auditLogEntries',
      value: s?.totalAuditLogEntries ?? 0,
      icon: 'pi pi-history',
      iconBg: 'bg-slate-100',
      iconColor: 'text-slate-600',
      valueColor: 'text-slate-600',
    },
  ]
})

function formatNumber(value: number): string {
  return new Intl.NumberFormat(locale.value === 'ar' ? 'en-US' : 'en-US').format(value)
}

function handleCardClick(card: KpiCard): void {
  if (card.route) {
    router.push({ name: card.route })
  }
}
</script>

<template>
  <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
    <div
      v-for="card in kpiCards"
      :key="card.key"
      :class="[
        'rounded-lg border border-surface-dim bg-white p-5 transition-shadow',
        card.route ? 'cursor-pointer hover:shadow-md' : '',
      ]"
      @click="handleCardClick(card)"
    >
      <!-- Skeleton loading -->
      <div v-if="isLoading" class="animate-pulse">
        <div class="flex items-center justify-between">
          <div>
            <div class="h-4 w-24 rounded bg-surface-dim"></div>
            <div class="mt-2 h-8 w-16 rounded bg-surface-dim"></div>
          </div>
          <div class="h-12 w-12 rounded-lg bg-surface-dim"></div>
        </div>
      </div>

      <!-- Actual content -->
      <div v-else class="flex items-center justify-between">
        <div>
          <p class="text-sm text-tertiary">
            {{ t(`operatorDashboard.kpi.${card.key}`) }}
          </p>
          <p :class="['mt-1 text-2xl font-bold', card.valueColor]">
            {{ formatNumber(card.value) }}
          </p>
        </div>
        <div :class="['flex h-12 w-12 items-center justify-center rounded-lg', card.iconBg]">
          <i :class="[card.icon, 'text-xl', card.iconColor]"></i>
        </div>
      </div>
    </div>
  </div>
</template>
