<script setup lang="ts">
/**
 * ActiveCompetitionsList Component
 *
 * Displays a list of active competitions with their current phase,
 * progress bar, and status indicators.
 * Data is fetched dynamically from the dashboard store.
 */
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { ActiveCompetition } from '@/types/dashboard'

defineProps<{
  competitions: ActiveCompetition[]
  isLoading: boolean
}>()

const { t, locale } = useI18n()
const { formatNumber, formatCurrency, formatDate } = useFormatters()

function getStatusClass(status: string): string {
  const map: Record<string, string> = {
    draft: 'badge-secondary',
    published: 'badge-primary',
    receiving_offers: 'badge-primary',
    technical_evaluation: 'badge-warning',
    financial_evaluation: 'badge-warning',
    awarding: 'badge-success',
    completed: 'badge-success',
    cancelled: 'badge-danger',
  }
  return map[status] ?? 'badge-secondary'
}

function getStatusLabel(status: string): string {
  return t(`dashboard.competitionStatus.${status}`)
}

function getLocalizedTitle(competition: ActiveCompetition): string {
  return locale.value === 'ar' ? competition.titleAr : competition.titleEn
}

function getProgressColor(progress: number, isDelayed: boolean): string {
  if (isDelayed) return 'bg-danger'
  if (progress >= 75) return 'bg-success'
  if (progress >= 50) return 'bg-primary'
  if (progress >= 25) return 'bg-warning'
  return 'bg-info'
}
</script>

<template>
  <div class="card">
    <div class="mb-4 flex items-center justify-between">
      <h3 class="text-lg font-semibold text-secondary">
        {{ t('dashboard.activeCompetitions') }}
      </h3>
      <router-link
        to="/rfp"
        class="text-sm font-medium text-primary hover:text-primary-dark transition-colors"
      >
        {{ t('dashboard.viewAll') }}
        <i class="pi pi-arrow-left ms-1 text-xs" v-if="locale === 'ar'"></i>
        <i class="pi pi-arrow-right ms-1 text-xs" v-else></i>
      </router-link>
    </div>

    <!-- Loading skeleton -->
    <div v-if="isLoading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="animate-pulse rounded-lg border border-surface-dim p-4">
        <div class="mb-2 h-4 w-3/4 rounded bg-surface-dim"></div>
        <div class="mb-3 h-3 w-1/2 rounded bg-surface-muted"></div>
        <div class="h-2 w-full rounded-full bg-surface-dim"></div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="competitions.length === 0"
      class="flex flex-col items-center justify-center py-8 text-center"
    >
      <i class="pi pi-inbox mb-3 text-4xl text-surface-dim"></i>
      <p class="text-sm text-tertiary">{{ t('dashboard.noActiveCompetitions') }}</p>
    </div>

    <!-- Competition list -->
    <div v-else class="space-y-3">
      <div
        v-for="competition in competitions"
        :key="competition.id"
        class="group cursor-pointer rounded-lg border border-surface-dim p-4 transition-all duration-200 hover:border-primary/30 hover:shadow-sm"
      >
        <div class="mb-2 flex items-start justify-between gap-2">
          <div class="min-w-0 flex-1">
            <h4 class="truncate text-sm font-semibold text-secondary">
              {{ getLocalizedTitle(competition) }}
            </h4>
            <p class="mt-0.5 text-xs text-tertiary">
              {{ competition.referenceNumber }}
            </p>
          </div>
          <span class="badge shrink-0" :class="getStatusClass(competition.status)">
            {{ getStatusLabel(competition.status) }}
          </span>
        </div>

        <div class="mb-3 flex items-center gap-4 text-xs text-tertiary">
          <span class="flex items-center gap-1">
            <i class="pi pi-calendar text-xs"></i>
            {{ formatDate(competition.deadline) }}
          </span>
          <span class="flex items-center gap-1">
            <i class="pi pi-file text-xs"></i>
            {{ formatNumber(competition.offersCount) }} {{ t('dashboard.offers') }}
          </span>
          <span class="flex items-center gap-1">
            <i class="pi pi-wallet text-xs"></i>
            {{ formatCurrency(competition.estimatedBudget) }}
          </span>
        </div>

        <!-- Progress bar -->
        <div class="relative h-2 w-full overflow-hidden rounded-full bg-surface-muted">
          <div
            class="absolute inset-y-0 start-0 rounded-full transition-all duration-500"
            :class="getProgressColor(competition.progress, competition.isDelayed)"
            :style="{ width: `${competition.progress}%` }"
          ></div>
        </div>
        <div class="mt-1 flex items-center justify-between text-xs">
          <span class="text-tertiary">
            {{ locale === 'ar' ? competition.currentPhase.nameAr : competition.currentPhase.nameEn }}
          </span>
          <span class="font-medium" :class="competition.isDelayed ? 'text-danger' : 'text-primary'">
            {{ formatNumber(competition.progress) }}%
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
