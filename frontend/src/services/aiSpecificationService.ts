/**
 * AI Specification Drafting Service
 *
 * Provides API calls for AI-assisted RFP content generation:
 * - Section draft generation with RAG-grounded citations
 * - Section draft refinement based on user feedback
 * - Complete booklet structure generation
 * - BOQ (Bill of Quantities) generation
 * - BOQ refinement
 */
import http from './http'

// ═══════════════════════════════════════════════════════════════
//  Request Types
// ═══════════════════════════════════════════════════════════════

export interface GenerateSectionDraftRequest {
  tenantId: string
  competitionId: string
  projectNameAr: string
  projectDescriptionAr: string
  projectType: string
  estimatedBudget?: number
  sectionType: string
  sectionTitleAr: string
  additionalInstructions?: string
  collectionName?: string
}

export interface RefineSectionDraftRequest {
  tenantId: string
  competitionId: string
  projectNameAr: string
  sectionTitleAr: string
  currentContentHtml: string
  userFeedbackAr: string
  collectionName?: string
}

export interface GenerateBookletStructureRequest {
  tenantId: string
  competitionId: string
  projectNameAr: string
  projectDescriptionAr: string
  projectType: string
  estimatedBudget?: number
  collectionName?: string
}

export interface GenerateBoqRequest {
  tenantId: string
  competitionId: string
  projectNameAr: string
  projectDescriptionAr: string
  projectType: string
  estimatedBudget?: number
  specificationsContentHtml?: string
  additionalInstructions?: string
  collectionName?: string
}

export interface RefineBoqRequest {
  tenantId: string
  competitionId: string
  projectNameAr: string
  existingBoqJson: string
  userFeedbackAr: string
  collectionName?: string
}

// ═══════════════════════════════════════════════════════════════
//  Response Types
// ═══════════════════════════════════════════════════════════════

export interface AiCitation {
  sourceDocument: string
  pageNumber?: number
  relevanceScore: number
  excerpt: string
}

/** Matches backend AiRegulatoryReference record */
export interface AiRegulatoryReference {
  regulationName: string
  articleNumber?: string
  excerpt: string
}

/** Matches backend AiSpecificationDraftResult record */
export interface AiSpecificationDraftResult {
  contentHtml: string
  contentPlainText: string
  citations: AiCitation[]
  regulatoryReferences: AiRegulatoryReference[]
  groundingConfidenceScore: number
  warnings: string[]
  providerName: string
  modelName: string
  latencyMs: number
}

export interface GenerateSectionDraftResult {
  isSuccess: boolean
  errorMessage?: string
  draft?: AiSpecificationDraftResult
}

export interface RefineSectionDraftResult {
  isSuccess: boolean
  errorMessage?: string
  draft?: AiSpecificationDraftResult
}

/** Matches backend ProposedSection record */
export interface ProposedSection {
  titleAr: string
  titleEn: string
  sectionType: string
  isMandatory: boolean
  descriptionAr: string
  sortOrder: number
  subSections?: ProposedSection[]
}

/** Matches backend AiBookletStructureResult record */
export interface AiBookletStructureResult {
  sections: ProposedSection[]
  structureSummaryAr: string
  citations: AiCitation[]
  providerName: string
  modelName: string
  latencyMs: number
}

export interface GenerateBookletStructureResult {
  isSuccess: boolean
  errorMessage?: string
  structure?: AiBookletStructureResult
}

/** Legacy alias kept for backward compatibility in components */
export interface BookletSection {
  sectionTitleAr: string
  sectionType: string
  colorCode: string
  isRequired: boolean
  suggestedContentBrief: string
  orderIndex: number
}

/** Matches backend GeneratedBoqItem record */
export interface GeneratedBoqItem {
  itemNumber: string
  descriptionAr: string
  descriptionEn: string
  unit: string
  quantity: number
  estimatedUnitPrice?: number
  priceEstimateSource?: string
  category?: string
  justificationAr: string
  sortOrder: number
}

/** Matches backend AiBoqGenerationResult record */
export interface AiBoqGenerationResult {
  items: GeneratedBoqItem[]
  summaryAr: string
  totalEstimatedCost?: number
  citations: AiCitation[]
  groundingConfidenceScore: number
  warnings: string[]
  providerName: string
  modelName: string
  latencyMs: number
}

export interface GenerateBoqResult {
  isSuccess: boolean
  errorMessage?: string
  boq?: AiBoqGenerationResult
}

export interface RefineBoqResult {
  isSuccess: boolean
  errorMessage?: string
  boq?: AiBoqGenerationResult
}

/** Legacy alias used in store mapping */
export interface BoqItem {
  itemNumber: string
  descriptionAr: string
  unit: string
  estimatedQuantity: number
  estimatedUnitPrice: number
  estimatedTotalPrice: number
  category: string
  notes?: string
}

// ═══════════════════════════════════════════════════════════════
//  API Functions
// ═══════════════════════════════════════════════════════════════

const BASE_URL = '/v1/ai/specifications'

/**
 * Generate an AI-assisted draft for an RFP booklet section.
 */
/** Extended timeout for AI endpoints (2 minutes) */
const AI_TIMEOUT = 120_000

export async function generateSectionDraft(
  request: GenerateSectionDraftRequest,
): Promise<GenerateSectionDraftResult> {
  const response = await http.post<GenerateSectionDraftResult>(
    `${BASE_URL}/sections/generate`,
    request,
    { timeout: AI_TIMEOUT },
  )
  return response.data
}

/**
 * Refine an existing AI-generated section draft based on user feedback.
 */
export async function refineSectionDraft(
  request: RefineSectionDraftRequest,
): Promise<RefineSectionDraftResult> {
  const response = await http.post<RefineSectionDraftResult>(
    `${BASE_URL}/sections/refine`,
    request,
    { timeout: AI_TIMEOUT },
  )
  return response.data
}

/**
 * Generate a complete RFP booklet structure with mandatory and optional sections.
 */
export async function generateBookletStructure(
  request: GenerateBookletStructureRequest,
): Promise<GenerateBookletStructureResult> {
  const response = await http.post<GenerateBookletStructureResult>(
    `${BASE_URL}/structure/generate`,
    request,
    { timeout: AI_TIMEOUT },
  )
  return response.data
}

/**
 * Generate an AI-assisted Bill of Quantities (BOQ).
 */
export async function generateBoq(
  request: GenerateBoqRequest,
): Promise<GenerateBoqResult> {
  const response = await http.post<GenerateBoqResult>(
    `${BASE_URL}/boq/generate`,
    request,
    { timeout: AI_TIMEOUT },
  )
  return response.data
}

/**
 * Refine an existing AI-generated BOQ based on user feedback.
 */
export async function refineBoq(
  request: RefineBoqRequest,
): Promise<RefineBoqResult> {
  const response = await http.post<RefineBoqResult>(
    `${BASE_URL}/boq/refine`,
    request,
    { timeout: AI_TIMEOUT },
  )
  return response.data
}
