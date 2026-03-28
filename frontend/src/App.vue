<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useAppStore } from '@/stores/app'
import { watch } from 'vue'
import { useI18n } from 'vue-i18n'

const appStore = useAppStore()
const { locale: i18nLocale } = useI18n()

/**
 * Keep vue-i18n locale in sync with the app store.
 * When the store locale changes, update i18n and document direction.
 */
watch(
  () => appStore.locale,
  (newLocale) => {
    i18nLocale.value = newLocale
  },
  { immediate: true },
)
</script>

<template>
  <RouterView />
</template>
