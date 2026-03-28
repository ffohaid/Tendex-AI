<script setup lang="ts">
/**
 * FinancialComparison - Side-by-side financial offer comparison.
 * Implements price comparison with deviation analysis and ranking.
 * Shows item-level breakdown across all vendors.
 */
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()

const competitionId = computed(() => route.params.id as string)
const viewMode = ref<'summary' | 'detailed'>('summary')

onMounted(async () => {
  await store.selectCompetition(competitionId.value)
  await store.loadFinancialData(competitionId.value)
})

const sortedOffers = computed(() =>
  [...store.financialOffers].sort((a, b) => a.totalAmount - b.totalAmount)
)

const lowestOffer = computed(() =>
  sortedOffers.value.length > 0 ? sortedOffers.value[0].totalAmount : 0
)

/**
 * Get all unique item names across all offers.
 */
const allItemNames = computed(() => {
  const names = new Set<string>()
  store.financialOffers.forEach(offer => {
    offer.items.forEach(item => names.add(item.itemName))
  })
  return Array.from(names)
})

/**
 * Get item price for a specific vendor and item name.
 */
function getItemPrice(vendorId: string, itemName: string): number | null {
  const offer = store.financialOffers.find(o => o.vendorId === vendorId)
  const item = offer?.items.find(i => i.itemName === itemName)
  return item?.totalPrice ?? null
}

/**
 * Get the lowest price for a specific item across all vendors.
 */
function getLowestItemPrice(itemName: string): number {
  let lowest = Infinity
  store.financialOffers.forEach(offer => {
    const item = offer.items.find(i => i.itemName === itemName)
    if (item && item.totalPrice < lowest) {
      lowest = item.totalPrice
    }
  })
  return lowest === Infinity ? 0 : lowest
}

function getDeviationClass(amount: number): string {
  const deviation = lowestOffer.value > 0
    ? ((amount - lowestOffer.value) / lowestOffer.value) * 100
    : 0
  if (deviation === 0) return 'text-success'
  if (deviation <= 10) return 'text-warning'
  return 'text-danger'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div class="flex items-center gap-3">
        <button
          class="flex h-9 w-9 items-center justify-center rounded-lg border border-surface-dim text-secondary/60 transition-colors hover:bg-surface-muted hover:text-secondary"
          @click="router.back()"
        >
          <i class="pi pi-arrow-right rtl:pi-arrow-left" />
        </button>
        <div>
          <h1 class="text-xl font-bold text-secondary">
            <i class="pi pi-dollar me-2 text-primary" />
            {{ t('evaluation.comparison.financialTitle') }}
          </h1>
          <p class="text-sm text-secondary/60">
            {{ store.selectedCompetition?.projectName }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <!-- View mode toggle -->
        <div class="flex rounded-lg border border-surface-dim bg-surface-muted p-0.5">
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'summary'
              ? 'bg-white text-primary shadow-sm'
              : 'text-secondary/60 hover:text-secondary'"
            @click="viewMode = 'summary'"
          >
            {{ t('evaluation.comparison.summary') }}
          </button>
          <button
            class="rounded-md px-3 py-1.5 text-xs font-medium transition-all"
            :class="viewMode === 'detailed'
              ? 'bg-white text-primary shadow-sm'
              : 'text-secondary/60 hover:text-secondary'"
            @click="viewMode = 'detailed'"
          >
            {{ t('evaluation.comparison.detailed') }}
          </button>
        </div>

        <button
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
        >
          <i class="pi pi-download" />
          {{ t('evaluation.comparison.export') }}
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-primary" />
    </div>

    <template v-else>
      <!-- Summary View: Vendor cards side by side -->
      <div v-if="viewMode === 'summary'" class="space-y-6">
        <!-- Vendor ranking cards -->
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          <div
            v-for="(offer, index) in sortedOffers"
            :key="offer.id"
            class="card relative overflow-hidden transition-all"
            :class="index === 0 ? 'border-primary/30 ring-2 ring-primary/10' : ''"
          >
            <!-- Rank badge -->
            <div
              class="absolute end-4 top-4 flex h-10 w-10 items-center justify-center rounded-full text-lg font-bold"
              :class="index === 0
                ? 'bg-primary text-white'
                : index === 1
                  ? 'bg-tertiary/20 text-tertiary'
                  : 'bg-surface-muted text-secondary'"
            >
              {{ index + 1 }}
            </div>

            <!-- Best offer label -->
            <div v-if="index === 0" class="mb-3">
              <span class="badge badge-primary">
                <i class="pi pi-star me-1" />
                {{ t('evaluation.financial.bestOffer') }}
              </span>
            </div>

            <!-- Vendor info -->
            <div class="mb-4 flex items-center gap-3">
              <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-tertiary/10 text-lg font-bold text-tertiary">
                {{ offer.vendorCode.charAt(offer.vendorCode.length - 1) }}
              </div>
              <div>
                <h3 class="text-base font-bold text-secondary">{{ offer.vendorCode }}</h3>
                <span
                  class="badge text-xs"
                  :class="offer.arithmeticValid ? 'badge-success' : 'badge-danger'"
                >
                  {{ offer.arithmeticValid
                    ? t('evaluation.financial.arithmeticOk')
                    : t('evaluation.financial.arithmeticError')
                  }}
                </span>
              </div>
            </div>

            <!-- Total amount -->
            <div class="mb-4 rounded-xl bg-surface-muted p-4 text-center">
              <span class="text-xs text-secondary/50">{{ t('evaluation.financial.totalAmount') }}</span>
              <p class="text-2xl font-bold text-secondary">
                {{ formatCurrency(offer.totalAmount) }}
                <span class="text-base">&#xFDFC;</span>
              </p>
            </div>

            <!-- Deviation -->
            <div v-if="offer.deviationFromEstimate !== undefined" class="flex items-center justify-between text-sm">
              <span class="text-secondary/60">{{ t('evaluation.financial.deviationFromEstimate') }}</span>
              <span
                class="font-semibold"
                :class="getDeviationClass(offer.totalAmount)"
              >
                {{ offer.deviationFromEstimate > 0 ? '+' : '' }}{{ offer.deviationFromEstimate.toFixed(1) }}%
              </span>
            </div>

            <!-- Items count -->
            <div class="mt-2 flex items-center justify-between text-sm">
              <span class="text-secondary/60">{{ t('evaluation.financial.itemCount') }}</span>
              <span class="font-medium text-secondary">{{ offer.items.length }}</span>
            </div>
          </div>
        </div>

        <!-- Price comparison chart placeholder -->
        <div class="card">
          <h3 class="mb-4 text-base font-bold text-secondary">
            <i class="pi pi-chart-bar me-2 text-primary" />
            {{ t('evaluation.comparison.priceChart') }}
          </h3>
          <!-- Visual bar chart -->
          <div class="space-y-3">
            <div
              v-for="offer in sortedOffers"
              :key="`bar-${offer.id}`"
              class="flex items-center gap-3"
            >
              <span class="w-24 text-sm font-medium text-secondary">{{ offer.vendorCode }}</span>
              <div class="flex-1">
                <div class="h-8 overflow-hidden rounded-lg bg-surface-muted">
                  <div
                    class="flex h-full items-center rounded-lg px-3 text-xs font-bold text-white transition-all duration-500"
                    :class="offer.id === sortedOffers[0]?.id ? 'bg-primary' : 'bg-tertiary'"
                    :style="{
                      width: `${lowestOffer > 0 ? Math.min((offer.totalAmount / (sortedOffers[sortedOffers.length - 1]?.totalAmount || 1)) * 100, 100) : 0}%`
                    }"
                  >
                    {{ formatCurrency(offer.totalAmount) }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Detailed View: Item-level comparison table -->
      <div v-else class="card overflow-x-auto">
        <h3 class="mb-4 text-base font-bold text-secondary">
          <i class="pi pi-table me-2 text-primary" />
          {{ t('evaluation.comparison.itemComparison') }}
        </h3>

        <table class="w-full border-collapse text-sm">
          <thead>
            <tr class="bg-surface-muted">
              <th class="sticky start-0 z-10 border-b border-e border-surface-dim bg-surface-muted px-4 py-3 text-start text-xs font-semibold text-secondary/60">
                {{ t('evaluation.financial.item') }}
              </th>
              <th
                v-for="offer in sortedOffers"
                :key="offer.id"
                class="border-b border-e border-surface-dim px-4 py-3 text-center"
              >
                <div class="flex flex-col items-center gap-1">
                  <span class="text-xs font-semibold text-secondary">{{ offer.vendorCode }}</span>
                  <span
                    v-if="offer.id === sortedOffers[0]?.id"
                    class="badge badge-primary text-xs"
                  >
                    {{ t('evaluation.financial.lowest') }}
                  </span>
                </div>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="itemName in allItemNames" :key="itemName">
              <td class="sticky start-0 z-10 border-b border-e border-surface-dim bg-white px-4 py-3 text-sm font-medium text-secondary">
                {{ itemName }}
              </td>
              <td
                v-for="offer in sortedOffers"
                :key="`${offer.id}-${itemName}`"
                class="border-b border-e border-surface-dim px-4 py-3 text-center"
                :class="getItemPrice(offer.vendorId, itemName) === getLowestItemPrice(itemName)
                  ? 'bg-success/5 font-bold text-success'
                  : 'text-secondary'"
              >
                <template v-if="getItemPrice(offer.vendorId, itemName) !== null">
                  {{ formatCurrency(getItemPrice(offer.vendorId, itemName)!) }}
                </template>
                <span v-else class="text-secondary/30">—</span>
              </td>
            </tr>

            <!-- Total row -->
            <tr class="bg-secondary/5 font-bold">
              <td class="sticky start-0 z-10 border-t-2 border-e border-secondary/20 bg-secondary/5 px-4 py-3 text-sm text-secondary">
                {{ t('evaluation.comparison.total') }}
              </td>
              <td
                v-for="offer in sortedOffers"
                :key="`total-${offer.id}`"
                class="border-t-2 border-e border-secondary/20 px-4 py-3 text-center text-base"
                :class="offer.id === sortedOffers[0]?.id ? 'text-primary' : 'text-secondary'"
              >
                {{ formatCurrency(offer.totalAmount) }} &#xFDFC;
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
