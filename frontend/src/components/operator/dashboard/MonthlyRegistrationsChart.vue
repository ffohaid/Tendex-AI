<script setup lang="ts">
/**
 * Monthly Tenant Registrations Chart — Operator Dashboard.
 *
 * Displays a bar chart showing tenant registration trends
 * over the last 12 months.
 *
 * Uses Chart.js via vue-chartjs. All data from API — NO mock data.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  type ChartData,
  type ChartOptions,
} from 'chart.js'
import type { MonthlyTrendDto } from '@/types/operatorDashboard'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const props = defineProps<{
  data: MonthlyTrendDto[]
  isLoading: boolean
}>()

const { t } = useI18n()

/** Format month label from yyyy-MM to short month name. */
function formatMonthLabel(month: string): string {
  try {
    const [year, m] = month.split('-')
    const date = new Date(Number(year), Number(m) - 1)
    return date.toLocaleDateString('en-US', { month: 'short', year: '2-digit' })
  } catch {
    return month
  }
}

const chartData = computed<ChartData<'bar'>>(() => ({
  labels: props.data.map((d) => formatMonthLabel(d.month)),
  datasets: [
    {
      label: t('operatorDashboard.charts.newRegistrations'),
      data: props.data.map((d) => d.count),
      backgroundColor: 'rgba(59, 130, 246, 0.7)',
      borderColor: 'rgba(59, 130, 246, 1)',
      borderWidth: 1,
      borderRadius: 4,
      maxBarThickness: 40,
    },
  ],
}))

const chartOptions = computed<ChartOptions<'bar'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (context) => ` ${t('operatorDashboard.charts.registrations')}: ${context.parsed.y}`,
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        stepSize: 1,
        precision: 0,
      },
      grid: { color: 'rgba(0, 0, 0, 0.05)' },
    },
    x: {
      grid: { display: false },
    },
  },
}))

const hasData = computed(() => props.data.length > 0)
</script>

<template>
  <div class="rounded-lg border border-surface-dim bg-white p-6">
    <h3 class="mb-4 text-lg font-semibold text-secondary">
      {{ t('operatorDashboard.charts.monthlyRegistrations') }}
    </h3>

    <!-- Skeleton -->
    <div v-if="isLoading" class="flex h-64 items-center justify-center">
      <div class="animate-pulse space-y-2">
        <div class="flex items-end gap-2">
          <div v-for="i in 8" :key="i" class="w-8 rounded bg-surface-dim" :style="{ height: `${Math.random() * 120 + 40}px` }"></div>
        </div>
      </div>
    </div>

    <!-- No data -->
    <div v-else-if="!hasData" class="flex h-64 items-center justify-center text-tertiary">
      <div class="text-center">
        <i class="pi pi-chart-bar mb-2 text-3xl"></i>
        <p>{{ t('operatorDashboard.noData') }}</p>
      </div>
    </div>

    <!-- Chart -->
    <div v-else class="h-72">
      <Bar :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>
