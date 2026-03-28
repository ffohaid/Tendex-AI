import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

/**
 * Vite Configuration for Tendex AI Frontend
 *
 * Performance optimizations (TASK-703):
 * - Manual chunk splitting for vendor libraries to maximize cache efficiency.
 * - Terser minification with aggressive compression for smaller bundles.
 * - CSS code splitting enabled for per-route CSS loading.
 * - Source maps disabled in production for smaller output.
 * - Asset inlining threshold set to 4KB to reduce HTTP requests.
 * - Preload directives for dynamic imports.
 */
export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    allowedHosts: true,
  },
  build: {
    /* Performance optimizations */
    target: 'es2020',
    cssCodeSplit: true,
    sourcemap: false,

    /* Asset inlining threshold: inline assets smaller than 4KB as base64 */
    assetsInlineLimit: 4096,

    /* Minification configuration */
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true,
        pure_funcs: ['console.log', 'console.debug', 'console.info'],
        passes: 2,
      },
      mangle: {
        safari10: true,
      },
      format: {
        comments: false,
      },
    },

    rollupOptions: {
      output: {
        /* Optimized chunk splitting strategy */
        manualChunks(id: string) {
          /* Core Vue ecosystem - changes rarely, cached long-term */
          if (
            id.includes('node_modules/vue/') ||
            id.includes('node_modules/@vue/') ||
            id.includes('node_modules/vue-router/') ||
            id.includes('node_modules/pinia/')
          ) {
            return 'vendor-vue'
          }

          /* Internationalization - separate chunk for i18n */
          if (id.includes('node_modules/vue-i18n/')) {
            return 'vendor-i18n'
          }

          /* Chart libraries - only loaded on dashboard/evaluation pages */
          if (
            id.includes('node_modules/chart.js/') ||
            id.includes('node_modules/vue-chartjs/')
          ) {
            return 'vendor-charts'
          }

          /* PrimeVue UI components - large library, separate chunk */
          if (
            id.includes('node_modules/primevue/') ||
            id.includes('node_modules/@primeuix/') ||
            id.includes('node_modules/primeicons/')
          ) {
            return 'vendor-primevue'
          }

          /* Form validation libraries */
          if (
            id.includes('node_modules/vee-validate/') ||
            id.includes('node_modules/@vee-validate/') ||
            id.includes('node_modules/zod/')
          ) {
            return 'vendor-forms'
          }

          /* HTTP client */
          if (id.includes('node_modules/axios/')) {
            return 'vendor-http'
          }

          /* Drag and drop */
          if (id.includes('node_modules/vuedraggable/') || id.includes('node_modules/sortablejs/')) {
            return 'vendor-dnd'
          }
        },

        /* Consistent file naming for long-term caching */
        chunkFileNames: 'assets/js/[name]-[hash].js',
        entryFileNames: 'assets/js/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          const name = assetInfo.name || ''
          if (/\.(png|jpe?g|gif|svg|webp|avif|ico)$/.test(name)) {
            return 'assets/images/[name]-[hash][extname]'
          }
          if (/\.(woff2?|eot|ttf|otf)$/.test(name)) {
            return 'assets/fonts/[name]-[hash][extname]'
          }
          if (/\.css$/.test(name)) {
            return 'assets/css/[name]-[hash][extname]'
          }
          return 'assets/[name]-[hash][extname]'
        },
      },
    },

    /* Chunk size warning limit */
    chunkSizeWarningLimit: 350,
  },
})
