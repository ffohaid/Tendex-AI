<script setup lang="ts">
/**
 * AiEvaluationPanel - AI-powered evaluation analysis panel.
 * Provides a comprehensive AI analysis interface for technical evaluation.
 * Features:
 * - Trigger AI analysis for all offers
 * - View analysis summary with compliance scores
 * - Detailed per-offer analysis with strengths/weaknesses
 * - Human review workflow
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  triggerAiAnalysis,
  getAiAnalysisSummary,
  getAiOfferAnalysis,
  reviewAiAnalysis,
  type AiAnalysisSummary,
  type AiOfferAnalysis,
  type AiOfferSummaryItem,
} from '@/services/aiEvaluationService'

const props = defineProps<{
  competitionId: string
  evaluationId: string
}>()

const { t } = useI18n()

const summary = ref<AiAnalysisSummary | null>(null)
const selectedAnalysis = ref<AiOfferAnalysis | null>(null)
const isAnalyzing = ref(false)
const isLoadingSummary = ref(false)
const isLoadingDetail = ref(false)
const error = ref<string | null>(null)
const reviewNotes = ref('')
const showReviewDialog = ref(false)
const reviewingAnalysisId = ref<string | null>(null)

const hasAnalysis = computed(() => summary.value && summary.value.completedAnalyses > 0)

onMounted(async () => {
  await loadSummary()
})

async function loadSummary() {
  isLoadingSummary.value = true
  error.value = null
  try {
    summary.value = await getAiAnalysisSummary(props.competitionId, props.evaluationId)
  } catch {
    // No analysis yet — that's OK
    summary.value = null
  } finally {
    isLoadingSummary.value = false
  }
}

async function startAnalysis() {
  isAnalyzing.value = true
  error.value = null
  try {
    summary.value = await triggerAiAnalysis(props.competitionId, props.evaluationId)
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : t('ai.errors.evaluationFailed')
  } finally {
    isAnalyzing.value = false
  }
}

async function viewOfferAnalysis(item: AiOfferSummaryItem) {
  isLoadingDetail.value = true
  try {
    selectedAnalysis.value = await getAiOfferAnalysis(props.competitionId, item.supplierOfferId)
  } catch {
    error.value = t('ai.errors.evaluationFailed')
  } finally {
    isLoadingDetail.value = false
  }
}

function openReviewDialog(analysisId: string) {
  reviewingAnalysisId.value = analysisId
  reviewNotes.value = ''
  showReviewDialog.value = true
}

async function submitReview() {
  if (!reviewingAnalysisId.value) return
  try {
    await reviewAiAnalysis(props.competitionId, reviewingAnalysisId.value, reviewNotes.value)
    showReviewDialog.value = false
    await loadSummary()
    if (selectedAnalysis.value?.id === reviewingAnalysisId.value) {
      selectedAnalysis.value = await getAiOfferAnalysis(props.competitionId, reviewingAnalysisId.value)
    }
  } catch {
    error.value = t('ai.errors.evaluationFailed')
  }
}

function getComplianceColor(level: string): string {
  switch (level) {
    case 'FullyCompliant': return 'text-success bg-success/10 border-success/30'
    case 'PartiallyCompliant': return 'text-warning bg-warning/10 border-warning/30'
    case 'NonCompliant': return 'text-danger bg-danger/10 border-danger/30'
    default: return 'text-info bg-info/10 border-info/30'
  }
}

function getComplianceLabel(level: string): string {
  switch (level) {
    case 'FullyCompliant': return 'متوافق بالكامل'
    case 'PartiallyCompliant': return 'متوافق جزئياً'
    case 'NonCompliant': return 'غير متوافق'
    default: return 'يتطلب مراجعة'
  }
}

function getScoreColor(percentage: number): string {
  if (percentage >= 80) return 'text-success'
  if (percentage >= 60) return 'text-warning'
  return 'text-danger'
}
</script>

<template>
  <div class="space-y-4">
    <!-- AI Analysis Header -->
    <div class="rounded-2xl border border-ai-200 bg-gradient-to-l from-ai-50 to-white p-6">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-ai-100">
            <i class="pi pi-sparkles text-xl text-ai-600" />
          </div>
          <div>
            <h3 class="text-lg font-bold text-secondary">
              {{ t('ai.title') }} - {{ t('evaluation.tabs.scoring') }}
            </h3>
            <p class="text-sm text-secondary/60">
              {{ t('ai.subtitle') }}
            </p>
          </div>
        </div>

        <button
          v-if="!hasAnalysis"
          :disabled="isAnalyzing"
          class="flex items-center gap-2 rounded-xl bg-ai-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-ai-200 transition-all hover:bg-ai-700 disabled:opacity-50"
          @click="startAnalysis"
        >
          <i :class="isAnalyzing ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" />
          {{ isAnalyzing ? t('ai.analyzing') : t('ai.generateEvaluation') }}
        </button>

        <button
          v-else
          :disabled="isAnalyzing"
          class="flex items-center gap-2 rounded-xl border border-ai-300 bg-ai-50 px-4 py-2.5 text-sm font-medium text-ai-600 transition-all hover:bg-ai-100 disabled:opacity-50"
          @click="startAnalysis"
        >
          <i :class="isAnalyzing ? 'pi pi-spinner pi-spin' : 'pi pi-refresh'" />
          {{ isAnalyzing ? t('ai.analyzing') : 'إعادة التحليل' }}
        </button>
      </div>

      <!-- Error message -->
      <div v-if="error" class="mt-4 rounded-lg border border-danger/30 bg-danger/5 p-3 text-sm text-danger">
        <i class="pi pi-exclamation-triangle me-1.5" />
        {{ error }}
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoadingSummary" class="flex items-center justify-center py-8">
      <i class="pi pi-spinner pi-spin text-2xl text-ai-600" />
    </div>

    <!-- Analysis Summary -->
    <div v-else-if="hasAnalysis && summary" class="space-y-4">
      <!-- Summary Stats -->
      <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
        <div class="rounded-xl border border-surface-dim bg-white p-4 text-center">
          <div class="text-2xl font-bold text-primary">{{ summary.totalOffers }}</div>
          <div class="mt-1 text-xs text-secondary/60">إجمالي العروض</div>
        </div>
        <div class="rounded-xl border border-success/30 bg-success/5 p-4 text-center">
          <div class="text-2xl font-bold text-success">{{ summary.completedAnalyses }}</div>
          <div class="mt-1 text-xs text-secondary/60">تحليلات مكتملة</div>
        </div>
        <div class="rounded-xl border border-warning/30 bg-warning/5 p-4 text-center">
          <div class="text-2xl font-bold text-warning">{{ summary.pendingReviews }}</div>
          <div class="mt-1 text-xs text-secondary/60">بانتظار المراجعة</div>
        </div>
        <div v-if="summary.failedAnalyses > 0" class="rounded-xl border border-danger/30 bg-danger/5 p-4 text-center">
          <div class="text-2xl font-bold text-danger">{{ summary.failedAnalyses }}</div>
          <div class="mt-1 text-xs text-secondary/60">تحليلات فاشلة</div>
        </div>
      </div>

      <!-- Offer Cards -->
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        <div
          v-for="offer in summary.offerSummaries"
          :key="offer.supplierOfferId"
          class="cursor-pointer rounded-xl border border-surface-dim bg-white p-5 transition-all hover:border-ai-300 hover:shadow-md"
          :class="{ 'border-ai-400 ring-2 ring-ai-100': selectedAnalysis?.supplierOfferId === offer.supplierOfferId }"
          @click="viewOfferAnalysis(offer)"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-sm font-bold text-primary">
                {{ offer.blindCode }}
              </div>
              <div>
                <div class="text-sm font-semibold text-secondary">العرض {{ offer.blindCode }}</div>
                <div class="text-xs text-secondary/50">
                  {{ offer.criteriaAnalyzed }} معيار تم تحليله
                </div>
              </div>
            </div>
            <div class="text-end">
              <div class="text-lg font-bold" :class="getScoreColor(offer.overallComplianceScore)">
                {{ offer.overallComplianceScore.toFixed(0) }}%
              </div>
              <div v-if="offer.isHumanReviewed" class="text-xs text-success">
                <i class="pi pi-check-circle me-0.5" /> تمت المراجعة
              </div>
            </div>
          </div>

          <!-- Compliance breakdown -->
          <div class="mt-3 flex items-center gap-2">
            <span v-if="offer.fullyCompliantCount > 0" class="rounded-full bg-success/10 px-2 py-0.5 text-xs text-success">
              {{ offer.fullyCompliantCount }} متوافق
            </span>
            <span v-if="offer.partiallyCompliantCount > 0" class="rounded-full bg-warning/10 px-2 py-0.5 text-xs text-warning">
              {{ offer.partiallyCompliantCount }} جزئي
            </span>
            <span v-if="offer.nonCompliantCount > 0" class="rounded-full bg-danger/10 px-2 py-0.5 text-xs text-danger">
              {{ offer.nonCompliantCount }} غير متوافق
            </span>
          </div>
        </div>
      </div>

      <!-- Selected Offer Detail -->
      <div v-if="isLoadingDetail" class="flex items-center justify-center py-8">
        <i class="pi pi-spinner pi-spin text-2xl text-ai-600" />
      </div>

      <div v-else-if="selectedAnalysis" class="space-y-4">
        <!-- Analysis Header -->
        <div class="rounded-xl border border-ai-200 bg-white p-6">
          <div class="flex items-center justify-between">
            <div>
              <h4 class="text-lg font-bold text-secondary">
                تحليل العرض {{ selectedAnalysis.blindCode }}
              </h4>
              <p class="mt-1 text-sm text-secondary/60">
                نموذج: {{ selectedAnalysis.aiModelUsed }} | وقت التحليل: {{ selectedAnalysis.analysisLatencyMs }}ms
              </p>
            </div>
            <div class="flex items-center gap-2">
              <span
                v-if="selectedAnalysis.isHumanReviewed"
                class="rounded-full bg-success/10 px-3 py-1 text-xs font-medium text-success"
              >
                <i class="pi pi-check-circle me-1" /> تمت المراجعة
              </span>
              <button
                v-else
                class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark"
                @click="openReviewDialog(selectedAnalysis.id)"
              >
                <i class="pi pi-check me-1" /> اعتماد المراجعة
              </button>
            </div>
          </div>

          <!-- Executive Summary -->
          <div class="mt-4 rounded-lg bg-surface-muted p-4">
            <h5 class="mb-2 text-sm font-semibold text-secondary">الملخص التنفيذي</h5>
            <p class="text-sm leading-relaxed text-secondary/80">{{ selectedAnalysis.executiveSummary }}</p>
          </div>

          <!-- Strengths & Weaknesses -->
          <div class="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
            <div class="rounded-lg border border-success/20 bg-success/5 p-4">
              <h5 class="mb-2 flex items-center gap-2 text-sm font-semibold text-success">
                <i class="pi pi-check-circle" /> نقاط القوة
              </h5>
              <p class="text-sm leading-relaxed text-secondary/80">{{ selectedAnalysis.strengthsAnalysis }}</p>
            </div>
            <div class="rounded-lg border border-danger/20 bg-danger/5 p-4">
              <h5 class="mb-2 flex items-center gap-2 text-sm font-semibold text-danger">
                <i class="pi pi-exclamation-circle" /> نقاط الضعف
              </h5>
              <p class="text-sm leading-relaxed text-secondary/80">{{ selectedAnalysis.weaknessesAnalysis }}</p>
            </div>
          </div>

          <!-- Risks & Recommendation -->
          <div class="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
            <div class="rounded-lg border border-warning/20 bg-warning/5 p-4">
              <h5 class="mb-2 flex items-center gap-2 text-sm font-semibold text-warning">
                <i class="pi pi-exclamation-triangle" /> المخاطر
              </h5>
              <p class="text-sm leading-relaxed text-secondary/80">{{ selectedAnalysis.risksAnalysis }}</p>
            </div>
            <div class="rounded-lg border border-ai-200 bg-ai-50 p-4">
              <h5 class="mb-2 flex items-center gap-2 text-sm font-semibold text-ai-600">
                <i class="pi pi-sparkles" /> التوصية العامة
              </h5>
              <p class="text-sm leading-relaxed text-secondary/80">{{ selectedAnalysis.overallRecommendation }}</p>
            </div>
          </div>
        </div>

        <!-- Per-Criterion Analysis -->
        <div class="rounded-xl border border-surface-dim bg-white p-6">
          <h4 class="mb-4 text-lg font-bold text-secondary">تحليل المعايير التفصيلي</h4>
          <div class="space-y-3">
            <div
              v-for="criterion in selectedAnalysis.criterionAnalyses"
              :key="criterion.id"
              class="rounded-lg border border-surface-dim p-4"
            >
              <div class="flex items-center justify-between">
                <h5 class="text-sm font-semibold text-secondary">{{ criterion.criterionNameAr }}</h5>
                <div class="flex items-center gap-3">
                  <span
                    class="rounded-full border px-3 py-1 text-xs font-medium"
                    :class="getComplianceColor(criterion.complianceLevel)"
                  >
                    {{ getComplianceLabel(criterion.complianceLevel) }}
                  </span>
                  <span class="text-sm font-bold" :class="getScoreColor(criterion.scorePercentage)">
                    {{ criterion.suggestedScore }}/{{ criterion.maxScore }}
                  </span>
                </div>
              </div>
              <p class="mt-2 text-sm leading-relaxed text-secondary/70">
                {{ criterion.detailedJustification }}
              </p>
              <div v-if="criterion.offerCitations" class="mt-2 rounded bg-surface-muted p-2 text-xs text-secondary/60">
                <strong>الاستشهادات:</strong> {{ criterion.offerCitations }}
              </div>
            </div>
          </div>
        </div>

        <!-- Draft Notice -->
        <div class="rounded-lg border border-ai-200 bg-ai-50 p-3 text-center text-sm text-ai-600">
          <i class="pi pi-info-circle me-1.5" />
          مسودة مقترحة من الذكاء الاصطناعي — تتطلب مراجعة واعتماد من أعضاء اللجنة
        </div>
      </div>
    </div>

    <!-- No Analysis Yet -->
    <div v-else-if="!isLoadingSummary" class="rounded-xl border-2 border-dashed border-ai-200 bg-ai-50/30 p-12 text-center">
      <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-ai-100">
        <i class="pi pi-sparkles text-3xl text-ai-500" />
      </div>
      <h3 class="mt-4 text-lg font-bold text-secondary">{{ t('ai.generateEvaluation') }}</h3>
      <p class="mt-2 text-sm text-secondary/60">
        اضغط على زر "تقييم بالذكاء الاصطناعي" لبدء تحليل جميع العروض الفنية تلقائياً
      </p>
      <button
        :disabled="isAnalyzing"
        class="mt-6 inline-flex items-center gap-2 rounded-xl bg-ai-600 px-8 py-3 text-sm font-semibold text-white shadow-lg shadow-ai-200 transition-all hover:bg-ai-700 disabled:opacity-50"
        @click="startAnalysis"
      >
        <i :class="isAnalyzing ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" />
        {{ isAnalyzing ? t('ai.analyzing') : t('ai.generateEvaluation') }}
      </button>
    </div>

    <!-- Review Dialog -->
    <Teleport to="body">
      <div v-if="showReviewDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div class="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
          <h3 class="text-lg font-bold text-secondary">اعتماد مراجعة التحليل</h3>
          <p class="mt-2 text-sm text-secondary/60">
            بتأكيد المراجعة، تقر بأنك اطلعت على تحليل الذكاء الاصطناعي وقمت بمراجعته.
          </p>
          <textarea
            v-model="reviewNotes"
            class="mt-4 w-full rounded-lg border border-surface-dim p-3 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            rows="3"
            placeholder="ملاحظات المراجعة (اختياري)..."
          />
          <div class="mt-4 flex items-center justify-end gap-3">
            <button
              class="rounded-lg px-4 py-2 text-sm text-secondary/60 hover:text-secondary"
              @click="showReviewDialog = false"
            >
              إلغاء
            </button>
            <button
              class="rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white hover:bg-primary-dark"
              @click="submitReview"
            >
              <i class="pi pi-check me-1" /> تأكيد المراجعة
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
