<script setup lang="ts">
/**
 * CalendarToggle - Toggle between Hijri and Gregorian calendars.
 * Provides a compact toggle button for calendar switching.
 * Uses Umm al-Qura calendar for Hijri dates.
 */
import { useI18n } from 'vue-i18n'
import { useDualCalendar } from '@/composables/useDualCalendar'

const { t } = useI18n()
const { showHijri, toggleCalendar, calendarLabel } = useDualCalendar()
</script>

<template>
  <button
    type="button"
    class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-3 py-1.5 text-xs font-medium text-secondary transition-colors hover:bg-surface-muted"
    :title="t('evaluation.calendar.toggle')"
    @click="toggleCalendar"
  >
    <i class="pi pi-calendar text-sm text-primary" />
    <span>{{ calendarLabel }}</span>
    <i class="pi pi-sync text-xs text-secondary/40" />
  </button>
</template>
