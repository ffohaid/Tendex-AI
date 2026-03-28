<script setup lang="ts">
/**
 * App Header — Main navigation header.
 *
 * Features:
 * - Dynamic branding: shows tenant logo and name when branding is loaded
 * - Sidebar toggle
 * - Tenant selector for Super Admin
 * - Search bar
 * - Notifications, language switcher, user avatar
 */
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useBrandingStore } from '@/stores/branding'
import TenantSelector from '@/components/operator/TenantSelector.vue'

const { t, locale } = useI18n()
const appStore = useAppStore()
const authStore = useAuthStore()
const brandingStore = useBrandingStore()

/** Check if user is Super Admin to show tenant selector */
const isSuperAdmin = computed(() => {
  return authStore.user?.roles?.some(
    (r: string) => r === 'SuperAdmin' || r === 'Operator'
  ) ?? false
})

/** Get the active branding (tenant-specific or default) */
const activeBranding = computed(() => brandingStore.activeBranding)

/** Whether tenant branding is loaded and has a logo */
const hasTenantLogo = computed(() => !!activeBranding.value.logoUrl)

/** Get tenant display name based on locale */
const tenantDisplayName = computed(() => {
  if (!activeBranding.value.tenantNameAr && !activeBranding.value.tenantNameEn) {
    return null
  }
  return locale.value === 'ar'
    ? activeBranding.value.tenantNameAr
    : activeBranding.value.tenantNameEn
})

/**
 * Toggles between Arabic and English locales.
 */
function switchLanguage(): void {
  const newLocale = appStore.locale === 'ar' ? 'en' : 'ar'
  appStore.setLocale(newLocale)
}

/**
 * Toggles sidebar collapsed state.
 */
function toggleSidebar(): void {
  appStore.toggleSidebar()
}

/** Load branding on mount if user is authenticated */
onMounted(async () => {
  const hasToken = !!localStorage.getItem('access_token')
  const tenantId = localStorage.getItem('tenant_id')
  if (hasToken && tenantId) {
    await brandingStore.loadAndApplyBranding()
  }
})
</script>

<template>
  <header
    class="fixed inset-inline-0 top-0 z-50 flex h-16 items-center border-b border-surface-dim bg-white shadow-sm"
  >
    <!-- Start section: Sidebar toggle + Logo -->
    <div class="flex items-center gap-3 ps-4">
      <!-- Sidebar toggle button -->
      <button
        type="button"
        class="flex h-10 w-10 items-center justify-center rounded-lg text-secondary transition-colors hover:bg-surface-muted"
        :aria-label="appStore.sidebarCollapsed ? t('sidebar.expand') : t('sidebar.collapse')"
        @click="toggleSidebar"
      >
        <i class="pi pi-bars text-lg"></i>
      </button>

      <!-- Platform Logo — Dynamic Branding -->
      <div class="flex items-center gap-2">
        <!-- Tenant logo (if available) -->
        <div
          v-if="hasTenantLogo"
          class="flex h-9 w-9 items-center justify-center overflow-hidden rounded-lg border border-surface-dim bg-white"
        >
          <img
            :src="activeBranding.logoUrl ?? undefined"
            :alt="tenantDisplayName || t('app.name')"
            class="h-full w-full object-contain"
          />
        </div>
        <!-- Default platform icon -->
        <div
          v-else
          class="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-white"
        >
          <i class="pi pi-bolt text-lg"></i>
        </div>
        <div class="flex flex-col">
          <span class="text-lg font-bold leading-tight text-secondary">
            {{ tenantDisplayName || t('app.name') }}
          </span>
          <span
            v-if="tenantDisplayName"
            class="text-[10px] leading-tight text-tertiary"
          >
            {{ t('app.name') }}
          </span>
        </div>
      </div>
    </div>

    <!-- Tenant Selector for Super Admin -->
    <div v-if="isSuperAdmin" class="ms-4 hidden md:flex">
      <TenantSelector />
    </div>

    <!-- Center section: Search bar -->
    <div class="mx-6 hidden flex-1 md:flex">
      <div class="relative w-full max-w-md">
        <i
          class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-surface-dim"
        ></i>
        <input
          type="text"
          :placeholder="t('header.searchPlaceholder')"
          class="w-full rounded-lg border border-surface-dim bg-surface-muted py-2 pe-4 ps-10 text-sm text-secondary outline-none transition-colors placeholder:text-surface-dim focus:border-primary focus:ring-1 focus:ring-primary"
        />
      </div>
    </div>

    <!-- End section: Actions -->
    <div class="flex items-center gap-2 pe-4">
      <!-- AI Year 2026 Badge -->
      <div
        class="hidden items-center gap-1.5 rounded-full bg-primary/10 px-3 py-1.5 lg:flex"
      >
        <i class="pi pi-sparkles text-xs text-primary"></i>
        <span class="text-xs font-medium text-primary">
          {{ t('header.aiYearBadge') }}
        </span>
      </div>

      <!-- Notifications -->
      <button
        type="button"
        class="relative flex h-10 w-10 items-center justify-center rounded-lg text-secondary transition-colors hover:bg-surface-muted"
        :aria-label="t('common.notifications')"
      >
        <i class="pi pi-bell text-lg"></i>
        <!-- Notification dot -->
        <span
          class="absolute end-2 top-2 h-2 w-2 rounded-full bg-red-500"
        ></span>
      </button>

      <!-- Language switcher -->
      <button
        type="button"
        class="flex h-10 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
        @click="switchLanguage"
      >
        <i class="pi pi-globe text-base"></i>
        <span class="hidden sm:inline">{{ t('header.switchLanguage') }}</span>
      </button>

      <!-- User avatar -->
      <button
        type="button"
        class="flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-sm font-bold text-white transition-opacity hover:opacity-90"
        :aria-label="t('common.profile')"
      >
        <i class="pi pi-user"></i>
      </button>
    </div>
  </header>
</template>
