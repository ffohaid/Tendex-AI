<script setup lang="ts">
/**
 * FinancialComparison - Side-by-side financial offer comparison.
 * Uses FinancialComparisonMatrix from the store.
 * Shows price comparison with deviation analysis and ranking.
 */
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'
import { PriceDeviationLevel } from '@/types/evaluation'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const competitionId = computed(() => route.params.id as string)
const viewMode = ref<'summary' | 'detailed'>('summary')

onMounted(async () => {
  try {
    await store.selectCompetition(competitionId.value)
    await store.loadFinancialData(competitionId.value)
  } catch (err) {
    console.error('[FinancialComparison] Failed to load data:', err)
  }
})

const matrix = computed(() => store.financialComparisonMatrix)

/* Use supplierTotals from the comparison matrix, sorted by rank */
const sortedSuppliers = computed(() => {
  if (!matrix.value?.supplierTotals) return []
  return [...matrix.value.supplierTotals].sort((a, b) => a.financialRank - b.financialRank)
})

const highestTotal = computed(() =>
  sortedSuppliers.value.length > 0 ? sortedSuppliers.value[sortedSuppliers.value.length - 1].totalAmount : 0
)

function getDeviationClass(level: PriceDeviationLevel | number | null | undefined): string {
  if (level === PriceDeviationLevel.WithinRange) return 'text-green-600'
  if (level === PriceDeviationLevel.ModerateDeviation) return 'text-yellow-600'
  return 'text-red-600'
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div class="flex items-center gap-3">
        <button
          class="flex h-9 w-9 items-center justify-center rounded-lg border border-gray-200 text-gray-500 hover:bg-gray-50"
          @click="router.back()"
        >
          <i :class="isRtl ? 'pi pi-arrow-left' : 'pi pi-arrow-right'" />
        </button>
        <div>
          <h1 class="text-xl font-bold text-gray-900">
            <i class="pi pi-dollar me-2 text-blue-600" />
            {{ isRtl ? 'مقارنة العروض المالية' : 'Financial Offer Comparison' }}
          </h1>
          <p class="text-sm text-gray-500">
            {{ store.selectedCompetition?.projectName }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <div class="flex rounded-lg border border-gray-200 bg-gray-50 p-0.5">
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'summary'
              ? 'bg-white text-blue-600 shadow-sm'
              : 'text-gray-500 hover:text-gray-700'"
            @click="viewMode = 'summary'"
          >
            {{ isRtl ? 'ملخص' : 'Summary' }}
          </button>
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'detailed'
              ? 'bg-white text-blue-600 shadow-sm'
              : 'text-gray-500 hover:text-gray-700'"
            @click="viewMode = 'detailed'"
          >
            {{ isRtl ? 'تفصيلي' : 'Detailed' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-blue-600" />
      <span class="ms-3 text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Error -->
    <div v-else-if="store.error" class="rounded-lg border border-red-200 bg-red-50 p-6 text-center">
      <i class="pi pi-exclamation-triangle mb-3 text-3xl text-red-500" />
      <p class="mt-2 text-sm text-red-700">{{ store.error }}</p>
    </div>

    <!-- No data -->
    <div v-else-if="!sortedSuppliers.length" class="rounded-lg border border-gray-200 bg-white p-12 text-center">
      <i class="pi pi-dollar text-4xl text-gray-300" />
      <h3 class="mt-4 text-lg font-semibold text-gray-500">
        {{ isRtl ? 'لا توجد عروض مالية للمقارنة' : 'No financial offers to compare' }}
      </h3>
    </div>

    <template v-else>
      <!-- Summary View: Supplier cards sorted by rank -->
      <div v-if="viewMode === 'summary'" class="space-y-6">
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <div
            v-for="(supplier, index) in sortedSuppliers"
            :key="supplier.offerId"
            class="relative overflow-hidden rounded-xl border bg-white p-5 transition-all"
            :class="index === 0 ? 'border-blue-300 ring-2 ring-blue-100' : 'border-gray-200'"
          >
            <!-- Rank badge -->
            <div
              class="absolute end-4 top-4 flex h-10 w-10 items-center justify-center rounded-full text-lg font-bold"
              :class="index === 0
                ? 'bg-blue-600 text-white'
                : index === 1
                  ? 'bg-gray-200 text-gray-600'
                  : 'bg-gray-100 text-gray-500'"
            >
              {{ supplier.financialRank }}
            </div>

            <!-- Best offer label -->
            <div v-if="index === 0" class="mb-3">
              <span class="inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-xs font-semibold text-blue-700">
                <i class="pi pi-star me-1" />
                {{ isRtl ? 'أفضل عرض' : 'Best Offer' }}
              </span>
            </div>

            <!-- Supplier info -->
            <div class="mb-4 flex items-center gap-3">
              <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-50 text-lg font-bold text-blue-600">
                {{ supplier.blindCode?.charAt(supplier.blindCode.length - 1) || '#' }}
              </div>
              <div>
                <h3 class="text-base font-bold text-gray-900">{{ supplier.blindCode }}</h3>
                <span v-if="supplier.supplierName" class="text-xs text-gray-500">{{ supplier.supplierName }}</span>
              </div>
            </div>

            <!-- Total amount -->
            <div class="mb-4 rounded-xl bg-gray-50 p-4 text-center">
              <span class="text-xs text-gray-400">{{ isRtl ? 'إجمالي العرض' : 'Total Amount' }}</span>
              <p class="text-2xl font-bold text-gray-900">
                {{ formatCurrency(supplier.totalAmount) }}
                <span class="text-base">&#xFDFC;</span>
              </p>
            </div>

            <!-- Deviation -->
            <div class="flex items-center justify-between text-sm">
              <span class="text-gray-500">{{ isRtl ? 'نسبة الانحراف' : 'Deviation' }}</span>
              <span class="font-semibold" :class="getDeviationClass(supplier.deviationLevel)">
                {{ supplier.deviationPercentage === 0 ? '—' : `${supplier.deviationPercentage > 0 ? '+' : ''}${supplier.deviationPercentage.toFixed(1)}%` }}
              </span>
            </div>
          </div>
        </div>

        <!-- Price comparison bar chart -->
        <div class="rounded-xl border border-gray-200 bg-white p-5">
          <h3 class="mb-4 text-base font-bold text-gray-900">
            <i class="pi pi-chart-bar me-2 text-blue-600" />
            {{ isRtl ? 'مقارنة الأسعار' : 'Price Comparison' }}
          </h3>
          <div class="space-y-3">
            <div
              v-for="supplier in sortedSuppliers"
              :key="`bar-${supplier.offerId}`"
              class="flex items-center gap-3"
            >
              <span class="w-24 truncate text-sm font-medium text-gray-700">{{ supplier.blindCode }}</span>
              <div class="flex-1">
                <div class="h-8 overflow-hidden rounded-lg bg-gray-100">
                  <div
                    class="flex h-full items-center rounded-lg px-3 text-xs font-bold text-white transition-all duration-500"
                    :class="supplier.offerId === sortedSuppliers[0]?.offerId ? 'bg-blue-600' : 'bg-gray-400'"
                    :style="{
                      width: `${highestTotal > 0 ? Math.max((supplier.totalAmount / highestTotal) * 100, 20) : 0}%`
                    }"
                  >
                    {{ formatCurrency(supplier.totalAmount) }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Estimated total cost -->
        <div v-if="matrix?.estimatedTotalCost" class="rounded-xl border border-gray-200 bg-white p-5">
          <div class="flex items-center justify-between">
            <span class="text-sm font-medium text-gray-500">
              {{ isRtl ? 'التكلفة التقديرية' : 'Estimated Cost' }}
            </span>
            <span class="text-lg font-bold text-gray-900">
              {{ formatCurrency(matrix.estimatedTotalCost) }} &#xFDFC;
            </span>
          </div>
        </div>
      </div>

      <!-- Detailed View: Item-level comparison table -->
      <div v-else class="overflow-x-auto rounded-xl border border-gray-200 bg-white">
        <div v-if="matrix && matrix.rows.length" class="p-5">
          <h3 class="mb-4 text-base font-bold text-gray-900">
            <i class="pi pi-table me-2 text-blue-600" />
            {{ isRtl ? 'مقارنة تفصيلية بالبنود' : 'Item-level Comparison' }}
          </h3>

          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="bg-gray-50">
                <th class="sticky start-0 z-10 border-b border-e border-gray-200 bg-gray-50 px-4 py-3 text-start text-xs font-semibold text-gray-500">
                  {{ isRtl ? 'البند' : 'Item' }}
                </th>
                <th class="border-b border-e border-gray-200 bg-gray-50 px-4 py-3 text-center text-xs font-semibold text-gray-500">
                  {{ isRtl ? 'الكمية' : 'Qty' }}
                </th>
                <th
                  v-for="total in sortedSuppliers"
                  :key="total.offerId"
                  class="border-b border-e border-gray-200 px-4 py-3 text-center"
                >
                  <div class="flex flex-col items-center gap-1">
                    <span class="text-xs font-semibold text-gray-700">{{ total.blindCode }}</span>
                    <span v-if="total.supplierName" class="text-xs text-gray-400">{{ total.supplierName }}</span>
                  </div>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in matrix.rows" :key="row.boqItemId">
                <td class="sticky start-0 z-10 border-b border-e border-gray-200 bg-white px-4 py-3 text-sm font-medium text-gray-700">
                  {{ row.descriptionAr }}
                </td>
                <td class="border-b border-e border-gray-200 px-4 py-3 text-center text-xs text-gray-500">
                  {{ row.quantity }} {{ row.unit }}
                </td>
                <td
                  v-for="total in sortedSuppliers"
                  :key="`${row.boqItemId}-${total.offerId}`"
                  class="border-b border-e border-gray-200 px-4 py-3 text-center"
                >
                  <template v-for="price in row.supplierPrices.filter(p => p.offerId === total.offerId)" :key="price.offerId">
                    <div
                      class="flex flex-col items-center gap-0.5"
                      :class="price.deviationLevel === PriceDeviationLevel.WithinRange ? 'text-green-700 font-bold' : price.deviationLevel === PriceDeviationLevel.SignificantDeviation ? 'text-red-600' : 'text-gray-700'"
                    >
                      <span>{{ formatCurrency(price.totalPrice) }}</span>
                      <span class="text-xs text-gray-400">
                        {{ formatCurrency(price.unitPrice) }} x {{ row.quantity }}
                      </span>
                      <span v-if="price.hasArithmeticError" class="text-xs text-red-500">
                        <i class="pi pi-exclamation-circle me-1" />
                        {{ isRtl ? 'خطأ حسابي' : 'Arithmetic error' }}
                      </span>
                    </div>
                  </template>
                  <span v-if="!row.supplierPrices.some(p => p.offerId === total.offerId)" class="text-gray-300">—</span>
                </td>
              </tr>

              <!-- Total row -->
              <tr class="bg-gray-50 font-bold">
                <td class="sticky start-0 z-10 border-t-2 border-e border-gray-300 bg-gray-50 px-4 py-3 text-sm text-gray-700">
                  {{ isRtl ? 'الإجمالي' : 'Total' }}
                </td>
                <td class="border-t-2 border-e border-gray-300 bg-gray-50 px-4 py-3" />
                <td
                  v-for="total in sortedSuppliers"
                  :key="`total-${total.offerId}`"
                  class="border-t-2 border-e border-gray-300 px-4 py-3 text-center text-base"
                  :class="total.offerId === sortedSuppliers[0]?.offerId ? 'text-blue-600' : 'text-gray-700'"
                >
                  {{ formatCurrency(total.totalAmount) }} &#xFDFC;
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- No matrix data -->
        <div v-else class="p-12 text-center">
          <i class="pi pi-table text-4xl text-gray-300" />
          <h3 class="mt-4 text-lg font-semibold text-gray-500">
            {{ isRtl ? 'لا توجد بيانات مقارنة تفصيلية' : 'No detailed comparison data' }}
          </h3>
        </div>
      </div>
    </template>
  </div>
</template>
