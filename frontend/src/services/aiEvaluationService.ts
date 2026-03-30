/**
 * AI Evaluation Service
 *
 * Provides frontend API integration for AI-powered offer analysis.
 * Connects to backend endpoints for triggering analysis, retrieving results,
 * and managing human review workflow.
 *
 * Backend endpoints:
 * - POST /api/v1/competitions/{id}/technical-evaluation/ai-analysis/trigger
 * - GET  /api/v1/competitions/{id}/technical-evaluation/ai-analysis/summary
 * - GET  /api/v1/competitions/{id}/technical-evaluation/ai-analysis/{analysisId}
 * - GET  /api/v1/competitions/{id}/technical-evaluation/ai-analysis/comparison-matrix
 * - POST /api/v1/competitions/{id}/technical-evaluation/ai-analysis/{analysisId}/review
 */
import http, { httpGet, httpPost } from '@/services/http'

/* ── Types ──────────────────────────────────── */

export interface AiCriterionAnalysis {
  id: string
  evaluationCriterionId: string
  criterionNameAr: string
  suggestedScore: number
  maxScore: number
  scorePercentage: number
  detailedJustification: string
  offerCitations: string
  bookletRequirementReference?: string
  complianceNotes: string
  complianceLevel: 'FullyCompliant' | 'PartiallyCompliant' | 'NonCompliant' | 'RequiresReview'
}

export interface AiOfferAnalysis {
  id: string
  technicalEvaluationId: string
  supplierOfferId: string
  blindCode: string
  executiveSummary: string
  strengthsAnalysis: string
  weaknessesAnalysis: string
  risksAnalysis: string
  complianceAssessment: string
  overallRecommendation: string
  overallComplianceScore: number
  status: 'Pending' | 'InProgress' | 'Completed' | 'Failed'
  aiModelUsed: string
  aiProviderUsed: string
  analysisLatencyMs: number
  isHumanReviewed: boolean
  reviewedBy?: string
  reviewedAt?: string
  reviewNotes?: string
  createdAt: string
  criterionAnalyses: AiCriterionAnalysis[]
}

export interface AiOfferSummaryItem {
  supplierOfferId: string
  blindCode: string
  overallComplianceScore: number
  status: string
  isHumanReviewed: boolean
  criteriaAnalyzed: number
  fullyCompliantCount: number
  partiallyCompliantCount: number
  nonCompliantCount: number
  requiresReviewCount: number
}

export interface AiAnalysisSummary {
  technicalEvaluationId: string
  competitionId: string
  totalOffers: number
  completedAnalyses: number
  failedAnalyses: number
  pendingReviews: number
  offerSummaries: AiOfferSummaryItem[]
}

export interface AiComparisonCell {
  blindCode: string
  criterionId: string
  criterionNameAr: string
  suggestedScore: number
  maxScore: number
  scorePercentage: number
  complianceLevel: string
  justificationSummary: string
}

export interface AiComparisonMatrix {
  technicalEvaluationId: string
  offerBlindCodes: string[]
  criteria: { id: string; nameAr: string; weight: number; maxScore: number }[]
  cells: AiComparisonCell[]
}

/* ── API Functions ──────────────────────────── */

const BASE = '/v1/competitions'

/** Extended timeout for AI endpoints (3 minutes for multi-offer analysis) */
const AI_TIMEOUT = 180_000

/**
 * Trigger AI analysis for all offers in a technical evaluation.
 */
export async function triggerAiAnalysis(
  competitionId: string,
  evaluationId: string
): Promise<AiAnalysisSummary> {
  const response = await http.post<AiAnalysisSummary>(
    `${BASE}/${competitionId}/technical-evaluation/ai-analysis/trigger`,
    { evaluationId },
    { timeout: AI_TIMEOUT },
  )
  return response.data
}

/**
 * Get summary of AI analyses for all offers.
 */
export function getAiAnalysisSummary(
  competitionId: string,
  evaluationId: string
): Promise<AiAnalysisSummary> {
  return httpGet<AiAnalysisSummary>(
    `${BASE}/${competitionId}/technical-evaluation/ai-analysis/summary?evaluationId=${evaluationId}`
  )
}

/**
 * Get detailed AI analysis for a specific offer.
 */
export function getAiOfferAnalysis(
  competitionId: string,
  analysisId: string
): Promise<AiOfferAnalysis> {
  return httpGet<AiOfferAnalysis>(
    `${BASE}/${competitionId}/technical-evaluation/ai-analysis/${analysisId}`
  )
}

/**
 * Get AI comparison matrix across all offers and criteria.
 */
export function getAiComparisonMatrix(
  competitionId: string,
  evaluationId: string
): Promise<AiComparisonMatrix> {
  return httpGet<AiComparisonMatrix>(
    `${BASE}/${competitionId}/technical-evaluation/ai-analysis/comparison-matrix?evaluationId=${evaluationId}`
  )
}

/**
 * Mark an AI analysis as reviewed by a human committee member.
 */
export function reviewAiAnalysis(
  competitionId: string,
  analysisId: string,
  reviewNotes?: string
): Promise<AiOfferAnalysis> {
  return httpPost<AiOfferAnalysis>(
    `${BASE}/${competitionId}/technical-evaluation/ai-analysis/${analysisId}/review`,
    { analysisId, reviewNotes }
  )
}
