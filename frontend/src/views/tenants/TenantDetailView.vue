<script setup lang="ts">
/**
 * Tenant Detail View — Super Admin Portal.
 *
 * Displays full tenant information with tabs for:
 * - Basic Info & Edit
 * - Branding
 * - Status Management
 * - Provisioning
 *
 * Data is fetched dynamically from the API — NO mock data.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useTenantStore } from '@/stores/tenant'
import { TenantStatus } from '@/types/tenant'
import type {
  UpdateTenantRequest,
  UpdateTenantBrandingRequest,
  ChangeTenantStatusRequest,
} from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const tenantStore = useTenantStore()

const { currentTenant, isLoading, isSubmitting, error, successMessage } =
  storeToRefs(tenantStore)

/** Active tab */
const activeTab = ref<'info' | 'branding' | 'status' | 'provisioning'>('info')

/** Edit mode */
const isEditing = ref(false)

/** Edit form */
const editForm = ref<UpdateTenantRequest>({
  nameAr: '',
  nameEn: '',
  contactPersonName: '',
  contactPersonEmail: '',
  contactPersonPhone: '',
  notes: '',
})

/** Branding form */
const brandingForm = ref<UpdateTenantBrandingRequest>({
  logoUrl: '',
  primaryColor: '',
  secondaryColor: '',
})

/** Status change form */
const statusChangeForm = ref<ChangeTenantStatusRequest>({
  newStatus: TenantStatus.Active,
})
const showStatusDialog = ref(false)

/** Tenant ID from route */
const tenantId = computed(() => route.params.id as string)

/** Get tenant display name */
function getTenantName(): string {
  if (!currentTenant.value) return ''
  return locale.value === 'ar'
    ? currentTenant.value.nameAr
    : currentTenant.value.nameEn
}

/** Format date */
function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-SA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Status badge */
function getStatusBadge(status: TenantStatus) {
  const config: Record<
    number,
    { labelKey: string; bgClass: string; textClass: string; icon: string }
  > = {
    [TenantStatus.PendingProvisioning]: {
      labelKey: 'tenants.statuses.pendingProvisioning',
      bgClass: 'bg-amber-50',
      textClass: 'text-amber-700',
      icon: 'pi pi-clock',
    },
    [TenantStatus.Active]: {
      labelKey: 'tenants.statuses.active',
      bgClass: 'bg-emerald-50',
      textClass: 'text-emerald-700',
      icon: 'pi pi-check-circle',
    },
    [TenantStatus.Suspended]: {
      labelKey: 'tenants.statuses.suspended',
      bgClass: 'bg-orange-50',
      textClass: 'text-orange-700',
      icon: 'pi pi-pause-circle',
    },
    [TenantStatus.Cancelled]: {
      labelKey: 'tenants.statuses.cancelled',
      bgClass: 'bg-red-50',
      textClass: 'text-red-700',
      icon: 'pi pi-times-circle',
    },
    [TenantStatus.Archived]: {
      labelKey: 'tenants.statuses.archived',
      bgClass: 'bg-slate-100',
      textClass: 'text-slate-600',
      icon: 'pi pi-inbox',
    },
  }
  return (
    config[status] || {
      labelKey: 'common.noData',
      bgClass: 'bg-slate-100',
      textClass: 'text-slate-600',
      icon: 'pi pi-question-circle',
    }
  )
}

/** Available status transitions */
const availableTransitions = computed(() => {
  if (!currentTenant.value) return []
  const transitions: Record<number, { value: TenantStatus; labelKey: string }[]> = {
    [TenantStatus.PendingProvisioning]: [
      { value: TenantStatus.Active, labelKey: 'tenants.statuses.active' },
      { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
    ],
    [TenantStatus.Active]: [
      { value: TenantStatus.Suspended, labelKey: 'tenants.statuses.suspended' },
      { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
    ],
    [TenantStatus.Suspended]: [
      { value: TenantStatus.Active, labelKey: 'tenants.statuses.active' },
      { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
    ],
    [TenantStatus.Cancelled]: [
      { value: TenantStatus.Archived, labelKey: 'tenants.statuses.archived' },
    ],
    [TenantStatus.Archived]: [],
  }
  return transitions[currentTenant.value.status] || []
})

/** Populate edit form from current tenant */
function populateEditForm() {
  if (!currentTenant.value) return
  editForm.value = {
    nameAr: currentTenant.value.nameAr,
    nameEn: currentTenant.value.nameEn,
    contactPersonName: currentTenant.value.contactPersonName || '',
    contactPersonEmail: currentTenant.value.contactPersonEmail || '',
    contactPersonPhone: currentTenant.value.contactPersonPhone || '',
    notes: currentTenant.value.notes || '',
  }
}

/** Populate branding form */
function populateBrandingForm() {
  if (!currentTenant.value) return
  brandingForm.value = {
    logoUrl: currentTenant.value.logoUrl || '',
    primaryColor: currentTenant.value.primaryColor || '#1e40af',
    secondaryColor: currentTenant.value.secondaryColor || '#3b82f6',
  }
}

/** Save edit */
async function saveEdit() {
  const result = await tenantStore.updateTenant(
    tenantId.value,
    editForm.value,
  )
  if (result) {
    isEditing.value = false
  }
}

/** Save branding */
async function saveBranding() {
  await tenantStore.updateBranding(tenantId.value, brandingForm.value)
}

/** Change status */
async function confirmStatusChange() {
  await tenantStore.changeStatus(tenantId.value, statusChangeForm.value)
  showStatusDialog.value = false
}

/** Trigger provisioning */
async function triggerProvisioning() {
  await tenantStore.provision(tenantId.value)
}

/** Go back */
function goBack() {
  router.push({ name: 'TenantList' })
}

/** Load tenant on mount */
onMounted(async () => {
  await tenantStore.loadTenantDetail(tenantId.value)
  populateEditForm()
  populateBrandingForm()
})

/** Watch for tenant changes */
watch(currentTenant, () => {
  populateEditForm()
  populateBrandingForm()
})

/** Clear messages on tab change */
watch(activeTab, () => {
  tenantStore.clearMessages()
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
        {{ t('tenants.actions.backToList') }}
      </button>

      <!-- Loading -->
      <div v-if="isLoading && !currentTenant" class="flex items-center justify-center py-20">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Error -->
      <div
        v-else-if="error && !currentTenant"
        class="rounded-lg border border-danger/20 bg-danger/5 p-8 text-center"
      >
        <i class="pi pi-exclamation-triangle mb-3 text-3xl text-danger"></i>
        <p class="text-sm text-danger">{{ error }}</p>
        <button
          type="button"
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="tenantStore.loadTenantDetail(tenantId)"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Content -->
      <template v-else-if="currentTenant">
        <!-- Header -->
        <div class="mb-8 flex items-start justify-between">
          <div>
            <div class="flex items-center gap-3">
              <h1 class="text-2xl font-bold text-secondary">
                {{ getTenantName() }}
              </h1>
              <span
                class="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium"
                :class="[
                  getStatusBadge(currentTenant.status).bgClass,
                  getStatusBadge(currentTenant.status).textClass,
                ]"
              >
                <i
                  :class="getStatusBadge(currentTenant.status).icon"
                  class="text-[10px]"
                ></i>
                {{ t(getStatusBadge(currentTenant.status).labelKey) }}
              </span>
            </div>
            <p class="mt-1 text-sm text-tertiary" dir="ltr">
              {{ currentTenant.subdomain }}.tendex.ai
            </p>
          </div>
          <div class="flex items-center gap-2">
            <span
              v-if="currentTenant.isProvisioned"
              class="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-3 py-1 text-xs font-medium text-emerald-700"
            >
              <i class="pi pi-check-circle text-[10px]"></i>
              {{ t('tenants.labels.provisioned') }}
            </span>
            <span
              v-else
              class="inline-flex items-center gap-1 rounded-full bg-amber-50 px-3 py-1 text-xs font-medium text-amber-700"
            >
              <i class="pi pi-clock text-[10px]"></i>
              {{ t('tenants.labels.notProvisioned') }}
            </span>
          </div>
        </div>

        <!-- Quick action cards (TASK-604) -->
        <div class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
          <button
            type="button"
            class="flex items-center gap-4 rounded-lg border border-surface-dim bg-white p-4 text-start transition-colors hover:border-primary/30 hover:bg-primary/5"
            @click="router.push({ name: 'TenantFeatureFlags', params: { id: tenantId } })"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <i class="pi pi-flag text-lg text-primary"></i>
            </div>
            <div>
              <p class="text-sm font-semibold text-secondary">{{ t('tenants.quickActions.featureFlags') }}</p>
              <p class="text-xs text-tertiary">{{ t('tenants.quickActions.featureFlagsDesc') }}</p>
            </div>
            <i class="pi pi-chevron-left ms-auto text-xs text-tertiary rtl:pi-chevron-right"></i>
          </button>
          <button
            type="button"
            class="flex items-center gap-4 rounded-lg border border-surface-dim bg-white p-4 text-start transition-colors hover:border-primary/30 hover:bg-primary/5"
            @click="router.push({ name: 'TenantBranding', params: { id: tenantId } })"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-violet-100">
              <i class="pi pi-palette text-lg text-violet-600"></i>
            </div>
            <div>
              <p class="text-sm font-semibold text-secondary">{{ t('tenants.quickActions.branding') }}</p>
              <p class="text-xs text-tertiary">{{ t('tenants.quickActions.brandingDesc') }}</p>
            </div>
            <i class="pi pi-chevron-left ms-auto text-xs text-tertiary rtl:pi-chevron-right"></i>
          </button>
        </div>

        <!-- Success message -->
        <div
          v-if="successMessage"
          class="mb-6 rounded-lg border border-emerald-200 bg-emerald-50 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-check-circle text-emerald-600"></i>
            <p class="text-sm text-emerald-700">{{ t(successMessage) }}</p>
          </div>
        </div>

        <!-- Error message -->
        <div
          v-if="error"
          class="mb-6 rounded-lg border border-danger/20 bg-danger/5 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-exclamation-triangle text-danger"></i>
            <p class="text-sm text-danger">{{ error }}</p>
          </div>
        </div>

        <!-- Tabs -->
        <div class="mb-6 flex gap-1 rounded-lg border border-surface-dim bg-white p-1">
          <button
            v-for="tab in [
              { key: 'info', labelKey: 'tenants.tabs.info', icon: 'pi pi-info-circle' },
              { key: 'branding', labelKey: 'tenants.tabs.branding', icon: 'pi pi-palette' },
              { key: 'status', labelKey: 'tenants.tabs.status', icon: 'pi pi-sync' },
              { key: 'provisioning', labelKey: 'tenants.tabs.provisioning', icon: 'pi pi-server' },
            ]"
            :key="tab.key"
            type="button"
            class="flex items-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors"
            :class="
              activeTab === tab.key
                ? 'bg-primary text-white'
                : 'text-tertiary hover:bg-surface-muted hover:text-secondary'
            "
            @click="activeTab = tab.key as typeof activeTab"
          >
            <i :class="tab.icon" class="text-xs"></i>
            {{ t(tab.labelKey) }}
          </button>
        </div>

        <!-- Tab: Info -->
        <div v-if="activeTab === 'info'" class="rounded-lg border border-surface-dim bg-white p-6">
          <div class="mb-4 flex items-center justify-between">
            <h2 class="text-lg font-semibold text-secondary">
              {{ t('tenants.sections.basicInfo') }}
            </h2>
            <button
              v-if="!isEditing"
              type="button"
              class="flex items-center gap-1 rounded-lg bg-primary/10 px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary/20"
              @click="isEditing = true"
            >
              <i class="pi pi-pencil text-xs"></i>
              {{ t('common.edit') }}
            </button>
          </div>

          <!-- View mode -->
          <div v-if="!isEditing" class="space-y-4">
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.nameAr') }}</span>
                <p class="text-sm font-medium text-secondary">{{ currentTenant.nameAr }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.nameEn') }}</span>
                <p class="text-sm font-medium text-secondary">{{ currentTenant.nameEn }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.identifier') }}</span>
                <p class="text-sm font-mono text-secondary">{{ currentTenant.identifier }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.subdomain') }}</span>
                <p class="text-sm font-mono text-secondary" dir="ltr">{{ currentTenant.subdomain }}.tendex.ai</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.contactName') }}</span>
                <p class="text-sm text-secondary">{{ currentTenant.contactPersonName || '-' }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.contactEmail') }}</span>
                <p class="text-sm text-secondary" dir="ltr">{{ currentTenant.contactPersonEmail || '-' }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.contactPhone') }}</span>
                <p class="text-sm text-secondary" dir="ltr">{{ currentTenant.contactPersonPhone || '-' }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.createdAt') }}</span>
                <p class="text-sm text-secondary">{{ formatDate(currentTenant.createdAt) }}</p>
              </div>
            </div>
            <div v-if="currentTenant.notes">
              <span class="text-xs text-tertiary">{{ t('tenants.fields.notes') }}</span>
              <p class="mt-1 text-sm text-secondary">{{ currentTenant.notes }}</p>
            </div>
          </div>

          <!-- Edit mode -->
          <form v-else @submit.prevent="saveEdit" class="space-y-4">
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.nameAr') }}</label>
                <input v-model="editForm.nameAr" type="text" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.nameEn') }}</label>
                <input v-model="editForm.nameEn" type="text" dir="ltr" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.contactName') }}</label>
                <input v-model="editForm.contactPersonName" type="text" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.contactEmail') }}</label>
                <input v-model="editForm.contactPersonEmail" type="email" dir="ltr" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.contactPhone') }}</label>
                <input v-model="editForm.contactPersonPhone" type="tel" dir="ltr" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
              </div>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.notes') }}</label>
              <textarea v-model="editForm.notes" rows="3" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"></textarea>
            </div>
            <div class="flex items-center justify-end gap-3">
              <button type="button" class="rounded-lg border border-surface-dim px-4 py-2 text-sm text-secondary hover:bg-surface-muted" @click="isEditing = false; populateEditForm()">
                {{ t('common.cancel') }}
              </button>
              <button type="submit" class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark disabled:opacity-50" :disabled="isSubmitting">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                {{ t('common.save') }}
              </button>
            </div>
          </form>
        </div>

        <!-- Tab: Branding -->
        <div v-if="activeTab === 'branding'" class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            {{ t('tenants.tabs.branding') }}
          </h2>

          <!-- Preview -->
          <div class="mb-6 rounded-lg border border-surface-dim p-4">
            <p class="mb-3 text-xs font-medium text-tertiary">{{ t('tenants.labels.brandingPreview') }}</p>
            <div class="flex items-center gap-4">
              <div
                v-if="brandingForm.logoUrl"
                class="flex h-16 w-16 items-center justify-center overflow-hidden rounded-lg border border-surface-dim"
              >
                <img :src="brandingForm.logoUrl" alt="Logo" class="h-full w-full object-contain" />
              </div>
              <div v-else class="flex h-16 w-16 items-center justify-center rounded-lg bg-slate-100">
                <i class="pi pi-image text-2xl text-slate-400"></i>
              </div>
              <div class="flex gap-2">
                <div class="h-10 w-10 rounded-lg" :style="{ backgroundColor: brandingForm.primaryColor }"></div>
                <div class="h-10 w-10 rounded-lg" :style="{ backgroundColor: brandingForm.secondaryColor }"></div>
              </div>
            </div>
          </div>

          <form @submit.prevent="saveBranding" class="space-y-4">
            <div>
              <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.logoUrl') }}</label>
              <input v-model="brandingForm.logoUrl" type="url" dir="ltr" class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
            </div>
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.primaryColor') }}</label>
                <div class="flex items-center gap-3">
                  <input v-model="brandingForm.primaryColor" type="color" class="h-10 w-10 cursor-pointer rounded border border-surface-dim" />
                  <input v-model="brandingForm.primaryColor" type="text" dir="ltr" class="flex-1 rounded-lg border border-surface-dim px-3 py-2 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
                </div>
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-secondary">{{ t('tenants.fields.secondaryColor') }}</label>
                <div class="flex items-center gap-3">
                  <input v-model="brandingForm.secondaryColor" type="color" class="h-10 w-10 cursor-pointer rounded border border-surface-dim" />
                  <input v-model="brandingForm.secondaryColor" type="text" dir="ltr" class="flex-1 rounded-lg border border-surface-dim px-3 py-2 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20" />
                </div>
              </div>
            </div>
            <div class="flex justify-end">
              <button type="submit" class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark disabled:opacity-50" :disabled="isSubmitting">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                {{ t('common.save') }}
              </button>
            </div>
          </form>
        </div>

        <!-- Tab: Status -->
        <div v-if="activeTab === 'status'" class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            {{ t('tenants.tabs.status') }}
          </h2>

          <!-- Current status -->
          <div class="mb-6 rounded-lg border border-surface-dim p-4">
            <p class="mb-2 text-xs text-tertiary">{{ t('tenants.labels.currentStatus') }}</p>
            <span
              class="inline-flex items-center gap-1 rounded-full px-3 py-1.5 text-sm font-medium"
              :class="[
                getStatusBadge(currentTenant.status).bgClass,
                getStatusBadge(currentTenant.status).textClass,
              ]"
            >
              <i :class="getStatusBadge(currentTenant.status).icon" class="text-xs"></i>
              {{ t(getStatusBadge(currentTenant.status).labelKey) }}
            </span>
          </div>

          <!-- Transitions -->
          <div v-if="availableTransitions.length > 0">
            <p class="mb-3 text-sm font-medium text-secondary">{{ t('tenants.labels.availableTransitions') }}</p>
            <div class="flex flex-wrap gap-3">
              <button
                v-for="transition in availableTransitions"
                :key="transition.value"
                type="button"
                class="flex items-center gap-2 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium transition-colors hover:bg-surface-muted"
                @click="statusChangeForm.newStatus = transition.value; showStatusDialog = true"
              >
                <i :class="getStatusBadge(transition.value).icon" class="text-xs" :class="getStatusBadge(transition.value).textClass"></i>
                {{ t(transition.labelKey) }}
              </button>
            </div>
          </div>
          <div v-else class="text-sm text-tertiary">
            {{ t('tenants.messages.noTransitions') }}
          </div>
        </div>

        <!-- Tab: Provisioning -->
        <div v-if="activeTab === 'provisioning'" class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            {{ t('tenants.tabs.provisioning') }}
          </h2>

          <div class="space-y-4">
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.databaseName') }}</span>
                <p class="text-sm font-mono text-secondary">{{ currentTenant.databaseName || '-' }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.provisionedAt') }}</span>
                <p class="text-sm text-secondary">{{ formatDate(currentTenant.provisionedAt) }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.labels.provisioningStatus') }}</span>
                <p class="text-sm font-medium" :class="currentTenant.isProvisioned ? 'text-emerald-600' : 'text-amber-600'">
                  {{ currentTenant.isProvisioned ? t('tenants.labels.provisioned') : t('tenants.labels.notProvisioned') }}
                </p>
              </div>
            </div>

            <div v-if="!currentTenant.isProvisioned && currentTenant.status === TenantStatus.PendingProvisioning" class="mt-6">
              <button
                type="button"
                class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-bold text-white hover:bg-primary-dark disabled:opacity-50"
                :disabled="isSubmitting"
                @click="triggerProvisioning"
              >
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-sm"></i>
                <i v-else class="pi pi-server text-sm"></i>
                {{ t('tenants.actions.provision') }}
              </button>
            </div>
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
            <i class="pi pi-exclamation-triangle mb-3 text-4xl text-warning"></i>
            <h3 class="text-lg font-bold text-secondary">
              {{ t('tenants.dialogs.statusChangeTitle') }}
            </h3>
            <p class="mt-2 text-sm text-tertiary">
              {{ t('tenants.dialogs.statusChangeMessage') }}
            </p>
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
