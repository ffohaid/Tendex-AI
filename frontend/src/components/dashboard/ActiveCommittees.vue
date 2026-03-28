<script setup lang="ts">
/**
 * ActiveCommittees Component
 *
 * Displays active committees with their status, member count,
 * and expiry dates. Supports both permanent and temporary committees.
 */
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { ActiveCommittee } from '@/types/dashboard'

defineProps<{
  committees: ActiveCommittee[]
  isLoading: boolean
}>()

const { t, locale } = useI18n()
const { formatDate, formatNumber } = useFormatters()

function getStatusClass(status: string): string {
  const map: Record<string, string> = {
    active: 'badge-success',
    expired: 'badge-danger',
    suspended: 'badge-warning',
  }
  return map[status] ?? 'badge-secondary'
}

function getTypeIcon(type: string): string {
  return type === 'permanent' ? 'pi-building' : 'pi-clock'
}

function getLocalizedName(committee: ActiveCommittee): string {
  return locale.value === 'ar' ? committee.nameAr : committee.nameEn
}
</script>

<template>
  <div class="card">
    <div class="mb-4 flex items-center justify-between">
      <h3 class="text-lg font-semibold text-secondary">
        {{ t('dashboard.activeCommittees') }}
      </h3>
      <router-link
        to="/committees/permanent"
        class="text-sm font-medium text-primary hover:text-primary-dark transition-colors"
      >
        {{ t('dashboard.viewAll') }}
      </router-link>
    </div>

    <!-- Loading skeleton -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="animate-pulse rounded-lg border border-surface-dim p-3">
        <div class="mb-2 h-4 w-2/3 rounded bg-surface-dim"></div>
        <div class="flex gap-4">
          <div class="h-3 w-16 rounded bg-surface-muted"></div>
          <div class="h-3 w-20 rounded bg-surface-muted"></div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="committees.length === 0"
      class="flex flex-col items-center justify-center py-8 text-center"
    >
      <i class="pi pi-users mb-3 text-4xl text-surface-dim"></i>
      <p class="text-sm text-tertiary">{{ t('dashboard.noCommittees') }}</p>
    </div>

    <!-- Committees list -->
    <div v-else class="space-y-2">
      <div
        v-for="committee in committees"
        :key="committee.id"
        class="rounded-lg border border-surface-dim p-3 transition-all duration-200 hover:border-primary/30 hover:shadow-sm"
      >
        <div class="flex items-start justify-between gap-2">
          <div class="flex items-center gap-2">
            <i class="pi text-sm text-tertiary" :class="getTypeIcon(committee.type)"></i>
            <h4 class="text-sm font-medium text-secondary">
              {{ getLocalizedName(committee) }}
            </h4>
          </div>
          <span class="badge text-[10px]" :class="getStatusClass(committee.status)">
            {{ t(`dashboard.committeeStatus.${committee.status}`) }}
          </span>
        </div>
        <div class="mt-2 flex items-center gap-4 text-xs text-tertiary">
          <span class="flex items-center gap-1">
            <i class="pi pi-users text-xs"></i>
            {{ formatNumber(committee.membersCount) }} {{ t('dashboard.members') }}
          </span>
          <span v-if="committee.expiryDate" class="flex items-center gap-1">
            <i class="pi pi-calendar text-xs"></i>
            {{ formatDate(committee.expiryDate) }}
          </span>
          <span class="badge text-[10px]" :class="committee.type === 'permanent' ? 'badge-primary' : 'badge-warning'">
            {{ t(`dashboard.committeeType.${committee.type}`) }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
