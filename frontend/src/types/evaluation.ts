/**
 * Evaluation system type definitions.
 * Aligned with backend DTOs from TendexAI.Application.Features.
 * Covers technical and financial evaluation workflows,
 * committee management, supplier offers, and AI-assisted scoring.
 */

/* ──────────────────────────────────────────────
 * Enums (matching backend enums)
 * ────────────────────────────────────────────── */

export const TechnicalEvaluationStatus = {
  Pending: 0,
  InProgress: 1,
  AllScoresSubmitted: 2,
  PendingApproval: 3,
  Approved: 4,
  Rejected: 5,
} as const
export type TechnicalEvaluationStatus = (typeof TechnicalEvaluationStatus)[keyof typeof TechnicalEvaluationStatus]

export const FinancialEvaluationStatus = {
  Pending: 0,
  InProgress: 1,
  AllScoresSubmitted: 2,
  PendingApproval: 3,
  Approved: 4,
  Rejected: 5,
} as const
export type FinancialEvaluationStatus = (typeof FinancialEvaluationStatus)[keyof typeof FinancialEvaluationStatus]

export const OfferTechnicalResult = {
  Pending: 0,
  Passed: 1,
  Failed: 2,
} as const
export type OfferTechnicalResult = (typeof OfferTechnicalResult)[keyof typeof OfferTechnicalResult]

export const PriceDeviationLevel = {
  WithinRange: 1,
  ModerateDeviation: 2,
  SignificantDeviation: 3,
} as const
export type PriceDeviationLevel = (typeof PriceDeviationLevel)[keyof typeof PriceDeviationLevel]

export const HeatmapColor = {
  Excellent: 'excellent',
  Average: 'average',
  Weak: 'weak',
} as const
export type HeatmapColor = (typeof HeatmapColor)[keyof typeof HeatmapColor]

// Legacy compatibility
export const EvaluationStatus = {
  Pending: 'pending',
  InProgress: 'in_progress',
  Completed: 'completed',
  Approved: 'approved',
  Rejected: 'rejected',
} as const
export type EvaluationStatus = (typeof EvaluationStatus)[keyof typeof EvaluationStatus]

export const EvaluationType = {
  Technical: 'technical',
  Financial: 'financial',
} as const
export type EvaluationType = (typeof EvaluationType)[keyof typeof EvaluationType]

export const OfferStatus = {
  Passed: 'passed',
  Failed: 'failed',
  UnderReview: 'under_review',
} as const
export type OfferStatus = (typeof OfferStatus)[keyof typeof OfferStatus]

/* ──────────────────────────────────────────────
 * Supplier Offer (from backend SupplierOfferDto)
 * ────────────────────────────────────────────── */

export interface SupplierOffer {
  id: string
  competitionId: string
  supplierName: string
  supplierIdentifier: string
  offerReferenceNumber: string
  submissionDate: string
  blindCode: string
  technicalResult: OfferTechnicalResult
  technicalTotalScore: number | null
  isFinancialEnvelopeOpen: boolean
  financialEnvelopeOpenedAt: string | null
  createdAt: string
}

/* ──────────────────────────────────────────────
 * Technical Evaluation Detail (from backend TechnicalEvaluationDetailDto)
 * ────────────────────────────────────────────── */

export interface TechnicalEvaluationDetail {
  id: string
  competitionId: string
  committeeId: string
  status: TechnicalEvaluationStatus
  minimumPassingScore: number
  isBlindEvaluationActive: boolean
  startedAt: string | null
  completedAt: string | null
  approvedAt: string | null
  approvedBy: string | null
  rejectionReason: string | null
  createdAt: string
}

/* ──────────────────────────────────────────────
 * Blind Offer Summary (from backend BlindOfferSummaryDto)
 * ────────────────────────────────────────────── */

export interface BlindOfferSummary {
  offerId: string
  blindCode: string
  supplierName: string | null
  technicalTotalScore: number | null
  technicalResult: OfferTechnicalResult
  isFinancialEnvelopeOpen: boolean
}

/* ──────────────────────────────────────────────
 * Technical Score (from backend TechnicalScoreDto)
 * ────────────────────────────────────────────── */

export interface TechnicalScore {
  id: string
  supplierOfferId: string
  offerBlindCode: string
  evaluationCriterionId: string
  criterionNameAr: string
  criterionNameEn: string
  evaluatorUserId: string
  score: number
  maxScore: number
  scorePercentage: number
  notes: string | null
  createdAt: string
}

/* ──────────────────────────────────────────────
 * AI Technical Score (from backend AiTechnicalScoreDto)
 * ────────────────────────────────────────────── */

export interface AiTechnicalScore {
  id: string
  supplierOfferId: string
  offerBlindCode: string
  evaluationCriterionId: string
  criterionNameAr: string
  criterionNameEn: string
  suggestedScore: number
  maxScore: number
  scorePercentage: number
  justification: string
  referenceCitations: string | null
}

/* ──────────────────────────────────────────────
 * Variance Alert (from backend VarianceAlertDto)
 * ────────────────────────────────────────────── */

export interface VarianceAlert {
  criterionId: string
  criterionNameAr: string
  criterionNameEn: string
  offerId: string
  offerBlindCode: string
  hasEvaluatorVariance: boolean
  hasHumanAiVariance: boolean
  evaluatorSpread: number | null
  humanAiDifference: number | null
}

/* ──────────────────────────────────────────────
 * Heatmap (from backend TechnicalHeatmapDto)
 * ────────────────────────────────────────────── */

export interface CriterionHeader {
  id: string
  nameAr: string
  nameEn: string
  weightPercentage: number
  minimumPassingScore: number | null
}

export interface HeatmapCell {
  offerBlindCode: string
  offerId: string
  criterionId: string
  criterionNameAr: string
  criterionNameEn: string
  averageScorePercentage: number
  color: string
}

export interface TechnicalHeatmap {
  competitionId: string
  evaluationId: string
  offerBlindCodes: string[]
  criteria: CriterionHeader[]
  cells: HeatmapCell[]
}

/* ──────────────────────────────────────────────
 * Evaluation Results (from backend OfferEvaluationResultDto)
 * ────────────────────────────────────────────── */

export interface CriterionScoreSummary {
  criterionId: string
  criterionNameAr: string
  criterionNameEn: string
  weightPercentage: number
  averageScore: number
  averagePercentage: number
  aiSuggestedScore: number | null
  aiPercentage: number | null
  hasVariance: boolean
  evaluatorScores: EvaluatorScore[]
}

export interface EvaluatorScore {
  evaluatorUserId: string
  score: number
  maxScore: number
  percentage: number
  notes: string | null
}

export interface OfferEvaluationResult {
  offerId: string
  blindCode: string
  supplierName: string | null
  weightedTotalScore: number
  result: OfferTechnicalResult
  rank: number
  criterionScores: CriterionScoreSummary[]
}

/* ──────────────────────────────────────────────
 * Financial Evaluation (from backend FinancialEvaluationDtos)
 * ────────────────────────────────────────────── */

export interface FinancialEvaluationDetail {
  id: string
  competitionId: string
  committeeId: string
  status: FinancialEvaluationStatus
  startedAt: string | null
  completedAt: string | null
  approvedAt: string | null
  approvedBy: string | null
  rejectionReason: string | null
  createdAt: string
}

export interface FinancialOfferItem {
  id: string
  supplierOfferId: string
  supplierBlindCode: string
  boqItemId: string
  boqItemNumber: string
  boqDescriptionAr: string
  unitPrice: number
  quantity: number
  totalPrice: number
  isArithmeticallyVerified: boolean
  hasArithmeticError: boolean
  supplierSubmittedTotal: number | null
  deviationPercentage: number | null
  deviationLevel: PriceDeviationLevel | null
}

export interface FinancialComparisonMatrix {
  competitionId: string
  rows: FinancialComparisonRow[]
  supplierTotals: SupplierTotalSummary[]
  estimatedTotalCost: number
}

export interface FinancialComparisonRow {
  boqItemId: string
  itemNumber: string
  descriptionAr: string
  unit: string
  quantity: number
  estimatedUnitPrice: number | null
  estimatedTotalPrice: number | null
  supplierPrices: SupplierPriceCell[]
}

export interface SupplierPriceCell {
  offerId: string
  blindCode: string
  supplierName: string | null
  unitPrice: number
  totalPrice: number
  deviationPercentage: number
  deviationLevel: PriceDeviationLevel
  hasArithmeticError: boolean
}

export interface SupplierTotalSummary {
  offerId: string
  blindCode: string
  supplierName: string | null
  totalAmount: number
  deviationPercentage: number
  deviationLevel: PriceDeviationLevel
  financialRank: number
}

export interface ArithmeticVerificationResult {
  supplierOfferId: string
  supplierBlindCode: string
  totalItems: number
  errorCount: number
  hasErrors: boolean
  errors: ArithmeticError[]
}

export interface ArithmeticError {
  boqItemId: string
  itemNumber: string
  calculatedTotal: number
  supplierSubmittedTotal: number | null
  difference: number
}

export interface FinancialScore {
  id: string
  supplierOfferId: string
  supplierBlindCode: string
  evaluatorUserId: string
  score: number
  maxScore: number
  scorePercentage: number
  notes: string | null
  createdAt: string
}

/* ──────────────────────────────────────────────
 * Committee (from backend CommitteeDto)
 * ────────────────────────────────────────────── */

export interface Committee {
  id: string
  name: string
  type: string
  competitionId: string
  competitionName?: string
  members: CommitteeMember[]
  status: string
  createdAt: string
  updatedAt?: string
}

export interface CommitteeMember {
  id: string
  userId: string
  name: string
  role: 'chair' | 'member' | 'secretary' | 'financial_auditor'
  hasSubmittedScores: boolean
}

/* ──────────────────────────────────────────────
 * Evaluation Criterion (from competition detail)
 * ────────────────────────────────────────────── */

export interface EvaluationCriterion {
  id: string
  name: string
  nameAr?: string
  nameEn?: string
  description: string
  weight: number
  minimumScore: number
  parentId: string | null
  subCriteria: EvaluationCriterion[]
  order: number
}

/* ──────────────────────────────────────────────
 * Vendor (legacy compatibility)
 * ────────────────────────────────────────────── */

export interface Vendor {
  id: string
  code: string
  realName?: string
  offerId: string
  technicalScore?: number
  financialScore?: number
  technicalStatus?: OfferStatus
  financialStatus?: OfferStatus
}

/* ──────────────────────────────────────────────
 * Competition Summary (for evaluation list)
 * ────────────────────────────────────────────── */

export interface CompetitionEvaluation {
  id: string
  competitionNumber: string
  competitionName: string
  projectName: string
  stage: 'technical' | 'financial' | 'completed'
  technicalStatus: EvaluationStatus
  financialStatus: EvaluationStatus
  vendorCount: number
  passedVendorCount: number
  estimatedBudget: number
  deadlineGregorian: string
  deadlineHijri: string
  assignedCommittee: string
  progress: number
}

/* ──────────────────────────────────────────────
 * AI Evaluation (legacy compatibility)
 * ────────────────────────────────────────────── */

export interface AiEvaluation {
  id: string
  criterionId: string
  vendorId: string
  suggestedScore: number
  maxScore: number
  justification: string
  references: string[]
  confidence: number
  createdAt: string
}

/* ──────────────────────────────────────────────
 * Comparison Matrix (legacy compatibility)
 * ────────────────────────────────────────────── */

export interface ComparisonMatrix {
  type: EvaluationType
  criteria: EvaluationCriterion[]
  vendors: Vendor[]
  cells: HeatmapCell[][]
  competitionId: string
  competitionName: string
}

/* ──────────────────────────────────────────────
 * Minutes / Reports
 * ────────────────────────────────────────────── */

export interface EvaluationMinutes {
  id: string
  type: 'technical' | 'financial' | 'comprehensive'
  competitionId: string
  competitionName: string
  committeeId: string
  status: EvaluationStatus
  content: string
  signatures: MinutesSignature[]
  createdAt: string
  approvedAt?: string
}

export interface MinutesSignature {
  memberId: string
  memberName: string
  role: string
  signed: boolean
  signedAt?: string
}

/* ──────────────────────────────────────────────
 * Dual Calendar Date
 * ────────────────────────────────────────────── */

export interface DualDate {
  gregorian: string
  hijri: string
}

/* ──────────────────────────────────────────────
 * Award Recommendation & Final Ranking
 * (Aligned with backend AwardDtos.cs)
 * ────────────────────────────────────────────── */

export const AwardStatus = {
  Draft: 0,
  Generated: 1,
  PendingApproval: 2,
  Approved: 3,
  Rejected: 4,
} as const
export type AwardStatus = (typeof AwardStatus)[keyof typeof AwardStatus]

export interface AwardRanking {
  offerId: string
  supplierName: string
  rank: number
  technicalScore: number
  financialScore: number
  combinedScore: number
  totalOfferAmount: number
}

export interface AwardRecommendation {
  id: string
  competitionId: string
  status: AwardStatus
  recommendedOfferId: string
  recommendedSupplierName: string
  technicalScore: number
  financialScore: number
  combinedScore: number
  totalOfferAmount: number
  justification: string
  approvedAt: string | null
  approvedBy: string | null
  rejectionReason: string | null
  rankings: AwardRanking[]
  createdAt: string
}

export interface FinalRanking {
  competitionId: string
  competitionName: string
  technicalWeight: number
  financialWeight: number
  rankings: AwardRanking[]
  estimatedTotalCost: number
}
