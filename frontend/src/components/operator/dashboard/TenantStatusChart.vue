<script setup lang="ts">
/**
 * Tenant Status Distribution Chart — Operator Dashboard.
 *
 * Displays a doughnut chart showing the distribution of tenants
 * across lifecycle statuses (Active, Suspended, Pending, etc.).
 *
 * Uses Chart.js via vue-chartjs. All data from API — NO mock data.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
  type ChartData,
  type ChartOptions,
} from 'chart.js'
import type { TenantStatusDistributionDto } from '@/types/operatorDashboard'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps<{
  distribution: TenantStatusDistributionDto[]
  isLoading: boolean
}>()

const { t } = useI18n()

/** Color mapping for tenant statuses. */
const statusColors: Record<string, string> = {
  PendingProvisioning: '#f59e0b',
  EnvironmentSetup: '#3b82f6',
  Training: '#8b5cf6',
  FinalAcceptance: '#06b6d4',
  Active: '#10b981',
  RenewalWindow: '#f97316',
  Suspended: '#ef4444',
  Cancelled: '#6b7280',
  Archived: '#94a3b8',
}

const chartData = computed<ChartData<'doughnut'>>(() => {
  const labels = props.distribution.map(
    (d) => t(`operatorDashboard.tenantStatus.${d.statusName}`, d.statusName),
  )
  const data = props.distribution.map((d) => d.count)
  const backgroundColor = props.distribution.map(
    (d) => statusColors[d.statusName] ?? '#94a3b8',
  )

  return {
    labels,
    datasets: [
      {
        data,
        backgroundColor,
        borderWidth: 2,
        borderColor: '#ffffff',
        hoverOffset: 8,
      },
    ],
  }
})

const chartOptions = computed<ChartOptions<'doughnut'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '60%',
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        padding: 16,
        usePointStyle: true,
        pointStyleWidth: 10,
        font: { size: 12 },
      },
    },
    tooltip: {
      callbacks: {
        label: (context) => {
          const value = context.parsed
          const total = (context.dataset.data as number[]).reduce((a, b) => a + b, 0)
          const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0'
          return ` ${context.label}: ${value} (${percentage}%)`
        },
      },
    },
  },
}))

const hasData = computed(() => props.distribution.length > 0)
</script>

<template>
  <div class="rounded-lg border border-surface-dim bg-white p-6">
    <h3 class="mb-4 text-lg font-semibold text-secondary">
      {{ t('operatorDashboard.charts.tenantStatusDistribution') }}
    </h3>

    <!-- Skeleton -->
    <div v-if="isLoading" class="flex h-64 items-center justify-center">
      <div class="animate-pulse">
        <div class="mx-auto h-48 w-48 rounded-full bg-surface-dim"></div>
      </div>
    </div>

    <!-- No data -->
    <div v-else-if="!hasData" class="flex h-64 items-center justify-center text-tertiary">
      <div class="text-center">
        <i class="pi pi-chart-pie mb-2 text-3xl"></i>
        <p>{{ t('operatorDashboard.noData') }}</p>
      </div>
    </div>

    <!-- Chart -->
    <div v-else class="h-72">
      <Doughnut :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>
