<script setup lang="ts">
/**
 * ReportsView — Reports & Analytics Page.
 *
 * TASK-904: Displays comprehensive reports and analytics for competitions,
 * evaluations, and compliance metrics.
 *
 * Features:
 * - KPI summary cards
 * - Monthly trends bar chart (competitions & offers)
 * - Competition status distribution doughnut chart
 * - Department performance table
 * - Date range and status filters
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 *
 * All data is fetched dynamically from the API (no mock data).
 */
import { ref, computed, onMounted } from 'vue'
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
import { fetchReportData } from '@/services/reportService'
import type {
  ReportData,
  ReportFilters,
  ReportSummary,
  MonthlyTrendItem,
  StatusDistributionItem,
  DepartmentPerformance,
} from '@/types/reports'

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

const { t, locale } = useI18n()
const { formatNumber, formatCurrency, formatPercentage } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const isLoading = ref(false)
const error = ref('')

const summary = ref<ReportSummary>({
  totalCompetitions: 0,
  totalOffers: 0,
  averageCycleTimeDays: 0,
  complianceRate: 0,
  totalBudget: 0,
  completedCompetitions: 0,
  cancelledCompetitions: 0,
  averageOffersPerCompetition: 0,
})

const monthlyTrends = ref<MonthlyTrendItem[]>([])
const statusDistribution = ref<StatusDistributionItem[]>([])
const departmentPerformance = ref<DepartmentPerformance[]>([])

/* Filters */
const filters = ref<ReportFilters>({
  dateFrom: null,
  dateTo: null,
  status: null,
  department: null,
})

const isRtl = computed(() => locale.value === 'ar')

/* ------------------------------------------------------------------ */
/*  KPI Cards Configuration                                            */
/* ------------------------------------------------------------------ */
const kpiCards = computed(() => [
  {
    key: 'totalCompetitions',
    icon: 'pi-file-edit',
    color: 'text-primary',
    bgColor: 'bg-primary/10',
    value: formatNumber(summary.value.totalCompetitions),
    label: t('reports.kpi.totalCompetitions'),
  },
  {
    key: 'totalOffers',
    icon: 'pi-inbox',
    color: 'text-info',
    bgColor: 'bg-info/10',
    value: formatNumber(summary.value.totalOffers),
    label: t('reports.kpi.totalOffers'),
  },
  {
    key: 'avgCycleTime',
    icon: 'pi-stopwatch',
    color: 'text-warning',
    bgColor: 'bg-warning/10',
    value: `${formatNumber(summary.value.averageCycleTimeDays)} ${t('reports.days')}`,
    label: t('reports.kpi.avgCycleTime'),
  },
  {
    key: 'complianceRate',
    icon: 'pi-shield',
    color: 'text-success',
    bgColor: 'bg-success/10',
    value: formatPercentage(summary.value.complianceRate),
    label: t('reports.kpi.complianceRate'),
  },
  {
    key: 'totalBudget',
    icon: 'pi-wallet',
    color: 'text-primary',
    bgColor: 'bg-primary/10',
    value: formatCurrency(summary.value.totalBudget),
    label: t('reports.kpi.totalBudget'),
  },
  {
    key: 'completed',
    icon: 'pi-check-circle',
    color: 'text-success',
    bgColor: 'bg-success/10',
    value: formatNumber(summary.value.completedCompetitions),
    label: t('reports.kpi.completed'),
  },
  {
    key: 'cancelled',
    icon: 'pi-times-circle',
    color: 'text-danger',
    bgColor: 'bg-danger/10',
    value: formatNumber(summary.value.cancelledCompetitions),
    label: t('reports.kpi.cancelled'),
  },
  {
    key: 'avgOffers',
    icon: 'pi-chart-bar',
    color: 'text-info',
    bgColor: 'bg-info/10',
    value: formatNumber(summary.value.averageOffersPerCompetition, 1),
    label: t('reports.kpi.avgOffers'),
  },
])

/* ------------------------------------------------------------------ */
/*  Chart Data                                                         */
/* ------------------------------------------------------------------ */
const barChartData = computed<ChartData<'bar'>>(() => ({
  labels: monthlyTrends.value.map((m) => m.month),
  datasets: [
    {
      label: t('reports.charts.competitions'),
      data: monthlyTrends.value.map((m) => m.competitions),
      backgroundColor: 'rgba(43, 182, 115, 0.8)',
      borderColor: '#2BB673',
      borderWidth: 1,
      borderRadius: 6,
      maxBarThickness: 40,
    },
    {
      label: t('reports.charts.offers'),
      data: monthlyTrends.value.map((m) => m.offers),
      backgroundColor: 'rgba(59, 130, 246, 0.8)',
      borderColor: '#3B82F6',
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
      position: 'top',
      labels: {
        padding: 16,
        usePointStyle: true,
        pointStyleWidth: 8,
        font: { size: 12 },
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
      grid: { color: 'rgba(219, 219, 219, 0.5)' },
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
  labels: statusDistribution.value.map((s) => t(`reports.status.${s.status}`)),
  datasets: [
    {
      data: statusDistribution.value.map((s) => s.count),
      backgroundColor: statusDistribution.value.map(
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

/* ------------------------------------------------------------------ */
/*  Data Loading                                                       */
/* ------------------------------------------------------------------ */
async function loadReportData(): Promise<void> {
  isLoading.value = true
  error.value = ''
  try {
    const data: ReportData = await fetchReportData(filters.value)
    summary.value = data.summary
    monthlyTrends.value = data.monthlyTrends
    statusDistribution.value = data.statusDistribution
    departmentPerformance.value = data.departmentPerformance
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err)
    error.value = message
    console.error('[ReportsView] Failed to load report data:', err)
  } finally {
    isLoading.value = false
  }
}

function applyFilters(): void {
  loadReportData()
}

function resetFilters(): void {
  filters.value = {
    dateFrom: null,
    dateTo: null,
    status: null,
    department: null,
  }
  loadReportData()
}

onMounted(() => {
  loadReportData()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('reports.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('reports.subtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
          :disabled="isLoading"
          @click="loadReportData"
        >
          <i class="pi" :class="isLoading ? 'pi-spinner pi-spin' : 'pi-refresh'" />
          {{ t('reports.refresh') }}
        </button>
      </div>
    </div>

    <!-- Error Alert -->
    <div
      v-if="error"
      class="flex items-center gap-3 rounded-xl border border-danger/20 bg-danger/5 p-4"
    >
      <i class="pi pi-exclamation-triangle text-danger" />
      <div class="flex-1">
        <p class="text-sm font-medium text-danger">{{ t('reports.errorLoading') }}</p>
        <p class="mt-0.5 text-xs text-danger/70">{{ error }}</p>
      </div>
      <button
        class="rounded-lg px-3 py-1.5 text-sm font-medium text-danger hover:bg-danger/10"
        @click="loadReportData"
      >
        {{ t('reports.retry') }}
      </button>
    </div>

    <!-- Filters Section -->
    <div class="card">
      <div class="flex flex-wrap items-end gap-4">
        <div class="flex-1 min-w-[200px]">
          <label class="mb-1 block text-xs font-medium text-tertiary">
            {{ t('reports.filters.dateFrom') }}
          </label>
          <input
            v-model="filters.dateFrom"
            type="date"
            class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div class="flex-1 min-w-[200px]">
          <label class="mb-1 block text-xs font-medium text-tertiary">
            {{ t('reports.filters.dateTo') }}
          </label>
          <input
            v-model="filters.dateTo"
            type="date"
            class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div class="flex-1 min-w-[200px]">
          <label class="mb-1 block text-xs font-medium text-tertiary">
            {{ t('reports.filters.status') }}
          </label>
          <select
            v-model="filters.status"
            class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option :value="null">{{ t('reports.filters.allStatuses') }}</option>
            <option value="draft">{{ t('reports.status.draft') }}</option>
            <option value="published">{{ t('reports.status.published') }}</option>
            <option value="receiving_offers">{{ t('reports.status.receiving_offers') }}</option>
            <option value="technical_evaluation">{{ t('reports.status.technical_evaluation') }}</option>
            <option value="financial_evaluation">{{ t('reports.status.financial_evaluation') }}</option>
            <option value="awarding">{{ t('reports.status.awarding') }}</option>
            <option value="completed">{{ t('reports.status.completed') }}</option>
            <option value="cancelled">{{ t('reports.status.cancelled') }}</option>
          </select>
        </div>
        <div class="flex gap-2">
          <button
            class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary/90"
            @click="applyFilters"
          >
            <i class="pi pi-search me-1" />
            {{ t('reports.filters.apply') }}
          </button>
          <button
            class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
            @click="resetFilters"
          >
            <i class="pi pi-filter-slash me-1" />
            {{ t('reports.filters.reset') }}
          </button>
        </div>
      </div>
    </div>

    <!-- KPI Summary Cards -->
    <div class="grid grid-cols-2 gap-3 md:grid-cols-4">
      <div
        v-for="kpi in kpiCards"
        :key="kpi.key"
        class="card flex items-center gap-3 !p-4"
      >
        <template v-if="isLoading">
          <div class="h-10 w-10 animate-pulse rounded-lg bg-surface-dim" />
          <div class="flex-1">
            <div class="mb-1 h-3 w-16 animate-pulse rounded bg-surface-dim" />
            <div class="h-5 w-12 animate-pulse rounded bg-surface-muted" />
          </div>
        </template>
        <template v-else>
          <div
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
            :class="kpi.bgColor"
          >
            <i class="pi text-base" :class="[kpi.icon, kpi.color]" />
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
      <!-- Monthly Trends Bar Chart -->
      <div class="card">
        <h3 class="mb-4 text-base font-semibold text-secondary">
          {{ t('reports.charts.monthlyTrends') }}
        </h3>
        <div v-if="isLoading" class="flex h-[280px] items-center justify-center">
          <div class="h-full w-full animate-pulse rounded-lg bg-surface-muted" />
        </div>
        <div v-else-if="monthlyTrends.length === 0" class="flex h-[280px] flex-col items-center justify-center">
          <i class="pi pi-chart-bar text-4xl text-surface-dim" />
          <p class="mt-3 text-sm text-tertiary">{{ t('reports.noData') }}</p>
        </div>
        <div v-else class="h-[280px]">
          <Bar :data="barChartData" :options="barChartOptions" />
        </div>
      </div>

      <!-- Status Distribution Doughnut Chart -->
      <div class="card">
        <h3 class="mb-4 text-base font-semibold text-secondary">
          {{ t('reports.charts.statusDistribution') }}
        </h3>
        <div v-if="isLoading" class="flex h-[280px] items-center justify-center">
          <div class="h-full w-full animate-pulse rounded-lg bg-surface-muted" />
        </div>
        <div v-else-if="statusDistribution.length === 0" class="flex h-[280px] flex-col items-center justify-center">
          <i class="pi pi-chart-pie text-4xl text-surface-dim" />
          <p class="mt-3 text-sm text-tertiary">{{ t('reports.noData') }}</p>
        </div>
        <div v-else class="h-[280px]">
          <Doughnut :data="doughnutChartData" :options="doughnutChartOptions" />
        </div>
      </div>
    </div>

    <!-- Department Performance Table -->
    <div class="card">
      <h3 class="mb-4 text-base font-semibold text-secondary">
        {{ t('reports.departmentPerformance') }}
      </h3>
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-12 animate-pulse rounded-lg bg-surface-muted" />
      </div>
      <div v-else-if="departmentPerformance.length === 0" class="flex flex-col items-center justify-center py-12">
        <i class="pi pi-building text-4xl text-surface-dim" />
        <p class="mt-3 text-sm text-tertiary">{{ t('reports.noDepartmentData') }}</p>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-surface-dim">
              <th class="px-4 py-3 text-start text-xs font-semibold uppercase text-tertiary">
                {{ t('reports.table.department') }}
              </th>
              <th class="px-4 py-3 text-start text-xs font-semibold uppercase text-tertiary">
                {{ t('reports.table.competitions') }}
              </th>
              <th class="px-4 py-3 text-start text-xs font-semibold uppercase text-tertiary">
                {{ t('reports.table.avgCycleTime') }}
              </th>
              <th class="px-4 py-3 text-start text-xs font-semibold uppercase text-tertiary">
                {{ t('reports.table.complianceRate') }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="dept in departmentPerformance"
              :key="dept.departmentNameEn"
              class="border-b border-surface-dim/50 transition-colors hover:bg-surface-muted/50"
            >
              <td class="px-4 py-3 font-medium text-secondary">
                {{ isRtl ? dept.departmentNameAr : dept.departmentNameEn }}
              </td>
              <td class="px-4 py-3 text-secondary">
                {{ formatNumber(dept.competitionsCount) }}
              </td>
              <td class="px-4 py-3 text-secondary">
                {{ formatNumber(dept.averageCycleTimeDays) }} {{ t('reports.days') }}
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <div class="h-2 w-16 overflow-hidden rounded-full bg-surface-dim">
                    <div
                      class="h-full rounded-full transition-all"
                      :class="dept.complianceRate >= 80 ? 'bg-success' : dept.complianceRate >= 60 ? 'bg-warning' : 'bg-danger'"
                      :style="{ width: `${Math.min(dept.complianceRate, 100)}%` }"
                    />
                  </div>
                  <span class="text-xs font-medium" :class="dept.complianceRate >= 80 ? 'text-success' : dept.complianceRate >= 60 ? 'text-warning' : 'text-danger'">
                    {{ formatPercentage(dept.complianceRate) }}
                  </span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
