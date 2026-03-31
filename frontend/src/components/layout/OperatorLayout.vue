<script setup lang="ts">
/**
 * OperatorLayout - Independent Operator/Super Admin Portal Layout (TASK-1001)
 *
 * Separate layout from the main tenant portal:
 * - Different sidebar with operator-specific navigation
 * - Operator branding (Tendex AI logo, not tenant logo)
 * - System health indicators in header
 * - Tenant switcher in header
 *
 * RTL/LTR support via logical properties.
 */
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { useAppStore } from '@/stores/app'
import TenantSelector from '@/components/operator/TenantSelector.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const appStore = useAppStore()

const isCollapsed = ref(false)

interface OperatorNavItem {
  key: string
  icon: string
  labelKey: string
  route: string
  badge?: string
}

const operatorNavItems: OperatorNavItem[] = [
  { key: 'dashboard', icon: 'pi-home', labelKey: 'nav.dashboard', route: '/operator' },
  { key: 'tenants', icon: 'pi-building', labelKey: 'nav.operator.tenants', route: '/operator/tenants' },
  { key: 'subscriptions', icon: 'pi-credit-card', labelKey: 'nav.operator.subscriptions', route: '/operator/subscriptions' },
  { key: 'aiSettings', icon: 'pi-sparkles', labelKey: 'nav.operator.aiSettings', route: '/operator/ai-settings' },
  { key: 'consumption', icon: 'pi-chart-line', labelKey: 'nav.operator.consumption', route: '/operator/consumption' },
  { key: 'health', icon: 'pi-server', labelKey: 'nav.operator.health', route: '/operator/system-health' },
  { key: 'support', icon: 'pi-headphones', labelKey: 'nav.operator.support', route: '/operator/support' },
  { key: 'impersonation', icon: 'pi-user-edit', labelKey: 'nav.operator.impersonation', route: '/operator/impersonation' },
]

function isActive(itemRoute: string): boolean {
  return route.path === itemRoute || route.path.startsWith(itemRoute + '/')
}

function navigateTo(itemRoute: string): void {
  router.push(itemRoute)
}

function switchLanguage(): void {
  const newLocale = appStore.locale === 'ar' ? 'en' : 'ar'
  appStore.setLocale(newLocale)
}
</script>

<template>
  <div class="flex h-screen overflow-hidden bg-surface-muted">
    <!-- Operator Sidebar -->
    <aside
      class="flex flex-col transition-all duration-300"
      :class="isCollapsed ? 'w-16' : 'w-64'"
      style="background: linear-gradient(180deg, #0F2440 0%, #1B3A5C 100%);"
    >
      <!-- Logo -->
      <div class="flex h-16 items-center gap-3 border-b border-white/10 px-4">
        <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-white/10">
          <i class="pi pi-bolt text-lg text-white"></i>
        </div>
        <div v-if="!isCollapsed" class="flex flex-col">
          <span class="text-sm font-bold text-white">Tendex AI</span>
          <span class="text-[10px] text-white/50">Operator Portal</span>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="sidebar-scroll flex-1 overflow-y-auto px-2 py-4">
        <ul class="space-y-0.5">
          <li v-for="item in operatorNavItems" :key="item.key">
            <button
              class="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition-all duration-200"
              :class="isActive(item.route)
                ? 'bg-white/15 text-white font-semibold'
                : 'text-white/60 hover:bg-white/10 hover:text-white'"
              @click="navigateTo(item.route)"
            >
              <i class="pi text-base" :class="item.icon"></i>
              <span v-if="!isCollapsed">{{ t(item.labelKey) }}</span>
            </button>
          </li>
        </ul>
      </nav>

      <!-- Collapse toggle -->
      <div class="border-t border-white/10 p-2">
        <button
          class="flex w-full items-center justify-center gap-2 rounded-lg px-3 py-2.5 text-sm text-white/40 transition-colors hover:bg-white/10 hover:text-white"
          @click="isCollapsed = !isCollapsed"
        >
          <i
            class="pi text-sm"
            :class="isCollapsed ? 'pi-angle-double-right' : 'pi-angle-double-left'"
          ></i>
          <span v-if="!isCollapsed" class="text-xs">Collapse</span>
        </button>
      </div>
    </aside>

    <!-- Main Content -->
    <div class="flex flex-1 flex-col overflow-hidden">
      <!-- Operator Header -->
      <header class="flex h-16 items-center justify-between border-b border-secondary-200/60 bg-white/95 px-6 backdrop-blur-sm">
        <div class="flex items-center gap-4">
          <TenantSelector />
        </div>
        <div class="flex items-center gap-3">
          <!-- System Health Indicator -->
          <div class="flex items-center gap-2 rounded-full bg-success-50 px-3 py-1.5">
            <span class="h-2 w-2 rounded-full bg-success animate-pulse"></span>
            <span class="text-xs font-medium text-success-dark">System Healthy</span>
          </div>
          <!-- Language -->
          <button
            class="flex h-10 items-center gap-1.5 rounded-xl px-3 text-sm font-medium text-secondary-600 transition-all hover:bg-surface-subtle"
            @click="switchLanguage"
          >
            <i class="pi pi-globe"></i>
            <span class="hidden sm:inline">{{ t('header.switchLanguage') }}</span>
          </button>
          <!-- User -->
          <button class="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-primary to-primary-light text-sm font-bold text-white ring-2 ring-primary-50">
            <i class="pi pi-user"></i>
          </button>
        </div>
      </header>

      <!-- Page Content -->
      <main class="flex-1 overflow-y-auto p-6">
        <router-view />
      </main>
    </div>
  </div>
</template>
