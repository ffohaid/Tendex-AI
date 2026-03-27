<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useAppStore } from '@/stores/app'

const { t } = useI18n()
const appStore = useAppStore()

function switchLanguage(): void {
  const newLocale = appStore.locale === 'ar' ? 'en' : 'ar'
  appStore.setLocale(newLocale)
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-surface-muted">
    <div class="text-center">
      <h1 class="mb-4 text-4xl font-bold text-secondary">
        {{ t('app.welcome') }}
      </h1>
      <p class="mb-6 text-lg text-tertiary">
        {{ t('app.title') }}
      </p>
      <button
        class="rounded-lg bg-primary px-6 py-3 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
        @click="switchLanguage"
      >
        {{ appStore.locale === 'ar' ? 'English' : 'العربية' }}
      </button>
    </div>
  </div>
</template>
