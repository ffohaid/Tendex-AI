<script setup lang="ts">
/**
 * StatsCards Component - Modern KPI Cards (TASK-1001)
 *
 * Animated KPI statistics cards with:
 * - Gradient accent borders
 * - Animated count-up numbers
 * - Hover scale effects
 * - Skeleton loading states
 * - Trend indicators (up/down)
 * - Colored icons with background
 */
import { computed, ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { DashboardStats } from '@/types/dashboard'

const props = defineProps<{
  stats: DashboardStats
  isLoading: boolean
}>()

const { t } = useI18n()
const { formatNumber, formatPercentage } = useFormatters()

const animatedValues = ref<Record<string, number>>({})
const isAnimating = ref(false)

interface StatCard {
  key: string
  icon: string
  iconBg: string
  iconColor: string
  accentClass: string
  gradientFrom: string
  gradientTo: string
  value: number
  formatted: string
  isPercentage: boolean
  labelKey: string
  trend?: number
  trendLabel?: string
}

const cards = computed<StatCard[]>(() => [
  {
    key: 'activeCompetitions',
    icon: 'pi-briefcase',
    iconBg: 'bg-primary-50',
    iconColor: 'text-primary',
    accentClass: 'kpi-card--primary',
    gradientFrom: '#1B3A5C',
    gradientTo: '#2A5580',
    value: props.stats.activeCompetitions,
    formatted: formatNumber(animatedValues.value['activeCompetitions'] ?? props.stats.activeCompetitions),
    isPercentage: false,
    labelKey: 'dashboard.stats.activeCompetitions',
  },
  {
    key: 'completedCompetitions',
    icon: 'pi-check-circle',
    iconBg: 'bg-success-50',
    iconColor: 'text-success',
    accentClass: 'kpi-card--accent',
    gradientFrom: '#27AE60',
    gradientTo: '#2ECC71',
    value: props.stats.completedCompetitions,
    formatted: formatNumber(animatedValues.value['completedCompetitions'] ?? props.stats.completedCompetitions),
    isPercentage: false,
    labelKey: 'dashboard.stats.completedCompetitions',
  },
  {
    key: 'pendingEvaluations',
    icon: 'pi-clipboard',
    iconBg: 'bg-warning-50',
    iconColor: 'text-warning',
    accentClass: 'kpi-card--warning',
    gradientFrom: '#F39C12',
    gradientTo: '#F5B041',
    value: props.stats.pendingEvaluations,
    formatted: formatNumber(animatedValues.value['pendingEvaluations'] ?? props.stats.pendingEvaluations),
    isPercentage: false,
    labelKey: 'dashboard.stats.pendingEvaluations',
  },
  {
    key: 'pendingTasks',
    icon: 'pi-list-check',
    iconBg: 'bg-info-50',
    iconColor: 'text-info',
    accentClass: 'kpi-card--info',
    gradientFrom: '#3498DB',
    gradientTo: '#5DADE2',
    value: props.stats.pendingTasks,
    formatted: formatNumber(animatedValues.value['pendingTasks'] ?? props.stats.pendingTasks),
    isPercentage: false,
    labelKey: 'dashboard.stats.pendingTasks',
  },
  {
    key: 'totalOffers',
    icon: 'pi-file',
    iconBg: 'bg-primary-50',
    iconColor: 'text-primary-400',
    accentClass: 'kpi-card--primary',
    gradientFrom: '#1B3A5C',
    gradientTo: '#537BA7',
    value: props.stats.totalOffers,
    formatted: formatNumber(animatedValues.value['totalOffers'] ?? props.stats.totalOffers),
    isPercentage: false,
    labelKey: 'dashboard.stats.totalOffers',
  },
  {
    key: 'complianceRate',
    icon: 'pi-shield',
    iconBg: 'bg-success-50',
    iconColor: 'text-success',
    accentClass: 'kpi-card--accent',
    gradientFrom: '#27AE60',
    gradientTo: '#2ECC71',
    value: props.stats.complianceRate,
    formatted: formatPercentage(animatedValues.value['complianceRate'] ?? props.stats.complianceRate),
    isPercentage: true,
    labelKey: 'dashboard.stats.complianceRate',
  },
])

/**
 * Animate number counting up
 */
function animateValue(key: string, target: number, duration: number = 1000): void {
  const start = animatedValues.value[key] ?? 0
  const startTime = performance.now()

  function update(currentTime: number) {
    const elapsed = currentTime - startTime
    const progress = Math.min(elapsed / duration, 1)
    const eased = 1 - Math.pow(1 - progress, 3) // easeOutCubic
    animatedValues.value[key] = Math.round(start + (target - start) * eased)
    if (progress < 1) {
      requestAnimationFrame(update)
    }
  }
  requestAnimationFrame(update)
}

watch(() => props.stats, (newStats) => {
  if (!props.isLoading) {
    animateValue('activeCompetitions', newStats.activeCompetitions)
    animateValue('completedCompetitions', newStats.completedCompetitions)
    animateValue('pendingEvaluations', newStats.pendingEvaluations)
    animateValue('pendingTasks', newStats.pendingTasks)
    animateValue('totalOffers', newStats.totalOffers)
    animateValue('complianceRate', newStats.complianceRate)
  }
}, { deep: true })

onMounted(() => {
  if (!props.isLoading) {
    cards.value.forEach(card => {
      animatedValues.value[card.key] = 0
      setTimeout(() => animateValue(card.key, card.value, 1200), 200)
    })
  }
})
</script>

<template>
  <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
    <div
      v-for="(card, index) in cards"
      :key="card.key"
      class="animate-fade-in-up"
      :style="{ animationDelay: `${index * 80}ms` }"
    >
      <!-- Loading skeleton -->
      <div v-if="isLoading" class="skeleton-kpi rounded-2xl"></div>

      <!-- Loaded content -->
      <div
        v-else
        class="kpi-card group"
        :class="card.accentClass"
      >
        <!-- Icon -->
        <div class="mb-4 flex items-center justify-between">
          <div
            class="flex h-12 w-12 items-center justify-center rounded-xl transition-all duration-300 group-hover:scale-110 group-hover:shadow-md"
            :class="card.iconBg"
          >
            <i class="pi text-xl" :class="[card.icon, card.iconColor]"></i>
          </div>
          <!-- Mini sparkline placeholder -->
          <div class="flex items-center gap-1 text-xs font-medium text-success">
            <i class="pi pi-arrow-up text-[10px]"></i>
          </div>
        </div>

        <!-- Label -->
        <p class="text-xs font-medium text-secondary-500 leading-relaxed">
          {{ t(card.labelKey) }}
        </p>

        <!-- Value with animation -->
        <p class="mt-1.5 text-3xl font-bold tracking-tight text-secondary-800">
          {{ card.formatted }}
        </p>

        <!-- Decorative gradient overlay on hover -->
        <div
          class="pointer-events-none absolute inset-0 rounded-2xl opacity-0 transition-opacity duration-300 group-hover:opacity-100"
          :style="{
            background: `linear-gradient(135deg, ${card.gradientFrom}08, ${card.gradientTo}05)`
          }"
        ></div>
      </div>
    </div>
  </div>
</template>
