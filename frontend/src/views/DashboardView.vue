<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <div>
    <!-- Page heading -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-secondary">
        {{ t('nav.dashboard') }}
      </h1>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('app.welcome') }}
      </p>
    </div>

    <!-- Placeholder cards (will be replaced with real data in TASK-503) -->
    <div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
      <div
        v-for="i in 4"
        :key="i"
        class="rounded-xl border border-surface-dim bg-white p-6 shadow-sm"
      >
        <div class="mb-4 flex items-center justify-between">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
            <i class="pi pi-chart-bar text-lg text-primary"></i>
          </div>
        </div>
        <div class="h-4 w-24 animate-pulse rounded bg-surface-dim"></div>
        <div class="mt-2 h-8 w-16 animate-pulse rounded bg-surface-muted"></div>
      </div>
    </div>
  </div>
</template>
