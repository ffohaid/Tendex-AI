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

export async function fetchCompetitionEvaluations(): Promise<CompetitionEvaluation[]> {
  // Backend requires page and pageSize params; fetch all competitions for evaluation list
  const response = await httpGet<{ items: CompetitionEvaluation[]; totalCount: number }>(
    `${COMPETITIONS_BASE}?page=1&pageSize=100`
  )
  // Backend returns paginated result with items array
  if (response && typeof response === 'object' && 'items' in response) {
    return (response as { items: CompetitionEvaluation[] }).items
  }
  // Fallback: if response is already an array
  return Array.isArray(response) ? response : []
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
