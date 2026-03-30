<script setup lang="ts">
/**
 * SubscriptionsView - Plans & Subscriptions Management (TASK-1001)
 *
 * Manages:
 * - Subscription plans (free, paid monthly, paid annually)
 * - Tenant subscription status
 * - Manual subscription additions for government entities
 * - Renewal reminders (2 months before expiry)
 * - PO tracking
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost, httpPut } from '@/services/http'

const { t } = useI18n()

interface Subscription {
  id: string
  tenantId: string
  tenantName: string
  planName: string
  planType: 'free' | 'monthly' | 'annual'
  status: 'active' | 'expiring_soon' | 'expired' | 'pending'
  startDate: string
  endDate: string
  poNumber: string | null
  maxUsers: number
  maxCompetitions: number
  aiEnabled: boolean
}

const isLoading = ref(false)
const subscriptions = ref<Subscription[]>([])
const statusFilter = ref('all')
const showAddDialog = ref(false)

const filteredSubscriptions = computed(() => {
  if (statusFilter.value === 'all') return subscriptions.value
  return subscriptions.value.filter(s => s.status === statusFilter.value)
})

async function loadSubscriptions(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: Subscription[] }>('/v1/operator/subscriptions')
    subscriptions.value = data.items
  } catch (err) {
    console.error('Failed to load subscriptions:', err)
  } finally {
    isLoading.value = false
  }
}

function getStatusBadge(status: string): string {
  const classes: Record<string, string> = {
    active: 'badge-success',
    expiring_soon: 'badge-warning',
    expired: 'badge-danger',
    pending: 'badge-info',
  }
  return classes[status] || 'badge-secondary'
}

function formatDate(date: string): string {
  return new Date(date).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

onMounted(() => {
  loadSubscriptions()
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">Plans & Subscriptions</h1>
        <p class="page-description">Manage tenant subscriptions and plans</p>
      </div>
      <button class="btn-primary" @click="showAddDialog = true">
        <i class="pi pi-plus"></i>
        Add Subscription
      </button>
    </div>

    <!-- Status Filters -->
    <div class="flex gap-2">
      <button
        v-for="status in ['all', 'active', 'expiring_soon', 'expired', 'pending']"
        :key="status"
        class="rounded-xl px-4 py-2 text-xs font-semibold transition-all"
        :class="statusFilter === status
          ? 'bg-primary text-white'
          : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
        @click="statusFilter = status"
      >
        {{ status === 'all' ? 'All' : status.replace('_', ' ') }}
      </button>
    </div>

    <!-- Subscriptions Table -->
    <div class="card overflow-hidden !p-0">
      <div v-if="isLoading" class="p-6">
        <div v-for="i in 5" :key="i" class="skeleton-text mb-3"></div>
      </div>
      <div v-else-if="filteredSubscriptions.length === 0" class="empty-state py-12">
        <div class="empty-state-icon"><i class="pi pi-credit-card"></i></div>
        <h3 class="empty-state-title">No subscriptions found</h3>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="table-modern">
          <thead>
            <tr>
              <th>Tenant</th>
              <th>Plan</th>
              <th>Type</th>
              <th>Status</th>
              <th>Start Date</th>
              <th>End Date</th>
              <th>PO Number</th>
              <th class="text-center">Users</th>
              <th class="text-center">AI</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="sub in filteredSubscriptions" :key="sub.id">
              <td class="font-medium">{{ sub.tenantName }}</td>
              <td>{{ sub.planName }}</td>
              <td>
                <span class="badge badge-info">{{ sub.planType }}</span>
              </td>
              <td>
                <span class="badge" :class="getStatusBadge(sub.status)">
                  {{ sub.status.replace('_', ' ') }}
                </span>
              </td>
              <td>{{ formatDate(sub.startDate) }}</td>
              <td>{{ formatDate(sub.endDate) }}</td>
              <td>{{ sub.poNumber || '-' }}</td>
              <td class="text-center">{{ sub.maxUsers }}</td>
              <td class="text-center">
                <i class="pi" :class="sub.aiEnabled ? 'pi-check-circle text-success' : 'pi-times-circle text-secondary-300'"></i>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
