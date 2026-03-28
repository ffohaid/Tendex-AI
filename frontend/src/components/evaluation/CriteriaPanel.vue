<script setup lang="ts">
/**
 * CriteriaPanel - Displays evaluation criteria with weights.
 * Supports nested sub-criteria with expandable sections.
 */
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { EvaluationCriterion } from '@/types/evaluation'

defineProps<{
  criteria: EvaluationCriterion[]
  totalWeight: number
}>()

const emit = defineEmits<{
  selectCriterion: [criterion: EvaluationCriterion]
}>()

const { t } = useI18n()
const expandedIds = ref<Set<string>>(new Set())

function toggleExpand(id: string) {
  if (expandedIds.value.has(id)) {
    expandedIds.value.delete(id)
  } else {
    expandedIds.value.add(id)
  }
}
</script>

<template>
  <div class="card">
    <div class="mb-4 flex items-center justify-between">
      <h2 class="text-lg font-bold text-secondary">
        <i class="pi pi-list me-2 text-primary" />
        {{ t('evaluation.criteria.title') }}
      </h2>
      <span class="badge badge-primary">
        {{ t('evaluation.criteria.totalWeight') }}: {{ totalWeight }}%
      </span>
    </div>

    <div class="space-y-2">
      <div
        v-for="criterion in criteria"
        :key="criterion.id"
        class="overflow-hidden rounded-lg border border-surface-dim"
      >
        <!-- Main criterion -->
        <div
          class="flex cursor-pointer items-center gap-3 px-4 py-3 transition-colors hover:bg-surface-muted"
          @click="emit('selectCriterion', criterion)"
        >
          <button
            v-if="criterion.subCriteria && criterion.subCriteria.length > 0"
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-secondary/50 hover:bg-surface-dim"
            @click.stop="toggleExpand(criterion.id)"
          >
            <i
              class="pi text-xs transition-transform"
              :class="expandedIds.has(criterion.id) ? 'pi-chevron-down' : 'pi-chevron-right rtl:pi-chevron-left'"
            />
          </button>
          <div v-else class="w-6" />

          <div class="flex-1">
            <span class="text-sm font-medium text-secondary">{{ criterion.name }}</span>
            <p v-if="criterion.description" class="mt-0.5 text-xs text-secondary/60">
              {{ criterion.description }}
            </p>
          </div>

          <div class="flex items-center gap-3">
            <span class="badge badge-secondary text-xs">
              {{ criterion.weight }}%
            </span>
            <span
              v-if="criterion.minimumScore > 0"
              class="text-xs text-secondary/50"
              :title="t('evaluation.criteria.minimumScore')"
            >
              {{ t('evaluation.criteria.min') }}: {{ criterion.minimumScore }}
            </span>
          </div>
        </div>

        <!-- Sub-criteria -->
        <div
          v-if="criterion.subCriteria && criterion.subCriteria.length > 0 && expandedIds.has(criterion.id)"
          class="border-t border-surface-dim bg-surface-muted/50"
        >
          <div
            v-for="sub in criterion.subCriteria"
            :key="sub.id"
            class="flex cursor-pointer items-center gap-3 px-4 py-2 ps-12 transition-colors hover:bg-surface-muted"
            @click="emit('selectCriterion', sub)"
          >
            <i class="pi pi-minus text-xs text-secondary/30" />
            <div class="flex-1">
              <span class="text-xs font-medium text-secondary">{{ sub.name }}</span>
            </div>
            <span class="badge badge-secondary text-xs">{{ sub.weight }}%</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
