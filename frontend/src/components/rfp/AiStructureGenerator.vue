<script setup lang="ts">
/**
 * AiStructureGenerator Component
 *
 * Generates a complete RFP booklet structure using AI.
 * Analyzes project type, description, and budget to suggest
 * the optimal set of sections with proper ordering.
 */
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useRfpStore } from '@/stores/rfp'
import {
  generateBookletStructure,
  type GenerateBookletStructureResult,
  type BookletSection,
} from '@/services/aiSpecificationService'

const emit = defineEmits<{
  (e: 'structure-generated', sections: BookletSection[]): void
}>()

const { t } = useI18n()
const authStore = useAuthStore()
const rfpStore = useRfpStore()

const isGenerating = ref(false)
const result = ref<GenerateBookletStructureResult | null>(null)
const error = ref('')
const showPreview = ref(false)

async function handleGenerate() {
  isGenerating.value = true
  error.value = ''
  result.value = null

  try {
    const response = await generateBookletStructure({
      tenantId: authStore.tenantId || '',
      competitionId: rfpStore.formData.id || crypto.randomUUID(),
      projectNameAr: rfpStore.formData.basicInfo.projectName || '',
      projectDescriptionAr: rfpStore.formData.basicInfo.projectDescription || '',
      projectType: (rfpStore.formData.basicInfo.competitionType as string) || 'general',
      estimatedBudget: rfpStore.formData.basicInfo.estimatedValue ?? undefined,
    })

    if (response.isSuccess) {
      result.value = response
      showPreview.value = true
    } else {
      error.value = response.errorMessage || t('ai.errors.structureGenerationFailed')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage || t('ai.errors.structureGenerationFailed')
  } finally {
    isGenerating.value = false
  }
}

function applyStructure() {
  if (result.value?.sections) {
    emit('structure-generated', result.value.sections)
    showPreview.value = false
  }
}

function getColorBadge(colorCode: string) {
  switch (colorCode) {
    case 'black':
      return { bg: 'bg-secondary-100', text: 'text-secondary-700', dot: 'bg-secondary-500' }
    case 'green':
      return { bg: 'bg-success/10', text: 'text-success', dot: 'bg-success' }
    case 'red':
      return { bg: 'bg-danger/10', text: 'text-danger', dot: 'bg-danger' }
    case 'blue':
      return { bg: 'bg-info/10', text: 'text-info', dot: 'bg-info' }
    default:
      return { bg: 'bg-secondary-50', text: 'text-secondary-500', dot: 'bg-secondary-400' }
  }
}
</script>

<template>
  <div class="ai-structure-generator">
    <!-- Generate Structure Button -->
    <button
      type="button"
      class="group flex items-center gap-2.5 rounded-xl border-2 border-dashed border-ai-300 bg-gradient-to-r from-ai-50 to-white px-5 py-3 text-sm font-semibold text-ai-600 transition-all duration-300 hover:border-ai-400 hover:shadow-lg hover:shadow-ai-100/50"
      :disabled="isGenerating"
      @click="handleGenerate"
    >
      <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100 transition-transform duration-300 group-hover:scale-110">
        <i
          class="pi text-lg text-ai-600"
          :class="isGenerating ? 'pi-spin pi-spinner' : 'pi-sparkles'"
        ></i>
      </div>
      <div class="text-start">
        <span class="block">{{ isGenerating ? t('ai.generatingStructure') : t('ai.generateStructure') }}</span>
        <span class="block text-[11px] font-normal text-ai-400">{{ t('ai.generateStructureDesc') }}</span>
      </div>
    </button>

    <!-- Error -->
    <div v-if="error" class="mt-3 rounded-lg border border-danger/20 bg-danger/5 p-3 text-xs text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ error }}
    </div>

    <!-- Preview Modal -->
    <Transition name="fade">
      <div
        v-if="showPreview && result"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm"
        @click.self="showPreview = false"
      >
        <div class="mx-4 max-h-[80vh] w-full max-w-2xl overflow-hidden rounded-2xl bg-white shadow-2xl">
          <!-- Modal Header -->
          <div class="border-b border-secondary-100 bg-gradient-to-r from-ai-50 to-white p-5">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100">
                <i class="pi pi-sparkles text-lg text-ai-600"></i>
              </div>
              <div>
                <h3 class="text-base font-bold text-secondary-800">{{ t('ai.suggestedStructure') }}</h3>
                <p class="text-xs text-secondary-500">{{ t('ai.reviewAndApply') }}</p>
              </div>
              <button
                type="button"
                class="ms-auto flex h-8 w-8 items-center justify-center rounded-lg hover:bg-secondary-100"
                @click="showPreview = false"
              >
                <i class="pi pi-times text-sm text-secondary-400"></i>
              </button>
            </div>
          </div>

          <!-- Sections List -->
          <div class="max-h-[50vh] overflow-y-auto p-5">
            <!-- Rationale -->
            <div v-if="result.rationale" class="mb-4 rounded-lg bg-ai-50 p-3">
              <p class="text-xs leading-relaxed text-ai-700">
                <i class="pi pi-info-circle me-1"></i>
                {{ result.rationale }}
              </p>
            </div>

            <!-- Sections -->
            <div class="space-y-2">
              <div
                v-for="(section, idx) in result.sections"
                :key="idx"
                class="flex items-start gap-3 rounded-lg border border-secondary-100 bg-white p-3 transition-colors hover:bg-secondary-50"
              >
                <span class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                  {{ idx + 1 }}
                </span>
                <div class="flex-1">
                  <div class="flex items-center gap-2">
                    <span class="text-sm font-semibold text-secondary-800">{{ section.sectionTitleAr }}</span>
                    <span
                      v-if="section.isRequired"
                      class="rounded-full bg-warning/10 px-1.5 py-0.5 text-[10px] font-medium text-warning"
                    >
                      {{ t('rfp.labels.required') }}
                    </span>
                    <span
                      class="flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10px] font-medium"
                      :class="[getColorBadge(section.colorCode).bg, getColorBadge(section.colorCode).text]"
                    >
                      <span class="h-1.5 w-1.5 rounded-full" :class="getColorBadge(section.colorCode).dot"></span>
                      {{ section.colorCode }}
                    </span>
                  </div>
                  <p class="mt-1 text-[11px] leading-relaxed text-secondary-500">{{ section.suggestedContentBrief }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 bg-secondary-50 px-5 py-4">
            <button
              type="button"
              class="btn btn-sm border border-secondary-200 bg-white text-secondary-600 hover:bg-secondary-50"
              @click="showPreview = false"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="btn-ai btn-sm"
              @click="applyStructure"
            >
              <i class="pi pi-check text-xs"></i>
              <span>{{ t('ai.applyStructure') }}</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
