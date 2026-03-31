<script setup lang="ts">
/**
 * AiSettingsView - Operator AI Configuration Management (TASK-1001)
 *
 * Manages AI provider settings stored in database (AiConfiguration table):
 * - Provider selection (Gemini, OpenAI, etc.)
 * - API key management (encrypted with AES-256)
 * - Qdrant vector DB endpoint configuration
 * - RAG settings (chunk size, overlap, embedding model)
 * - Feature flags for AI capabilities
 * - Connection testing
 *
 * CRITICAL: API keys are NEVER displayed in plain text.
 * They are encrypted before storage and decrypted in-memory only.
 */
import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost, httpPut } from '@/services/http'

const { t, locale } = useI18n()

/* ── Types ── */
interface AiProvider {
  id: string
  providerName: string
  modelName: string
  apiKeyMasked: string
  endpoint: string
  isActive: boolean
  isDefault: boolean
  lastTestedAt: string | null
  lastTestResult: 'success' | 'failed' | null
}

interface RagConfig {
  vectorDbEndpoint: string
  vectorDbCollection: string
  chunkSize: number
  chunkOverlap: number
  embeddingModel: string
  maxRetrievedChunks: number
}

interface FeatureFlag {
  key: string
  label: string
  enabled: boolean
  description: string
}

/* ── State ── */
const isLoading = ref(false)
const isSaving = ref(false)
const isTesting = ref<string | null>(null)
const testResult = ref<Record<string, 'success' | 'failed' | null>>({})

const providers = ref<AiProvider[]>([])
const ragConfig = reactive<RagConfig>({
  vectorDbEndpoint: '',
  vectorDbCollection: 'tendex_documents',
  chunkSize: 1000,
  chunkOverlap: 200,
  embeddingModel: 'text-embedding-004',
  maxRetrievedChunks: 5,
})

const featureFlags = ref<FeatureFlag[]>([
  { key: 'ai_spec_generation', label: 'AI Spec Generation', enabled: true, description: 'Generate specifications using AI' },
  { key: 'ai_evaluation', label: 'AI Evaluation Assist', enabled: true, description: 'AI-assisted offer evaluation' },
  { key: 'ai_inquiry_response', label: 'AI Inquiry Response', enabled: true, description: 'AI-powered inquiry responses' },
  { key: 'ai_document_analysis', label: 'AI Document Analysis', enabled: true, description: 'Analyze uploaded documents with AI' },
  { key: 'ai_semantic_search', label: 'Semantic Search', enabled: true, description: 'AI-powered semantic search in knowledge base' },
  { key: 'ai_arabic_generation', label: 'Arabic Text Generation', enabled: true, description: 'Generate Arabic text content' },
])

const showAddProvider = ref(false)
const newProvider = reactive({
  providerName: 'Gemini',
  modelName: 'gemini-2.5-flash',
  apiKey: '',
  endpoint: '',
})

/* ── Methods ── */
async function loadSettings(): Promise<void> {
  isLoading.value = true
  try {
    const [providersData, ragData] = await Promise.all([
      httpGet<{ items: AiProvider[] }>('/v1/operator/ai/providers'),
      httpGet<RagConfig>('/v1/operator/ai/rag-config'),
    ])
    providers.value = providersData.items
    Object.assign(ragConfig, ragData)
  } catch (err) {
    console.error('Failed to load AI settings:', err)
  } finally {
    isLoading.value = false
  }
}

async function testConnection(providerId: string): Promise<void> {
  isTesting.value = providerId
  testResult.value[providerId] = null
  try {
    const result = await httpPost<{ success: boolean }>(`/v1/operator/ai/providers/${providerId}/test`)
    testResult.value[providerId] = result.success ? 'success' : 'failed'
  } catch {
    testResult.value[providerId] = 'failed'
  } finally {
    isTesting.value = null
  }
}

async function addProvider(): Promise<void> {
  isSaving.value = true
  try {
    await httpPost('/v1/operator/ai/providers', newProvider)
    showAddProvider.value = false
    newProvider.apiKey = ''
    await loadSettings()
  } catch (err) {
    console.error('Failed to add provider:', err)
  } finally {
    isSaving.value = false
  }
}

async function toggleProvider(provider: AiProvider): Promise<void> {
  try {
    await httpPut(`/v1/operator/ai/providers/${provider.id}`, {
      isActive: !provider.isActive,
    })
    provider.isActive = !provider.isActive
  } catch (err) {
    console.error('Failed to toggle provider:', err)
  }
}

async function saveRagConfig(): Promise<void> {
  isSaving.value = true
  try {
    await httpPut('/v1/operator/ai/rag-config', ragConfig)
  } catch (err) {
    console.error('Failed to save RAG config:', err)
  } finally {
    isSaving.value = false
  }
}

async function toggleFeatureFlag(flag: FeatureFlag): Promise<void> {
  try {
    await httpPut(`/v1/operator/ai/feature-flags/${flag.key}`, {
      enabled: !flag.enabled,
    })
    flag.enabled = !flag.enabled
  } catch (err) {
    console.error('Failed to toggle feature flag:', err)
  }
}

onMounted(() => {
  loadSettings()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div>
      <h1 class="page-title">{{ t('operatorAi.title') }}</h1>
      <p class="page-description">{{ locale === 'ar' ? 'إدارة مزودي الذكاء الاصطناعي وإعدادات RAG ومفاتيح الميزات' : 'Manage AI providers, RAG configuration, and feature flags' }}</p>
    </div>

    <!-- AI Providers Section -->
    <div class="card">
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-50">
            <i class="pi pi-sparkles text-ai"></i>
          </div>
          <div>
            <h2 class="text-lg font-bold text-secondary">{{ t('operatorAi.providers') }}</h2>
            <p class="text-xs text-tertiary">{{ locale === 'ar' ? 'إعداد مزودي نماذج الذكاء الاصطناعي ومفاتيح API' : 'Configure AI model providers and API keys' }}</p>
          </div>
        </div>
        <button class="btn-ai btn-sm" @click="showAddProvider = true">
          <i class="pi pi-plus"></i>
          {{ t('operatorAi.addProvider') }}
        </button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 3" :key="i" class="skeleton h-20 rounded-xl"></div>
      </div>

      <!-- Provider Cards -->
      <div v-else class="space-y-3">
        <div
          v-for="provider in providers"
          :key="provider.id"
          class="flex items-center gap-4 rounded-xl border border-secondary-100 p-4 transition-all hover:border-ai/20"
        >
          <!-- Provider icon -->
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-ai-50">
            <i class="pi pi-sparkles text-xl text-ai"></i>
          </div>

          <!-- Info -->
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <h3 class="text-sm font-bold text-secondary">{{ provider.providerName }}</h3>
              <span class="badge" :class="provider.isActive ? 'badge-success' : 'badge-secondary'">
                {{ provider.isActive ? t('operatorAi.active') : t('operatorAi.inactive') }}
              </span>
              <span v-if="provider.isDefault" class="badge badge-primary">{{ locale === 'ar' ? 'افتراضي' : 'Default' }}</span>
            </div>
            <div class="mt-1 flex items-center gap-3 text-xs text-secondary-500">
              <span>{{ locale === "ar" ? "النموذج" : "Model" }}: {{ provider.modelName }}</span>
              <span>{{ locale === "ar" ? "المفتاح" : "Key" }}: {{ provider.apiKeyMasked }}</span>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2">
            <!-- Test Connection -->
            <button
              class="btn-ghost btn-sm"
              :disabled="isTesting === provider.id"
              @click="testConnection(provider.id)"
            >
              <i
                class="pi text-xs"
                :class="isTesting === provider.id ? 'pi-spin pi-spinner' : 'pi-wifi'"
              ></i>
              {{ t('operatorAi.test') }}
            </button>
            <!-- Test Result -->
            <span
              v-if="testResult[provider.id]"
              class="text-xs font-medium"
              :class="testResult[provider.id] === 'success' ? 'text-success' : 'text-danger'"
            >
              <i class="pi" :class="testResult[provider.id] === 'success' ? 'pi-check-circle' : 'pi-times-circle'"></i>
              {{ testResult[provider.id] === 'success' ? t('operatorAi.testSuccess') : t('operatorAi.testFailed') }}
            </span>
            <!-- Toggle -->
            <button
              class="relative h-6 w-11 rounded-full transition-colors"
              :class="provider.isActive ? 'bg-success' : 'bg-secondary-300'"
              @click="toggleProvider(provider)"
            >
              <span
                class="absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-xs transition-all"
                :class="provider.isActive ? 'start-5' : 'start-0.5'"
              ></span>
            </button>
          </div>
        </div>
      </div>

      <!-- Add Provider Form -->
      <Transition name="fade">
        <div v-if="showAddProvider" class="mt-4 rounded-xl border border-ai/20 bg-ai-50/30 p-4">
          <h3 class="mb-3 text-sm font-bold text-secondary">{{ t('operatorAi.addProvider') }}</h3>
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <label class="mb-1 block text-xs font-medium text-secondary-600">{{ t('operatorAi.provider') }}</label>
              <select v-model="newProvider.providerName" class="input text-sm">
                <option value="Gemini">Google Gemini</option>
                <option value="OpenAI">OpenAI</option>
                <option value="Anthropic">Anthropic Claude</option>
                <option value="Custom">Custom</option>
              </select>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-secondary-600">{{ t('operatorAi.model') }}</label>
              <input v-model="newProvider.modelName" class="input text-sm" placeholder="gemini-2.5-flash" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-secondary-600">{{ t('operatorAi.apiKey') }}</label>
              <input v-model="newProvider.apiKey" type="password" class="input text-sm" placeholder="sk-..." />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-secondary-600">{{ t('operatorAi.endpoint') }}</label>
              <input v-model="newProvider.endpoint" class="input text-sm" placeholder="https://..." />
            </div>
          </div>
          <div class="mt-3 flex justify-end gap-2">
            <button class="btn-ghost btn-sm" @click="showAddProvider = false">{{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}</button>
            <button class="btn-ai btn-sm" :disabled="isSaving" @click="addProvider">
              <i class="pi" :class="isSaving ? 'pi-spin pi-spinner' : 'pi-plus'"></i>
              {{ locale === 'ar' ? 'إضافة' : 'Add' }}
            </button>
          </div>
        </div>
      </Transition>
    </div>

    <!-- RAG Configuration -->
    <div class="card">
      <div class="flex items-center gap-3 mb-6">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-info-50">
          <i class="pi pi-database text-info"></i>
        </div>
        <div>
          <h2 class="text-lg font-bold text-secondary">{{ t('operatorAi.ragSettings') }}</h2>
          <p class="text-xs text-tertiary">{{ locale === 'ar' ? 'إعداد خط أنابيب RAG وقاعدة بيانات المتجهات' : 'Configure RAG pipeline and vector database' }}</p>
        </div>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">{{ locale === 'ar' ? 'نقطة نهاية قاعدة المتجهات' : 'Vector DB Endpoint' }}</label>
          <input v-model="ragConfig.vectorDbEndpoint" class="input text-sm" placeholder="http://qdrant:6333" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">{{ locale === 'ar' ? 'اسم المجموعة' : 'Collection Name' }}</label>
          <input v-model="ragConfig.vectorDbCollection" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">{{ locale === 'ar' ? 'نموذج التضمين' : 'Embedding Model' }}</label>
          <input v-model="ragConfig.embeddingModel" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">{{ locale === 'ar' ? 'حجم القطعة' : 'Chunk Size' }}</label>
          <input v-model.number="ragConfig.chunkSize" type="number" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">{{ locale === 'ar' ? 'تداخل القطع' : 'Chunk Overlap' }}</label>
          <input v-model.number="ragConfig.chunkOverlap" type="number" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">{{ locale === 'ar' ? 'أقصى عدد قطع مسترجعة' : 'Max Retrieved Chunks' }}</label>
          <input v-model.number="ragConfig.maxRetrievedChunks" type="number" class="input text-sm" />
        </div>
      </div>

      <div class="mt-4 flex justify-end">
        <button class="btn-primary btn-sm" :disabled="isSaving" @click="saveRagConfig">
          <i class="pi pi-save"></i>
          {{ locale === 'ar' ? 'حفظ إعدادات RAG' : 'Save RAG Config' }}
        </button>
      </div>
    </div>

    <!-- Feature Flags -->
    <div class="card">
      <div class="flex items-center gap-3 mb-6">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-warning-50">
          <i class="pi pi-flag text-warning"></i>
        </div>
        <div>
          <h2 class="text-lg font-bold text-secondary">{{ t('operatorAi.featureFlags') }}</h2>
          <p class="text-xs text-tertiary">{{ locale === 'ar' ? 'تفعيل أو تعطيل ميزات الذكاء الاصطناعي لكل مستأجر' : 'Enable or disable AI features per tenant' }}</p>
        </div>
      </div>

      <div class="space-y-3">
        <div
          v-for="flag in featureFlags"
          :key="flag.key"
          class="flex items-center justify-between rounded-xl border border-secondary-100 p-4"
        >
          <div>
            <h4 class="text-sm font-semibold text-secondary">{{ flag.label }}</h4>
            <p class="text-xs text-tertiary">{{ flag.description }}</p>
          </div>
          <button
            class="relative h-6 w-11 rounded-full transition-colors"
            :class="flag.enabled ? 'bg-success' : 'bg-secondary-300'"
            @click="toggleFeatureFlag(flag)"
          >
            <span
              class="absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-xs transition-all"
              :class="flag.enabled ? 'start-5' : 'start-0.5'"
            ></span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: all 0.3s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
