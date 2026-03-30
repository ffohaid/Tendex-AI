<script setup lang="ts">
/**
 * AiStructureGenerator Component
 *
 * Generates a complete RFP booklet structure using AI.
 * Analyzes project type, description, and budget to suggest
 * the optimal set of sections with proper ordering.
 *
 * Maps backend ProposedSection → frontend BookletSection for the parent.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useRfpStore } from '@/stores/rfp'
import {
  generateBookletStructure,
  type GenerateBookletStructureResult,
  type ProposedSection,
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

/** Convenience accessor: backend nests sections inside `structure` */
const proposedSections = computed<ProposedSection[]>(() => {
  return result.value?.structure?.sections ?? []
})

const structureSummary = computed<string>(() => {
  return result.value?.structure?.structureSummaryAr ?? ''
})

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

    if (response.isSuccess && response.structure?.sections?.length) {
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

/**
 * Map backend ProposedSection → frontend BookletSection and emit.
 * Determines colorCode from sectionType:
 *   mandatory → black (fixed text), editable → green, example → red, instructions → blue
 */
function mapSectionColor(section: ProposedSection): string {
  if (section.isMandatory) return 'black'
  const type = section.sectionType?.toLowerCase() ?? ''
  if (type.includes('example') || type.includes('template')) return 'red'
  if (type.includes('instruction') || type.includes('guide')) return 'blue'
  return 'green'
}

function applyStructure() {
  if (!proposedSections.value.length) return

  const mapped: BookletSection[] = proposedSections.value.map((s, idx) => ({
    sectionTitleAr: s.titleAr,
    sectionType: s.sectionType,
    colorCode: mapSectionColor(s),
    isRequired: s.isMandatory,
    suggestedContentBrief: s.descriptionAr,
    orderIndex: s.sortOrder ?? idx + 1,
  }))

  emit('structure-generated', mapped)
  showPreview.value = false
}

function getColorBadge(colorCode: string) {
  switch (colorCode) {
    case 'black':
      return { bg: 'bg-secondary-100', text: 'text-secondary-700', dot: 'bg-secondary-500', label: 'نص ثابت' }
    case 'green':
      return { bg: 'bg-success/10', text: 'text-success', dot: 'bg-success', label: 'قابل للتعديل' }
    case 'red':
      return { bg: 'bg-danger/10', text: 'text-danger', dot: 'bg-danger', label: 'مثال' }
    case 'blue':
      return { bg: 'bg-info/10', text: 'text-info', dot: 'bg-info', label: 'تعليمات' }
    default:
      return { bg: 'bg-secondary-50', text: 'text-secondary-500', dot: 'bg-secondary-400', label: colorCode }
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
            <!-- Structure Summary -->
            <div v-if="structureSummary" class="mb-4 rounded-lg bg-ai-50 p-3">
              <p class="text-xs leading-relaxed text-ai-700">
                <i class="pi pi-info-circle me-1"></i>
                {{ structureSummary }}
              </p>
            </div>

            <!-- Sections -->
            <div class="space-y-2">
              <div
                v-for="(section, idx) in proposedSections"
                :key="idx"
                class="flex items-start gap-3 rounded-lg border border-secondary-100 bg-white p-3 transition-colors hover:bg-secondary-50"
              >
                <span class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                  {{ idx + 1 }}
                </span>
                <div class="flex-1">
                  <div class="flex items-center gap-2">
                    <span class="text-sm font-semibold text-secondary-800">{{ section.titleAr }}</span>
                    <span
                      v-if="section.isMandatory"
                      class="rounded-full bg-warning/10 px-1.5 py-0.5 text-[10px] font-medium text-warning"
                    >
                      {{ t('rfp.labels.required') }}
                    </span>
                    <span
                      class="flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10px] font-medium"
                      :class="[getColorBadge(mapSectionColor(section)).bg, getColorBadge(mapSectionColor(section)).text]"
                    >
                      <span class="h-1.5 w-1.5 rounded-full" :class="getColorBadge(mapSectionColor(section)).dot"></span>
                      {{ getColorBadge(mapSectionColor(section)).label }}
                    </span>
                  </div>
                  <p class="mt-1 text-[11px] leading-relaxed text-secondary-500">{{ section.descriptionAr }}</p>
                </div>
              </div>
            </div>

            <!-- Empty state -->
            <div v-if="!proposedSections.length" class="py-8 text-center text-sm text-secondary-400">
              <i class="pi pi-inbox mb-2 text-2xl"></i>
              <p>{{ t('ai.errors.structureGenerationFailed') }}</p>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 bg-secondary-50 px-5 py-4">
            <button
              type="button"
              class="rounded-lg border border-secondary-200 bg-white px-4 py-2 text-sm text-secondary-600 hover:bg-secondary-50"
              @click="showPreview = false"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="btn-ai btn-sm"
              :disabled="!proposedSections.length"
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
