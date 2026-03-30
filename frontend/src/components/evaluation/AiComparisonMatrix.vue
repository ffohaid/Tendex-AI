<script setup lang="ts">
/**
 * AiComparisonMatrix - AI comparison matrix for offers across criteria.
 * Shows a side-by-side comparison of AI-suggested scores for all offers.
 */
import { ref, onMounted } from 'vue'
import {
  getAiComparisonMatrix,
  type AiComparisonMatrix,
  type AiComparisonCell,
} from '@/services/aiEvaluationService'

const props = defineProps<{
  competitionId: string
  evaluationId: string
}>()

const matrix = ref<AiComparisonMatrix | null>(null)
const isLoading = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  await loadMatrix()
})

async function loadMatrix() {
  isLoading.value = true
  error.value = null
  try {
    matrix.value = await getAiComparisonMatrix(props.competitionId, props.evaluationId)
  } catch {
    error.value = 'لا تتوفر بيانات مقارنة بعد'
    matrix.value = null
  } finally {
    isLoading.value = false
  }
}

function getCellForOfferCriterion(blindCode: string, criterionId: string): AiComparisonCell | undefined {
  return matrix.value?.cells.find(c => c.blindCode === blindCode && c.criterionId === criterionId)
}

function getComplianceBg(level: string): string {
  switch (level) {
    case 'FullyCompliant': return 'bg-success/10 text-success'
    case 'PartiallyCompliant': return 'bg-warning/10 text-warning'
    case 'NonCompliant': return 'bg-danger/10 text-danger'
    default: return 'bg-info/10 text-info'
  }
}

function getScoreColor(percentage: number): string {
  if (percentage >= 80) return 'text-success font-bold'
  if (percentage >= 60) return 'text-warning font-bold'
  return 'text-danger font-bold'
}
</script>

<template>
  <div class="rounded-xl border border-surface-dim bg-white p-6">
    <div class="mb-4 flex items-center gap-3">
      <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-ai-100">
        <i class="pi pi-th-large text-ai-600" />
      </div>
      <div>
        <h3 class="text-lg font-bold text-secondary">مصفوفة المقارنة بالذكاء الاصطناعي</h3>
        <p class="text-sm text-secondary/60">مقارنة الدرجات المقترحة لجميع العروض عبر جميع المعايير</p>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-8">
      <i class="pi pi-spinner pi-spin text-2xl text-ai-600" />
    </div>

    <!-- Error / No Data -->
    <div v-else-if="error || !matrix" class="rounded-lg border border-surface-dim bg-surface-muted p-8 text-center text-sm text-secondary/50">
      <i class="pi pi-table mb-2 text-2xl" />
      <p>{{ error || 'لا تتوفر بيانات مقارنة' }}</p>
    </div>

    <!-- Matrix Table -->
    <div v-else class="overflow-x-auto">
      <table class="w-full border-collapse text-sm">
        <thead>
          <tr>
            <th class="sticky start-0 z-10 border-b border-surface-dim bg-surface-muted px-4 py-3 text-start text-xs font-semibold text-secondary/70">
              المعيار
            </th>
            <th class="border-b border-surface-dim bg-surface-muted px-3 py-3 text-center text-xs font-semibold text-secondary/70">
              الوزن
            </th>
            <th
              v-for="code in matrix.offerBlindCodes"
              :key="code"
              class="border-b border-surface-dim bg-ai-50 px-4 py-3 text-center text-xs font-semibold text-ai-700"
            >
              العرض {{ code }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="criterion in matrix.criteria" :key="criterion.id" class="hover:bg-surface-muted/50">
            <td class="sticky start-0 z-10 border-b border-surface-dim bg-white px-4 py-3 text-sm font-medium text-secondary">
              {{ criterion.nameAr }}
            </td>
            <td class="border-b border-surface-dim px-3 py-3 text-center text-xs text-secondary/60">
              {{ criterion.weight }}%
            </td>
            <td
              v-for="code in matrix.offerBlindCodes"
              :key="`${criterion.id}-${code}`"
              class="border-b border-surface-dim px-4 py-3 text-center"
            >
              <template v-if="getCellForOfferCriterion(code, criterion.id)">
                <div :class="getScoreColor(getCellForOfferCriterion(code, criterion.id)!.scorePercentage)">
                  {{ getCellForOfferCriterion(code, criterion.id)!.suggestedScore }}/{{ getCellForOfferCriterion(code, criterion.id)!.maxScore }}
                </div>
                <span
                  class="mt-1 inline-block rounded-full px-2 py-0.5 text-[10px]"
                  :class="getComplianceBg(getCellForOfferCriterion(code, criterion.id)!.complianceLevel)"
                >
                  {{ getCellForOfferCriterion(code, criterion.id)!.complianceLevel === 'FullyCompliant' ? 'متوافق' :
                     getCellForOfferCriterion(code, criterion.id)!.complianceLevel === 'PartiallyCompliant' ? 'جزئي' :
                     getCellForOfferCriterion(code, criterion.id)!.complianceLevel === 'NonCompliant' ? 'غير متوافق' : 'مراجعة' }}
                </span>
              </template>
              <span v-else class="text-xs text-secondary/30">—</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Draft Notice -->
    <div class="mt-4 rounded-lg border border-ai-200 bg-ai-50 p-2.5 text-center text-xs text-ai-600">
      <i class="pi pi-info-circle me-1" />
      مسودة مقترحة من الذكاء الاصطناعي — الدرجات النهائية تحددها اللجنة
    </div>
  </div>
</template>
