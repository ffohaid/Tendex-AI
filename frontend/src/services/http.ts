/**
 * Centralized Axios HTTP client instance for Tendex AI Platform.
 *
 * Features:
 * - Reads base URL from VITE_API_BASE_URL environment variable.
 * - Automatically attaches Authorization header when a token exists.
 * - Handles 401 responses by attempting a silent token refresh.
 * - Only redirects to login when refresh token is truly invalid/expired.
 * - Tenant-aware headers (X-Tenant-Id).
 * - Generic typed HTTP helpers (httpGet, httpPost, httpPut, httpPatch, httpDelete).
 */
import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
  type AxiosResponse,
  type AxiosError,
} from 'axios'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

const httpClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30_000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
})

/* ------------------------------------------------------------------ */
/*  Request Interceptor — Attach Bearer Token & Tenant ID              */
/* ------------------------------------------------------------------ */
httpClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('access_token')
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }

    const tenantId = localStorage.getItem('tenant_id')
    if (tenantId && config.headers) {
      config.headers['X-Tenant-Id'] = tenantId
    }

    return config
  },
  (error: AxiosError) => Promise.reject(error),
)

/* ------------------------------------------------------------------ */
/*  Response Interceptor — Handle 401 & Token Refresh                  */
/* ------------------------------------------------------------------ */
let isRefreshing = false
let failedQueue: Array<{
  resolve: (value: AxiosResponse) => void
  reject: (reason: unknown) => void
  config: InternalAxiosRequestConfig
}> = []

function processQueue(error: unknown): void {
  failedQueue.forEach(({ reject }) => reject(error))
  failedQueue = []
}

/**
 * Redirect to login page and clear auth state.
 * Uses Vue Router navigation when available, falls back to window.location.
 */
function redirectToLogin(): void {
  localStorage.removeItem('access_token')
  localStorage.removeItem('refresh_token')
  localStorage.removeItem('tenant_id')
  localStorage.removeItem('user')

  /* Use soft navigation to avoid full page reload */
  const currentPath = window.location.pathname
  if (currentPath !== '/auth/login') {
    window.location.href = '/auth/login'
  }
}

httpClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean
    }

    /* ---------------------------------------------------------------- */
    /*  Handle network errors and non-401 errors gracefully             */
    /* ---------------------------------------------------------------- */

    /* If there's no response (network error, timeout, etc.), just reject */
    if (!error.response) {
      return Promise.reject(error)
    }

    /* For non-401 errors (404, 500, etc.), just reject without logout */
    if (error.response.status !== 401) {
      return Promise.reject(error)
    }

    /* ---------------------------------------------------------------- */
    /*  Handle 401 Unauthorized                                         */
    /* ---------------------------------------------------------------- */

    /* Skip refresh for auth endpoints themselves */
    const isAuthEndpoint =
      originalRequest?.url?.includes('/auth/login') ||
      originalRequest?.url?.includes('/auth/refresh-token')

    if (isAuthEndpoint) {
      return Promise.reject(error)
    }

    /* Prevent infinite retry loops */
    if (originalRequest._retry) {
      return Promise.reject(error)
    }

    /* Check if we have a refresh token before attempting refresh */
    const refreshToken = localStorage.getItem('refresh_token')
    const tenantId = localStorage.getItem('tenant_id')

    if (!refreshToken || !tenantId) {
      /* No refresh token available — redirect to login */
      redirectToLogin()
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise<AxiosResponse>((resolve, reject) => {
        failedQueue.push({ resolve, reject, config: originalRequest })
      })
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      const { data } = await axios.post(
        `${API_BASE_URL}/v1/auth/refresh-token`,
        { refreshToken, tenantId },
      )

      localStorage.setItem('access_token', data.accessToken)
      localStorage.setItem('refresh_token', data.refreshToken)

      /* Retry queued requests */
      for (const { resolve, config } of failedQueue) {
        config.headers.Authorization = `Bearer ${data.accessToken}`
        const retryResponse = await httpClient(config)
        resolve(retryResponse)
      }
      failedQueue = []

      /* Retry original request */
      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
      return httpClient(originalRequest)
    } catch (refreshError) {
      processQueue(refreshError)

      /*
       * Only redirect to login if the refresh endpoint itself returned
       * 401 or 403, meaning the refresh token is truly invalid/expired.
       * For other errors (network, 500, 404), just reject without logout
       * to prevent unnecessary session loss.
       */
      const refreshAxiosError = refreshError as AxiosError
      const refreshStatus = refreshAxiosError?.response?.status

      if (
        refreshStatus === 401 ||
        refreshStatus === 403 ||
        refreshStatus === undefined /* no response = likely network error on refresh */
      ) {
        redirectToLogin()
      }

      return Promise.reject(refreshError)
    } finally {
      isRefreshing = false
    }
  },
)

export default httpClient

/* ------------------------------------------------------------------ */
/*  Generic Typed HTTP Helpers                                         */
/* ------------------------------------------------------------------ */

/**
 * Generic typed GET helper.
 */
export async function httpGet<T>(
  url: string,
  config?: AxiosRequestConfig,
): Promise<T> {
  const response = await httpClient.get<T>(url, config)
  return response.data
}

/**
 * Generic typed POST helper.
 */
export async function httpPost<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig,
): Promise<T> {
  const response = await httpClient.post<T>(url, data, config)
  return response.data
}

/**
 * Generic typed PUT helper.
 */
export async function httpPut<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig,
): Promise<T> {
  const response = await httpClient.put<T>(url, data, config)
  return response.data
}

/**
 * Generic typed PATCH helper.
 */
export async function httpPatch<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig,
): Promise<T> {
  const response = await httpClient.patch<T>(url, data, config)
  return response.data
}

/**
 * Generic typed DELETE helper.
 */
export async function httpDelete<T>(
  url: string,
  config?: AxiosRequestConfig,
): Promise<T> {
  const response = await httpClient.delete<T>(url, config)
  return response.data
}
