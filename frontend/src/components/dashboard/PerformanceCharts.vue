<script setup lang="ts">
/**
 * PerformanceCharts Component
 *
 * Displays interactive charts for dashboard performance metrics:
 * - Monthly competitions bar chart
 * - Competition status distribution doughnut chart
 * - KPI summary cards for cycle time, compliance, evaluation time
 *
 * Uses Chart.js via vue-chartjs for interactive visualizations.
 * All data is fetched dynamically from the API.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Bar, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  type ChartData,
  type ChartOptions,
} from 'chart.js'
import { useFormatters } from '@/composables/useFormatters'
import type { PerformanceMetrics } from '@/types/dashboard'

/* Register Chart.js components */
ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
)

const props = defineProps<{
  metrics: PerformanceMetrics
  isLoading: boolean
}>()

const { t } = useI18n()
const { formatNumber, formatPercentage } = useFormatters()

/* ── Monthly Competitions Bar Chart ─────── */
const barChartData = computed<ChartData<'bar'>>(() => ({
  labels: props.metrics.monthlyCompetitions.map((m) => m.month),
  datasets: [
    {
      label: t('dashboard.charts.monthlyCompetitions'),
      data: props.metrics.monthlyCompetitions.map((m) => m.count),
      backgroundColor: 'rgba(43, 182, 115, 0.8)',
      borderColor: '#2BB673',
      borderWidth: 1,
      borderRadius: 6,
      maxBarThickness: 40,
    },
  ],
}))

const barChartOptions = computed<ChartOptions<'bar'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: false,
    },
    tooltip: {
      backgroundColor: '#0D2745',
      titleFont: { size: 12 },
      bodyFont: { size: 11 },
      padding: 10,
      cornerRadius: 8,
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: {
        font: { size: 11 },
        color: '#255877',
      },
    },
    y: {
      beginAtZero: true,
      grid: {
        color: 'rgba(219, 219, 219, 0.5)',
      },
      ticks: {
        font: { size: 11 },
        color: '#255877',
        stepSize: 1,
      },
    },
  },
  animation: {
    duration: 800,
    easing: 'easeOutQuart',
  },
}))

/* ── Status Distribution Doughnut Chart ─── */
const statusColors: Record<string, string> = {
  draft: '#9CA3AF',
  published: '#3B82F6',
  receiving_offers: '#2BB673',
  technical_evaluation: '#F59E0B',
  financial_evaluation: '#F97316',
  awarding: '#8B5CF6',
  completed: '#10B981',
  cancelled: '#EF4444',
}

const doughnutChartData = computed<ChartData<'doughnut'>>(() => ({
  labels: props.metrics.statusDistribution.map((s) =>
    t(`dashboard.competitionStatus.${s.status}`),
  ),
  datasets: [
    {
      data: props.metrics.statusDistribution.map((s) => s.count),
      backgroundColor: props.metrics.statusDistribution.map(
        (s) => statusColors[s.status] ?? '#9CA3AF',
      ),
      borderWidth: 2,
      borderColor: '#FFFFFF',
      hoverOffset: 8,
    },
  ],
}))

const doughnutChartOptions = computed<ChartOptions<'doughnut'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '65%',
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        padding: 12,
        usePointStyle: true,
        pointStyleWidth: 8,
        font: { size: 11 },
        color: '#255877',
      },
    },
    tooltip: {
      backgroundColor: '#0D2745',
      titleFont: { size: 12 },
      bodyFont: { size: 11 },
      padding: 10,
      cornerRadius: 8,
    },
  },
  animation: {
    duration: 800,
    easing: 'easeOutQuart',
  },
}))

/* ── KPI Summary Cards ──────────────────── */
const kpiCards = computed(() => [
  {
    key: 'avgCycleTime',
    icon: 'pi-stopwatch',
    color: 'text-primary',
    bgColor: 'bg-primary/10',
    value: `${formatNumber(props.metrics.averageCycleTimeDays)} ${t('dashboard.charts.days')}`,
    label: t('dashboard.charts.avgCycleTime'),
  },
  {
    key: 'complianceRate',
    icon: 'pi-shield',
    color: 'text-success',
    bgColor: 'bg-success/10',
    value: formatPercentage(props.metrics.complianceRate),
    label: t('dashboard.charts.complianceRate'),
  },
  {
    key: 'avgEvalTime',
    icon: 'pi-clock',
    color: 'text-warning',
    bgColor: 'bg-warning/10',
    value: `${formatNumber(props.metrics.averageEvaluationTimeDays)} ${t('dashboard.charts.days')}`,
    label: t('dashboard.charts.avgEvalTime'),
  },
  {
    key: 'slaCompliance',
    icon: 'pi-check-square',
    color: 'text-info',
    bgColor: 'bg-info/10',
    value: formatPercentage(props.metrics.slaComplianceRate),
    label: t('dashboard.charts.slaCompliance'),
  },
])
</script>

<template>
  <div class="space-y-6">
    <!-- KPI Summary Row -->
    <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
      <div
        v-for="kpi in kpiCards"
        :key="kpi.key"
        class="card flex items-center gap-3 !p-4"
      >
        <template v-if="isLoading">
          <div class="h-10 w-10 animate-pulse rounded-lg bg-surface-dim"></div>
          <div class="flex-1">
            <div class="mb-1 h-3 w-16 animate-pulse rounded bg-surface-dim"></div>
            <div class="h-5 w-12 animate-pulse rounded bg-surface-muted"></div>
          </div>
        </template>
        <template v-else>
          <div
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
            :class="kpi.bgColor"
          >
            <i class="pi text-base" :class="[kpi.icon, kpi.color]"></i>
          </div>
          <div>
            <p class="text-[11px] text-tertiary">{{ kpi.label }}</p>
            <p class="text-lg font-bold text-secondary">{{ kpi.value }}</p>
          </div>
        </template>
      </div>
    </div>

    <!-- Charts Row -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <!-- Monthly Competitions Bar Chart -->
      <div class="card">
        <h3 class="mb-4 text-base font-semibold text-secondary">
          {{ t('dashboard.charts.monthlyCompetitions') }}
        </h3>
        <div v-if="isLoading" class="flex h-[250px] items-center justify-center">
          <div class="h-full w-full animate-pulse rounded-lg bg-surface-muted"></div>
        </div>
        <div v-else-if="metrics.monthlyCompetitions.length === 0" class="flex h-[250px] items-center justify-center">
          <p class="text-sm text-tertiary">{{ t('dashboard.noChartData') }}</p>
        </div>
        <div v-else class="h-[250px]">
          <Bar :data="barChartData" :options="barChartOptions" />
        </div>
      </div>

      <!-- Status Distribution Doughnut Chart -->
      <div class="card">
        <h3 class="mb-4 text-base font-semibold text-secondary">
          {{ t('dashboard.charts.statusDistribution') }}
        </h3>
        <div v-if="isLoading" class="flex h-[250px] items-center justify-center">
          <div class="h-full w-full animate-pulse rounded-lg bg-surface-muted"></div>
        </div>
        <div v-else-if="metrics.statusDistribution.length === 0" class="flex h-[250px] items-center justify-center">
          <p class="text-sm text-tertiary">{{ t('dashboard.noChartData') }}</p>
        </div>
        <div v-else class="h-[250px]">
          <Doughnut :data="doughnutChartData" :options="doughnutChartOptions" />
        </div>
      </div>
    </div>
  </div>
</template>
