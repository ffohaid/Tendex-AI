<script setup lang="ts">
/**
 * ActiveDirectorySettingsView — Per-tenant Active Directory integration settings.
 *
 * Features:
 * - Optional AD integration (can be enabled/disabled per tenant)
 * - LDAP connection settings (domain, URL, base DN, bind credentials)
 * - SSL/TLS toggle
 * - Auto-provisioning of users on first AD login
 * - Test connection button
 * - Search filter configuration
 */
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import httpClient from '@/services/http'

const { t } = useI18n()

/* ---- State ---- */
const isLoading = ref(true)
const isSaving = ref(false)
const isTesting = ref(false)
const isConfigured = ref(false)
const successMessage = ref('')
const errorMessage = ref('')
const testResult = ref<{ success: boolean; message: string } | null>(null)

const form = reactive({
  isEnabled: false,
  domain: '',
  ldapUrl: '',
  baseDn: '',
  bindUsername: '',
  bindPassword: '',
  searchFilter: '(&(objectClass=user)(sAMAccountName={0}))',
  useSsl: false,
  autoProvisionUsers: false,
})

/* ---- Computed ---- */
const canSave = computed(() => {
  if (!form.isEnabled) return true
  return !!(form.domain && form.ldapUrl && form.baseDn && form.bindUsername)
})

const canTest = computed(() => {
  return !!(form.domain && form.ldapUrl && form.baseDn && form.bindUsername && form.bindPassword)
})

/* ---- Methods ---- */
async function loadConfig(): Promise<void> {
  isLoading.value = true
  try {
    const tenantId = localStorage.getItem('tenant_id')
    const response = await httpClient.get(`/v1/settings/active-directory/${tenantId}`)
    if (response.data) {
      isConfigured.value = true
      form.isEnabled = response.data.isEnabled ?? false
      form.domain = response.data.description ?? ''
      form.ldapUrl = response.data.serverUrl ?? ''
      form.baseDn = response.data.baseDn ?? ''
      form.bindUsername = response.data.bindDn ?? ''
      form.searchFilter = response.data.searchFilter || '(&(objectClass=user)(sAMAccountName={0}))'
      form.useSsl = response.data.useSsl ?? false
      form.autoProvisionUsers = false
      // Don't load password for security
      form.bindPassword = ''
    }
  } catch (err: any) {
    if (err?.response?.status !== 404) {
      errorMessage.value = t('activeDirectory.settingsError')
    }
  } finally {
    isLoading.value = false
  }
}

async function saveConfig(): Promise<void> {
  isSaving.value = true
  clearMessages()
  try {
    const tenantId = localStorage.getItem('tenant_id')
    await httpClient.put(`/v1/settings/active-directory/${tenantId}`, {
      serverUrl: form.ldapUrl,
      port: form.useSsl ? 636 : 389,
      baseDn: form.baseDn,
      bindDn: form.bindUsername || null,
      bindPassword: form.bindPassword || null,
      searchFilter: form.searchFilter || null,
      useSsl: form.useSsl,
      useTls: false,
      userAttributeMapping: null,
      groupAttributeMapping: null,
      description: form.domain || null,
    })
    isConfigured.value = true
    successMessage.value = t('activeDirectory.settingsSaved')
    autoHideSuccess()
  } catch {
    errorMessage.value = t('activeDirectory.settingsError')
  } finally {
    isSaving.value = false
  }
}

async function testConnection(): Promise<void> {
  isTesting.value = true
  testResult.value = null
  clearMessages()
  try {
    const tenantId = localStorage.getItem('tenant_id')
    const response = await httpClient.post(`/v1/settings/active-directory/${tenantId}/test-connection`)
    testResult.value = {
      success: response.data.success,
      message: response.data.success
        ? t('activeDirectory.connectionSuccess')
        : (response.data.errorMessage || t('activeDirectory.connectionFailed')),
    }
  } catch {
    testResult.value = {
      success: false,
      message: t('activeDirectory.connectionFailed'),
    }
  } finally {
    isTesting.value = false
  }
}

async function toggleEnabled(): Promise<void> {
  if (!isConfigured.value && !form.isEnabled) return
  clearMessages()
  try {
    const tenantId = localStorage.getItem('tenant_id')
    await httpClient.patch(`/v1/settings/active-directory/${tenantId}/toggle`, {
      isEnabled: form.isEnabled,
    })
  } catch {
    form.isEnabled = !form.isEnabled
    errorMessage.value = t('activeDirectory.settingsError')
  }
}

function clearMessages(): void {
  successMessage.value = ''
  errorMessage.value = ''
  testResult.value = null
}

function autoHideSuccess(): void {
  setTimeout(() => {
    successMessage.value = ''
  }, 4000)
}

onMounted(() => {
  loadConfig()
})
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-6 sm:px-6 lg:px-8">
    <!-- Page Header -->
    <div class="mb-8">
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10">
          <i class="pi pi-microsoft text-lg text-primary"></i>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-secondary">{{ t('activeDirectory.title') }}</h1>
          <p class="mt-0.5 text-sm text-tertiary">{{ t('activeDirectory.subtitle') }}</p>
        </div>
      </div>
    </div>

    <!-- Optional Badge -->
    <div class="mb-6 flex items-center gap-3 rounded-xl border border-info/20 bg-info/5 px-4 py-3">
      <i class="pi pi-info-circle text-lg text-info"></i>
      <div>
        <span class="text-sm font-semibold text-info">{{ t('activeDirectory.optional') }}</span>
        <span class="ms-1 text-sm text-info/80">— {{ t('activeDirectory.optionalDesc') }}</span>
      </div>
    </div>

    <!-- Success/Error Banners -->
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="-translate-y-2 opacity-0"
    >
      <div v-if="successMessage" class="mb-6 flex items-center gap-3 rounded-xl border border-success/20 bg-success/5 px-4 py-3">
        <i class="pi pi-check-circle text-lg text-success"></i>
        <span class="text-sm font-medium text-success">{{ successMessage }}</span>
      </div>
    </Transition>

    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="-translate-y-2 opacity-0"
    >
      <div v-if="errorMessage" class="mb-6 flex items-center gap-3 rounded-xl border border-danger/20 bg-danger/5 px-4 py-3">
        <i class="pi pi-exclamation-circle text-lg text-danger"></i>
        <span class="text-sm font-medium text-danger">{{ errorMessage }}</span>
      </div>
    </Transition>

    <!-- Test Result -->
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="-translate-y-2 opacity-0"
    >
      <div
        v-if="testResult"
        class="mb-6 flex items-center gap-3 rounded-xl border px-4 py-3"
        :class="testResult.success
          ? 'border-success/20 bg-success/5'
          : 'border-danger/20 bg-danger/5'"
      >
        <i
          class="text-lg"
          :class="testResult.success
            ? 'pi pi-check-circle text-success'
            : 'pi pi-times-circle text-danger'"
        ></i>
        <span
          class="text-sm font-medium"
          :class="testResult.success ? 'text-success' : 'text-danger'"
        >{{ testResult.message }}</span>
      </div>
    </Transition>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <i class="pi pi-spinner pi-spin text-3xl text-primary"></i>
    </div>

    <template v-else>
      <!-- Enable/Disable Toggle Card -->
      <div class="mb-6 overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm">
        <div class="flex items-center justify-between px-6 py-5">
          <div class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg" :class="form.isEnabled ? 'bg-success/10' : 'bg-secondary-100'">
              <i class="pi pi-link text-lg" :class="form.isEnabled ? 'text-success' : 'text-tertiary'"></i>
            </div>
            <div>
              <h3 class="text-sm font-semibold text-secondary">{{ t('activeDirectory.enabled') }}</h3>
              <p class="text-xs text-tertiary">{{ t('activeDirectory.enabledDesc') }}</p>
            </div>
          </div>
          <label class="relative inline-flex cursor-pointer items-center">
            <input
              v-model="form.isEnabled"
              type="checkbox"
              class="peer sr-only"
              @change="toggleEnabled"
            />
            <div class="peer h-6 w-11 rounded-full bg-secondary-200 after:absolute after:start-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-secondary-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white rtl:peer-checked:after:-translate-x-full"></div>
          </label>
        </div>
      </div>

      <!-- Connection Settings Card -->
      <div
        class="mb-6 overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm transition-opacity"
        :class="{ 'pointer-events-none opacity-50': !form.isEnabled }"
      >
        <div class="border-b border-secondary-100 px-6 py-4">
          <h3 class="text-base font-semibold text-secondary">
            <i class="pi pi-cog me-2 text-primary"></i>
            {{ t('activeDirectory.connectionSettings') }}
          </h3>
        </div>

        <div class="space-y-5 px-6 py-5">
          <!-- Domain & LDAP URL -->
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('activeDirectory.domain') }}</label>
              <input
                v-model="form.domain"
                type="text"
                dir="ltr"
                :placeholder="t('activeDirectory.domainPlaceholder')"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('activeDirectory.ldapUrl') }}</label>
              <input
                v-model="form.ldapUrl"
                type="text"
                dir="ltr"
                :placeholder="t('activeDirectory.ldapUrlPlaceholder')"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>

          <!-- Base DN -->
          <div>
            <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('activeDirectory.baseDn') }}</label>
            <input
              v-model="form.baseDn"
              type="text"
              dir="ltr"
              :placeholder="t('activeDirectory.baseDnPlaceholder')"
              class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <!-- Bind Credentials -->
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('activeDirectory.bindUsername') }}</label>
              <input
                v-model="form.bindUsername"
                type="text"
                dir="ltr"
                :placeholder="t('activeDirectory.bindUsernamePlaceholder')"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('activeDirectory.bindPassword') }}</label>
              <input
                v-model="form.bindPassword"
                type="password"
                :placeholder="t('activeDirectory.bindPasswordPlaceholder')"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>

          <!-- Search Filter -->
          <div>
            <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('activeDirectory.searchFilter') }}</label>
            <input
              v-model="form.searchFilter"
              type="text"
              dir="ltr"
              :placeholder="t('activeDirectory.searchFilterPlaceholder')"
              class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 font-mono text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <!-- Toggles -->
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <!-- SSL/TLS -->
            <div class="flex items-center justify-between rounded-xl border border-secondary-100 bg-surface-subtle px-4 py-3">
              <div>
                <p class="text-sm font-medium text-secondary">{{ t('activeDirectory.useSsl') }}</p>
                <p class="text-xs text-tertiary">{{ t('activeDirectory.useSslDesc') }}</p>
              </div>
              <label class="relative inline-flex cursor-pointer items-center">
                <input v-model="form.useSsl" type="checkbox" class="peer sr-only" />
                <div class="peer h-6 w-11 rounded-full bg-secondary-200 after:absolute after:start-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-secondary-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white rtl:peer-checked:after:-translate-x-full"></div>
              </label>
            </div>

            <!-- Auto Provision -->
            <div class="flex items-center justify-between rounded-xl border border-secondary-100 bg-surface-subtle px-4 py-3">
              <div>
                <p class="text-sm font-medium text-secondary">{{ t('activeDirectory.autoProvision') }}</p>
                <p class="text-xs text-tertiary">{{ t('activeDirectory.autoProvisionDesc') }}</p>
              </div>
              <label class="relative inline-flex cursor-pointer items-center">
                <input v-model="form.autoProvisionUsers" type="checkbox" class="peer sr-only" />
                <div class="peer h-6 w-11 rounded-full bg-secondary-200 after:absolute after:start-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-secondary-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white rtl:peer-checked:after:-translate-x-full"></div>
              </label>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex flex-col gap-3 border-t border-secondary-100 pt-5 sm:flex-row sm:justify-end">
            <button
              type="button"
              class="flex items-center justify-center gap-2 rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
              :disabled="isTesting || !canTest"
              @click="testConnection"
            >
              <i :class="isTesting ? 'pi pi-spinner pi-spin' : 'pi pi-bolt'" class="text-sm"></i>
              {{ isTesting ? t('activeDirectory.testingConnection') : t('activeDirectory.testConnection') }}
            </button>
            <button
              type="button"
              class="flex items-center justify-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
              :disabled="isSaving || !canSave"
              @click="saveConfig"
            >
              <i :class="isSaving ? 'pi pi-spinner pi-spin' : 'pi pi-save'" class="text-sm"></i>
              {{ isSaving ? t('activeDirectory.savingSettings') : t('activeDirectory.saveSettings') }}
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
