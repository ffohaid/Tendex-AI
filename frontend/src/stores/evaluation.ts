/**
 * Evaluation Pinia store.
 * Manages state for technical and financial evaluation workflows.
 * All data is fetched from APIs — no hardcoded/mock data.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  CompetitionEvaluation,
  Committee,
  EvaluationCriterion,
  SupplierOffer,
  TechnicalEvaluationDetail,
  BlindOfferSummary,
  TechnicalScore,
  AiTechnicalScore,
  VarianceAlert,
  TechnicalHeatmap,
  OfferEvaluationResult,
  FinancialEvaluationDetail,
  FinancialOfferItem,
  FinancialComparisonMatrix,
  ArithmeticVerificationResult,
  FinancialScore,
  AiEvaluation,
} from '@/types/evaluation'
import * as api from '@/services/evaluationApi'

export const useEvaluationStore = defineStore('evaluation', () => {
  /* ── State ─────────────────────────────────── */
  const competitions = ref<CompetitionEvaluation[]>([])
  const selectedCompetition = ref<CompetitionEvaluation | null>(null)
  const committee = ref<Committee | null>(null)
  const criteria = ref<EvaluationCriterion[]>([])
  const supplierOffers = ref<SupplierOffer[]>([])

  // Technical
  const technicalEvaluation = ref<TechnicalEvaluationDetail | null>(null)
  const blindOffers = ref<BlindOfferSummary[]>([])
  const technicalScores = ref<TechnicalScore[]>([])
  const aiTechnicalScores = ref<AiTechnicalScore[]>([])
  const varianceAlerts = ref<VarianceAlert[]>([])
  const technicalHeatmap = ref<TechnicalHeatmap | null>(null)
  const technicalResults = ref<OfferEvaluationResult[]>([])

  // Financial
  const financialEvaluation = ref<FinancialEvaluationDetail | null>(null)
  const financialOfferItems = ref<Map<string, FinancialOfferItem[]>>(new Map())
  const financialScores = ref<FinancialScore[]>([])
  const financialComparisonMatrix = ref<FinancialComparisonMatrix | null>(null)
  const arithmeticResults = ref<Map<string, ArithmeticVerificationResult>>(new Map())

  // AI
  const aiEvaluations = ref<AiEvaluation[]>([])

  // UI
  const loading = ref(false)
  const error = ref<string | null>(null)

  /* ── Getters ───────────────────────────────── */
  const totalCriteriaWeight = computed(() =>
    criteria.value.reduce((sum, c) => sum + c.weight, 0)
  )

  const passedOffers = computed(() =>
    blindOffers.value.filter(o => o.technicalResult === 1)
  )

  const pendingScoresCount = computed(() => {
    const totalNeeded = criteria.value.length * blindOffers.value.length
    return totalNeeded - technicalScores.value.length
  })

  /* ── Actions ───────────────────────────────── */

  async function loadCompetitions() {
    loading.value = true
    error.value = null
    try {
      competitions.value = await api.fetchCompetitionEvaluations()
    } catch {
      error.value = 'لا توجد منافسات متاحة للتقييم حالياً'
    } finally {
      loading.value = false
    }
  }

  async function selectCompetition(id: string) {
    loading.value = true
    error.value = null
    try {
      selectedCompetition.value = await api.fetchCompetitionEvaluation(id)
    } catch {
      error.value = 'تعذر تحميل بيانات المنافسة'
    } finally {
      loading.value = false
    }
  }

  async function loadSupplierOffers(competitionId: string) {
    loading.value = true
    error.value = null
    try {
      supplierOffers.value = await api.fetchSupplierOffers(competitionId)
    } catch {
      supplierOffers.value = []
    } finally {
      loading.value = false
    }
  }

  async function loadTechnicalData(competitionId: string) {
    loading.value = true
    error.value = null
    try {
      const results = await Promise.allSettled([
        api.fetchTechnicalEvaluationDetails(competitionId),
        api.fetchBlindOffers(competitionId),
        api.fetchCriteria(competitionId),
        api.fetchCommittees(competitionId),
        api.fetchTechnicalScores(competitionId),
        api.fetchVarianceAlerts(competitionId),
        api.fetchTechnicalHeatmap(competitionId),
        api.fetchTechnicalResults(competitionId),
      ])

      if (results[0].status === 'fulfilled') {
        technicalEvaluation.value = results[0].value as TechnicalEvaluationDetail
      }
      if (results[1].status === 'fulfilled') {
        blindOffers.value = Array.isArray(results[1].value) ? results[1].value as BlindOfferSummary[] : []
      }
      if (results[2].status === 'fulfilled') {
        criteria.value = Array.isArray(results[2].value) ? results[2].value as EvaluationCriterion[] : []
      }
      if (results[3].status === 'fulfilled') {
        const committees = results[3].value as Committee[]
        if (Array.isArray(committees) && committees.length > 0) {
          committee.value = committees[0]
        }
      }
      if (results[4].status === 'fulfilled') {
        technicalScores.value = Array.isArray(results[4].value) ? results[4].value as TechnicalScore[] : []
      }
      if (results[5].status === 'fulfilled') {
        varianceAlerts.value = Array.isArray(results[5].value) ? results[5].value as VarianceAlert[] : []
      }
      if (results[6].status === 'fulfilled') {
        technicalHeatmap.value = results[6].value as TechnicalHeatmap
      }
      if (results[7].status === 'fulfilled') {
        technicalResults.value = Array.isArray(results[7].value) ? results[7].value as OfferEvaluationResult[] : []
      }
    } catch {
      error.value = 'تعذر تحميل بيانات التقييم الفني'
    } finally {
      loading.value = false
    }
  }

  async function loadFinancialData(competitionId: string) {
    loading.value = true
    error.value = null
    try {
      const results = await Promise.allSettled([
        api.fetchFinancialEvaluationDetails(competitionId),
        api.fetchBlindOffers(competitionId),
        api.fetchSupplierOffers(competitionId),
        api.fetchCommittees(competitionId),
        api.fetchCriteria(competitionId),
        api.fetchFinancialScores(competitionId),
        api.fetchFinancialComparisonMatrix(competitionId),
      ])

      if (results[0].status === 'fulfilled') {
        financialEvaluation.value = results[0].value as FinancialEvaluationDetail
      }
      if (results[1].status === 'fulfilled') {
        blindOffers.value = Array.isArray(results[1].value) ? results[1].value as BlindOfferSummary[] : []
      }
      if (results[2].status === 'fulfilled') {
        supplierOffers.value = Array.isArray(results[2].value) ? results[2].value as SupplierOffer[] : []
      }
      if (results[3].status === 'fulfilled') {
        const committees = results[3].value as Committee[]
        if (Array.isArray(committees) && committees.length > 0) {
          committee.value = committees[0]
        }
      }
      if (results[4].status === 'fulfilled') {
        criteria.value = Array.isArray(results[4].value) ? results[4].value as EvaluationCriterion[] : []
      }
      if (results[5].status === 'fulfilled') {
        financialScores.value = Array.isArray(results[5].value) ? results[5].value as FinancialScore[] : []
      }
      if (results[6].status === 'fulfilled') {
        financialComparisonMatrix.value = results[6].value as FinancialComparisonMatrix
      }
    } catch {
      error.value = 'تعذر تحميل بيانات التقييم المالي'
    } finally {
      loading.value = false
    }
  }

  async function submitTechScore(
    competitionId: string,
    supplierOfferId: string,
    criterionId: string,
    score: number,
    notes?: string
  ) {
    try {
      const result = await api.submitTechnicalScore(competitionId, {
        evaluationId: technicalEvaluation.value?.id ?? '',
        supplierOfferId,
        evaluationCriterionId: criterionId,
        score,
        notes,
      })
      const idx = technicalScores.value.findIndex(
        s => s.supplierOfferId === supplierOfferId && s.evaluationCriterionId === criterionId
      )
      if (idx >= 0) {
        technicalScores.value[idx] = result
      } else {
        technicalScores.value.push(result)
      }
      return result
    } catch {
      error.value = 'تعذر حفظ الدرجة'
      throw new Error('تعذر حفظ الدرجة')
    }
  }

  async function submitFinScore(
    competitionId: string,
    supplierOfferId: string,
    score: number,
    maxScore: number,
    notes?: string
  ) {
    try {
      const result = await api.submitFinancialScore(competitionId, supplierOfferId, {
        score,
        maxScore,
        notes,
      })
      const idx = financialScores.value.findIndex(s => s.supplierOfferId === supplierOfferId)
      if (idx >= 0) {
        financialScores.value[idx] = result
      } else {
        financialScores.value.push(result)
      }
      return result
    } catch {
      error.value = 'تعذر حفظ الدرجة المالية'
      throw new Error('تعذر حفظ الدرجة المالية')
    }
  }

  async function loadFinancialOfferItems(competitionId: string, supplierOfferId: string) {
    try {
      const items = await api.fetchFinancialOfferItems(competitionId, supplierOfferId)
      financialOfferItems.value.set(supplierOfferId, items)
      return items
    } catch {
      return []
    }
  }

  async function verifyArithmetic(competitionId: string, supplierOfferId: string) {
    try {
      const result = await api.verifyArithmetic(competitionId, supplierOfferId)
      arithmeticResults.value.set(supplierOfferId, result)
      return result
    } catch {
      error.value = 'تعذر التحقق الحسابي'
      throw new Error('تعذر التحقق الحسابي')
    }
  }

  async function triggerAiAnalysis(competitionId: string) {
    loading.value = true
    try {
      await api.triggerAiAnalysis(competitionId)
      const results = await api.fetchAiAnalysisSummary(competitionId)
      aiEvaluations.value = Array.isArray(results) ? results : []
    } catch {
      error.value = 'تعذر تشغيل تحليل الذكاء الاصطناعي'
    } finally {
      loading.value = false
    }
  }

  async function loadTechnicalHeatmap(competitionId: string) {
    try {
      technicalHeatmap.value = await api.fetchTechnicalHeatmap(competitionId)
    } catch {
      // Heatmap may not be available yet
    }
  }

  async function loadTechnicalResults(competitionId: string) {
    try {
      technicalResults.value = await api.fetchTechnicalResults(competitionId)
    } catch {
      technicalResults.value = []
    }
  }

  /* ── Reset ─────────────────────────────────── */

  function $reset() {
    competitions.value = []
    selectedCompetition.value = null
    committee.value = null
    criteria.value = []
    supplierOffers.value = []
    technicalEvaluation.value = null
    blindOffers.value = []
    technicalScores.value = []
    aiTechnicalScores.value = []
    varianceAlerts.value = []
    technicalHeatmap.value = null
    technicalResults.value = []
    financialEvaluation.value = null
    financialOfferItems.value = new Map()
    financialScores.value = []
    financialComparisonMatrix.value = null
    arithmeticResults.value = new Map()
    aiEvaluations.value = []
    loading.value = false
    error.value = null
  }

  /* ── Start Evaluation Actions ── */
  async function startTechnicalEvaluation(competitionId: string, committeeId?: string) {
    loading.value = true
    error.value = null
    try {
      const result = await api.startTechnicalEvaluation(competitionId, committeeId || '')
      technicalEvaluation.value = result
      await loadCompetitions()
      return result
    } catch (e: any) {
      error.value = e?.response?.data?.message || e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function startFinancialEvaluation(competitionId: string, committeeId?: string) {
    loading.value = true
    error.value = null
    try {
      const result = await api.startFinancialEvaluation(competitionId, committeeId || '')
      financialEvaluation.value = result
      await loadCompetitions()
      return result
    } catch (e: any) {
      error.value = e?.response?.data?.message || e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    /* State */
    competitions,
    selectedCompetition,
    committee,
    criteria,
    supplierOffers,
    technicalEvaluation,
    blindOffers,
    technicalScores,
    aiTechnicalScores,
    varianceAlerts,
    technicalHeatmap,
    technicalResults,
    financialEvaluation,
    financialOfferItems,
    financialScores,
    financialComparisonMatrix,
    arithmeticResults,
    aiEvaluations,
    loading,
    error,

    /* Getters */
    totalCriteriaWeight,
    passedOffers,
    pendingScoresCount,

    /* Actions */
    loadCompetitions,
    selectCompetition,
    loadSupplierOffers,
    loadTechnicalData,
    loadFinancialData,
    submitTechScore,
    submitFinScore,
    loadFinancialOfferItems,
    verifyArithmetic,
    triggerAiAnalysis,
    loadTechnicalHeatmap,
    loadTechnicalResults,
    startTechnicalEvaluation,
    startFinancialEvaluation,
    $reset,
  }
})
