<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'

const { t } = useI18n()
const authStore = useAuthStore()

const supportRoute = computed(() => (
  authStore.isAuthenticated
    ? { name: 'SupportTickets' }
    : { name: 'Login', query: { redirect: '/support' } }
))
</script>

<template>
  <footer
    class="flex flex-col items-center justify-between gap-3 border-t border-surface-dim bg-white px-6 py-4 text-sm text-tertiary sm:flex-row"
  >
    <!-- Copyright -->
    <p class="text-center sm:text-start">
      {{ t('footer.copyright') }}
    </p>

    <!-- Footer links -->
    <div class="flex items-center gap-4">
      <router-link
        :to="{ name: 'PrivacyPolicy' }"
        class="transition-colors hover:text-primary"
      >
        {{ t('footer.privacyPolicy') }}
      </router-link>
      <span class="text-surface-dim">|</span>
      <router-link
        :to="{ name: 'TermsOfService' }"
        class="transition-colors hover:text-primary"
      >
        {{ t('footer.termsOfService') }}
      </router-link>
      <span class="text-surface-dim">|</span>
      <router-link
        :to="supportRoute"
        class="transition-colors hover:text-primary"
      >
        {{ t('footer.support') }}
      </router-link>
    </div>

    <!-- AI Year badge (small) -->
    <div class="flex items-center gap-1.5">
      <i class="pi pi-sparkles text-xs text-primary"></i>
      <span class="text-xs text-tertiary">
        {{ t('header.aiYearBadge') }}
      </span>
    </div>
  </footer>
</template>
