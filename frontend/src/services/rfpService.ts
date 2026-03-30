/**
 * RFP (Competition) API Service.
 *
 * Provides methods to interact with the backend Competition APIs.
 * All data is fetched dynamically — no mock/hardcoded arrays allowed.
 *
 * Uses the centralized httpClient (Axios) for automatic auth token
 * and tenant ID injection.
 */
import httpClient, { httpGet, httpPost, httpPut, httpPatch, httpDelete } from '@/services/http'
import type {
  RfpFormData,
  RfpListItem,
  ApiResponse,
  PaginatedResponse,
  RfpAttachment,
} from '@/types/rfp'

const BASE_URL = '/v1/competitions'

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
    const axiosError = error as { response?: { data?: { message?: string; errors?: string[] }; status?: number }; message?: string }
    const errorBody = axiosError?.response?.data
    return {
      data: null as unknown as T,
      success: false,
      message: errorBody?.message || `خطأ في الاتصال بالخادم (${axiosError?.response?.status || 'unknown'})`,
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
  return apiCall(() => httpGet<RfpFormData>(`${BASE_URL}/${id}`))
}

/** Create a new competition (RFP) */
export async function createRfp(
  data: Partial<RfpFormData>,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(() => httpPost<RfpFormData>(BASE_URL, data))
}

/** Update an existing competition */
export async function updateRfp(
  id: string,
  data: Partial<RfpFormData>,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(() => httpPut<RfpFormData>(`${BASE_URL}/${id}`, data))
}

/** Auto-save draft (lightweight PATCH) */
export async function autoSaveDraft(
  id: string,
  data: Partial<RfpFormData>,
): Promise<ApiResponse<{ savedAt: string }>> {
  return apiCall(() =>
    httpPatch<{ savedAt: string }>(`${BASE_URL}/${id}/auto-save`, data),
  )
}

/** Submit competition for approval */
export async function submitRfpForApproval(
  id: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiCall(() =>
    httpPost<RfpFormData>(`${BASE_URL}/${id}/submit`),
  )
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
  return apiCall(() =>
    httpPost<RfpFormData>(`${BASE_URL}/${sourceId}/clone`),
  )
}
