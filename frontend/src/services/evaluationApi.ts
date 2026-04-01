/**
 * Evaluation API service.
 *
 * All data MUST be fetched from backend APIs.
 * NO mock/hardcoded data in Vue components.
 *
 * Aligned with actual backend endpoints:
 * - SupplierOffers: /api/v1/competitions/{id}/supplier-offers
 * - Technical: /api/v1/competitions/{id}/technical-evaluation
 * - Financial: /api/v1/competitions/{id}/financial-evaluation
 * - AI Analysis: /api/v1/competitions/{id}/technical-evaluation/ai-analysis
 */
import { httpGet, httpPost, httpPut, httpDelete } from '@/services/http'
import type {
  Committee,
  CompetitionEvaluation,
  SupplierOffer,
  TechnicalEvaluationDetail,
  BlindOfferSummary,
  TechnicalScore,
  TechnicalHeatmap,
  OfferEvaluationResult,
  VarianceAlert,
  FinancialEvaluationDetail,
  FinancialOfferItem,
  FinancialComparisonMatrix,
  ArithmeticVerificationResult,
  FinancialScore,
  EvaluationCriterion,
  AiEvaluation,
  ComparisonMatrix,
  EvaluationMinutes,
  Vendor,
} from '@/types/evaluation'

const COMPETITIONS_BASE = '/v1/competitions'

/* ══════════════════════════════════════════════
 * Competition Evaluations List
 * ══════════════════════════════════════════════ */

interface BackendCompetitionItem {
  id: string
  referenceNumber: string
  projectNameAr: string
  projectNameEn: string
  competitionType: string
  status: string
  creationMethod: string
  estimatedBudget: number | null
  currency: string
  submissionDeadline: string | null
  sectionsCount: number
  boqItemsCount: number
  attachmentsCount: number
  createdAt: string
  createdBy: string | null
}

function mapStatusToStage(status: string): 'technical' | 'financial' | 'completed' {
  switch (status) {
    case 'TechnicalEvaluation':
      return 'technical'
    case 'FinancialEvaluation':
      return 'financial'
    case 'Completed':
    case 'Awarded':
      return 'completed'
    default:
      return 'technical'
  }
}

function mapToEvalStatus(status: string): 'pending' | 'in_progress' | 'completed' | 'approved' {
  switch (status) {
    case 'Draft':
    case 'UnderPreparation':
    case 'PendingApproval':
    case 'Published':
    case 'ReceivingOffers':
    case 'OffersClosed':
      return 'pending'
    case 'TechnicalEvaluation':
      return 'in_progress'
    case 'FinancialEvaluation':
    case 'Completed':
    case 'Awarded':
      return 'completed'
    default:
      return 'pending'
  }
}

export async function fetchCompetitionEvaluations(): Promise<CompetitionEvaluation[]> {
  const response = await httpGet<{ items: BackendCompetitionItem[]; totalCount: number }>(
    `${COMPETITIONS_BASE}?page=1&pageSize=100`
  )

  let items: BackendCompetitionItem[] = []
  if (response && typeof response === 'object' && 'items' in response) {
    items = (response as { items: BackendCompetitionItem[] }).items
  } else if (Array.isArray(response)) {
    items = response as unknown as BackendCompetitionItem[]
  }

  return items.map((item): CompetitionEvaluation => {
    const stage = mapStatusToStage(item.status)
    const techStatus = mapToEvalStatus(item.status)
    const finStatus = stage === 'financial' ? 'in_progress' as const
      : stage === 'completed' ? 'completed' as const
      : 'pending' as const

    return {
      id: item.id,
      competitionNumber: item.referenceNumber,
      competitionName: item.projectNameAr,
      projectName: item.projectNameAr,
      stage,
      technicalStatus: techStatus,
      financialStatus: finStatus,
      vendorCount: 0,
      passedVendorCount: 0,
      estimatedBudget: item.estimatedBudget ?? 0,
      deadlineGregorian: item.submissionDeadline ?? '',
      deadlineHijri: '',
      assignedCommittee: '-',
      progress: stage === 'completed' ? 100 : stage === 'financial' ? 60 : 30,
    }
  })
}

export function fetchCompetitionEvaluation(id: string): Promise<CompetitionEvaluation> {
  return httpGet<CompetitionEvaluation>(`${COMPETITIONS_BASE}/${id}`)
}

/* ══════════════════════════════════════════════
 * Supplier Offers CRUD
 * ══════════════════════════════════════════════ */

export function fetchSupplierOffers(competitionId: string): Promise<SupplierOffer[]> {
  return httpGet<SupplierOffer[]>(`${COMPETITIONS_BASE}/${competitionId}/offers`)
}

export function createSupplierOffer(competitionId: string, data: {
  supplierName: string
  supplierIdentifier: string
  offerReferenceNumber: string
  submissionDate: string
}): Promise<SupplierOffer> {
  return httpPost<SupplierOffer>(`${COMPETITIONS_BASE}/${competitionId}/offers`, data)
}

export function deleteSupplierOffer(competitionId: string, offerId: string): Promise<void> {
  return httpDelete<void>(`${COMPETITIONS_BASE}/${competitionId}/offers/${offerId}`)
}

/* ══════════════════════════════════════════════
 * Technical Evaluation
 * ══════════════════════════════════════════════ */

export function startTechnicalEvaluation(competitionId: string, committeeId: string): Promise<TechnicalEvaluationDetail> {
  return httpPost<TechnicalEvaluationDetail>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/start`,
    { committeeId }
  )
}

export function fetchTechnicalEvaluationDetails(competitionId: string): Promise<TechnicalEvaluationDetail> {
  return httpGet<TechnicalEvaluationDetail>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation`
  )
}

export function fetchBlindOffers(competitionId: string): Promise<BlindOfferSummary[]> {
  return httpGet<BlindOfferSummary[]>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/offers`
  )
}

export function submitTechnicalScore(competitionId: string, data: {
  evaluationId: string
  supplierOfferId: string
  evaluationCriterionId: string
  score: number
  notes?: string
}): Promise<TechnicalScore> {
  return httpPost<TechnicalScore>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/scores`,
    data
  )
}

export function completeTechnicalScoring(competitionId: string, evaluationId: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/complete-scoring`,
    { evaluationId }
  )
}

export function approveTechnicalReport(competitionId: string, evaluationId: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/approve`,
    { evaluationId }
  )
}

export function rejectTechnicalReport(competitionId: string, evaluationId: string, reason: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/reject`,
    { evaluationId, reason }
  )
}

export function fetchTechnicalResults(competitionId: string): Promise<OfferEvaluationResult[]> {
  return httpGet<OfferEvaluationResult[]>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/results`
  )
}

export function fetchTechnicalHeatmap(competitionId: string): Promise<TechnicalHeatmap> {
  return httpGet<TechnicalHeatmap>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/heatmap`
  )
}

export function fetchVarianceAlerts(competitionId: string): Promise<VarianceAlert[]> {
  return httpGet<VarianceAlert[]>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/variance-alerts`
  )
}

export function openFinancialEnvelopes(competitionId: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/open-financial-envelopes`
  )
}

/* ══════════════════════════════════════════════
 * Financial Evaluation
 * ══════════════════════════════════════════════ */

export function startFinancialEvaluation(competitionId: string, committeeId: string): Promise<FinancialEvaluationDetail> {
  return httpPost<FinancialEvaluationDetail>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/start`,
    { committeeId }
  )
}

export function fetchFinancialEvaluationDetails(competitionId: string): Promise<FinancialEvaluationDetail> {
  return httpGet<FinancialEvaluationDetail>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation`
  )
}

export function submitFinancialOfferItems(competitionId: string, supplierOfferId: string, items: {
  boqItemId: string
  unitPrice: number
  quantity: number
  supplierSubmittedTotal?: number
}[]): Promise<FinancialOfferItem[]> {
  return httpPost<FinancialOfferItem[]>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/offers/${supplierOfferId}/items`,
    { items }
  )
}

export function fetchFinancialOfferItems(competitionId: string, supplierOfferId: string): Promise<FinancialOfferItem[]> {
  return httpGet<FinancialOfferItem[]>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/offers/${supplierOfferId}/items`
  )
}

export function verifyArithmetic(competitionId: string, supplierOfferId: string): Promise<ArithmeticVerificationResult> {
  return httpPost<ArithmeticVerificationResult>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/offers/${supplierOfferId}/verify-arithmetic`
  )
}

export function submitFinancialScore(competitionId: string, supplierOfferId: string, data: {
  score: number
  maxScore: number
  notes?: string
}): Promise<FinancialScore> {
  return httpPost<FinancialScore>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/offers/${supplierOfferId}/scores`,
    data
  )
}

export function completeFinancialScoring(competitionId: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/complete-scoring`
  )
}

export function fetchFinancialComparisonMatrix(competitionId: string): Promise<FinancialComparisonMatrix> {
  return httpGet<FinancialComparisonMatrix>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/comparison-matrix`
  )
}

export function approveFinancialReport(competitionId: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/approve`
  )
}

export function rejectFinancialReport(competitionId: string, reason: string): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/reject`,
    { reason }
  )
}

/* ══════════════════════════════════════════════
 * AI Offer Analysis
 * ══════════════════════════════════════════════ */

export function triggerAiAnalysis(competitionId: string): Promise<{ analysisIds: string[] }> {
  return httpPost<{ analysisIds: string[] }>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/trigger`
  )
}

export function fetchAiAnalysisSummary(competitionId: string): Promise<AiEvaluation[]> {
  return httpGet<AiEvaluation[]>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/summary`
  )
}

export function fetchAiOfferAnalysis(competitionId: string, analysisId: string): Promise<AiEvaluation> {
  return httpGet<AiEvaluation>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/${analysisId}`
  )
}

export function fetchAiComparisonMatrix(competitionId: string): Promise<ComparisonMatrix> {
  return httpGet<ComparisonMatrix>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/comparison-matrix`
  )
}

export function reviewAiAnalysis(competitionId: string, analysisId: string, data: {
  approved: boolean
  notes?: string
}): Promise<void> {
  return httpPost<void>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/${analysisId}/review`,
    data
  )
}

/* ══════════════════════════════════════════════
 * Committees
 * ══════════════════════════════════════════════ */

export function fetchCommittee(competitionId: string, type: string): Promise<Committee> {
  return httpGet<Committee>(`/v1/committees?competitionId=${competitionId}&type=${type}`)
}

export function fetchCommittees(competitionId: string): Promise<Committee[]> {
  return httpGet<Committee[]>(`/v1/committees?competitionId=${competitionId}`)
}

/* ══════════════════════════════════════════════
 * Criteria (from competition detail)
 * ══════════════════════════════════════════════ */

export function fetchCriteria(competitionId: string): Promise<EvaluationCriterion[]> {
  // Criteria are part of the competition detail - extract from there
  return httpGet<EvaluationCriterion[]>(`${COMPETITIONS_BASE}/${competitionId}/criteria`)
}

/* ══════════════════════════════════════════════
 * Legacy compatibility functions
 * ══════════════════════════════════════════════ */

export function fetchVendors(competitionId: string, _type: string): Promise<Vendor[]> {
  // Map to blind offers for backward compatibility
  return fetchBlindOffers(competitionId).then(offers =>
    offers.map(o => ({
      id: o.offerId,
      code: o.blindCode,
      realName: o.supplierName ?? undefined,
      offerId: o.offerId,
      technicalScore: o.technicalTotalScore ?? undefined,
      technicalStatus: o.technicalResult === 1 ? 'passed' as const
        : o.technicalResult === 2 ? 'failed' as const
        : 'under_review' as const,
    }))
  )
}

export function fetchTechnicalScores(competitionId: string): Promise<TechnicalScore[]> {
  return httpGet<TechnicalScore[]>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/scores`)
}

export function saveTechnicalScores(competitionId: string, scores: Partial<TechnicalScore>[]): Promise<void> {
  return httpPut<void>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/scores/batch`, { scores })
}

export function fetchFinancialOffers(competitionId: string): Promise<SupplierOffer[]> {
  return fetchSupplierOffers(competitionId)
}

export function fetchFinancialScores(competitionId: string): Promise<FinancialScore[]> {
  return httpGet<FinancialScore[]>(`${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/scores`)
}

export function requestAiEvaluation(competitionId: string, vendorId: string): Promise<AiEvaluation[]> {
  return httpPost<AiEvaluation[]>(
    `${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/trigger`,
    { vendorId }
  )
}

export function fetchAiEvaluations(competitionId: string): Promise<AiEvaluation[]> {
  return fetchAiAnalysisSummary(competitionId)
}

export function fetchComparisonMatrix(competitionId: string, type: string): Promise<ComparisonMatrix> {
  if (type === 'financial') {
    return fetchFinancialComparisonMatrix(competitionId) as unknown as Promise<ComparisonMatrix>
  }
  return fetchAiComparisonMatrix(competitionId)
}

export function fetchMinutes(competitionId: string, type: string): Promise<EvaluationMinutes> {
  return httpGet<EvaluationMinutes>(`${COMPETITIONS_BASE}/${competitionId}/minutes?type=${type}`)
}

export function approveMinutes(competitionId: string, minutesId: string): Promise<EvaluationMinutes> {
  return httpPost<EvaluationMinutes>(`${COMPETITIONS_BASE}/${competitionId}/minutes/${minutesId}/approve`)
}

export function signMinutes(competitionId: string, minutesId: string): Promise<void> {
  return httpPost<void>(`${COMPETITIONS_BASE}/${competitionId}/minutes/${minutesId}/sign`)
}
