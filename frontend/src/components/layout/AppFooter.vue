<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
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
      <a
        href="#"
        class="transition-colors hover:text-primary"
      >
        {{ t('footer.privacyPolicy') }}
      </a>
      <span class="text-surface-dim">|</span>
      <a
        href="#"
        class="transition-colors hover:text-primary"
      >
        {{ t('footer.termsOfService') }}
      </a>
      <span class="text-surface-dim">|</span>
      <a
        href="#"
        class="transition-colors hover:text-primary"
      >
        {{ t('footer.support') }}
      </a>
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
