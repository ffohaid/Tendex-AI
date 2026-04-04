<script setup lang="ts">
/**
 * ImpersonationView.vue
 *
 * Main view for the User Impersonation feature in the Super Admin / Operator panel.
 * Provides user search, consent request/approval, and session management.
 * Protected by SuperAdmin/SupportAdmin role check.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useImpersonationStore } from '@/stores/impersonation'
import type {
  ImpersonationConsentDto,
  ImpersonationSessionDto,
  UserSearchResultDto,
} from '@/types/impersonation'

const { t } = useI18n()
const authStore = useAuthStore()
const store = useImpersonationStore()

/* ---- Access Control ---- */
const hasAccess = computed(
  () => authStore.hasRole('Operator Super Admin') || authStore.hasRole('SuperAdmin') || authStore.hasRole('SupportAdmin') || authStore.hasPermission('users.impersonate'),
)

/* ---- Tab Management ---- */
type TabKey = 'search' | 'consents' | 'sessions'
const activeTab = ref<TabKey>('search')

/* ---- User Search ---- */
const searchTerm = ref('')
const searchPage = ref(1)
const searchPageSize = ref(20)

async function handleSearch(): Promise<void> {
  await store.searchUsers({
    searchTerm: searchTerm.value || undefined,
    page: searchPage.value,
    pageSize: searchPageSize.value,
  })
}

/* ---- Consent Request ---- */
const showConsentDialog = ref(false)
const selectedUser = ref<UserSearchResultDto | null>(null)
const consentReason = ref('')
const consentTicket = ref('')
const consentPage = ref(1)
const consentStatusFilter = ref('')

function openConsentDialog(user: UserSearchResultDto): void {
  selectedUser.value = user
  consentReason.value = ''
  consentTicket.value = ''
  showConsentDialog.value = true
}

async function submitConsentRequest(): Promise<void> {
  if (!selectedUser.value || consentReason.value.length < 10) return
  const result = await store.requestConsent({
    targetUserId: selectedUser.value.id,
    reason: consentReason.value,
    ticketReference: consentTicket.value || undefined,
  })
  if (result) {
    showConsentDialog.value = false
    activeTab.value = 'consents'
    await loadConsents()
  }
}

async function loadConsents(): Promise<void> {
  await store.loadConsents({
    status: consentStatusFilter.value || undefined,
    page: consentPage.value,
    pageSize: 20,
  })
}

/* ---- Consent Approval/Rejection ---- */
const showRejectDialog = ref(false)
const rejectingConsentId = ref('')
const rejectionReason = ref('')

async function handleApproveConsent(consent: ImpersonationConsentDto): Promise<void> {
  if (!confirm(t('impersonation.confirmApprove'))) return
  await store.approveConsent(consent.id)
  await loadConsents()
}

function openRejectDialog(consent: ImpersonationConsentDto): void {
  rejectingConsentId.value = consent.id
  rejectionReason.value = ''
  showRejectDialog.value = true
}

async function submitRejection(): Promise<void> {
  if (!rejectionReason.value) return
  await store.rejectConsent(rejectingConsentId.value, {
    rejectionReason: rejectionReason.value,
  })
  showRejectDialog.value = false
  await loadConsents()
}

/* ---- Impersonation Session ---- */
const sessionPage = ref(1)
const sessionStatusFilter = ref('')

async function handleStartImpersonation(consent: ImpersonationConsentDto): Promise<void> {
  if (!confirm(t('impersonation.confirmStart'))) return
  const result = await store.startImpersonation(consent.id)
  if (result) {
    // Open target user's view in a new window
    window.open('/', '_blank')
  }
}

async function handleEndImpersonation(_session: ImpersonationSessionDto): Promise<void> {
  if (!confirm(t('impersonation.confirmEnd'))) return
  const success = await store.endImpersonation()
  if (success) {
    await loadSessions()
    // Reload the page to restore admin context
    window.location.reload()
  }
}

async function handleEndAndReload(): Promise<void> {
  await store.endImpersonation()
  window.location.reload()
}

async function loadSessions(): Promise<void> {
  await store.loadSessions({
    status: sessionStatusFilter.value || undefined,
    page: sessionPage.value,
    pageSize: 20,
  })
}

/* ---- Helpers ---- */
function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('en-US', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function getStatusClass(status: string): string {
  switch (status) {
    case 'Pending':
      return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
    case 'Approved':
      return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
    case 'Rejected':
      return 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
    case 'Expired':
      return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300'
    case 'Active':
      return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300'
    case 'Ended':
      return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-300'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

function getStatusLabel(status: string): string {
  return t(`impersonation.status.${status.toLowerCase()}`)
}

/* ---- Lifecycle ---- */
onMounted(async () => {
  if (hasAccess.value) {
    await loadConsents()
    await loadSessions()
  }
})

watch(consentStatusFilter, () => {
  consentPage.value = 1
  loadConsents()
})

watch(sessionStatusFilter, () => {
  sessionPage.value = 1
  loadSessions()
})
</script>

<template>
  <div class="min-h-screen">
    <!-- Access Denied -->
    <div
      v-if="!hasAccess"
      class="flex items-center justify-center min-h-[60vh]"
    >
      <div class="text-center">
        <i class="pi pi-lock text-6xl text-red-400 mb-4" />
        <h2 class="text-2xl font-bold text-gray-700 dark:text-gray-300">
          {{ t('impersonation.accessDenied') }}
        </h2>
        <p class="text-gray-500 mt-2">
          {{ t('impersonation.accessDeniedDesc') }}
        </p>
      </div>
    </div>

    <!-- Main Content -->
    <div v-else>
      <!-- Page Header -->
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
          {{ t('impersonation.title') }}
        </h1>
        <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
          {{ t('impersonation.description') }}
        </p>
      </div>

      <!-- Active Impersonation Banner -->
      <div
        v-if="store.checkActiveImpersonation()"
        class="mb-6 rounded-lg border-2 border-orange-500 bg-orange-50 dark:bg-orange-900/20 p-4"
      >
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3">
            <i class="pi pi-exclamation-triangle text-2xl text-orange-600" />
            <div>
              <p class="font-semibold text-orange-800 dark:text-orange-300">
                {{ t('impersonation.activeSession') }}
              </p>
              <p class="text-sm text-orange-600 dark:text-orange-400">
                {{ t('impersonation.activeSessionDesc') }}
              </p>
            </div>
          </div>
          <button
            class="rounded-lg bg-orange-600 px-4 py-2 text-sm font-medium text-white hover:bg-orange-700 transition-colors"
            @click="handleEndAndReload()"
          >
            {{ t('impersonation.endSession') }}
          </button>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-6 border-b border-gray-200 dark:border-gray-700">
        <nav class="-mb-px flex gap-6">
          <button
            v-for="tab in (['search', 'consents', 'sessions'] as TabKey[])"
            :key="tab"
            :class="[
              'pb-3 text-sm font-medium transition-colors border-b-2',
              activeTab === tab
                ? 'border-primary-600 text-primary-600 dark:text-primary-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300',
            ]"
            @click="activeTab = tab"
          >
            {{ t(`impersonation.tabs.${tab}`) }}
          </button>
        </nav>
      </div>

      <!-- Tab: User Search -->
      <div v-if="activeTab === 'search'">
        <div class="mb-4 flex gap-3">
          <div class="flex-1">
            <input
              v-model="searchTerm"
              type="text"
              :placeholder="t('impersonation.searchPlaceholder')"
              class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm text-gray-900 dark:text-white placeholder-gray-400 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-colors"
              @keyup.enter="handleSearch"
            />
          </div>
          <button
            class="rounded-lg bg-primary-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50 transition-colors"
            :disabled="store.isLoading"
            @click="handleSearch"
          >
            <i class="pi pi-search me-2" />
            {{ t('impersonation.search') }}
          </button>
        </div>

        <!-- Search Results -->
        <div class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.user') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.email') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.tenant') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.status') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.lastLogin') }}
                </th>
                <th class="px-4 py-3 text-end text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.actions') }}
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-900">
              <tr
                v-for="user in store.userSearchResults"
                :key="user.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
              >
                <td class="whitespace-nowrap px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                  {{ user.firstName }} {{ user.lastName }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400" dir="ltr">
                  {{ user.email }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                  {{ user.tenantName ?? '-' }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm">
                  <span
                    :class="[
                      'inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium',
                      user.isActive
                        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
                        : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
                    ]"
                  >
                    {{ user.isActive ? t('impersonation.active') : t('impersonation.inactive') }}
                  </span>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400" dir="ltr">
                  {{ formatDate(user.lastLoginAt) }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-end text-sm">
                  <button
                    v-if="user.isActive"
                    class="rounded-lg bg-amber-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-700 disabled:opacity-50 transition-colors"
                    :disabled="store.isLoading"
                    @click="openConsentDialog(user)"
                  >
                    <i class="pi pi-user-edit me-1" />
                    {{ t('impersonation.requestConsent') }}
                  </button>
                </td>
              </tr>
              <tr v-if="store.userSearchResults.length === 0 && !store.isLoading">
                <td colspan="6" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.noResults') }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Search Pagination -->
        <div
          v-if="store.userSearchTotalCount > searchPageSize"
          class="mt-4 flex items-center justify-between"
        >
          <p class="text-sm text-gray-500 dark:text-gray-400">
            {{ t('impersonation.showing', { count: store.userSearchResults.length, total: store.userSearchTotalCount }) }}
          </p>
          <div class="flex gap-2">
            <button
              :disabled="searchPage <= 1"
              class="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
              @click="searchPage--; handleSearch()"
            >
              {{ t('impersonation.previous') }}
            </button>
            <button
              :disabled="searchPage * searchPageSize >= store.userSearchTotalCount"
              class="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
              @click="searchPage++; handleSearch()"
            >
              {{ t('impersonation.next') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Tab: Consent Requests -->
      <div v-if="activeTab === 'consents'">
        <!-- Filter -->
        <div class="mb-4 flex gap-3">
          <select
            v-model="consentStatusFilter"
            class="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm text-gray-900 dark:text-white focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-colors"
          >
            <option value="">{{ t('impersonation.allStatuses') }}</option>
            <option value="Pending">{{ t('impersonation.status.pending') }}</option>
            <option value="Approved">{{ t('impersonation.status.approved') }}</option>
            <option value="Rejected">{{ t('impersonation.status.rejected') }}</option>
            <option value="Expired">{{ t('impersonation.status.expired') }}</option>
          </select>
        </div>

        <!-- Consents Table -->
        <div class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.targetUser') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.requestedBy') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.reason') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.ticket') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.requestedAt') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.status') }}
                </th>
                <th class="px-4 py-3 text-end text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.actions') }}
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-900">
              <tr
                v-for="consent in store.consents"
                :key="consent.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
              >
                <td class="whitespace-nowrap px-4 py-3 text-sm">
                  <div class="font-medium text-gray-900 dark:text-white">{{ consent.targetUserName }}</div>
                  <div class="text-xs text-gray-500 dark:text-gray-400" dir="ltr">{{ consent.targetEmail }}</div>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                  {{ consent.requestedByUserName }}
                </td>
                <td class="max-w-xs truncate px-4 py-3 text-sm text-gray-500 dark:text-gray-400" :title="consent.reason">
                  {{ consent.reason }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400" dir="ltr">
                  {{ consent.ticketReference ?? '-' }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400" dir="ltr">
                  {{ formatDate(consent.requestedAtUtc) }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm">
                  <span
                    :class="['inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium', getStatusClass(consent.status)]"
                  >
                    {{ getStatusLabel(consent.status) }}
                  </span>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-end text-sm">
                  <div class="flex justify-end gap-2">
                    <!-- Approve -->
                    <button
                      v-if="consent.status === 'Pending'"
                      class="rounded-lg bg-green-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-green-700 transition-colors"
                      :disabled="store.isLoading"
                      @click="handleApproveConsent(consent)"
                    >
                      <i class="pi pi-check me-1" />
                      {{ t('impersonation.approve') }}
                    </button>
                    <!-- Reject -->
                    <button
                      v-if="consent.status === 'Pending'"
                      class="rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700 transition-colors"
                      :disabled="store.isLoading"
                      @click="openRejectDialog(consent)"
                    >
                      <i class="pi pi-times me-1" />
                      {{ t('impersonation.reject') }}
                    </button>
                    <!-- Start Impersonation -->
                    <button
                      v-if="consent.status === 'Approved' && consent.expiresAtUtc && new Date(consent.expiresAtUtc) > new Date()"
                      class="rounded-lg bg-amber-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-700 transition-colors"
                      :disabled="store.isLoading"
                      @click="handleStartImpersonation(consent)"
                    >
                      <i class="pi pi-sign-in me-1" />
                      {{ t('impersonation.startSession') }}
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="store.consents.length === 0 && !store.isLoading">
                <td colspan="7" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.noConsents') }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Consents Pagination -->
        <div
          v-if="store.consentsTotalCount > 20"
          class="mt-4 flex items-center justify-between"
        >
          <p class="text-sm text-gray-500 dark:text-gray-400">
            {{ t('impersonation.showing', { count: store.consents.length, total: store.consentsTotalCount }) }}
          </p>
          <div class="flex gap-2">
            <button
              :disabled="consentPage <= 1"
              class="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
              @click="consentPage--; loadConsents()"
            >
              {{ t('impersonation.previous') }}
            </button>
            <button
              :disabled="consentPage * 20 >= store.consentsTotalCount"
              class="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
              @click="consentPage++; loadConsents()"
            >
              {{ t('impersonation.next') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Tab: Sessions -->
      <div v-if="activeTab === 'sessions'">
        <!-- Filter -->
        <div class="mb-4 flex gap-3">
          <select
            v-model="sessionStatusFilter"
            class="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm text-gray-900 dark:text-white focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-colors"
          >
            <option value="">{{ t('impersonation.allStatuses') }}</option>
            <option value="Active">{{ t('impersonation.status.active') }}</option>
            <option value="Ended">{{ t('impersonation.status.ended') }}</option>
            <option value="Expired">{{ t('impersonation.status.expired') }}</option>
          </select>
        </div>

        <!-- Sessions Table -->
        <div class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.admin') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.targetUser') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.tenant') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.reason') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.startedAt') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.endedAt') }}
                </th>
                <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.status') }}
                </th>
                <th class="px-4 py-3 text-end text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.table.actions') }}
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-900">
              <tr
                v-for="session in store.sessions"
                :key="session.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
              >
                <td class="whitespace-nowrap px-4 py-3 text-sm">
                  <div class="font-medium text-gray-900 dark:text-white">{{ session.adminUserName }}</div>
                  <div class="text-xs text-gray-500 dark:text-gray-400" dir="ltr">{{ session.adminEmail }}</div>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm">
                  <div class="font-medium text-gray-900 dark:text-white">{{ session.targetUserName }}</div>
                  <div class="text-xs text-gray-500 dark:text-gray-400" dir="ltr">{{ session.targetEmail }}</div>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
                  {{ session.targetTenantName }}
                </td>
                <td class="max-w-xs truncate px-4 py-3 text-sm text-gray-500 dark:text-gray-400" :title="session.reason">
                  {{ session.reason }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400" dir="ltr">
                  {{ formatDate(session.startedAtUtc) }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500 dark:text-gray-400" dir="ltr">
                  {{ formatDate(session.endedAtUtc) }}
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-sm">
                  <span
                    :class="['inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium', getStatusClass(session.status)]"
                  >
                    {{ getStatusLabel(session.status) }}
                  </span>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-end text-sm">
                  <button
                    v-if="session.status === 'Active'"
                    class="rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700 transition-colors"
                    :disabled="store.isLoading"
                    @click="handleEndImpersonation(session)"
                  >
                    <i class="pi pi-sign-out me-1" />
                    {{ t('impersonation.endSession') }}
                  </button>
                </td>
              </tr>
              <tr v-if="store.sessions.length === 0 && !store.isLoading">
                <td colspan="8" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                  {{ t('impersonation.noSessions') }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Sessions Pagination -->
        <div
          v-if="store.sessionsTotalCount > 20"
          class="mt-4 flex items-center justify-between"
        >
          <p class="text-sm text-gray-500 dark:text-gray-400">
            {{ t('impersonation.showing', { count: store.sessions.length, total: store.sessionsTotalCount }) }}
          </p>
          <div class="flex gap-2">
            <button
              :disabled="sessionPage <= 1"
              class="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
              @click="sessionPage--; loadSessions()"
            >
              {{ t('impersonation.previous') }}
            </button>
            <button
              :disabled="sessionPage * 20 >= store.sessionsTotalCount"
              class="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
              @click="sessionPage++; loadSessions()"
            >
              {{ t('impersonation.next') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div
        v-if="store.isLoading"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/20"
      >
        <div class="rounded-lg bg-white dark:bg-gray-800 p-6 shadow-xl">
          <i class="pi pi-spin pi-spinner text-3xl text-primary-600" />
          <p class="mt-2 text-sm text-gray-500 dark:text-gray-400">
            {{ t('impersonation.loading') }}
          </p>
        </div>
      </div>

      <!-- Error Toast -->
      <div
        v-if="store.error"
        class="fixed bottom-4 end-4 z-50 max-w-md rounded-lg border border-red-200 bg-red-50 dark:bg-red-900/20 dark:border-red-800 p-4 shadow-lg"
      >
        <div class="flex items-start gap-3">
          <i class="pi pi-exclamation-circle text-red-600 mt-0.5" />
          <div class="flex-1">
            <p class="text-sm font-medium text-red-800 dark:text-red-300">
              {{ store.error }}
            </p>
          </div>
          <button
            class="text-red-400 hover:text-red-600 transition-colors"
            @click="store.error = null"
          >
            <i class="pi pi-times" />
          </button>
        </div>
      </div>

      <!-- Consent Request Dialog -->
      <Teleport to="body">
        <div
          v-if="showConsentDialog"
          class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
          @click.self="showConsentDialog = false"
        >
          <div class="w-full max-w-lg rounded-xl bg-white dark:bg-gray-800 p-6 shadow-2xl">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              {{ t('impersonation.consentDialog.title') }}
            </h3>

            <!-- Target User Info -->
            <div
              v-if="selectedUser"
              class="mb-4 rounded-lg bg-gray-50 dark:bg-gray-700/50 p-3"
            >
              <p class="text-sm font-medium text-gray-900 dark:text-white">
                {{ selectedUser.firstName }} {{ selectedUser.lastName }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400" dir="ltr">
                {{ selectedUser.email }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">
                {{ selectedUser.tenantName }}
              </p>
            </div>

            <!-- Reason -->
            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {{ t('impersonation.consentDialog.reason') }} *
              </label>
              <textarea
                v-model="consentReason"
                rows="3"
                :placeholder="t('impersonation.consentDialog.reasonPlaceholder')"
                class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm text-gray-900 dark:text-white placeholder-gray-400 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-colors"
              />
              <p
                v-if="consentReason.length > 0 && consentReason.length < 10"
                class="mt-1 text-xs text-red-500"
              >
                {{ t('impersonation.consentDialog.reasonMinLength') }}
              </p>
            </div>

            <!-- Ticket Reference -->
            <div class="mb-6">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {{ t('impersonation.consentDialog.ticket') }}
              </label>
              <input
                v-model="consentTicket"
                type="text"
                :placeholder="t('impersonation.consentDialog.ticketPlaceholder')"
                class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm text-gray-900 dark:text-white placeholder-gray-400 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-colors"
              />
            </div>

            <!-- Actions -->
            <div class="flex justify-end gap-3">
              <button
                class="rounded-lg border border-gray-300 dark:border-gray-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                @click="showConsentDialog = false"
              >
                {{ t('impersonation.cancel') }}
              </button>
              <button
                class="rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white hover:bg-amber-700 disabled:opacity-50 transition-colors"
                :disabled="consentReason.length < 10 || store.isLoading"
                @click="submitConsentRequest"
              >
                {{ t('impersonation.submitRequest') }}
              </button>
            </div>
          </div>
        </div>
      </Teleport>

      <!-- Reject Dialog -->
      <Teleport to="body">
        <div
          v-if="showRejectDialog"
          class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
          @click.self="showRejectDialog = false"
        >
          <div class="w-full max-w-lg rounded-xl bg-white dark:bg-gray-800 p-6 shadow-2xl">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              {{ t('impersonation.rejectDialog.title') }}
            </h3>

            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {{ t('impersonation.rejectDialog.reason') }} *
              </label>
              <textarea
                v-model="rejectionReason"
                rows="3"
                :placeholder="t('impersonation.rejectDialog.reasonPlaceholder')"
                class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm text-gray-900 dark:text-white placeholder-gray-400 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 transition-colors"
              />
            </div>

            <div class="flex justify-end gap-3">
              <button
                class="rounded-lg border border-gray-300 dark:border-gray-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                @click="showRejectDialog = false"
              >
                {{ t('impersonation.cancel') }}
              </button>
              <button
                class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
                :disabled="!rejectionReason || store.isLoading"
                @click="submitRejection"
              >
                {{ t('impersonation.confirmReject') }}
              </button>
            </div>
          </div>
        </div>
      </Teleport>
    </div>
  </div>
</template>
