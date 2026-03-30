<script setup lang="ts">
/**
 * StatusBadge - Displays evaluation status with appropriate color coding.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { EvaluationStatus } from '@/types/evaluation'

const props = defineProps<{
  status: EvaluationStatus
  size?: 'sm' | 'md'
}>()

const { t } = useI18n()

const statusConfig = computed(() => {
  const configs: Record<string, { class: string; icon: string }> = {
    pending: { class: 'badge-warning', icon: 'pi-clock' },
    in_progress: { class: 'badge-primary', icon: 'pi-spinner' },
    completed: { class: 'badge-success', icon: 'pi-check-circle' },
    approved: { class: 'badge-success', icon: 'pi-verified' },
    rejected: { class: 'badge-danger', icon: 'pi-times-circle' },
  }
  return configs[props.status || 'pending'] ?? configs.pending
})

const sizeClass = computed(() =>
  props.size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-2.5 py-1 text-xs'
)
</script>

<template>
  <span
    class="badge inline-flex items-center gap-1"
    :class="[statusConfig.class, sizeClass]"
  >
    <i class="pi text-xs" :class="statusConfig.icon" />
    {{ t(`evaluation.status.${status || 'pending'}`) }}
  </span>
</template>
