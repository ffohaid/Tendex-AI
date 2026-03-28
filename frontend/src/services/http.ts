/**
 * Centralized Axios HTTP client instance.
 *
 * - Reads base URL from VITE_API_BASE_URL environment variable.
 * - Automatically attaches Authorization header when a token exists.
 * - Handles 401 responses by attempting a silent token refresh.
 */
import axios, {
  type AxiosInstance,
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
/*  Request Interceptor — Attach Bearer Token                          */
/* ------------------------------------------------------------------ */

httpClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('access_token')
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
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

httpClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean
    }

    /* Skip refresh for auth endpoints themselves */
    const isAuthEndpoint =
      originalRequest?.url?.includes('/auth/login') ||
      originalRequest?.url?.includes('/auth/refresh-token')

    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !isAuthEndpoint
    ) {
      if (isRefreshing) {
        return new Promise<AxiosResponse>((resolve, reject) => {
          failedQueue.push({ resolve, reject, config: originalRequest })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const refreshToken = localStorage.getItem('refresh_token')
        const tenantId = localStorage.getItem('tenant_id')

        if (!refreshToken || !tenantId) {
          throw new Error('No refresh token available')
        }

        const { data } = await axios.post(
          `${API_BASE_URL}/v1/auth/refresh-token`,
          { refreshToken, tenantId },
        )

        localStorage.setItem('access_token', data.accessToken)
        localStorage.setItem('refresh_token', data.refreshToken)

        /* Retry queued requests */
        failedQueue.forEach(({ resolve, config }) => {
          config.headers.Authorization = `Bearer ${data.accessToken}`
          resolve(httpClient(config))
        })
        failedQueue = []

        /* Retry original request */
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
        return httpClient(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError)

        /* Clear auth state and redirect to login */
        localStorage.removeItem('access_token')
        localStorage.removeItem('refresh_token')
        localStorage.removeItem('tenant_id')
        localStorage.removeItem('user')

        window.location.href = '/auth/login'
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(error)
  },
)

export default httpClient
