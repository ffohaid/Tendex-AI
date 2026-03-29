/**
 * Performance Optimization Composable (TASK-703)
 *
 * Provides utilities for frontend performance optimization:
 * - Debounce function for search inputs
 * - Throttle function for scroll/resize handlers
 * - Virtual scrolling helper for large lists
 * - Request deduplication for concurrent API calls
 * - Performance metrics collection
 */

import { ref, onUnmounted } from 'vue'

/**
 * Creates a debounced version of a function.
 * Useful for search inputs to reduce API calls.
 *
 * @param fn - Function to debounce
 * @param delay - Delay in milliseconds (default: 300)
 * @returns Debounced function with cancel method
 */
export function useDebounce<T extends (...args: unknown[]) => unknown>(
  fn: T,
  delay = 300,
) {
  let timer: ReturnType<typeof setTimeout> | null = null

  const debouncedFn = (...args: Parameters<T>) => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => {
      fn(...args)
      timer = null
    }, delay)
  }

  const cancel = () => {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }

  onUnmounted(cancel)

  return { debouncedFn, cancel }
}

/**
 * Creates a throttled version of a function.
 * Useful for scroll and resize event handlers.
 *
 * @param fn - Function to throttle
 * @param limit - Minimum interval in milliseconds (default: 200)
 * @returns Throttled function
 */
export function useThrottle<T extends (...args: unknown[]) => unknown>(
  fn: T,
  limit = 200,
) {
  let inThrottle = false
  let lastArgs: Parameters<T> | null = null

  const throttledFn = (...args: Parameters<T>) => {
    if (!inThrottle) {
      fn(...args)
      inThrottle = true
      setTimeout(() => {
        inThrottle = false
        if (lastArgs) {
          fn(...lastArgs)
          lastArgs = null
        }
      }, limit)
    } else {
      lastArgs = args
    }
  }

  return throttledFn
}

/**
 * Request deduplication for concurrent API calls.
 * Prevents duplicate requests for the same resource.
 *
 * @returns Object with deduplicated fetch function
 */
export function useRequestDedup() {
  const pendingRequests = new Map<string, Promise<unknown>>()

  async function dedupedFetch<T>(
    key: string,
    fetchFn: () => Promise<T>,
  ): Promise<T> {
    if (pendingRequests.has(key)) {
      return pendingRequests.get(key) as Promise<T>
    }

    const promise = fetchFn().finally(() => {
      pendingRequests.delete(key)
    })

    pendingRequests.set(key, promise)
    return promise
  }

  onUnmounted(() => {
    pendingRequests.clear()
  })

  return { dedupedFetch }
}

/**
 * Collects and reports Web Vitals metrics.
 * Useful for monitoring real-user performance.
 *
 * @returns Object with performance metrics
 */
export function usePerformanceMetrics() {
  const metrics = ref<{
    fcp: number | null
    lcp: number | null
    fid: number | null
    cls: number | null
    ttfb: number | null
  }>({
    fcp: null,
    lcp: null,
    fid: null,
    cls: null,
    ttfb: null,
  })

  if (typeof window !== 'undefined' && 'PerformanceObserver' in window) {
    // First Contentful Paint
    try {
      const fcpObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries()
        if (entries.length > 0) {
          metrics.value.fcp = entries[0].startTime
        }
      })
      fcpObserver.observe({ type: 'paint', buffered: true })
    } catch {
      // Observer not supported
    }

    // Largest Contentful Paint
    try {
      const lcpObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries()
        if (entries.length > 0) {
          metrics.value.lcp = entries[entries.length - 1].startTime
        }
      })
      lcpObserver.observe({ type: 'largest-contentful-paint', buffered: true })
    } catch {
      // Observer not supported
    }

    // Time to First Byte
    try {
      const navEntries = performance.getEntriesByType('navigation')
      if (navEntries.length > 0) {
        const navEntry = navEntries[0] as PerformanceNavigationTiming
        metrics.value.ttfb = navEntry.responseStart - navEntry.requestStart
      }
    } catch {
      // Not available
    }
  }

  return { metrics }
}
