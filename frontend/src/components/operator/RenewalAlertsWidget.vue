<script setup lang="ts">
/**
 * Renewal Alerts Widget — Super Admin Portal.
 *
 * Displays early warning alerts for subscriptions approaching expiry.
 * Per PRD section 3.2.3: alerts at 60, 30, and 14 days before expiry.
 * Data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { usePurchaseOrderStore } from '@/stores/purchaseOrder'
import type { RenewalAlertDto, RenewalAlertSeverity } from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const poStore = usePurchaseOrderStore()

const { renewalAlerts, hasAlerts } = storeToRefs(poStore)

/** Severity configuration */
function getSeverityConfig(severity: RenewalAlertSeverity) {
  const config: Record<
    string,
    {
      labelKey: string
      bgClass: string
      textClass: string
      borderClass: string
      icon: string
      iconBgClass: string
    }
  > = {
    expired: {
      labelKey: 'renewalAlerts.severity.expired',
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
      borderClass: 'border-red-200',
      icon: 'pi pi-exclamation-circle',
      iconBgClass: 'bg-red-100',
    },
    critical: {
      labelKey: 'renewalAlerts.severity.critical',
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
      borderClass: 'border-red-200',
      icon: 'pi pi-exclamation-triangle',
      iconBgClass: 'bg-red-100',
    },
    warning: {
      labelKey: 'renewalAlerts.severity.warning',
      bgClass: 'bg-amber-50',
      textClass: 'text-amber-700',
      borderClass: 'border-amber-200',
      icon: 'pi pi-clock',
      iconBgClass: 'bg-amber-100',
    },
    info: {
      labelKey: 'renewalAlerts.severity.info',
      bgClass: 'bg-blue-50',
      textClass: 'text-blue-700',
      borderClass: 'border-blue-200',
      icon: 'pi pi-info-circle',
      iconBgClass: 'bg-blue-100',
    },
  }
  return (
    config[severity] || {
      labelKey: 'renewalAlerts.severity.info',
      bgClass: 'bg-slate-50',
      textClass: 'text-slate-700',
      borderClass: 'border-slate-200',
      icon: 'pi pi-info-circle',
      iconBgClass: 'bg-slate-100',
    }
  )
}

/** Get tenant name based on locale */
function getTenantName(alert: RenewalAlertDto): string {
  return locale.value === 'ar' ? alert.tenantNameAr : alert.tenantNameEn
}

/** Sorted alerts: expired and critical first */
const sortedAlerts = computed(() => {
  const severityOrder: Record<string, number> = {
    expired: 0,
    critical: 1,
    warning: 2,
    info: 3,
  }
  return [...renewalAlerts.value].sort(
    (a, b) =>
      (severityOrder[a.severity] ?? 4) - (severityOrder[b.severity] ?? 4),
  )
})

/** Summary stats */
const expiredCount = computed(
  () => renewalAlerts.value.filter((a) => a.severity === 'expired').length,
)
const criticalCount = computed(
  () => renewalAlerts.value.filter((a) => a.severity === 'critical').length,
)
const warningCount = computed(
  () => renewalAlerts.value.filter((a) => a.severity === 'warning').length,
)

/** Navigate to PO detail */
function goToPo(poId: string) {
  router.push({ name: 'PurchaseOrderDetail', params: { id: poId } })
}

/** Navigate to tenant detail */
function goToTenant(tenantId: string) {
  router.push({ name: 'TenantDetail', params: { id: tenantId } })
}

/** Format date */
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-SA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

onMounted(() => {
  poStore.loadRenewalAlerts(60)
})
</script>

<template>
  <div class="rounded-lg border border-surface-dim bg-white">
    <!-- Header -->
    <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100">
          <i class="pi pi-bell text-lg text-amber-700"></i>
        </div>
        <div>
          <h2 class="text-lg font-semibold text-secondary">
            {{ t('renewalAlerts.title') }}
          </h2>
          <p class="text-xs text-tertiary">
            {{ t('renewalAlerts.subtitle') }}
          </p>
        </div>
      </div>
      <span
        v-if="hasAlerts"
        class="flex h-6 min-w-[24px] items-center justify-center rounded-full bg-red-500 px-1.5 text-xs font-bold text-white"
      >
        {{ renewalAlerts.length }}
      </span>
    </div>

    <!-- Summary Stats -->
    <div v-if="hasAlerts" class="grid grid-cols-3 gap-3 border-b border-surface-dim px-6 py-3">
      <div class="flex items-center gap-2 rounded-lg bg-red-50 px-3 py-2">
        <i class="pi pi-exclamation-circle text-sm text-red-600"></i>
        <div>
          <p class="text-lg font-bold text-red-700">{{ expiredCount + criticalCount }}</p>
          <p class="text-[10px] text-red-600">{{ t('renewalAlerts.labels.critical') }}</p>
        </div>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-amber-50 px-3 py-2">
        <i class="pi pi-clock text-sm text-amber-600"></i>
        <div>
          <p class="text-lg font-bold text-amber-700">{{ warningCount }}</p>
          <p class="text-[10px] text-amber-600">{{ t('renewalAlerts.labels.warning') }}</p>
        </div>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-blue-50 px-3 py-2">
        <i class="pi pi-info-circle text-sm text-blue-600"></i>
        <div>
          <p class="text-lg font-bold text-blue-700">{{ renewalAlerts.length }}</p>
          <p class="text-[10px] text-blue-600">{{ t('renewalAlerts.labels.total') }}</p>
        </div>
      </div>
    </div>

    <!-- Alert List -->
    <div v-if="hasAlerts" class="max-h-[400px] overflow-y-auto">
      <div
        v-for="alert in sortedAlerts"
        :key="`${alert.tenantId}-${alert.poId}`"
        class="border-b border-surface-dim px-6 py-4 transition-colors last:border-b-0 hover:bg-surface-muted/30"
      >
        <div class="flex items-start gap-3">
          <!-- Severity icon -->
          <div
            class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
            :class="getSeverityConfig(alert.severity).iconBgClass"
          >
            <i
              :class="getSeverityConfig(alert.severity).icon"
              class="text-sm"
              :class="getSeverityConfig(alert.severity).textClass"
            ></i>
          </div>

          <!-- Content -->
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <button
                type="button"
                class="text-sm font-medium text-primary hover:underline"
                @click="goToTenant(alert.tenantId)"
              >
                {{ getTenantName(alert) }}
              </button>
              <span
                class="inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium"
                :class="[
                  getSeverityConfig(alert.severity).bgClass,
                  getSeverityConfig(alert.severity).textClass,
                ]"
              >
                {{ t(getSeverityConfig(alert.severity).labelKey) }}
              </span>
            </div>

            <div class="mt-1 flex items-center gap-4 text-xs text-tertiary">
              <span>
                {{ t('renewalAlerts.labels.poNumber') }}:
                <button
                  type="button"
                  class="font-mono text-primary hover:underline"
                  @click="goToPo(alert.poId)"
                >
                  {{ alert.poNumber }}
                </button>
              </span>
              <span>
                {{ t('renewalAlerts.labels.expiryDate') }}:
                {{ formatDate(alert.subscriptionEndDate) }}
              </span>
            </div>

            <div class="mt-1.5">
              <span
                class="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold"
                :class="
                  alert.daysUntilExpiry <= 0
                    ? 'bg-red-100 text-red-700'
                    : alert.daysUntilExpiry <= 14
                      ? 'bg-red-50 text-red-600'
                      : alert.daysUntilExpiry <= 30
                        ? 'bg-orange-50 text-orange-600'
                        : 'bg-amber-50 text-amber-600'
                "
              >
                <template v-if="alert.daysUntilExpiry <= 0">
                  {{ t('renewalAlerts.labels.expiredSince', { days: Math.abs(alert.daysUntilExpiry) }) }}
                </template>
                <template v-else>
                  {{ t('renewalAlerts.labels.expiresIn', { days: alert.daysUntilExpiry }) }}
                </template>
              </span>
            </div>

            <!-- Contact info -->
            <div v-if="alert.contactPersonName || alert.contactPersonEmail" class="mt-2 flex items-center gap-3 text-xs text-tertiary">
              <span v-if="alert.contactPersonName">
                <i class="pi pi-user me-1 text-[10px]"></i>
                {{ alert.contactPersonName }}
              </span>
              <span v-if="alert.contactPersonEmail" dir="ltr">
                <i class="pi pi-envelope me-1 text-[10px]"></i>
                {{ alert.contactPersonEmail }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else class="px-6 py-12 text-center">
      <i class="pi pi-check-circle mb-3 text-4xl text-emerald-500"></i>
      <h3 class="text-sm font-semibold text-secondary">
        {{ t('renewalAlerts.messages.noAlerts') }}
      </h3>
      <p class="mt-1 text-xs text-tertiary">
        {{ t('renewalAlerts.messages.noAlertsDesc') }}
      </p>
    </div>
  </div>
</template>
