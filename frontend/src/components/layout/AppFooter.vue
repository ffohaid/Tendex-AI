<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

const { t } = useI18n()
const router = useRouter()

function navigateTo(name: 'PrivacyPolicy' | 'TermsOfService' | 'SupportTickets') {
  void router.push({ name })
}
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
      <button
        type="button"
        class="transition-colors hover:text-primary"
        @click="navigateTo('PrivacyPolicy')"
      >
        {{ t('footer.privacyPolicy') }}
      </button>
      <span class="text-surface-dim">|</span>
      <button
        type="button"
        class="transition-colors hover:text-primary"
        @click="navigateTo('TermsOfService')"
      >
        {{ t('footer.termsOfService') }}
      </button>
      <span class="text-surface-dim">|</span>
      <button
        type="button"
        class="transition-colors hover:text-primary"
        @click="navigateTo('SupportTickets')"
      >
        {{ t('footer.support') }}
      </button>
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
