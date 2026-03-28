<script setup lang="ts">
/**
 * NotificationsList Component
 *
 * Displays recent notifications with read/unread status.
 * Supports marking notifications as read.
 * Data is fetched dynamically from the dashboard store.
 */
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import type { Notification } from '@/types/dashboard'

const props = defineProps<{
  notifications: Notification[]
  isLoading: boolean
}>()

const emit = defineEmits<{
  markRead: [notificationId: string]
}>()

const { t, locale } = useI18n()
const { formatRelativeTime } = useFormatters()

function getNotificationIcon(type: string): string {
  const map: Record<string, string> = {
    task_assigned: 'pi-user-plus',
    offer_submitted: 'pi-upload',
    status_changed: 'pi-sync',
    sla_approaching: 'pi-clock',
    sla_exceeded: 'pi-exclamation-triangle',
    approval_result: 'pi-check-circle',
    committee_member_added: 'pi-users',
    committee_extended: 'pi-calendar-plus',
  }
  return map[type] ?? 'pi-bell'
}

function getNotificationIconColor(type: string): string {
  const map: Record<string, string> = {
    task_assigned: 'text-info bg-info/10',
    offer_submitted: 'text-primary bg-primary/10',
    status_changed: 'text-tertiary bg-tertiary/10',
    sla_approaching: 'text-warning bg-warning/10',
    sla_exceeded: 'text-danger bg-danger/10',
    approval_result: 'text-success bg-success/10',
    committee_member_added: 'text-primary bg-primary/10',
    committee_extended: 'text-info bg-info/10',
  }
  return map[type] ?? 'text-tertiary bg-surface-muted'
}

function getLocalizedTitle(notification: Notification): string {
  return locale.value === 'ar' ? notification.titleAr : notification.titleEn
}

function getLocalizedBody(notification: Notification): string {
  return locale.value === 'ar' ? notification.bodyAr : notification.bodyEn
}

function handleMarkRead(notificationId: string): void {
  emit('markRead', notificationId)
}
</script>

<template>
  <div class="card">
    <div class="mb-4 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <h3 class="text-lg font-semibold text-secondary">
          {{ t('dashboard.notifications') }}
        </h3>
        <span
          v-if="notifications.filter(n => !n.isRead).length > 0"
          class="flex h-5 min-w-5 items-center justify-center rounded-full bg-danger px-1 text-[10px] font-bold text-white"
        >
          {{ notifications.filter(n => !n.isRead).length }}
        </span>
      </div>
    </div>

    <!-- Loading skeleton -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="animate-pulse flex items-start gap-3 p-2">
        <div class="h-8 w-8 rounded-full bg-surface-dim"></div>
        <div class="flex-1">
          <div class="mb-1 h-3 w-3/4 rounded bg-surface-dim"></div>
          <div class="h-2 w-full rounded bg-surface-muted"></div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="notifications.length === 0"
      class="flex flex-col items-center justify-center py-8 text-center"
    >
      <i class="pi pi-bell-slash mb-3 text-4xl text-surface-dim"></i>
      <p class="text-sm text-tertiary">{{ t('dashboard.noNotifications') }}</p>
    </div>

    <!-- Notifications list -->
    <div v-else class="space-y-1 max-h-[350px] overflow-y-auto pe-1">
      <div
        v-for="notification in notifications"
        :key="notification.id"
        class="flex items-start gap-3 rounded-lg p-2.5 transition-all duration-200 cursor-pointer"
        :class="notification.isRead ? 'opacity-60 hover:opacity-80' : 'bg-surface-muted/50 hover:bg-surface-muted'"
        @click="!notification.isRead && handleMarkRead(notification.id)"
      >
        <!-- Icon -->
        <div
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
          :class="getNotificationIconColor(notification.type)"
        >
          <i class="pi text-xs" :class="getNotificationIcon(notification.type)"></i>
        </div>

        <!-- Content -->
        <div class="min-w-0 flex-1">
          <div class="flex items-start justify-between gap-2">
            <h4
              class="text-sm leading-tight"
              :class="notification.isRead ? 'text-tertiary' : 'font-medium text-secondary'"
            >
              {{ getLocalizedTitle(notification) }}
            </h4>
            <span
              v-if="!notification.isRead"
              class="mt-1 h-2 w-2 shrink-0 rounded-full bg-primary"
            ></span>
          </div>
          <p class="mt-0.5 text-xs text-tertiary line-clamp-2">
            {{ getLocalizedBody(notification) }}
          </p>
          <p class="mt-1 text-[10px] text-surface-dim">
            {{ formatRelativeTime(notification.createdAt) }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
