/**
 * Image Optimization Composable (TASK-703)
 *
 * Provides utilities for optimizing image loading performance:
 * - Lazy loading with IntersectionObserver
 * - WebP format detection and fallback
 * - Responsive image srcset generation
 * - Placeholder/blur-up pattern support
 */

import { ref, onMounted, onUnmounted, type Ref } from 'vue'

/**
 * Checks if the browser supports WebP format.
 * Result is cached after first check.
 */
let webpSupportCached: boolean | null = null

export async function supportsWebP(): Promise<boolean> {
  if (webpSupportCached !== null) return webpSupportCached

  return new Promise((resolve) => {
    const img = new Image()
    img.onload = () => {
      webpSupportCached = img.width > 0 && img.height > 0
      resolve(webpSupportCached)
    }
    img.onerror = () => {
      webpSupportCached = false
      resolve(false)
    }
    img.src =
      'data:image/webp;base64,UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA=='
  })
}

/**
 * Composable for lazy loading images using IntersectionObserver.
 *
 * @param rootMargin - Margin around the root for early loading (default: '200px')
 * @returns Object with directive-like functionality for lazy loading
 */
export function useLazyImage(rootMargin = '200px') {
  const observer = ref<IntersectionObserver | null>(null)

  onMounted(() => {
    if ('IntersectionObserver' in window) {
      observer.value = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              const img = entry.target as HTMLImageElement
              const dataSrc = img.getAttribute('data-src')
              if (dataSrc) {
                img.src = dataSrc
                img.removeAttribute('data-src')
              }
              const dataSrcset = img.getAttribute('data-srcset')
              if (dataSrcset) {
                img.srcset = dataSrcset
                img.removeAttribute('data-srcset')
              }
              observer.value?.unobserve(img)
            }
          })
        },
        {
          rootMargin,
          threshold: 0.01,
        },
      )
    }
  })

  onUnmounted(() => {
    observer.value?.disconnect()
  })

  /**
   * Observe an image element for lazy loading.
   */
  function observe(el: HTMLImageElement) {
    if (observer.value) {
      observer.value.observe(el)
    } else {
      // Fallback: load immediately if IntersectionObserver is not supported
      const dataSrc = el.getAttribute('data-src')
      if (dataSrc) {
        el.src = dataSrc
      }
    }
  }

  return { observe }
}

/**
 * Generates responsive image attributes for optimal loading.
 *
 * @param basePath - Base path of the image (without extension)
 * @param extension - Image file extension (default: 'png')
 * @param sizes - Array of width sizes to generate (default: [320, 640, 1024, 1280])
 * @returns Object with src, srcset, and sizes attributes
 */
export function generateResponsiveAttrs(
  basePath: string,
  extension = 'png',
  sizes = [320, 640, 1024, 1280],
) {
  const srcset = sizes
    .map((size) => `${basePath}-${size}w.${extension} ${size}w`)
    .join(', ')

  return {
    src: `${basePath}-${sizes[sizes.length - 1]}w.${extension}`,
    srcset,
    sizes: '(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw',
  }
}

/**
 * Preloads critical images to improve perceived performance.
 *
 * @param urls - Array of image URLs to preload
 */
export function preloadImages(urls: string[]): void {
  urls.forEach((url) => {
    const link = document.createElement('link')
    link.rel = 'preload'
    link.as = 'image'
    link.href = url
    document.head.appendChild(link)
  })
}
