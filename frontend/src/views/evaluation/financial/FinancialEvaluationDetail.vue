<script setup lang="ts">
/**
 * FinancialEvaluationDetail - Financial evaluation workspace.
 * Implements price comparison matrix, arithmetic verification, scoring, ranking, and AI analysis.
 * Only accessible after technical evaluation approval.
 * Aligned with backend FinancialEvaluationEndpoints and evaluation store.
 */
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'
import type { ArithmeticVerificationResult } from '@/types/evaluation'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const competitionId = computed(() => route.params.id as string)
const activeTab = ref<'comparison' | 'verification' | 'ai-analysis' | 'ranking' | 'minutes'>('comparison')
const showSubmitDialog = ref(false)
const submitting = ref(false)
const submitError = ref('')

/* AI state */
const aiAnalysisLoading = ref(false)
const aiAnalysisResult = ref<string>('')
const aiAnalysisError = ref('')

/* Arithmetic verification */
const verificationResults = ref<Map<string, ArithmeticVerificationResult>>(new Map())
const verifyingOfferId = ref<string | null>(null)

/* Minutes */
const minutesData = ref({
  meetingDate: '',
  meetingLocation: '',
  decisions: '',
})
const generatingMinutes = ref(false)

onMounted(async () => {
  await store.loadFinancialData(competitionId.value)
})

/* Computed: supplier totals from comparison matrix */
const supplierTotals = computed(() => {
  return store.financialComparisonMatrix?.supplierTotals ?? []
})

const sortedTotals = computed(() =>
  [...supplierTotals.value].sort((a, b) => a.financialRank - b.financialRank)
)

const lowestOffer = computed(() => {
  if (!supplierTotals.value.length) return 0
  return Math.min(...supplierTotals.value.map(s => s.totalAmount || Infinity))
})

const highestOffer = computed(() => {
  if (!supplierTotals.value.length) return 0
  return Math.max(...supplierTotals.value.map(s => s.totalAmount || 0))
})

const averageOffer = computed(() => {
  if (!supplierTotals.value.length) return 0
  const sum = supplierTotals.value.reduce((s, o) => s + (o.totalAmount || 0), 0)
  return Math.round(sum / supplierTotals.value.length)
})

const estimatedCost = computed(() => {
  return store.financialComparisonMatrix?.estimatedTotalCost ?? 0
})

function getDeviationColor(deviation: number | undefined | null): string {
  if (deviation === undefined || deviation === null) return 'text-gray-600'
  if (Math.abs(deviation) <= 10) return 'text-green-600'
  if (Math.abs(deviation) <= 25) return 'text-yellow-600'
  return 'text-red-600'
}

function getDeviationBg(deviation: number | undefined | null): string {
  if (deviation === undefined || deviation === null) return ''
  if (Math.abs(deviation) <= 10) return 'bg-green-50'
  if (Math.abs(deviation) <= 25) return 'bg-yellow-50'
  return 'bg-red-50'
}

function getDeviationLevelBadge(level: number | null | undefined): { bg: string; text: string; label: string } {
  switch (level) {
    case 1: return { bg: 'bg-green-100', text: 'text-green-700', label: isRtl.value ? 'ضمن النطاق' : 'Within Range' }
    case 2: return { bg: 'bg-yellow-100', text: 'text-yellow-700', label: isRtl.value ? 'انحراف متوسط' : 'Moderate Deviation' }
    case 3: return { bg: 'bg-red-100', text: 'text-red-700', label: isRtl.value ? 'انحراف كبير' : 'Significant Deviation' }
    default: return { bg: 'bg-gray-100', text: 'text-gray-600', label: '-' }
  }
}

async function verifyArithmetic(supplierOfferId: string) {
  verifyingOfferId.value = supplierOfferId
  try {
    const result = await store.verifyArithmetic(competitionId.value, supplierOfferId)
    verificationResults.value.set(supplierOfferId, result)
  } catch (e: any) {
    console.error('Arithmetic verification failed:', e)
  } finally {
    verifyingOfferId.value = null
  }
}

async function verifyAllArithmetic() {
  for (const total of supplierTotals.value) {
    await verifyArithmetic(total.offerId)
  }
}

async function runAiAnalysis() {
  aiAnalysisLoading.value = true
  aiAnalysisError.value = ''
  try {
    // Use the AI text assist endpoint with a custom prompt for financial analysis
    const response = await fetch('/api/ai/text/assist', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        text: JSON.stringify({
          competition: store.selectedCompetition?.projectName,
          estimatedCost: estimatedCost.value,
          suppliers: supplierTotals.value.map(s => ({
            code: s.blindCode,
            total: s.totalAmount,
            deviation: s.deviationPercentage,
            rank: s.financialRank,
          })),
          comparisonRows: store.financialComparisonMatrix?.rows?.slice(0, 10).map(r => ({
            item: r.descriptionAr,
            estimated: r.estimatedTotalPrice,
            prices: r.supplierPrices.map(p => ({ code: p.blindCode, total: p.totalPrice, deviation: p.deviationPercentage })),
          })),
        }),
        action: 'custom',
        customPrompt: `أنت خبير في تحليل العروض المالية للمشتريات الحكومية السعودية. قم بتحليل البيانات المالية التالية وقدم:
1. ملخص تنفيذي للعروض المالية
2. تحليل الانحرافات السعرية وأسبابها المحتملة
3. كشف الأسعار المنخفضة بشكل غير طبيعي (Abnormally Low Prices) مع التبرير
4. كشف الأسعار المرتفعة بشكل مبالغ فيه
5. تقييم المخاطر المالية لكل عرض
6. توصية الترسية مع التبرير المفصل
7. ملاحظات على هيكل التسعير

اكتب التحليل باللغة العربية بأسلوب رسمي مناسب للجان المشتريات الحكومية.`,
      }),
    })
    const data = await response.json()
    if (data.isSuccess) {
      aiAnalysisResult.value = data.generatedText
    } else {
      aiAnalysisError.value = data.errorMessage || (isRtl.value ? 'فشل التحليل' : 'Analysis failed')
    }
  } catch (e: any) {
    aiAnalysisError.value = e?.message || (isRtl.value ? 'حدث خطأ أثناء التحليل' : 'Analysis error')
  } finally {
    aiAnalysisLoading.value = false
  }
}

async function generateMinutesWithAi() {
  generatingMinutes.value = true
  try {
    const response = await fetch('/api/ai/text/assist', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        text: JSON.stringify({
          competition: store.selectedCompetition?.projectName,
          estimatedCost: estimatedCost.value,
          suppliers: sortedTotals.value.map(s => ({
            code: s.blindCode,
            name: s.supplierName,
            total: s.totalAmount,
            deviation: s.deviationPercentage,
            rank: s.financialRank,
          })),
          meetingDate: minutesData.value.meetingDate,
          meetingLocation: minutesData.value.meetingLocation,
        }),
        action: 'custom',
        customPrompt: `أنت كاتب محاضر رسمي للجان المشتريات الحكومية السعودية. قم بإعداد محضر اجتماع لجنة فحص العروض المالية يتضمن:
1. بيانات الاجتماع (التاريخ، المكان)
2. أسماء أعضاء اللجنة (اترك فراغات للأسماء)
3. ملخص العروض المالية المقدمة مع الترتيب
4. نتائج التحقق الحسابي
5. تحليل الانحرافات السعرية
6. قرارات اللجنة وتوصياتها
7. التوصية بالترسية مع التبرير
8. التوقيعات (اترك فراغات)

اكتب المحضر بأسلوب رسمي حكومي باللغة العربية.`,
      }),
    })
    const data = await response.json()
    if (data.isSuccess) {
      minutesData.value.decisions = data.generatedText
    }
  } catch (e: any) {
    console.error('Minutes generation failed:', e)
  } finally {
    generatingMinutes.value = false
  }
}

async function submitFinancialEvaluation() {
  submitting.value = true
  submitError.value = ''
  try {
    // Submit financial evaluation for approval
    await fetch(`/api/financial-evaluations/${competitionId.value}/submit`, { method: 'POST' })
    router.push({ name: 'EvaluationFinancial' })
  } catch (e: any) {
    submitError.value = e?.message || (isRtl.value ? 'حدث خطأ أثناء التقديم' : 'Submission error')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Back button -->
    <button
      class="flex items-center gap-2 text-sm text-gray-500 transition-colors hover:text-green-600"
      @click="router.push({ name: 'EvaluationFinancial' })"
    >
      <i :class="isRtl ? 'pi pi-arrow-right' : 'pi pi-arrow-left'" />
      {{ isRtl ? 'العودة للقائمة' : 'Back to List' }}
    </button>

    <!-- Loading -->
    <div v-if="store.loading && !supplierTotals.length" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-green-600" />
      <span class="ms-3 text-sm text-gray-500">{{ isRtl ? 'جارٍ تحميل بيانات التقييم المالي...' : 'Loading financial evaluation data...' }}</span>
    </div>

    <template v-else>
      <!-- Header Card -->
      <div class="rounded-lg border border-gray-200 bg-white p-5">
        <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div class="flex-1">
            <div class="mb-2 flex items-center gap-3">
              <h1 class="text-xl font-bold text-gray-900">
                <i class="pi pi-wallet me-2 text-green-600" />
                {{ isRtl ? 'التقييم المالي' : 'Financial Evaluation' }}
              </h1>
              <span
                v-if="store.financialEvaluation?.status"
                class="rounded-full px-2.5 py-1 text-xs font-medium"
                :class="{
                  'bg-blue-100 text-blue-700': store.financialEvaluation.status === 1,
                  'bg-yellow-100 text-yellow-700': store.financialEvaluation.status === 3,
                  'bg-green-100 text-green-700': store.financialEvaluation.status === 4,
                }"
              >
                {{ store.financialEvaluation.status === 1 ? (isRtl ? 'قيد التقييم' : 'In Progress') :
                   store.financialEvaluation.status === 3 ? (isRtl ? 'بانتظار الاعتماد' : 'Pending Approval') :
                   store.financialEvaluation.status === 4 ? (isRtl ? 'معتمد' : 'Approved') :
                   store.financialEvaluation.status }}
              </span>
            </div>
            <p class="text-sm text-gray-500">
              {{ store.selectedCompetition?.projectName || competitionId }}
            </p>
          </div>
          <div class="flex items-center gap-6">
            <div class="text-center">
              <div class="text-2xl font-bold text-green-600">{{ supplierTotals.length }}</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'عروض' : 'Offers' }}</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-bold text-purple-600">{{ formatCurrency(estimatedCost) }}</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'التكلفة التقديرية' : 'Estimated' }}</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-bold text-blue-600">{{ formatCurrency(lowestOffer) }}</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'أقل عرض' : 'Lowest' }}</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-bold text-red-600">{{ formatCurrency(highestOffer) }}</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'أعلى عرض' : 'Highest' }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tab navigation -->
      <div class="flex gap-1 rounded-xl border border-gray-200 bg-gray-50 p-1">
        <button
          v-for="tab in (['comparison', 'verification', 'ai-analysis', 'ranking', 'minutes'] as const)"
          :key="tab"
          class="flex-1 rounded-lg px-3 py-2.5 text-sm font-medium transition-all"
          :class="[
            activeTab === tab
              ? tab === 'ai-analysis'
                ? 'bg-gradient-to-l from-purple-600 to-purple-500 text-white shadow-sm'
                : 'bg-white text-green-600 shadow-sm'
              : 'text-gray-500 hover:text-gray-700',
          ]"
          @click="activeTab = tab"
        >
          <i
            class="pi me-1.5"
            :class="{
              'pi-table': tab === 'comparison',
              'pi-calculator': tab === 'verification',
              'pi-sparkles': tab === 'ai-analysis',
              'pi-sort-amount-down': tab === 'ranking',
              'pi-file-edit': tab === 'minutes',
            }"
          />
          {{
            tab === 'comparison' ? (isRtl ? 'مصفوفة المقارنة' : 'Comparison Matrix') :
            tab === 'verification' ? (isRtl ? 'التحقق الحسابي' : 'Arithmetic Check') :
            tab === 'ai-analysis' ? (isRtl ? 'تحليل الذكاء الاصطناعي' : 'AI Analysis') :
            tab === 'ranking' ? (isRtl ? 'الترتيب النهائي' : 'Final Ranking') :
            (isRtl ? 'المحضر' : 'Minutes')
          }}
        </button>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- COMPARISON MATRIX TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-if="activeTab === 'comparison'" class="space-y-4">
        <!-- Summary cards -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
          <div class="rounded-lg border border-purple-200 bg-purple-50 p-4 text-center">
            <div class="text-xs text-purple-600">{{ isRtl ? 'التكلفة التقديرية' : 'Estimated Cost' }}</div>
            <div class="mt-1 text-xl font-bold text-purple-700">{{ formatCurrency(estimatedCost) }}</div>
          </div>
          <div class="rounded-lg border border-green-200 bg-green-50 p-4 text-center">
            <div class="text-xs text-green-600">{{ isRtl ? 'أقل عرض' : 'Lowest Offer' }}</div>
            <div class="mt-1 text-xl font-bold text-green-700">{{ formatCurrency(lowestOffer) }}</div>
          </div>
          <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 text-center">
            <div class="text-xs text-gray-500">{{ isRtl ? 'متوسط العروض' : 'Average' }}</div>
            <div class="mt-1 text-xl font-bold text-gray-700">{{ formatCurrency(averageOffer) }}</div>
          </div>
          <div class="rounded-lg border border-red-200 bg-red-50 p-4 text-center">
            <div class="text-xs text-red-600">{{ isRtl ? 'أعلى عرض' : 'Highest Offer' }}</div>
            <div class="mt-1 text-xl font-bold text-red-700">{{ formatCurrency(highestOffer) }}</div>
          </div>
        </div>

        <!-- Comparison matrix table -->
        <div v-if="store.financialComparisonMatrix" class="rounded-lg border border-gray-200 bg-white overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr class="bg-gray-50">
                <th class="sticky start-0 z-10 border-b border-e border-gray-200 bg-gray-50 px-3 py-3 text-start text-xs font-bold text-gray-600 min-w-[40px]">#</th>
                <th class="sticky start-10 z-10 border-b border-e border-gray-200 bg-gray-50 px-3 py-3 text-start text-xs font-bold text-gray-600 min-w-[200px]">{{ isRtl ? 'البند' : 'Item' }}</th>
                <th class="border-b border-e border-gray-200 px-3 py-3 text-center text-xs font-bold text-gray-600 min-w-[60px]">{{ isRtl ? 'الوحدة' : 'Unit' }}</th>
                <th class="border-b border-e border-gray-200 px-3 py-3 text-center text-xs font-bold text-gray-600 min-w-[60px]">{{ isRtl ? 'الكمية' : 'Qty' }}</th>
                <th class="border-b border-e border-gray-200 px-3 py-3 text-end text-xs font-bold text-purple-600 min-w-[100px]">{{ isRtl ? 'التقديري' : 'Estimated' }}</th>
                <th
                  v-for="supplier in store.financialComparisonMatrix.supplierTotals"
                  :key="supplier.offerId"
                  class="border-b border-e border-gray-200 px-3 py-3 text-center text-xs font-bold text-gray-600 min-w-[120px]"
                >
                  <div class="flex flex-col items-center gap-1">
                    <span class="rounded-full bg-gray-800 px-2 py-0.5 text-xs text-white">{{ supplier.blindCode }}</span>
                    <span class="text-[10px] text-gray-400">{{ isRtl ? 'الترتيب' : 'Rank' }}: {{ supplier.financialRank }}</span>
                  </div>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="row in store.financialComparisonMatrix.rows"
                :key="row.boqItemId"
                class="transition-colors hover:bg-gray-50"
              >
                <td class="sticky start-0 z-10 border-b border-e border-gray-100 bg-white px-3 py-2 text-xs text-gray-500">{{ row.itemNumber }}</td>
                <td class="sticky start-10 z-10 border-b border-e border-gray-100 bg-white px-3 py-2 text-sm text-gray-900">{{ row.descriptionAr }}</td>
                <td class="border-b border-e border-gray-100 px-3 py-2 text-center text-xs text-gray-500">{{ row.unit }}</td>
                <td class="border-b border-e border-gray-100 px-3 py-2 text-center text-xs text-gray-700">{{ row.quantity }}</td>
                <td class="border-b border-e border-gray-100 px-3 py-2 text-end text-xs font-medium text-purple-600">
                  {{ row.estimatedTotalPrice ? formatCurrency(row.estimatedTotalPrice) : '-' }}
                </td>
                <td
                  v-for="price in row.supplierPrices"
                  :key="price.offerId"
                  class="border-b border-e border-gray-100 px-3 py-2 text-end"
                  :class="getDeviationBg(price.deviationPercentage)"
                >
                  <div class="text-sm font-medium text-gray-900">{{ formatCurrency(price.totalPrice) }}</div>
                  <div class="flex items-center justify-end gap-1 mt-0.5">
                    <span
                      v-if="price.hasArithmeticError"
                      class="rounded-full bg-red-100 px-1.5 py-0.5 text-[10px] text-red-600"
                    >
                      <i class="pi pi-exclamation-triangle" />
                    </span>
                    <span
                      class="text-[10px]"
                      :class="getDeviationColor(price.deviationPercentage)"
                    >
                      {{ price.deviationPercentage > 0 ? '+' : '' }}{{ price.deviationPercentage?.toFixed(1) }}%
                    </span>
                  </div>
                </td>
              </tr>
              <!-- Totals row -->
              <tr class="bg-gray-100 font-bold">
                <td class="sticky start-0 z-10 border-t-2 border-gray-300 bg-gray-100 px-3 py-3" colspan="2">
                  {{ isRtl ? 'الإجمالي' : 'Total' }}
                </td>
                <td class="border-t-2 border-gray-300 px-3 py-3" colspan="2"></td>
                <td class="border-t-2 border-gray-300 px-3 py-3 text-end text-purple-700">
                  {{ formatCurrency(estimatedCost) }}
                </td>
                <td
                  v-for="total in store.financialComparisonMatrix.supplierTotals"
                  :key="`total-${total.offerId}`"
                  class="border-t-2 border-gray-300 px-3 py-3 text-end"
                >
                  <div class="text-base text-gray-900">{{ formatCurrency(total.totalAmount) }}</div>
                  <div class="text-xs" :class="getDeviationColor(total.deviationPercentage)">
                    {{ total.deviationPercentage > 0 ? '+' : '' }}{{ total.deviationPercentage?.toFixed(1) }}%
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- No comparison data -->
        <div v-else class="rounded-lg border border-gray-200 bg-white py-12 text-center">
          <i class="pi pi-table text-4xl text-gray-300" />
          <h3 class="mt-4 text-lg font-semibold text-gray-500">{{ isRtl ? 'لا توجد بيانات مقارنة' : 'No comparison data' }}</h3>
          <p class="mt-2 text-sm text-gray-400">{{ isRtl ? 'تأكد من وجود عروض مالية مقدمة' : 'Ensure financial offers are submitted' }}</p>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- ARITHMETIC VERIFICATION TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'verification'" class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-bold text-gray-900">
            <i class="pi pi-calculator me-2 text-blue-600" />
            {{ isRtl ? 'التحقق الحسابي من العروض' : 'Arithmetic Verification' }}
          </h2>
          <button
            @click="verifyAllArithmetic"
            :disabled="!!verifyingOfferId"
            class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            <i :class="verifyingOfferId ? 'pi pi-spinner pi-spin' : 'pi pi-check-circle'" class="me-1.5" />
            {{ isRtl ? 'تحقق من جميع العروض' : 'Verify All Offers' }}
          </button>
        </div>

        <p class="text-sm text-gray-500">
          {{ isRtl ? 'يقوم النظام بالتحقق من صحة العمليات الحسابية في كل عرض مالي (سعر الوحدة × الكمية = الإجمالي) وكشف أي أخطاء حسابية.' : 'The system verifies arithmetic operations in each financial offer (unit price × quantity = total) and detects any calculation errors.' }}
        </p>

        <div class="space-y-4">
          <div
            v-for="total in supplierTotals"
            :key="`verify-${total.offerId}`"
            class="rounded-lg border border-gray-200 bg-white p-4"
          >
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-full bg-gray-800 text-sm font-bold text-white">
                  {{ total.blindCode?.slice(-2) }}
                </div>
                <div>
                  <span class="font-bold text-gray-900">{{ total.blindCode }}</span>
                  <div class="text-sm text-gray-500">{{ formatCurrency(total.totalAmount) }}</div>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span
                  v-if="verificationResults.has(total.offerId)"
                  class="rounded-full px-3 py-1 text-xs font-medium"
                  :class="verificationResults.get(total.offerId)!.hasErrors ? 'bg-red-100 text-red-700' : 'bg-green-100 text-green-700'"
                >
                  <i class="pi me-1" :class="verificationResults.get(total.offerId)!.hasErrors ? 'pi-times' : 'pi-check'" />
                  {{ verificationResults.get(total.offerId)!.hasErrors
                    ? (isRtl ? `${verificationResults.get(total.offerId)!.errorCount} أخطاء` : `${verificationResults.get(total.offerId)!.errorCount} errors`)
                    : (isRtl ? 'صحيح حسابياً' : 'Arithmetic Valid')
                  }}
                </span>
                <button
                  @click="verifyArithmetic(total.offerId)"
                  :disabled="verifyingOfferId === total.offerId"
                  class="rounded-lg border border-blue-300 bg-blue-50 px-3 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-100 disabled:opacity-50"
                >
                  <i :class="verifyingOfferId === total.offerId ? 'pi pi-spinner pi-spin' : 'pi pi-calculator'" class="me-1" />
                  {{ isRtl ? 'تحقق' : 'Verify' }}
                </button>
              </div>
            </div>

            <!-- Error details -->
            <div
              v-if="verificationResults.has(total.offerId) && verificationResults.get(total.offerId)!.hasErrors"
              class="mt-3 rounded-lg border border-red-200 bg-red-50 p-3"
            >
              <h4 class="mb-2 text-sm font-bold text-red-700">
                <i class="pi pi-exclamation-triangle me-1" />
                {{ isRtl ? 'الأخطاء الحسابية المكتشفة' : 'Arithmetic Errors Detected' }}
              </h4>
              <table class="w-full text-xs">
                <thead>
                  <tr>
                    <th class="px-2 py-1 text-start text-red-600">{{ isRtl ? 'البند' : 'Item' }}</th>
                    <th class="px-2 py-1 text-end text-red-600">{{ isRtl ? 'المحسوب' : 'Calculated' }}</th>
                    <th class="px-2 py-1 text-end text-red-600">{{ isRtl ? 'المقدم' : 'Submitted' }}</th>
                    <th class="px-2 py-1 text-end text-red-600">{{ isRtl ? 'الفرق' : 'Difference' }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="err in verificationResults.get(total.offerId)!.errors" :key="err.boqItemId">
                    <td class="px-2 py-1 text-gray-700">{{ err.itemNumber }}</td>
                    <td class="px-2 py-1 text-end text-gray-700">{{ formatCurrency(err.calculatedTotal) }}</td>
                    <td class="px-2 py-1 text-end text-red-600">{{ formatCurrency(err.supplierSubmittedTotal) }}</td>
                    <td class="px-2 py-1 text-end font-bold text-red-700">{{ formatCurrency(err.difference) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Valid message -->
            <div
              v-else-if="verificationResults.has(total.offerId) && !verificationResults.get(total.offerId)!.hasErrors"
              class="mt-3 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700"
            >
              <i class="pi pi-check-circle me-1" />
              {{ isRtl ? `تم التحقق من ${verificationResults.get(total.offerId)!.totalItems} بند - جميع العمليات الحسابية صحيحة` : `Verified ${verificationResults.get(total.offerId)!.totalItems} items - all arithmetic is correct` }}
            </div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- AI ANALYSIS TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'ai-analysis'" class="space-y-6">
        <div class="rounded-lg border border-purple-200 bg-gradient-to-l from-purple-50 to-white p-5">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-bold text-gray-900">
                <i class="pi pi-sparkles me-2 text-purple-600" />
                {{ isRtl ? 'التحليل المالي بالذكاء الاصطناعي' : 'AI Financial Analysis' }}
              </h2>
              <p class="mt-1 text-sm text-gray-500">
                {{ isRtl ? 'تحليل شامل للعروض المالية مع كشف الانحرافات والمخاطر المالية وتوصيات الترسية' : 'Comprehensive financial analysis with deviation detection, risk assessment, and award recommendations' }}
              </p>
            </div>
            <button
              @click="runAiAnalysis"
              :disabled="aiAnalysisLoading"
              class="rounded-lg bg-purple-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-purple-700 disabled:opacity-50"
            >
              <i :class="aiAnalysisLoading ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" class="me-1.5" />
              {{ aiAnalysisLoading ? (isRtl ? 'جارٍ التحليل...' : 'Analyzing...') : (isRtl ? 'تشغيل التحليل المالي' : 'Run Financial Analysis') }}
            </button>
          </div>
        </div>

        <!-- AI Analysis Result -->
        <div v-if="aiAnalysisResult" class="rounded-lg border border-purple-200 bg-white p-5">
          <div class="mb-3 flex items-center gap-2">
            <i class="pi pi-sparkles text-purple-600" />
            <h3 class="font-bold text-gray-900">{{ isRtl ? 'نتائج التحليل' : 'Analysis Results' }}</h3>
          </div>
          <div class="prose prose-sm max-w-none text-gray-700 whitespace-pre-wrap leading-relaxed" dir="rtl">
            {{ aiAnalysisResult }}
          </div>
        </div>

        <!-- AI Analysis Error -->
        <div v-if="aiAnalysisError" class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <i class="pi pi-exclamation-triangle me-1" />
          {{ aiAnalysisError }}
        </div>

        <!-- AI Analysis features cards (shown when no result yet) -->
        <div v-if="!aiAnalysisResult && !aiAnalysisLoading" class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div class="rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              <i class="pi pi-exclamation-triangle me-1 text-yellow-600" />
              {{ isRtl ? 'كشف الانحرافات السعرية' : 'Price Anomaly Detection' }}
            </h3>
            <p class="text-xs text-gray-500">
              {{ isRtl ? 'يقوم الذكاء الاصطناعي بتحليل أسعار البنود لكشف الأسعار المنخفضة بشكل غير طبيعي أو المرتفعة بشكل مبالغ فيه' : 'AI analyzes item prices to detect abnormally low or excessively high prices' }}
            </p>
          </div>
          <div class="rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              <i class="pi pi-shield me-1 text-red-600" />
              {{ isRtl ? 'تقييم المخاطر المالية' : 'Financial Risk Assessment' }}
            </h3>
            <p class="text-xs text-gray-500">
              {{ isRtl ? 'تقييم المخاطر المالية لكل عرض بناءً على هيكل التسعير والانحرافات' : 'Assess financial risks for each offer based on pricing structure and deviations' }}
            </p>
          </div>
          <div class="rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              <i class="pi pi-chart-line me-1 text-blue-600" />
              {{ isRtl ? 'مقارنة أسعار السوق' : 'Market Price Comparison' }}
            </h3>
            <p class="text-xs text-gray-500">
              {{ isRtl ? 'مقارنة الأسعار المقدمة مع أسعار السوق المعروفة والعقود السابقة' : 'Compare submitted prices with known market prices and similar contracts' }}
            </p>
          </div>
          <div class="rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              <i class="pi pi-star me-1 text-green-600" />
              {{ isRtl ? 'توصية الترسية' : 'Award Recommendation' }}
            </h3>
            <p class="text-xs text-gray-500">
              {{ isRtl ? 'توصية شاملة بالترسية بناءً على التقييم الفني والمالي مع تبرير مفصل' : 'Comprehensive award recommendation based on technical and financial evaluation' }}
            </p>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- RANKING TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'ranking'" class="space-y-4">
        <h2 class="text-lg font-bold text-gray-900">
          <i class="pi pi-sort-amount-down me-2 text-green-600" />
          {{ isRtl ? 'الترتيب النهائي للعروض المالية' : 'Final Financial Offer Ranking' }}
        </h2>

        <div class="space-y-3">
          <div
            v-for="(total, index) in sortedTotals"
            :key="total.offerId"
            class="rounded-lg border p-4 transition-all"
            :class="index === 0 ? 'border-green-300 bg-green-50' : 'border-gray-200 bg-white'"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-4">
                <div
                  class="flex h-12 w-12 items-center justify-center rounded-full text-lg font-bold"
                  :class="index === 0 ? 'bg-green-600 text-white' : index === 1 ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600'"
                >
                  {{ total.financialRank }}
                </div>
                <div>
                  <span class="text-base font-bold text-gray-900">{{ total.blindCode }}</span>
                  <span v-if="total.supplierName" class="ms-2 text-sm text-gray-500">{{ total.supplierName }}</span>
                  <div class="flex items-center gap-2 mt-1">
                    <span
                      class="rounded-full px-2 py-0.5 text-xs font-medium"
                      :class="getDeviationLevelBadge(total.deviationLevel).bg + ' ' + getDeviationLevelBadge(total.deviationLevel).text"
                    >
                      {{ getDeviationLevelBadge(total.deviationLevel).label }}
                    </span>
                    <span v-if="index === 0" class="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
                      {{ isRtl ? 'الأقل سعراً' : 'Lowest Price' }}
                    </span>
                  </div>
                </div>
              </div>
              <div class="text-end">
                <div class="text-xl font-bold text-gray-900">{{ formatCurrency(total.totalAmount) }}</div>
                <div
                  class="text-xs"
                  :class="getDeviationColor(total.deviationPercentage)"
                >
                  {{ total.deviationPercentage > 0 ? '+' : '' }}{{ total.deviationPercentage?.toFixed(1) }}% {{ isRtl ? 'عن التقدير' : 'from estimate' }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- MINUTES TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'minutes'" class="space-y-4">
        <div class="rounded-lg border border-gray-200 bg-white p-5">
          <h2 class="mb-4 text-lg font-bold text-gray-900">
            <i class="pi pi-file-edit me-2 text-green-600" />
            {{ isRtl ? 'محضر التقييم المالي' : 'Financial Evaluation Minutes' }}
          </h2>

          <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label class="mb-1 block text-xs font-medium text-gray-600">{{ isRtl ? 'تاريخ الاجتماع' : 'Meeting Date' }}</label>
              <input v-model="minutesData.meetingDate" type="date" class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-green-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-gray-600">{{ isRtl ? 'مكان الاجتماع' : 'Meeting Location' }}</label>
              <input v-model="minutesData.meetingLocation" type="text" :placeholder="isRtl ? 'قاعة الاجتماعات' : 'Meeting room'" class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-green-500 focus:outline-none" />
            </div>
          </div>

          <div class="mt-4">
            <label class="mb-2 block text-xs font-medium text-gray-600">{{ isRtl ? 'القرارات والتوصيات' : 'Decisions & Recommendations' }}</label>
            <textarea
              v-model="minutesData.decisions"
              rows="12"
              :placeholder="isRtl ? 'اكتب قرارات وتوصيات اللجنة هنا أو استخدم الذكاء الاصطناعي لتوليد المحضر...' : 'Write committee decisions here or use AI to generate minutes...'"
              class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-green-500 focus:outline-none"
            />
          </div>

          <div class="mt-4 flex gap-3">
            <button
              @click="generateMinutesWithAi"
              :disabled="generatingMinutes"
              class="rounded-lg border border-purple-300 bg-purple-50 px-4 py-2 text-sm font-medium text-purple-700 hover:bg-purple-100 disabled:opacity-50"
            >
              <i :class="generatingMinutes ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" class="me-1" />
              {{ generatingMinutes ? (isRtl ? 'جارٍ التوليد...' : 'Generating...') : (isRtl ? 'توليد المحضر بالذكاء الاصطناعي' : 'Generate Minutes with AI') }}
            </button>
            <button class="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700">
              <i class="pi pi-save me-1" />
              {{ isRtl ? 'حفظ المحضر' : 'Save Minutes' }}
            </button>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- SUBMIT BUTTON -->
      <!-- ═══════════════════════════════════════════════ -->
      <div class="flex items-center justify-between rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-sm text-gray-500">
          {{ isRtl ? 'بعد الانتهاء من مراجعة جميع العروض المالية، يمكنك تقديم التقييم المالي للاعتماد.' : 'After reviewing all financial offers, you can submit the financial evaluation for approval.' }}
        </div>
        <button
          @click="showSubmitDialog = true"
          class="rounded-lg bg-green-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-green-700"
        >
          <i class="pi pi-send me-1.5" />
          {{ isRtl ? 'تقديم التقييم المالي' : 'Submit Financial Evaluation' }}
        </button>
      </div>

      <!-- Submit confirmation dialog -->
      <div v-if="showSubmitDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div class="mx-4 w-full max-w-md rounded-lg bg-white p-6 shadow-xl" :dir="isRtl ? 'rtl' : 'ltr'">
          <h3 class="text-lg font-bold text-gray-900">{{ isRtl ? 'تأكيد تقديم التقييم المالي' : 'Confirm Financial Evaluation Submission' }}</h3>
          <p class="mt-2 text-sm text-gray-600">
            {{ isRtl ? 'هل أنت متأكد من تقديم التقييم المالي؟ سيتم إرساله للاعتماد.' : 'Are you sure? The evaluation will be sent for approval.' }}
          </p>

          <div class="mt-4 rounded-lg bg-gray-50 p-3">
            <div v-for="total in sortedTotals" :key="total.offerId" class="flex items-center justify-between py-1">
              <span class="text-sm text-gray-700">{{ total.blindCode }}</span>
              <span class="text-sm font-bold text-gray-900">{{ formatCurrency(total.totalAmount) }}</span>
            </div>
          </div>

          <div v-if="submitError" class="mt-3 text-sm text-red-600">{{ submitError }}</div>

          <div class="mt-5 flex gap-3">
            <button
              @click="showSubmitDialog = false"
              class="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              {{ isRtl ? 'إلغاء' : 'Cancel' }}
            </button>
            <button
              @click="submitFinancialEvaluation"
              :disabled="submitting"
              class="flex-1 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
            >
              <i :class="submitting ? 'pi pi-spinner pi-spin' : 'pi pi-check'" class="me-1" />
              {{ submitting ? (isRtl ? 'جارٍ التقديم...' : 'Submitting...') : (isRtl ? 'تأكيد التقديم' : 'Confirm') }}
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
