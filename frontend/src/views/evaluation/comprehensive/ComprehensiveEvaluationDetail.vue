<script setup lang="ts">
/**
 * ComprehensiveEvaluationDetail - Final comprehensive evaluation page.
 * Combines technical and financial evaluation results.
 * Generates award recommendation per Saudi government procurement regulations.
 * Tabs: Final Ranking | Award Recommendation | Comprehensive Minutes
 */
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'
import { httpPost } from '@/services/http'
import {
  fetchFinalRanking,
  fetchAwardRecommendation,
  generateAwardRecommendation,
  approveAward,
  rejectAward,
} from '@/services/evaluationApi'
import type { FinalRanking, AwardRecommendation } from '@/types/evaluation'
import { AwardStatus } from '@/types/evaluation'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const competitionId = computed(() => route.params.id as string)
const activeTab = ref<'ranking' | 'recommendation' | 'minutes'>('ranking')

/* State */
const loading = ref(false)
const finalRanking = ref<FinalRanking | null>(null)
const awardRecommendation = ref<AwardRecommendation | null>(null)
const generating = ref(false)
const approving = ref(false)
const rejecting = ref(false)
const errorMsg = ref('')

/* Weight configuration */
const technicalWeight = ref(70)
const financialWeight = ref(30)

/* Rejection dialog */
const showRejectDialog = ref(false)
const rejectionReason = ref('')

/* Minutes */
const minutesData = ref({
  meetingDate: '',
  meetingLocation: '',
  content: '',
})
const generatingMinutes = ref(false)

/* AI Justification */
const aiJustification = ref('')
const generatingJustification = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    await store.selectCompetition(competitionId.value)
    await loadFinalRanking()
    await loadAwardRecommendation()
  } catch (e: any) {
    console.error('Failed to load comprehensive evaluation:', e)
  } finally {
    loading.value = false
  }
})

async function loadFinalRanking() {
  try {
    finalRanking.value = await fetchFinalRanking(competitionId.value)
    technicalWeight.value = finalRanking.value.technicalWeight
    financialWeight.value = finalRanking.value.financialWeight
  } catch (e: any) {
    console.error('Failed to load final ranking:', e)
  }
}

async function loadAwardRecommendation() {
  try {
    awardRecommendation.value = await fetchAwardRecommendation(competitionId.value)
  } catch {
    // No recommendation generated yet — this is expected
    awardRecommendation.value = null
  }
}

async function handleGenerateRecommendation() {
  generating.value = true
  errorMsg.value = ''
  try {
    awardRecommendation.value = await generateAwardRecommendation(
      competitionId.value,
      technicalWeight.value,
      financialWeight.value
    )
    activeTab.value = 'recommendation'
  } catch (e: any) {
    errorMsg.value = e?.response?.data?.detail || e?.message || (isRtl.value ? 'فشل في توليد التوصية' : 'Failed to generate recommendation')
  } finally {
    generating.value = false
  }
}

async function handleApprove() {
  approving.value = true
  errorMsg.value = ''
  try {
    awardRecommendation.value = await approveAward(competitionId.value)
  } catch (e: any) {
    errorMsg.value = e?.response?.data?.detail || e?.message || (isRtl.value ? 'فشل في اعتماد التوصية' : 'Failed to approve')
  } finally {
    approving.value = false
  }
}

async function handleReject() {
  if (!rejectionReason.value.trim()) return
  rejecting.value = true
  errorMsg.value = ''
  try {
    awardRecommendation.value = await rejectAward(competitionId.value, rejectionReason.value)
    showRejectDialog.value = false
    rejectionReason.value = ''
  } catch (e: any) {
    errorMsg.value = e?.response?.data?.detail || e?.message || (isRtl.value ? 'فشل في رفض التوصية' : 'Failed to reject')
  } finally {
    rejecting.value = false
  }
}

function getStatusLabel(status: number): string {
  const labels: Record<number, { ar: string; en: string }> = {
    [AwardStatus.Draft]: { ar: 'مسودة', en: 'Draft' },
    [AwardStatus.Generated]: { ar: 'تم التوليد', en: 'Generated' },
    [AwardStatus.PendingApproval]: { ar: 'بانتظار الاعتماد', en: 'Pending Approval' },
    [AwardStatus.Approved]: { ar: 'معتمد', en: 'Approved' },
    [AwardStatus.Rejected]: { ar: 'مرفوض', en: 'Rejected' },
  }
  return labels[status]?.[isRtl.value ? 'ar' : 'en'] ?? '-'
}

function getStatusColor(status: number): string {
  switch (status) {
    case AwardStatus.Approved: return 'bg-green-100 text-green-700'
    case AwardStatus.Rejected: return 'bg-red-100 text-red-700'
    case AwardStatus.PendingApproval: return 'bg-yellow-100 text-yellow-700'
    case AwardStatus.Generated: return 'bg-blue-100 text-blue-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}

function getRankBadge(rank: number): string {
  switch (rank) {
    case 1: return 'bg-green-600 text-white'
    case 2: return 'bg-blue-100 text-blue-700'
    case 3: return 'bg-amber-100 text-amber-700'
    default: return 'bg-gray-100 text-gray-600'
  }
}

function getRankBorder(rank: number): string {
  switch (rank) {
    case 1: return 'border-green-300 bg-green-50'
    default: return 'border-gray-200 bg-white'
  }
}

function getScoreColor(score: number): string {
  if (score >= 80) return 'text-green-600'
  if (score >= 60) return 'text-blue-600'
  if (score >= 40) return 'text-yellow-600'
  return 'text-red-600'
}

function getScoreBg(score: number): string {
  if (score >= 80) return 'bg-green-500'
  if (score >= 60) return 'bg-blue-500'
  if (score >= 40) return 'bg-yellow-500'
  return 'bg-red-500'
}

async function generateComprehensiveMinutes() {
  generatingMinutes.value = true
  try {
    const rankings = finalRanking.value?.rankings ?? awardRecommendation.value?.rankings ?? []
    const minutesContext = JSON.stringify({
      competition: store.selectedCompetition?.projectName || finalRanking.value?.competitionName,
      competitionNumber: store.selectedCompetition?.competitionNumber,
      estimatedCost: finalRanking.value?.estimatedTotalCost,
      technicalWeight: technicalWeight.value,
      financialWeight: financialWeight.value,
      rankings: rankings.map(r => ({
        rank: r.rank,
        supplier: r.supplierName,
        technicalScore: r.technicalScore,
        financialScore: r.financialScore,
        combinedScore: r.combinedScore,
        totalAmount: r.totalOfferAmount,
      })),
      recommendation: awardRecommendation.value ? {
        recommendedSupplier: awardRecommendation.value.recommendedSupplierName,
        combinedScore: awardRecommendation.value.combinedScore,
        totalAmount: awardRecommendation.value.totalOfferAmount,
        justification: awardRecommendation.value.justification,
      } : null,
      meetingDate: minutesData.value.meetingDate,
      meetingLocation: minutesData.value.meetingLocation,
    })
    const data = await httpPost<{ isSuccess: boolean; generatedText: string }>('/v1/ai/text/assist', {
      action: 'custom',
      currentText: minutesContext,
      fieldName: 'محضر التقييم الشامل',
      fieldPurpose: 'إعداد محضر لجنة فحص العروض الشامل (فني ومالي) مع توصية الترسية',
      projectName: store.selectedCompetition?.projectName || finalRanking.value?.competitionName || '',
      customPrompt: `أنت كاتب محاضر رسمي للجان المشتريات الحكومية السعودية وفق نظام المنافسات والمشتريات الحكومية. قم بإعداد محضر اجتماع لجنة فحص العروض الشامل (التقييم النهائي) يتضمن:

1. بيانات الاجتماع (التاريخ، المكان، رقم المنافسة، اسم المشروع)
2. أسماء أعضاء اللجنة (اترك فراغات للأسماء والتوقيعات)
3. ملخص نتائج التقييم الفني مع درجات كل عرض
4. ملخص نتائج التقييم المالي مع المبالغ الإجمالية لكل عرض
5. الأوزان المستخدمة (${technicalWeight.value}% فني، ${financialWeight.value}% مالي)
6. الترتيب النهائي الشامل مع الدرجات المركبة
7. مقارنة أقل عرض بالتكلفة التقديرية
8. توصية اللجنة بالترسية على العرض الأفضل مع التبرير المفصل وفق المادة 45 من نظام المنافسات
9. الشروط والملاحظات (إن وجدت)
10. قرار اللجنة النهائي
11. فراغات التوقيعات لجميع أعضاء اللجنة

اكتب المحضر بأسلوب رسمي حكومي باللغة العربية. استخدم الأرقام الإنجليزية (1, 2, 3). لا تستخدم أي نص إنجليزي.`,
    })
    if (data.isSuccess) {
      minutesData.value.content = data.generatedText
    }
  } catch (e: any) {
    console.error('Minutes generation failed:', e)
  } finally {
    generatingMinutes.value = false
  }
}

async function generateAiJustification() {
  generatingJustification.value = true
  try {
    const rankings = finalRanking.value?.rankings ?? []
    const data = await httpPost<{ isSuccess: boolean; generatedText: string }>('/v1/ai/text/assist', {
      action: 'custom',
      currentText: JSON.stringify({
        competition: store.selectedCompetition?.projectName || finalRanking.value?.competitionName,
        estimatedCost: finalRanking.value?.estimatedTotalCost,
        rankings: rankings.map(r => ({
          rank: r.rank,
          supplier: r.supplierName,
          technicalScore: r.technicalScore,
          financialScore: r.financialScore,
          combinedScore: r.combinedScore,
          totalAmount: r.totalOfferAmount,
        })),
      }),
      fieldName: 'تبرير توصية الترسية',
      fieldPurpose: 'إعداد تبرير مفصل لتوصية الترسية وفق نظام المنافسات الحكومية',
      projectName: store.selectedCompetition?.projectName || '',
      customPrompt: `أنت خبير في المشتريات الحكومية السعودية. قم بإعداد تبرير مفصل لتوصية الترسية يتضمن:
1. تحليل مقارن بين العروض من الناحية الفنية والمالية
2. أسباب اختيار العرض الأفضل وفق معايير التقييم المعتمدة
3. مدى توافق العرض الموصى به مع الميزانية التقديرية
4. تقييم المخاطر المرتبطة بالعرض الموصى به
5. التوصية النهائية مع الإشارة للمواد النظامية ذات العلاقة

اكتب التبرير باللغة العربية بأسلوب رسمي. استخدم الأرقام الإنجليزية.`,
    })
    if (data.isSuccess) {
      aiJustification.value = data.generatedText
    }
  } catch (e: any) {
    console.error('AI justification generation failed:', e)
  } finally {
    generatingJustification.value = false
  }
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Back button -->
    <button
      class="flex items-center gap-2 text-sm text-gray-500 transition-colors hover:text-indigo-600"
      @click="router.push({ name: 'ComprehensiveEvaluation' })"
    >
      <i :class="isRtl ? 'pi pi-arrow-right' : 'pi pi-arrow-left'" />
      {{ isRtl ? 'العودة للقائمة' : 'Back to List' }}
    </button>

    <!-- Header -->
    <div class="rounded-lg border border-gray-200 bg-white p-5">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-bold text-gray-900">
            <i class="pi pi-chart-pie me-2 text-indigo-600" />
            {{ isRtl ? 'التقييم الشامل النهائي' : 'Final Comprehensive Evaluation' }}
          </h1>
          <p class="mt-1 text-sm text-gray-500">
            {{ store.selectedCompetition?.projectName || finalRanking?.competitionName || '' }}
          </p>
        </div>
        <div v-if="awardRecommendation" class="flex items-center gap-2">
          <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusColor(awardRecommendation.status)]">
            {{ getStatusLabel(awardRecommendation.status) }}
          </span>
        </div>
      </div>

      <!-- Summary Stats -->
      <div v-if="finalRanking" class="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
        <div class="rounded-lg bg-indigo-50 p-3 text-center">
          <div class="text-xs text-indigo-600">{{ isRtl ? 'عدد العروض المؤهلة' : 'Qualified Offers' }}</div>
          <div class="mt-1 text-xl font-bold text-indigo-900">{{ finalRanking.rankings.length }}</div>
        </div>
        <div class="rounded-lg bg-green-50 p-3 text-center">
          <div class="text-xs text-green-600">{{ isRtl ? 'التكلفة التقديرية' : 'Estimated Cost' }}</div>
          <div class="mt-1 text-lg font-bold text-green-900">{{ formatCurrency(finalRanking.estimatedTotalCost) }}</div>
        </div>
        <div class="rounded-lg bg-blue-50 p-3 text-center">
          <div class="text-xs text-blue-600">{{ isRtl ? 'الوزن الفني' : 'Technical Weight' }}</div>
          <div class="mt-1 text-xl font-bold text-blue-900">{{ finalRanking.technicalWeight }}%</div>
        </div>
        <div class="rounded-lg bg-amber-50 p-3 text-center">
          <div class="text-xs text-amber-600">{{ isRtl ? 'الوزن المالي' : 'Financial Weight' }}</div>
          <div class="mt-1 text-xl font-bold text-amber-900">{{ finalRanking.financialWeight }}%</div>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 rounded-lg bg-gray-100 p-1">
      <button
        v-for="tab in [
          { key: 'ranking', icon: 'pi-sort-amount-down', labelAr: 'الترتيب النهائي', labelEn: 'Final Ranking' },
          { key: 'recommendation', icon: 'pi-star', labelAr: 'توصية الترسية', labelEn: 'Award Recommendation' },
          { key: 'minutes', icon: 'pi-file-edit', labelAr: 'المحضر الشامل', labelEn: 'Comprehensive Minutes' },
        ]"
        :key="tab.key"
        @click="activeTab = tab.key as any"
        class="flex-1 rounded-md px-4 py-2.5 text-sm font-medium transition-all"
        :class="activeTab === tab.key
          ? 'bg-white text-indigo-700 shadow-sm'
          : 'text-gray-500 hover:text-gray-700'"
      >
        <i :class="['pi me-1.5', tab.icon]" />
        {{ isRtl ? tab.labelAr : tab.labelEn }}
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-16">
      <i class="pi pi-spinner pi-spin text-3xl text-indigo-600" />
      <span class="ms-3 text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <template v-else>
      <!-- ═══════════════════════════════════════════════ -->
      <!-- FINAL RANKING TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-if="activeTab === 'ranking'" class="space-y-4">
        <div v-if="!finalRanking" class="rounded-lg border border-dashed border-gray-300 py-12 text-center">
          <i class="pi pi-exclamation-triangle text-3xl text-yellow-500" />
          <p class="mt-3 text-sm text-gray-500">
            {{ isRtl ? 'لا يمكن عرض الترتيب النهائي. تأكد من اكتمال التقييم الفني والمالي.' : 'Cannot display final ranking. Ensure technical and financial evaluations are complete.' }}
          </p>
        </div>

        <template v-else>
          <!-- Ranking Table -->
          <div class="overflow-hidden rounded-lg border border-gray-200 bg-white">
            <table class="w-full">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-4 py-3 text-start text-xs font-bold text-gray-600">{{ isRtl ? 'الترتيب' : 'Rank' }}</th>
                  <th class="px-4 py-3 text-start text-xs font-bold text-gray-600">{{ isRtl ? 'المورد' : 'Supplier' }}</th>
                  <th class="px-4 py-3 text-center text-xs font-bold text-gray-600">
                    {{ isRtl ? `الدرجة الفنية (${finalRanking.technicalWeight}%)` : `Technical (${finalRanking.technicalWeight}%)` }}
                  </th>
                  <th class="px-4 py-3 text-center text-xs font-bold text-gray-600">
                    {{ isRtl ? `الدرجة المالية (${finalRanking.financialWeight}%)` : `Financial (${finalRanking.financialWeight}%)` }}
                  </th>
                  <th class="px-4 py-3 text-center text-xs font-bold text-gray-600">{{ isRtl ? 'الدرجة المركبة' : 'Combined Score' }}</th>
                  <th class="px-4 py-3 text-end text-xs font-bold text-gray-600">{{ isRtl ? 'إجمالي العرض' : 'Total Offer' }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr
                  v-for="ranking in finalRanking.rankings"
                  :key="ranking.offerId"
                  :class="ranking.rank === 1 ? 'bg-green-50' : ''"
                >
                  <td class="px-4 py-3">
                    <div
                      class="flex h-9 w-9 items-center justify-center rounded-full text-sm font-bold"
                      :class="getRankBadge(ranking.rank)"
                    >
                      {{ ranking.rank }}
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <div class="font-medium text-gray-900">{{ ranking.supplierName }}</div>
                    <div v-if="ranking.rank === 1" class="mt-0.5 text-xs text-green-600">
                      <i class="pi pi-star-fill me-1" />
                      {{ isRtl ? 'العرض الأفضل' : 'Best Offer' }}
                    </div>
                  </td>
                  <td class="px-4 py-3 text-center">
                    <div :class="['text-sm font-bold', getScoreColor(ranking.technicalScore)]">
                      {{ ranking.technicalScore.toFixed(2) }}
                    </div>
                    <div class="mx-auto mt-1 h-1.5 w-16 overflow-hidden rounded-full bg-gray-200">
                      <div :class="['h-full rounded-full', getScoreBg(ranking.technicalScore)]" :style="{ width: ranking.technicalScore + '%' }" />
                    </div>
                  </td>
                  <td class="px-4 py-3 text-center">
                    <div :class="['text-sm font-bold', getScoreColor(ranking.financialScore)]">
                      {{ ranking.financialScore.toFixed(2) }}
                    </div>
                    <div class="mx-auto mt-1 h-1.5 w-16 overflow-hidden rounded-full bg-gray-200">
                      <div :class="['h-full rounded-full', getScoreBg(ranking.financialScore)]" :style="{ width: ranking.financialScore + '%' }" />
                    </div>
                  </td>
                  <td class="px-4 py-3 text-center">
                    <div :class="['text-lg font-bold', getScoreColor(ranking.combinedScore)]">
                      {{ ranking.combinedScore.toFixed(2) }}
                    </div>
                  </td>
                  <td class="px-4 py-3 text-end">
                    <div class="text-sm font-bold text-gray-900">{{ formatCurrency(ranking.totalOfferAmount) }}</div>
                    <div
                      v-if="finalRanking.estimatedTotalCost > 0"
                      class="mt-0.5 text-xs"
                      :class="ranking.totalOfferAmount <= finalRanking.estimatedTotalCost ? 'text-green-600' : 'text-red-600'"
                    >
                      {{ ((ranking.totalOfferAmount - finalRanking.estimatedTotalCost) / finalRanking.estimatedTotalCost * 100).toFixed(1) }}%
                      {{ isRtl ? 'عن التقدير' : 'from estimate' }}
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Visual Comparison Chart -->
          <div class="rounded-lg border border-gray-200 bg-white p-5">
            <h3 class="mb-4 text-sm font-bold text-gray-900">
              <i class="pi pi-chart-bar me-1 text-indigo-600" />
              {{ isRtl ? 'المقارنة البصرية للدرجات' : 'Visual Score Comparison' }}
            </h3>
            <div class="space-y-4">
              <div v-for="ranking in finalRanking.rankings" :key="'chart-' + ranking.offerId" class="space-y-1">
                <div class="flex items-center justify-between text-xs text-gray-600">
                  <span class="font-medium">{{ ranking.supplierName }}</span>
                  <span class="font-bold">{{ ranking.combinedScore.toFixed(2) }}</span>
                </div>
                <div class="flex h-6 overflow-hidden rounded-full bg-gray-100">
                  <div
                    class="flex items-center justify-center bg-blue-500 text-[10px] font-bold text-white transition-all"
                    :style="{ width: (ranking.technicalScore * finalRanking.technicalWeight / 100) + '%' }"
                    :title="isRtl ? 'فني' : 'Technical'"
                  >
                    {{ (ranking.technicalScore * finalRanking.technicalWeight / 100).toFixed(1) }}
                  </div>
                  <div
                    class="flex items-center justify-center bg-green-500 text-[10px] font-bold text-white transition-all"
                    :style="{ width: (ranking.financialScore * finalRanking.financialWeight / 100) + '%' }"
                    :title="isRtl ? 'مالي' : 'Financial'"
                  >
                    {{ (ranking.financialScore * finalRanking.financialWeight / 100).toFixed(1) }}
                  </div>
                </div>
              </div>
              <div class="flex items-center gap-4 text-xs text-gray-500">
                <span class="flex items-center gap-1">
                  <span class="inline-block h-3 w-3 rounded bg-blue-500" />
                  {{ isRtl ? 'فني' : 'Technical' }} ({{ finalRanking.technicalWeight }}%)
                </span>
                <span class="flex items-center gap-1">
                  <span class="inline-block h-3 w-3 rounded bg-green-500" />
                  {{ isRtl ? 'مالي' : 'Financial' }} ({{ finalRanking.financialWeight }}%)
                </span>
              </div>
            </div>
          </div>

          <!-- Generate Recommendation Button -->
          <div v-if="!awardRecommendation" class="rounded-lg border border-indigo-200 bg-indigo-50 p-5">
            <div class="flex items-center justify-between">
              <div>
                <h3 class="text-sm font-bold text-indigo-900">
                  <i class="pi pi-star me-1" />
                  {{ isRtl ? 'توليد توصية الترسية' : 'Generate Award Recommendation' }}
                </h3>
                <p class="mt-1 text-xs text-indigo-700">
                  {{ isRtl ? 'سيتم توليد توصية الترسية تلقائياً بناءً على الترتيب النهائي للعروض' : 'Award recommendation will be auto-generated based on the final ranking' }}
                </p>
              </div>
              <button
                @click="handleGenerateRecommendation"
                :disabled="generating"
                class="rounded-lg bg-indigo-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                <i :class="generating ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" class="me-1.5" />
                {{ generating ? (isRtl ? 'جارٍ التوليد...' : 'Generating...') : (isRtl ? 'توليد التوصية' : 'Generate Recommendation') }}
              </button>
            </div>
            <div v-if="errorMsg" class="mt-3 text-sm text-red-600">{{ errorMsg }}</div>
          </div>
        </template>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- AWARD RECOMMENDATION TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'recommendation'" class="space-y-4">
        <div v-if="!awardRecommendation" class="rounded-lg border border-dashed border-gray-300 py-12 text-center">
          <i class="pi pi-star text-3xl text-gray-300" />
          <p class="mt-3 text-sm text-gray-500">
            {{ isRtl ? 'لم يتم توليد توصية الترسية بعد. انتقل لتبويب الترتيب النهائي لتوليد التوصية.' : 'No recommendation generated yet. Go to Final Ranking tab to generate.' }}
          </p>
        </div>

        <template v-else>
          <!-- Recommended Supplier Card -->
          <div class="overflow-hidden rounded-lg border-2 border-green-300 bg-gradient-to-br from-green-50 to-white">
            <div class="bg-green-600 px-5 py-3">
              <h2 class="text-base font-bold text-white">
                <i class="pi pi-trophy me-2" />
                {{ isRtl ? 'المورد الموصى بترسية المنافسة عليه' : 'Recommended Supplier for Award' }}
              </h2>
            </div>
            <div class="p-5">
              <div class="flex items-center justify-between">
                <div>
                  <div class="text-2xl font-bold text-gray-900">{{ awardRecommendation.recommendedSupplierName }}</div>
                  <div class="mt-2 flex items-center gap-4 text-sm">
                    <span class="flex items-center gap-1 text-blue-600">
                      <i class="pi pi-cog" />
                      {{ isRtl ? 'فني:' : 'Technical:' }}
                      <strong>{{ awardRecommendation.technicalScore.toFixed(2) }}</strong>
                    </span>
                    <span class="flex items-center gap-1 text-green-600">
                      <i class="pi pi-wallet" />
                      {{ isRtl ? 'مالي:' : 'Financial:' }}
                      <strong>{{ awardRecommendation.financialScore.toFixed(2) }}</strong>
                    </span>
                    <span class="flex items-center gap-1 text-indigo-600">
                      <i class="pi pi-chart-pie" />
                      {{ isRtl ? 'مركب:' : 'Combined:' }}
                      <strong>{{ awardRecommendation.combinedScore.toFixed(2) }}</strong>
                    </span>
                  </div>
                </div>
                <div class="text-end">
                  <div class="text-xs text-gray-500">{{ isRtl ? 'إجمالي العرض' : 'Total Offer' }}</div>
                  <div class="text-2xl font-bold text-green-700">{{ formatCurrency(awardRecommendation.totalOfferAmount) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- All Rankings -->
          <div class="rounded-lg border border-gray-200 bg-white p-5">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              <i class="pi pi-list me-1 text-indigo-600" />
              {{ isRtl ? 'ترتيب جميع العروض' : 'All Offers Ranking' }}
            </h3>
            <div class="space-y-2">
              <div
                v-for="r in awardRecommendation.rankings"
                :key="r.offerId"
                class="flex items-center justify-between rounded-lg border p-3 transition-all"
                :class="getRankBorder(r.rank)"
              >
                <div class="flex items-center gap-3">
                  <div
                    class="flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold"
                    :class="getRankBadge(r.rank)"
                  >
                    {{ r.rank }}
                  </div>
                  <div>
                    <span class="font-medium text-gray-900">{{ r.supplierName }}</span>
                    <div class="flex gap-3 text-xs text-gray-500">
                      <span>{{ isRtl ? 'فني' : 'Tech' }}: {{ r.technicalScore.toFixed(1) }}</span>
                      <span>{{ isRtl ? 'مالي' : 'Fin' }}: {{ r.financialScore.toFixed(1) }}</span>
                      <span class="font-bold text-indigo-600">{{ isRtl ? 'مركب' : 'Combined' }}: {{ r.combinedScore.toFixed(2) }}</span>
                    </div>
                  </div>
                </div>
                <div class="text-end">
                  <div class="text-sm font-bold text-gray-900">{{ formatCurrency(r.totalOfferAmount) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Justification -->
          <div class="rounded-lg border border-gray-200 bg-white p-5">
            <div class="flex items-center justify-between mb-3">
              <h3 class="text-sm font-bold text-gray-900">
                <i class="pi pi-file-check me-1 text-indigo-600" />
                {{ isRtl ? 'تبرير التوصية' : 'Recommendation Justification' }}
              </h3>
              <button
                @click="generateAiJustification"
                :disabled="generatingJustification"
                class="rounded-lg border border-purple-300 bg-purple-50 px-3 py-1.5 text-xs font-medium text-purple-700 hover:bg-purple-100 disabled:opacity-50"
              >
                <i :class="generatingJustification ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" class="me-1" />
                {{ isRtl ? 'توليد تبرير بالذكاء الاصطناعي' : 'Generate AI Justification' }}
              </button>
            </div>
            <div v-if="awardRecommendation.justification" class="rounded-lg bg-gray-50 p-4 text-sm leading-relaxed text-gray-700 whitespace-pre-wrap">
              {{ awardRecommendation.justification }}
            </div>
            <div v-if="aiJustification" class="mt-3 rounded-lg border border-purple-200 bg-purple-50 p-4 text-sm leading-relaxed text-gray-700 whitespace-pre-wrap">
              <div class="mb-2 text-xs font-bold text-purple-700">
                <i class="pi pi-sparkles me-1" />
                {{ isRtl ? 'تبرير الذكاء الاصطناعي' : 'AI-Generated Justification' }}
              </div>
              {{ aiJustification }}
            </div>
          </div>

          <!-- Approval Actions -->
          <div class="flex items-center justify-between rounded-lg border border-gray-200 bg-white p-4">
            <div>
              <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusColor(awardRecommendation.status)]">
                {{ getStatusLabel(awardRecommendation.status) }}
              </span>
              <span v-if="awardRecommendation.approvedAt" class="ms-3 text-xs text-gray-500">
                {{ isRtl ? 'تاريخ الاعتماد:' : 'Approved:' }} {{ new Date(awardRecommendation.approvedAt).toLocaleDateString('ar-SA') }}
              </span>
              <span v-if="awardRecommendation.rejectionReason" class="ms-3 text-xs text-red-600">
                {{ isRtl ? 'سبب الرفض:' : 'Reason:' }} {{ awardRecommendation.rejectionReason }}
              </span>
            </div>
            <div v-if="awardRecommendation.status !== 3 && awardRecommendation.status !== 4" class="flex gap-2">
              <button
                @click="showRejectDialog = true"
                :disabled="rejecting"
                class="rounded-lg border border-red-300 px-4 py-2 text-sm font-medium text-red-700 hover:bg-red-50 disabled:opacity-50"
              >
                <i class="pi pi-times me-1" />
                {{ isRtl ? 'رفض' : 'Reject' }}
              </button>
              <button
                @click="handleApprove"
                :disabled="approving"
                class="rounded-lg bg-green-600 px-6 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
              >
                <i :class="approving ? 'pi pi-spinner pi-spin' : 'pi pi-check'" class="me-1" />
                {{ approving ? (isRtl ? 'جارٍ الاعتماد...' : 'Approving...') : (isRtl ? 'اعتماد التوصية' : 'Approve Recommendation') }}
              </button>
            </div>
          </div>

          <div v-if="errorMsg" class="rounded-lg bg-red-50 p-3 text-sm text-red-600">{{ errorMsg }}</div>
        </template>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- COMPREHENSIVE MINUTES TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'minutes'" class="space-y-4">
        <div class="rounded-lg border border-gray-200 bg-white p-5">
          <h2 class="mb-4 text-lg font-bold text-gray-900">
            <i class="pi pi-file-edit me-2 text-indigo-600" />
            {{ isRtl ? 'محضر التقييم الشامل النهائي' : 'Final Comprehensive Evaluation Minutes' }}
          </h2>

          <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label class="mb-1 block text-xs font-medium text-gray-600">{{ isRtl ? 'تاريخ الاجتماع' : 'Meeting Date' }}</label>
              <input v-model="minutesData.meetingDate" type="date" class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-gray-600">{{ isRtl ? 'مكان الاجتماع' : 'Meeting Location' }}</label>
              <input v-model="minutesData.meetingLocation" type="text" :placeholder="isRtl ? 'قاعة الاجتماعات الرئيسية' : 'Main meeting room'" class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
          </div>

          <div class="mt-4">
            <label class="mb-2 block text-xs font-medium text-gray-600">{{ isRtl ? 'محتوى المحضر' : 'Minutes Content' }}</label>
            <textarea
              v-model="minutesData.content"
              rows="16"
              :placeholder="isRtl ? 'اكتب محتوى المحضر هنا أو استخدم الذكاء الاصطناعي لتوليده تلقائياً...' : 'Write minutes content here or use AI to auto-generate...'"
              class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm leading-relaxed focus:border-indigo-500 focus:outline-none"
            />
          </div>

          <div class="mt-4 flex gap-3">
            <button
              @click="generateComprehensiveMinutes"
              :disabled="generatingMinutes"
              class="rounded-lg border border-purple-300 bg-purple-50 px-4 py-2 text-sm font-medium text-purple-700 hover:bg-purple-100 disabled:opacity-50"
            >
              <i :class="generatingMinutes ? 'pi pi-spinner pi-spin' : 'pi pi-sparkles'" class="me-1" />
              {{ generatingMinutes ? (isRtl ? 'جارٍ التوليد...' : 'Generating...') : (isRtl ? 'توليد المحضر بالذكاء الاصطناعي' : 'Generate Minutes with AI') }}
            </button>
            <button class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700">
              <i class="pi pi-save me-1" />
              {{ isRtl ? 'حفظ المحضر' : 'Save Minutes' }}
            </button>
            <button class="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50">
              <i class="pi pi-print me-1" />
              {{ isRtl ? 'طباعة' : 'Print' }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- Reject Dialog -->
    <div v-if="showRejectDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div class="mx-4 w-full max-w-md rounded-lg bg-white p-6 shadow-xl" :dir="isRtl ? 'rtl' : 'ltr'">
        <h3 class="text-lg font-bold text-gray-900">
          <i class="pi pi-times-circle me-2 text-red-600" />
          {{ isRtl ? 'رفض توصية الترسية' : 'Reject Award Recommendation' }}
        </h3>
        <p class="mt-2 text-sm text-gray-600">
          {{ isRtl ? 'يرجى تحديد سبب الرفض:' : 'Please specify the rejection reason:' }}
        </p>
        <textarea
          v-model="rejectionReason"
          rows="4"
          :placeholder="isRtl ? 'اكتب سبب الرفض...' : 'Enter rejection reason...'"
          class="mt-3 w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-red-500 focus:outline-none"
        />
        <div class="mt-4 flex gap-3">
          <button
            @click="showRejectDialog = false"
            class="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {{ isRtl ? 'إلغاء' : 'Cancel' }}
          </button>
          <button
            @click="handleReject"
            :disabled="rejecting || !rejectionReason.trim()"
            class="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
          >
            <i :class="rejecting ? 'pi pi-spinner pi-spin' : 'pi pi-times'" class="me-1" />
            {{ rejecting ? (isRtl ? 'جارٍ الرفض...' : 'Rejecting...') : (isRtl ? 'تأكيد الرفض' : 'Confirm Reject') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
