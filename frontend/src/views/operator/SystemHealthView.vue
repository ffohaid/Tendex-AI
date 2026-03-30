<script setup lang="ts">
/**
 * SystemHealthView - System Health Dashboard for Operator (TASK-1001)
 *
 * Monitors:
 * - Service status (API, DB, Redis, RabbitMQ, Qdrant, MinIO)
 * - Resource usage (CPU, Memory, Disk)
 * - Active connections
 * - Error rates
 * - Response times
 */
import { ref, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet } from '@/services/http'

const { t } = useI18n()

interface ServiceStatus {
  name: string
  status: 'healthy' | 'degraded' | 'down'
  responseTimeMs: number
  lastCheckedAt: string
  version?: string
}

interface SystemMetrics {
  cpuUsagePercent: number
  memoryUsageMb: number
  memoryTotalMb: number
  diskUsageGb: number
  diskTotalGb: number
  activeConnections: number
  requestsPerMinute: number
  errorRate: number
  avgResponseTimeMs: number
}

const isLoading = ref(false)
const services = ref<ServiceStatus[]>([])
const metrics = ref<SystemMetrics>({
  cpuUsagePercent: 0,
  memoryUsageMb: 0,
  memoryTotalMb: 0,
  diskUsageGb: 0,
  diskTotalGb: 0,
  activeConnections: 0,
  requestsPerMinute: 0,
  errorRate: 0,
  avgResponseTimeMs: 0,
})

let refreshTimer: ReturnType<typeof setInterval> | null = null

async function loadHealth(): Promise<void> {
  isLoading.value = true
  try {
    const [servicesData, metricsData] = await Promise.all([
      httpGet<{ services: ServiceStatus[] }>('/v1/operator/health/services'),
      httpGet<SystemMetrics>('/v1/operator/health/metrics'),
    ])
    services.value = servicesData.services
    Object.assign(metrics.value, metricsData)
  } catch (err) {
    console.error('Failed to load health data:', err)
  } finally {
    isLoading.value = false
  }
}

function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    healthy: 'text-success',
    degraded: 'text-warning',
    down: 'text-danger',
  }
  return colors[status] || 'text-secondary'
}

function getStatusBg(status: string): string {
  const bgs: Record<string, string> = {
    healthy: 'bg-success',
    degraded: 'bg-warning',
    down: 'bg-danger',
  }
  return bgs[status] || 'bg-secondary'
}

function getUsageColor(percent: number): string {
  if (percent < 60) return 'bg-success'
  if (percent < 80) return 'bg-warning'
  return 'bg-danger'
}

onMounted(() => {
  loadHealth()
  refreshTimer = setInterval(loadHealth, 15000)
})

onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer)
})
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="page-title">System Health</h1>
      <p class="page-description">Real-time monitoring of all system services and resources</p>
    </div>

    <!-- Service Status Grid -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="service in services"
        :key="service.name"
        class="card !p-4"
      >
        <div class="flex items-center justify-between mb-3">
          <h3 class="text-sm font-bold text-secondary">{{ service.name }}</h3>
          <div class="flex items-center gap-1.5">
            <span class="h-2.5 w-2.5 rounded-full" :class="[getStatusBg(service.status), service.status === 'healthy' ? 'animate-pulse' : '']"></span>
            <span class="text-xs font-semibold capitalize" :class="getStatusColor(service.status)">
              {{ service.status }}
            </span>
          </div>
        </div>
        <div class="flex items-center justify-between text-xs text-secondary-500">
          <span>{{ service.responseTimeMs }}ms</span>
          <span v-if="service.version">v{{ service.version }}</span>
        </div>
      </div>
    </div>

    <!-- Resource Metrics -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <!-- CPU -->
      <div class="card !p-4">
        <div class="flex items-center gap-3 mb-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-info-50">
            <i class="pi pi-microchip text-info"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">CPU Usage</p>
            <p class="text-xl font-bold text-secondary">{{ metrics.cpuUsagePercent }}%</p>
          </div>
        </div>
        <div class="progress-bar">
          <div class="progress-bar-fill" :class="getUsageColor(metrics.cpuUsagePercent)" :style="{ width: `${metrics.cpuUsagePercent}%` }"></div>
        </div>
      </div>

      <!-- Memory -->
      <div class="card !p-4">
        <div class="flex items-center gap-3 mb-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-50">
            <i class="pi pi-server text-ai"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Memory</p>
            <p class="text-xl font-bold text-secondary">
              {{ (metrics.memoryUsageMb / 1024).toFixed(1) }} / {{ (metrics.memoryTotalMb / 1024).toFixed(1) }} GB
            </p>
          </div>
        </div>
        <div class="progress-bar">
          <div
            class="progress-bar-fill"
            :class="getUsageColor((metrics.memoryUsageMb / metrics.memoryTotalMb) * 100)"
            :style="{ width: `${(metrics.memoryUsageMb / metrics.memoryTotalMb) * 100}%` }"
          ></div>
        </div>
      </div>

      <!-- Active Connections -->
      <div class="card !p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-success-50">
            <i class="pi pi-users text-success"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Active Connections</p>
            <p class="text-xl font-bold text-secondary">{{ metrics.activeConnections }}</p>
          </div>
        </div>
      </div>

      <!-- Error Rate -->
      <div class="card !p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl" :class="metrics.errorRate > 1 ? 'bg-danger-50' : 'bg-success-50'">
            <i class="pi pi-exclamation-triangle" :class="metrics.errorRate > 1 ? 'text-danger' : 'text-success'"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Error Rate</p>
            <p class="text-xl font-bold" :class="metrics.errorRate > 1 ? 'text-danger' : 'text-success'">
              {{ metrics.errorRate.toFixed(2) }}%
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
