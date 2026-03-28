<script setup lang="ts">
/**
 * Resource Consumption Trends Charts — Operator Dashboard.
 *
 * Displays multiple charts:
 * - Daily audit log activity (line chart)
 * - Feature adoption rates (horizontal bar chart)
 * - AI provider usage (doughnut chart)
 *
 * Uses Chart.js via vue-chartjs. All data from API — NO mock data.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Line, Bar, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  type ChartData,
  type ChartOptions,
} from 'chart.js'
import type { ResourceConsumptionTrendsDto } from '@/types/operatorDashboard'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler,
)

const props = defineProps<{
  trends: ResourceConsumptionTrendsDto | null
  isLoading: boolean
}>()

const { t, locale } = useI18n()

/* ------------------------------------------------------------------ */
/*  Daily Audit Log Activity (Line Chart)                              */
/* ------------------------------------------------------------------ */

function formatDateLabel(dateStr: string): string {
  try {
    const date = new Date(dateStr)
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  } catch {
    return dateStr
  }
}

const auditLogChartData = computed<ChartData<'line'>>(() => {
  const entries = props.trends?.dailyAuditLogEntries ?? []
  return {
    labels: entries.map((d) => formatDateLabel(d.date)),
    datasets: [
      {
        label: t('operatorDashboard.trends.auditLogEntries'),
        data: entries.map((d) => d.count),
        borderColor: 'rgba(99, 102, 241, 1)',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        fill: true,
        tension: 0.4,
        pointRadius: 2,
        pointHoverRadius: 6,
      },
    ],
  }
})

const auditLogChartOptions = computed<ChartOptions<'line'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: { precision: 0 },
      grid: { color: 'rgba(0, 0, 0, 0.05)' },
    },
    x: {
      grid: { display: false },
      ticks: { maxTicksLimit: 10 },
    },
  },
}))

/* ------------------------------------------------------------------ */
/*  Feature Adoption Rates (Horizontal Bar Chart)                      */
/* ------------------------------------------------------------------ */

const featureAdoptionChartData = computed<ChartData<'bar'>>(() => {
  const features = props.trends?.featureAdoptionRates ?? []
  const labels = features.map((f) =>
    locale.value === 'ar' ? f.featureNameAr : f.featureNameEn,
  )
  return {
    labels,
    datasets: [
      {
        label: t('operatorDashboard.trends.adoptionRate'),
        data: features.map((f) => f.adoptionRate),
        backgroundColor: 'rgba(16, 185, 129, 0.7)',
        borderColor: 'rgba(16, 185, 129, 1)',
        borderWidth: 1,
        borderRadius: 4,
      },
    ],
  }
})

const featureAdoptionChartOptions = computed<ChartOptions<'bar'>>(() => ({
  indexAxis: 'y',
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (context) => ` ${context.parsed.x}%`,
      },
    },
  },
  scales: {
    x: {
      beginAtZero: true,
      max: 100,
      ticks: {
        callback: (value) => `${value}%`,
      },
      grid: { color: 'rgba(0, 0, 0, 0.05)' },
    },
    y: {
      grid: { display: false },
    },
  },
}))

/* ------------------------------------------------------------------ */
/*  AI Provider Usage (Doughnut Chart)                                 */
/* ------------------------------------------------------------------ */

const aiProviderColors = [
  'rgba(59, 130, 246, 0.8)',
  'rgba(16, 185, 129, 0.8)',
  'rgba(245, 158, 11, 0.8)',
  'rgba(139, 92, 246, 0.8)',
  'rgba(239, 68, 68, 0.8)',
  'rgba(6, 182, 212, 0.8)',
]

const aiProviderChartData = computed<ChartData<'doughnut'>>(() => {
  const providers = props.trends?.aiProviderUsage ?? []
  return {
    labels: providers.map((p) => p.providerName),
    datasets: [
      {
        data: providers.map((p) => p.configurationsCount),
        backgroundColor: providers.map((_, i) => aiProviderColors[i % aiProviderColors.length]),
        borderWidth: 2,
        borderColor: '#ffffff',
      },
    ],
  }
})

const aiProviderChartOptions = computed<ChartOptions<'doughnut'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '55%',
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        padding: 12,
        usePointStyle: true,
        font: { size: 11 },
      },
    },
  },
}))

const hasAuditData = computed(() => (props.trends?.dailyAuditLogEntries?.length ?? 0) > 0)
const hasFeatureData = computed(() => (props.trends?.featureAdoptionRates?.length ?? 0) > 0)
const hasAiData = computed(() => (props.trends?.aiProviderUsage?.length ?? 0) > 0)
</script>

<template>
  <div class="space-y-6">
    <!-- Daily Audit Log Activity -->
    <div class="rounded-lg border border-surface-dim bg-white p-6">
      <h3 class="mb-4 text-lg font-semibold text-secondary">
        {{ t('operatorDashboard.trends.dailyAuditActivity') }}
      </h3>
      <div v-if="isLoading" class="flex h-56 items-center justify-center">
        <div class="animate-pulse h-40 w-full rounded bg-surface-dim"></div>
      </div>
      <div v-else-if="!hasAuditData" class="flex h-56 items-center justify-center text-tertiary">
        <div class="text-center">
          <i class="pi pi-chart-line mb-2 text-3xl"></i>
          <p>{{ t('operatorDashboard.noData') }}</p>
        </div>
      </div>
      <div v-else class="h-56">
        <Line :data="auditLogChartData" :options="auditLogChartOptions" />
      </div>
    </div>

    <!-- Feature Adoption & AI Provider Usage -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <!-- Feature Adoption Rates -->
      <div class="rounded-lg border border-surface-dim bg-white p-6">
        <h3 class="mb-4 text-lg font-semibold text-secondary">
          {{ t('operatorDashboard.trends.featureAdoption') }}
        </h3>
        <div v-if="isLoading" class="flex h-56 items-center justify-center">
          <div class="animate-pulse h-40 w-full rounded bg-surface-dim"></div>
        </div>
        <div v-else-if="!hasFeatureData" class="flex h-56 items-center justify-center text-tertiary">
          <div class="text-center">
            <i class="pi pi-flag mb-2 text-3xl"></i>
            <p>{{ t('operatorDashboard.noData') }}</p>
          </div>
        </div>
        <div v-else class="h-56">
          <Bar :data="featureAdoptionChartData" :options="featureAdoptionChartOptions" />
        </div>
      </div>

      <!-- AI Provider Usage -->
      <div class="rounded-lg border border-surface-dim bg-white p-6">
        <h3 class="mb-4 text-lg font-semibold text-secondary">
          {{ t('operatorDashboard.trends.aiProviderUsage') }}
        </h3>
        <div v-if="isLoading" class="flex h-56 items-center justify-center">
          <div class="animate-pulse h-40 w-40 mx-auto rounded-full bg-surface-dim"></div>
        </div>
        <div v-else-if="!hasAiData" class="flex h-56 items-center justify-center text-tertiary">
          <div class="text-center">
            <i class="pi pi-microchip-ai mb-2 text-3xl"></i>
            <p>{{ t('operatorDashboard.noData') }}</p>
          </div>
        </div>
        <div v-else class="h-56">
          <Doughnut :data="aiProviderChartData" :options="aiProviderChartOptions" />
        </div>
      </div>
    </div>
  </div>
</template>
