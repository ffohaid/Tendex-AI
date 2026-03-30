<script setup lang="ts">
/**
 * AiEvaluationAssistant - AI-Powered Offer Evaluation (TASK-1001)
 *
 * Features:
 * - Automated technical evaluation scoring suggestions
 * - Financial analysis and comparison
 * - Compliance checking against specifications
 * - Risk assessment per offer
 * - Arabic-only output
 * - RAG-enhanced with knowledge base regulations
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpPost } from '@/services/http'

const { t } = useI18n()

const props = defineProps<{
  competitionId: string
  evaluationType: 'technical' | 'financial'
}>()

interface EvaluationSuggestion {
  offerId: string
  offerName: string
  overallScore: number
  criteriaScores: { criterionName: string; score: number; maxScore: number; justification: string }[]
  strengths: string[]
  weaknesses: string[]
  risks: string[]
  complianceStatus: 'compliant' | 'partially_compliant' | 'non_compliant'
  recommendation: string
}

const isAnalyzing = ref(false)
const suggestions = ref<EvaluationSuggestion[]>([])
const selectedOffer = ref<EvaluationSuggestion | null>(null)

async function analyzeOffers(): Promise<void> {
  isAnalyzing.value = true
  try {
    const data = await httpPost<{ suggestions: EvaluationSuggestion[] }>(
      `/v1/ai/evaluate/${props.competitionId}`,
      { evaluationType: props.evaluationType }
    )
    suggestions.value = data.suggestions
    if (data.suggestions.length > 0) {
      selectedOffer.value = data.suggestions[0]
    }
  } catch (err) {
    console.error('AI evaluation failed:', err)
  } finally {
    isAnalyzing.value = false
  }
}

function getComplianceColor(status: string): string {
  const colors: Record<string, string> = {
    compliant: 'text-success',
    partially_compliant: 'text-warning',
    non_compliant: 'text-danger',
  }
  return colors[status] || 'text-secondary'
}

function getComplianceBadge(status: string): string {
  const classes: Record<string, string> = {
    compliant: 'badge-success',
    partially_compliant: 'badge-warning',
    non_compliant: 'badge-danger',
  }
  return classes[status] || 'badge-secondary'
}

function getScoreColor(score: number, max: number): string {
  const pct = (score / max) * 100
  if (pct >= 80) return 'text-success'
  if (pct >= 60) return 'text-warning'
  return 'text-danger'
}
</script>

<template>
  <div class="space-y-5">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-ai to-ai-dark">
          <i class="pi pi-sparkles text-white"></i>
        </div>
        <div>
          <h3 class="text-sm font-bold text-secondary">
            مساعد التقييم بالذكاء الاصطناعي
          </h3>
          <p class="text-[10px] text-tertiary">
            {{ props.evaluationType === 'technical' ? 'تقييم فني' : 'تقييم مالي' }}
          </p>
        </div>
      </div>
      <button
        class="btn-ai btn-sm"
        :disabled="isAnalyzing"
        @click="analyzeOffers"
      >
        <i class="pi" :class="isAnalyzing ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
        {{ isAnalyzing ? 'جاري التحليل...' : 'تحليل العروض' }}
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="isAnalyzing" class="rounded-2xl border border-ai/20 bg-ai-50/30 p-8 text-center">
      <i class="pi pi-spin pi-spinner text-3xl text-ai mb-3"></i>
      <p class="text-sm font-medium text-ai">يتم تحليل العروض باستخدام الذكاء الاصطناعي...</p>
      <p class="mt-1 text-xs text-ai/70">قد يستغرق هذا بضع دقائق حسب عدد العروض</p>
    </div>

    <!-- Results -->
    <div v-if="suggestions.length > 0 && !isAnalyzing" class="flex gap-4">
      <!-- Offer List -->
      <div class="w-64 shrink-0 space-y-2">
        <h4 class="text-xs font-semibold text-secondary-500 uppercase tracking-wider mb-2">العروض</h4>
        <button
          v-for="s in suggestions"
          :key="s.offerId"
          class="flex w-full items-center gap-3 rounded-xl border p-3 text-start transition-all"
          :class="selectedOffer?.offerId === s.offerId
            ? 'border-ai bg-ai-50/50'
            : 'border-secondary-100 hover:border-ai/30'"
          @click="selectedOffer = s"
        >
          <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary-50">
            <span class="text-xs font-bold text-primary">{{ s.overallScore }}</span>
          </div>
          <div class="min-w-0 flex-1">
            <p class="truncate text-xs font-semibold text-secondary">{{ s.offerName }}</p>
            <span class="badge text-[10px]" :class="getComplianceBadge(s.complianceStatus)">
              {{ s.complianceStatus === 'compliant' ? 'متوافق' : s.complianceStatus === 'partially_compliant' ? 'متوافق جزئياً' : 'غير متوافق' }}
            </span>
          </div>
        </button>
      </div>

      <!-- Selected Offer Detail -->
      <div v-if="selectedOffer" class="flex-1 space-y-4">
        <!-- Score Summary -->
        <div class="rounded-2xl border border-secondary-100 bg-white p-4">
          <div class="flex items-center justify-between mb-4">
            <h4 class="text-sm font-bold text-secondary">{{ selectedOffer.offerName }}</h4>
            <div class="flex items-center gap-2">
              <span class="text-2xl font-bold text-primary">{{ selectedOffer.overallScore }}</span>
              <span class="text-xs text-secondary-400">/100</span>
            </div>
          </div>

          <!-- Criteria Scores -->
          <div class="space-y-3">
            <div v-for="cs in selectedOffer.criteriaScores" :key="cs.criterionName">
              <div class="flex items-center justify-between mb-1">
                <span class="text-xs font-medium text-secondary-700">{{ cs.criterionName }}</span>
                <span class="text-xs font-bold" :class="getScoreColor(cs.score, cs.maxScore)">
                  {{ cs.score }}/{{ cs.maxScore }}
                </span>
              </div>
              <div class="progress-bar h-1.5">
                <div
                  class="progress-bar-fill"
                  :class="getScoreColor(cs.score, cs.maxScore).replace('text-', 'bg-')"
                  :style="{ width: `${(cs.score / cs.maxScore) * 100}%` }"
                ></div>
              </div>
              <p class="mt-1 text-[10px] text-secondary-400">{{ cs.justification }}</p>
            </div>
          </div>
        </div>

        <!-- Strengths & Weaknesses -->
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div class="rounded-xl border border-success/20 bg-success-50/30 p-3">
            <h5 class="mb-2 text-xs font-bold text-success">نقاط القوة</h5>
            <ul class="space-y-1">
              <li v-for="s in selectedOffer.strengths" :key="s" class="flex items-start gap-1.5 text-[11px] text-secondary-700">
                <i class="pi pi-check-circle text-[10px] text-success mt-0.5"></i>
                {{ s }}
              </li>
            </ul>
          </div>
          <div class="rounded-xl border border-danger/20 bg-danger-50/30 p-3">
            <h5 class="mb-2 text-xs font-bold text-danger">نقاط الضعف</h5>
            <ul class="space-y-1">
              <li v-for="w in selectedOffer.weaknesses" :key="w" class="flex items-start gap-1.5 text-[11px] text-secondary-700">
                <i class="pi pi-exclamation-triangle text-[10px] text-danger mt-0.5"></i>
                {{ w }}
              </li>
            </ul>
          </div>
        </div>

        <!-- Risks -->
        <div v-if="selectedOffer.risks.length > 0" class="rounded-xl border border-warning/20 bg-warning-50/30 p-3">
          <h5 class="mb-2 text-xs font-bold text-warning">المخاطر المحتملة</h5>
          <ul class="space-y-1">
            <li v-for="r in selectedOffer.risks" :key="r" class="flex items-start gap-1.5 text-[11px] text-secondary-700">
              <i class="pi pi-shield text-[10px] text-warning mt-0.5"></i>
              {{ r }}
            </li>
          </ul>
        </div>

        <!-- Recommendation -->
        <div class="rounded-xl border border-ai/20 bg-ai-50/30 p-3">
          <h5 class="mb-1 text-xs font-bold text-ai">توصية الذكاء الاصطناعي</h5>
          <p class="text-xs text-secondary-700 leading-relaxed">{{ selectedOffer.recommendation }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
