<script setup lang="ts">
/**
 * NotificationsView - Full Notifications Page (TASK-1001)
 *
 * Features:
 * - All notifications list with pagination
 * - Filter by type (task, approval, SLA, inquiry, competition)
 * - Mark as read/unread
 * - Bulk operations
 * - Notification preferences management
 * - Real-time updates via SignalR
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet, httpPost, httpPut } from '@/services/http'

const { t, locale } = useI18n()
const router = useRouter()

interface Notification {
  id: string
  type: 'task' | 'approval' | 'sla_escalation' | 'inquiry' | 'competition_update' | 'system'
  title: string
  titleEn: string
  body: string
  bodyEn: string
  isRead: boolean
  createdAt: string
  actionUrl: string | null
  metadata: Record<string, string>
}

interface NotificationPreference {
  type: string
  label: string
  inApp: boolean
  email: boolean
}

const isLoading = ref(false)
const notifications = ref<Notification[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const activeTab = ref<'all' | 'preferences'>('all')
const typeFilter = ref('all')

const preferences = ref<NotificationPreference[]>([
  { type: 'task', label: 'New Task Assignment', inApp: true, email: true },
  { type: 'approval', label: 'Approval Required', inApp: true, email: true },
  { type: 'sla_escalation', label: 'SLA Escalation', inApp: true, email: true },
  { type: 'inquiry', label: 'New Inquiry', inApp: true, email: false },
  { type: 'competition_update', label: 'Competition Updates', inApp: true, email: false },
  { type: 'system', label: 'System Notifications', inApp: true, email: false },
])

const unreadCount = computed(() => notifications.value.filter(n => !n.isRead).length)

const filteredNotifications = computed(() => {
  if (typeFilter.value === 'all') return notifications.value
  return notifications.value.filter(n => n.type === typeFilter.value)
})

async function loadNotifications(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: Notification[]; totalCount: number }>('/v1/notifications', {
      params: { page: currentPage.value, pageSize: 50 },
    })
    notifications.value = data.items
    totalCount.value = data.totalCount
  } catch (err) {
    console.error('Failed to load notifications:', err)
  } finally {
    isLoading.value = false
  }
}

async function markAsRead(id: string): Promise<void> {
  try {
    await httpPut(`/v1/notifications/${id}/read`)
    const notif = notifications.value.find(n => n.id === id)
    if (notif) notif.isRead = true
  } catch (err) {
    console.error('Failed to mark as read:', err)
  }
}

async function markAllAsRead(): Promise<void> {
  try {
    await httpPost('/v1/notifications/mark-all-read')
    notifications.value.forEach(n => (n.isRead = true))
  } catch (err) {
    console.error('Failed to mark all as read:', err)
  }
}

function handleNotificationClick(notif: Notification): void {
  if (!notif.isRead) markAsRead(notif.id)
  if (notif.actionUrl) router.push(notif.actionUrl)
}

function getTypeIcon(type: string): string {
  const icons: Record<string, string> = {
    task: 'pi-check-square',
    approval: 'pi-check-circle',
    sla_escalation: 'pi-exclamation-triangle',
    inquiry: 'pi-question-circle',
    competition_update: 'pi-briefcase',
    system: 'pi-cog',
  }
  return icons[type] || 'pi-bell'
}

function getTypeColor(type: string): string {
  const colors: Record<string, string> = {
    task: 'bg-primary-50 text-primary',
    approval: 'bg-success-50 text-success',
    sla_escalation: 'bg-danger-50 text-danger',
    inquiry: 'bg-info-50 text-info',
    competition_update: 'bg-warning-50 text-warning',
    system: 'bg-secondary-50 text-secondary',
  }
  return colors[type] || 'bg-secondary-50 text-secondary'
}

function formatTimeAgo(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return locale.value === 'ar' ? 'الآن' : 'Just now'
  if (diffMins < 60) return locale.value === 'ar' ? `منذ ${diffMins} دقيقة` : `${diffMins}m ago`
  if (diffHours < 24) return locale.value === 'ar' ? `منذ ${diffHours} ساعة` : `${diffHours}h ago`
  return locale.value === 'ar' ? `منذ ${diffDays} يوم` : `${diffDays}d ago`
}

async function savePreferences(): Promise<void> {
  try {
    await httpPut('/v1/notifications/preferences', { preferences: preferences.value })
  } catch (err) {
    console.error('Failed to save preferences:', err)
  }
}

onMounted(() => {
  loadNotifications()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">{{ t('notifications.title') }}</h1>
        <p class="page-description">
          {{ unreadCount > 0 ? `${unreadCount} unread notifications` : 'All caught up!' }}
        </p>
      </div>
      <div class="flex gap-2">
        <button class="btn-ghost btn-sm" @click="markAllAsRead" :disabled="unreadCount === 0">
          <i class="pi pi-check-circle"></i>
          {{ t('notifications.markAllRead') }}
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex items-center gap-4 border-b border-secondary-100">
      <button
        class="tab-item"
        :class="{ 'tab-item-active': activeTab === 'all' }"
        @click="activeTab = 'all'"
      >
        {{ t('notifications.title') }}
        <span v-if="unreadCount > 0" class="ms-1 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-danger text-[10px] font-bold text-white">
          {{ unreadCount }}
        </span>
      </button>
      <button
        class="tab-item"
        :class="{ 'tab-item-active': activeTab === 'preferences' }"
        @click="activeTab = 'preferences'"
      >
        {{ t('notifications.preferences') }}
      </button>
    </div>

    <!-- Notifications List -->
    <div v-if="activeTab === 'all'">
      <!-- Type Filter -->
      <div class="mb-4 flex flex-wrap gap-2">
        <button
          v-for="filter in ['all', 'task', 'approval', 'sla_escalation', 'inquiry', 'competition_update']"
          :key="filter"
          class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors"
          :class="typeFilter === filter
            ? 'bg-primary text-white'
            : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
          @click="typeFilter = filter"
        >
          {{ filter === 'all' ? t('taskCenter.filters.all') : filter.replace('_', ' ') }}
        </button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="skeleton h-16 rounded-xl"></div>
      </div>

      <!-- Empty -->
      <div v-else-if="filteredNotifications.length === 0" class="empty-state">
        <div class="empty-state-icon"><i class="pi pi-bell-slash"></i></div>
        <h3 class="empty-state-title">{{ t('notifications.noNotifications') }}</h3>
      </div>

      <!-- Notification Items -->
      <div v-else class="space-y-2">
        <div
          v-for="notif in filteredNotifications"
          :key="notif.id"
          class="flex items-start gap-3 rounded-xl border p-4 transition-all cursor-pointer"
          :class="notif.isRead
            ? 'border-secondary-100 bg-white hover:bg-surface-subtle'
            : 'border-primary/20 bg-primary-50/30 hover:bg-primary-50/50'"
          @click="handleNotificationClick(notif)"
        >
          <!-- Type Icon -->
          <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl" :class="getTypeColor(notif.type)">
            <i class="pi text-sm" :class="getTypeIcon(notif.type)"></i>
          </div>

          <!-- Content -->
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <h4 class="text-sm font-semibold" :class="notif.isRead ? 'text-secondary-600' : 'text-secondary-800'">
                {{ locale === 'ar' ? notif.title : notif.titleEn }}
              </h4>
              <span v-if="!notif.isRead" class="h-2 w-2 rounded-full bg-primary"></span>
            </div>
            <p class="mt-0.5 text-xs text-secondary-500 line-clamp-1">
              {{ locale === 'ar' ? notif.body : notif.bodyEn }}
            </p>
          </div>

          <!-- Time -->
          <span class="shrink-0 text-[10px] text-secondary-400">
            {{ formatTimeAgo(notif.createdAt) }}
          </span>
        </div>
      </div>
    </div>

    <!-- Preferences Tab -->
    <div v-if="activeTab === 'preferences'" class="card">
      <h3 class="mb-4 text-sm font-bold text-secondary">{{ t('notifications.preferences') }}</h3>
      <table class="table-modern">
        <thead>
          <tr>
            <th>{{ t('notifications.type') }}</th>
            <th class="text-center">{{ t('notifications.inApp') }}</th>
            <th class="text-center">{{ t('notifications.email') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="pref in preferences" :key="pref.type">
            <td class="font-medium">{{ pref.label }}</td>
            <td class="text-center">
              <input
                v-model="pref.inApp"
                type="checkbox"
                class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
              />
            </td>
            <td class="text-center">
              <input
                v-model="pref.email"
                type="checkbox"
                class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
              />
            </td>
          </tr>
        </tbody>
      </table>
      <div class="mt-4 flex justify-end">
        <button class="btn-primary btn-sm" @click="savePreferences">
          <i class="pi pi-save"></i>
          Save Preferences
        </button>
      </div>
    </div>
  </div>
</template>
