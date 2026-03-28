<script setup lang="ts">
/**
 * DualDateDisplay - Shows both Hijri and Gregorian dates.
 * Supports compact and full display modes.
 * Uses Umm al-Qura calendar for Hijri dates.
 */
import { computed } from 'vue'
import { useDualCalendar } from '@/composables/useDualCalendar'

const props = withDefaults(defineProps<{
  date: string | Date
  compact?: boolean
  showToggle?: boolean
}>(), {
  compact: false,
  showToggle: false,
})

const { formatDualDate, formatCompactDual, showHijri, toggleCalendar, calendarLabel } = useDualCalendar()

const dualDate = computed(() => formatDualDate(props.date))
const compactDate = computed(() => formatCompactDual(props.date))
</script>

<template>
  <div class="inline-flex items-center gap-1.5">
    <!-- Compact mode: single line -->
    <template v-if="compact">
      <span class="text-sm text-secondary">{{ compactDate }}</span>
    </template>

    <!-- Full mode: primary + secondary -->
    <template v-else>
      <div class="flex flex-col">
        <span class="text-sm font-medium text-secondary">
          {{ showHijri ? dualDate.hijri : dualDate.gregorian }}
        </span>
        <span class="text-xs text-secondary/60">
          {{ showHijri ? dualDate.gregorian : dualDate.hijri }}
        </span>
      </div>
    </template>

    <!-- Toggle button -->
    <button
      v-if="showToggle"
      type="button"
      class="ms-1 rounded-md border border-surface-dim px-1.5 py-0.5 text-xs text-secondary/70 transition-colors hover:bg-surface-muted"
      :title="calendarLabel"
      @click="toggleCalendar"
    >
      <i class="pi pi-calendar text-xs" />
    </button>
  </div>
</template>
