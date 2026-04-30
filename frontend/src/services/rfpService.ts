/**
 * RFP (Competition) API Service.
 *
 * Provides methods to interact with the backend Competition APIs.
 * All data is fetched dynamically — no mock/hardcoded arrays allowed.
 *
 * Uses the centralized httpClient (Axios) for automatic auth token
 * and tenant ID injection.
 *
 * IMPORTANT: The frontend uses a nested RfpFormData structure while the
 * backend expects flat request DTOs. This service handles the mapping.
 */
import httpClient, { httpGet, httpPost, httpPut, httpPatch, httpDelete } from '@/services/http'
import type {
  RfpFormData,
  RfpListItem,
  ApiResponse,
  PaginatedResponse,
  RfpAttachment,
  CompetitionType,
  EvaluationCriterion,
  RfpSection,
  BoqItem,
  UnitOfMeasure,
  TextColorCode,
  RfpCreationMethod,
} from '@/types/rfp'

const BASE_URL = '/v1/competitions'

/* ------------------------------------------------------------------ */
/*  Helper: Strip HTML tags for plain text display                     */
/* ------------------------------------------------------------------ */

function stripHtmlTags(html: string): string {
  if (!html) return ''
  return html
    .replace(/<br\s*\/?>/gi, '\n')
    .replace(/<\/p>/gi, '\n\n')
    .replace(/<\/li>/gi, '\n')
    .replace(/<\/h[1-6]>/gi, '\n\n')
    .replace(/<[^>]+>/g, '')
    .replace(/&nbsp;/g, ' ')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/\n{3,}/g, '\n\n')
    .trim()
}

/* ------------------------------------------------------------------ */
/*  Mapping: Frontend CompetitionType string → Backend enum int        */
/* ------------------------------------------------------------------ */

const competitionTypeMap: Record<string, number> = {
  public_tender: 0,
  limited_tender: 1,
  direct_purchase: 2,
  framework_agreement: 3,
  reverse_auction: 4,
}

function mapCompetitionType(type: CompetitionType | string | undefined): number {
  if (type === undefined || type === '') return 0
  return competitionTypeMap[type] ?? 0
}

/* ------------------------------------------------------------------ */
/*  Mapping: Frontend UnitOfMeasure string → Backend BoqItemUnit int   */
/* ------------------------------------------------------------------ */

const unitOfMeasureToBackend: Record<string, number> = {
  unit: 0,       // Each
  lump_sum: 1,   // LumpSum
  sqm: 2,        // SquareMeter
  meter: 3,      // LinearMeter
  cbm: 4,        // CubicMeter
  kg: 5,         // Kilogram
  ton: 6,        // Ton
  hour: 7,       // Hour
  day: 8,        // Day
  month: 9,      // Month
  year: 10,      // Year
  trip: 11,      // Trip
  set: 12,       // Set
  liter: 13,     // Liter (custom)
  person: 14,    // Person
  license: 15,   // License
}

const backendUnitToFrontend: Record<number, UnitOfMeasure> = {
  0: 'unit',
  1: 'lump_sum',
  2: 'sqm',
  3: 'meter',
  4: 'cbm',
  5: 'kg',
  6: 'ton',
  7: 'hour',
  8: 'day',
  9: 'month',
  10: 'year',
  11: 'trip',
  12: 'set',
}

/* ------------------------------------------------------------------ */
/*  Mapping: Frontend TextColorCode → Backend TextColorType int        */
/* ------------------------------------------------------------------ */

const colorCodeToBackend: Record<string, number> = {
  black: 0,   // Mandatory
  green: 1,   // Editable
  red: 2,     // Example
  blue: 3,    // Instruction
}

const backendColorToFrontend: Record<number, TextColorCode> = {
  0: 'black',
  1: 'green',
  2: 'red',
  3: 'blue',
}

/* ------------------------------------------------------------------ */
/*  Mapping: Frontend RfpFormData → Backend CreateCompetitionRequest    */
/* ------------------------------------------------------------------ */

interface CreateCompetitionRequest {
  projectNameAr: string
  projectNameEn: string
  description: string | null
  competitionType: number
  creationMethod: number
  estimatedBudget: number | null
  bookletNumber: string | null
  bookletIssueDate: string | null
  inquiriesStartDate: string | null
  inquiryPeriodDays: number | null
  offersStartDate: string | null
  submissionDeadline: string | null
  expectedAwardDate: string | null
  workStartDate: string | null
  department: string | null
  fiscalYear: string | null
  sourceTemplateId: string | null
  sourceCompetitionId: string | null
}

function mapToCreateRequest(data: Partial<RfpFormData>): CreateCompetitionRequest {
  const basic = data.basicInfo
  return {
    projectNameAr: basic?.projectName || '',
    projectNameEn: basic?.projectName || '',
    description: basic?.projectDescription || null,
    competitionType: mapCompetitionType(basic?.competitionType),
    creationMethod: 0, // ManualWizard = 0
    estimatedBudget: basic?.estimatedValue || null,
    bookletNumber: basic?.bookletNumber?.trim() || null,
    bookletIssueDate: basic?.bookletIssueDate || null,
    inquiriesStartDate: basic?.inquiriesStartDate || null,
    inquiryPeriodDays: basic?.inquiryPeriodDays ?? null,
    offersStartDate: basic?.offersStartDate || null,
    submissionDeadline: basic?.submissionDeadline || null,
    expectedAwardDate: basic?.expectedAwardDate || null,
    workStartDate: basic?.workStartDate || null,
    department: basic?.department || null,
    fiscalYear: basic?.fiscalYear || null,
    sourceTemplateId: null,
    sourceCompetitionId: null,
  }
}

/* ------------------------------------------------------------------ */
/*  Mapping: Frontend RfpFormData → Backend AutoSaveCompetitionRequest  */
/* ------------------------------------------------------------------ */
interface AutoSaveCompetitionRequest {
  projectNameAr: string | null
  projectNameEn: string | null
  description: string | null
  competitionType: number | null
  estimatedBudget: number | null
  bookletNumber: string | null
  bookletIssueDate: string | null
  inquiriesStartDate: string | null
  inquiryPeriodDays: number | null
  offersStartDate: string | null
  submissionDeadline: string | null
  expectedAwardDate: string | null
  workStartDate: string | null
  department: string | null
  fiscalYear: string | null
  requiredAttachmentTypes: string[] | null
  currentWizardStep: number | null
}

const EXTRACTION_PROJECT_NAME_MAX_LENGTH = 500
const EXTRACTION_DESCRIPTION_MAX_LENGTH = 4000

function normalizeExtractionText(value: string | null | undefined, maxLength: number): string {
  if (!value) {
    return ''
  }

  return value
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, maxLength)
}

function buildCreateRequestFromExtraction(data: {
  projectNameAr: string
  projectNameEn?: string
  description: string | null
  competitionType: number
  estimatedBudget: number | null
  projectDurationDays: number | null
}): CreateCompetitionRequest {
  const projectNameAr = normalizeExtractionText(data.projectNameAr, EXTRACTION_PROJECT_NAME_MAX_LENGTH)
  const projectNameEn = normalizeExtractionText(
    data.projectNameEn || projectNameAr,
    EXTRACTION_PROJECT_NAME_MAX_LENGTH,
  ) || projectNameAr
  const description = normalizeExtractionText(
    data.description,
    EXTRACTION_DESCRIPTION_MAX_LENGTH,
  ) || null
  const estimatedBudget = typeof data.estimatedBudget === 'number' && data.estimatedBudget > 0
    ? data.estimatedBudget
    : null

  return {
    projectNameAr,
    projectNameEn,
    description,
    competitionType: data.competitionType,
    creationMethod: 4, // UploadExtract = 4
    estimatedBudget,
    bookletNumber: null,
    bookletIssueDate: null,
    inquiriesStartDate: null,
    inquiryPeriodDays: null,
    offersStartDate: null,
    submissionDeadline: null,
    expectedAwardDate: null,
    workStartDate: null,
    department: null,
    fiscalYear: null,
    sourceTemplateId: null,
    sourceCompetitionId: null,
  }
}

function mapToAutoSaveRequest(data: Partial<RfpFormData>): AutoSaveCompetitionRequest {
  const basic = data.basicInfo
  const attachments = data.attachments
  return {
    projectNameAr: basic?.projectName || null,
    projectNameEn: basic?.projectName || null,
    description: basic?.projectDescription || null,
    competitionType: basic?.competitionType ? mapCompetitionType(basic.competitionType) : null,
    estimatedBudget: basic?.estimatedValue || null,
    bookletNumber: basic?.bookletNumber?.trim() || null,
    bookletIssueDate: basic?.bookletIssueDate || null,
    inquiriesStartDate: basic?.inquiriesStartDate || null,
    inquiryPeriodDays: basic?.inquiryPeriodDays ?? null,
    offersStartDate: basic?.offersStartDate || null,
    submissionDeadline: basic?.submissionDeadline || null,
    expectedAwardDate: basic?.expectedAwardDate || null,
    workStartDate: basic?.workStartDate || null,
    department: basic?.department || null,
    fiscalYear: basic?.fiscalYear || null,
    requiredAttachmentTypes: attachments?.requiredAttachmentTypes || null,
    currentWizardStep: data.currentStep || null,
  }
}

/* ------------------------------------------------------------------ */
/*  Mapping: Backend CompetitionDetailDto → Frontend RfpFormData        */
/* ------------------------------------------------------------------ */

const reverseCompetitionTypeMap: Record<number, CompetitionType> = {
  0: 'public_tender',
  1: 'limited_tender',
  2: 'direct_purchase',
  3: 'framework_agreement',
  4: 'reverse_auction',
}

const statusIntToString: Record<number, string> = {
  0: 'draft',
  1: 'draft',
  2: 'pending_approval',
  3: 'approved',
  4: 'published',
  5: 'published',
  6: 'receiving_offers',
  7: 'receiving_offers',
  8: 'technical_evaluation',
  9: 'technical_evaluation',
  10: 'financial_evaluation',
  11: 'financial_evaluation',
  12: 'awarding',
  13: 'awarding',
  14: 'contracting',
  15: 'contracting',
  16: 'completed',
  90: 'cancelled',
  91: 'cancelled',
  92: 'cancelled',
}

/** Map frontend status strings back to backend enum values for filtering */
const statusStringToInt: Record<string, number> = {
  'draft': 0,
  'pending_approval': 2,
  'approved': 3,
  'published': 4,
  'receiving_offers': 6,
  'technical_evaluation': 8,
  'financial_evaluation': 10,
  'awarding': 12,
  'contracting': 14,
  'completed': 16,
  'cancelled': 91,
}

const creationMethodIntToString: Record<number, RfpCreationMethod> = {
  0: 'wizard',
  1: 'template',
  2: 'clone',
  3: 'ai',
  4: 'upload_extract',
}

/* ------------------------------------------------------------------ */
/*  Mapping: Backend CompetitionListItemDto → Frontend RfpListItem      */
/* ------------------------------------------------------------------ */

function mapListItemFromBackend(dto: Record<string, unknown>): RfpListItem {
  const sectionsCount = (dto.sectionsCount as number) || 0
  const boqItemsCount = (dto.boqItemsCount as number) || 0
  const attachmentsCount = (dto.attachmentsCount as number) || 0

  // Calculate completion percentage based on available data from list DTO.
  // The list DTO provides counts for sections, BOQ items, and attachments,
  // plus basic fields like projectNameAr. We use a weighted approach:
  // Step 1 (Basic Info): 20% - has project name
  // Step 2 (Settings): 15% - has evaluation criteria (approximated by technicalWeight != default 70)
  // Step 3 (Content): 25% - has sections
  // Step 4 (BOQ): 25% - has BOQ items
  // Step 5 (Attachments): 15% - has attachments
  let completionPercentage = 0
  if (dto.projectNameAr) completionPercentage += 20 // Step 1: Basic info filled
  // Step 2: Settings - check if technicalWeight exists and differs from default,
  // or if sectionsCount > 0 (which implies settings were passed through)
  const techWeight = dto.technicalWeight as number | undefined
  if (techWeight !== undefined && techWeight !== null) {
    completionPercentage += 15 // Step 2: Settings configured
  } else if (sectionsCount > 0) {
    completionPercentage += 15 // If sections exist, user passed through step 2
  }
  if (sectionsCount > 0) completionPercentage += 25 // Step 3: Content added
  if (boqItemsCount > 0) completionPercentage += 25 // Step 4: BOQ items added
  if (attachmentsCount > 0) completionPercentage += 15 // Step 5: Attachments uploaded
  // Cap at 100
  completionPercentage = Math.min(completionPercentage, 100)

  return {
    id: String(dto.id || ''),
    projectName: (dto.projectNameAr as string) || (dto.projectNameEn as string) || '',
    referenceNumber: (dto.referenceNumber as string) || '',
    competitionType: reverseCompetitionTypeMap[dto.competitionType as number] || 'public_tender',
    status: (statusIntToString[dto.status as number] || 'draft') as RfpListItem['status'],
    estimatedValue: (dto.estimatedBudget as number) || 0,
    createdAt: (dto.createdAt as string) || '',
    updatedAt: (dto.lastModifiedAt as string) || '',
    createdBy: (dto.createdBy as string) || '',
    department: '',
    completionPercentage,
  }
}

function mapPagedResponseFromBackend(dto: Record<string, unknown>): PaginatedResponse<RfpListItem> {
  const items = (dto.items as Record<string, unknown>[]) || []
  return {
    items: items.map(mapListItemFromBackend),
    totalCount: (dto.totalCount as number) || 0,
    pageNumber: (dto.pageNumber as number) || 1,
    pageSize: (dto.pageSize as number) || 10,
    totalPages: (dto.totalPages as number) || 0,
  }
}

function mapFromBackendResponse(dto: Record<string, unknown>): RfpFormData {
  // Map evaluation criteria from backend
  const evaluationCriteria: EvaluationCriterion[] = ((dto.evaluationCriteria as unknown[]) || []).map((c: unknown) => {
    const crit = c as Record<string, unknown>
    return {
      id: String(crit.id || ''),
      name: (crit.nameAr as string) || (crit.nameEn as string) || '',
      weight: (crit.weightPercentage as number) || 0,
      description: (crit.descriptionAr as string) || (crit.descriptionEn as string) || '',
      order: (crit.sortOrder as number) || 0,
    }
  })

  // Map sections from backend
  const sections: RfpSection[] = ((dto.sections as unknown[]) || []).map((s: unknown) => {
    const sec = s as Record<string, unknown>
    const htmlContent = (sec.contentHtml as string) || ''
    const plainContent = (sec.contentPlainText as string) || ''
    // Use plain text for textarea display; fall back to stripped HTML
    const displayContent = plainContent || stripHtmlTags(htmlContent)
    return {
      id: String(sec.id || ''),
      title: (sec.titleAr as string) || (sec.titleEn as string) || '',
      content: displayContent,
      contentHtml: htmlContent,
      order: (sec.sortOrder as number) || 0,
      isRequired: (sec.isMandatory as boolean) || false,
      colorCode: backendColorToFrontend[(sec.defaultTextColor as number) ?? 1] || 'green',
      assignedTo: (sec.assignedToUserId as string) || null,
      isCompleted: !!(htmlContent || plainContent),
    }
  })

  // Map BOQ items from backend
  const boqItems: BoqItem[] = ((dto.boqItems as unknown[]) || []).map((b: unknown) => {
    const item = b as Record<string, unknown>
    const quantity = (item.quantity as number) || 0
    const unitPrice = (item.estimatedUnitPrice as number) || 0
    return {
      id: String(item.id || ''),
      category: (item.category as string) || '',
      description: (item.descriptionAr as string) || (item.descriptionEn as string) || '',
      unit: backendUnitToFrontend[(item.unit as number) ?? 0] || 'unit',
      quantity,
      estimatedPrice: unitPrice,
      totalPrice: (item.estimatedTotalPrice as number) || (quantity * unitPrice),
      notes: '',
      order: (item.sortOrder as number) || 0,
    }
  })

  // Map attachments from backend
  const attachments: RfpAttachment[] = ((dto.attachments as unknown[]) || []).map((a: unknown) => {
    const att = a as Record<string, unknown>
    return {
      id: String(att.id || ''),
      name: (att.fileName as string) || '',
      fileUrl: (att.fileObjectKey as string) || '',
      fileSize: (att.fileSizeBytes as number) || 0,
      fileType: (att.contentType as string) || '',
      isRequired: (att.isMandatory as boolean) || false,
      uploadedAt: (att.createdAt as string) || '',
      uploadedBy: (att.createdBy as string) || '',
    }
  })

  // Determine evaluation method based on weights
  const techWeight = (dto.technicalWeight as number) || 70
  const finWeight = (dto.financialWeight as number) || 30
  let evaluationMethod: string = ''
  if (techWeight === 0 && finWeight === 100) {
    evaluationMethod = 'lowest_price'
  } else if (techWeight > 0 && finWeight > 0) {
    evaluationMethod = 'weighted_criteria'
  } else if (evaluationCriteria.length > 0) {
    evaluationMethod = 'quality_cost_based'
  }

  // Calculate completion percentage
  let completedSteps = 0
  if (dto.projectNameAr) completedSteps++
  if (evaluationCriteria.length > 0 || techWeight !== 70) completedSteps++
  if (sections.length > 0) completedSteps++
  if (boqItems.length > 0) completedSteps++
  if (attachments.length > 0) completedSteps++
  const completionPercentage = Math.round((completedSteps / 5) * 100)

  // Read dates and additional fields from backend
  const toDateInput = (value: unknown) => {
    if (!value || typeof value !== 'string') return ''
    return value.split('T')[0]
  }

  const department = (dto.department as string) || ''
  const fiscalYear = (dto.fiscalYear as string) || ''

  return {
    id: String(dto.id || ''),
    basicInfo: {
      projectName: (dto.projectNameAr as string) || '',
      projectDescription: (dto.description as string) || '',
      competitionType: reverseCompetitionTypeMap[dto.competitionType as number] || 'public_tender',
      estimatedValue: (dto.estimatedBudget as number) || null,
      currency: (dto.currency as string) || 'SAR',
      bookletNumber: (dto.referenceNumber as string) || '',
      department,
      fiscalYear,
      bookletIssueDate: toDateInput(dto.bookletIssueDate ?? dto.startDate),
      inquiriesStartDate: toDateInput(dto.inquiriesStartDate),
      inquiryPeriodDays: (dto.inquiryPeriodDays as number) || null,
      offersStartDate: toDateInput(dto.offersStartDate),
      submissionDeadline: toDateInput(dto.submissionDeadline),
      expectedAwardDate: toDateInput(dto.expectedAwardDate),
      workStartDate: toDateInput(dto.workStartDate),
    },
    settings: {
      evaluationMethod: evaluationMethod as RfpFormData['settings']['evaluationMethod'],
      technicalWeight: techWeight,
      financialWeight: finWeight,
      minimumTechnicalScore: (dto.technicalPassingScore as number) || 60,
      allowPartialOffers: false,
      requireBankGuarantee: true,
      guaranteePercentage: 5,
      evaluationCriteria,
    },
    content: {
      sections,
      creationMethod: creationMethodIntToString[(dto.creationMethod as number) ?? 0] || 'wizard',
      templateId: (dto.sourceTemplateId as string) || null,
      cloneFromId: (dto.sourceCompetitionId as string) || null,
    },
    boq: {
      items: boqItems,
      totalEstimatedValue: boqItems.reduce((sum, item) => sum + item.totalPrice, 0),
      includesVat: true,
      vatPercentage: 15,
    },
    attachments: {
      files: attachments,
      requiredAttachmentTypes: (dto.requiredAttachmentTypes as string[]) || [],
    },
    status: (statusIntToString[dto.status as number] || 'draft') as RfpFormData['status'],
    createdAt: (dto.createdAt as string) || '',
    updatedAt: (dto.lastModifiedAt as string) || '',
    lastAutoSavedAt: (dto.lastAutoSavedAt as string) || null,
    currentStep: (dto.currentWizardStep as number) || 1,
    completionPercentage,
  }
}

/* ------------------------------------------------------------------ */
/*  Utility                                                            */
/* ------------------------------------------------------------------ */

interface BookletNumberAvailabilityDto {
  isAvailable: boolean
  message?: string
}

/* ------------------------------------------------------------------ */
/*  Helper: Wrap httpClient responses into ApiResponse format          */
/* ------------------------------------------------------------------ */

async function apiCall<T>(fn: () => Promise<T>): Promise<ApiResponse<T>> {
  try {
    const data = await fn()
    return {
      data,
      success: true,
      message: '',
      errors: [],
    }
  } catch (error: unknown) {
    const axiosError = error as { response?: { data?: { message?: string; detail?: string; errors?: string[] }; status?: number }; message?: string }
    const errorBody = axiosError?.response?.data
    return {
      data: null as unknown as T,
      success: false,
      message: errorBody?.detail || errorBody?.message || `خطأ في الاتصال بالخادم (${axiosError?.response?.status || 'unknown'})`,
      errors: errorBody?.errors || [axiosError?.message || 'Unknown error'],
    }
  }
}

/* ------------------------------------------------------------------ */
/*  Competition CRUD Operations                                       */
/* ------------------------------------------------------------------ */

export async function checkBookletNumberAvailability(
  bookletNumber: string,
  competitionId?: string | null,
): Promise<ApiResponse<BookletNumberAvailabilityDto>> {
  const query = new URLSearchParams({ bookletNumber })
  if (competitionId) {
    query.set('competitionId', competitionId)
  }

  return apiCall(async () => {
    const dto = await httpGet<Record<string, unknown>>(`${BASE_URL}/booklet-number-availability?${query.toString()}`)
    return {
      isAvailable: Boolean(dto.isAvailable),
      message: (dto.message as string) || undefined,
    }
  })
}

/** Fetch paginated list of competitions (RFPs) */
export async function fetchRfpList(params: {
  page?: number
  pageSize?: number
  status?: string
  search?: string
}): Promise<ApiResponse<PaginatedResponse<RfpListItem>>> {
  return apiCall(async () => {
    const dto = await httpGet<Record<string, unknown>>(BASE_URL, {
      params: {
        page: params.page || 1,
        pageSize: params.pageSize || 10,
        status: params.status ? statusStringToInt[params.status] : undefined,
        search: params.search,
      },
    })
    return mapPagedResponseFromBackend(dto)
  })
}

/** Fetch a single competition by ID */
export async function fetchRfpById(
  id: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(async () => {
    const dto = await httpGet<Record<string, unknown>>(`${BASE_URL}/${id}`)
    return mapFromBackendResponse(dto)
  })
}

/** Create a new competition (RFP) */
export async function createRfp(
  data: Partial<RfpFormData>,
): Promise<ApiResponse<RfpFormData>> {
  const request = mapToCreateRequest(data)
  return apiCall(async () => {
    const dto = await httpPost<Record<string, unknown>>(BASE_URL, request)
    return mapFromBackendResponse(dto)
  })
}

/** Update an existing competition */
export async function updateRfp(
  id: string,
  data: Partial<RfpFormData>,
): Promise<ApiResponse<RfpFormData>> {
  const request = mapToCreateRequest(data)
  return apiCall(async () => {
    const dto = await httpPut<Record<string, unknown>>(`${BASE_URL}/${id}`, request)
    return mapFromBackendResponse(dto)
  })
}

/** Auto-save draft (lightweight PATCH) */
export async function autoSaveDraft(
  id: string,
  data: Partial<RfpFormData>,
): Promise<ApiResponse<{ savedAt: string }>> {
  const request = mapToAutoSaveRequest(data)
  return apiCall(() =>
    httpPatch<{ savedAt: string }>(`${BASE_URL}/${id}/auto-save`, request),
  )
}

/** Submit competition for approval.
 *  Handles the two-step transition: Draft(0) → UnderPreparation(1) → PendingApproval(2).
 *  If the competition is already in UnderPreparation, it transitions directly to PendingApproval.
 */
export async function submitRfpForApproval(
  id: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(async () => {
    // First, get current competition status
    const current = await httpGet<Record<string, unknown>>(`${BASE_URL}/${id}`)
    const currentStatus = (current as Record<string, unknown>).status as number

    // If in Draft (0), transition to UnderPreparation (1) first
    if (currentStatus === 0) {
      await httpPost<Record<string, unknown>>(`${BASE_URL}/${id}/status`, {
        newStatus: 1, // UnderPreparation
        reason: null,
      })
    }

    // Now transition to PendingApproval (2)
    const dto = await httpPost<Record<string, unknown>>(`${BASE_URL}/${id}/status`, {
      newStatus: 2, // PendingApproval
      reason: null,
    })
    return mapFromBackendResponse(dto)
  })
}

/** Delete a draft competition */
export async function deleteRfp(
  id: string,
): Promise<ApiResponse<void>> {
  return apiCall(() => httpDelete<void>(`${BASE_URL}/${id}`))
}

/* ------------------------------------------------------------------ */
/*  Step 2: Evaluation Settings & Criteria Operations                  */
/* ------------------------------------------------------------------ */

/** Update evaluation settings (weights and passing score) */
export async function saveEvaluationSettings(
  competitionId: string,
  settings: {
    technicalWeight: number
    financialWeight: number
    minimumTechnicalScore: number
  },
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(async () => {
    const dto = await httpPut<Record<string, unknown>>(
      `${BASE_URL}/${competitionId}/evaluation-settings`,
      {
        technicalPassingScore: settings.minimumTechnicalScore,
        technicalWeight: settings.technicalWeight,
        financialWeight: settings.financialWeight,
      },
    )
    return mapFromBackendResponse(dto)
  })
}

/** Add a single evaluation criterion */
export async function addEvaluationCriterion(
  competitionId: string,
  criterion: EvaluationCriterion,
): Promise<ApiResponse<Record<string, unknown>>> {
  return apiCall(() =>
    httpPost<Record<string, unknown>>(
      `${BASE_URL}/${competitionId}/evaluation-criteria`,
      {
        nameAr: criterion.name,
        nameEn: criterion.name,
        descriptionAr: criterion.description || null,
        descriptionEn: criterion.description || null,
        weightPercentage: criterion.weight,
        minimumPassingScore: null,
        parentCriterionId: null,
      },
    ),
  )
}

/** Save all evaluation criteria (batch: delete existing + add new) */
export async function saveAllEvaluationCriteria(
  competitionId: string,
  criteria: EvaluationCriterion[],
): Promise<ApiResponse<void>> {
  try {
    // Add each criterion one by one (backend supports individual adds)
    for (const criterion of criteria) {
      // Skip criteria that already have a valid UUID (already saved)
      if (criterion.id && criterion.id.match(/^[0-9a-f]{8}-/)) {
        continue
      }
      const result = await addEvaluationCriterion(competitionId, criterion)
      if (!result.success) {
        return {
          data: undefined as unknown as void,
          success: false,
          message: result.message,
          errors: result.errors,
        }
      }
    }
    return { data: undefined as unknown as void, success: true, message: '', errors: [] }
  } catch (error: unknown) {
    return {
      data: undefined as unknown as void,
      success: false,
      message: 'فشل في حفظ معايير التقييم',
      errors: [(error as Error).message],
    }
  }
}

/* ------------------------------------------------------------------ */
/*  Step 3: Section Operations                                         */
/* ------------------------------------------------------------------ */

/** Add a single section to the competition */
export async function addRfpSection(
  competitionId: string,
  section: RfpSection,
): Promise<ApiResponse<Record<string, unknown>>> {
  return apiCall(() =>
    httpPost<Record<string, unknown>>(
      `${BASE_URL}/${competitionId}/sections`,
      {
        titleAr: section.title,
        titleEn: section.title,
        sectionType: 6, // Custom
        contentHtml: section.contentHtml || section.content || null,
        contentPlainText: section.content || null,
        isMandatory: section.isRequired || false,
        isFromTemplate: false,
        defaultTextColor: colorCodeToBackend[section.colorCode] ?? 1,
        parentSectionId: null,
      },
    ),
  )
}

/** Update an existing section */
export async function updateRfpSection(
  competitionId: string,
  sectionId: string,
  section: Partial<RfpSection>,
): Promise<ApiResponse<Record<string, unknown>>> {
  return apiCall(() =>
    httpPut<Record<string, unknown>>(
      `${BASE_URL}/${competitionId}/sections/${sectionId}`,
      {
        titleAr: section.title || null,
        titleEn: section.title || null,
        contentHtml: section.contentHtml || section.content || null,
        contentPlainText: section.content || null,
      },
    ),
  )
}

/** Save all sections using batch endpoint (single transaction) */
export async function saveAllSections(
  competitionId: string,
  sections: RfpSection[],
  clearExisting: boolean = false,
): Promise<ApiResponse<void>> {
  try {
    // Separate existing sections (update individually) from new ones (batch add)
    const existingSections = sections.filter(
      (s) => s.id && s.id.match(/^[0-9a-f]{8}-/),
    )
    const newSections = sections.filter(
      (s) => !s.id || !s.id.match(/^[0-9a-f]{8}-/),
    )

    // Update existing sections individually
    if (!clearExisting) {
      for (const section of existingSections) {
        const result = await updateRfpSection(competitionId, section.id, section)
        if (!result.success) {
          return {
            data: undefined as unknown as void,
            success: false,
            message: result.message,
            errors: result.errors,
          }
        }
      }
    }

    // Batch add new sections (or all sections if clearExisting)
    const sectionsToSend = clearExisting ? sections : newSections

    if (sectionsToSend.length > 0) {
      const batchPayload = {
        sections: sectionsToSend.map((section) => ({
          titleAr: section.title,
          titleEn: section.title,
          sectionType: 6, // Custom
          contentHtml: section.contentHtml || section.content || null,
          contentPlainText: section.content || null,
          isMandatory: section.isRequired || false,
          isFromTemplate: false,
          defaultTextColor: colorCodeToBackend[section.colorCode] ?? 1,
          parentSectionId: null,
        })),
        clearExisting,
      }

      const result = await apiCall(() =>
        httpPost<Record<string, unknown>[]>(
          `${BASE_URL}/${competitionId}/sections/batch`,
          batchPayload,
        ),
      )

      if (!result.success) {
        return {
          data: undefined as unknown as void,
          success: false,
          message: result.message,
          errors: result.errors,
        }
      }
    }

    return { data: undefined as unknown as void, success: true, message: '', errors: [] }
  } catch (error: unknown) {
    return {
      data: undefined as unknown as void,
      success: false,
      message: 'فشل في حفظ أقسام الكراسة',
      errors: [(error as Error).message],
    }
  }
}

/* ------------------------------------------------------------------ */
/*  Step 4: BOQ Item Operations                                        */
/* ------------------------------------------------------------------ */

/** Add a single BOQ item */
export async function addBoqItem(
  competitionId: string,
  item: BoqItem,
  index: number,
): Promise<ApiResponse<Record<string, unknown>>> {
  return apiCall(() =>
    httpPost<Record<string, unknown>>(
      `${BASE_URL}/${competitionId}/boq-items`,
      {
        itemNumber: String(index + 1),
        descriptionAr: item.description,
        descriptionEn: item.description,
        unit: unitOfMeasureToBackend[item.unit] ?? 0,
        quantity: item.quantity,
        estimatedUnitPrice: item.estimatedPrice || null,
        category: item.category || null,
      },
    ),
  )
}

/** Save all BOQ items using batch endpoint (single transaction) */
export async function saveAllBoqItems(
  competitionId: string,
  items: BoqItem[],
  clearExisting: boolean = false,
): Promise<ApiResponse<void>> {
  try {
    /**
     * Issue 24 Fix: Always send all items with clearExisting=true.
     * Previously, items with valid UUIDs were filtered out, causing
     * the BOQ table to appear empty after save because existing items
     * were skipped and no new items were sent. By always clearing and
     * re-creating, we ensure the full BOQ state is persisted.
     */
    const itemsToSend = items

    if (itemsToSend.length === 0) {
      return { data: undefined as unknown as void, success: true, message: '', errors: [] }
    }

    // Always clear existing to ensure full sync
    clearExisting = true

    const batchPayload = {
      items: itemsToSend.map((item, index) => ({
        itemNumber: String(index + 1),
        descriptionAr: item.description,
        descriptionEn: item.description,
        unit: unitOfMeasureToBackend[item.unit] ?? 0,
        quantity: item.quantity,
        estimatedUnitPrice: item.estimatedPrice || null,
        category: item.category || null,
      })),
      clearExisting,
    }

    const result = await apiCall(() =>
      httpPost<Record<string, unknown>[]>(
        `${BASE_URL}/${competitionId}/boq-items/batch`,
        batchPayload,
      ),
    )

    if (!result.success) {
      return {
        data: undefined as unknown as void,
        success: false,
        message: result.message,
        errors: result.errors,
      }
    }

    return { data: undefined as unknown as void, success: true, message: '', errors: [] }
  } catch (error: unknown) {
    return {
      data: undefined as unknown as void,
      success: false,
      message: 'فشل في حفظ جدول الكميات',
      errors: [(error as Error).message],
    }
  }
}

/* ------------------------------------------------------------------ */
/*  Attachment Operations                                             */
/* ------------------------------------------------------------------ */

/** Upload an attachment for a competition */
export async function uploadAttachment(
  rfpId: string,
  file: File,
  isRequired: boolean = false,
): Promise<ApiResponse<RfpAttachment>> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('isRequired', String(isRequired))

  return apiCall(async () => {
    const { data } = await httpClient.post<RfpAttachment>(
      `${BASE_URL}/${rfpId}/attachments`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      },
    )
    return data
  })
}

/** Delete an attachment */
export async function deleteAttachment(
  rfpId: string,
  attachmentId: string,
): Promise<ApiResponse<void>> {
  return apiCall(() =>
    httpDelete<void>(`${BASE_URL}/${rfpId}/attachments/${attachmentId}`),
  )
}

/* ------------------------------------------------------------------ */
/*  Template & Clone Operations                                       */
/* ------------------------------------------------------------------ */

/** Fetch available templates */
export async function fetchTemplates(): Promise<
  ApiResponse<{ id: string; name: string; description: string }[]>
> {
  return apiCall(() =>
    httpGet<{ id: string; name: string; description: string }[]>(
      `${BASE_URL}/templates`,
    ),
  )
}

/** Create a new competition from extraction results (Upload & Extract method) */
export async function createRfpFromExtraction(
  data: {
    projectNameAr: string
    projectNameEn?: string
    description: string | null
    competitionType: number
    estimatedBudget: number | null
    projectDurationDays: number | null
  },
): Promise<ApiResponse<RfpFormData>> {
  const request = buildCreateRequestFromExtraction(data)

  return apiCall(async () => {
    const dto = await httpPost<Record<string, unknown>>(BASE_URL, request)
    return mapFromBackendResponse(dto)
  })
}

/** Clone an existing competition */
export async function cloneRfp(
  sourceId: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(async () => {
    const dto = await httpPost<Record<string, unknown>>(`${BASE_URL}/${sourceId}/clone`)
    return mapFromBackendResponse(dto)
  })
}

/* ------------------------------------------------------------------ */
/*  Comprehensive Step Save (called when moving between steps)         */
/* ------------------------------------------------------------------ */

/**
 * Save all data for a specific step to the backend.
 * Called by the wizard when the user clicks "Next" or "Save Draft".
 */
export async function saveStepData(
  competitionId: string,
  step: number,
  formData: RfpFormData,
): Promise<ApiResponse<void>> {
  const emptySuccess: ApiResponse<void> = {
    data: undefined as unknown as void,
    success: true,
    message: '',
    errors: [],
  }

  if (!competitionId) {
    return {
      data: undefined as unknown as void,
      success: false,
      message: 'يجب حفظ الكراسة أولاً',
      errors: ['Competition ID is required'],
    }
  }

  try {
    switch (step) {
      case 1: {
        // Step 1: Basic info is handled by auto-save
        const autoSaveResult = await autoSaveDraft(competitionId, formData)
        if (!autoSaveResult.success) {
          return {
            data: undefined as unknown as void,
            success: false,
            message: autoSaveResult.message,
            errors: autoSaveResult.errors,
          }
        }
        return emptySuccess
      }
      case 2: {
        // Step 2: Save evaluation settings + criteria
        const settingsResult = await saveEvaluationSettings(competitionId, {
          technicalWeight: formData.settings.technicalWeight,
          financialWeight: formData.settings.financialWeight,
          minimumTechnicalScore: formData.settings.minimumTechnicalScore,
        })
        if (!settingsResult.success) {
          return {
            data: undefined as unknown as void,
            success: false,
            message: settingsResult.message,
            errors: settingsResult.errors,
          }
        }
        // Save criteria
        if (formData.settings.evaluationCriteria.length > 0) {
          const criteriaResult = await saveAllEvaluationCriteria(
            competitionId,
            formData.settings.evaluationCriteria,
          )
          if (!criteriaResult.success) {
            return criteriaResult
          }
        }
        // Update wizard step
        await autoSaveDraft(competitionId, { ...formData, currentStep: 3 })
        return emptySuccess
      }
      case 3: {
        // Step 3: Save sections
        if (formData.content.sections.length > 0) {
          const sectionsResult = await saveAllSections(
            competitionId,
            formData.content.sections,
          )
          if (!sectionsResult.success) {
            return sectionsResult
          }
        }
        // Update wizard step
        await autoSaveDraft(competitionId, { ...formData, currentStep: 4 })
        return emptySuccess
      }
      case 4: {
        // Step 4: Save BOQ items
        if (formData.boq.items.length > 0) {
          const boqResult = await saveAllBoqItems(
            competitionId,
            formData.boq.items,
          )
          if (!boqResult.success) {
            return boqResult
          }
        }
        // Update wizard step
        await autoSaveDraft(competitionId, { ...formData, currentStep: 5 })
        return emptySuccess
      }
      case 5: {
        // Step 5: Attachments are handled individually via upload
        // Just update wizard step
        await autoSaveDraft(competitionId, { ...formData, currentStep: 6 })
        return emptySuccess
      }
      default:
        return emptySuccess
    }
  } catch (error: unknown) {
    return {
      data: undefined as unknown as void,
      success: false,
      message: 'فشل في حفظ البيانات',
      errors: [(error as Error).message],
    }
  }
}
