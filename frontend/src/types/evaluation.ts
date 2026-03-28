/**
 * Evaluation system type definitions.
 * Covers technical and financial evaluation workflows,
 * committee management, and AI-assisted scoring.
 */

/* ──────────────────────────────────────────────
 * Enums
 * ────────────────────────────────────────────── */

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

export const HeatmapColor = {
  Excellent: 'excellent',  // >= 80%
  Average: 'average',      // 60-79%
  Weak: 'weak',            // < 60%
} as const
export type HeatmapColor = (typeof HeatmapColor)[keyof typeof HeatmapColor]

/* ──────────────────────────────────────────────
 * Core Entities
 * ────────────────────────────────────────────── */

export interface Committee {
  id: string
  name: string
  type: EvaluationType
  competitionId: string
  competitionName: string
  members: CommitteeMember[]
  status: EvaluationStatus
  createdAt: string
  updatedAt: string
}

export interface CommitteeMember {
  id: string
  userId: string
  name: string
  role: 'chair' | 'member' | 'secretary' | 'financial_auditor'
  hasSubmittedScores: boolean
}

export interface EvaluationCriterion {
  id: string
  name: string
  description: string
  weight: number
  minimumScore: number
  parentId: string | null
  subCriteria: EvaluationCriterion[]
  order: number
}

export interface Vendor {
  id: string
  code: string         // Random anonymous code (e.g., "Vendor A")
  realName?: string    // Only revealed after minutes approval
  offerId: string
  technicalScore?: number
  financialScore?: number
  technicalStatus?: OfferStatus
  financialStatus?: OfferStatus
}

/* ──────────────────────────────────────────────
 * Technical Evaluation
 * ────────────────────────────────────────────── */

export interface TechnicalScore {
  id: string
  criterionId: string
  vendorId: string
  memberId: string
  score: number
  maxScore: number
  notes: string
  submittedAt: string
}

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

export interface VarianceAlert {
  criterionId: string
  criterionName: string
  vendorId: string
  vendorCode: string
  humanScore: number
  aiScore: number
  variancePercent: number
}

/* ──────────────────────────────────────────────
 * Financial Evaluation
 * ────────────────────────────────────────────── */

export interface FinancialOffer {
  id: string
  vendorId: string
  vendorCode: string
  items: FinancialItem[]
  totalAmount: number
  arithmeticValid: boolean
  rank?: number
  deviationFromEstimate?: number
}

export interface FinancialItem {
  id: string
  itemName: string
  unit: string
  quantity: number
  unitPrice: number
  totalPrice: number
  boqPrice?: number // Price from BOQ for comparison
}

export interface FinancialScore {
  id: string
  criterionId: string
  vendorId: string
  memberId: string
  score: number
  maxScore: number
  notes: string
  submittedAt: string
}

/* ──────────────────────────────────────────────
 * Heatmap / Comparison Matrix
 * ────────────────────────────────────────────── */

export interface HeatmapCell {
  vendorId: string
  vendorCode: string
  criterionId: string
  criterionName: string
  score: number
  maxScore: number
  percentage: number
  color: HeatmapColor
  notes: string
  aiScore?: number
  aiJustification?: string
}

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
 * Dual Calendar Date
 * ────────────────────────────────────────────── */

export interface DualDate {
  gregorian: string
  hijri: string
}
