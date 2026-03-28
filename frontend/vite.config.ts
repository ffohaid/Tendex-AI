import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

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
    rollupOptions: {
      output: {
        manualChunks(id: string) {
          if (id.includes('node_modules/vue/') || id.includes('node_modules/vue-router/') || id.includes('node_modules/pinia/')) {
            return 'vendor-vue'
          }
          if (id.includes('node_modules/vue-i18n/')) {
            return 'vendor-i18n'
          }
          if (id.includes('node_modules/chart.js/') || id.includes('node_modules/vue-chartjs/')) {
            return 'vendor-charts'
          }
          if (id.includes('node_modules/primevue/')) {
            return 'vendor-primevue'
          }
        },
      },
    },
    /* Chunk size warning limit */
    chunkSizeWarningLimit: 350,
  },
})
