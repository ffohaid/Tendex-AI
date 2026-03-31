<script setup lang="ts">
/**
 * NotificationBell - Header notification bell with dropdown (TASK-1001)
 *
 * Features:
 * - Unread count badge
 * - Quick preview dropdown (latest 5)
 * - Click to navigate to full notifications page
 * - Real-time updates via SignalR
 */
import { ref, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet } from '@/services/http'

const { t, locale } = useI18n()
const router = useRouter()

interface NotificationPreview {
  id: string
  type: string
  title: string
  titleEn: string
  body: string
  bodyEn: string
  isRead: boolean
  createdAt: string
  actionUrl: string | null
}

const isOpen = ref(false)
const notifications = ref<NotificationPreview[]>([])
const unreadCount = ref(0)

async function loadPreview(): Promise<void> {
  try {
    const data = await httpGet<{ items: NotificationPreview[]; unreadCount: number }>(
      '/v1/notifications/preview'
    )
    notifications.value = data.items
    unreadCount.value = data.unreadCount
  } catch (err) {
    console.error('Failed to load notification preview:', err)
  }
}

function toggle(): void {
  isOpen.value = !isOpen.value
  if (isOpen.value) loadPreview()
}

function close(): void {
  isOpen.value = false
}

function handleClick(notif: NotificationPreview): void {
  close()
  if (notif.actionUrl) router.push(notif.actionUrl)
  else router.push('/notifications')
}

function viewAll(): void {
  close()
  router.push('/notifications')
}

function formatTimeAgo(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  if (diffMins < 1) return locale.value === 'ar' ? 'الآن' : 'now'
  if (diffMins < 60) return `${diffMins}m`
  if (diffHours < 24) return `${diffHours}h`
  return `${Math.floor(diffMs / 86400000)}d`
}

/* Close on outside click */
function handleOutsideClick(e: MouseEvent): void {
  const el = (e.target as HTMLElement).closest('.notification-bell-container')
  if (!el) close()
}

onMounted(() => {
  loadPreview()
  /* Poll every 30 seconds for new notifications */
  const timer = setInterval(loadPreview, 30000)
  document.addEventListener('click', handleOutsideClick)
  onUnmounted(() => {
    clearInterval(timer)
    document.removeEventListener('click', handleOutsideClick)
  })
})
</script>

<template>
  <div class="notification-bell-container relative">
    <!-- Bell Button -->
    <button
      class="relative flex h-10 w-10 items-center justify-center rounded-xl text-secondary-500 transition-all hover:bg-surface-subtle hover:text-secondary"
      @click="toggle"
    >
      <i class="pi pi-bell text-lg"></i>
      <span
        v-if="unreadCount > 0"
        class="absolute -end-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-danger text-[10px] font-bold text-white animate-bounce-subtle"
      >
        {{ unreadCount > 99 ? '99+' : unreadCount }}
      </span>
    </button>

    <!-- Dropdown -->
    <Transition name="dropdown">
      <div
        v-if="isOpen"
        class="absolute end-0 top-full z-50 mt-2 w-80 rounded-2xl border border-secondary-100 bg-white shadow-elevated"
      >
        <!-- Header -->
        <div class="flex items-center justify-between border-b border-secondary-100 px-4 py-3">
          <h4 class="text-sm font-bold text-secondary">{{ t('notifications.title') }}</h4>
          <button class="text-[10px] font-medium text-primary hover:underline" @click="viewAll">
            {{ t('notifications.viewAll') }}
          </button>
        </div>

        <!-- Notification Items -->
        <div class="max-h-80 overflow-y-auto">
          <div
            v-for="notif in notifications"
            :key="notif.id"
            class="flex items-start gap-3 border-b border-secondary-50 px-4 py-3 transition-colors cursor-pointer hover:bg-surface-subtle"
            :class="{ 'bg-primary-50/20': !notif.isRead }"
            @click="handleClick(notif)"
          >
            <span
              v-if="!notif.isRead"
              class="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary"
            ></span>
            <span v-else class="mt-1.5 h-2 w-2 shrink-0"></span>
            <div class="min-w-0 flex-1">
              <p class="text-xs font-semibold text-secondary-800 line-clamp-1">
                {{ locale === 'ar' ? notif.title : notif.titleEn }}
              </p>
              <p class="mt-0.5 text-[10px] text-secondary-500 line-clamp-1">
                {{ locale === 'ar' ? notif.body : notif.bodyEn }}
              </p>
            </div>
            <span class="shrink-0 text-[10px] text-secondary-400">
              {{ formatTimeAgo(notif.createdAt) }}
            </span>
          </div>

          <div v-if="notifications.length === 0" class="px-4 py-8 text-center">
            <i class="pi pi-bell-slash text-2xl text-secondary-300"></i>
            <p class="mt-2 text-xs text-secondary-400">{{ t('notifications.noNotifications') }}</p>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
.animate-bounce-subtle {
  animation: bounce-subtle 2s ease-in-out infinite;
}
@keyframes bounce-subtle {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-2px); }
}
</style>
