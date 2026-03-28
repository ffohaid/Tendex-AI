<script setup lang="ts">
/**
 * StatsCards Component
 *
 * Displays quick KPI statistics as cards at the top of the dashboard.
 * Data is fetched dynamically from the dashboard store.
 * Supports RTL/LTR and uses English numerals exclusively.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { DashboardStats } from '@/types/dashboard'

const props = defineProps<{
  stats: DashboardStats
  isLoading: boolean
}>()

const { t } = useI18n()
const { formatNumber, formatPercentage } = useFormatters()

interface StatCard {
  key: string
  icon: string
  color: string
  bgColor: string
  value: string
  labelKey: string
  trend?: 'up' | 'down' | 'neutral'
}

const cards = computed<StatCard[]>(() => [
  {
    key: 'activeCompetitions',
    icon: 'pi-briefcase',
    color: 'text-primary',
    bgColor: 'bg-primary/10',
    value: formatNumber(props.stats.activeCompetitions),
    labelKey: 'dashboard.stats.activeCompetitions',
  },
  {
    key: 'completedCompetitions',
    icon: 'pi-check-circle',
    color: 'text-success',
    bgColor: 'bg-success/10',
    value: formatNumber(props.stats.completedCompetitions),
    labelKey: 'dashboard.stats.completedCompetitions',
  },
  {
    key: 'pendingEvaluations',
    icon: 'pi-clipboard',
    color: 'text-warning',
    bgColor: 'bg-warning/10',
    value: formatNumber(props.stats.pendingEvaluations),
    labelKey: 'dashboard.stats.pendingEvaluations',
  },
  {
    key: 'pendingTasks',
    icon: 'pi-list-check',
    color: 'text-info',
    bgColor: 'bg-info/10',
    value: formatNumber(props.stats.pendingTasks),
    labelKey: 'dashboard.stats.pendingTasks',
  },
  {
    key: 'totalOffers',
    icon: 'pi-file',
    color: 'text-tertiary',
    bgColor: 'bg-tertiary/10',
    value: formatNumber(props.stats.totalOffers),
    labelKey: 'dashboard.stats.totalOffers',
  },
  {
    key: 'complianceRate',
    icon: 'pi-shield',
    color: 'text-success',
    bgColor: 'bg-success/10',
    value: formatPercentage(props.stats.complianceRate),
    labelKey: 'dashboard.stats.complianceRate',
  },
])
</script>

<template>
  <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
    <div
      v-for="card in cards"
      :key="card.key"
      class="card group transition-all duration-200 hover:shadow-md"
    >
      <!-- Loading skeleton -->
      <template v-if="isLoading">
        <div class="mb-3 flex items-center justify-between">
          <div class="h-10 w-10 animate-pulse rounded-lg bg-surface-dim"></div>
        </div>
        <div class="h-3 w-20 animate-pulse rounded bg-surface-dim"></div>
        <div class="mt-2 h-7 w-14 animate-pulse rounded bg-surface-muted"></div>
      </template>

      <!-- Loaded content -->
      <template v-else>
        <div class="mb-3 flex items-center justify-between">
          <div
            class="flex h-10 w-10 items-center justify-center rounded-lg transition-transform duration-200 group-hover:scale-110"
            :class="card.bgColor"
          >
            <i class="pi text-lg" :class="[card.icon, card.color]"></i>
          </div>
        </div>
        <p class="text-xs font-medium text-tertiary">
          {{ t(card.labelKey) }}
        </p>
        <p class="mt-1 text-2xl font-bold text-secondary" :class="card.color">
          {{ card.value }}
        </p>
      </template>
    </div>
  </div>
</template>
