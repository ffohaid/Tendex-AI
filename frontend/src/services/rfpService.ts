/**
 * RFP API Service.
 *
 * Provides methods to interact with the backend RFP APIs (TASK-301).
 * All data is fetched dynamically — no mock/hardcoded arrays allowed.
 *
 * NOTE: API base URL is configured via environment variables.
 * Until the backend is deployed, calls will gracefully degrade.
 */
import type {
  RfpFormData,
  RfpListItem,
  ApiResponse,
  PaginatedResponse,
  RfpAttachment,
} from '@/types/rfp'

const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api'

/**
 * Generic fetch wrapper with error handling.
 */
async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {},
): Promise<ApiResponse<T>> {
  const url = `${API_BASE}${endpoint}`
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'Accept-Language': document.documentElement.lang || 'ar',
    ...((options.headers as Record<string, string>) || {}),
  }

  try {
    const response = await fetch(url, { ...options, headers })

    if (!response.ok) {
      const errorBody = await response.json().catch(() => ({}))
      return {
        data: null as unknown as T,
        success: false,
        message: errorBody.message || `خطأ في الاتصال بالخادم (${response.status})`,
        errors: errorBody.errors || [],
      }
    }

    const data = await response.json()
    return {
      data: data.data ?? data,
      success: true,
      message: data.message || '',
      errors: [],
    }
  } catch (error) {
    return {
      data: null as unknown as T,
      success: false,
      message: 'تعذر الاتصال بالخادم. يرجى التحقق من اتصال الإنترنت.',
      errors: [(error as Error).message],
    }
  }
}

/* ------------------------------------------------------------------ */
/*  RFP CRUD Operations                                               */
/* ------------------------------------------------------------------ */

/** Fetch paginated list of RFPs */
export async function fetchRfpList(params: {
  page?: number
  pageSize?: number
  status?: string
  search?: string
}): Promise<ApiResponse<PaginatedResponse<RfpListItem>>> {
  const query = new URLSearchParams()
  if (params.page) query.set('pageNumber', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  if (params.status) query.set('status', params.status)
  if (params.search) query.set('search', params.search)

  return apiFetch<PaginatedResponse<RfpListItem>>(
    `/rfp?${query.toString()}`,
  )
}

/** Fetch a single RFP by ID */
export async function fetchRfpById(
  id: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiFetch<RfpFormData>(`/rfp/${id}`)
}

/** Create a new RFP (returns the created RFP with ID) */
export async function createRfp(
  data: Partial<RfpFormData>,
): Promise<ApiResponse<RfpFormData>> {
  return apiFetch<RfpFormData>('/rfp', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

/** Update an existing RFP */
export async function updateRfp(
  id: string,
  data: Partial<RfpFormData>,
): Promise<ApiResponse<RfpFormData>> {
  return apiFetch<RfpFormData>(`/rfp/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

/** Auto-save draft (lightweight PATCH) */
export async function autoSaveDraft(
  id: string,
  data: Partial<RfpFormData>,
): Promise<ApiResponse<{ savedAt: string }>> {
  return apiFetch<{ savedAt: string }>(`/rfp/${id}/auto-save`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  })
}

/** Submit RFP for approval */
export async function submitRfpForApproval(
  id: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiFetch<RfpFormData>(`/rfp/${id}/submit`, {
    method: 'POST',
  })
}

/** Delete a draft RFP */
export async function deleteRfp(
  id: string,
): Promise<ApiResponse<void>> {
  return apiFetch<void>(`/rfp/${id}`, {
    method: 'DELETE',
  })
}

/* ------------------------------------------------------------------ */
/*  Attachment Operations                                             */
/* ------------------------------------------------------------------ */

/** Upload an attachment for an RFP */
export async function uploadAttachment(
  rfpId: string,
  file: File,
  isRequired: boolean = false,
): Promise<ApiResponse<RfpAttachment>> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('isRequired', String(isRequired))

  const url = `${API_BASE}/rfp/${rfpId}/attachments`
  try {
    const response = await fetch(url, {
      method: 'POST',
      body: formData,
      headers: {
        'Accept-Language': document.documentElement.lang || 'ar',
      },
    })

    if (!response.ok) {
      const errorBody = await response.json().catch(() => ({}))
      return {
        data: null as unknown as RfpAttachment,
        success: false,
        message: errorBody.message || 'فشل رفع المرفق',
        errors: errorBody.errors || [],
      }
    }

    const data = await response.json()
    return { data, success: true, message: '', errors: [] }
  } catch (error) {
    return {
      data: null as unknown as RfpAttachment,
      success: false,
      message: 'تعذر رفع المرفق. يرجى المحاولة مرة أخرى.',
      errors: [(error as Error).message],
    }
  }
}

/** Delete an attachment */
export async function deleteAttachment(
  rfpId: string,
  attachmentId: string,
): Promise<ApiResponse<void>> {
  return apiFetch<void>(`/rfp/${rfpId}/attachments/${attachmentId}`, {
    method: 'DELETE',
  })
}

/* ------------------------------------------------------------------ */
/*  Template & Clone Operations                                       */
/* ------------------------------------------------------------------ */

/** Fetch available templates */
export async function fetchTemplates(): Promise<
  ApiResponse<{ id: string; name: string; description: string }[]>
> {
  return apiFetch('/rfp/templates')
}

/** Clone an existing RFP */
export async function cloneRfp(
  sourceId: string,
): Promise<ApiResponse<RfpFormData>> {
  return apiFetch<RfpFormData>(`/rfp/${sourceId}/clone`, {
    method: 'POST',
  })
}
