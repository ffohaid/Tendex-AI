<script setup lang="ts">
/**
 * ScoreInput - Score entry component for evaluation criteria.
 * Shows score input with visual indicator and notes field.
 */
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps<{
  modelValue: number
  maxScore: number
  minimumPass?: number
  notes?: string
  disabled?: boolean
  aiSuggestion?: number
  aiJustification?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: number]
  'update:notes': [value: string]
}>()

const { t } = useI18n()
const showNotes = ref(false)
const localNotes = ref(props.notes ?? '')

const percentage = computed(() =>
  props.maxScore > 0 ? (props.modelValue / props.maxScore) * 100 : 0
)

const colorClass = computed(() => {
  if (percentage.value >= 80) return 'text-success'
  if (percentage.value >= 60) return 'text-warning'
  return 'text-danger'
})

const barColor = computed(() => {
  if (percentage.value >= 80) return 'bg-success'
  if (percentage.value >= 60) return 'bg-warning'
  return 'bg-danger'
})

const isPassing = computed(() =>
  props.minimumPass ? props.modelValue >= props.minimumPass : true
)

function onScoreChange(event: Event) {
  const target = event.target as HTMLInputElement
  let value = parseFloat(target.value)
  if (isNaN(value)) value = 0
  if (value < 0) value = 0
  if (value > props.maxScore) value = props.maxScore
  emit('update:modelValue', value)
}

function onNotesChange() {
  emit('update:notes', localNotes.value)
}
</script>

<template>
  <div class="flex flex-col gap-2">
    <!-- Score input row -->
    <div class="flex items-center gap-3">
      <div class="relative flex items-center">
        <input
          type="number"
          :value="modelValue"
          :max="maxScore"
          :min="0"
          step="1"
          :disabled="disabled"
          class="w-20 rounded-lg border border-surface-dim bg-white px-3 py-2 text-center text-sm font-semibold focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-muted disabled:text-secondary/50"
          :class="colorClass"
          @input="onScoreChange"
        />
        <span class="ms-1 text-xs text-secondary/60">/ {{ maxScore }}</span>
      </div>

      <!-- Progress bar -->
      <div class="flex-1">
        <div class="h-2 w-full overflow-hidden rounded-full bg-surface-muted">
          <div
            class="h-full rounded-full transition-all duration-300"
            :class="barColor"
            :style="{ width: `${percentage}%` }"
          />
        </div>
      </div>

      <!-- Percentage -->
      <span class="min-w-[3rem] text-end text-sm font-medium" :class="colorClass">
        {{ percentage.toFixed(0) }}%
      </span>

      <!-- Pass/Fail indicator -->
      <span v-if="minimumPass" class="text-xs">
        <i
          class="pi"
          :class="isPassing ? 'pi-check-circle text-success' : 'pi-times-circle text-danger'"
        />
      </span>

      <!-- Notes toggle -->
      <button
        type="button"
        class="rounded-md p-1 text-secondary/50 transition-colors hover:bg-surface-muted hover:text-secondary"
        @click="showNotes = !showNotes"
      >
        <i class="pi pi-comment text-sm" />
      </button>
    </div>

    <!-- AI suggestion -->
    <div
      v-if="aiSuggestion !== undefined"
      class="flex items-start gap-2 rounded-lg border border-info/20 bg-info/5 p-2"
    >
      <i class="pi pi-sparkles mt-0.5 text-sm text-info" />
      <div class="flex-1">
        <div class="flex items-center gap-2">
          <span class="text-xs font-medium text-info">
            {{ t('evaluation.ai.suggestedScore') }}:
          </span>
          <span class="text-sm font-bold text-info">{{ aiSuggestion }} / {{ maxScore }}</span>
        </div>
        <p v-if="aiJustification" class="mt-1 text-xs leading-relaxed text-secondary/70">
          {{ aiJustification }}
        </p>
      </div>
    </div>

    <!-- Notes textarea -->
    <div v-if="showNotes" class="mt-1">
      <textarea
        v-model="localNotes"
        :placeholder="t('evaluation.notesPlaceholder')"
        :disabled="disabled"
        rows="2"
        class="w-full resize-none rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:bg-surface-muted"
        @blur="onNotesChange"
      />
    </div>
  </div>
</template>
