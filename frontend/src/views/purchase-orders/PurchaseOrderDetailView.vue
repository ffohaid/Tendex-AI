<script setup lang="ts">
/**
 * Purchase Order Detail View — Super Admin Portal.
 *
 * Displays full PO information with lifecycle management.
 * Data is fetched dynamically from the API — NO mock data.
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { usePurchaseOrderStore } from '@/stores/purchaseOrder'
import { PoStatus } from '@/types/tenant'
import type { ChangePurchaseOrderStatusRequest } from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const poStore = usePurchaseOrderStore()

const { currentPo, isLoading, isSubmitting, error, successMessage } =
  storeToRefs(poStore)

const poId = computed(() => route.params.id as string)

/** Status change dialog */
const showStatusDialog = ref(false)
const statusChangeForm = ref<ChangePurchaseOrderStatusRequest>({
  newStatus: PoStatus.EnvironmentSetup,
  notes: '',
})

/** PO Status badge */
function getPoStatusBadge(status: PoStatus) {
  const config: Record<number, { labelKey: string; bgClass: string; textClass: string; icon: string }> = {
    [PoStatus.Received]: { labelKey: 'purchaseOrders.statuses.received', bgClass: 'bg-blue-50', textClass: 'text-blue-700', icon: 'pi pi-inbox' },
    [PoStatus.EnvironmentSetup]: { labelKey: 'purchaseOrders.statuses.environmentSetup', bgClass: 'bg-indigo-50', textClass: 'text-indigo-700', icon: 'pi pi-cog' },
    [PoStatus.Training]: { labelKey: 'purchaseOrders.statuses.training', bgClass: 'bg-purple-50', textClass: 'text-purple-700', icon: 'pi pi-users' },
    [PoStatus.FinalAcceptance]: { labelKey: 'purchaseOrders.statuses.finalAcceptance', bgClass: 'bg-cyan-50', textClass: 'text-cyan-700', icon: 'pi pi-verified' },
    [PoStatus.ActiveOperation]: { labelKey: 'purchaseOrders.statuses.activeOperation', bgClass: 'bg-emerald-50', textClass: 'text-emerald-700', icon: 'pi pi-check-circle' },
    [PoStatus.RenewalWindow]: { labelKey: 'purchaseOrders.statuses.renewalWindow', bgClass: 'bg-amber-50', textClass: 'text-amber-700', icon: 'pi pi-clock' },
    [PoStatus.Renewed]: { labelKey: 'purchaseOrders.statuses.renewed', bgClass: 'bg-teal-50', textClass: 'text-teal-700', icon: 'pi pi-refresh' },
    [PoStatus.Cancelled]: { labelKey: 'purchaseOrders.statuses.cancelled', bgClass: 'bg-red-50', textClass: 'text-red-700', icon: 'pi pi-times-circle' },
  }
  return config[status] || { labelKey: 'common.noData', bgClass: 'bg-slate-100', textClass: 'text-slate-600', icon: 'pi pi-question-circle' }
}

/** PO lifecycle stages for the stepper */
const lifecycleStages = [
  { status: PoStatus.Received, labelKey: 'purchaseOrders.statuses.received', icon: 'pi pi-inbox' },
  { status: PoStatus.EnvironmentSetup, labelKey: 'purchaseOrders.statuses.environmentSetup', icon: 'pi pi-cog' },
  { status: PoStatus.Training, labelKey: 'purchaseOrders.statuses.training', icon: 'pi pi-users' },
  { status: PoStatus.FinalAcceptance, labelKey: 'purchaseOrders.statuses.finalAcceptance', icon: 'pi pi-verified' },
  { status: PoStatus.ActiveOperation, labelKey: 'purchaseOrders.statuses.activeOperation', icon: 'pi pi-check-circle' },
]

/** Available transitions based on current status */
const availableTransitions = computed(() => {
  if (!currentPo.value) return []
  const transitions: Record<number, { value: PoStatus; labelKey: string }[]> = {
    [PoStatus.Received]: [
      { value: PoStatus.EnvironmentSetup, labelKey: 'purchaseOrders.statuses.environmentSetup' },
      { value: PoStatus.Cancelled, labelKey: 'purchaseOrders.statuses.cancelled' },
    ],
    [PoStatus.EnvironmentSetup]: [
      { value: PoStatus.Training, labelKey: 'purchaseOrders.statuses.training' },
      { value: PoStatus.Cancelled, labelKey: 'purchaseOrders.statuses.cancelled' },
    ],
    [PoStatus.Training]: [
      { value: PoStatus.FinalAcceptance, labelKey: 'purchaseOrders.statuses.finalAcceptance' },
      { value: PoStatus.Cancelled, labelKey: 'purchaseOrders.statuses.cancelled' },
    ],
    [PoStatus.FinalAcceptance]: [
      { value: PoStatus.ActiveOperation, labelKey: 'purchaseOrders.statuses.activeOperation' },
      { value: PoStatus.Cancelled, labelKey: 'purchaseOrders.statuses.cancelled' },
    ],
    [PoStatus.ActiveOperation]: [
      { value: PoStatus.RenewalWindow, labelKey: 'purchaseOrders.statuses.renewalWindow' },
    ],
    [PoStatus.RenewalWindow]: [
      { value: PoStatus.Renewed, labelKey: 'purchaseOrders.statuses.renewed' },
      { value: PoStatus.Cancelled, labelKey: 'purchaseOrders.statuses.cancelled' },
    ],
    [PoStatus.Renewed]: [],
    [PoStatus.Cancelled]: [],
  }
  return transitions[currentPo.value.status] || []
})

/** Format date */
function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-SA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

/** Format currency */
function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-SA', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount) + ' \uFDFC'
}

/** Get tenant name */
function getTenantName(): string {
  if (!currentPo.value) return ''
  return locale.value === 'ar' ? currentPo.value.tenantNameAr : currentPo.value.tenantNameEn
}

/** Confirm status change */
async function confirmStatusChange() {
  await poStore.changePoStatus(poId.value, statusChangeForm.value)
  showStatusDialog.value = false
}

/** File upload */
const fileInput = ref<HTMLInputElement | null>(null)
async function handleFileUpload(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  await poStore.uploadDocument(poId.value, file)
  await poStore.loadPoDetail(poId.value)
}

/** Go back */
function goBack() {
  router.push({ name: 'PurchaseOrderList' })
}

onMounted(() => {
  poStore.loadPoDetail(poId.value)
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-5xl px-4 py-8 sm:px-6">
      <!-- Back button -->
      <button
        type="button"
        class="mb-4 flex items-center gap-1 text-sm text-tertiary hover:text-primary"
        @click="goBack"
      >
        <i class="pi pi-arrow-right rotate-180 text-xs rtl:rotate-0"></i>
        {{ t('purchaseOrders.actions.backToList') }}
      </button>

      <!-- Loading -->
      <div v-if="isLoading && !currentPo" class="flex items-center justify-center py-20">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Error -->
      <div v-else-if="error && !currentPo" class="rounded-lg border border-danger/20 bg-danger/5 p-8 text-center">
        <i class="pi pi-exclamation-triangle mb-3 text-3xl text-danger"></i>
        <p class="text-sm text-danger">{{ error }}</p>
        <button type="button" class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark" @click="poStore.loadPoDetail(poId)">
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Content -->
      <template v-else-if="currentPo">
        <!-- Header -->
        <div class="mb-8 flex items-start justify-between">
          <div>
            <div class="flex items-center gap-3">
              <h1 class="text-2xl font-bold text-secondary" dir="ltr">
                {{ currentPo.poNumber }}
              </h1>
              <span
                class="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium"
                :class="[getPoStatusBadge(currentPo.status).bgClass, getPoStatusBadge(currentPo.status).textClass]"
              >
                <i :class="getPoStatusBadge(currentPo.status).icon" class="text-[10px]"></i>
                {{ t(getPoStatusBadge(currentPo.status).labelKey) }}
              </span>
            </div>
            <p class="mt-1 text-sm text-tertiary">
              {{ getTenantName() }}
            </p>
          </div>
        </div>

        <!-- Success message -->
        <div v-if="successMessage" class="mb-6 rounded-lg border border-emerald-200 bg-emerald-50 p-4">
          <div class="flex items-center gap-2">
            <i class="pi pi-check-circle text-emerald-600"></i>
            <p class="text-sm text-emerald-700">{{ t(successMessage) }}</p>
          </div>
        </div>

        <!-- Error message -->
        <div v-if="error" class="mb-6 rounded-lg border border-danger/20 bg-danger/5 p-4">
          <div class="flex items-center gap-2">
            <i class="pi pi-exclamation-triangle text-danger"></i>
            <p class="text-sm text-danger">{{ error }}</p>
          </div>
        </div>

        <!-- Lifecycle Stepper -->
        <div class="mb-8 rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-4 text-sm font-semibold text-secondary">
            {{ t('purchaseOrders.labels.lifecycle') }}
          </h2>
          <div class="flex items-center justify-between">
            <template v-for="(stage, index) in lifecycleStages" :key="stage.status">
              <div class="flex flex-col items-center gap-1">
                <div
                  class="flex h-10 w-10 items-center justify-center rounded-full text-sm"
                  :class="
                    currentPo.status >= stage.status
                      ? 'bg-primary text-white'
                      : 'bg-surface-muted text-tertiary'
                  "
                >
                  <i :class="stage.icon" class="text-sm"></i>
                </div>
                <span
                  class="text-center text-[10px] font-medium"
                  :class="currentPo.status >= stage.status ? 'text-primary' : 'text-tertiary'"
                >
                  {{ t(stage.labelKey) }}
                </span>
              </div>
              <div
                v-if="index < lifecycleStages.length - 1"
                class="mb-4 h-0.5 flex-1"
                :class="currentPo.status > stage.status ? 'bg-primary' : 'bg-surface-dim'"
              ></div>
            </template>
          </div>
        </div>

        <!-- PO Details -->
        <div class="mb-8 rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            {{ t('purchaseOrders.sections.poInfo') }}
          </h2>
          <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.poNumber') }}</span>
              <p class="text-sm font-mono font-medium text-secondary" dir="ltr">{{ currentPo.poNumber }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.tenant') }}</span>
              <p class="text-sm font-medium text-secondary">{{ getTenantName() }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.subscriptionPlan') }}</span>
              <p class="text-sm font-medium text-secondary">{{ t(`purchaseOrders.plans.${currentPo.subscriptionPlan}`) }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.totalAmount') }}</span>
              <p class="text-sm font-medium text-secondary" dir="ltr">{{ formatAmount(currentPo.totalAmount) }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.poDate') }}</span>
              <p class="text-sm text-secondary">{{ formatDate(currentPo.poDateGregorian) }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.duration') }}</span>
              <p class="text-sm text-secondary">{{ currentPo.durationMonths }} {{ t('purchaseOrders.labels.months') }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.subscriptionStart') }}</span>
              <p class="text-sm text-secondary">{{ formatDate(currentPo.subscriptionStartDate) }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.subscriptionEnd') }}</span>
              <p class="text-sm text-secondary">{{ formatDate(currentPo.subscriptionEndDate) }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.createdAt') }}</span>
              <p class="text-sm text-secondary">{{ formatDate(currentPo.createdAt) }}</p>
            </div>
          </div>
          <div v-if="currentPo.notes" class="mt-4">
            <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.notes') }}</span>
            <p class="mt-1 text-sm text-secondary">{{ currentPo.notes }}</p>
          </div>
        </div>

        <!-- Contact Info -->
        <div class="mb-8 rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            {{ t('purchaseOrders.sections.contactInfo') }}
          </h2>
          <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.contactName') }}</span>
              <p class="text-sm text-secondary">{{ currentPo.contactPersonName || '-' }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.contactEmail') }}</span>
              <p class="text-sm text-secondary" dir="ltr">{{ currentPo.contactPersonEmail || '-' }}</p>
            </div>
            <div>
              <span class="text-xs text-tertiary">{{ t('purchaseOrders.fields.contactPhone') }}</span>
              <p class="text-sm text-secondary" dir="ltr">{{ currentPo.contactPersonPhone || '-' }}</p>
            </div>
          </div>
        </div>

        <!-- Document Upload -->
        <div class="mb-8 rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-4 text-lg font-semibold text-secondary">
            {{ t('purchaseOrders.sections.document') }}
          </h2>
          <div v-if="currentPo.poDocumentUrl" class="flex items-center gap-3">
            <i class="pi pi-file-pdf text-2xl text-red-600"></i>
            <a :href="currentPo.poDocumentUrl" target="_blank" class="text-sm text-primary hover:underline" dir="ltr">
              {{ t('purchaseOrders.labels.viewDocument') }}
            </a>
          </div>
          <div v-else class="text-center">
            <i class="pi pi-upload mb-2 text-3xl text-tertiary"></i>
            <p class="mb-3 text-sm text-tertiary">{{ t('purchaseOrders.labels.noDocument') }}</p>
            <input ref="fileInput" type="file" accept=".pdf" class="hidden" @change="handleFileUpload" />
            <button
              type="button"
              class="rounded-lg bg-primary/10 px-4 py-2 text-sm font-medium text-primary hover:bg-primary/20"
              @click="fileInput?.click()"
            >
              <i class="pi pi-upload me-1 text-xs"></i>
              {{ t('purchaseOrders.actions.uploadDocument') }}
            </button>
          </div>
        </div>

        <!-- Status Transitions -->
        <div v-if="availableTransitions.length > 0" class="mb-8 rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-4 text-lg font-semibold text-secondary">
            {{ t('purchaseOrders.labels.statusTransitions') }}
          </h2>
          <div class="flex flex-wrap gap-3">
            <button
              v-for="transition in availableTransitions"
              :key="transition.value"
              type="button"
              class="flex items-center gap-2 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium transition-colors hover:bg-surface-muted"
              @click="statusChangeForm.newStatus = transition.value; showStatusDialog = true"
            >
              <i :class="[getPoStatusBadge(transition.value).icon, 'text-xs', getPoStatusBadge(transition.value).textClass]"></i>
              {{ t(transition.labelKey) }}
            </button>
          </div>
        </div>
      </template>
    </div>

    <!-- Status Change Dialog -->
    <Teleport to="body">
      <div
        v-if="showStatusDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
        @click.self="showStatusDialog = false"
      >
        <div class="mx-4 w-full max-w-md rounded-xl bg-white p-6 shadow-2xl">
          <div class="mb-4 text-center">
            <i class="pi pi-sync mb-3 text-4xl text-primary"></i>
            <h3 class="text-lg font-bold text-secondary">{{ t('purchaseOrders.dialogs.statusChangeTitle') }}</h3>
            <p class="mt-2 text-sm text-tertiary">{{ t('purchaseOrders.dialogs.statusChangeMessage') }}</p>
          </div>
          <div class="mb-4">
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('purchaseOrders.fields.notes') }}</label>
            <textarea
              v-model="statusChangeForm.notes"
              rows="3"
              class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              :placeholder="t('purchaseOrders.placeholders.statusNotes')"
            ></textarea>
          </div>
          <div class="flex items-center justify-center gap-3">
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-6 py-2 text-sm font-medium text-secondary hover:bg-surface-muted"
              @click="showStatusDialog = false"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white hover:bg-primary-dark disabled:opacity-50"
              :disabled="isSubmitting"
              @click="confirmStatusChange"
            >
              {{ t('common.confirm') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
