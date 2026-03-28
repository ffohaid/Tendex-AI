<script setup lang="ts">
/**
 * AiRecommendationCard - Displays a single AI recommendation.
 * Used in the evaluation detail pages to show AI-generated insights.
 * All AI text output is in Arabic per project standards.
 */
import { useI18n } from 'vue-i18n'

defineProps<{
  title: string
  description: string
  type: 'suggestion' | 'warning' | 'insight'
  confidence?: number
  references?: string[]
}>()

const emit = defineEmits<{
  accept: []
  dismiss: []
}>()

const { t } = useI18n()

function getTypeConfig(type: string) {
  switch (type) {
    case 'warning':
      return {
        icon: 'pi-exclamation-triangle',
        borderClass: 'border-warning/30',
        bgClass: 'bg-warning/5',
        iconBg: 'bg-warning/10',
        iconColor: 'text-warning',
      }
    case 'insight':
      return {
        icon: 'pi-lightbulb',
        borderClass: 'border-primary/30',
        bgClass: 'bg-primary/5',
        iconBg: 'bg-primary/10',
        iconColor: 'text-primary',
      }
    default: // suggestion
      return {
        icon: 'pi-sparkles',
        borderClass: 'border-info/30',
        bgClass: 'bg-info/5',
        iconBg: 'bg-info/10',
        iconColor: 'text-info',
      }
  }
}
</script>

<template>
  <div
    class="rounded-xl border p-4 transition-all hover:shadow-sm"
    :class="[getTypeConfig(type).borderClass, getTypeConfig(type).bgClass]"
  >
    <div class="flex items-start gap-3">
      <!-- Icon -->
      <div
        class="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg"
        :class="getTypeConfig(type).iconBg"
      >
        <i class="pi" :class="[getTypeConfig(type).icon, getTypeConfig(type).iconColor]" />
      </div>

      <!-- Content -->
      <div class="flex-1 min-w-0">
        <div class="flex items-start justify-between gap-2">
          <h4 class="text-sm font-semibold text-secondary">{{ title }}</h4>
          <!-- Confidence -->
          <span
            v-if="confidence !== undefined"
            class="flex-shrink-0 rounded-full bg-white px-2 py-0.5 text-xs font-medium text-secondary/60"
          >
            {{ (confidence * 100).toFixed(0) }}%
          </span>
        </div>

        <p class="mt-1 text-sm leading-relaxed text-secondary/70">
          {{ description }}
        </p>

        <!-- References -->
        <div v-if="references && references.length > 0" class="mt-2 flex flex-wrap gap-1">
          <span
            v-for="(ref, idx) in references"
            :key="idx"
            class="rounded bg-white px-1.5 py-0.5 text-xs text-secondary/50"
          >
            <i class="pi pi-file me-0.5 text-xs" />{{ ref }}
          </span>
        </div>

        <!-- Actions -->
        <div class="mt-3 flex items-center gap-2">
          <button
            class="rounded-md bg-white px-3 py-1 text-xs font-medium text-info shadow-sm transition-colors hover:bg-info/5"
            @click="emit('accept')"
          >
            <i class="pi pi-check me-1" />
            {{ t('evaluation.ai.accept') }}
          </button>
          <button
            class="rounded-md px-3 py-1 text-xs font-medium text-secondary/50 transition-colors hover:text-secondary"
            @click="emit('dismiss')"
          >
            {{ t('evaluation.ai.dismiss') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
