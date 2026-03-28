<script setup lang="ts">
/**
 * TechnicalEvaluationList - Lists competitions available for technical evaluation.
 * Shows status, progress, and quick actions for each competition.
 */
import { onMounted, computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import DualDateDisplay from '@/components/common/DualDateDisplay.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'

const { t } = useI18n()
const router = useRouter()
const store = useEvaluationStore()

const searchQuery = ref('')
const statusFilter = ref<string>('all')

onMounted(() => {
  store.loadCompetitions()
})

const filteredCompetitions = computed(() => {
  let result = store.competitions

  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(
      c =>
        c.competitionName.toLowerCase().includes(q) ||
        c.projectName.toLowerCase().includes(q) ||
        c.competitionNumber.includes(q)
    )
  }

  if (statusFilter.value !== 'all') {
    result = result.filter(c => c.technicalStatus === statusFilter.value)
  }

  return result
})

function openEvaluation(id: string) {
  router.push({ name: 'TechnicalEvaluationDetail', params: { id } })
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          <i class="pi pi-check-square me-2 text-primary" />
          {{ t('evaluation.technical.title') }}
        </h1>
        <p class="mt-1 text-sm text-secondary/60">
          {{ t('evaluation.technical.subtitle') }}
        </p>
      </div>
    </div>

    <!-- Filters -->
    <div class="card">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div class="relative flex-1">
          <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-secondary/40" />
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('evaluation.searchPlaceholder')"
            class="w-full rounded-lg border border-surface-dim bg-white py-2 pe-4 ps-10 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <select
          v-model="statusFilter"
          class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        >
          <option value="all">{{ t('evaluation.allStatuses') }}</option>
          <option value="pending">{{ t('evaluation.status.pending') }}</option>
          <option value="in_progress">{{ t('evaluation.status.in_progress') }}</option>
          <option value="completed">{{ t('evaluation.status.completed') }}</option>
          <option value="approved">{{ t('evaluation.status.approved') }}</option>
        </select>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-primary" />
      <span class="ms-3 text-sm text-secondary/60">{{ t('common.loading') }}</span>
    </div>

    <!-- Error state -->
    <div v-else-if="store.error" class="card border-danger/20 bg-danger/5 text-center">
      <i class="pi pi-exclamation-circle text-3xl text-danger" />
      <p class="mt-2 text-sm text-danger">{{ store.error }}</p>
      <button
        class="mt-3 rounded-lg bg-danger px-4 py-2 text-sm text-white hover:bg-danger/90"
        @click="store.loadCompetitions()"
      >
        {{ t('evaluation.retry') }}
      </button>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="filteredCompetitions.length === 0"
      class="card text-center"
    >
      <i class="pi pi-inbox text-4xl text-secondary/20" />
      <p class="mt-3 text-sm text-secondary/60">{{ t('evaluation.noCompetitions') }}</p>
    </div>

    <!-- Competition cards -->
    <div v-else class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div
        v-for="comp in filteredCompetitions"
        :key="comp.id"
        class="card cursor-pointer transition-all hover:border-primary/30 hover:shadow-md"
        @click="openEvaluation(comp.id)"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="mb-2 flex items-center gap-2">
              <h3 class="text-base font-bold text-secondary">{{ comp.projectName }}</h3>
              <StatusBadge :status="comp.technicalStatus" size="sm" />
            </div>
            <p class="text-xs text-secondary/60">
              {{ t('evaluation.competitionNumber') }}: {{ comp.competitionNumber }}
            </p>
          </div>
        </div>

        <div class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div>
            <span class="text-xs text-secondary/50">{{ t('evaluation.vendorCount') }}</span>
            <p class="text-sm font-semibold text-secondary">{{ comp.vendorCount }}</p>
          </div>
          <div>
            <span class="text-xs text-secondary/50">{{ t('evaluation.passedVendors') }}</span>
            <p class="text-sm font-semibold text-success">{{ comp.passedVendorCount }}</p>
          </div>
          <div>
            <span class="text-xs text-secondary/50">{{ t('evaluation.deadline') }}</span>
            <DualDateDisplay :date="comp.deadlineGregorian" compact />
          </div>
          <div>
            <span class="text-xs text-secondary/50">{{ t('evaluation.committee') }}</span>
            <p class="text-sm font-medium text-secondary">{{ comp.assignedCommittee }}</p>
          </div>
        </div>

        <!-- Progress bar -->
        <div class="mt-4">
          <div class="mb-1 flex items-center justify-between">
            <span class="text-xs text-secondary/50">{{ t('evaluation.progress') }}</span>
            <span class="text-xs font-semibold text-primary">{{ comp.progress }}%</span>
          </div>
          <div class="h-1.5 w-full overflow-hidden rounded-full bg-surface-muted">
            <div
              class="h-full rounded-full bg-primary transition-all duration-500"
              :style="{ width: `${comp.progress}%` }"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
