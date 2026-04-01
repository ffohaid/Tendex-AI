<script setup lang="ts">
/**
 * TechnicalEvaluationDetail - Main technical evaluation workspace.
 * Implements blind evaluation with AI-assisted dual scoring.
 * Vendors are shown with anonymous codes only.
 * Includes dedicated AI analysis tab, heatmap matrix, and minutes.
 */
import { onMounted, ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import type { EvaluationCriterion } from '@/types/evaluation'
import {
  triggerAiAnalysis as triggerAi,
  getAiAnalysisSummary,
  getAiOfferAnalysis,
  getAiComparisonMatrix,
  reviewAiAnalysis,
  type AiAnalysisSummary,
  type AiOfferAnalysis,
  type AiComparisonMatrix,
} from '@/services/aiEvaluationService'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const competitionId = computed(() => route.params.id as string)
const selectedCriterion = ref<EvaluationCriterion | null>(null)
const activeTab = ref<'scoring' | 'ai-analysis' | 'matrix' | 'minutes'>('scoring')
const savingStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')
const showSubmitDialog = ref(false)
const submitting = ref(false)
const submitError = ref('')
const startingEvaluation = ref(false)

/* Local score map: offerId -> { score, notes } */
const localScores = ref<Record<string, { score: number; notes: string }>>({})

/* AI Analysis state */
const aiSummary = ref<AiAnalysisSummary | null>(null)
const aiOfferDetails = ref<Record<string, AiOfferAnalysis>>({})
const aiCompMatrix = ref<AiComparisonMatrix | null>(null)
const aiLoading = ref(false)
const aiError = ref('')
const selectedAiOfferId = ref<string | null>(null)
const reviewNotes = ref('')

/* Minutes state */
const minutesContent = ref('')
const minutesGenerating = ref(false)
const minutesId = ref<string | null>(null)
const minutesError = ref('')
const minutesData = ref<any>(null)

function printMinutes() {
  window.print()
}

onMounted(async () => {
  await store.loadTechnicalData(competitionId.value)
  if (store.criteria.length > 0) {
    selectedCriterion.value = store.criteria[0]
  }
  initializeLocalScores()
})

function initializeLocalScores() {
  store.blindOffers.forEach(offer => {
    if (selectedCriterion.value) {
      const existing = store.technicalScores.find(
        s => s.supplierOfferId === offer.offerId && s.evaluationCriterionId === selectedCriterion.value!.id
      )
      localScores.value[offer.offerId] = {
        score: existing?.score ?? 0,
        notes: existing?.notes ?? '',
      }
    }
  })
}

function onCriterionSelect(criterion: EvaluationCriterion) {
  selectedCriterion.value = criterion
  initializeLocalScores()
}

function updateScore(offerId: string, score: number) {
  if (!localScores.value[offerId]) {
    localScores.value[offerId] = { score: 0, notes: '' }
  }
  localScores.value[offerId].score = Math.max(0, Math.min(100, score))
}

function updateNotes(offerId: string, notes: string) {
  if (!localScores.value[offerId]) {
    localScores.value[offerId] = { score: 0, notes: '' }
  }
  localScores.value[offerId].notes = notes
}

function getScoreColor(score: number): string {
  if (score >= 80) return 'text-green-600'
  if (score >= 60) return 'text-yellow-600'
  return 'text-red-600'
}

function getScoreBgColor(score: number): string {
  if (score >= 80) return 'bg-green-50 border-green-200'
  if (score >= 60) return 'bg-yellow-50 border-yellow-200'
  return 'bg-red-50 border-red-200'
}

function getAiScore(offerId: string) {
  if (!selectedCriterion.value) return undefined
  return store.aiTechnicalScores.find(
    a => a.supplierOfferId === offerId && a.evaluationCriterionId === selectedCriterion.value!.id
  )
}

async function saveScores() {
  if (!selectedCriterion.value) return
  savingStatus.value = 'saving'
  try {
    for (const [offerId, data] of Object.entries(localScores.value)) {
      await store.submitTechScore(
        competitionId.value,
        offerId,
        selectedCriterion.value!.id,
        data.score,
        data.notes || undefined
      )
    }
    savingStatus.value = 'saved'
    setTimeout(() => { savingStatus.value = 'idle' }, 2000)
  } catch {
    savingStatus.value = 'error'
    setTimeout(() => { savingStatus.value = 'idle' }, 3000)
  }
}

/* Start evaluation */
async function startEvaluation() {
  startingEvaluation.value = true
  try {
    const { startTechnicalEvaluation } = await import('@/services/evaluationApi')
    await startTechnicalEvaluation(competitionId.value)
    await store.loadTechnicalData(competitionId.value)
  } catch (e: any) {
    store.error = e?.message || 'تعذر بدء التقييم الفني'
  } finally {
    startingEvaluation.value = false
  }
}

/* AI Analysis functions */
async function runAiAnalysis() {
  aiLoading.value = true
  aiError.value = ''
  try {
    const evalId = store.technicalEvaluation?.id ?? ''
    aiSummary.value = await triggerAi(competitionId.value, evalId)
    // Also load comparison matrix
    try {
      aiCompMatrix.value = await getAiComparisonMatrix(competitionId.value, evalId)
    } catch { /* matrix may not be ready */ }
  } catch (e: any) {
    aiError.value = e?.message || 'حدث خطأ أثناء تشغيل تحليل الذكاء الاصطناعي'
  } finally {
    aiLoading.value = false
  }
}

async function loadAiSummary() {
  try {
    const evalId = store.technicalEvaluation?.id ?? ''
    aiSummary.value = await getAiAnalysisSummary(competitionId.value, evalId)
  } catch {
    // Summary not available yet
  }
}

async function loadAiOfferDetail(analysisId: string) {
  selectedAiOfferId.value = analysisId
  if (aiOfferDetails.value[analysisId]) return
  try {
    const detail = await getAiOfferAnalysis(competitionId.value, analysisId)
    aiOfferDetails.value[analysisId] = detail
  } catch {
    aiError.value = 'تعذر تحميل تفاصيل تحليل العرض'
  }
}

async function submitAiReview(analysisId: string) {
  try {
    const updated = await reviewAiAnalysis(competitionId.value, analysisId, reviewNotes.value)
    aiOfferDetails.value[analysisId] = updated
    reviewNotes.value = ''
    // Refresh summary
    await loadAiSummary()
  } catch {
    aiError.value = 'تعذر حفظ المراجعة'
  }
}

const completedCriteria = computed(() => {
  if (!store.criteria.length || !store.blindOffers.length) return 0
  const criteriaWithScores = new Set(
    store.technicalScores.map(s => s.evaluationCriterionId)
  )
  return criteriaWithScores.size
})

const overallProgress = computed(() => {
  if (!store.criteria.length || !store.blindOffers.length) return 0
  const total = store.criteria.length * store.blindOffers.length
  return total > 0 ? Math.round((store.technicalScores.length / total) * 100) : 0
})

/* Offer total scores (weighted) */
const offerTotals = computed(() => {
  const totals: Record<string, number> = {}
  store.blindOffers.forEach(offer => {
    const scores = store.technicalScores.filter(s => s.supplierOfferId === offer.offerId)
    const totalWeight = store.criteria.reduce((sum, c) => sum + c.weight, 0)
    let weightedScore = 0
    store.criteria.forEach(criterion => {
      const score = scores.find(s => s.evaluationCriterionId === criterion.id)
      if (score && totalWeight > 0) {
        weightedScore += (score.score / score.maxScore) * (criterion.weight / totalWeight) * 100
      }
    })
    totals[offer.offerId] = Math.round(weightedScore * 10) / 10
  })
  return totals
})

/* Heatmap data from store */
const heatmapData = computed(() => {
  if (store.technicalHeatmap) return store.technicalHeatmap
  // Fallback: build from local data
  return {
    competitionId: competitionId.value,
    evaluationId: store.technicalEvaluation?.id ?? '',
    offerBlindCodes: store.blindOffers.map(o => o.blindCode),
    criteria: store.criteria.map(c => ({
      id: c.id,
      nameAr: c.nameAr || c.name,
      nameEn: c.nameEn || c.name,
      weightPercentage: c.weight,
      minimumPassingScore: c.minimumScore || null,
    })),
    cells: store.criteria.flatMap(criterion =>
      store.blindOffers.map(offer => {
        const score = store.technicalScores.find(
          s => s.supplierOfferId === offer.offerId && s.evaluationCriterionId === criterion.id
        )
        const pct = score ? (score.score / score.maxScore) * 100 : 0
        return {
          offerBlindCode: offer.blindCode,
          offerId: offer.offerId,
          criterionId: criterion.id,
          criterionNameAr: criterion.nameAr || criterion.name,
          criterionNameEn: criterion.nameEn || criterion.name,
          averageScorePercentage: pct,
          color: pct >= 80 ? 'excellent' : pct >= 60 ? 'average' : 'weak',
        }
      })
    ),
  }
})

function getHeatmapCell(blindCode: string, criterionId: string) {
  return heatmapData.value.cells.find(
    c => c.offerBlindCode === blindCode && c.criterionId === criterionId
  )
}

function getHeatmapCellColor(pct: number): string {
  if (pct >= 80) return 'bg-green-100 text-green-800'
  if (pct >= 60) return 'bg-yellow-100 text-yellow-800'
  if (pct > 0) return 'bg-red-100 text-red-800'
  return 'bg-gray-50 text-gray-400'
}

/* Generate minutes - builds a professional HTML-formatted minutes document */
async function generateMinutes() {
  minutesGenerating.value = true
  minutesError.value = ''
  try {
    // Try to call backend API first
    try {
      const { generateMinutes: genApi, fetchMinutesList } = await import('@/services/evaluationApi')
      // Check if minutes already exist
      const list = await fetchMinutesList(competitionId.value)
      const techMinutes = list?.find((m: any) => m.minutesType === 1)
      if (techMinutes) {
        minutesId.value = techMinutes.id
        minutesData.value = techMinutes
      } else {
        // Generate new minutes via API
        const result = await genApi(competitionId.value, 1) // 1 = TechnicalEvaluation
        minutesId.value = result.id
        minutesData.value = result
      }
    } catch (apiErr: any) {
      console.warn('Backend minutes API unavailable, using local generation:', apiErr)
    }

    // Build professional HTML minutes from local data
    const passingScore = store.technicalEvaluation?.minimumPassingScore ?? 60
    const hijriDate = new Date().toLocaleDateString('ar-SA-u-ca-islamic-umalqura', { year: 'numeric', month: 'long', day: 'numeric' })
    const gregorianDate = new Date().toLocaleDateString('ar-SA', { year: 'numeric', month: 'long', day: 'numeric' })
    const competitionName = store.selectedCompetition?.projectName || competitionId.value

    // Sort offers by total score descending
    const sortedOffers = [...store.blindOffers].sort((a, b) => {
      return (offerTotals.value[b.offerId] ?? 0) - (offerTotals.value[a.offerId] ?? 0)
    })

    const passedCount = sortedOffers.filter(o => (offerTotals.value[o.offerId] ?? 0) >= passingScore).length
    const failedCount = sortedOffers.length - passedCount

    // Build criteria scores table rows
    let criteriaRows = ''
    store.criteria.forEach(criterion => {
      criteriaRows += `<tr class="border-b border-gray-200">`
      criteriaRows += `<td class="px-3 py-2 text-sm font-medium text-gray-900">${criterion.name}</td>`
      criteriaRows += `<td class="px-3 py-2 text-center text-sm text-gray-600">${criterion.weight}%</td>`
      sortedOffers.forEach(offer => {
        const score = store.technicalScores.find(
          s => s.supplierOfferId === offer.offerId && s.evaluationCriterionId === criterion.id
        )
        const pct = score ? Math.round((score.score / score.maxScore) * 100) : 0
        const colorClass = pct >= 80 ? 'text-green-700 bg-green-50' : pct >= 60 ? 'text-yellow-700 bg-yellow-50' : 'text-red-700 bg-red-50'
        criteriaRows += `<td class="px-3 py-2 text-center text-sm ${colorClass} font-semibold">${score?.score ?? '-'} / ${score?.maxScore ?? 100}</td>`
      })
      criteriaRows += `</tr>`
    })

    // Build results table rows
    let resultsRows = ''
    let rank = 1
    sortedOffers.forEach(offer => {
      const total = offerTotals.value[offer.offerId] ?? 0
      const passed = total >= passingScore
      const statusClass = passed ? 'text-green-700 bg-green-50' : 'text-red-700 bg-red-50'
      const statusText = passed ? 'مؤهل فنياً' : 'غير مؤهل'
      resultsRows += `<tr class="border-b border-gray-200">`
      resultsRows += `<td class="px-3 py-2 text-center text-sm font-bold text-gray-900">${rank}</td>`
      resultsRows += `<td class="px-3 py-2 text-sm font-medium text-gray-900">${offer.blindCode}</td>`
      resultsRows += `<td class="px-3 py-2 text-center text-sm font-bold text-blue-700">${total}%</td>`
      resultsRows += `<td class="px-3 py-2 text-center text-sm font-semibold ${statusClass}">${statusText}</td>`
      resultsRows += `</tr>`
      rank++
    })

    // Build variance alerts
    let varianceHtml = ''
    if (store.varianceAlerts.length > 0) {
      varianceHtml = `
        <div class="mt-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-orange-300 pb-2">سادساً: تنبيهات التباين بين التقييم البشري والذكاء الاصطناعي</h3>
          <div class="space-y-2">`
      store.varianceAlerts.forEach(alert => {
        varianceHtml += `
            <div class="flex items-start gap-2 rounded-lg bg-orange-50 p-3 border border-orange-200">
              <span class="text-orange-500 mt-0.5">⚠</span>
              <div>
                <p class="text-sm font-medium text-gray-900">${alert.criterionNameAr} - ${alert.offerBlindCode}</p>
                ${alert.hasHumanAiVariance ? `<p class="text-xs text-orange-700">فرق بين التقييم البشري والذكاء الاصطناعي: ${alert.humanAiDifference}%</p>` : ''}
                ${alert.hasEvaluatorVariance ? `<p class="text-xs text-orange-700">تباين بين المقيمين: ${alert.evaluatorSpread}%</p>` : ''}
              </div>
            </div>`
      })
      varianceHtml += `
          </div>
        </div>`
    }

    // Build committee signatures
    let signaturesHtml = ''
    if (store.committee) {
      store.committee.members.forEach(m => {
        const roleText = m.role === 'chair' ? 'رئيس اللجنة' : m.role === 'secretary' ? 'أمين السر' : 'عضو'
        signaturesHtml += `
          <div class="flex items-center justify-between border-b border-gray-200 py-3">
            <div>
              <p class="text-sm font-semibold text-gray-900">${m.name}</p>
              <p class="text-xs text-gray-500">${roleText}</p>
            </div>
            <div class="w-40 border-b-2 border-dotted border-gray-400 text-center text-xs text-gray-400">التوقيع</div>
          </div>`
      })
    }

    // AI recommendation
    let aiRecommendation = ''
    if (aiSummary.value && aiSummary.value.completedAnalyses > 0) {
      const bestOffer = sortedOffers[0]
      aiRecommendation = `
        <div class="mt-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-purple-300 pb-2">سابعاً: ملاحظات الذكاء الاصطناعي</h3>
          <div class="rounded-lg bg-purple-50 p-4 border border-purple-200">
            <p class="text-sm text-gray-800 leading-relaxed">
              تم تحليل جميع العروض المقدمة باستخدام الذكاء الاصطناعي. أظهر التحليل أن العرض <strong>${bestOffer?.blindCode}</strong>
              حصل على أعلى تقييم مرجح بنسبة <strong>${offerTotals.value[bestOffer?.offerId] ?? 0}%</strong>.
              ${store.varianceAlerts.length > 0 ? `تم رصد ${store.varianceAlerts.length} تنبيه تباين بين التقييم البشري وتقييم الذكاء الاصطناعي تستدعي مراجعة اللجنة.` : 'لم يتم رصد تباينات جوهرية بين التقييم البشري وتقييم الذكاء الاصطناعي.'}
            </p>
          </div>
        </div>`
    }

    minutesContent.value = `
      <div class="max-w-4xl mx-auto">
        <!-- Header -->
        <div class="text-center mb-8 pb-4 border-b-4 border-blue-600">
          <h1 class="text-xl font-bold text-blue-900 mb-2">محضر اجتماع لجنة فحص العروض الفنية</h1>
          <p class="text-sm text-gray-600">${competitionName}</p>
        </div>

        <!-- Metadata -->
        <div class="grid grid-cols-2 gap-4 mb-6 rounded-lg bg-gray-50 p-4 border border-gray-200">
          <div class="text-sm"><span class="font-semibold text-gray-700">رقم المنافسة:</span> <span class="text-gray-900">${competitionId.value.substring(0, 8)}</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">التاريخ الهجري:</span> <span class="text-gray-900">${hijriDate}</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">عدد العروض المقدمة:</span> <span class="text-gray-900">${store.blindOffers.length}</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">التاريخ الميلادي:</span> <span class="text-gray-900">${gregorianDate}</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">عدد المعايير:</span> <span class="text-gray-900">${store.criteria.length}</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">درجة النجاح:</span> <span class="text-gray-900">${passingScore}%</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">العروض المؤهلة:</span> <span class="text-green-700 font-bold">${passedCount}</span></div>
          <div class="text-sm"><span class="font-semibold text-gray-700">العروض غير المؤهلة:</span> <span class="text-red-700 font-bold">${failedCount}</span></div>
        </div>

        <!-- Introduction -->
        <div class="mb-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-blue-300 pb-2">أولاً: مقدمة</h3>
          <p class="text-sm text-gray-800 leading-relaxed">
            بناءً على قرار تشكيل لجنة فحص العروض، وبعد استلام العروض الفنية المقدمة من المتنافسين على مشروع
            <strong>"${competitionName}"</strong>، اجتمعت اللجنة لفحص وتقييم العروض الفنية المقدمة وفقاً لمعايير التقييم
            المعتمدة في كراسة الشروط والمواصفات، وذلك بتطبيق أسلوب التقييم الأعمى لضمان الحيادية والشفافية.
          </p>
        </div>

        <!-- Evaluation Criteria -->
        <div class="mb-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-blue-300 pb-2">ثانياً: معايير التقييم المعتمدة</h3>
          <table class="w-full border-collapse border border-gray-300 text-sm">
            <thead>
              <tr class="bg-blue-50">
                <th class="border border-gray-300 px-3 py-2 text-start font-semibold text-blue-900">المعيار</th>
                <th class="border border-gray-300 px-3 py-2 text-center font-semibold text-blue-900">الوزن</th>
              </tr>
            </thead>
            <tbody>
              ${store.criteria.map(c => `<tr class="border-b border-gray-200"><td class="border border-gray-300 px-3 py-2">${c.name}</td><td class="border border-gray-300 px-3 py-2 text-center font-semibold">${c.weight}%</td></tr>`).join('')}
              <tr class="bg-gray-100 font-bold"><td class="border border-gray-300 px-3 py-2">الإجمالي</td><td class="border border-gray-300 px-3 py-2 text-center">100%</td></tr>
            </tbody>
          </table>
        </div>

        <!-- Detailed Scores -->
        <div class="mb-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-blue-300 pb-2">ثالثاً: تفاصيل الدرجات حسب المعيار</h3>
          <div class="overflow-x-auto">
            <table class="w-full border-collapse border border-gray-300 text-sm">
              <thead>
                <tr class="bg-blue-50">
                  <th class="border border-gray-300 px-3 py-2 text-start font-semibold text-blue-900">المعيار</th>
                  <th class="border border-gray-300 px-3 py-2 text-center font-semibold text-blue-900">الوزن</th>
                  ${sortedOffers.map(o => `<th class="border border-gray-300 px-3 py-2 text-center font-semibold text-blue-900">${o.blindCode}</th>`).join('')}
                </tr>
              </thead>
              <tbody>
                ${criteriaRows}
                <tr class="bg-gray-100 font-bold">
                  <td class="border border-gray-300 px-3 py-2">المجموع المرجح</td>
                  <td class="border border-gray-300 px-3 py-2 text-center">100%</td>
                  ${sortedOffers.map(o => {
                    const t = offerTotals.value[o.offerId] ?? 0
                    const c = t >= 80 ? 'text-green-700' : t >= 60 ? 'text-yellow-700' : 'text-red-700'
                    return `<td class="border border-gray-300 px-3 py-2 text-center ${c}">${t}%</td>`
                  }).join('')}
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Results Summary -->
        <div class="mb-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-blue-300 pb-2">رابعاً: نتائج التقييم الفني</h3>
          <table class="w-full border-collapse border border-gray-300 text-sm">
            <thead>
              <tr class="bg-blue-50">
                <th class="border border-gray-300 px-3 py-2 text-center font-semibold text-blue-900">الترتيب</th>
                <th class="border border-gray-300 px-3 py-2 text-start font-semibold text-blue-900">رمز العرض</th>
                <th class="border border-gray-300 px-3 py-2 text-center font-semibold text-blue-900">الدرجة المرجحة</th>
                <th class="border border-gray-300 px-3 py-2 text-center font-semibold text-blue-900">النتيجة</th>
              </tr>
            </thead>
            <tbody>
              ${resultsRows}
            </tbody>
          </table>
        </div>

        <!-- Recommendation -->
        <div class="mb-6">
          <h3 class="text-base font-bold text-gray-900 mb-3 border-b-2 border-green-300 pb-2">خامساً: توصية اللجنة</h3>
          <div class="rounded-lg bg-green-50 p-4 border border-green-200">
            <p class="text-sm text-gray-800 leading-relaxed">
              ${passedCount > 0
                ? `بعد فحص وتقييم جميع العروض الفنية المقدمة، توصي اللجنة بتأهيل <strong>${passedCount}</strong> عرض/عروض فنياً من أصل <strong>${store.blindOffers.length}</strong> عرض مقدم، والانتقال إلى مرحلة فتح المظاريف المالية للعروض المؤهلة فنياً.`
                : 'لم يتأهل أي عرض فنياً وفقاً للحد الأدنى المطلوب. توصي اللجنة بإعادة طرح المنافسة.'}
            </p>
          </div>
        </div>

        ${varianceHtml}
        ${aiRecommendation}

        <!-- Signatures -->
        <div class="mt-8">
          <h3 class="text-base font-bold text-gray-900 mb-4 border-b-2 border-gray-400 pb-2">${aiRecommendation ? 'ثامناً' : varianceHtml ? 'سابعاً' : 'سادساً'}: توقيعات أعضاء اللجنة</h3>
          ${signaturesHtml || '<p class="text-sm text-gray-500">لم يتم تحديد أعضاء اللجنة بعد.</p>'}
        </div>

        <!-- Footer -->
        <div class="mt-8 pt-4 border-t-2 border-gray-300 text-center">
          <p class="text-xs text-gray-400">تم إنشاء هذا المحضر آلياً بواسطة منصة Tendex AI بتاريخ ${gregorianDate}</p>
          <p class="text-xs text-gray-400">هذا المحضر سري ولا يجوز تداوله خارج نطاق اللجنة المختصة</p>
        </div>
      </div>`
  } catch (err: any) {
    minutesError.value = err?.message || 'حدث خطأ أثناء توليد المحضر'
  } finally {
    minutesGenerating.value = false
  }
}

/* Submit evaluation */
async function submitEvaluation() {
  submitting.value = true
  submitError.value = ''
  try {
    const { completeTechnicalScoring } = await import('@/services/evaluationApi')
    const evalId = store.technicalEvaluation?.id ?? ''
    await completeTechnicalScoring(competitionId.value, evalId)
    showSubmitDialog.value = false
    router.push({ name: 'EvaluationTechnical' })
  } catch (e: any) {
    submitError.value = e?.message || 'حدث خطأ أثناء تقديم التقييم'
  } finally {
    submitting.value = false
  }
}

/* Watch for tab changes to load AI data */
watch(activeTab, async (tab) => {
  if (tab === 'ai-analysis' && !aiSummary.value) {
    await loadAiSummary()
  }
  if (tab === 'matrix') {
    await store.loadTechnicalHeatmap(competitionId.value)
  }
  if (tab === 'minutes' && !minutesContent.value) {
    await generateMinutes()
  }
})

/* Evaluation not started state */
const needsStart = computed(() => {
  return !store.technicalEvaluation || store.technicalEvaluation.status === 0
})

const isCompleted = computed(() => {
  return store.technicalEvaluation && (store.technicalEvaluation.status >= 2)
})
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Back button -->
    <button
      class="flex items-center gap-2 text-sm text-gray-500 transition-colors hover:text-blue-600"
      @click="router.push({ name: 'EvaluationTechnical' })"
    >
      <i :class="isRtl ? 'pi pi-arrow-right' : 'pi pi-arrow-left'" />
      {{ isRtl ? 'العودة للقائمة' : 'Back to List' }}
    </button>

    <!-- Loading -->
    <div v-if="store.loading && !store.blindOffers.length" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-blue-600" />
      <span class="ms-3 text-sm text-gray-500">{{ isRtl ? 'جارٍ تحميل بيانات التقييم...' : 'Loading evaluation data...' }}</span>
    </div>

    <!-- Evaluation not started - need to start first -->
    <div v-else-if="needsStart" class="rounded-lg border border-gray-200 bg-white py-12 text-center">
      <i class="pi pi-play-circle text-5xl text-blue-500" />
      <h3 class="mt-4 text-lg font-semibold text-gray-700">{{ isRtl ? 'التقييم الفني لم يبدأ بعد' : 'Technical evaluation has not started yet' }}</h3>
      <p class="mt-2 text-sm text-gray-500">{{ isRtl ? 'يجب بدء التقييم الفني أولاً لتتمكن من تقييم العروض' : 'You need to start the technical evaluation first' }}</p>
      <div class="mt-6 flex justify-center gap-3">
        <button
          @click="startEvaluation"
          :disabled="startingEvaluation"
          class="rounded-lg bg-blue-600 px-6 py-3 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          <i class="pi me-2" :class="startingEvaluation ? 'pi-spinner pi-spin' : 'pi-play'" />
          {{ isRtl ? 'بدء التقييم الفني' : 'Start Technical Evaluation' }}
        </button>
        <button
          @click="router.push({ name: 'EvaluationTechnical' })"
          class="rounded-lg border border-gray-300 px-6 py-3 text-sm text-gray-700 hover:bg-gray-50"
        >
          {{ isRtl ? 'العودة' : 'Go Back' }}
        </button>
      </div>
    </div>

    <!-- No offers state -->
    <div v-else-if="!store.loading && store.blindOffers.length === 0 && store.criteria.length === 0" class="rounded-lg border border-gray-200 bg-white py-12 text-center">
      <i class="pi pi-exclamation-triangle text-4xl text-yellow-500" />
      <h3 class="mt-4 text-lg font-semibold text-gray-700">{{ isRtl ? 'لا توجد بيانات للتقييم' : 'No evaluation data available' }}</h3>
      <p class="mt-2 text-sm text-gray-500">{{ isRtl ? 'تأكد من وجود عروض موردين ومعايير تقييم مرتبطة بهذه المنافسة' : 'Make sure there are supplier offers and evaluation criteria for this competition' }}</p>
      <div class="mt-4 flex justify-center gap-3">
        <button
          @click="router.push({ name: 'SupplierOffers' })"
          class="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          <i class="pi pi-plus me-1" /> {{ isRtl ? 'إدارة العروض' : 'Manage Offers' }}
        </button>
        <button
          @click="store.loadTechnicalData(competitionId)"
          class="rounded-lg border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
        >
          <i class="pi pi-refresh me-1" /> {{ isRtl ? 'إعادة المحاولة' : 'Retry' }}
        </button>
      </div>
    </div>

    <template v-else>
      <!-- Header Card -->
      <div class="rounded-lg border border-gray-200 bg-white p-5">
        <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div class="flex-1">
            <div class="mb-2 flex items-center gap-3">
              <h1 class="text-xl font-bold text-gray-900">
                <i class="pi pi-check-square me-2 text-blue-600" />
                {{ isRtl ? 'التقييم الفني' : 'Technical Evaluation' }}
              </h1>
              <span class="rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-800">
                {{ isRtl ? 'تقييم أعمى' : 'Blind Evaluation' }}
              </span>
              <span v-if="isCompleted" class="rounded-full bg-green-100 px-3 py-1 text-xs font-medium text-green-800">
                {{ isRtl ? 'مكتمل' : 'Completed' }}
              </span>
            </div>
            <p class="text-sm text-gray-500">
              {{ store.selectedCompetition?.projectName || store.technicalEvaluation?.competitionId || competitionId }}
            </p>
            <div v-if="store.committee" class="mt-1 text-xs text-gray-400">
              <i class="pi pi-users me-1" />
              {{ isRtl ? 'اللجنة:' : 'Committee:' }} {{ store.committee.name }}
              ({{ store.committee.members.length }} {{ isRtl ? 'أعضاء' : 'members' }})
            </div>
          </div>
          <div class="flex items-center gap-4">
            <div class="text-center">
              <div class="text-2xl font-bold text-blue-600">{{ store.blindOffers.length }}</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'عروض' : 'Offers' }}</div>
            </div>
            <div class="text-center">
              <div class="text-2xl font-bold text-green-600">{{ store.criteria.length }}</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'معايير' : 'Criteria' }}</div>
            </div>
            <div class="text-center">
              <div class="text-2xl font-bold" :class="overallProgress >= 100 ? 'text-green-600' : 'text-yellow-600'">{{ overallProgress }}%</div>
              <div class="text-xs text-gray-400">{{ isRtl ? 'الإنجاز' : 'Progress' }}</div>
            </div>
          </div>
        </div>
        <!-- Progress bar -->
        <div class="mt-4">
          <div class="h-2 w-full overflow-hidden rounded-full bg-gray-100">
            <div
              class="h-full rounded-full transition-all duration-500"
              :class="overallProgress >= 100 ? 'bg-green-500' : 'bg-blue-600'"
              :style="{ width: `${overallProgress}%` }"
            />
          </div>
        </div>
        <!-- Variance alerts banner -->
        <div v-if="store.varianceAlerts.length > 0" class="mt-3 rounded-lg border border-orange-200 bg-orange-50 p-3">
          <div class="flex items-center gap-2 text-sm font-medium text-orange-700">
            <i class="pi pi-exclamation-triangle" />
            {{ isRtl ? `يوجد ${store.varianceAlerts.length} تنبيه تباين يحتاج مراجعة` : `${store.varianceAlerts.length} variance alerts need review` }}
          </div>
        </div>
      </div>

      <!-- Tab navigation -->
      <div class="flex gap-1 rounded-xl border border-gray-200 bg-gray-50 p-1">
        <button
          v-for="tab in (['scoring', 'ai-analysis', 'matrix', 'minutes'] as const)"
          :key="tab"
          class="flex-1 rounded-lg px-4 py-2.5 text-sm font-medium transition-all"
          :class="[
            activeTab === tab
              ? tab === 'ai-analysis'
                ? 'bg-gradient-to-l from-purple-600 to-purple-500 text-white shadow-sm'
                : 'bg-white text-blue-600 shadow-sm'
              : 'text-gray-500 hover:text-gray-700',
          ]"
          @click="activeTab = tab"
        >
          <i
            class="pi me-1.5"
            :class="{
              'pi-pencil': tab === 'scoring',
              'pi-sparkles': tab === 'ai-analysis',
              'pi-th-large': tab === 'matrix',
              'pi-file-edit': tab === 'minutes',
            }"
          />
          {{
            tab === 'scoring' ? (isRtl ? 'التقييم' : 'Scoring') :
            tab === 'ai-analysis' ? (isRtl ? 'تحليل الذكاء الاصطناعي' : 'AI Analysis') :
            tab === 'matrix' ? (isRtl ? 'المصفوفة الحرارية' : 'Heatmap') :
            (isRtl ? 'المحضر' : 'Minutes')
          }}
        </button>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- SCORING TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-if="activeTab === 'scoring'" class="grid grid-cols-1 gap-6 xl:grid-cols-12">
        <!-- Criteria sidebar -->
        <div class="xl:col-span-4">
          <div class="rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              <i class="pi pi-list me-1 text-blue-600" />
              {{ isRtl ? 'معايير التقييم' : 'Evaluation Criteria' }}
            </h3>
            <div class="space-y-2">
              <button
                v-for="criterion in store.criteria"
                :key="criterion.id"
                class="w-full rounded-lg border p-3 text-start transition-all"
                :class="selectedCriterion?.id === criterion.id
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50'"
                @click="onCriterionSelect(criterion)"
              >
                <div class="flex items-center justify-between">
                  <span class="text-sm font-medium text-gray-900">{{ criterion.name }}</span>
                  <span class="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-bold text-blue-700">{{ criterion.weight }}%</span>
                </div>
                <p v-if="criterion.description" class="mt-1 text-xs text-gray-500 line-clamp-2">{{ criterion.description }}</p>
                <!-- Score completion indicator -->
                <div class="mt-2 flex gap-1">
                  <span
                    v-for="offer in store.blindOffers"
                    :key="offer.offerId"
                    class="h-1.5 flex-1 rounded-full"
                    :class="store.technicalScores.some(s => s.supplierOfferId === offer.offerId && s.evaluationCriterionId === criterion.id)
                      ? 'bg-green-400' : 'bg-gray-200'"
                  />
                </div>
              </button>
            </div>

            <!-- Total weight -->
            <div class="mt-3 flex items-center justify-between rounded-lg bg-gray-50 p-3">
              <span class="text-xs text-gray-500">{{ isRtl ? 'إجمالي الأوزان' : 'Total Weight' }}</span>
              <span class="text-sm font-bold" :class="store.totalCriteriaWeight === 100 ? 'text-green-600' : 'text-red-600'">
                {{ store.totalCriteriaWeight }}%
              </span>
            </div>
          </div>

          <!-- Scoring progress -->
          <div class="mt-4 rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-2 text-sm font-bold text-gray-900">
              {{ isRtl ? 'تقدم التقييم' : 'Scoring Progress' }}
            </h3>
            <div class="flex items-center gap-3">
              <div class="flex-1">
                <div class="h-2 w-full overflow-hidden rounded-full bg-gray-100">
                  <div
                    class="h-full rounded-full bg-blue-600 transition-all"
                    :style="{ width: `${(completedCriteria / Math.max(store.criteria.length, 1)) * 100}%` }"
                  />
                </div>
              </div>
              <span class="text-sm font-medium text-gray-700">
                {{ completedCriteria }} / {{ store.criteria.length }}
              </span>
            </div>
          </div>

          <!-- Offer summary -->
          <div class="mt-4 rounded-lg border border-gray-200 bg-white p-4">
            <h3 class="mb-3 text-sm font-bold text-gray-900">
              {{ isRtl ? 'ملخص الدرجات' : 'Score Summary' }}
            </h3>
            <div class="space-y-2">
              <div
                v-for="offer in store.blindOffers"
                :key="offer.offerId"
                class="flex items-center justify-between rounded-lg border border-gray-100 p-2"
              >
                <div class="flex items-center gap-2">
                  <div class="flex h-6 w-6 items-center justify-center rounded-full bg-gray-800 text-[10px] font-bold text-white">
                    {{ offer.blindCode?.slice(-2) || '??' }}
                  </div>
                  <span class="text-sm font-medium text-gray-700">{{ offer.blindCode }}</span>
                </div>
                <div class="flex items-center gap-2">
                  <span class="text-sm font-bold" :class="getScoreColor(offerTotals[offer.offerId] ?? 0)">
                    {{ offerTotals[offer.offerId] ?? 0 }}%
                  </span>
                  <span
                    class="rounded px-1.5 py-0.5 text-[10px] font-medium"
                    :class="(offerTotals[offer.offerId] ?? 0) >= (store.technicalEvaluation?.minimumPassingScore ?? 60)
                      ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'"
                  >
                    {{ (offerTotals[offer.offerId] ?? 0) >= (store.technicalEvaluation?.minimumPassingScore ?? 60)
                      ? (isRtl ? 'ناجح' : 'Pass') : (isRtl ? 'غير ناجح' : 'Fail') }}
                  </span>
                </div>
              </div>
            </div>
            <div class="mt-3 rounded-lg bg-gray-50 p-2 text-center text-xs text-gray-500">
              {{ isRtl ? `درجة النجاح: ${store.technicalEvaluation?.minimumPassingScore ?? 60}%` : `Passing score: ${store.technicalEvaluation?.minimumPassingScore ?? 60}%` }}
            </div>
          </div>
        </div>

        <!-- Scoring area -->
        <div class="xl:col-span-8">
          <div v-if="selectedCriterion" class="space-y-4">
            <!-- Selected criterion header -->
            <div class="rounded-lg border border-blue-200 bg-blue-50 p-4">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-lg font-bold text-gray-900">{{ selectedCriterion.name }}</h2>
                  <p v-if="selectedCriterion.description" class="mt-1 text-sm text-gray-600">
                    {{ selectedCriterion.description }}
                  </p>
                </div>
                <div class="flex items-center gap-3">
                  <span class="rounded-full bg-blue-600 px-3 py-1 text-sm font-bold text-white">{{ selectedCriterion.weight }}%</span>
                </div>
              </div>
            </div>

            <!-- Offer score cards (blind evaluation) -->
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div
                v-for="offer in store.blindOffers"
                :key="offer.offerId"
                class="rounded-lg border p-4 transition-all"
                :class="getScoreBgColor(localScores[offer.offerId]?.score ?? 0)"
              >
                <!-- Offer header (blind code) -->
                <div class="mb-3 flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <div class="flex h-8 w-8 items-center justify-center rounded-full bg-gray-800 text-xs font-bold text-white">
                      {{ offer.blindCode?.slice(-2) || '??' }}
                    </div>
                    <span class="text-sm font-bold text-gray-900">{{ offer.blindCode }}</span>
                  </div>
                  <span class="text-lg font-bold" :class="getScoreColor(localScores[offer.offerId]?.score ?? 0)">
                    {{ localScores[offer.offerId]?.score ?? 0 }} / 100
                  </span>
                </div>

                <!-- Score slider -->
                <div class="mb-3">
                  <input
                    type="range"
                    min="0"
                    max="100"
                    :value="localScores[offer.offerId]?.score ?? 0"
                    @input="updateScore(offer.offerId, Number(($event.target as HTMLInputElement).value))"
                    class="w-full accent-blue-600"
                  />
                  <div class="mt-1 flex justify-between text-xs text-gray-400">
                    <span>0</span>
                    <span>25</span>
                    <span>50</span>
                    <span>75</span>
                    <span>100</span>
                  </div>
                </div>

                <!-- Direct score input -->
                <div class="mb-3">
                  <input
                    type="number"
                    min="0"
                    max="100"
                    :value="localScores[offer.offerId]?.score ?? 0"
                    @input="updateScore(offer.offerId, Number(($event.target as HTMLInputElement).value))"
                    class="w-full rounded-lg border border-gray-300 px-3 py-2 text-center text-sm font-bold focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                    :placeholder="isRtl ? 'الدرجة' : 'Score'"
                  />
                </div>

                <!-- AI suggestion -->
                <div v-if="getAiScore(offer.offerId)" class="mb-3 rounded-lg border border-purple-200 bg-purple-50 p-3">
                  <div class="flex items-center gap-2 text-xs font-medium text-purple-700">
                    <i class="pi pi-sparkles" />
                    {{ isRtl ? 'اقتراح الذكاء الاصطناعي' : 'AI Suggestion' }}
                  </div>
                  <div class="mt-1 text-lg font-bold text-purple-700">
                    {{ getAiScore(offer.offerId)?.suggestedScore }} / {{ getAiScore(offer.offerId)?.maxScore }}
                  </div>
                  <p v-if="getAiScore(offer.offerId)?.justification" class="mt-1 text-xs text-purple-600 line-clamp-3">
                    {{ getAiScore(offer.offerId)?.justification }}
                  </p>
                  <button
                    @click="updateScore(offer.offerId, getAiScore(offer.offerId)?.suggestedScore ?? 0)"
                    class="mt-2 rounded bg-purple-600 px-3 py-1 text-xs font-medium text-white hover:bg-purple-700"
                  >
                    {{ isRtl ? 'تطبيق الاقتراح' : 'Apply Suggestion' }}
                  </button>
                </div>

                <!-- Notes -->
                <textarea
                  :value="localScores[offer.offerId]?.notes ?? ''"
                  @input="updateNotes(offer.offerId, ($event.target as HTMLTextAreaElement).value)"
                  :placeholder="isRtl ? 'ملاحظات التقييم...' : 'Evaluation notes...'"
                  rows="2"
                  class="w-full rounded-lg border border-gray-300 px-3 py-2 text-xs focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                />
              </div>
            </div>

            <!-- Action buttons -->
            <div class="flex items-center justify-between rounded-lg border border-gray-200 bg-white p-4">
              <div class="flex items-center gap-2">
                <span v-if="savingStatus === 'saving'" class="flex items-center gap-1 text-sm text-blue-600">
                  <i class="pi pi-spinner pi-spin" /> {{ isRtl ? 'جارٍ الحفظ...' : 'Saving...' }}
                </span>
                <span v-else-if="savingStatus === 'saved'" class="flex items-center gap-1 text-sm text-green-600">
                  <i class="pi pi-check" /> {{ isRtl ? 'تم الحفظ' : 'Saved' }}
                </span>
                <span v-else-if="savingStatus === 'error'" class="flex items-center gap-1 text-sm text-red-600">
                  <i class="pi pi-times" /> {{ isRtl ? 'خطأ في الحفظ' : 'Save error' }}
                </span>
              </div>
              <div class="flex gap-3">
                <button
                  @click="saveScores"
                  :disabled="savingStatus === 'saving'"
                  class="rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
                >
                  <i class="pi pi-save me-1" /> {{ isRtl ? 'حفظ الدرجات' : 'Save Scores' }}
                </button>
                <button
                  v-if="overallProgress >= 100"
                  @click="showSubmitDialog = true"
                  class="rounded-lg bg-green-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-green-700"
                >
                  <i class="pi pi-send me-1" /> {{ isRtl ? 'تقديم التقييم' : 'Submit Evaluation' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- AI ANALYSIS TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'ai-analysis'" class="space-y-6">
        <!-- AI Header -->
        <div class="rounded-lg border border-purple-200 bg-gradient-to-l from-purple-50 to-indigo-50 p-5">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-bold text-purple-900">
                <i class="pi pi-sparkles me-2" />
                {{ isRtl ? 'تحليل العروض بالذكاء الاصطناعي' : 'AI Offer Analysis' }}
              </h2>
              <p class="mt-1 text-sm text-purple-700">
                {{ isRtl ? 'تحليل شامل لجميع العروض مقابل معايير التقييم ومتطلبات الكراسة' : 'Comprehensive analysis of all offers against evaluation criteria and booklet requirements' }}
              </p>
            </div>
            <button
              @click="runAiAnalysis"
              :disabled="aiLoading"
              class="rounded-lg bg-purple-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-purple-700 disabled:opacity-50"
            >
              <i class="pi me-1" :class="aiLoading ? 'pi-spinner pi-spin' : 'pi-bolt'" />
              {{ aiLoading ? (isRtl ? 'جارٍ التحليل...' : 'Analyzing...') : (isRtl ? 'تشغيل التحليل' : 'Run Analysis') }}
            </button>
          </div>
          <div class="mt-3 rounded-lg bg-white/50 p-3 text-xs text-purple-600">
            <i class="pi pi-info-circle me-1" />
            {{ isRtl ? 'نتائج الذكاء الاصطناعي استشارية فقط ولا تحل محل تقييم اللجنة. يجب مراجعة كل تحليل واعتماده من قبل عضو اللجنة.' : 'AI results are advisory only and do not replace committee evaluation. Each analysis must be reviewed and approved by a committee member.' }}
          </div>
        </div>

        <!-- AI Error -->
        <div v-if="aiError" class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <i class="pi pi-exclamation-triangle me-1" /> {{ aiError }}
        </div>

        <!-- AI Summary -->
        <div v-if="aiSummary" class="space-y-4">
          <!-- Summary stats -->
          <div class="grid grid-cols-2 gap-4 md:grid-cols-5">
            <div class="rounded-lg border border-gray-200 bg-white p-4 text-center">
              <div class="text-2xl font-bold text-blue-600">{{ aiSummary.totalOffers }}</div>
              <div class="text-xs text-gray-500">{{ isRtl ? 'إجمالي العروض' : 'Total Offers' }}</div>
            </div>
            <div class="rounded-lg border border-gray-200 bg-white p-4 text-center">
              <div class="text-2xl font-bold text-green-600">{{ aiSummary.completedAnalyses }}</div>
              <div class="text-xs text-gray-500">{{ isRtl ? 'تحليلات مكتملة' : 'Completed' }}</div>
            </div>
            <div class="rounded-lg border border-gray-200 bg-white p-4 text-center">
              <div class="text-2xl font-bold text-red-600">{{ aiSummary.failedAnalyses }}</div>
              <div class="text-xs text-gray-500">{{ isRtl ? 'تحليلات فاشلة' : 'Failed' }}</div>
            </div>
            <div class="rounded-lg border border-gray-200 bg-white p-4 text-center">
              <div class="text-2xl font-bold text-orange-600">{{ aiSummary.pendingReviews }}</div>
              <div class="text-xs text-gray-500">{{ isRtl ? 'بانتظار المراجعة' : 'Pending Review' }}</div>
            </div>
            <div class="rounded-lg border border-gray-200 bg-white p-4 text-center">
              <div class="text-2xl font-bold text-purple-600">
                {{ aiSummary.offerSummaries.filter(o => o.isHumanReviewed).length }}
              </div>
              <div class="text-xs text-gray-500">{{ isRtl ? 'تمت المراجعة' : 'Reviewed' }}</div>
            </div>
          </div>

          <!-- Offer analysis cards -->
          <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            <div
              v-for="offerSummary in aiSummary.offerSummaries"
              :key="offerSummary.supplierOfferId"
              class="cursor-pointer rounded-lg border border-gray-200 bg-white p-4 transition-all hover:border-purple-300 hover:shadow-md"
              :class="selectedAiOfferId === offerSummary.analysisId ? 'border-purple-500 ring-2 ring-purple-200' : ''"
              @click="loadAiOfferDetail(offerSummary.analysisId)"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <div class="flex h-8 w-8 items-center justify-center rounded-full bg-purple-100 text-xs font-bold text-purple-700">
                    {{ offerSummary.blindCode?.slice(-2) || '??' }}
                  </div>
                  <span class="text-sm font-bold text-gray-900">{{ offerSummary.blindCode }}</span>
                </div>
                <span
                  class="rounded-full px-2 py-0.5 text-xs font-medium"
                  :class="offerSummary.isHumanReviewed ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'"
                >
                  {{ offerSummary.isHumanReviewed ? (isRtl ? 'تمت المراجعة' : 'Reviewed') : (isRtl ? 'بانتظار المراجعة' : 'Pending') }}
                </span>
              </div>
              <div class="mt-3">
                <div class="text-2xl font-bold" :class="getScoreColor(offerSummary.overallComplianceScore)">
                  {{ offerSummary.overallComplianceScore }}%
                </div>
                <div class="mt-2 flex gap-1 text-xs">
                  <span class="rounded bg-green-100 px-1.5 py-0.5 text-green-700">{{ offerSummary.fullyCompliantCount }} {{ isRtl ? 'متوافق' : 'compliant' }}</span>
                  <span class="rounded bg-yellow-100 px-1.5 py-0.5 text-yellow-700">{{ offerSummary.partiallyCompliantCount }} {{ isRtl ? 'جزئي' : 'partial' }}</span>
                  <span class="rounded bg-red-100 px-1.5 py-0.5 text-red-700">{{ offerSummary.nonCompliantCount }} {{ isRtl ? 'غير متوافق' : 'non-compliant' }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Selected offer detail -->
          <div v-if="selectedAiOfferId && aiOfferDetails[selectedAiOfferId]" class="rounded-lg border border-purple-200 bg-white p-5">
            <div class="mb-4 flex items-center justify-between">
              <h3 class="text-lg font-bold text-gray-900">
                <i class="pi pi-sparkles me-2 text-purple-600" />
                {{ isRtl ? 'تفاصيل تحليل العرض' : 'Offer Analysis Details' }}
                - {{ aiOfferDetails[selectedAiOfferId].blindCode }}
              </h3>
              <span
                class="rounded-full px-3 py-1 text-xs font-medium"
                :class="aiOfferDetails[selectedAiOfferId].isHumanReviewed ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'"
              >
                {{ aiOfferDetails[selectedAiOfferId].isHumanReviewed ? (isRtl ? 'تمت المراجعة' : 'Reviewed') : (isRtl ? 'بانتظار المراجعة' : 'Pending Review') }}
              </span>
            </div>

            <!-- Executive Summary -->
            <div class="mb-4 rounded-lg bg-gray-50 p-4">
              <h4 class="mb-2 text-sm font-bold text-gray-700">{{ isRtl ? 'الملخص التنفيذي' : 'Executive Summary' }}</h4>
              <p class="text-sm text-gray-600">{{ aiOfferDetails[selectedAiOfferId].executiveSummary }}</p>
            </div>

            <!-- Analysis sections -->
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div class="rounded-lg border border-green-200 bg-green-50 p-4">
                <h4 class="mb-2 text-sm font-bold text-green-700">
                  <i class="pi pi-check-circle me-1" /> {{ isRtl ? 'نقاط القوة' : 'Strengths' }}
                </h4>
                <p class="text-xs text-green-600 whitespace-pre-line">{{ aiOfferDetails[selectedAiOfferId].strengthsAnalysis }}</p>
              </div>
              <div class="rounded-lg border border-red-200 bg-red-50 p-4">
                <h4 class="mb-2 text-sm font-bold text-red-700">
                  <i class="pi pi-times-circle me-1" /> {{ isRtl ? 'نقاط الضعف' : 'Weaknesses' }}
                </h4>
                <p class="text-xs text-red-600 whitespace-pre-line">{{ aiOfferDetails[selectedAiOfferId].weaknessesAnalysis }}</p>
              </div>
              <div class="rounded-lg border border-orange-200 bg-orange-50 p-4">
                <h4 class="mb-2 text-sm font-bold text-orange-700">
                  <i class="pi pi-exclamation-triangle me-1" /> {{ isRtl ? 'المخاطر' : 'Risks' }}
                </h4>
                <p class="text-xs text-orange-600 whitespace-pre-line">{{ aiOfferDetails[selectedAiOfferId].risksAnalysis }}</p>
              </div>
              <div class="rounded-lg border border-blue-200 bg-blue-50 p-4">
                <h4 class="mb-2 text-sm font-bold text-blue-700">
                  <i class="pi pi-shield me-1" /> {{ isRtl ? 'تقييم الامتثال' : 'Compliance Assessment' }}
                </h4>
                <p class="text-xs text-blue-600 whitespace-pre-line">{{ aiOfferDetails[selectedAiOfferId].complianceAssessment }}</p>
              </div>
            </div>

            <!-- Overall recommendation -->
            <div class="mt-4 rounded-lg border border-purple-200 bg-purple-50 p-4">
              <h4 class="mb-2 text-sm font-bold text-purple-700">
                <i class="pi pi-star me-1" /> {{ isRtl ? 'التوصية العامة' : 'Overall Recommendation' }}
              </h4>
              <p class="text-sm text-purple-600">{{ aiOfferDetails[selectedAiOfferId].overallRecommendation }}</p>
            </div>

            <!-- Criterion analyses -->
            <div v-if="aiOfferDetails[selectedAiOfferId].criterionAnalyses?.length" class="mt-4">
              <h4 class="mb-3 text-sm font-bold text-gray-700">{{ isRtl ? 'تحليل المعايير' : 'Criteria Analysis' }}</h4>
              <div class="space-y-3">
                <div
                  v-for="ca in aiOfferDetails[selectedAiOfferId].criterionAnalyses"
                  :key="ca.id"
                  class="rounded-lg border border-gray-200 p-3"
                >
                  <div class="flex items-center justify-between">
                    <span class="text-sm font-medium text-gray-900">{{ ca.criterionNameAr }}</span>
                    <div class="flex items-center gap-2">
                      <span class="text-sm font-bold" :class="getScoreColor(ca.scorePercentage)">
                        {{ ca.suggestedScore }} / {{ ca.maxScore }}
                      </span>
                      <span
                        class="rounded px-2 py-0.5 text-xs font-medium"
                        :class="{
                          'bg-green-100 text-green-700': ca.complianceLevel === 'FullyCompliant',
                          'bg-yellow-100 text-yellow-700': ca.complianceLevel === 'PartiallyCompliant',
                          'bg-red-100 text-red-700': ca.complianceLevel === 'NonCompliant',
                          'bg-gray-100 text-gray-700': ca.complianceLevel === 'RequiresReview',
                        }"
                      >
                        {{ ca.complianceLevel === 'FullyCompliant' ? (isRtl ? 'متوافق' : 'Compliant') :
                           ca.complianceLevel === 'PartiallyCompliant' ? (isRtl ? 'جزئي' : 'Partial') :
                           ca.complianceLevel === 'NonCompliant' ? (isRtl ? 'غير متوافق' : 'Non-compliant') :
                           (isRtl ? 'يحتاج مراجعة' : 'Needs Review') }}
                      </span>
                    </div>
                  </div>
                  <p class="mt-2 text-xs text-gray-600">{{ ca.detailedJustification }}</p>
                  <p v-if="ca.complianceNotes" class="mt-1 text-xs text-gray-500 italic">{{ ca.complianceNotes }}</p>
                </div>
              </div>
            </div>

            <!-- Human review section -->
            <div v-if="!aiOfferDetails[selectedAiOfferId].isHumanReviewed" class="mt-4 rounded-lg border border-yellow-200 bg-yellow-50 p-4">
              <h4 class="mb-2 text-sm font-bold text-yellow-700">
                <i class="pi pi-user-edit me-1" /> {{ isRtl ? 'مراجعة عضو اللجنة' : 'Committee Member Review' }}
              </h4>
              <textarea
                v-model="reviewNotes"
                :placeholder="isRtl ? 'أضف ملاحظات المراجعة...' : 'Add review notes...'"
                rows="3"
                class="mb-3 w-full rounded-lg border border-yellow-300 px-3 py-2 text-sm focus:border-yellow-500 focus:outline-none focus:ring-1 focus:ring-yellow-500"
              />
              <button
                @click="submitAiReview(selectedAiOfferId!)"
                class="rounded-lg bg-yellow-600 px-4 py-2 text-sm font-medium text-white hover:bg-yellow-700"
              >
                <i class="pi pi-check me-1" /> {{ isRtl ? 'اعتماد المراجعة' : 'Approve Review' }}
              </button>
            </div>

            <!-- AI metadata -->
            <div class="mt-4 flex items-center gap-4 text-xs text-gray-400">
              <span><i class="pi pi-cpu me-1" /> {{ aiOfferDetails[selectedAiOfferId].aiModelUsed }}</span>
              <span><i class="pi pi-clock me-1" /> {{ aiOfferDetails[selectedAiOfferId].analysisLatencyMs }}ms</span>
              <span><i class="pi pi-calendar me-1" /> {{ new Date(aiOfferDetails[selectedAiOfferId].createdAt).toLocaleDateString('ar-SA') }}</span>
            </div>
          </div>
        </div>

        <!-- No AI data yet -->
        <div v-else-if="!aiLoading" class="rounded-lg border border-gray-200 bg-white py-12 text-center">
          <i class="pi pi-sparkles text-4xl text-purple-400" />
          <h3 class="mt-4 text-lg font-semibold text-gray-700">{{ isRtl ? 'لم يتم تشغيل التحليل بعد' : 'Analysis not run yet' }}</h3>
          <p class="mt-2 text-sm text-gray-500">{{ isRtl ? 'اضغط على "تشغيل التحليل" لبدء تحليل العروض بالذكاء الاصطناعي' : 'Click "Run Analysis" to start AI-powered offer analysis' }}</p>
        </div>
      </div>

      <!-- ═══════════════════════════════════════════════ -->
      <!-- HEATMAP MATRIX TAB -->
      <!-- ═══════════════════════════════════════════════ -->
      <div v-else-if="activeTab === 'matrix'" class="space-y-4">
        <div class="rounded-lg border border-gray-200 bg-white p-5">
          <h2 class="mb-4 text-lg font-bold text-gray-900">
            <i class="pi pi-th-large me-2 text-blue-600" />
            {{ isRtl ? 'المصفوفة الحرارية للتقييم الفني' : 'Technical Evaluation Heatmap' }}
          </h2>
          
          <div v-if="heatmapData.cells.length > 0" class="overflow-x-auto">
            <table class="min-w-full border-collapse text-sm">
              <thead>
                <tr class="bg-gray-50">
                  <th class="border border-gray-200 px-3 py-2 text-start text-xs font-bold text-gray-700">
                    {{ isRtl ? 'المعيار' : 'Criterion' }}
                  </th>
                  <th class="border border-gray-200 px-2 py-2 text-center text-xs font-bold text-gray-500">
                    {{ isRtl ? 'الوزن' : 'Weight' }}
                  </th>
                  <th
                    v-for="code in heatmapData.offerBlindCodes"
                    :key="code"
                    class="border border-gray-200 px-3 py-2 text-center text-xs font-bold text-gray-700"
                  >
                    {{ code }}
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="criterion in heatmapData.criteria" :key="criterion.id">
                  <td class="border border-gray-200 px-3 py-2 text-xs font-medium text-gray-900">
                    {{ isRtl ? criterion.nameAr : criterion.nameEn }}
                  </td>
                  <td class="border border-gray-200 px-2 py-2 text-center text-xs text-gray-500">
                    {{ criterion.weightPercentage }}%
                  </td>
                  <td
                    v-for="code in heatmapData.offerBlindCodes"
                    :key="code"
                    class="border border-gray-200 px-3 py-2 text-center text-sm font-bold"
                    :class="getHeatmapCellColor(getHeatmapCell(code, criterion.id)?.averageScorePercentage ?? 0)"
                  >
                    {{ Math.round(getHeatmapCell(code, criterion.id)?.averageScorePercentage ?? 0) }}%
                  </td>
                </tr>
              </tbody>
              <!-- Totals row -->
              <tfoot>
                <tr class="bg-gray-100 font-bold">
                  <td class="border border-gray-200 px-3 py-2 text-xs text-gray-900">
                    {{ isRtl ? 'المجموع المرجح' : 'Weighted Total' }}
                  </td>
                  <td class="border border-gray-200 px-2 py-2 text-center text-xs text-gray-500">100%</td>
                  <td
                    v-for="code in heatmapData.offerBlindCodes"
                    :key="code"
                    class="border border-gray-200 px-3 py-2 text-center text-sm"
                    :class="getScoreColor(offerTotals[store.blindOffers.find(o => o.blindCode === code)?.offerId ?? ''] ?? 0)"
                  >
                    {{ offerTotals[store.blindOffers.find(o => o.blindCode === code)?.offerId ?? ''] ?? 0 }}%
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
          <div v-else class="py-8 text-center text-sm text-gray-500">
            {{ isRtl ? 'لا توجد بيانات كافية لعرض المصفوفة الحرارية' : 'Not enough data to display heatmap' }}
          </div>

          <!-- Legend -->
          <div class="mt-4 flex items-center gap-4 text-xs text-gray-500">
            <span class="flex items-center gap-1"><span class="inline-block h-3 w-3 rounded bg-green-100" /> {{ isRtl ? 'ممتاز (80%+)' : 'Excellent (80%+)' }}</span>
            <span class="flex items-center gap-1"><span class="inline-block h-3 w-3 rounded bg-yellow-100" /> {{ isRtl ? 'متوسط (60-79%)' : 'Average (60-79%)' }}</span>
            <span class="flex items-center gap-1"><span class="inline-block h-3 w-3 rounded bg-red-100" /> {{ isRtl ? 'ضعيف (<60%)' : 'Weak (<60%)' }}</span>
          </div>
        </div>

        <!-- Variance alerts detail -->
        <div v-if="store.varianceAlerts.length > 0" class="rounded-lg border border-orange-200 bg-white p-5">
          <h3 class="mb-3 text-sm font-bold text-orange-700">
            <i class="pi pi-exclamation-triangle me-1" />
            {{ isRtl ? 'تنبيهات التباين' : 'Variance Alerts' }}
          </h3>
          <div class="space-y-2">
            <div
              v-for="alert in store.varianceAlerts"
              :key="`${alert.criterionId}-${alert.offerId}`"
              class="rounded-lg border border-orange-100 bg-orange-50 p-3"
            >
              <div class="flex items-center justify-between">
                <span class="text-sm font-medium text-gray-900">{{ alert.criterionNameAr }} - {{ alert.offerBlindCode }}</span>
                <div class="flex gap-2">
                  <span v-if="alert.hasEvaluatorVariance" class="rounded bg-orange-200 px-2 py-0.5 text-xs text-orange-800">
                    {{ isRtl ? `تباين مقيمين: ${alert.evaluatorSpread}%` : `Evaluator spread: ${alert.evaluatorSpread}%` }}
                  </span>
                  <span v-if="alert.hasHumanAiVariance" class="rounded bg-purple-200 px-2 py-0.5 text-xs text-purple-800">
                    {{ isRtl ? `فرق AI: ${alert.humanAiDifference}%` : `AI diff: ${alert.humanAiDifference}%` }}
                  </span>
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
          <div class="mb-4 flex items-center justify-between">
            <h2 class="text-lg font-bold text-gray-900">
              <i class="pi pi-file-edit me-2 text-blue-600" />
              {{ isRtl ? 'محضر التقييم الفني' : 'Technical Evaluation Minutes' }}
            </h2>
            <div class="flex gap-2">
              <button
                @click="generateMinutes"
                :disabled="minutesGenerating"
                class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
              >
                <i class="pi me-1" :class="minutesGenerating ? 'pi-spinner pi-spin' : 'pi-refresh'" />
                {{ isRtl ? 'إعادة التوليد' : 'Regenerate' }}
              </button>
              <button
                v-if="minutesContent"
                @click="printMinutes"
                class="rounded-lg border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
              >
                <i class="pi pi-print me-1" /> {{ isRtl ? 'طباعة' : 'Print' }}
              </button>
            </div>
          </div>
          <div v-if="minutesError" class="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700 border border-red-200">
            <i class="pi pi-exclamation-triangle me-1" /> {{ minutesError }}
          </div>
          <div v-if="minutesGenerating" class="flex flex-col items-center justify-center py-12">
            <i class="pi pi-spinner pi-spin text-3xl text-blue-600" />
            <span class="mt-3 text-sm text-gray-500">{{ isRtl ? 'جارٍ توليد المحضر...' : 'Generating minutes...' }}</span>
          </div>
          <div v-else-if="minutesContent" class="rounded-lg border border-gray-200 bg-white p-6 print:p-0 print:border-0" dir="rtl" v-html="minutesContent"></div>
          <div v-else class="py-12 text-center text-sm text-gray-500">
            <i class="pi pi-file-edit text-4xl text-gray-300 mb-3" />
            <p>{{ isRtl ? 'لا يوجد محضر بعد. سيتم توليده تلقائياً.' : 'No minutes yet. Will be generated automatically.' }}</p>
          </div>
        </div>
      </div>
    </template>

    <!-- Submit confirmation dialog -->
    <div v-if="showSubmitDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div class="mx-4 w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h3 class="text-lg font-bold text-gray-900">{{ isRtl ? 'تأكيد تقديم التقييم' : 'Confirm Submission' }}</h3>
        <p class="mt-2 text-sm text-gray-600">
          {{ isRtl ? 'هل أنت متأكد من تقديم التقييم الفني؟ لن تتمكن من تعديل الدرجات بعد التقديم.' : 'Are you sure you want to submit the technical evaluation? You will not be able to modify scores after submission.' }}
        </p>
        <div v-if="submitError" class="mt-3 rounded-lg bg-red-50 p-3 text-sm text-red-700">{{ submitError }}</div>
        <div class="mt-4 flex justify-end gap-3">
          <button
            @click="showSubmitDialog = false"
            class="rounded-lg border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
          >
            {{ isRtl ? 'إلغاء' : 'Cancel' }}
          </button>
          <button
            @click="submitEvaluation"
            :disabled="submitting"
            class="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
          >
            <i class="pi me-1" :class="submitting ? 'pi-spinner pi-spin' : 'pi-check'" />
            {{ isRtl ? 'تأكيد التقديم' : 'Confirm Submit' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
