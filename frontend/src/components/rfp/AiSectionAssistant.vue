<script setup lang="ts">
/**
 * AiSectionAssistant Component
 *
 * Provides AI-powered content generation and refinement for individual RFP sections.
 * Features:
 * - Generate section content using AI with RAG-grounded citations
 * - Refine existing content with natural language feedback
 * - Show AI confidence score and citations
 * - Animated typing effect for generated content
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useRfpStore } from '@/stores/rfp'
import {
  generateSectionDraft,
  refineSectionDraft,
  type GenerateSectionDraftResult,
  type AiCitation,
} from '@/services/aiSpecificationService'

const props = defineProps<{
  sectionId: string
  sectionTitle: string
  sectionType: string
  currentContent: string
}>()

const emit = defineEmits<{
  (e: 'content-generated', content: string): void
  (e: 'content-refined', content: string): void
}>()

const { t } = useI18n()
const authStore = useAuthStore()
const rfpStore = useRfpStore()

const isGenerating = ref(false)
const isRefining = ref(false)
const showPanel = ref(false)
const showRefineInput = ref(false)
const refineFeedback = ref('')
const additionalInstructions = ref('')
const lastResult = ref<GenerateSectionDraftResult | null>(null)
const citations = ref<AiCitation[]>([])
const error = ref('')
const showCitations = ref(false)

const hasContent = computed(() => props.currentContent && props.currentContent.trim().length > 0)

const confidenceLabel = computed(() => {
  if (!lastResult.value) return ''
  const score = lastResult.value.confidenceScore
  if (score >= 0.8) return t('ai.confidence.high')
  if (score >= 0.5) return t('ai.confidence.medium')
  return t('ai.confidence.low')
})

const confidenceColor = computed(() => {
  if (!lastResult.value) return ''
  const score = lastResult.value.confidenceScore
  if (score >= 0.8) return 'text-success'
  if (score >= 0.5) return 'text-warning'
  return 'text-danger'
})

async function handleGenerate() {
  isGenerating.value = true
  error.value = ''

  try {
    const result = await generateSectionDraft({
      tenantId: authStore.tenantId || '',
      competitionId: rfpStore.formData.id || crypto.randomUUID(),
      projectNameAr: rfpStore.formData.basicInfo.projectName || '',
      projectDescriptionAr: rfpStore.formData.basicInfo.projectDescription || '',
      projectType: rfpStore.formData.basicInfo.projectType || 'general',
      estimatedBudget: rfpStore.formData.basicInfo.estimatedBudget,
      sectionType: props.sectionType || 'general',
      sectionTitleAr: props.sectionTitle,
      additionalInstructions: additionalInstructions.value || undefined,
    })

    if (result.isSuccess) {
      lastResult.value = result
      citations.value = result.citations || []
      emit('content-generated', result.contentHtml)
    } else {
      error.value = result.errorMessage || t('ai.errors.generationFailed')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage || t('ai.errors.generationFailed')
  } finally {
    isGenerating.value = false
  }
}

async function handleRefine() {
  if (!refineFeedback.value.trim()) return

  isRefining.value = true
  error.value = ''

  try {
    const result = await refineSectionDraft({
      tenantId: authStore.tenantId || '',
      competitionId: rfpStore.formData.id || crypto.randomUUID(),
      projectNameAr: rfpStore.formData.basicInfo.projectName || '',
      sectionTitleAr: props.sectionTitle,
      currentContentHtml: props.currentContent,
      userFeedbackAr: refineFeedback.value,
    })

    if (result.isSuccess) {
      citations.value = result.citations || []
      emit('content-refined', result.refinedContentHtml)
      refineFeedback.value = ''
      showRefineInput.value = false
    } else {
      error.value = result.errorMessage || t('ai.errors.refineFailed')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage || t('ai.errors.refineFailed')
  } finally {
    isRefining.value = false
  }
}

function togglePanel() {
  showPanel.value = !showPanel.value
}
</script>

<template>
  <div class="ai-section-assistant">
    <!-- AI Toggle Button -->
    <button
      type="button"
      class="group flex items-center gap-2 rounded-lg border border-ai-200 bg-gradient-to-r from-ai-50 to-white px-3 py-1.5 text-xs font-semibold text-ai-600 transition-all duration-200 hover:border-ai-300 hover:shadow-sm hover:shadow-ai-100/50"
      @click="togglePanel"
    >
      <i class="pi pi-sparkles text-xs transition-transform duration-200 group-hover:scale-110"></i>
      <span>{{ t('ai.assistSection') }}</span>
      <i
        class="pi text-[10px] transition-transform duration-200"
        :class="showPanel ? 'pi-chevron-up' : 'pi-chevron-down'"
      ></i>
    </button>

    <!-- AI Panel (Expandable) -->
    <Transition name="slide-fade">
      <div
        v-if="showPanel"
        class="mt-3 rounded-xl border border-ai-200 bg-gradient-to-br from-ai-50/50 via-white to-ai-50/30 p-4"
      >
        <!-- Panel Header -->
        <div class="mb-3 flex items-center gap-2">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-ai-100">
            <i class="pi pi-sparkles text-sm text-ai-600"></i>
          </div>
          <div>
            <h4 class="text-sm font-semibold text-ai-700">{{ t('ai.sectionAssistant') }}</h4>
            <p class="text-[11px] text-ai-500">{{ t('ai.sectionAssistantDesc') }}</p>
          </div>
        </div>

        <!-- Additional Instructions -->
        <div class="mb-3">
          <label class="mb-1 block text-xs font-medium text-secondary-600">
            {{ t('ai.additionalInstructions') }}
          </label>
          <textarea
            v-model="additionalInstructions"
            rows="2"
            class="w-full rounded-lg border border-ai-200 bg-white px-3 py-2 text-xs text-secondary-700 placeholder:text-secondary-400 focus:border-ai-400 focus:outline-none focus:ring-2 focus:ring-ai-200"
            :placeholder="t('ai.additionalInstructionsPlaceholder')"
            dir="rtl"
          ></textarea>
        </div>

        <!-- Action Buttons -->
        <div class="flex flex-wrap items-center gap-2">
          <!-- Generate Button -->
          <button
            type="button"
            class="btn-ai btn-sm flex items-center gap-1.5"
            :disabled="isGenerating"
            @click="handleGenerate"
          >
            <i
              class="pi text-xs"
              :class="isGenerating ? 'pi-spin pi-spinner' : 'pi-sparkles'"
            ></i>
            <span>{{ isGenerating ? t('ai.generating') : (hasContent ? t('ai.regenerate') : t('ai.generateContent')) }}</span>
          </button>

          <!-- Refine Button (only if content exists) -->
          <button
            v-if="hasContent"
            type="button"
            class="btn btn-sm border border-ai-200 bg-white text-ai-600 hover:bg-ai-50"
            :disabled="isRefining"
            @click="showRefineInput = !showRefineInput"
          >
            <i class="pi pi-pencil text-xs"></i>
            <span>{{ t('ai.refineContent') }}</span>
          </button>

          <!-- Show Citations -->
          <button
            v-if="citations.length > 0"
            type="button"
            class="btn btn-sm border border-secondary-200 bg-white text-secondary-600 hover:bg-secondary-50"
            @click="showCitations = !showCitations"
          >
            <i class="pi pi-book text-xs"></i>
            <span>{{ t('ai.citations') }} ({{ citations.length }})</span>
          </button>
        </div>

        <!-- Refine Input -->
        <Transition name="slide-fade">
          <div v-if="showRefineInput" class="mt-3">
            <div class="flex gap-2">
              <input
                v-model="refineFeedback"
                type="text"
                class="flex-1 rounded-lg border border-ai-200 bg-white px-3 py-2 text-xs text-secondary-700 placeholder:text-secondary-400 focus:border-ai-400 focus:outline-none"
                :placeholder="t('ai.refinePlaceholder')"
                dir="rtl"
                @keyup.enter="handleRefine"
              />
              <button
                type="button"
                class="btn-ai btn-sm"
                :disabled="isRefining || !refineFeedback.trim()"
                @click="handleRefine"
              >
                <i
                  class="pi text-xs"
                  :class="isRefining ? 'pi-spin pi-spinner' : 'pi-send'"
                ></i>
              </button>
            </div>
          </div>
        </Transition>

        <!-- Confidence Score -->
        <div v-if="lastResult" class="mt-3 flex items-center gap-3 text-xs">
          <span class="text-secondary-500">{{ t('ai.confidenceScore') }}:</span>
          <span class="font-semibold" :class="confidenceColor">
            {{ confidenceLabel }} ({{ Math.round((lastResult.confidenceScore || 0) * 100) }}%)
          </span>
          <div v-if="lastResult.suggestedImprovements?.length" class="ms-auto">
            <span class="text-secondary-400">{{ t('ai.suggestions') }}: {{ lastResult.suggestedImprovements.length }}</span>
          </div>
        </div>

        <!-- Citations Panel -->
        <Transition name="slide-fade">
          <div v-if="showCitations && citations.length > 0" class="mt-3 space-y-2">
            <h5 class="text-xs font-semibold text-secondary-600">{{ t('ai.citationsSources') }}</h5>
            <div
              v-for="(citation, idx) in citations"
              :key="idx"
              class="rounded-lg border border-secondary-100 bg-white p-2.5"
            >
              <div class="flex items-center gap-2">
                <i class="pi pi-file-pdf text-xs text-danger"></i>
                <span class="text-xs font-medium text-secondary-700">{{ citation.sourceDocument }}</span>
                <span v-if="citation.pageNumber" class="text-[10px] text-secondary-400">
                  ({{ t('ai.page') }} {{ citation.pageNumber }})
                </span>
                <span class="ms-auto rounded-full bg-primary-50 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                  {{ Math.round(citation.relevanceScore * 100) }}%
                </span>
              </div>
              <p class="mt-1 text-[11px] leading-relaxed text-secondary-500">{{ citation.excerpt }}</p>
            </div>
          </div>
        </Transition>

        <!-- Error Message -->
        <div v-if="error" class="mt-3 rounded-lg border border-danger-50 bg-danger-50 p-2.5 text-xs text-danger">
          <i class="pi pi-exclamation-circle me-1"></i>
          {{ error }}
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.3s ease;
}

.slide-fade-enter-from,
.slide-fade-leave-to {
  max-height: 0;
  opacity: 0;
  transform: translateY(-8px);
  overflow: hidden;
}

.slide-fade-enter-to,
.slide-fade-leave-from {
  max-height: 600px;
  opacity: 1;
  transform: translateY(0);
}
</style>
