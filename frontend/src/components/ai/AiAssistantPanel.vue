<script setup lang="ts">
/**
 * AiAssistantPanel - AI-powered evaluation assistant.
 * Provides contextual recommendations, analysis, and scoring suggestions.
 * Displays AI outputs in Arabic per project standards.
 * Features:
 * - Real-time AI analysis requests
 * - Confidence indicators
 * - Reference citations
 * - Variance alerts between human and AI scores
 * - Streaming-style text display
 */
import { ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AiEvaluation, VarianceAlert, Vendor, EvaluationCriterion } from '@/types/evaluation'

const props = defineProps<{
  aiEvaluations: AiEvaluation[]
  varianceAlerts: VarianceAlert[]
  vendors: Vendor[]
  criteria: EvaluationCriterion[]
  competitionId: string
  loading?: boolean
}>()

const emit = defineEmits<{
  requestAnalysis: [vendorId: string]
  requestFullAnalysis: []
  acceptSuggestion: [evaluation: AiEvaluation]
}>()

const { t } = useI18n()
const isExpanded = ref(true)
const activeVendorTab = ref<string | null>(null)
const showAllAlerts = ref(false)

/* Set initial active vendor tab */
watch(
  () => props.vendors,
  (vendors) => {
    if (vendors.length > 0 && !activeVendorTab.value) {
      activeVendorTab.value = vendors[0].id
    }
  },
  { immediate: true }
)

const vendorEvaluations = computed(() => {
  if (!activeVendorTab.value) return []
  return props.aiEvaluations.filter(e => e.vendorId === activeVendorTab.value)
})

const visibleAlerts = computed(() =>
  showAllAlerts.value ? props.varianceAlerts : props.varianceAlerts.slice(0, 3)
)

function getConfidenceColor(confidence: number): string {
  if (confidence >= 0.8) return 'text-success'
  if (confidence >= 0.6) return 'text-warning'
  return 'text-danger'
}

function getConfidenceLabel(confidence: number): string {
  if (confidence >= 0.8) return t('evaluation.ai.highConfidence')
  if (confidence >= 0.6) return t('evaluation.ai.mediumConfidence')
  return t('evaluation.ai.lowConfidence')
}

function getConfidenceBg(confidence: number): string {
  if (confidence >= 0.8) return 'bg-success/10'
  if (confidence >= 0.6) return 'bg-warning/10'
  return 'bg-danger/10'
}
</script>

<template>
  <div class="overflow-hidden rounded-xl border border-info/20 bg-gradient-to-b from-info/5 to-white">
    <!-- Panel header -->
    <div
      class="flex cursor-pointer items-center justify-between border-b border-info/10 bg-info/5 px-5 py-3"
      @click="isExpanded = !isExpanded"
    >
      <div class="flex items-center gap-3">
        <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-info/10">
          <i class="pi pi-sparkles text-lg text-info" />
        </div>
        <div>
          <h3 class="text-sm font-bold text-secondary">
            {{ t('evaluation.ai.title') }}
          </h3>
          <p class="text-xs text-secondary/50">
            {{ t('evaluation.ai.subtitle') }}
          </p>
        </div>
      </div>
      <div class="flex items-center gap-2">
        <!-- Alert count badge -->
        <span
          v-if="varianceAlerts.length > 0"
          class="flex h-6 min-w-[1.5rem] items-center justify-center rounded-full bg-warning/10 px-1.5 text-xs font-bold text-warning"
        >
          {{ varianceAlerts.length }}
        </span>
        <i
          class="pi text-sm text-secondary/40 transition-transform"
          :class="isExpanded ? 'pi-chevron-up' : 'pi-chevron-down'"
        />
      </div>
    </div>

    <!-- Panel content -->
    <div v-if="isExpanded" class="p-5">
      <!-- Quick actions -->
      <div class="mb-5 flex flex-wrap gap-2">
        <button
          class="flex items-center gap-1.5 rounded-lg bg-info px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-info/90"
          :disabled="loading"
          @click="emit('requestFullAnalysis')"
        >
          <i class="pi" :class="loading ? 'pi-spinner pi-spin' : 'pi-sparkles'" />
          {{ t('evaluation.ai.analyzeAll') }}
        </button>
        <button
          v-for="vendor in vendors"
          :key="`analyze-${vendor.id}`"
          class="flex items-center gap-1.5 rounded-lg border border-info/30 bg-white px-3 py-2 text-xs font-medium text-info transition-colors hover:bg-info/5"
          :disabled="loading"
          @click="emit('requestAnalysis', vendor.id)"
        >
          <i class="pi pi-sparkles" />
          {{ t('evaluation.ai.analyze') }} {{ vendor.code }}
        </button>
      </div>

      <!-- Variance Alerts Section -->
      <div v-if="varianceAlerts.length > 0" class="mb-5">
        <h4 class="mb-3 flex items-center gap-2 text-sm font-semibold text-warning">
          <i class="pi pi-exclamation-triangle" />
          {{ t('evaluation.ai.varianceAlerts') }}
          <span class="badge badge-warning">{{ varianceAlerts.length }}</span>
        </h4>
        <div class="space-y-2">
          <div
            v-for="alert in visibleAlerts"
            :key="`${alert.criterionId}-${alert.vendorId}`"
            class="flex items-center gap-3 rounded-lg border border-warning/20 bg-warning/5 p-3"
          >
            <div class="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-warning/10">
              <i class="pi pi-exclamation-triangle text-xs text-warning" />
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-xs font-medium text-secondary">
                {{ alert.vendorCode }} — {{ alert.criterionName }}
              </p>
              <div class="mt-1 flex items-center gap-3 text-xs text-secondary/60">
                <span>{{ t('evaluation.ai.humanScore') }}: <strong>{{ alert.humanScore }}</strong></span>
                <span>{{ t('evaluation.ai.aiScore') }}: <strong>{{ alert.aiScore }}</strong></span>
                <span class="font-bold text-warning">
                  {{ t('evaluation.ai.variancePercent', { percent: alert.variancePercent.toFixed(0) }) }}
                </span>
              </div>
            </div>
          </div>
          <button
            v-if="varianceAlerts.length > 3"
            class="text-xs font-medium text-info hover:underline"
            @click="showAllAlerts = !showAllAlerts"
          >
            {{ showAllAlerts
              ? t('evaluation.ai.showLess')
              : t('evaluation.ai.showAll', { count: varianceAlerts.length })
            }}
          </button>
        </div>
      </div>

      <!-- Vendor tabs -->
      <div v-if="vendors.length > 0" class="mb-4 flex gap-1 overflow-x-auto rounded-lg border border-surface-dim bg-surface-muted p-0.5">
        <button
          v-for="vendor in vendors"
          :key="`tab-${vendor.id}`"
          class="flex-shrink-0 rounded-md px-3 py-1.5 text-xs font-medium transition-all"
          :class="activeVendorTab === vendor.id
            ? 'bg-white text-info shadow-sm'
            : 'text-secondary/60 hover:text-secondary'"
          @click="activeVendorTab = vendor.id"
        >
          {{ vendor.code }}
          <span
            v-if="aiEvaluations.filter(e => e.vendorId === vendor.id).length > 0"
            class="ms-1 inline-flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-info/10 px-1 text-xs text-info"
          >
            {{ aiEvaluations.filter(e => e.vendorId === vendor.id).length }}
          </span>
        </button>
      </div>

      <!-- Loading state -->
      <div v-if="loading" class="flex flex-col items-center gap-3 py-8">
        <div class="relative">
          <div class="h-12 w-12 animate-spin rounded-full border-4 border-info/20 border-t-info" />
          <i class="pi pi-sparkles absolute inset-0 m-auto text-lg text-info" />
        </div>
        <p class="text-sm text-secondary/60">{{ t('evaluation.ai.analyzing') }}</p>
      </div>

      <!-- AI Evaluations for selected vendor -->
      <div v-else-if="vendorEvaluations.length > 0" class="space-y-3">
        <div
          v-for="evaluation in vendorEvaluations"
          :key="evaluation.id"
          class="rounded-lg border border-surface-dim bg-white p-4"
        >
          <!-- Criterion name -->
          <div class="mb-3 flex items-start justify-between">
            <div class="flex-1">
              <h5 class="text-sm font-semibold text-secondary">
                {{ criteria.find(c => c.id === evaluation.criterionId)?.name ?? '' }}
              </h5>
            </div>
            <!-- Confidence badge -->
            <div
              class="flex items-center gap-1 rounded-full px-2 py-0.5"
              :class="getConfidenceBg(evaluation.confidence)"
            >
              <div class="flex gap-0.5">
                <div
                  v-for="i in 5"
                  :key="i"
                  class="h-1.5 w-1.5 rounded-full"
                  :class="i <= Math.round(evaluation.confidence * 5)
                    ? getConfidenceColor(evaluation.confidence).replace('text-', 'bg-')
                    : 'bg-surface-dim'"
                />
              </div>
              <span class="text-xs font-medium" :class="getConfidenceColor(evaluation.confidence)">
                {{ getConfidenceLabel(evaluation.confidence) }}
              </span>
            </div>
          </div>

          <!-- Suggested score -->
          <div class="mb-3 flex items-center gap-3 rounded-lg bg-info/5 p-3">
            <div class="text-center">
              <span class="text-2xl font-bold text-info">{{ evaluation.suggestedScore }}</span>
              <span class="text-sm text-info/60"> / {{ evaluation.maxScore }}</span>
            </div>
            <div class="h-8 w-px bg-info/20" />
            <div class="flex-1">
              <div class="h-2 w-full overflow-hidden rounded-full bg-info/10">
                <div
                  class="h-full rounded-full bg-info transition-all"
                  :style="{ width: `${(evaluation.suggestedScore / evaluation.maxScore) * 100}%` }"
                />
              </div>
              <span class="mt-1 text-xs text-info/60">
                {{ ((evaluation.suggestedScore / evaluation.maxScore) * 100).toFixed(0) }}%
              </span>
            </div>
          </div>

          <!-- Justification -->
          <div class="mb-3">
            <h6 class="mb-1 text-xs font-semibold text-secondary/60">
              {{ t('evaluation.ai.justification') }}
            </h6>
            <p class="text-sm leading-relaxed text-secondary/80">
              {{ evaluation.justification }}
            </p>
          </div>

          <!-- References -->
          <div v-if="evaluation.references.length > 0" class="mb-3">
            <h6 class="mb-1 text-xs font-semibold text-secondary/60">
              {{ t('evaluation.ai.references') }}
            </h6>
            <div class="flex flex-wrap gap-1">
              <span
                v-for="(ref, idx) in evaluation.references"
                :key="idx"
                class="rounded-md bg-surface-muted px-2 py-0.5 text-xs text-secondary/70"
              >
                <i class="pi pi-file me-1 text-xs" />
                {{ ref }}
              </span>
            </div>
          </div>

          <!-- Accept suggestion button -->
          <div class="flex justify-end">
            <button
              class="flex items-center gap-1.5 rounded-lg border border-info/30 px-3 py-1.5 text-xs font-medium text-info transition-colors hover:bg-info/5"
              @click="emit('acceptSuggestion', evaluation)"
            >
              <i class="pi pi-check" />
              {{ t('evaluation.ai.acceptSuggestion') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-else class="py-8 text-center">
        <i class="pi pi-sparkles text-3xl text-info/20" />
        <p class="mt-3 text-sm text-secondary/50">
          {{ t('evaluation.ai.noAnalysis') }}
        </p>
        <p class="mt-1 text-xs text-secondary/40">
          {{ t('evaluation.ai.clickToAnalyze') }}
        </p>
      </div>
    </div>
  </div>
</template>
