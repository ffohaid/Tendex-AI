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
} from '@/types/rfp'

const BASE_URL = '/v1/competitions'

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
/*  Mapping: Frontend RfpFormData → Backend CreateCompetitionRequest    */
/* ------------------------------------------------------------------ */

interface CreateCompetitionRequest {
  projectNameAr: string
  projectNameEn: string
  description: string | null
  competitionType: number
  creationMethod: number
  estimatedBudget: number | null
  submissionDeadline: string | null
  projectDurationDays: number | null
  sourceTemplateId: string | null
  sourceCompetitionId: string | null
}

function mapToCreateRequest(data: Partial<RfpFormData>): CreateCompetitionRequest {
  const basic = data.basicInfo
  return {
    projectNameAr: basic?.projectName || '',
    projectNameEn: basic?.projectName || '', // Use same name for both until bilingual support is added
    description: basic?.projectDescription || null,
    competitionType: mapCompetitionType(basic?.competitionType),
    creationMethod: 0, // ManualWizard = 0
    estimatedBudget: basic?.estimatedValue || null,
    submissionDeadline: basic?.submissionDeadline || null,
    projectDurationDays: calculateDurationDays(basic?.startDate, basic?.endDate),
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
  submissionDeadline: string | null
  projectDurationDays: number | null
  currentWizardStep: number | null
}

function mapToAutoSaveRequest(data: Partial<RfpFormData>): AutoSaveCompetitionRequest {
  const basic = data.basicInfo
  return {
    projectNameAr: basic?.projectName || null,
    projectNameEn: basic?.projectName || null,
    description: basic?.projectDescription || null,
    competitionType: basic?.competitionType ? mapCompetitionType(basic.competitionType) : null,
    estimatedBudget: basic?.estimatedValue || null,
    submissionDeadline: basic?.submissionDeadline || null,
    projectDurationDays: calculateDurationDays(basic?.startDate, basic?.endDate),
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

function mapFromBackendResponse(dto: Record<string, unknown>): RfpFormData {
  return {
    id: String(dto.id || ''),
    basicInfo: {
      projectName: (dto.projectNameAr as string) || '',
      projectDescription: (dto.description as string) || '',
      competitionType: reverseCompetitionTypeMap[dto.competitionType as number] || 'public_tender',
      estimatedValue: (dto.estimatedBudget as number) || null,
      currency: (dto.currency as string) || 'SAR',
      startDate: '',
      endDate: '',
      submissionDeadline: (dto.submissionDeadline as string) || '',
      referenceNumber: (dto.referenceNumber as string) || '',
      department: '',
      fiscalYear: '',
    },
    settings: {
      evaluationMethod: '',
      technicalWeight: (dto.technicalWeight as number) || 70,
      financialWeight: (dto.financialWeight as number) || 30,
      minimumTechnicalScore: (dto.technicalPassingScore as number) || 60,
      allowPartialOffers: false,
      requireSampleSubmission: false,
      requireSitVisit: false,
      preQualificationRequired: false,
    },
    content: {
      sections: (dto.sections as unknown[])?.map((s: unknown) => {
        const sec = s as Record<string, unknown>
        return {
          id: String(sec.id || ''),
          title: (sec.title as string) || '',
          content: (sec.content as string) || '',
          order: (sec.order as number) || 0,
          isRequired: (sec.isRequired as boolean) || false,
          colorCode: 'default' as const,
          assignedTo: null,
          isCompleted: false,
        }
      }) || [],
    },
    boq: {
      items: (dto.boqItems as unknown[])?.map((b: unknown) => {
        const item = b as Record<string, unknown>
        return {
          id: String(item.id || ''),
          category: (item.category as string) || '',
          description: (item.description as string) || '',
          unit: (item.unit as string) || 'unit',
          quantity: (item.quantity as number) || 0,
          estimatedPrice: (item.estimatedPrice as number) || 0,
          totalPrice: (item.totalPrice as number) || 0,
          notes: (item.notes as string) || '',
          order: (item.order as number) || 0,
        }
      }) || [],
      totalEstimatedCost: 0,
    },
    attachments: {
      files: [],
      maxFileSize: 50,
      allowedTypes: [],
    },
    status: 'draft',
    createdAt: (dto.createdAt as string) || '',
    updatedAt: (dto.lastModifiedAt as string) || '',
    lastAutoSavedAt: (dto.lastAutoSavedAt as string) || null,
    currentStep: (dto.currentWizardStep as number) || 1,
    completionPercentage: 0,
  }
}

/* ------------------------------------------------------------------ */
/*  Utility                                                            */
/* ------------------------------------------------------------------ */

function calculateDurationDays(startDate?: string, endDate?: string): number | null {
  if (!startDate || !endDate) return null
  const start = new Date(startDate)
  const end = new Date(endDate)
  const diffMs = end.getTime() - start.getTime()
  return diffMs > 0 ? Math.ceil(diffMs / (1000 * 60 * 60 * 24)) : null
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

/** Fetch paginated list of competitions (RFPs) */
export async function fetchRfpList(params: {
  page?: number
  pageSize?: number
  status?: string
  search?: string
}): Promise<ApiResponse<PaginatedResponse<RfpListItem>>> {
  return apiCall(() =>
    httpGet<PaginatedResponse<RfpListItem>>(BASE_URL, {
      params: {
        page: params.page || 1,
        pageSize: params.pageSize || 10,
        status: params.status,
        search: params.search,
      },
    }),
  )
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

/** Submit competition for approval */
export async function submitRfpForApproval(
  id: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(async () => {
    const dto = await httpPost<Record<string, unknown>>(`${BASE_URL}/${id}/submit`)
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

/** Clone an existing competition */
export async function cloneRfp(
  sourceId: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(async () => {
    const dto = await httpPost<Record<string, unknown>>(`${BASE_URL}/${sourceId}/clone`)
    return mapFromBackendResponse(dto)
  })
}
