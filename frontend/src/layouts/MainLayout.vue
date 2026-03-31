<script setup lang="ts">
import { computed } from 'vue'
import { useAppStore } from '@/stores/app'
import AppHeader from '@/components/layout/AppHeader.vue'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppFooter from '@/components/layout/AppFooter.vue'

const appStore = useAppStore()

const isCollapsed = computed(() => appStore.sidebarCollapsed)
</script>

<template>
  <div class="min-h-screen bg-surface-muted">
    <!-- Header (fixed top) -->
    <AppHeader />

    <!-- Sidebar (fixed start) -->
    <AppSidebar />

    <!-- Main content area -->
    <div
      class="flex min-h-screen flex-col pt-16 transition-all duration-300"
      :style="{
        paddingInlineStart: isCollapsed ? '4rem' : '20rem',
      }"
    >
      <!-- Page content -->
      <main class="flex-1 p-6">
        <RouterView />
      </main>

      <!-- Footer -->
      <AppFooter />
    </div>
  </div>
</template>
