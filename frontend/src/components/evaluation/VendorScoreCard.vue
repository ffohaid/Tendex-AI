<script setup lang="ts">
/**
 * VendorScoreCard - Card for scoring a single vendor on a criterion.
 * Supports blind evaluation (anonymous vendor codes).
 * Shows AI suggestion alongside human scoring.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import ScoreInput from './ScoreInput.vue'
import type { Vendor, EvaluationCriterion, AiEvaluation } from '@/types/evaluation'

const props = defineProps<{
  vendor: Vendor
  criterion: EvaluationCriterion
  score: number
  notes: string
  maxScore: number
  aiEvaluation?: AiEvaluation
  disabled?: boolean
  showVarianceAlert?: boolean
  variancePercent?: number
}>()

const emit = defineEmits<{
  'update:score': [value: number]
  'update:notes': [value: string]
}>()

const { t } = useI18n()

const hasVariance = computed(() =>
  props.showVarianceAlert && props.variancePercent !== undefined && props.variancePercent > 20
)
</script>

<template>
  <div
    class="rounded-xl border p-4 transition-all"
    :class="[
      hasVariance
        ? 'border-warning/50 bg-warning/5'
        : 'border-surface-dim bg-white',
    ]"
  >
    <!-- Vendor header (anonymous code) -->
    <div class="mb-3 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-tertiary/10 text-sm font-bold text-tertiary">
          {{ vendor.code.charAt(vendor.code.length - 1) }}
        </div>
        <span class="text-sm font-semibold text-secondary">{{ vendor.code }}</span>
      </div>

      <!-- Variance alert -->
      <div
        v-if="hasVariance"
        class="flex items-center gap-1 rounded-full bg-warning/10 px-2 py-0.5"
      >
        <i class="pi pi-exclamation-triangle text-xs text-warning" />
        <span class="text-xs font-medium text-warning">
          {{ t('evaluation.varianceAlert', { percent: variancePercent?.toFixed(0) }) }}
        </span>
      </div>
    </div>

    <!-- Score input -->
    <ScoreInput
      :model-value="score"
      :max-score="maxScore"
      :minimum-pass="criterion.minimumScore"
      :notes="notes"
      :disabled="disabled"
      :ai-suggestion="aiEvaluation?.suggestedScore"
      :ai-justification="aiEvaluation?.justification"
      @update:model-value="emit('update:score', $event)"
      @update:notes="emit('update:notes', $event)"
    />
  </div>
</template>
