<script setup lang="ts">
/**
 * App Header — Main navigation header.
 *
 * Features:
 * - Dynamic branding: shows tenant logo and name when branding is loaded
 * - Sidebar toggle
 * - Tenant selector for Super Admin
 * - Search bar
 * - Notifications, language switcher, user menu with logout
 */
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAppStore } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'
import { useBrandingStore } from '@/stores/branding'
import TenantSelector from '@/components/operator/TenantSelector.vue'
import NotificationBell from '@/components/common/NotificationBell.vue'
import UserMenu from '@/components/layout/UserMenu.vue'

const { t, locale } = useI18n()
const router = useRouter()
const appStore = useAppStore()
const authStore = useAuthStore()
const brandingStore = useBrandingStore()
const headerSearch = ref('')

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

function submitHeaderSearch(): void {
  const normalizedSearch = headerSearch.value.trim()
  router.push({
    name: 'rfp-list',
    query: normalizedSearch ? { search: normalizedSearch } : {},
  })
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
    class="fixed inset-inline-0 top-0 z-50 flex h-16 items-center border-b border-secondary-200/60 bg-white/95 backdrop-blur-sm"
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
      <div class="flex items-center gap-2.5">
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
        <!-- Default Tendex AI icon -->
        <div
          v-else
          class="flex h-9 w-9 items-center justify-center overflow-hidden rounded-lg"
        >
          <img
            src="/logos/Tendexicon-01.svg"
            alt="Tendex AI"
            class="h-full w-full object-contain"
          />
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
    <div class="mx-4 hidden min-w-0 flex-1 md:flex lg:mx-6">
      <form class="relative w-full" @submit.prevent="submitHeaderSearch">
        <i
          class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-surface-dim"
        ></i>
        <input
          v-model="headerSearch"
          type="text"
          :placeholder="t('header.searchPlaceholder')"
          class="w-full rounded-xl border border-secondary-200 bg-surface-subtle py-2.5 pe-4 ps-10 text-sm text-secondary outline-none transition-all placeholder:text-secondary-400 focus:border-primary focus:bg-white focus:ring-2 focus:ring-primary/20"
        />
      </form>
    </div>

    <!-- End section: Actions -->
    <div class="flex shrink-0 items-center gap-2 pe-4">
      <!-- AI Powered Badge -->
      <div
        class="hidden items-center gap-1.5 rounded-full bg-gradient-to-r from-ai-50 to-ai-100 px-3 py-1.5 lg:flex"
      >
        <i class="pi pi-sparkles text-xs text-ai-600"></i>
        <span class="text-xs font-semibold text-ai-600">
          {{ t('header.aiYearBadge') }}
        </span>
      </div>

      <!-- Notifications -->
      <NotificationBell />

      <!-- Language switcher -->
      <button
        type="button"
        class="flex h-10 items-center gap-1.5 rounded-xl px-3 text-sm font-medium text-secondary-600 transition-all hover:bg-surface-subtle hover:text-secondary"
        @click="switchLanguage"
      >
        <i class="pi pi-globe text-base"></i>
        <span class="hidden sm:inline">{{ t('header.switchLanguage') }}</span>
      </button>

      <!-- User Menu with Logout -->
      <UserMenu />
    </div>
  </header>
</template>
