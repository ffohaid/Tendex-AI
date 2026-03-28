/**
 * Evaluation API service.
 * All data MUST be fetched from backend APIs.
 * NO mock/hardcoded data in Vue components.
 *
 * Note: API base URL is configured via environment variables.
 * This service provides typed wrappers around fetch calls.
 */
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

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api'

/**
 * Generic fetch wrapper with error handling.
 */
async function apiFetch<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const url = `${API_BASE}${endpoint}`
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      'Accept-Language': document.documentElement.lang || 'ar',
      ...options?.headers,
    },
    ...options,
  })

  if (!response.ok) {
    const errorBody = await response.text().catch(() => '')
    throw new Error(`API Error ${response.status}: ${errorBody}`)
  }

  return response.json() as Promise<T>
}

/* ──────────────────────────────────────────────
 * Competition Evaluations
 * ────────────────────────────────────────────── */

export function fetchCompetitionEvaluations(): Promise<CompetitionEvaluation[]> {
  return apiFetch<CompetitionEvaluation[]>('/evaluations/competitions')
}

export function fetchCompetitionEvaluation(id: string): Promise<CompetitionEvaluation> {
  return apiFetch<CompetitionEvaluation>(`/evaluations/competitions/${id}`)
}

/* ──────────────────────────────────────────────
 * Committees
 * ────────────────────────────────────────────── */

export function fetchCommittee(competitionId: string, type: string): Promise<Committee> {
  return apiFetch<Committee>(`/evaluations/competitions/${competitionId}/committees/${type}`)
}

/* ──────────────────────────────────────────────
 * Criteria
 * ────────────────────────────────────────────── */

export function fetchCriteria(competitionId: string, type: string): Promise<EvaluationCriterion[]> {
  return apiFetch<EvaluationCriterion[]>(`/evaluations/competitions/${competitionId}/criteria/${type}`)
}

/* ──────────────────────────────────────────────
 * Vendors
 * ────────────────────────────────────────────── */

export function fetchVendors(competitionId: string, type: string): Promise<Vendor[]> {
  return apiFetch<Vendor[]>(`/evaluations/competitions/${competitionId}/vendors/${type}`)
}

/* ──────────────────────────────────────────────
 * Technical Scores
 * ────────────────────────────────────────────── */

export function fetchTechnicalScores(competitionId: string): Promise<TechnicalScore[]> {
  return apiFetch<TechnicalScore[]>(`/evaluations/competitions/${competitionId}/technical/scores`)
}

export function submitTechnicalScore(competitionId: string, score: Partial<TechnicalScore>): Promise<TechnicalScore> {
  return apiFetch<TechnicalScore>(`/evaluations/competitions/${competitionId}/technical/scores`, {
    method: 'POST',
    body: JSON.stringify(score),
  })
}

export function saveTechnicalScores(competitionId: string, scores: Partial<TechnicalScore>[]): Promise<void> {
  return apiFetch<void>(`/evaluations/competitions/${competitionId}/technical/scores/batch`, {
    method: 'PUT',
    body: JSON.stringify({ scores }),
  })
}

/* ──────────────────────────────────────────────
 * Financial Evaluation
 * ────────────────────────────────────────────── */

export function fetchFinancialOffers(competitionId: string): Promise<FinancialOffer[]> {
  return apiFetch<FinancialOffer[]>(`/evaluations/competitions/${competitionId}/financial/offers`)
}

export function fetchFinancialScores(competitionId: string): Promise<FinancialScore[]> {
  return apiFetch<FinancialScore[]>(`/evaluations/competitions/${competitionId}/financial/scores`)
}

export function submitFinancialScore(competitionId: string, score: Partial<FinancialScore>): Promise<FinancialScore> {
  return apiFetch<FinancialScore>(`/evaluations/competitions/${competitionId}/financial/scores`, {
    method: 'POST',
    body: JSON.stringify(score),
  })
}

export function saveFinancialScores(competitionId: string, scores: Partial<FinancialScore>[]): Promise<void> {
  return apiFetch<void>(`/evaluations/competitions/${competitionId}/financial/scores/batch`, {
    method: 'PUT',
    body: JSON.stringify({ scores }),
  })
}

/* ──────────────────────────────────────────────
 * AI Evaluation
 * ────────────────────────────────────────────── */

export function requestAiEvaluation(competitionId: string, vendorId: string): Promise<AiEvaluation[]> {
  return apiFetch<AiEvaluation[]>(`/evaluations/competitions/${competitionId}/ai/evaluate`, {
    method: 'POST',
    body: JSON.stringify({ vendorId }),
  })
}

export function fetchAiEvaluations(competitionId: string): Promise<AiEvaluation[]> {
  return apiFetch<AiEvaluation[]>(`/evaluations/competitions/${competitionId}/ai/evaluations`)
}

export function fetchVarianceAlerts(competitionId: string): Promise<VarianceAlert[]> {
  return apiFetch<VarianceAlert[]>(`/evaluations/competitions/${competitionId}/ai/variance-alerts`)
}

/* ──────────────────────────────────────────────
 * Comparison Matrix
 * ────────────────────────────────────────────── */

export function fetchComparisonMatrix(competitionId: string, type: string): Promise<ComparisonMatrix> {
  return apiFetch<ComparisonMatrix>(`/evaluations/competitions/${competitionId}/comparison/${type}`)
}

/* ──────────────────────────────────────────────
 * Minutes
 * ────────────────────────────────────────────── */

export function fetchMinutes(competitionId: string, type: string): Promise<EvaluationMinutes> {
  return apiFetch<EvaluationMinutes>(`/evaluations/competitions/${competitionId}/minutes/${type}`)
}

export function approveMinutes(competitionId: string, minutesId: string): Promise<EvaluationMinutes> {
  return apiFetch<EvaluationMinutes>(`/evaluations/competitions/${competitionId}/minutes/${minutesId}/approve`, {
    method: 'POST',
  })
}

export function signMinutes(competitionId: string, minutesId: string): Promise<void> {
  return apiFetch<void>(`/evaluations/competitions/${competitionId}/minutes/${minutesId}/sign`, {
    method: 'POST',
  })
}
