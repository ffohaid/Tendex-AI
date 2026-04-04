<script setup lang="ts">
/**
 * TenantAiSettingsView — Per-tenant AI model configuration.
 *
 * Features:
 * - Each tenant has its own AI models (not shared)
 * - Supports deployment types: Public Cloud, Private Cloud, On-Premise, Hybrid
 * - CRUD operations for AI model configurations
 * - Provider selection (OpenAI, Azure, Anthropic, Google, Custom)
 * - API key management (encrypted in backend)
 */
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import httpClient from '@/services/http'

const { t } = useI18n()

/* ---- Types ---- */
interface AiModel {
  id: string
  provider: number
  providerName: string
  modelName: string
  endpoint: string | null
  maxTokens: number
  temperature: number
  priority: number
  isActive: boolean
  deploymentType: number
  description: string | null
}

/* ---- State ---- */
const isLoading = ref(true)
const isSaving = ref(false)
const models = ref<AiModel[]>([])
const showForm = ref(false)
const editingId = ref<string | null>(null)
const successMessage = ref('')
const errorMessage = ref('')
const showDeleteConfirm = ref(false)
const deletingId = ref<string | null>(null)

const form = reactive({
  provider: 0,
  modelName: '',
  endpoint: '',
  apiKey: '',
  maxTokens: 4096,
  temperature: 0.7,
  priority: 1,
  isActive: true,
  deploymentType: 0,
  description: '',
})

/* ---- Constants ---- */
const providers = [
  { value: 0, label: 'OpenAI' },
  { value: 1, label: 'Azure OpenAI' },
  { value: 2, label: 'Google AI' },
  { value: 3, label: 'Anthropic' },
  { value: 99, label: 'Custom / Self-Hosted' },
]

const deploymentTypes = computed(() => [
  { value: 0, label: t('tenantAiSettings.publicCloud'), icon: 'pi-cloud', color: 'text-info' },
  { value: 1, label: t('tenantAiSettings.privateCloud'), icon: 'pi-lock', color: 'text-warning' },
  { value: 2, label: t('tenantAiSettings.onPremise'), icon: 'pi-server', color: 'text-success' },
  { value: 3, label: t('tenantAiSettings.hybrid'), icon: 'pi-sitemap', color: 'text-primary' },
])

/* ---- Methods ---- */
async function loadModels(): Promise<void> {
  isLoading.value = true
  try {
    const tenantId = localStorage.getItem('tenant_id') || ''
    const response = await httpClient.get(`/v1/ai/configurations/${tenantId}`)
    models.value = response.data ?? []
  } catch {
    errorMessage.value = t('tenantAiSettings.noModels')
  } finally {
    isLoading.value = false
  }
}

function getDeploymentLabel(type: number): string {
  return deploymentTypes.value.find(d => d.value === type)?.label ?? '-'
}

function getDeploymentIcon(type: number): string {
  return deploymentTypes.value.find(d => d.value === type)?.icon ?? 'pi-question'
}

function getDeploymentColor(type: number): string {
  return deploymentTypes.value.find(d => d.value === type)?.color ?? 'text-tertiary'
}

function getProviderLabel(provider: number): string {
  return providers.find(p => p.value === provider)?.label ?? 'Unknown'
}

function openAddForm(): void {
  editingId.value = null
  form.provider = 0
  form.modelName = ''
  form.endpoint = ''
  form.apiKey = ''
  form.maxTokens = 4096
  form.temperature = 0.7
  form.priority = models.value.length + 1
  form.isActive = true
  form.deploymentType = 0
  form.description = ''
  showForm.value = true
  clearMessages()
}

function openEditForm(model: AiModel): void {
  editingId.value = model.id
  form.provider = model.provider
  form.modelName = model.modelName
  form.endpoint = model.endpoint ?? ''
  form.apiKey = '' // Don't pre-fill API key for security
  form.maxTokens = model.maxTokens
  form.temperature = model.temperature
  form.priority = model.priority
  form.isActive = model.isActive
  form.deploymentType = model.deploymentType
  form.description = model.description ?? ''
  showForm.value = true
  clearMessages()
}

function cancelForm(): void {
  showForm.value = false
  editingId.value = null
  clearMessages()
}

async function saveModel(): Promise<void> {
  isSaving.value = true
  clearMessages()
  try {
    const tenantId = localStorage.getItem('tenant_id') || ''
    const payload = {
      tenantId,
      provider: form.provider,
      modelName: form.modelName,
      endpoint: form.endpoint || null,
      apiKey: form.apiKey || null,
      maxTokens: form.maxTokens,
      temperature: form.temperature,
      priority: form.priority,
      isActive: form.isActive,
      deploymentType: form.deploymentType,
      description: form.description || null,
    }

    if (editingId.value) {
      await httpClient.put(`/v1/ai/configurations/${editingId.value}`, payload)
    } else {
      await httpClient.post('/v1/ai/configurations', payload)
    }

    await loadModels()
    showForm.value = false
    editingId.value = null
    successMessage.value = editingId.value
      ? t('tenantAiSettings.editModel')
      : t('tenantAiSettings.addModel')
    autoHideSuccess()
  } catch {
    errorMessage.value = t('activeDirectory.settingsError')
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(id: string): void {
  deletingId.value = id
  showDeleteConfirm.value = true
}

async function deleteModel(): Promise<void> {
  if (!deletingId.value) return
  try {
    await httpClient.delete(`/v1/ai/configurations/${deletingId.value}`)
    await loadModels()
    showDeleteConfirm.value = false
    deletingId.value = null
  } catch {
    errorMessage.value = t('activeDirectory.settingsError')
  }
}

function clearMessages(): void {
  successMessage.value = ''
  errorMessage.value = ''
}

function autoHideSuccess(): void {
  setTimeout(() => { successMessage.value = '' }, 4000)
}

onMounted(() => {
  loadModels()
})
</script>

<template>
  <div class="mx-auto max-w-5xl px-4 py-6 sm:px-6 lg:px-8">
    <!-- Page Header -->
    <div class="mb-8 flex items-start justify-between">
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100">
          <i class="pi pi-sparkles text-lg text-ai-600"></i>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-secondary">{{ t('tenantAiSettings.title') }}</h1>
          <p class="mt-0.5 text-sm text-tertiary">{{ t('tenantAiSettings.subtitle') }}</p>
        </div>
      </div>
      <button
        v-if="!showForm"
        type="button"
        class="flex items-center gap-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
        @click="openAddForm"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ t('tenantAiSettings.addModel') }}
      </button>
    </div>

    <!-- Success/Error Banners -->
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
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
    >
      <div v-if="errorMessage" class="mb-6 flex items-center gap-3 rounded-xl border border-danger/20 bg-danger/5 px-4 py-3">
        <i class="pi pi-exclamation-circle text-lg text-danger"></i>
        <span class="text-sm font-medium text-danger">{{ errorMessage }}</span>
      </div>
    </Transition>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <i class="pi pi-spinner pi-spin text-3xl text-primary"></i>
    </div>

    <!-- Add/Edit Form -->
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-4 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="-translate-y-4 opacity-0"
    >
      <div v-if="showForm" class="mb-6 overflow-hidden rounded-2xl border border-primary/20 bg-white shadow-sm">
        <div class="border-b border-secondary-100 bg-primary/5 px-6 py-4">
          <h3 class="text-base font-semibold text-secondary">
            {{ editingId ? t('tenantAiSettings.editModel') : t('tenantAiSettings.addModel') }}
          </h3>
        </div>
        <div class="space-y-5 px-6 py-5">
          <!-- Deployment Type Selection -->
          <div>
            <label class="mb-2 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.deploymentType') }}</label>
            <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <button
                v-for="dt in deploymentTypes"
                :key="dt.value"
                type="button"
                class="flex flex-col items-center gap-2 rounded-xl border-2 px-3 py-4 text-center transition-all"
                :class="form.deploymentType === dt.value
                  ? 'border-primary bg-primary/5'
                  : 'border-secondary-200 hover:border-secondary-300'"
                @click="form.deploymentType = dt.value"
              >
                <i :class="['pi', dt.icon, dt.color]" class="text-xl"></i>
                <span class="text-xs font-medium text-secondary">{{ dt.label }}</span>
              </button>
            </div>
          </div>

          <!-- Provider & Model Name -->
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.provider') }}</label>
              <select
                v-model.number="form.provider"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              >
                <option v-for="p in providers" :key="p.value" :value="p.value">{{ p.label }}</option>
              </select>
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.modelName') }}</label>
              <input
                v-model="form.modelName"
                type="text"
                dir="ltr"
                placeholder="e.g., gpt-4o, claude-3.5-sonnet"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>

          <!-- Endpoint & API Key -->
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.endpoint') }}</label>
              <input
                v-model="form.endpoint"
                type="text"
                dir="ltr"
                placeholder="https://api.openai.com/v1"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.apiKey') }}</label>
              <input
                v-model="form.apiKey"
                type="password"
                dir="ltr"
                placeholder="sk-..."
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>

          <!-- Description -->
          <div>
            <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.description') }}</label>
            <textarea
              v-model="form.description"
              rows="2"
              :placeholder="t('tenantAiSettings.descriptionPlaceholder')"
              class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
            ></textarea>
          </div>

          <!-- Max Tokens, Temperature, Priority -->
          <div class="grid grid-cols-1 gap-5 sm:grid-cols-3">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.maxTokens') }}</label>
              <input
                v-model.number="form.maxTokens"
                type="number"
                min="100"
                max="128000"
                dir="ltr"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.temperature') }}</label>
              <input
                v-model.number="form.temperature"
                type="number"
                min="0"
                max="2"
                step="0.1"
                dir="ltr"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('tenantAiSettings.priority') }}</label>
              <input
                v-model.number="form.priority"
                type="number"
                min="1"
                max="100"
                dir="ltr"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>

          <!-- Actions -->
          <div class="flex justify-end gap-3 border-t border-secondary-100 pt-4">
            <button
              type="button"
              class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
              :disabled="isSaving"
              @click="cancelForm"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
              :disabled="isSaving || !form.modelName"
              @click="saveModel"
            >
              <i v-if="isSaving" class="pi pi-spinner pi-spin text-sm"></i>
              {{ t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Models List -->
    <template v-if="!isLoading">
      <!-- Empty State -->
      <div
        v-if="models.length === 0 && !showForm"
        class="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-secondary-200 py-16"
      >
        <div class="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-ai-100">
          <i class="pi pi-sparkles text-2xl text-ai-600"></i>
        </div>
        <h3 class="mb-1 text-base font-semibold text-secondary">{{ t('tenantAiSettings.noModels') }}</h3>
        <p class="mb-4 text-sm text-tertiary">{{ t('tenantAiSettings.noModelsDesc') }}</p>
        <button
          type="button"
          class="flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
          @click="openAddForm"
        >
          <i class="pi pi-plus text-xs"></i>
          {{ t('tenantAiSettings.addModel') }}
        </button>
      </div>

      <!-- Models Grid -->
      <div v-else class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div
          v-for="model in models"
          :key="model.id"
          class="overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm transition-all hover:shadow-md"
        >
          <div class="flex items-center justify-between border-b border-secondary-100 px-5 py-3">
            <div class="flex items-center gap-3">
              <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-ai-100">
                <i class="pi pi-sparkles text-sm text-ai-600"></i>
              </div>
              <div>
                <h4 class="text-sm font-semibold text-secondary" dir="ltr">{{ model.modelName }}</h4>
                <p class="text-xs text-tertiary">{{ getProviderLabel(model.provider) }}</p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span
                class="inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-[10px] font-medium"
                :class="model.isActive ? 'bg-success/10 text-success' : 'bg-secondary-100 text-tertiary'"
              >
                <span class="h-1.5 w-1.5 rounded-full" :class="model.isActive ? 'bg-success' : 'bg-secondary-300'"></span>
                {{ model.isActive ? t('profile.active') : t('profile.inactive') }}
              </span>
            </div>
          </div>

          <div class="px-5 py-3">
            <!-- Deployment Type -->
            <div class="mb-3 flex items-center gap-2">
              <i :class="['pi', getDeploymentIcon(model.deploymentType), getDeploymentColor(model.deploymentType)]" class="text-sm"></i>
              <span class="text-xs font-medium text-tertiary">{{ getDeploymentLabel(model.deploymentType) }}</span>
            </div>

            <!-- Description -->
            <p v-if="model.description" class="mb-3 text-xs text-tertiary">{{ model.description }}</p>

            <!-- Stats -->
            <div class="grid grid-cols-3 gap-3">
              <div class="rounded-lg bg-surface-subtle px-3 py-2 text-center">
                <p class="text-[10px] text-tertiary">{{ t('tenantAiSettings.maxTokens') }}</p>
                <p class="text-sm font-semibold text-secondary" dir="ltr">{{ model.maxTokens.toLocaleString() }}</p>
              </div>
              <div class="rounded-lg bg-surface-subtle px-3 py-2 text-center">
                <p class="text-[10px] text-tertiary">{{ t('tenantAiSettings.temperature') }}</p>
                <p class="text-sm font-semibold text-secondary" dir="ltr">{{ model.temperature }}</p>
              </div>
              <div class="rounded-lg bg-surface-subtle px-3 py-2 text-center">
                <p class="text-[10px] text-tertiary">{{ t('tenantAiSettings.priority') }}</p>
                <p class="text-sm font-semibold text-secondary" dir="ltr">{{ model.priority }}</p>
              </div>
            </div>
          </div>

          <!-- Card Actions -->
          <div class="flex items-center justify-end gap-2 border-t border-secondary-100 px-5 py-2.5">
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium text-secondary transition-colors hover:bg-surface-subtle"
              @click="openEditForm(model)"
            >
              <i class="pi pi-pencil text-xs"></i>
              {{ t('common.edit') }}
            </button>
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium text-danger transition-colors hover:bg-danger/5"
              @click="confirmDelete(model.id)"
            >
              <i class="pi pi-trash text-xs"></i>
              {{ t('common.delete') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- Delete Confirmation Dialog -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
      >
        <div
          v-if="showDeleteConfirm"
          class="fixed inset-0 z-[100] flex items-center justify-center bg-black/40 backdrop-blur-sm"
          @click.self="showDeleteConfirm = false"
        >
          <div class="mx-4 w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl">
            <div class="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-danger/10">
              <i class="pi pi-trash text-2xl text-danger"></i>
            </div>
            <h3 class="mb-2 text-center text-lg font-bold text-secondary">{{ t('tenantAiSettings.deleteModel') }}</h3>
            <div class="flex gap-3">
              <button
                type="button"
                class="flex-1 rounded-xl border border-secondary-200 px-4 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
                @click="showDeleteConfirm = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="button"
                class="flex-1 rounded-xl bg-danger px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-danger/90"
                @click="deleteModel"
              >
                {{ t('common.delete') }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
