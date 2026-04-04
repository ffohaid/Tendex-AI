<script setup lang="ts">
/**
 * AccessDeniedView.vue
 *
 * Displayed when a user tries to access a page they don't have permission for.
 * Provides a clear message and navigation back to the dashboard.
 */
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

const { t } = useI18n()
const router = useRouter()

function goBack() {
  router.push({ name: 'Dashboard' })
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-surface-50 dark:bg-surface-900 px-4">
    <div class="text-center max-w-lg">
      <!-- Icon -->
      <div class="mx-auto mb-6 w-24 h-24 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center">
        <i class="pi pi-lock text-4xl text-red-500 dark:text-red-400"></i>
      </div>

      <!-- Title -->
      <h1 class="text-3xl font-bold text-surface-900 dark:text-surface-100 mb-3">
        {{ t('errors.accessDenied', 'غير مصرح بالوصول') }}
      </h1>

      <!-- Description -->
      <p class="text-surface-600 dark:text-surface-400 mb-8 text-lg leading-relaxed">
        {{ t('errors.accessDeniedDescription', 'ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة. يرجى التواصل مع مدير النظام إذا كنت تعتقد أن هذا خطأ.') }}
      </p>

      <!-- Action Buttons -->
      <div class="flex flex-col sm:flex-row gap-3 justify-center">
        <button
          @click="goBack"
          class="inline-flex items-center justify-center gap-2 px-6 py-3 bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium transition-colors"
        >
          <i class="pi pi-home text-sm"></i>
          {{ t('errors.backToDashboard', 'العودة للوحة المعلومات') }}
        </button>
        <button
          @click="router.back()"
          class="inline-flex items-center justify-center gap-2 px-6 py-3 bg-surface-200 dark:bg-surface-700 hover:bg-surface-300 dark:hover:bg-surface-600 text-surface-700 dark:text-surface-300 rounded-lg font-medium transition-colors"
        >
          <i class="pi pi-arrow-right text-sm"></i>
          {{ t('errors.goBack', 'العودة للصفحة السابقة') }}
        </button>
      </div>

      <!-- Contact Admin -->
      <p class="mt-8 text-sm text-surface-500 dark:text-surface-500">
        {{ t('errors.contactAdmin', 'إذا كنت بحاجة إلى صلاحيات إضافية، يرجى التواصل مع مدير النظام في جهتك.') }}
      </p>
    </div>
  </div>
</template>
