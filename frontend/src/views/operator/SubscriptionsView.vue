<script setup lang="ts">
/**
 * SubscriptionsView - Plans & Subscriptions Management
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
import { httpGet } from '@/services/http'

const { t, locale } = useI18n()

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

const statusLabels = computed(() => ({
  all: locale.value === 'ar' ? 'الكل' : 'All',
  active: locale.value === 'ar' ? 'نشط' : 'Active',
  expiring_soon: locale.value === 'ar' ? 'قارب على الانتهاء' : 'Expiring Soon',
  expired: locale.value === 'ar' ? 'منتهي' : 'Expired',
  pending: locale.value === 'ar' ? 'قيد الانتظار' : 'Pending',
}))

const filteredSubscriptions = computed(() => {
  if (statusFilter.value === 'all') return subscriptions.value
  return subscriptions.value.filter(s => s.status === statusFilter.value)
})

async function loadSubscriptions(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: Subscription[] }>('/v1/operator/subscriptions')
    subscriptions.value = data.items
  } catch {
    console.warn('Subscriptions API not available')
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
  const loc = locale.value === 'ar' ? 'ar-SA' : 'en-US'
  return new Date(date).toLocaleDateString(loc, { year: 'numeric', month: 'short', day: 'numeric' })
}

onMounted(() => {
  loadSubscriptions()
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">{{ t('operator.subscriptions') }}</h1>
        <p class="page-description">{{ t('operator.subscriptionsDesc') }}</p>
      </div>
      <button class="btn-primary" @click="showAddDialog = true">
        <i class="pi pi-plus"></i>
        {{ locale === 'ar' ? 'إضافة اشتراك' : 'Add Subscription' }}
      </button>
    </div>

    <!-- Status Filters -->
    <div class="flex flex-wrap gap-2">
      <button
        v-for="status in ['all', 'active', 'expiring_soon', 'expired', 'pending']"
        :key="status"
        class="rounded-xl px-4 py-2 text-xs font-semibold transition-all"
        :class="statusFilter === status
          ? 'bg-primary text-white'
          : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
        @click="statusFilter = status"
      >
        {{ statusLabels[status as keyof typeof statusLabels] || status }}
      </button>
    </div>

    <!-- Subscriptions Table -->
    <div class="card overflow-hidden !p-0">
      <div v-if="isLoading" class="p-6">
        <div v-for="i in 5" :key="i" class="skeleton-text mb-3"></div>
      </div>
      <div v-else-if="filteredSubscriptions.length === 0" class="py-12 text-center">
        <div class="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-secondary-50">
          <i class="pi pi-credit-card text-xl text-secondary-400"></i>
        </div>
        <h3 class="text-base font-bold text-secondary-700">
          {{ locale === 'ar' ? 'لا توجد اشتراكات' : 'No subscriptions found' }}
        </h3>
        <p class="mt-1 text-sm text-secondary-500">
          {{ locale === 'ar' ? 'ستظهر هنا اشتراكات الجهات الحكومية' : 'Government entity subscriptions will appear here' }}
        </p>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="table-modern">
          <thead>
            <tr>
              <th>{{ locale === 'ar' ? 'الجهة' : 'Tenant' }}</th>
              <th>{{ locale === 'ar' ? 'الخطة' : 'Plan' }}</th>
              <th>{{ locale === 'ar' ? 'النوع' : 'Type' }}</th>
              <th>{{ locale === 'ar' ? 'الحالة' : 'Status' }}</th>
              <th>{{ locale === 'ar' ? 'تاريخ البدء' : 'Start Date' }}</th>
              <th>{{ locale === 'ar' ? 'تاريخ الانتهاء' : 'End Date' }}</th>
              <th>{{ locale === 'ar' ? 'رقم أمر الشراء' : 'PO Number' }}</th>
              <th class="text-center">{{ locale === 'ar' ? 'المستخدمون' : 'Users' }}</th>
              <th class="text-center">{{ locale === 'ar' ? 'الذكاء الاصطناعي' : 'AI' }}</th>
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
                  {{ statusLabels[sub.status as keyof typeof statusLabels] || sub.status }}
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
