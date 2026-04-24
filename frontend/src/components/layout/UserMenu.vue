<script setup lang="ts">
/**
 * UserMenu — Dropdown menu for user actions.
 *
 * Features:
 * - Shows user avatar/initials and name
 * - Links to profile page
 * - Logout with confirmation dialog
 */
import { ref, computed, watch, onBeforeUnmount } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const isOpen = ref(false)
const showLogoutConfirm = ref(false)
const isLoggingOut = ref(false)
const menuRef = ref<HTMLElement | null>(null)

const userInitials = computed(() => {
  if (!authStore.user) return '?'
  const first = authStore.user.firstName?.charAt(0) || ''
  const last = authStore.user.lastName?.charAt(0) || ''
  return `${first}${last}`.toUpperCase() || '?'
})

const displayName = computed(() => authStore.fullName || t('common.profile'))

const userEmail = computed(() => authStore.user?.email || '')

function toggleMenu(): void {
  if (isOpen.value) {
    closeMenu()
    return
  }

  isOpen.value = true
  bindGlobalListeners()
}

function closeMenu(): void {
  if (!isOpen.value) {
    return
  }

  isOpen.value = false
  unbindGlobalListeners()
}

function goToProfile(): void {
  closeMenu()
  router.push({ name: 'UserProfile' })
}

function promptLogout(): void {
  closeMenu()
  showLogoutConfirm.value = true
}

function cancelLogout(): void {
  showLogoutConfirm.value = false
}

async function confirmLogout(): Promise<void> {
  isLoggingOut.value = true
  try {
    await authStore.logoutAction()
    showLogoutConfirm.value = false
    router.push({ name: 'Login' })
  } catch {
    // Silently handle errors — clearAuthState already runs in finally
    router.push({ name: 'Login' })
  } finally {
    isLoggingOut.value = false
  }
}

function handleDocumentPointerDown(event: MouseEvent): void {
  const target = event.target as Node | null

  if (!target || !menuRef.value) {
    return
  }

  if (!menuRef.value.contains(target)) {
    closeMenu()
  }
}

function handleEscapeKey(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    closeMenu()
  }
}

function bindGlobalListeners(): void {
  document.addEventListener('mousedown', handleDocumentPointerDown)
  document.addEventListener('keydown', handleEscapeKey)
}

function unbindGlobalListeners(): void {
  document.removeEventListener('mousedown', handleDocumentPointerDown)
  document.removeEventListener('keydown', handleEscapeKey)
}

watch(() => route.fullPath, () => {
  closeMenu()
})

onBeforeUnmount(() => {
  unbindGlobalListeners()
})
</script>

<template>
  <div ref="menuRef" class="relative">
    <!-- User avatar button -->
    <button
      type="button"
      class="flex items-center gap-2 rounded-xl px-2 py-1.5 transition-all hover:bg-surface-subtle"
      :aria-label="t('common.profile')"
      @click="toggleMenu"
    >
      <div
        class="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-primary to-primary-dark text-sm font-bold text-white ring-2 ring-primary/20 transition-all hover:ring-primary/40"
      >
        {{ userInitials }}
      </div>
      <div class="hidden flex-col items-start md:flex">
        <span class="text-sm font-medium text-secondary">{{ displayName }}</span>
        <span class="text-[11px] text-tertiary">{{ userEmail }}</span>
      </div>
      <i
        class="pi pi-chevron-down hidden text-xs text-tertiary transition-transform md:block"
        :class="{ 'rotate-180': isOpen }"
      ></i>
    </button>

    <!-- Dropdown menu -->
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="scale-95 opacity-0"
      enter-to-class="scale-100 opacity-100"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="scale-100 opacity-100"
      leave-to-class="scale-95 opacity-0"
    >
      <div
        v-if="isOpen"
        class="absolute end-0 top-full z-50 mt-2 w-64 overflow-hidden rounded-xl border border-secondary-200/60 bg-white shadow-lg"
      >
        <!-- User info header -->
        <div class="border-b border-secondary-100 bg-surface-subtle px-4 py-3">
          <p class="text-sm font-semibold text-secondary">{{ displayName }}</p>
          <p class="text-xs text-tertiary">{{ userEmail }}</p>
        </div>

        <!-- Menu items -->
        <div class="py-1">
          <!-- Profile -->
          <button
            type="button"
            class="flex w-full items-center gap-3 px-4 py-2.5 text-sm text-secondary transition-colors hover:bg-surface-subtle"
            @click="goToProfile"
          >
            <i class="pi pi-user text-base text-tertiary"></i>
            <span>{{ t('userMenu.profile') }}</span>
          </button>
        </div>

        <!-- Logout -->
        <div class="border-t border-secondary-100 py-1">
          <button
            type="button"
            class="flex w-full items-center gap-3 px-4 py-2.5 text-sm text-danger transition-colors hover:bg-danger/5"
            @click="promptLogout"
          >
            <i class="pi pi-sign-out text-base"></i>
            <span>{{ t('userMenu.logout') }}</span>
          </button>
        </div>
      </div>
    </Transition>
  </div>

  <!-- Logout Confirmation Dialog -->
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="showLogoutConfirm"
        class="fixed inset-0 z-[100] flex items-center justify-center bg-black/40 backdrop-blur-sm"
        @click.self="cancelLogout"
      >
        <div class="mx-4 w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl">
          <!-- Icon -->
          <div class="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-danger/10">
            <i class="pi pi-sign-out text-2xl text-danger"></i>
          </div>

          <!-- Title -->
          <h3 class="mb-2 text-center text-lg font-bold text-secondary">
            {{ t('userMenu.logoutConfirm') }}
          </h3>
          <p class="mb-6 text-center text-sm text-tertiary">
            {{ t('userMenu.logoutConfirmMessage') }}
          </p>

          <!-- Actions -->
          <div class="flex gap-3">
            <button
              type="button"
              class="flex-1 rounded-xl border border-secondary-200 px-4 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
              :disabled="isLoggingOut"
              @click="cancelLogout"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="flex flex-1 items-center justify-center gap-2 rounded-xl bg-danger px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-danger/90"
              :disabled="isLoggingOut"
              @click="confirmLogout"
            >
              <i v-if="isLoggingOut" class="pi pi-spinner pi-spin text-sm"></i>
              <span>{{ isLoggingOut ? t('userMenu.loggingOut') : t('userMenu.logout') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
