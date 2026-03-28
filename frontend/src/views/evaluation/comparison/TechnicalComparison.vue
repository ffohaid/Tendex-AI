<script setup lang="ts">
/**
 * TechnicalComparison - Full-screen heatmap comparison matrix.
 * Implements the Heatmap Comparison as specified in PRD Section 9.3.
 * Color coding: Green (>=80%), Yellow (60-79%), Red (<60%).
 * Supports toggling between scores and notes views.
 */
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import type { HeatmapCell } from '@/types/evaluation'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()

const competitionId = computed(() => route.params.id as string)
const viewMode = ref<'scores' | 'notes'>('scores')
const heatmapData = ref<HeatmapCell[][]>([])

onMounted(async () => {
  await store.loadTechnicalData(competitionId.value)
  heatmapData.value = store.buildHeatmapCells()
})

function getCellClasses(cell: HeatmapCell): string {
  const base = 'px-4 py-3 text-center border-b border-e border-surface-dim transition-all'
  switch (cell.color) {
    case 'excellent':
      return `${base} bg-success/10`
    case 'average':
      return `${base} bg-warning/10`
    case 'weak':
      return `${base} bg-danger/10`
    default:
      return `${base} bg-surface-muted/50`
  }
}

function getCellTextColor(cell: HeatmapCell): string {
  switch (cell.color) {
    case 'excellent': return 'text-success'
    case 'average': return 'text-warning'
    case 'weak': return 'text-danger'
    default: return 'text-secondary'
  }
}

/**
 * Calculate vendor totals.
 */
const vendorTotals = computed(() => {
  if (!heatmapData.value.length) return []
  const vendorCount = store.vendors.length
  const totals: { vendorCode: string; average: number; color: string }[] = []

  for (let v = 0; v < vendorCount; v++) {
    let sum = 0
    let count = 0
    for (const row of heatmapData.value) {
      if (row[v]) {
        sum += row[v].percentage
        count++
      }
    }
    const avg = count > 0 ? sum / count : 0
    totals.push({
      vendorCode: store.vendors[v]?.code ?? '',
      average: avg,
      color: avg >= 80 ? 'text-success' : avg >= 60 ? 'text-warning' : 'text-danger',
    })
  }
  return totals
})

function exportMatrix() {
  // Placeholder for export functionality
  // Will be connected to backend PDF generation API
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div class="flex items-center gap-3">
        <button
          class="flex h-9 w-9 items-center justify-center rounded-lg border border-surface-dim text-secondary/60 transition-colors hover:bg-surface-muted hover:text-secondary"
          @click="router.back()"
        >
          <i class="pi pi-arrow-right rtl:pi-arrow-left" />
        </button>
        <div>
          <h1 class="text-xl font-bold text-secondary">
            <i class="pi pi-th-large me-2 text-primary" />
            {{ t('evaluation.comparison.technicalTitle') }}
          </h1>
          <p class="text-sm text-secondary/60">
            {{ store.selectedCompetition?.projectName }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <!-- View mode toggle -->
        <div class="flex rounded-lg border border-surface-dim bg-surface-muted p-0.5">
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'scores'
              ? 'bg-white text-primary shadow-sm'
              : 'text-secondary/60 hover:text-secondary'"
            @click="viewMode = 'scores'"
          >
            <i class="pi pi-chart-bar me-1" />
            {{ t('evaluation.comparison.scores') }}
          </button>
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'notes'
              ? 'bg-white text-primary shadow-sm'
              : 'text-secondary/60 hover:text-secondary'"
            @click="viewMode = 'notes'"
          >
            <i class="pi pi-comment me-1" />
            {{ t('evaluation.comparison.notes') }}
          </button>
        </div>

        <!-- Export button -->
        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="exportMatrix"
        >
          <i class="pi pi-download" />
          {{ t('evaluation.comparison.export') }}
        </button>
      </div>
    </div>

    <!-- Color legend -->
    <div class="card">
      <div class="flex flex-wrap items-center gap-6">
        <span class="text-sm font-medium text-secondary">{{ t('evaluation.comparison.legend') }}:</span>
        <div class="flex items-center gap-2">
          <div class="h-4 w-8 rounded bg-success/20" />
          <span class="text-xs text-secondary/70">{{ t('evaluation.comparison.excellent') }} (≥ 80%)</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="h-4 w-8 rounded bg-warning/20" />
          <span class="text-xs text-secondary/70">{{ t('evaluation.comparison.average') }} (60-79%)</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="h-4 w-8 rounded bg-danger/20" />
          <span class="text-xs text-secondary/70">{{ t('evaluation.comparison.weak') }} (&lt; 60%)</span>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-primary" />
    </div>

    <!-- Heatmap Matrix -->
    <div v-else class="card overflow-x-auto">
      <table class="w-full border-collapse text-sm">
        <thead>
          <tr>
            <th class="sticky start-0 z-10 border-b border-e border-surface-dim bg-white px-4 py-3 text-start text-xs font-semibold text-secondary/60">
              {{ t('evaluation.comparison.criterion') }}
            </th>
            <th class="border-b border-e border-surface-dim bg-white px-4 py-3 text-center text-xs font-semibold text-secondary/60">
              {{ t('evaluation.comparison.weight') }}
            </th>
            <th
              v-for="vendor in store.vendors"
              :key="vendor.id"
              class="border-b border-e border-surface-dim bg-secondary/5 px-4 py-3 text-center text-xs font-semibold text-secondary"
            >
              <div class="flex flex-col items-center gap-1">
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-tertiary/10 text-sm font-bold text-tertiary">
                  {{ vendor.code.charAt(vendor.code.length - 1) }}
                </div>
                <span>{{ vendor.code }}</span>
              </div>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, rowIdx) in heatmapData" :key="rowIdx">
            <!-- Criterion name (sticky) -->
            <td class="sticky start-0 z-10 border-b border-e border-surface-dim bg-white px-4 py-3 text-sm font-medium text-secondary">
              {{ store.criteria[rowIdx]?.name }}
            </td>
            <!-- Weight -->
            <td class="border-b border-e border-surface-dim bg-white px-4 py-3 text-center text-xs font-medium text-secondary/60">
              {{ store.criteria[rowIdx]?.weight }}%
            </td>
            <!-- Vendor cells -->
            <td
              v-for="cell in row"
              :key="cell.vendorId"
              :class="getCellClasses(cell)"
            >
              <!-- Scores view -->
              <template v-if="viewMode === 'scores'">
                <div class="flex flex-col items-center gap-1">
                  <span class="text-lg font-bold" :class="getCellTextColor(cell)">
                    {{ cell.percentage.toFixed(0) }}%
                  </span>
                  <span class="text-xs text-secondary/50">
                    {{ cell.score.toFixed(1) }} / {{ cell.maxScore }}
                  </span>
                  <!-- AI score indicator -->
                  <div v-if="cell.aiScore !== undefined" class="mt-1 flex items-center gap-1">
                    <i class="pi pi-sparkles text-xs text-info" />
                    <span class="text-xs text-info">{{ ((cell.aiScore / cell.maxScore) * 100).toFixed(0) }}%</span>
                  </div>
                </div>
              </template>

              <!-- Notes view -->
              <template v-else>
                <div class="max-w-[200px] text-start">
                  <p class="text-xs leading-relaxed text-secondary/70">
                    {{ cell.notes || t('evaluation.comparison.noNotes') }}
                  </p>
                  <p v-if="cell.aiJustification" class="mt-1 border-t border-surface-dim pt-1 text-xs text-info">
                    <i class="pi pi-sparkles me-1" />
                    {{ cell.aiJustification }}
                  </p>
                </div>
              </template>
            </td>
          </tr>

          <!-- Totals row -->
          <tr class="bg-secondary/5 font-bold">
            <td class="sticky start-0 z-10 border-t-2 border-e border-secondary/20 bg-secondary/5 px-4 py-3 text-sm text-secondary">
              {{ t('evaluation.comparison.totalAverage') }}
            </td>
            <td class="border-t-2 border-e border-secondary/20 px-4 py-3 text-center text-sm text-secondary">
              {{ store.totalCriteriaWeight }}%
            </td>
            <td
              v-for="total in vendorTotals"
              :key="total.vendorCode"
              class="border-t-2 border-e border-secondary/20 px-4 py-3 text-center"
            >
              <span class="text-xl font-bold" :class="total.color">
                {{ total.average.toFixed(1) }}%
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Variance alerts -->
    <div v-if="store.varianceAlerts.length > 0" class="card border-warning/20 bg-warning/5">
      <h3 class="mb-3 flex items-center gap-2 text-base font-bold text-warning">
        <i class="pi pi-exclamation-triangle" />
        {{ t('evaluation.comparison.varianceAlerts') }}
      </h3>
      <div class="space-y-2">
        <div
          v-for="alert in store.varianceAlerts"
          :key="`${alert.criterionId}-${alert.vendorId}`"
          class="flex items-center gap-3 rounded-lg bg-white p-3"
        >
          <div class="flex h-8 w-8 items-center justify-center rounded-full bg-warning/10">
            <i class="pi pi-exclamation-triangle text-sm text-warning" />
          </div>
          <div class="flex-1">
            <p class="text-sm font-medium text-secondary">
              {{ alert.vendorCode }} — {{ alert.criterionName }}
            </p>
            <p class="text-xs text-secondary/60">
              {{ t('evaluation.comparison.humanScore') }}: {{ alert.humanScore }} |
              {{ t('evaluation.comparison.aiScore') }}: {{ alert.aiScore }} |
              {{ t('evaluation.comparison.variance') }}: {{ alert.variancePercent.toFixed(0) }}%
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
