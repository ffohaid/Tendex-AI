<script setup lang="ts">
/**
 * AutoSaveIndicator Component.
 *
 * Displays the current auto-save status with appropriate
 * visual feedback (icon, color, message).
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AutoSaveStatus } from '@/types/rfp'

const props = defineProps<{
  status: AutoSaveStatus
  lastSavedAt: string | null
}>()

const { t } = useI18n()

const statusConfig = computed(() => {
  switch (props.status) {
    case 'saving':
      return {
        icon: 'pi pi-spin pi-spinner',
        colorClass: 'text-info',
        messageKey: 'rfp.autoSave.saving',
      }
    case 'saved':
      return {
        icon: 'pi pi-check-circle',
        colorClass: 'text-success',
        messageKey: 'rfp.autoSave.saved',
      }
    case 'error':
      return {
        icon: 'pi pi-exclamation-triangle',
        colorClass: 'text-danger',
        messageKey: 'rfp.autoSave.error',
      }
    default:
      return {
        icon: 'pi pi-clock',
        colorClass: 'text-tertiary',
        messageKey: 'rfp.autoSave.idle',
      }
  }
})

const formattedTime = computed(() => {
  if (!props.lastSavedAt) return ''
  const date = new Date(props.lastSavedAt)
  return date.toLocaleTimeString('en-SA', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
})
</script>

<template>
  <div
    class="flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm"
    :class="[
      status === 'error' ? 'bg-danger/5' : 'bg-surface-muted',
    ]"
    role="status"
    :aria-label="t(statusConfig.messageKey)"
  >
    <i :class="[statusConfig.icon, statusConfig.colorClass]" aria-hidden="true"></i>
    <span :class="statusConfig.colorClass">
      {{ t(statusConfig.messageKey) }}
    </span>
    <span v-if="formattedTime && status === 'saved'" class="text-tertiary">
      ({{ formattedTime }})
    </span>
  </div>
</template>
