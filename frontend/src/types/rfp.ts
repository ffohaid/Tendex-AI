/**
 * RFP (Request for Proposal) / Specifications Booklet type definitions.
 *
 * These types model the full lifecycle of a competition booklet,
 * from creation through approval, aligned with PRD v6 Section 8.
 */

/** Booklet creation method */
export type RfpCreationMethod = 'wizard' | 'template' | 'clone' | 'ai' | 'upload_extract'

/** Competition status aligned with the 9-stage lifecycle */
export type RfpStatus =
  | 'draft'
  | 'pending_approval'
  | 'approved'
  | 'published'
  | 'receiving_offers'
  | 'technical_evaluation'
  | 'financial_evaluation'
  | 'awarding'
  | 'contracting'
  | 'completed'
  | 'cancelled'

/** Competition type / procurement method */
export type CompetitionType =
  | 'public_tender'
  | 'limited_tender'
  | 'direct_purchase'
  | 'framework_agreement'
  | 'reverse_auction'

/** Evaluation criteria weighting method */
export type EvaluationMethod =
  | 'lowest_price'
  | 'weighted_criteria'
  | 'quality_cost_based'

/** BOQ item unit of measurement */
export type UnitOfMeasure =
  | 'unit'
  | 'meter'
  | 'sqm'
  | 'cbm'
  | 'kg'
  | 'ton'
  | 'liter'
  | 'hour'
  | 'day'
  | 'month'
  | 'year'
  | 'lump_sum'
  | 'trip'
  | 'set'
  | 'person'
  | 'license'

/** Section content color code per PRD template compliance */
export type TextColorCode = 'black' | 'green' | 'red' | 'blue'

/** A single section within the booklet */
export interface RfpSection {
  id: string
  title: string
  content: string
  contentHtml: string
  order: number
  isRequired: boolean
  colorCode: TextColorCode
  assignedTo: string | null
  isCompleted: boolean
}

/** A single item in the Bill of Quantities (BOQ) */
export interface BoqItem {
  id: string
  category: string
  description: string
  unit: UnitOfMeasure
  quantity: number
  estimatedPrice: number
  totalPrice: number
  notes: string
  order: number
}

/** Attachment associated with the booklet */
export interface RfpAttachment {
  id: string
  name: string
  fileUrl: string
  fileSize: number
  fileType: string
  isRequired: boolean
  uploadedAt: string
  uploadedBy: string
}

/** Evaluation criterion */
export interface EvaluationCriterion {
  id: string
  name: string
  weight: number
  description: string
  order: number
}

/** Step 1: Basic information */
export interface RfpBasicInfo {
  projectName: string
  projectDescription: string
  competitionType: CompetitionType | ''
  estimatedValue: number | null
  currency: string
  bookletNumber: string
  department: string
  fiscalYear: string
  bookletIssueDate: string
  inquiriesStartDate: string
  inquiryPeriodDays: number | null
  offersStartDate: string
  submissionDeadline: string
  expectedAwardDate: string
  workStartDate: string
}

/** Step 2: Competition settings */
export interface RfpSettings {
  evaluationMethod: EvaluationMethod | ''
  technicalWeight: number
  financialWeight: number
  minimumTechnicalScore: number
  allowPartialOffers: boolean
  requireBankGuarantee: boolean
  guaranteePercentage: number
  evaluationCriteria: EvaluationCriterion[]
}

/** Step 3: Sections / Content */
export interface RfpContent {
  sections: RfpSection[]
  creationMethod: RfpCreationMethod
  templateId: string | null
  cloneFromId: string | null
}

/** Step 4: BOQ */
export interface RfpBoq {
  items: BoqItem[]
  totalEstimatedValue: number
  includesVat: boolean
  vatPercentage: number
}

/** Step 5: Attachments */
export interface RfpAttachments {
  files: RfpAttachment[]
  /** Keys of required attachment types selected by the user */
  requiredAttachmentTypes: string[]
}

/** Complete RFP form data */
export interface RfpFormData {
  id: string | null
  basicInfo: RfpBasicInfo
  settings: RfpSettings
  content: RfpContent
  boq: RfpBoq
  attachments: RfpAttachments
  status: RfpStatus
  createdAt: string
  updatedAt: string
  lastAutoSavedAt: string | null
  currentStep: number
  completionPercentage: number
}

/** RFP list item (summary for table display) */
export interface RfpListItem {
  id: string
  projectName: string
  referenceNumber: string
  competitionType: CompetitionType
  status: RfpStatus
  estimatedValue: number
  createdAt: string
  updatedAt: string
  createdBy: string
  department: string
  completionPercentage: number
}

/** API response wrapper */
export interface ApiResponse<T> {
  data: T
  success: boolean
  message: string
  errors: string[]
}

/** Paginated list response */
export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

/** Auto-save status */
export type AutoSaveStatus = 'idle' | 'saving' | 'saved' | 'error'

/** Wizard step definition */
export interface WizardStep {
  id: number
  titleKey: string
  descriptionKey: string
  icon: string
  isCompleted: boolean
  isActive: boolean
  isAccessible: boolean
}
