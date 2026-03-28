<script setup lang="ts">
/**
 * FinancialEvaluationList - Lists competitions for financial evaluation.
 * Only shows competitions that have completed technical evaluation.
 */
import { onMounted, computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import DualDateDisplay from '@/components/common/DualDateDisplay.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import { formatCurrency } from '@/utils/numbers'

const { t } = useI18n()
const router = useRouter()
const store = useEvaluationStore()

const searchQuery = ref('')
const statusFilter = ref<string>('all')

onMounted(() => {
  store.loadCompetitions()
})

/**
 * Financial evaluation is only available for competitions
 * where technical evaluation has been approved.
 */
const eligibleCompetitions = computed(() => {
  let result = store.competitions.filter(
    c => c.technicalStatus === 'approved'
  )

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
    result = result.filter(c => c.financialStatus === statusFilter.value)
  }

  return result
})

function openEvaluation(id: string) {
  router.push({ name: 'FinancialEvaluationDetail', params: { id } })
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          <i class="pi pi-wallet me-2 text-primary" />
          {{ t('evaluation.financial.title') }}
        </h1>
        <p class="mt-1 text-sm text-secondary/60">
          {{ t('evaluation.financial.subtitle') }}
        </p>
      </div>
    </div>

    <!-- Info banner: Financial evaluation prerequisite -->
    <div class="flex items-start gap-3 rounded-xl border border-info/20 bg-info/5 p-4">
      <i class="pi pi-info-circle mt-0.5 text-info" />
      <p class="text-sm text-secondary/80">
        {{ t('evaluation.financial.prerequisiteNote') }}
      </p>
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

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-primary" />
      <span class="ms-3 text-sm text-secondary/60">{{ t('common.loading') }}</span>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="eligibleCompetitions.length === 0"
      class="card text-center"
    >
      <i class="pi pi-inbox text-4xl text-secondary/20" />
      <p class="mt-3 text-sm text-secondary/60">{{ t('evaluation.financial.noEligible') }}</p>
    </div>

    <!-- Competition cards -->
    <div v-else class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div
        v-for="comp in eligibleCompetitions"
        :key="comp.id"
        class="card cursor-pointer transition-all hover:border-primary/30 hover:shadow-md"
        @click="openEvaluation(comp.id)"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="mb-2 flex items-center gap-2">
              <h3 class="text-base font-bold text-secondary">{{ comp.projectName }}</h3>
              <StatusBadge :status="comp.financialStatus" size="sm" />
            </div>
            <p class="text-xs text-secondary/60">
              {{ t('evaluation.competitionNumber') }}: {{ comp.competitionNumber }}
            </p>
          </div>
          <div class="badge badge-success">
            <i class="pi pi-check me-1" />
            {{ t('evaluation.financial.technicalApproved') }}
          </div>
        </div>

        <div class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div>
            <span class="text-xs text-secondary/50">{{ t('evaluation.passedVendors') }}</span>
            <p class="text-sm font-semibold text-success">{{ comp.passedVendorCount }}</p>
          </div>
          <div>
            <span class="text-xs text-secondary/50">{{ t('evaluation.estimatedBudget') }}</span>
            <p class="text-sm font-semibold text-secondary">{{ formatCurrency(comp.estimatedBudget) }}</p>
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

        <!-- Progress -->
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
