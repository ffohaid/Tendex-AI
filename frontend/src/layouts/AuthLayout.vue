<script setup lang="ts">
/**
 * AuthLayout: A minimal layout for authentication pages (login, MFA, password reset).
 * No sidebar or header, just centered content with branding.
 * Supports RTL/LTR via logical Tailwind properties.
 */
</script>

<template>
  <div class="min-h-screen bg-[var(--color-secondary)] transition-colors duration-200">
    <RouterView />
  </div>
</template>
