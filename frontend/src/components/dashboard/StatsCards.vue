<script setup lang="ts">
/**
 * StatsCards Component - Modern Design
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
  iconBg: string
  iconColor: string
  borderAccent: string
  value: string
  labelKey: string
}

const cards = computed<StatCard[]>(() => [
  {
    key: 'activeCompetitions',
    icon: 'pi-briefcase',
    iconBg: 'bg-primary-50',
    iconColor: 'text-primary',
    borderAccent: 'border-t-primary',
    value: formatNumber(props.stats.activeCompetitions),
    labelKey: 'dashboard.stats.activeCompetitions',
  },
  {
    key: 'completedCompetitions',
    icon: 'pi-check-circle',
    iconBg: 'bg-success-50',
    iconColor: 'text-success',
    borderAccent: 'border-t-success',
    value: formatNumber(props.stats.completedCompetitions),
    labelKey: 'dashboard.stats.completedCompetitions',
  },
  {
    key: 'pendingEvaluations',
    icon: 'pi-clipboard',
    iconBg: 'bg-warning-50',
    iconColor: 'text-warning',
    borderAccent: 'border-t-warning',
    value: formatNumber(props.stats.pendingEvaluations),
    labelKey: 'dashboard.stats.pendingEvaluations',
  },
  {
    key: 'pendingTasks',
    icon: 'pi-list-check',
    iconBg: 'bg-info-50',
    iconColor: 'text-info',
    borderAccent: 'border-t-info',
    value: formatNumber(props.stats.pendingTasks),
    labelKey: 'dashboard.stats.pendingTasks',
  },
  {
    key: 'totalOffers',
    icon: 'pi-file',
    iconBg: 'bg-secondary-50',
    iconColor: 'text-secondary',
    borderAccent: 'border-t-secondary',
    value: formatNumber(props.stats.totalOffers),
    labelKey: 'dashboard.stats.totalOffers',
  },
  {
    key: 'complianceRate',
    icon: 'pi-shield',
    iconBg: 'bg-success-50',
    iconColor: 'text-success',
    borderAccent: 'border-t-success',
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
      class="group relative overflow-hidden rounded-xl border border-secondary-100 bg-white p-5 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-lg"
      :class="['border-t-[3px]', card.borderAccent]"
    >
      <!-- Loading skeleton -->
      <template v-if="isLoading">
        <div class="mb-3 flex items-center justify-between">
          <div class="h-11 w-11 animate-pulse rounded-xl bg-secondary-100"></div>
        </div>
        <div class="h-3 w-20 animate-pulse rounded-md bg-secondary-100"></div>
        <div class="mt-2 h-8 w-14 animate-pulse rounded-md bg-secondary-50"></div>
      </template>

      <!-- Loaded content -->
      <template v-else>
        <div class="mb-3 flex items-center justify-between">
          <div
            class="flex h-11 w-11 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110"
            :class="card.iconBg"
          >
            <i class="pi text-lg" :class="[card.icon, card.iconColor]"></i>
          </div>
        </div>
        <p class="text-xs font-medium text-secondary-500">
          {{ t(card.labelKey) }}
        </p>
        <p class="mt-1 text-2xl font-bold text-secondary-800">
          {{ card.value }}
        </p>
      </template>

      <!-- Decorative gradient overlay on hover -->
      <div
        class="pointer-events-none absolute inset-0 bg-gradient-to-br from-transparent to-secondary-50/50 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
      ></div>
    </div>
  </div>
</template>
