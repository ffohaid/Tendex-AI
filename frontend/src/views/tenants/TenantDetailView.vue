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
  OperatorResetTenantAdminPasswordRequest,
  SetupTenantAdminRequest,
} from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const tenantStore = useTenantStore()

const { currentTenant, isLoading, isSubmitting, error, successMessage } =
  storeToRefs(tenantStore)

/** Active tab */
const activeTab = ref<'info' | 'branding' | 'status' | 'provisioning' | 'activity' | 'support'>('info')

/** Activity data */
const activityLogs = ref<any[]>([])
const activityLoading = ref(false)
const activityPage = ref(1)
const activityTotal = ref(0)

/** Support tickets data */
const tenantTickets = ref<any[]>([])
const ticketsLoading = ref(false)

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

/** Setup Admin dialog */
const showSetupAdminDialog = ref(false)
const setupAdminForm = ref<SetupTenantAdminRequest>({
  adminEmail: '',
  firstName: '',
  lastName: '',
  password: '',
  confirmPassword: '',
  forceChangeOnLogin: true,
})
const showSetupPassword = ref(false)
const showSetupConfirmPassword = ref(false)
const setupPasswordStrength = ref<{ level: string; label: string; color: string }>({
  level: '',
  label: '',
  color: '',
})
const setupAdminError = ref('')
const setupAdminSubmitting = ref(false)

/** Reset Admin Password dialog */
const showResetAdminPasswordDialog = ref(false)
const resetAdminPasswordForm = ref<OperatorResetTenantAdminPasswordRequest>({
  newPassword: '',
  confirmPassword: '',
  notifyAdmin: true,
  forceChangeOnLogin: true,
})
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)
const passwordStrength = ref<{ level: string; label: string; color: string }>({
  level: '',
  label: '',
  color: '',
})
const resetPasswordError = ref('')
const resetPasswordSubmitting = ref(false)

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
    [TenantStatus.EnvironmentSetup]: {
      labelKey: 'tenants.statuses.environmentSetup',
      bgClass: 'bg-blue-50',
      textClass: 'text-blue-700',
      icon: 'pi pi-cog',
    },
    [TenantStatus.Training]: {
      labelKey: 'tenants.statuses.training',
      bgClass: 'bg-indigo-50',
      textClass: 'text-indigo-700',
      icon: 'pi pi-book',
    },
    [TenantStatus.FinalAcceptance]: {
      labelKey: 'tenants.statuses.finalAcceptance',
      bgClass: 'bg-cyan-50',
      textClass: 'text-cyan-700',
      icon: 'pi pi-verified',
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
    [TenantStatus.RenewalWindow]: {
      labelKey: 'tenants.statuses.renewalWindow',
      bgClass: 'bg-yellow-50',
      textClass: 'text-yellow-700',
      icon: 'pi pi-bell',
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
      { value: TenantStatus.EnvironmentSetup, labelKey: 'tenants.statuses.environmentSetup' },
      { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
    ],
    [TenantStatus.EnvironmentSetup]: [
      { value: TenantStatus.Training, labelKey: 'tenants.statuses.training' },
      { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
    ],
    [TenantStatus.Training]: [
      { value: TenantStatus.FinalAcceptance, labelKey: 'tenants.statuses.finalAcceptance' },
      { value: TenantStatus.Cancelled, labelKey: 'tenants.statuses.cancelled' },
    ],
    [TenantStatus.FinalAcceptance]: [
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

/** Open Setup Admin dialog */
function openSetupAdminDialog() {
  setupAdminForm.value = {
    adminEmail: '',
    firstName: '',
    lastName: '',
    password: '',
    confirmPassword: '',
    forceChangeOnLogin: true,
  }
  showSetupPassword.value = false
  showSetupConfirmPassword.value = false
  setupPasswordStrength.value = { level: '', label: '', color: '' }
  setupAdminError.value = ''
  showSetupAdminDialog.value = true
}

/** Check setup password strength */
function checkSetupPasswordStrength(password: string) {
  if (!password) {
    setupPasswordStrength.value = { level: '', label: '', color: '' }
    return
  }
  let score = 0
  if (password.length >= 8) score++
  if (password.length >= 12) score++
  if (/[A-Z]/.test(password)) score++
  if (/[a-z]/.test(password)) score++
  if (/[0-9]/.test(password)) score++
  if (/[^A-Za-z0-9]/.test(password)) score++
  if (score <= 2)
    setupPasswordStrength.value = {
      level: 'weak',
      label: t('tenants.resetAdminPassword.strengthWeak'),
      color: 'bg-red-500',
    }
  else if (score <= 4)
    setupPasswordStrength.value = {
      level: 'medium',
      label: t('tenants.resetAdminPassword.strengthMedium'),
      color: 'bg-amber-500',
    }
  else
    setupPasswordStrength.value = {
      level: 'strong',
      label: t('tenants.resetAdminPassword.strengthStrong'),
      color: 'bg-green-500',
    }
}

/** Generate random password for setup admin */
function generateSetupRandomPassword() {
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const special = '@#$%&*!'
  const all = upper + lower + digits + special
  let pwd = ''
  pwd += upper[Math.floor(Math.random() * upper.length)]
  pwd += lower[Math.floor(Math.random() * lower.length)]
  pwd += digits[Math.floor(Math.random() * digits.length)]
  pwd += special[Math.floor(Math.random() * special.length)]
  for (let i = 0; i < 8; i++) pwd += all[Math.floor(Math.random() * all.length)]
  pwd = pwd.split('').sort(() => Math.random() - 0.5).join('')
  setupAdminForm.value.password = pwd
  setupAdminForm.value.confirmPassword = pwd
  showSetupPassword.value = true
  showSetupConfirmPassword.value = true
  checkSetupPasswordStrength(pwd)
}

/** Handle Setup Admin submission */
async function handleSetupAdmin() {
  setupAdminError.value = ''
  if (!setupAdminForm.value.adminEmail) {
    setupAdminError.value = t('tenants.setupAdmin.emailRequired')
    return
  }
  if (!setupAdminForm.value.firstName || !setupAdminForm.value.lastName) {
    setupAdminError.value = t('tenants.setupAdmin.nameRequired')
    return
  }
  if (setupAdminForm.value.password !== setupAdminForm.value.confirmPassword) {
    setupAdminError.value = t('tenants.resetAdminPassword.passwordMismatch')
    return
  }
  if (setupAdminForm.value.password.length < 8) {
    setupAdminError.value = t('tenants.resetAdminPassword.passwordTooShort')
    return
  }
  setupAdminSubmitting.value = true
  try {
    const success = await tenantStore.setupTenantAdmin(
      tenantId.value,
      setupAdminForm.value,
    )
    if (success) {
      showSetupAdminDialog.value = false
      setTimeout(() => {
        tenantStore.clearMessages()
      }, 5000)
    } else {
      setupAdminError.value =
        error.value || t('tenants.setupAdmin.failed')
    }
  } catch (e: any) {
    const detail =
      e?.response?.data?.detail ||
      e?.response?.data?.title ||
      e?.message ||
      ''
    setupAdminError.value = detail
      ? `${t('tenants.setupAdmin.failed')}: ${detail}`
      : t('tenants.setupAdmin.failed')
  } finally {
    setupAdminSubmitting.value = false
  }
}

/** Open Reset Admin Password dialog */
function openResetAdminPasswordDialog() {
  resetAdminPasswordForm.value = {
    newPassword: '',
    confirmPassword: '',
    notifyAdmin: true,
    forceChangeOnLogin: true,
  }
  showNewPassword.value = false
  showConfirmPassword.value = false
  passwordStrength.value = { level: '', label: '', color: '' }
  resetPasswordError.value = ''
  showResetAdminPasswordDialog.value = true
}

/** Check password strength */
function checkPasswordStrength(password: string) {
  if (!password) {
    passwordStrength.value = { level: '', label: '', color: '' }
    return
  }
  let score = 0
  if (password.length >= 8) score++
  if (password.length >= 12) score++
  if (/[A-Z]/.test(password)) score++
  if (/[a-z]/.test(password)) score++
  if (/[0-9]/.test(password)) score++
  if (/[^A-Za-z0-9]/.test(password)) score++
  if (score <= 2)
    passwordStrength.value = {
      level: 'weak',
      label: t('tenants.resetAdminPassword.strengthWeak'),
      color: 'bg-red-500',
    }
  else if (score <= 4)
    passwordStrength.value = {
      level: 'medium',
      label: t('tenants.resetAdminPassword.strengthMedium'),
      color: 'bg-amber-500',
    }
  else
    passwordStrength.value = {
      level: 'strong',
      label: t('tenants.resetAdminPassword.strengthStrong'),
      color: 'bg-green-500',
    }
}

/** Generate random password */
function generateRandomPassword() {
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const special = '@#$%&*!'
  const all = upper + lower + digits + special
  let pwd = ''
  pwd += upper[Math.floor(Math.random() * upper.length)]
  pwd += lower[Math.floor(Math.random() * lower.length)]
  pwd += digits[Math.floor(Math.random() * digits.length)]
  pwd += special[Math.floor(Math.random() * special.length)]
  for (let i = 0; i < 8; i++) pwd += all[Math.floor(Math.random() * all.length)]
  pwd = pwd
    .split('')
    .sort(() => Math.random() - 0.5)
    .join('')
  resetAdminPasswordForm.value.newPassword = pwd
  resetAdminPasswordForm.value.confirmPassword = pwd
  showNewPassword.value = true
  showConfirmPassword.value = true
  checkPasswordStrength(pwd)
}

/** Handle Reset Admin Password submission */
async function handleResetAdminPassword() {
  resetPasswordError.value = ''
  if (
    resetAdminPasswordForm.value.newPassword !==
    resetAdminPasswordForm.value.confirmPassword
  ) {
    resetPasswordError.value = t('tenants.resetAdminPassword.passwordMismatch')
    return
  }
  if (resetAdminPasswordForm.value.newPassword.length < 8) {
    resetPasswordError.value = t('tenants.resetAdminPassword.passwordTooShort')
    return
  }
  resetPasswordSubmitting.value = true
  try {
    const success = await tenantStore.resetTenantAdminPassword(
      tenantId.value,
      resetAdminPasswordForm.value,
    )
    if (success) {
      showResetAdminPasswordDialog.value = false
      setTimeout(() => {
        tenantStore.clearMessages()
      }, 5000)
    } else {
      resetPasswordError.value =
        error.value || t('tenants.resetAdminPassword.failed')
    }
  } catch (e: any) {
    const detail =
      e?.response?.data?.detail ||
      e?.response?.data?.title ||
      e?.message ||
      ''
    resetPasswordError.value = detail
      ? `${t('tenants.resetAdminPassword.failed')}: ${detail}`
      : t('tenants.resetAdminPassword.failed')
  } finally {
    resetPasswordSubmitting.value = false
  }
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

/** Load activity data when tab changes */
async function loadActivityLogs() {
  activityLoading.value = true
  try {
    const r = await import('@/services/http').then(m => m.default.get(`/v1/audit-logs?tenantId=${tenantId.value}&page=${activityPage.value}&pageSize=20`))
    activityLogs.value = r.data.items || []
    activityTotal.value = r.data.totalCount || 0
  } catch { activityLogs.value = [] }
  finally { activityLoading.value = false }
}

async function loadTenantTickets() {
  ticketsLoading.value = true
  try {
    const r = await import('@/services/http').then(m => m.default.get(`/v1/support-tickets?tenantId=${tenantId.value}&page=1&pageSize=20`))
    tenantTickets.value = r.data.items || []
  } catch { tenantTickets.value = [] }
  finally { ticketsLoading.value = false }
}

const actionTypeLabel = (at: any) => {
  const map: Record<string, string> = { Create: '\u0625\u0646\u0634\u0627\u0621', Update: '\u062a\u062d\u062f\u064a\u062b', Delete: '\u062d\u0630\u0641', Approve: '\u0627\u0639\u062a\u0645\u0627\u062f', Reject: '\u0631\u0641\u0636', Login: '\u062f\u062e\u0648\u0644', Logout: '\u062e\u0631\u0648\u062c', Access: '\u0648\u0635\u0648\u0644', Export: '\u062a\u0635\u062f\u064a\u0631', Impersonate: '\u0627\u0646\u062a\u062d\u0627\u0644', StateTransition: '\u0627\u0646\u062a\u0642\u0627\u0644' }
  return map[at] || String(at)
}

const entityTypeLabel = (et: string) => {
  const map: Record<string, string> = { Tenant: '\u062c\u0647\u0629', User: '\u0645\u0633\u062a\u062e\u062f\u0645', Role: '\u062f\u0648\u0631', Rfp: '\u0643\u0631\u0627\u0633\u0629', Committee: '\u0644\u062c\u0646\u0629', SupportTicket: '\u062a\u0630\u0643\u0631\u0629' }
  return map[et] || et
}

const ticketStatusLabel = (s: string) => {
  const map: Record<string, string> = { Open: '\u0645\u0641\u062a\u0648\u062d\u0629', InProgress: '\u0642\u064a\u062f \u0627\u0644\u0645\u0639\u0627\u0644\u062c\u0629', WaitingForCustomer: '\u0628\u0627\u0646\u062a\u0638\u0627\u0631 \u0627\u0644\u0639\u0645\u064a\u0644', WaitingForOperator: '\u0628\u0627\u0646\u062a\u0638\u0627\u0631 \u0627\u0644\u062f\u0639\u0645', Resolved: '\u062a\u0645 \u0627\u0644\u062d\u0644', Closed: '\u0645\u063a\u0644\u0642\u0629' }
  return map[s] || s
}

/** Clear messages on tab change */
watch(activeTab, (newTab) => {
  if (newTab === 'activity') loadActivityLogs()
  if (newTab === 'support') loadTenantTickets()
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
        <p class="text-sm text-danger">{{ t(error) }}</p>
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
            <a
              v-if="currentTenant.platformUrl"
              :href="currentTenant.platformUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="mt-1 inline-flex items-center gap-1 text-sm text-primary hover:underline"
              dir="ltr"
            >
              <i class="pi pi-external-link text-xs"></i>
              {{ currentTenant.platformUrl }}
            </a>
            <p v-else class="mt-1 text-sm text-tertiary" dir="ltr">
              {{ currentTenant.subdomain }}
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
        <div class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
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
          <!-- Setup Admin -->
          <button
            v-if="currentTenant.isProvisioned"
            type="button"
            class="flex items-center gap-4 rounded-lg border border-emerald-200 bg-emerald-50/50 p-4 text-start transition-colors hover:border-emerald-300 hover:bg-emerald-50"
            @click="openSetupAdminDialog"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-100">
              <i class="pi pi-user-plus text-lg text-emerald-600"></i>
            </div>
            <div>
              <p class="text-sm font-semibold text-secondary">{{ t('tenants.quickActions.setupAdmin') }}</p>
              <p class="text-xs text-tertiary">{{ t('tenants.quickActions.setupAdminDesc') }}</p>
            </div>
            <i class="pi pi-chevron-left ms-auto text-xs text-tertiary rtl:pi-chevron-right"></i>
          </button>
          <!-- Reset Admin Password -->
          <button
            v-if="currentTenant.isProvisioned"
            type="button"
            class="flex items-center gap-4 rounded-lg border border-amber-200 bg-amber-50/50 p-4 text-start transition-colors hover:border-amber-300 hover:bg-amber-50"
            @click="openResetAdminPasswordDialog"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100">
              <i class="pi pi-key text-lg text-amber-600"></i>
            </div>
            <div>
              <p class="text-sm font-semibold text-secondary">{{ t('tenants.quickActions.resetAdminPassword') }}</p>
              <p class="text-xs text-tertiary">{{ t('tenants.quickActions.resetAdminPasswordDesc') }}</p>
            </div>
            <i class="pi pi-chevron-left ms-auto text-xs text-tertiary rtl:pi-chevron-right"></i>
          </button>
          <!-- Platform URL -->
          <a
            v-if="currentTenant.platformUrl"
            :href="currentTenant.platformUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center gap-4 rounded-lg border border-blue-200 bg-blue-50/50 p-4 text-start transition-colors hover:border-blue-300 hover:bg-blue-50"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100">
              <i class="pi pi-globe text-lg text-blue-600"></i>
            </div>
            <div>
              <p class="text-sm font-semibold text-secondary">{{ t('tenants.quickActions.platformUrl') }}</p>
              <p class="text-xs text-tertiary" dir="ltr">{{ currentTenant.platformUrl }}</p>
            </div>
            <i class="pi pi-external-link ms-auto text-xs text-tertiary"></i>
          </a>
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
            <p class="text-sm text-danger">{{ t(error) }}</p>
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
              { key: 'activity', labelKey: 'tenants.tabs.activity', icon: 'pi pi-history' },
              { key: 'support', labelKey: 'tenants.tabs.support', icon: 'pi pi-ticket' },
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
                <p class="text-sm font-mono text-secondary" dir="ltr">{{ currentTenant.subdomain }}</p>
              </div>
              <div>
                <span class="text-xs text-tertiary">{{ t('tenants.fields.platformUrl') }}</span>
                <a
                  v-if="currentTenant.platformUrl"
                  :href="currentTenant.platformUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="flex items-center gap-1 text-sm font-mono text-primary hover:underline"
                  dir="ltr"
                >
                  <i class="pi pi-external-link text-[10px]"></i>
                  {{ currentTenant.platformUrl }}
                </a>
                <p v-else class="text-sm text-tertiary">-</p>
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
                <i :class="[getStatusBadge(transition.value).icon, 'text-xs', getStatusBadge(transition.value).textClass]"></i>
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

        <!-- Tab: Activity -->
        <div v-if="activeTab === 'activity'" class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">سجل نشاط الجهة</h2>
          <div v-if="activityLoading" class="flex items-center justify-center py-8">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
          <div v-else-if="activityLogs.length === 0" class="text-center py-8">
            <i class="pi pi-history text-4xl text-tertiary mb-3"></i>
            <p class="text-sm text-tertiary">لا توجد سجلات نشاط لهذه الجهة</p>
          </div>
          <div v-else class="space-y-3">
            <div class="mb-4 flex items-center justify-between">
              <p class="text-sm text-tertiary">إجمالي السجلات: {{ activityTotal }}</p>
            </div>
            <div v-for="log in activityLogs" :key="log.id" class="flex items-start gap-3 rounded-lg border border-surface-dim p-3 hover:bg-surface-muted/50 transition-colors">
              <div class="mt-0.5 flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full" :class="log.actionType === 'Create' || log.actionType === 1 ? 'bg-green-100 text-green-600' : log.actionType === 'Delete' || log.actionType === 3 ? 'bg-red-100 text-red-600' : log.actionType === 'Login' || log.actionType === 6 ? 'bg-indigo-100 text-indigo-600' : 'bg-blue-100 text-blue-600'">
                <i :class="log.actionType === 'Create' || log.actionType === 1 ? 'pi pi-plus' : log.actionType === 'Delete' || log.actionType === 3 ? 'pi pi-trash' : log.actionType === 'Login' || log.actionType === 6 ? 'pi pi-sign-in' : 'pi pi-pencil'" class="text-xs"></i>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="text-sm font-medium text-secondary">{{ actionTypeLabel(log.actionType) }}</span>
                  <span class="text-xs px-1.5 py-0.5 rounded bg-surface-muted text-tertiary">{{ entityTypeLabel(log.entityType) }}</span>
                </div>
                <div class="flex items-center gap-2 mt-1">
                  <span class="text-xs text-tertiary">{{ log.userName }}</span>
                  <span class="text-xs text-tertiary">&middot;</span>
                  <span class="text-xs text-tertiary">{{ formatDate(log.timestampUtc) }}</span>
                </div>
                <p v-if="log.reason" class="text-xs text-tertiary mt-1">{{ log.reason }}</p>
              </div>
            </div>
            <div v-if="activityTotal > 20" class="flex justify-center gap-2 pt-4">
              <button @click="activityPage--; loadActivityLogs()" :disabled="activityPage <= 1" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50">السابق</button>
              <span class="px-3 py-1 text-sm text-tertiary">{{ activityPage }}</span>
              <button @click="activityPage++; loadActivityLogs()" :disabled="activityPage * 20 >= activityTotal" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50">التالي</button>
            </div>
          </div>
        </div>

        <!-- Tab: Support -->
        <div v-if="activeTab === 'support'" class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">تذاكر الدعم الفني</h2>
          <div v-if="ticketsLoading" class="flex items-center justify-center py-8">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
          <div v-else-if="tenantTickets.length === 0" class="text-center py-8">
            <i class="pi pi-ticket text-4xl text-tertiary mb-3"></i>
            <p class="text-sm text-tertiary">لا توجد تذاكر دعم فني لهذه الجهة</p>
          </div>
          <div v-else class="space-y-3">
            <div v-for="tk in tenantTickets" :key="tk.id" class="flex items-center justify-between rounded-lg border border-surface-dim p-4 hover:bg-surface-muted/50 transition-colors">
              <div class="flex-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="text-xs font-mono text-primary">{{ tk.ticketNumber }}</span>
                  <span class="px-2 py-0.5 rounded text-xs font-medium" :class="tk.status === 'Open' ? 'bg-blue-100 text-blue-700' : tk.status === 'InProgress' ? 'bg-yellow-100 text-yellow-700' : tk.status === 'Resolved' ? 'bg-green-100 text-green-700' : tk.status === 'Closed' ? 'bg-gray-100 text-gray-700' : 'bg-orange-100 text-orange-700'">{{ ticketStatusLabel(tk.status) }}</span>
                </div>
                <h3 class="text-sm font-medium text-secondary mt-1">{{ tk.subject }}</h3>
                <p class="text-xs text-tertiary mt-0.5">{{ formatDate(tk.createdAt) }}</p>
              </div>
              <button @click="router.push({ name: 'OperatorSupportTickets' })" class="text-xs text-primary hover:underline">عرض</button>
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

    <!-- Reset Admin Password Dialog -->
    <Teleport to="body">
      <div
        v-if="showResetAdminPasswordDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showResetAdminPasswordDialog = false"
      >
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl mx-4">
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <div class="flex items-center gap-2">
              <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-50">
                <i class="pi pi-key text-amber-600"></i>
              </div>
              <h3 class="text-lg font-semibold text-secondary">
                {{ t('tenants.resetAdminPassword.title') }}
              </h3>
            </div>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showResetAdminPasswordDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Form -->
          <form class="p-6" @submit.prevent="handleResetAdminPassword">
            <!-- Tenant info header -->
            <div class="mb-5 flex items-center gap-3 rounded-lg border border-surface-dim bg-surface-ground p-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                <i class="pi pi-building text-lg"></i>
              </div>
              <div>
                <p class="text-sm font-medium text-secondary">{{ getTenantName() }}</p>
                <p class="text-xs text-tertiary" dir="ltr">{{ currentTenant?.contactPersonEmail || currentTenant?.subdomain + '.tendex.ai' }}</p>
              </div>
            </div>

            <!-- Warning -->
            <div class="mb-5 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
              <i class="pi pi-exclamation-triangle mt-0.5 text-sm"></i>
              <p>{{ t('tenants.resetAdminPassword.warning') }}</p>
            </div>

            <!-- Error -->
            <div
              v-if="resetPasswordError"
              class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              <div class="flex items-center gap-2">
                <i class="pi pi-exclamation-circle text-sm"></i>
                <p>{{ resetPasswordError }}</p>
              </div>
            </div>

            <div class="space-y-4">
              <!-- New Password -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('tenants.resetAdminPassword.newPassword') }} *
                </label>
                <div class="relative">
                  <input
                    v-model="resetAdminPasswordForm.newPassword"
                    :type="showNewPassword ? 'text' : 'password'"
                    required
                    minlength="8"
                    dir="ltr"
                    class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-10 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    :placeholder="t('tenants.resetAdminPassword.newPasswordPlaceholder')"
                    @input="checkPasswordStrength(resetAdminPasswordForm.newPassword)"
                  />
                  <button
                    type="button"
                    class="absolute end-3 top-1/2 -translate-y-1/2 text-tertiary hover:text-secondary"
                    @click="showNewPassword = !showNewPassword"
                  >
                    <i :class="['pi text-sm', showNewPassword ? 'pi-eye-slash' : 'pi-eye']"></i>
                  </button>
                </div>
                <!-- Password Strength -->
                <div v-if="passwordStrength.level" class="mt-2">
                  <div class="flex items-center gap-2">
                    <div class="h-1.5 flex-1 overflow-hidden rounded-full bg-gray-200">
                      <div
                        :class="[passwordStrength.color, 'h-full rounded-full transition-all']"
                        :style="{
                          width:
                            passwordStrength.level === 'weak'
                              ? '33%'
                              : passwordStrength.level === 'medium'
                                ? '66%'
                                : '100%',
                        }"
                      ></div>
                    </div>
                    <span
                      class="text-xs font-medium"
                      :class="
                        passwordStrength.level === 'weak'
                          ? 'text-red-600'
                          : passwordStrength.level === 'medium'
                            ? 'text-amber-600'
                            : 'text-green-600'
                      "
                    >
                      {{ passwordStrength.label }}
                    </span>
                  </div>
                </div>
              </div>

              <!-- Confirm Password -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('tenants.resetAdminPassword.confirmPassword') }} *
                </label>
                <div class="relative">
                  <input
                    v-model="resetAdminPasswordForm.confirmPassword"
                    :type="showConfirmPassword ? 'text' : 'password'"
                    required
                    minlength="8"
                    dir="ltr"
                    class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-10 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    :placeholder="t('tenants.resetAdminPassword.confirmPasswordPlaceholder')"
                  />
                  <button
                    type="button"
                    class="absolute end-3 top-1/2 -translate-y-1/2 text-tertiary hover:text-secondary"
                    @click="showConfirmPassword = !showConfirmPassword"
                  >
                    <i :class="['pi text-sm', showConfirmPassword ? 'pi-eye-slash' : 'pi-eye']"></i>
                  </button>
                </div>
                <p
                  v-if="
                    resetAdminPasswordForm.confirmPassword &&
                    resetAdminPasswordForm.newPassword !==
                      resetAdminPasswordForm.confirmPassword
                  "
                  class="mt-1 text-xs text-red-600"
                >
                  {{ t('tenants.resetAdminPassword.passwordMismatch') }}
                </p>
              </div>

              <!-- Generate Random Password -->
              <button
                type="button"
                class="flex items-center gap-2 rounded-lg border border-dashed border-primary/30 bg-primary/5 px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary/10"
                @click="generateRandomPassword"
              >
                <i class="pi pi-refresh text-sm"></i>
                {{ t('tenants.resetAdminPassword.generateRandom') }}
              </button>

              <!-- Options -->
              <div class="rounded-lg border border-surface-dim bg-surface-ground/50 p-4 space-y-3">
                <label class="flex items-center gap-3 cursor-pointer">
                  <input
                    v-model="resetAdminPasswordForm.notifyAdmin"
                    type="checkbox"
                    class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                  />
                  <div>
                    <p class="text-sm font-medium text-secondary">
                      {{ t('tenants.resetAdminPassword.notifyAdmin') }}
                    </p>
                    <p class="text-xs text-tertiary">
                      {{ t('tenants.resetAdminPassword.notifyAdminDesc') }}
                    </p>
                  </div>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                  <input
                    v-model="resetAdminPasswordForm.forceChangeOnLogin"
                    type="checkbox"
                    class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                  />
                  <div>
                    <p class="text-sm font-medium text-secondary">
                      {{ t('tenants.resetAdminPassword.forceChange') }}
                    </p>
                    <p class="text-xs text-tertiary">
                      {{ t('tenants.resetAdminPassword.forceChangeDesc') }}
                    </p>
                  </div>
                </label>
              </div>

              <!-- Password Policy -->
              <div class="rounded-lg border border-blue-100 bg-blue-50 px-4 py-3">
                <p class="mb-1 text-xs font-medium text-blue-700">
                  {{ t('tenants.resetAdminPassword.policyTitle') }}
                </p>
                <ul class="space-y-0.5 text-xs text-blue-600">
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyMinLength') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyUppercase') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyLowercase') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyDigit') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policySpecial') }}
                  </li>
                </ul>
              </div>
            </div>

            <!-- Footer -->
            <div class="mt-6 flex items-center justify-end gap-3">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground"
                @click="showResetAdminPasswordDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="
                  resetPasswordSubmitting ||
                  !resetAdminPasswordForm.newPassword ||
                  resetAdminPasswordForm.newPassword !==
                    resetAdminPasswordForm.confirmPassword
                "
                class="flex items-center gap-2 rounded-lg bg-amber-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-amber-700 disabled:opacity-50"
              >
                <i
                  v-if="resetPasswordSubmitting"
                  class="pi pi-spin pi-spinner text-xs"
                ></i>
                <i v-else class="pi pi-key text-xs"></i>
                {{ t('tenants.resetAdminPassword.submit') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Setup Admin Dialog -->
    <Teleport to="body">
      <div
        v-if="showSetupAdminDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showSetupAdminDialog = false"
      >
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl mx-4 max-h-[90vh] overflow-y-auto">
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <div class="flex items-center gap-2">
              <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-50">
                <i class="pi pi-user-plus text-emerald-600"></i>
              </div>
              <h3 class="text-lg font-semibold text-secondary">
                {{ t('tenants.setupAdmin.title') }}
              </h3>
            </div>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showSetupAdminDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Form -->
          <form class="p-6" @submit.prevent="handleSetupAdmin">
            <!-- Tenant info header -->
            <div class="mb-5 flex items-center gap-3 rounded-lg border border-surface-dim bg-surface-ground p-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                <i class="pi pi-building text-lg"></i>
              </div>
              <div>
                <p class="text-sm font-medium text-secondary">{{ getTenantName() }}</p>
                <p class="text-xs text-tertiary" dir="ltr">{{ currentTenant?.platformUrl || currentTenant?.subdomain }}</p>
              </div>
            </div>

            <!-- Info -->
            <div class="mb-5 flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-700">
              <i class="pi pi-info-circle mt-0.5 text-sm"></i>
              <p>{{ t('tenants.setupAdmin.info') }}</p>
            </div>

            <!-- Error -->
            <div
              v-if="setupAdminError"
              class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              <div class="flex items-center gap-2">
                <i class="pi pi-exclamation-circle text-sm"></i>
                <p>{{ setupAdminError }}</p>
              </div>
            </div>

            <div class="space-y-4">
              <!-- Admin Email -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('tenants.setupAdmin.adminEmail') }} *
                </label>
                <input
                  v-model="setupAdminForm.adminEmail"
                  type="email"
                  required
                  dir="ltr"
                  class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-4 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                  :placeholder="t('tenants.setupAdmin.adminEmailPlaceholder')"
                />
              </div>

              <!-- Name fields -->
              <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">
                    {{ t('tenants.setupAdmin.firstName') }} *
                  </label>
                  <input
                    v-model="setupAdminForm.firstName"
                    type="text"
                    required
                    class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-4 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    :placeholder="t('tenants.setupAdmin.firstNamePlaceholder')"
                  />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">
                    {{ t('tenants.setupAdmin.lastName') }} *
                  </label>
                  <input
                    v-model="setupAdminForm.lastName"
                    type="text"
                    required
                    class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-4 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    :placeholder="t('tenants.setupAdmin.lastNamePlaceholder')"
                  />
                </div>
              </div>

              <!-- Password -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('tenants.setupAdmin.password') }} *
                </label>
                <div class="relative">
                  <input
                    v-model="setupAdminForm.password"
                    :type="showSetupPassword ? 'text' : 'password'"
                    required
                    minlength="8"
                    dir="ltr"
                    class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-10 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    :placeholder="t('tenants.setupAdmin.passwordPlaceholder')"
                    @input="checkSetupPasswordStrength(setupAdminForm.password)"
                  />
                  <button
                    type="button"
                    class="absolute end-3 top-1/2 -translate-y-1/2 text-tertiary hover:text-secondary"
                    @click="showSetupPassword = !showSetupPassword"
                  >
                    <i :class="['pi text-sm', showSetupPassword ? 'pi-eye-slash' : 'pi-eye']"></i>
                  </button>
                </div>
                <!-- Password Strength -->
                <div v-if="setupPasswordStrength.level" class="mt-2">
                  <div class="flex items-center gap-2">
                    <div class="h-1.5 flex-1 overflow-hidden rounded-full bg-gray-200">
                      <div
                        :class="[setupPasswordStrength.color, 'h-full rounded-full transition-all']"
                        :style="{
                          width:
                            setupPasswordStrength.level === 'weak'
                              ? '33%'
                              : setupPasswordStrength.level === 'medium'
                                ? '66%'
                                : '100%',
                        }"
                      ></div>
                    </div>
                    <span
                      class="text-xs font-medium"
                      :class="
                        setupPasswordStrength.level === 'weak'
                          ? 'text-red-600'
                          : setupPasswordStrength.level === 'medium'
                            ? 'text-amber-600'
                            : 'text-green-600'
                      "
                    >
                      {{ setupPasswordStrength.label }}
                    </span>
                  </div>
                </div>
              </div>

              <!-- Confirm Password -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">
                  {{ t('tenants.setupAdmin.confirmPassword') }} *
                </label>
                <div class="relative">
                  <input
                    v-model="setupAdminForm.confirmPassword"
                    :type="showSetupConfirmPassword ? 'text' : 'password'"
                    required
                    minlength="8"
                    dir="ltr"
                    class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-10 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    :placeholder="t('tenants.setupAdmin.confirmPasswordPlaceholder')"
                  />
                  <button
                    type="button"
                    class="absolute end-3 top-1/2 -translate-y-1/2 text-tertiary hover:text-secondary"
                    @click="showSetupConfirmPassword = !showSetupConfirmPassword"
                  >
                    <i :class="['pi text-sm', showSetupConfirmPassword ? 'pi-eye-slash' : 'pi-eye']"></i>
                  </button>
                </div>
                <p
                  v-if="
                    setupAdminForm.confirmPassword &&
                    setupAdminForm.password !== setupAdminForm.confirmPassword
                  "
                  class="mt-1 text-xs text-red-600"
                >
                  {{ t('tenants.resetAdminPassword.passwordMismatch') }}
                </p>
              </div>

              <!-- Generate Random Password -->
              <button
                type="button"
                class="flex items-center gap-2 rounded-lg border border-dashed border-primary/30 bg-primary/5 px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary/10"
                @click="generateSetupRandomPassword"
              >
                <i class="pi pi-refresh text-sm"></i>
                {{ t('tenants.resetAdminPassword.generateRandom') }}
              </button>

              <!-- Options -->
              <div class="rounded-lg border border-surface-dim bg-surface-ground/50 p-4">
                <label class="flex items-center gap-3 cursor-pointer">
                  <input
                    v-model="setupAdminForm.forceChangeOnLogin"
                    type="checkbox"
                    class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                  />
                  <div>
                    <p class="text-sm font-medium text-secondary">
                      {{ t('tenants.resetAdminPassword.forceChange') }}
                    </p>
                    <p class="text-xs text-tertiary">
                      {{ t('tenants.resetAdminPassword.forceChangeDesc') }}
                    </p>
                  </div>
                </label>
              </div>

              <!-- Password Policy -->
              <div class="rounded-lg border border-blue-100 bg-blue-50 px-4 py-3">
                <p class="mb-1 text-xs font-medium text-blue-700">
                  {{ t('tenants.resetAdminPassword.policyTitle') }}
                </p>
                <ul class="space-y-0.5 text-xs text-blue-600">
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyMinLength') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyUppercase') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyLowercase') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policyDigit') }}
                  </li>
                  <li class="flex items-center gap-1">
                    <i class="pi pi-check text-xs"></i>
                    {{ t('tenants.resetAdminPassword.policySpecial') }}
                  </li>
                </ul>
              </div>
            </div>

            <!-- Footer -->
            <div class="mt-6 flex items-center justify-end gap-3">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground"
                @click="showSetupAdminDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="
                  setupAdminSubmitting ||
                  !setupAdminForm.adminEmail ||
                  !setupAdminForm.firstName ||
                  !setupAdminForm.lastName ||
                  !setupAdminForm.password ||
                  setupAdminForm.password !== setupAdminForm.confirmPassword
                "
                class="flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-emerald-700 disabled:opacity-50"
              >
                <i
                  v-if="setupAdminSubmitting"
                  class="pi pi-spin pi-spinner text-xs"
                ></i>
                <i v-else class="pi pi-user-plus text-xs"></i>
                {{ t('tenants.setupAdmin.submit') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>
