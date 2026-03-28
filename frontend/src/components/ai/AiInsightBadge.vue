<script setup lang="ts">
/**
 * AiInsightBadge - Compact AI insight indicator.
 * Shows a small badge with AI score and confidence.
 * Can be placed inline next to human scores.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps<{
  score: number
  maxScore: number
  confidence: number
  compact?: boolean
}>()

const { t } = useI18n()

const percentage = computed(() =>
  props.maxScore > 0 ? (props.score / props.maxScore) * 100 : 0
)

const confidenceLevel = computed(() => {
  if (props.confidence >= 0.8) return 'high'
  if (props.confidence >= 0.6) return 'medium'
  return 'low'
})
</script>

<template>
  <div
    class="inline-flex items-center gap-1.5 rounded-full border border-info/20 bg-info/5 px-2 py-0.5"
    :title="t('evaluation.ai.suggestedScore')"
  >
    <i class="pi pi-sparkles text-xs text-info" />
    <template v-if="compact">
      <span class="text-xs font-bold text-info">{{ percentage.toFixed(0) }}%</span>
    </template>
    <template v-else>
      <span class="text-xs font-bold text-info">{{ score }}/{{ maxScore }}</span>
      <div class="flex gap-0.5">
        <div
          v-for="i in 3"
          :key="i"
          class="h-1 w-1 rounded-full"
          :class="i <= (confidenceLevel === 'high' ? 3 : confidenceLevel === 'medium' ? 2 : 1)
            ? 'bg-info'
            : 'bg-info/20'"
        />
      </div>
    </template>
  </div>
</template>
