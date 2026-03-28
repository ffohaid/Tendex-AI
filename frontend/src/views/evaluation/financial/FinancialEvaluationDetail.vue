<script setup lang="ts">
/**
 * FinancialEvaluationDetail - Financial evaluation workspace.
 * Implements price comparison, arithmetic verification, and scoring.
 * Only accessible after technical evaluation approval.
 */
import { onMounted, onUnmounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'
import EvaluationHeader from '@/components/evaluation/EvaluationHeader.vue'
import DualDateDisplay from '@/components/common/DualDateDisplay.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()

const competitionId = computed(() => route.params.id as string)
const activeTab = ref<'prices' | 'scoring' | 'ranking' | 'minutes'>('prices')
const savingStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')

onMounted(async () => {
  await store.selectCompetition(competitionId.value)
  await store.loadFinancialData(competitionId.value)
  store.startAutoSave(competitionId.value, 'financial')
})

onUnmounted(() => {
  store.stopAutoSave()
})

/**
 * Get deviation color class based on percentage from estimate.
 */
function getDeviationColor(deviation: number | undefined): string {
  if (deviation === undefined) return 'text-secondary'
  if (Math.abs(deviation) <= 10) return 'text-success'
  if (Math.abs(deviation) <= 25) return 'text-warning'
  return 'text-danger'
}

function getDeviationBg(deviation: number | undefined): string {
  if (deviation === undefined) return ''
  if (Math.abs(deviation) <= 10) return 'bg-success/10'
  if (Math.abs(deviation) <= 25) return 'bg-warning/10'
  return 'bg-danger/10'
}

const sortedOffers = computed(() =>
  [...store.financialOffers].sort((a, b) => (a.rank ?? 999) - (b.rank ?? 999))
)

function goToComparison() {
  router.push({
    name: 'FinancialComparison',
    params: { id: competitionId.value },
  })
}
</script>

<template>
  <div class="space-y-6">
    <!-- Back button -->
    <button
      class="flex items-center gap-2 text-sm text-secondary/60 transition-colors hover:text-primary"
      @click="router.push({ name: 'EvaluationFinancial' })"
    >
      <i class="pi pi-arrow-right rtl:pi-arrow-left" />
      {{ t('common.back') }}
    </button>

    <!-- Loading -->
    <div v-if="store.loading && !store.selectedCompetition" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-primary" />
    </div>

    <template v-else-if="store.selectedCompetition">
      <!-- Header -->
      <EvaluationHeader
        :competition="store.selectedCompetition"
        :committee="store.committee"
        evaluation-type="financial"
      />

      <!-- Tab navigation -->
      <div class="flex gap-1 rounded-xl border border-surface-dim bg-surface-muted p-1">
        <button
          v-for="tab in (['prices', 'scoring', 'ranking', 'minutes'] as const)"
          :key="tab"
          class="flex-1 rounded-lg px-4 py-2 text-sm font-medium transition-all"
          :class="activeTab === tab
            ? 'bg-white text-primary shadow-sm'
            : 'text-secondary/60 hover:text-secondary'"
          @click="activeTab = tab"
        >
          <i
            class="pi me-1.5"
            :class="{
              'pi-dollar': tab === 'prices',
              'pi-pencil': tab === 'scoring',
              'pi-sort-amount-down': tab === 'ranking',
              'pi-file-edit': tab === 'minutes',
            }"
          />
          {{ t(`evaluation.financial.tabs.${tab}`) }}
        </button>
      </div>

      <!-- Prices tab: Price comparison table -->
      <div v-if="activeTab === 'prices'" class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-bold text-secondary">
            <i class="pi pi-table me-2 text-primary" />
            {{ t('evaluation.financial.priceComparison') }}
          </h2>
          <button
            class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
            @click="goToComparison"
          >
            <i class="pi pi-external-link" />
            {{ t('evaluation.financial.fullComparison') }}
          </button>
        </div>

        <!-- Financial offers table -->
        <div class="card overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="bg-surface-muted">
                <th class="border-b border-surface-dim px-4 py-3 text-start text-xs font-semibold text-secondary/60">
                  {{ t('evaluation.financial.vendor') }}
                </th>
                <th class="border-b border-surface-dim px-4 py-3 text-end text-xs font-semibold text-secondary/60">
                  {{ t('evaluation.financial.totalAmount') }}
                </th>
                <th class="border-b border-surface-dim px-4 py-3 text-center text-xs font-semibold text-secondary/60">
                  {{ t('evaluation.financial.arithmeticCheck') }}
                </th>
                <th class="border-b border-surface-dim px-4 py-3 text-end text-xs font-semibold text-secondary/60">
                  {{ t('evaluation.financial.deviation') }}
                </th>
                <th class="border-b border-surface-dim px-4 py-3 text-center text-xs font-semibold text-secondary/60">
                  {{ t('evaluation.financial.rank') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="offer in sortedOffers"
                :key="offer.id"
                class="transition-colors hover:bg-surface-muted/50"
              >
                <td class="border-b border-surface-dim px-4 py-3">
                  <div class="flex items-center gap-2">
                    <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-tertiary/10 text-sm font-bold text-tertiary">
                      {{ offer.vendorCode.charAt(offer.vendorCode.length - 1) }}
                    </div>
                    <span class="font-medium text-secondary">{{ offer.vendorCode }}</span>
                  </div>
                </td>
                <td class="border-b border-surface-dim px-4 py-3 text-end font-semibold text-secondary">
                  {{ formatCurrency(offer.totalAmount) }}
                </td>
                <td class="border-b border-surface-dim px-4 py-3 text-center">
                  <span
                    class="badge"
                    :class="offer.arithmeticValid ? 'badge-success' : 'badge-danger'"
                  >
                    <i class="pi me-1" :class="offer.arithmeticValid ? 'pi-check' : 'pi-times'" />
                    {{ offer.arithmeticValid
                      ? t('evaluation.financial.valid')
                      : t('evaluation.financial.invalid')
                    }}
                  </span>
                </td>
                <td
                  class="border-b border-surface-dim px-4 py-3 text-end"
                  :class="getDeviationColor(offer.deviationFromEstimate)"
                >
                  <span
                    v-if="offer.deviationFromEstimate !== undefined"
                    class="rounded-full px-2 py-0.5 text-xs font-medium"
                    :class="getDeviationBg(offer.deviationFromEstimate)"
                  >
                    {{ offer.deviationFromEstimate > 0 ? '+' : '' }}{{ offer.deviationFromEstimate?.toFixed(1) }}%
                  </span>
                </td>
                <td class="border-b border-surface-dim px-4 py-3 text-center">
                  <span
                    v-if="offer.rank"
                    class="flex h-8 w-8 mx-auto items-center justify-center rounded-full text-sm font-bold"
                    :class="offer.rank === 1
                      ? 'bg-primary text-white'
                      : offer.rank === 2
                        ? 'bg-tertiary/20 text-tertiary'
                        : 'bg-surface-muted text-secondary'"
                  >
                    {{ offer.rank }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Offer details (expandable) -->
        <div class="space-y-4">
          <h3 class="text-base font-semibold text-secondary">
            {{ t('evaluation.financial.itemDetails') }}
          </h3>
          <div
            v-for="offer in store.financialOffers"
            :key="`detail-${offer.id}`"
            class="card"
          >
            <div class="mb-3 flex items-center justify-between">
              <h4 class="font-semibold text-secondary">{{ offer.vendorCode }}</h4>
              <span class="text-sm font-bold text-primary">{{ formatCurrency(offer.totalAmount) }}</span>
            </div>
            <table class="w-full border-collapse text-sm">
              <thead>
                <tr class="bg-surface-muted">
                  <th class="border-b border-surface-dim px-3 py-2 text-start text-xs font-semibold text-secondary/60">
                    {{ t('evaluation.financial.item') }}
                  </th>
                  <th class="border-b border-surface-dim px-3 py-2 text-center text-xs font-semibold text-secondary/60">
                    {{ t('evaluation.financial.unit') }}
                  </th>
                  <th class="border-b border-surface-dim px-3 py-2 text-center text-xs font-semibold text-secondary/60">
                    {{ t('evaluation.financial.quantity') }}
                  </th>
                  <th class="border-b border-surface-dim px-3 py-2 text-end text-xs font-semibold text-secondary/60">
                    {{ t('evaluation.financial.unitPrice') }}
                  </th>
                  <th class="border-b border-surface-dim px-3 py-2 text-end text-xs font-semibold text-secondary/60">
                    {{ t('evaluation.financial.totalPrice') }}
                  </th>
                  <th class="border-b border-surface-dim px-3 py-2 text-end text-xs font-semibold text-secondary/60">
                    {{ t('evaluation.financial.boqPrice') }}
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in offer.items" :key="item.id">
                  <td class="border-b border-surface-dim px-3 py-2 text-secondary">{{ item.itemName }}</td>
                  <td class="border-b border-surface-dim px-3 py-2 text-center text-secondary/70">{{ item.unit }}</td>
                  <td class="border-b border-surface-dim px-3 py-2 text-center text-secondary/70">{{ item.quantity }}</td>
                  <td class="border-b border-surface-dim px-3 py-2 text-end text-secondary">{{ formatCurrency(item.unitPrice) }}</td>
                  <td class="border-b border-surface-dim px-3 py-2 text-end font-medium text-secondary">{{ formatCurrency(item.totalPrice) }}</td>
                  <td class="border-b border-surface-dim px-3 py-2 text-end text-secondary/50">
                    {{ item.boqPrice ? formatCurrency(item.boqPrice) : '—' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Scoring tab -->
      <div v-else-if="activeTab === 'scoring'" class="card text-center py-12">
        <i class="pi pi-pencil text-4xl text-secondary/20" />
        <p class="mt-3 text-sm text-secondary/60">{{ t('evaluation.financial.scoringArea') }}</p>
      </div>

      <!-- Ranking tab -->
      <div v-else-if="activeTab === 'ranking'" class="space-y-4">
        <h2 class="text-lg font-bold text-secondary">
          <i class="pi pi-sort-amount-down me-2 text-primary" />
          {{ t('evaluation.financial.finalRanking') }}
        </h2>

        <div class="space-y-3">
          <div
            v-for="(offer, index) in sortedOffers"
            :key="offer.id"
            class="card flex items-center gap-4 transition-all"
            :class="index === 0 ? 'border-primary/30 bg-primary/5' : ''"
          >
            <!-- Rank number -->
            <div
              class="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl text-lg font-bold"
              :class="index === 0
                ? 'bg-primary text-white'
                : index === 1
                  ? 'bg-tertiary/20 text-tertiary'
                  : 'bg-surface-muted text-secondary'"
            >
              {{ index + 1 }}
            </div>

            <!-- Vendor info -->
            <div class="flex-1">
              <h3 class="font-semibold text-secondary">{{ offer.vendorCode }}</h3>
              <div class="mt-1 flex items-center gap-4 text-xs text-secondary/60">
                <span>{{ t('evaluation.financial.totalAmount') }}: {{ formatCurrency(offer.totalAmount) }}</span>
                <span
                  v-if="offer.deviationFromEstimate !== undefined"
                  :class="getDeviationColor(offer.deviationFromEstimate)"
                >
                  {{ t('evaluation.financial.deviation') }}:
                  {{ offer.deviationFromEstimate > 0 ? '+' : '' }}{{ offer.deviationFromEstimate?.toFixed(1) }}%
                </span>
              </div>
            </div>

            <!-- Status badges -->
            <div class="flex items-center gap-2">
              <span
                class="badge"
                :class="offer.arithmeticValid ? 'badge-success' : 'badge-danger'"
              >
                {{ offer.arithmeticValid
                  ? t('evaluation.financial.arithmeticOk')
                  : t('evaluation.financial.arithmeticError')
                }}
              </span>
              <span v-if="index === 0" class="badge badge-primary">
                <i class="pi pi-star me-1" />
                {{ t('evaluation.financial.bestOffer') }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Minutes tab -->
      <div v-else-if="activeTab === 'minutes'" class="card text-center py-12">
        <i class="pi pi-file-edit text-4xl text-secondary/20" />
        <p class="mt-3 text-sm text-secondary/60">{{ t('evaluation.minutes.comingSoon') }}</p>
      </div>
    </template>
  </div>
</template>
