<script setup lang="ts">
/**
 * TechnicalComparison - Full-screen heatmap comparison matrix.
 * Implements the Heatmap Comparison as specified in PRD Section 9.3.
 * Color coding: Green (>=80%), Yellow (60-79%), Red (<60%).
 * Uses backend TechnicalHeatmap data with proper types.
 */
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import type { HeatmapCell } from '@/types/evaluation'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const competitionId = computed(() => route.params.id as string)
const viewMode = ref<'scores' | 'notes'>('scores')

onMounted(async () => {
  try {
    await store.loadTechnicalData(competitionId.value)
  } catch (err) {
    console.error('[TechnicalComparison] Failed to load data:', err)
  }
})

/* Build heatmap grid from store data: rows = criteria, cols = offers */
const heatmapGrid = computed(() => {
  const heatmap = store.technicalHeatmap
  if (!heatmap || !heatmap.cells.length) return []

  const grid: HeatmapCell[][] = []
  for (const criterion of heatmap.criteria) {
    const row = heatmap.cells.filter(c => c.criterionId === criterion.id)
    // Sort by offerBlindCodes order
    row.sort((a, b) => {
      const idxA = heatmap.offerBlindCodes.indexOf(a.offerBlindCode)
      const idxB = heatmap.offerBlindCodes.indexOf(b.offerBlindCode)
      return idxA - idxB
    })
    grid.push(row)
  }
  return grid
})

const offerCodes = computed(() => store.technicalHeatmap?.offerBlindCodes ?? [])
const criteriaHeaders = computed(() => store.technicalHeatmap?.criteria ?? [])

function getCellClasses(cell: HeatmapCell): string {
  const base = 'px-4 py-3 text-center border-b border-e border-gray-200 transition-all'
  switch (cell.color) {
    case 'excellent':
      return `${base} bg-green-50`
    case 'average':
      return `${base} bg-yellow-50`
    case 'weak':
      return `${base} bg-red-50`
    default:
      return `${base} bg-gray-50`
  }
}

function getCellTextColor(cell: HeatmapCell): string {
  switch (cell.color) {
    case 'excellent': return 'text-green-700'
    case 'average': return 'text-yellow-700'
    case 'weak': return 'text-red-700'
    default: return 'text-gray-500'
  }
}

/* Calculate offer column totals */
const offerTotals = computed(() => {
  if (!heatmapGrid.value.length || !offerCodes.value.length) return []
  return offerCodes.value.map((code, colIdx) => {
    let sum = 0
    let count = 0
    for (const row of heatmapGrid.value) {
      const cell = row[colIdx]
      if (cell) {
        sum += cell.averageScorePercentage
        count++
      }
    }
    const avg = count > 0 ? sum / count : 0
    return {
      code,
      average: avg,
      color: avg >= 80 ? 'text-green-700' : avg >= 60 ? 'text-yellow-700' : 'text-red-700',
    }
  })
})

/* Get score from technicalScores for a specific cell */
function getCellScore(cell: HeatmapCell) {
  const score = store.technicalScores.find(
    s => s.supplierOfferId === cell.offerId && s.evaluationCriterionId === cell.criterionId
  )
  return score
}

/* Get AI score for a specific cell */
function getCellAiScore(cell: HeatmapCell) {
  return store.aiTechnicalScores.find(
    s => s.supplierOfferId === cell.offerId && s.evaluationCriterionId === cell.criterionId
  )
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div class="flex items-center gap-3">
        <button
          class="flex h-9 w-9 items-center justify-center rounded-lg border border-gray-200 text-gray-500 hover:bg-gray-50"
          @click="router.back()"
        >
          <i :class="isRtl ? 'pi pi-arrow-left' : 'pi pi-arrow-right'" />
        </button>
        <div>
          <h1 class="text-xl font-bold text-gray-900">
            <i class="pi pi-th-large me-2 text-blue-600" />
            {{ isRtl ? 'مصفوفة المقارنة الفنية' : 'Technical Comparison Matrix' }}
          </h1>
          <p class="text-sm text-gray-500">
            {{ store.selectedCompetition?.projectName }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <!-- View mode toggle -->
        <div class="flex rounded-lg border border-gray-200 bg-gray-50 p-0.5">
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'scores'
              ? 'bg-white text-blue-600 shadow-sm'
              : 'text-gray-500 hover:text-gray-700'"
            @click="viewMode = 'scores'"
          >
            <i class="pi pi-chart-bar me-1" />
            {{ isRtl ? 'الدرجات' : 'Scores' }}
          </button>
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'notes'
              ? 'bg-white text-blue-600 shadow-sm'
              : 'text-gray-500 hover:text-gray-700'"
            @click="viewMode = 'notes'"
          >
            <i class="pi pi-comment me-1" />
            {{ isRtl ? 'الملاحظات' : 'Notes' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Color legend -->
    <div class="rounded-lg border border-gray-200 bg-white p-4">
      <div class="flex flex-wrap items-center gap-6">
        <span class="text-sm font-medium text-gray-700">{{ isRtl ? 'دليل الألوان' : 'Legend' }}:</span>
        <div class="flex items-center gap-2">
          <div class="h-4 w-8 rounded bg-green-100" />
          <span class="text-xs text-gray-600">{{ isRtl ? 'ممتاز' : 'Excellent' }} (≥ 80%)</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="h-4 w-8 rounded bg-yellow-100" />
          <span class="text-xs text-gray-600">{{ isRtl ? 'متوسط' : 'Average' }} (60-79%)</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="h-4 w-8 rounded bg-red-100" />
          <span class="text-xs text-gray-600">{{ isRtl ? 'ضعيف' : 'Weak' }} (&lt; 60%)</span>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-blue-600" />
      <span class="ms-3 text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Error -->
    <div v-else-if="store.error" class="rounded-lg border border-red-200 bg-red-50 p-6 text-center">
      <i class="pi pi-exclamation-triangle mb-3 text-3xl text-red-500" />
      <p class="mt-2 text-sm text-red-700">{{ store.error }}</p>
      <button
        class="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        @click="router.back()"
      >
        {{ isRtl ? 'رجوع' : 'Back' }}
      </button>
    </div>

    <!-- No heatmap data -->
    <div v-else-if="!heatmapGrid.length" class="rounded-lg border border-gray-200 bg-white p-12 text-center">
      <i class="pi pi-th-large text-4xl text-gray-300" />
      <h3 class="mt-4 text-lg font-semibold text-gray-500">
        {{ isRtl ? 'لا توجد بيانات للمقارنة' : 'No comparison data available' }}
      </h3>
      <p class="mt-2 text-sm text-gray-400">
        {{ isRtl ? 'يجب إكمال التقييم الفني أولاً لعرض المصفوفة' : 'Complete technical evaluation first to view the matrix' }}
      </p>
    </div>

    <!-- Heatmap Matrix -->
    <div v-else class="overflow-x-auto rounded-lg border border-gray-200 bg-white">
      <table class="w-full border-collapse text-sm">
        <thead>
          <tr>
            <th class="sticky start-0 z-10 border-b border-e border-gray-200 bg-white px-4 py-3 text-start text-xs font-semibold text-gray-500">
              {{ isRtl ? 'المعيار' : 'Criterion' }}
            </th>
            <th class="border-b border-e border-gray-200 bg-white px-4 py-3 text-center text-xs font-semibold text-gray-500">
              {{ isRtl ? 'الوزن' : 'Weight' }}
            </th>
            <th
              v-for="code in offerCodes"
              :key="code"
              class="border-b border-e border-gray-200 bg-gray-50 px-4 py-3 text-center text-xs font-semibold text-gray-700"
            >
              <div class="flex flex-col items-center gap-1">
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-50 text-sm font-bold text-blue-600">
                  {{ code.charAt(code.length - 1) }}
                </div>
                <span>{{ code }}</span>
              </div>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, rowIdx) in heatmapGrid" :key="rowIdx">
            <!-- Criterion name (sticky) -->
            <td class="sticky start-0 z-10 border-b border-e border-gray-200 bg-white px-4 py-3 text-sm font-medium text-gray-700">
              {{ isRtl ? criteriaHeaders[rowIdx]?.nameAr : criteriaHeaders[rowIdx]?.nameEn }}
            </td>
            <!-- Weight -->
            <td class="border-b border-e border-gray-200 bg-white px-4 py-3 text-center text-xs font-medium text-gray-500">
              {{ criteriaHeaders[rowIdx]?.weightPercentage }}%
            </td>
            <!-- Offer cells -->
            <td
              v-for="cell in row"
              :key="`${cell.offerId}-${cell.criterionId}`"
              :class="getCellClasses(cell)"
            >
              <!-- Scores view -->
              <template v-if="viewMode === 'scores'">
                <div class="flex flex-col items-center gap-1">
                  <span class="text-lg font-bold" :class="getCellTextColor(cell)">
                    {{ cell.averageScorePercentage.toFixed(0) }}%
                  </span>
                  <span v-if="getCellScore(cell)" class="text-xs text-gray-400">
                    {{ getCellScore(cell)!.score.toFixed(1) }} / {{ getCellScore(cell)!.maxScore }}
                  </span>
                  <!-- AI score indicator -->
                  <div v-if="getCellAiScore(cell)" class="mt-1 flex items-center gap-1">
                    <i class="pi pi-sparkles text-xs text-blue-500" />
                    <span class="text-xs text-blue-500">
                      {{ getCellAiScore(cell)!.scorePercentage.toFixed(0) }}%
                    </span>
                  </div>
                </div>
              </template>

              <!-- Notes view -->
              <template v-else>
                <div class="max-w-[200px] text-start">
                  <p class="text-xs leading-relaxed text-gray-600">
                    {{ getCellScore(cell)?.notes || (isRtl ? 'لا توجد ملاحظات' : 'No notes') }}
                  </p>
                  <p v-if="getCellAiScore(cell)" class="mt-1 border-t border-gray-200 pt-1 text-xs text-blue-600">
                    <i class="pi pi-sparkles me-1" />
                    {{ getCellAiScore(cell)!.justification }}
                  </p>
                </div>
              </template>
            </td>
          </tr>

          <!-- Totals row -->
          <tr class="bg-gray-50 font-bold">
            <td class="sticky start-0 z-10 border-t-2 border-e border-gray-300 bg-gray-50 px-4 py-3 text-sm text-gray-700">
              {{ isRtl ? 'المتوسط الكلي' : 'Total Average' }}
            </td>
            <td class="border-t-2 border-e border-gray-300 px-4 py-3 text-center text-sm text-gray-700">
              {{ store.totalCriteriaWeight }}%
            </td>
            <td
              v-for="total in offerTotals"
              :key="total.code"
              class="border-t-2 border-e border-gray-300 px-4 py-3 text-center"
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
    <div v-if="store.varianceAlerts.length > 0" class="rounded-lg border border-yellow-200 bg-yellow-50 p-4">
      <h3 class="mb-3 flex items-center gap-2 text-base font-bold text-yellow-700">
        <i class="pi pi-exclamation-triangle" />
        {{ isRtl ? 'تنبيهات التباين' : 'Variance Alerts' }}
      </h3>
      <div class="space-y-2">
        <div
          v-for="alert in store.varianceAlerts"
          :key="`${alert.criterionId}-${alert.offerId}`"
          class="flex items-center gap-3 rounded-lg bg-white p-3"
        >
          <div class="flex h-8 w-8 items-center justify-center rounded-full bg-yellow-100">
            <i class="pi pi-exclamation-triangle text-sm text-yellow-600" />
          </div>
          <div class="flex-1">
            <p class="text-sm font-medium text-gray-700">
              {{ alert.offerBlindCode }} — {{ isRtl ? alert.criterionNameAr : alert.criterionNameEn }}
            </p>
            <p class="text-xs text-gray-500">
              <template v-if="alert.hasEvaluatorVariance">
                {{ isRtl ? 'تباين بين المقيّمين' : 'Evaluator variance' }}: {{ alert.evaluatorSpread?.toFixed(1) }}
              </template>
              <template v-if="alert.hasHumanAiVariance">
                {{ alert.hasEvaluatorVariance ? ' | ' : '' }}
                {{ isRtl ? 'فرق بشري/ذكاء اصطناعي' : 'Human-AI difference' }}: {{ alert.humanAiDifference?.toFixed(1) }}
              </template>
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
