<script setup lang="ts">
/**
 * System Health Panel — Operator Dashboard.
 *
 * Displays the health status of all infrastructure services
 * (SQL Server, Redis, RabbitMQ, MinIO, Qdrant) with response times.
 *
 * All data from API — NO mock data.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SystemHealthStatusDto } from '@/types/operatorDashboard'

const props = defineProps<{
  health: SystemHealthStatusDto | null
  isLoading: boolean
}>()

const { t } = useI18n()

const overallStatusClass = computed(() => {
  const status = props.health?.overallStatus
  if (status === 'Healthy') return 'bg-emerald-100 text-emerald-800'
  if (status === 'Degraded') return 'bg-amber-100 text-amber-800'
  return 'bg-red-100 text-red-800'
})

const overallStatusIcon = computed(() => {
  const status = props.health?.overallStatus
  if (status === 'Healthy') return 'pi pi-check-circle text-emerald-600'
  if (status === 'Degraded') return 'pi pi-exclamation-triangle text-amber-600'
  return 'pi pi-times-circle text-red-600'
})

function getServiceStatusClass(status: string): string {
  if (status === 'Healthy') return 'text-emerald-600'
  if (status === 'Degraded') return 'text-amber-600'
  return 'text-red-600'
}

function getServiceStatusIcon(status: string): string {
  if (status === 'Healthy') return 'pi pi-check-circle'
  if (status === 'Degraded') return 'pi pi-exclamation-triangle'
  return 'pi pi-times-circle'
}

function formatResponseTime(ms: number): string {
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

function formatCheckedAt(dateStr: string): string {
  try {
    const date = new Date(dateStr)
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  } catch {
    return dateStr
  }
}
</script>

<template>
  <div class="rounded-lg border border-surface-dim bg-white p-6">
    <div class="mb-4 flex items-center justify-between">
      <h3 class="text-lg font-semibold text-secondary">
        {{ t('operatorDashboard.systemHealth.title') }}
      </h3>
      <span
        v-if="health && !isLoading"
        :class="['rounded-full px-3 py-1 text-xs font-medium', overallStatusClass]"
      >
        <i :class="[overallStatusIcon, 'me-1']"></i>
        {{ t(`operatorDashboard.systemHealth.status.${health.overallStatus}`, health.overallStatus) }}
      </span>
    </div>

    <!-- Skeleton -->
    <div v-if="isLoading" class="animate-pulse space-y-3">
      <div v-for="i in 3" :key="i" class="flex items-center justify-between rounded-lg border border-surface-dim p-3">
        <div class="flex items-center gap-3">
          <div class="h-8 w-8 rounded-full bg-surface-dim"></div>
          <div class="h-4 w-24 rounded bg-surface-dim"></div>
        </div>
        <div class="h-4 w-16 rounded bg-surface-dim"></div>
      </div>
    </div>

    <!-- No data -->
    <div v-else-if="!health" class="flex h-40 items-center justify-center text-tertiary">
      <div class="text-center">
        <i class="pi pi-server mb-2 text-3xl"></i>
        <p>{{ t('operatorDashboard.noData') }}</p>
      </div>
    </div>

    <!-- Services list -->
    <div v-else class="space-y-2">
      <div
        v-for="service in health.services"
        :key="service.serviceName"
        class="flex items-center justify-between rounded-lg border border-surface-dim p-3"
      >
        <div class="flex items-center gap-3">
          <i :class="[getServiceStatusIcon(service.status), getServiceStatusClass(service.status), 'text-lg']"></i>
          <div>
            <p class="text-sm font-medium text-secondary">{{ service.serviceName }}</p>
            <p v-if="service.description" class="text-xs text-tertiary">{{ service.description }}</p>
          </div>
        </div>
        <div class="text-end">
          <span :class="['text-sm font-medium', getServiceStatusClass(service.status)]">
            {{ t(`operatorDashboard.systemHealth.status.${service.status}`, service.status) }}
          </span>
          <p class="text-xs text-tertiary">{{ formatResponseTime(service.responseTimeMs) }}</p>
        </div>
      </div>

      <!-- Last checked -->
      <p class="mt-2 text-xs text-tertiary">
        {{ t('operatorDashboard.systemHealth.lastChecked') }}: {{ formatCheckedAt(health.checkedAt) }}
      </p>
    </div>
  </div>
</template>
