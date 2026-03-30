<script setup lang="ts">
/**
 * ConsumptionView - AI & Resource Consumption Monitoring (TASK-1001)
 *
 * Tracks:
 * - AI API calls per tenant
 * - Token usage (input/output)
 * - Storage consumption
 * - Cost estimates
 * - Usage trends
 */
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet } from '@/services/http'

const { t } = useI18n()

interface TenantConsumption {
  tenantId: string
  tenantName: string
  aiCalls: number
  tokensUsed: number
  storageUsedMb: number
  estimatedCost: number
  period: string
}

const isLoading = ref(false)
const consumptionData = ref<TenantConsumption[]>([])
const selectedPeriod = ref('current_month')

const totalCalls = ref(0)
const totalTokens = ref(0)
const totalStorage = ref(0)
const totalCost = ref(0)

async function loadConsumption(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{
      items: TenantConsumption[]
      totals: { calls: number; tokens: number; storageMb: number; cost: number }
    }>('/v1/operator/consumption', {
      params: { period: selectedPeriod.value },
    })
    consumptionData.value = data.items
    totalCalls.value = data.totals.calls
    totalTokens.value = data.totals.tokens
    totalStorage.value = data.totals.storageMb
    totalCost.value = data.totals.cost
  } catch (err) {
    console.error('Failed to load consumption:', err)
  } finally {
    isLoading.value = false
  }
}

function formatNumber(num: number): string {
  return new Intl.NumberFormat('en-US').format(num)
}

onMounted(() => {
  loadConsumption()
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">Consumption Monitoring</h1>
        <p class="page-description">Track AI usage and resource consumption across tenants</p>
      </div>
      <select v-model="selectedPeriod" class="input w-48 text-sm" @change="loadConsumption">
        <option value="current_month">Current Month</option>
        <option value="last_month">Last Month</option>
        <option value="last_3_months">Last 3 Months</option>
        <option value="last_year">Last Year</option>
      </select>
    </div>

    <!-- Summary Cards -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div class="card !p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-50">
            <i class="pi pi-sparkles text-ai"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Total AI Calls</p>
            <p class="text-xl font-bold text-secondary">{{ formatNumber(totalCalls) }}</p>
          </div>
        </div>
      </div>
      <div class="card !p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-info-50">
            <i class="pi pi-code text-info"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Total Tokens</p>
            <p class="text-xl font-bold text-secondary">{{ formatNumber(totalTokens) }}</p>
          </div>
        </div>
      </div>
      <div class="card !p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-warning-50">
            <i class="pi pi-database text-warning"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Storage Used</p>
            <p class="text-xl font-bold text-secondary">{{ (totalStorage / 1024).toFixed(1) }} GB</p>
          </div>
        </div>
      </div>
      <div class="card !p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-success-50">
            <i class="pi pi-dollar text-success"></i>
          </div>
          <div>
            <p class="text-xs text-tertiary">Estimated Cost</p>
            <p class="text-xl font-bold text-secondary">{{ formatNumber(totalCost) }} &#xFDFC;</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Tenant Consumption Table -->
    <div class="card overflow-hidden !p-0">
      <div class="border-b border-secondary-100 bg-surface-subtle px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">Per-Tenant Consumption</h3>
      </div>
      <div v-if="isLoading" class="p-6">
        <div v-for="i in 5" :key="i" class="skeleton-text mb-3"></div>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="table-modern">
          <thead>
            <tr>
              <th>Tenant</th>
              <th class="text-center">AI Calls</th>
              <th class="text-center">Tokens</th>
              <th class="text-center">Storage (MB)</th>
              <th class="text-end">Est. Cost (&#xFDFC;)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in consumptionData" :key="item.tenantId">
              <td class="font-medium">{{ item.tenantName }}</td>
              <td class="text-center">{{ formatNumber(item.aiCalls) }}</td>
              <td class="text-center">{{ formatNumber(item.tokensUsed) }}</td>
              <td class="text-center">{{ formatNumber(item.storageUsedMb) }}</td>
              <td class="text-end font-semibold">{{ formatNumber(item.estimatedCost) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
