<script setup lang="ts">
/**
 * ConsumptionView - AI & Resource Consumption Monitoring
 *
 * Tracks:
 * - AI API calls per tenant
 * - Token usage (input/output)
 * - Storage consumption
 * - Cost estimates
 * - Usage trends
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet } from '@/services/http'

const { t, locale } = useI18n()

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

const isAr = computed(() => locale.value === 'ar')

const periodOptions = computed(() => [
  { value: 'current_month', label: isAr.value ? 'الشهر الحالي' : 'Current Month' },
  { value: 'last_month', label: isAr.value ? 'الشهر الماضي' : 'Last Month' },
  { value: 'last_3_months', label: isAr.value ? 'آخر 3 أشهر' : 'Last 3 Months' },
  { value: 'last_year', label: isAr.value ? 'السنة الماضية' : 'Last Year' },
])

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
  } catch {
    console.warn('Consumption API not available')
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
        <h1 class="page-title">{{ t('operator.consumption') }}</h1>
        <p class="page-description">{{ t('operator.consumptionDesc') }}</p>
      </div>
      <select v-model="selectedPeriod" class="input w-48 text-sm" @change="loadConsumption">
        <option v-for="opt in periodOptions" :key="opt.value" :value="opt.value">
          {{ opt.label }}
        </option>
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
            <p class="text-xs text-tertiary">
              {{ isAr ? 'إجمالي استدعاءات الذكاء الاصطناعي' : 'Total AI Calls' }}
            </p>
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
            <p class="text-xs text-tertiary">
              {{ isAr ? 'إجمالي الرموز' : 'Total Tokens' }}
            </p>
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
            <p class="text-xs text-tertiary">
              {{ isAr ? 'التخزين المستخدم' : 'Storage Used' }}
            </p>
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
            <p class="text-xs text-tertiary">
              {{ isAr ? 'التكلفة التقديرية' : 'Estimated Cost' }}
            </p>
            <p class="text-xl font-bold text-secondary">{{ formatNumber(totalCost) }} &#xFDFC;</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Tenant Consumption Table -->
    <div class="card overflow-hidden !p-0">
      <div class="border-b border-secondary-100 bg-surface-subtle px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">
          {{ isAr ? 'استهلاك كل جهة' : 'Per-Tenant Consumption' }}
        </h3>
      </div>
      <div v-if="isLoading" class="p-6">
        <div v-for="i in 5" :key="i" class="skeleton-text mb-3"></div>
      </div>
      <div v-else-if="consumptionData.length === 0" class="py-12 text-center">
        <div class="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-secondary-50">
          <i class="pi pi-chart-bar text-xl text-secondary-400"></i>
        </div>
        <h3 class="text-base font-bold text-secondary-700">
          {{ isAr ? 'لا توجد بيانات استهلاك' : 'No consumption data' }}
        </h3>
        <p class="mt-1 text-sm text-secondary-500">
          {{ isAr ? 'ستظهر بيانات الاستهلاك عند بدء استخدام النظام' : 'Consumption data will appear when the system is in use' }}
        </p>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="table-modern">
          <thead>
            <tr>
              <th>{{ isAr ? 'الجهة' : 'Tenant' }}</th>
              <th class="text-center">{{ isAr ? 'استدعاءات AI' : 'AI Calls' }}</th>
              <th class="text-center">{{ isAr ? 'الرموز' : 'Tokens' }}</th>
              <th class="text-center">{{ isAr ? 'التخزين (MB)' : 'Storage (MB)' }}</th>
              <th class="text-end">{{ isAr ? 'التكلفة التقديرية (﷼)' : 'Est. Cost (﷼)' }}</th>
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
