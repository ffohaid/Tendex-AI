/**
 * Evaluation API service.
 *
 * All data MUST be fetched from backend APIs.
 * NO mock/hardcoded data in Vue components.
 *
 * Uses the centralized httpClient (Axios) for automatic auth token
 * and tenant ID injection.
 *
 * Backend endpoints:
 * - Technical: /api/v1/competitions/{competitionId}/technical-evaluation
 * - Financial: /api/v1/competitions/{competitionId}/financial-evaluation
 * - AI Analysis: /api/v1/competitions/{competitionId}/technical-evaluation/ai-analysis
 * - Minutes: /api/v1/competitions/{competitionId}/minutes
 * - Award: /api/v1/competitions/{competitionId}/award
 */
import { httpGet, httpPost, httpPut } from '@/services/http'
import type {
  Committee,
  CompetitionEvaluation,
  ComparisonMatrix,
  TechnicalScore,
  FinancialScore,
  FinancialOffer,
  AiEvaluation,
  EvaluationCriterion,
  Vendor,
  EvaluationMinutes,
  VarianceAlert,
} from '@/types/evaluation'

const COMPETITIONS_BASE = '/v1/competitions'

/* ──────────────────────────────────────────────
 * Competition Evaluations
 * ────────────────────────────────────────────── */

/**
 * Backend CompetitionListItemDto shape (from /api/v1/competitions).
 * We map this to the frontend CompetitionEvaluation interface.
 */
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

/**
 * Maps a backend competition status to an evaluation stage.
 */
function mapStatusToStage(status: string): 'technical' | 'financial' | 'completed' {
  switch (status) {
    case 'TechnicalEvaluation':
    case 'Draft':
    case 'Published':
    case 'ReceivingOffers':
      return 'technical'
    case 'FinancialEvaluation':
      return 'financial'
    case 'Completed':
    case 'Awarded':
    case 'Cancelled':
      return 'completed'
    default:
      return 'technical'
  }
}

/**
 * Maps a backend competition status to a technical evaluation status.
 */
function mapToEvalStatus(status: string): 'pending' | 'in_progress' | 'completed' | 'approved' {
  switch (status) {
    case 'Draft':
    case 'Published':
    case 'ReceivingOffers':
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
  // Backend returns CompetitionListItemDto from /api/v1/competitions
  const response = await httpGet<{ items: BackendCompetitionItem[]; totalCount: number }>(
    `${COMPETITIONS_BASE}?page=1&pageSize=100`
  )

  let items: BackendCompetitionItem[] = []
  if (response && typeof response === 'object' && 'items' in response) {
    items = (response as { items: BackendCompetitionItem[] }).items
  } else if (Array.isArray(response)) {
    items = response as unknown as BackendCompetitionItem[]
  }

  // Map backend DTO to frontend CompetitionEvaluation interface
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

/* ──────────────────────────────────────────────
 * Committees
 * ────────────────────────────────────────────── */

export function fetchCommittee(competitionId: string, type: string): Promise<Committee> {
  return httpGet<Committee>(`/v1/committees?competitionId=${competitionId}&type=${type}`)
}

/* ──────────────────────────────────────────────
 * Criteria
 * ────────────────────────────────────────────── */

export function fetchCriteria(competitionId: string, type: string): Promise<EvaluationCriterion[]> {
  const evalType = type === 'technical' ? 'technical-evaluation' : 'financial-evaluation'
  return httpGet<EvaluationCriterion[]>(`${COMPETITIONS_BASE}/${competitionId}/${evalType}/criteria`)
}

/* ──────────────────────────────────────────────
 * Vendors
 * ────────────────────────────────────────────── */

export function fetchVendors(competitionId: string, type: string): Promise<Vendor[]> {
  const evalType = type === 'technical' ? 'technical-evaluation' : 'financial-evaluation'
  return httpGet<Vendor[]>(`${COMPETITIONS_BASE}/${competitionId}/${evalType}/vendors`)
}

/* ──────────────────────────────────────────────
 * Technical Scores
 * ────────────────────────────────────────────── */

export function fetchTechnicalScores(competitionId: string): Promise<TechnicalScore[]> {
  return httpGet<TechnicalScore[]>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/scores`)
}

export function submitTechnicalScore(competitionId: string, score: Partial<TechnicalScore>): Promise<TechnicalScore> {
  return httpPost<TechnicalScore>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/scores`, score)
}

export function saveTechnicalScores(competitionId: string, scores: Partial<TechnicalScore>[]): Promise<void> {
  return httpPut<void>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/scores/batch`, { scores })
}

/* ──────────────────────────────────────────────
 * Financial Evaluation
 * ────────────────────────────────────────────── */

export function fetchFinancialOffers(competitionId: string): Promise<FinancialOffer[]> {
  return httpGet<FinancialOffer[]>(`${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/offers`)
}

export function fetchFinancialScores(competitionId: string): Promise<FinancialScore[]> {
  return httpGet<FinancialScore[]>(`${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/scores`)
}

export function submitFinancialScore(competitionId: string, score: Partial<FinancialScore>): Promise<FinancialScore> {
  return httpPost<FinancialScore>(`${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/scores`, score)
}

export function saveFinancialScores(competitionId: string, scores: Partial<FinancialScore>[]): Promise<void> {
  return httpPut<void>(`${COMPETITIONS_BASE}/${competitionId}/financial-evaluation/scores/batch`, { scores })
}

/* ──────────────────────────────────────────────
 * AI Evaluation
 * ────────────────────────────────────────────── */

export function requestAiEvaluation(competitionId: string, vendorId: string): Promise<AiEvaluation[]> {
  return httpPost<AiEvaluation[]>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/evaluate`, { vendorId })
}

export function fetchAiEvaluations(competitionId: string): Promise<AiEvaluation[]> {
  return httpGet<AiEvaluation[]>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis`)
}

export function fetchVarianceAlerts(competitionId: string): Promise<VarianceAlert[]> {
  return httpGet<VarianceAlert[]>(`${COMPETITIONS_BASE}/${competitionId}/technical-evaluation/ai-analysis/variance-alerts`)
}

/* ──────────────────────────────────────────────
 * Comparison Matrix
 * ────────────────────────────────────────────── */

export function fetchComparisonMatrix(competitionId: string, type: string): Promise<ComparisonMatrix> {
  const evalType = type === 'technical' ? 'technical-evaluation' : 'financial-evaluation'
  return httpGet<ComparisonMatrix>(`${COMPETITIONS_BASE}/${competitionId}/${evalType}/comparison`)
}

/* ──────────────────────────────────────────────
 * Minutes
 * ────────────────────────────────────────────── */

export function fetchMinutes(competitionId: string, type: string): Promise<EvaluationMinutes> {
  return httpGet<EvaluationMinutes>(`${COMPETITIONS_BASE}/${competitionId}/minutes?type=${type}`)
}

export function approveMinutes(competitionId: string, minutesId: string): Promise<EvaluationMinutes> {
  return httpPost<EvaluationMinutes>(`${COMPETITIONS_BASE}/${competitionId}/minutes/${minutesId}/approve`)
}

export function signMinutes(competitionId: string, minutesId: string): Promise<void> {
  return httpPost<void>(`${COMPETITIONS_BASE}/${competitionId}/minutes/${minutesId}/sign`)
}
