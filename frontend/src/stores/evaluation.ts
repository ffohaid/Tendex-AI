/**
 * Evaluation Pinia store.
 * Manages state for technical and financial evaluation workflows.
 * All data is fetched from APIs — no hardcoded/mock data.
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  HeatmapColor,
} from '@/types/evaluation'
import type {
  CompetitionEvaluation,
  Committee,
  EvaluationCriterion,
  Vendor,
  TechnicalScore,
  FinancialScore,
  FinancialOffer,
  AiEvaluation,
  VarianceAlert,
  ComparisonMatrix,
  HeatmapCell,
} from '@/types/evaluation'
import * as api from '@/services/evaluationApi'

export const useEvaluationStore = defineStore('evaluation', () => {
  /* ── State ─────────────────────────────────── */
  const competitions = ref<CompetitionEvaluation[]>([])
  const selectedCompetition = ref<CompetitionEvaluation | null>(null)
  const committee = ref<Committee | null>(null)
  const criteria = ref<EvaluationCriterion[]>([])
  const vendors = ref<Vendor[]>([])
  const technicalScores = ref<TechnicalScore[]>([])
  const financialScores = ref<FinancialScore[]>([])
  const financialOffers = ref<FinancialOffer[]>([])
  const aiEvaluations = ref<AiEvaluation[]>([])
  const varianceAlerts = ref<VarianceAlert[]>([])
  const comparisonMatrix = ref<ComparisonMatrix | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const autoSaveTimer = ref<ReturnType<typeof setInterval> | null>(null)

  /* ── Getters ───────────────────────────────── */
  const totalCriteriaWeight = computed(() =>
    criteria.value.reduce((sum, c) => sum + c.weight, 0)
  )

  const passedVendors = computed(() =>
    vendors.value.filter(v => v.technicalStatus === 'passed')
  )

  const pendingScoresCount = computed(() => {
    const totalNeeded = criteria.value.length * vendors.value.length
    return totalNeeded - technicalScores.value.length
  })

  /* ── Heatmap Helpers ───────────────────────── */
  function getHeatmapColor(percentage: number): HeatmapColor {
    if (percentage >= 80) return HeatmapColor.Excellent
    if (percentage >= 60) return HeatmapColor.Average
    return HeatmapColor.Weak
  }

  function buildHeatmapCells(): HeatmapCell[][] {
    return criteria.value.map(criterion => {
      return vendors.value.map(vendor => {
        const scores = technicalScores.value.filter(
          s => s.criterionId === criterion.id && s.vendorId === vendor.id
        )
        const avgScore = scores.length > 0
          ? scores.reduce((sum, s) => sum + s.score, 0) / scores.length
          : 0
        const maxScore = scores.length > 0 ? scores[0].maxScore : 100
        const percentage = maxScore > 0 ? (avgScore / maxScore) * 100 : 0

        const aiEval = aiEvaluations.value.find(
          a => a.criterionId === criterion.id && a.vendorId === vendor.id
        )

        return {
          vendorId: vendor.id,
          vendorCode: vendor.code,
          criterionId: criterion.id,
          criterionName: criterion.name,
          score: avgScore,
          maxScore,
          percentage,
          color: getHeatmapColor(percentage),
          notes: scores.map(s => s.notes).filter(Boolean).join(' | '),
          aiScore: aiEval?.suggestedScore,
          aiJustification: aiEval?.justification,
        } satisfies HeatmapCell
      })
    })
  }

  /* ── Actions ───────────────────────────────── */

  async function loadCompetitions() {
    loading.value = true
    error.value = null
    try {
      competitions.value = await api.fetchCompetitionEvaluations()
    } catch (e: unknown) {
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
    } catch (e: unknown) {
      error.value = 'تعذر تحميل بيانات المنافسة'
    } finally {
      loading.value = false
    }
  }

  async function loadTechnicalData(competitionId: string) {
    loading.value = true
    error.value = null
    try {
      const [committeeData, criteriaData, vendorsData, scoresData, aiData, alertsData] =
        await Promise.all([
          api.fetchCommittee(competitionId, 'technical'),
          api.fetchCriteria(competitionId, 'technical'),
          api.fetchVendors(competitionId, 'technical'),
          api.fetchTechnicalScores(competitionId),
          api.fetchAiEvaluations(competitionId),
          api.fetchVarianceAlerts(competitionId),
        ])
      committee.value = committeeData
      criteria.value = criteriaData
      vendors.value = vendorsData
      technicalScores.value = scoresData
      aiEvaluations.value = aiData
      varianceAlerts.value = alertsData
    } catch (e: unknown) {
      error.value = 'تعذر تحميل بيانات التقييم الفني'
    } finally {
      loading.value = false
    }
  }

  async function loadFinancialData(competitionId: string) {
    loading.value = true
    error.value = null
    try {
      const [committeeData, criteriaData, vendorsData, offersData, scoresData] =
        await Promise.all([
          api.fetchCommittee(competitionId, 'financial'),
          api.fetchCriteria(competitionId, 'financial'),
          api.fetchVendors(competitionId, 'financial'),
          api.fetchFinancialOffers(competitionId),
          api.fetchFinancialScores(competitionId),
        ])
      committee.value = committeeData
      criteria.value = criteriaData
      vendors.value = vendorsData
      financialOffers.value = offersData
      financialScores.value = scoresData
    } catch (e: unknown) {
      error.value = 'تعذر تحميل بيانات التقييم المالي'
    } finally {
      loading.value = false
    }
  }

  async function submitScore(
    competitionId: string,
    type: 'technical' | 'financial',
    score: Partial<TechnicalScore | FinancialScore>
  ) {
    try {
      if (type === 'technical') {
        const result = await api.submitTechnicalScore(competitionId, score)
        technicalScores.value.push(result)
      } else {
        const result = await api.submitFinancialScore(competitionId, score as Partial<FinancialScore>)
        financialScores.value.push(result)
      }
    } catch (e: unknown) {
      error.value = 'تعذر حفظ الدرجة'
      throw e
    }
  }

  async function requestAiAnalysis(competitionId: string, vendorId: string) {
    loading.value = true
    try {
      const results = await api.requestAiEvaluation(competitionId, vendorId)
      aiEvaluations.value.push(...results)
    } catch (e: unknown) {
      error.value = 'تعذر الحصول على تقييم الذكاء الاصطناعي'
    } finally {
      loading.value = false
    }
  }

  async function loadComparisonMatrix(competitionId: string, type: string) {
    loading.value = true
    try {
      comparisonMatrix.value = await api.fetchComparisonMatrix(competitionId, type)
    } catch (e: unknown) {
      error.value = 'تعذر تحميل مصفوفة المقارنة'
    } finally {
      loading.value = false
    }
  }

  /* ── Auto-save ─────────────────────────────── */

  function startAutoSave(competitionId: string, type: 'technical' | 'financial') {
    stopAutoSave()
    autoSaveTimer.value = setInterval(async () => {
      try {
        if (type === 'technical' && technicalScores.value.length > 0) {
          await api.saveTechnicalScores(competitionId, technicalScores.value)
        } else if (type === 'financial' && financialScores.value.length > 0) {
          await api.saveFinancialScores(competitionId, financialScores.value)
        }
      } catch {
        // Silent fail for auto-save; user can manually save
      }
    }, 30000) // 30 seconds
  }

  function stopAutoSave() {
    if (autoSaveTimer.value) {
      clearInterval(autoSaveTimer.value)
      autoSaveTimer.value = null
    }
  }

  /* ── Reset ─────────────────────────────────── */

  function $reset() {
    stopAutoSave()
    competitions.value = []
    selectedCompetition.value = null
    committee.value = null
    criteria.value = []
    vendors.value = []
    technicalScores.value = []
    financialScores.value = []
    financialOffers.value = []
    aiEvaluations.value = []
    varianceAlerts.value = []
    comparisonMatrix.value = null
    loading.value = false
    error.value = null
  }

  return {
    /* State */
    competitions,
    selectedCompetition,
    committee,
    criteria,
    vendors,
    technicalScores,
    financialScores,
    financialOffers,
    aiEvaluations,
    varianceAlerts,
    comparisonMatrix,
    loading,
    error,

    /* Getters */
    totalCriteriaWeight,
    passedVendors,
    pendingScoresCount,

    /* Actions */
    loadCompetitions,
    selectCompetition,
    loadTechnicalData,
    loadFinancialData,
    submitScore,
    requestAiAnalysis,
    loadComparisonMatrix,
    buildHeatmapCells,
    getHeatmapColor,
    startAutoSave,
    stopAutoSave,
    $reset,
  }
})
