<script setup lang="ts">
/**
 * Step 3: Booklet Content / Sections.
 *
 * Provides drag-and-drop section management with:
 * - Add/remove/reorder sections
 * - Rich text editing per section
 * - Color-coded template compliance
 * - Section assignment to team members
 * - AI-powered structure generation
 * - AI-powered section content generation and refinement
 */
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { contentSchema } from '@/validations/rfp'
import { useRfpStore } from '@/stores/rfp'
import draggable from 'vuedraggable'
import AiSectionAssistant from './AiSectionAssistant.vue'
import AiStructureGenerator from './AiStructureGenerator.vue'
import RichTextEditor from '@/components/common/RichTextEditor.vue'
import type { BookletSection } from '@/services/aiSpecificationService'

const { t } = useI18n()
const rfpStore = useRfpStore()

const schema = toTypedSchema(contentSchema)

const { errors, validate, setFieldValue } = useForm({
  validationSchema: schema,
  initialValues: { ...rfpStore.formData.content },
  validateOnMount: false,
})

const dragEnabled = ref(true)
const editingSectionId = ref<string | null>(null)

/** Default sections for a new booklet */
const defaultSections = [
  { titleKey: 'rfp.defaultSections.introduction', colorCode: 'black' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.scope', colorCode: 'green' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.technicalRequirements', colorCode: 'green' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.qualifications', colorCode: 'green' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.timeline', colorCode: 'green' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.termsAndConditions', colorCode: 'black' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.evaluationCriteria', colorCode: 'green' as const, isRequired: true },
  { titleKey: 'rfp.defaultSections.submissionInstructions', colorCode: 'black' as const, isRequired: true },
]

/** Add default sections if none exist */
function addDefaultSections() {
  defaultSections.forEach((section) => {
    rfpStore.addSection({
      title: t(section.titleKey),
      colorCode: section.colorCode,
      isRequired: section.isRequired,
    })
  })
}

/** Handle AI-generated structure */
function handleAiStructure(sections: BookletSection[]) {
  // Clear existing sections
  rfpStore.formData.content.sections = []
  // Add AI-generated sections
  sections.forEach((section) => {
    rfpStore.addSection({
      title: section.sectionTitleAr,
      colorCode: (section.colorCode || 'green') as import('@/types/rfp').TextColorCode,
      isRequired: section.isRequired,
      content: section.suggestedContentBrief || '',
    })
  })
}

/** Handle AI-generated content for a section */
function handleAiContent(sectionId: string, content: string, contentHtml?: string) {
  rfpStore.updateSection(sectionId, { content, ...(contentHtml ? { contentHtml } : {}) })
}

/** Add a new empty section */
function addNewSection() {
  rfpStore.addSection({
    title: '',
    colorCode: '',
    isRequired: false,
  })
}

function getSectionFieldError(index: number, field: 'title' | 'content') {
  const validationErrors = errors.value as Record<string, string | undefined>
  return validationErrors[`sections[${index}].${field}`] || validationErrors[`sections.${index}.${field}`] || ''
}

/** Handle drag end */
function onDragEnd() {
  rfpStore.reorderSections(rfpStore.formData.content.sections)
}

/** Color code label and style */
function getColorCodeInfo(code: string) {
  switch (code) {
    case 'black':
      return { label: t('rfp.colorCodes.mandatory'), bgClass: 'bg-secondary/10', textClass: 'text-secondary', dotClass: 'bg-secondary' }
    case 'green':
      return { label: t('rfp.colorCodes.editable'), bgClass: 'bg-success/10', textClass: 'text-success', dotClass: 'bg-success' }
    case 'red':
      return { label: t('rfp.colorCodes.example'), bgClass: 'bg-danger/10', textClass: 'text-danger', dotClass: 'bg-danger' }
    case 'blue':
      return { label: t('rfp.colorCodes.instruction'), bgClass: 'bg-info/10', textClass: 'text-info', dotClass: 'bg-info' }
    default:
      return { label: '', bgClass: 'bg-surface-muted', textClass: 'text-tertiary', dotClass: 'bg-tertiary' }
  }
}

/** Toggle section editing */
function toggleEdit(sectionId: string) {
  editingSectionId.value = editingSectionId.value === sectionId ? null : sectionId
}

defineExpose({
  validate: async () => {
    /**
     * CRITICAL FIX: Sync Pinia store sections into VeeValidate before
     * running validation. Sections are managed directly in the Pinia
     * store, so VeeValidate's internal state becomes stale.
     */
    const storeSections = rfpStore.formData.content.sections
    setFieldValue('sections', storeSections)

    const result = await validate()
    return result.valid
  },
})
</script>

<template>
  <div class="space-y-6">
    <div class="mb-6">
      <h2 class="text-xl font-bold text-secondary">
        {{ t('rfp.steps.content') }}
      </h2>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('rfp.steps.contentDesc') }}
      </p>
    </div>

    <!-- Color code legend -->
    <div class="mb-6 flex flex-wrap gap-4 rounded-lg border border-surface-dim bg-surface-muted p-4">
      <div
        v-for="code in ['black', 'green', 'red', 'blue']"
        :key="code"
        class="flex items-center gap-2"
      >
        <span
          class="h-3 w-3 rounded-full"
          :class="getColorCodeInfo(code).dotClass"
        ></span>
        <span class="text-xs text-secondary">{{ getColorCodeInfo(code).label }}</span>
      </div>
    </div>

    <!-- AI Structure Generator (shown when no sections exist) -->
    <div v-if="rfpStore.formData.content.sections.length === 0" class="mb-6">
      <AiStructureGenerator @structure-generated="handleAiStructure" />
    </div>

    <!-- Action bar -->
    <div class="flex flex-wrap items-center gap-3">
      <button
        type="button"
        class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
        @click="addNewSection"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ t('rfp.actions.addSection') }}
      </button>

      <button
        type="button"
        class="flex items-center gap-2 rounded-lg border border-primary bg-white px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary/5"
        @click="addDefaultSections"
      >
        <i class="pi pi-file text-xs"></i>
        {{ t('rfp.actions.addDefaultSections') }}
      </button>

      <!-- AI Regenerate Structure (shown when sections exist) -->
      <button
        v-if="rfpStore.formData.content.sections.length > 0"
        type="button"
        class="flex items-center gap-2 rounded-lg border border-ai-300 bg-ai-50 px-4 py-2 text-sm font-medium text-ai-600 transition-colors hover:bg-ai-100"
        @click="rfpStore.formData.content.sections = []; "
      >
        <i class="pi pi-sparkles text-xs"></i>
        {{ t('ai.regenerateStructure') }}
      </button>
    </div>

    <!-- Empty state -->
    <div
      v-if="rfpStore.formData.content.sections.length === 0"
      class="rounded-lg border-2 border-dashed border-surface-dim p-12 text-center"
    >
      <i class="pi pi-file-edit mb-3 text-4xl text-tertiary"></i>
      <p class="text-sm text-tertiary">{{ t('rfp.messages.noSections') }}</p>
      <p class="mt-1 text-xs text-tertiary">{{ t('rfp.messages.dragDropHint') }}</p>
    </div>

    <!-- Draggable sections list -->
    <draggable
      v-else
      v-model="rfpStore.formData.content.sections"
      item-key="id"
      handle=".drag-handle"
      ghost-class="opacity-30"
      animation="200"
      :disabled="!dragEnabled"
      @end="onDragEnd"
    >
      <template #item="{ element: section, index }">
        <div
          class="mb-3 rounded-lg border bg-white transition-shadow hover:shadow-md"
          :class="{
            'border-primary shadow-md': editingSectionId === section.id,
            'border-surface-dim': editingSectionId !== section.id,
          }"
        >
          <!-- Section header -->
          <div class="flex items-center gap-3 p-4">
            <!-- Drag handle -->
            <div
              class="drag-handle flex cursor-grab items-center text-tertiary transition-colors hover:text-secondary active:cursor-grabbing"
              :title="t('rfp.actions.dragToReorder')"
            >
              <i class="pi pi-bars text-lg"></i>
            </div>

            <!-- Order number -->
            <span class="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
              {{ index + 1 }}
            </span>

            <!-- Section title (inline edit) -->
            <div class="flex-1">
              <label class="mb-1 block px-2 text-xs font-medium text-tertiary">
                {{ t('rfp.fields.sectionName') }}
                <span class="text-danger">*</span>
              </label>
              <input
                :value="section.title"
                type="text"
                class="w-full rounded px-2 py-1 text-sm font-medium text-secondary focus:bg-surface-muted focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="getSectionFieldError(index, 'title') ? 'border border-danger bg-danger/5 focus:ring-danger/20' : 'border-0 bg-transparent'"
                :placeholder="t('rfp.placeholders.sectionTitle')"
                @input="rfpStore.updateSection(section.id, { title: ($event.target as HTMLInputElement).value })"
              />
              <p v-if="getSectionFieldError(index, 'title')" class="mt-1 px-2 text-xs text-danger">
                <i class="pi pi-exclamation-circle me-1"></i>
                {{ getSectionFieldError(index, 'title') }}
              </p>
            </div>

            <!-- Color code badge -->
            <span
              class="flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium"
              :class="[getColorCodeInfo(section.colorCode).bgClass, getColorCodeInfo(section.colorCode).textClass]"
            >
              <span class="h-2 w-2 rounded-full" :class="getColorCodeInfo(section.colorCode).dotClass"></span>
              {{ getColorCodeInfo(section.colorCode).label }}
            </span>

            <!-- Required badge -->
            <span
              v-if="section.isRequired"
              class="rounded-full bg-warning/10 px-2 py-0.5 text-xs font-medium text-warning"
            >
              {{ t('rfp.labels.required') }}
            </span>

            <!-- Actions -->
            <div class="flex items-center gap-1">
              <button
                type="button"
                class="flex h-8 w-8 items-center justify-center rounded-lg transition-colors hover:bg-surface-muted"
                :title="t('common.edit')"
                @click="toggleEdit(section.id)"
              >
                <i class="pi pi-pencil text-sm text-tertiary"></i>
              </button>
              <button
                v-if="!section.isRequired"
                type="button"
                class="flex h-8 w-8 items-center justify-center rounded-lg transition-colors hover:bg-danger/10"
                :title="t('common.delete')"
                @click="rfpStore.removeSection(section.id)"
              >
                <i class="pi pi-trash text-sm text-danger"></i>
              </button>
            </div>
          </div>

          <!-- Section content editor (expandable) -->
          <Transition name="slide">
            <div
              v-if="editingSectionId === section.id"
              class="border-t border-surface-dim p-4"
            >
              <!-- AI Section Assistant -->
              <div class="mb-4">
                <AiSectionAssistant
                  :section-id="section.id"
                  :section-title="section.title"
                  :section-type="section.colorCode === 'black' ? 'mandatory' : 'editable'"
                  :current-content="section.content || ''"
                  @content-generated="(content, html) => handleAiContent(section.id, content, html)"
                  @content-refined="(content, html) => handleAiContent(section.id, content, html)"
                />
              </div>

              <div class="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
                <!-- Color code selector -->
                <div>
                  <label class="mb-1 block text-xs font-medium text-tertiary">
                    {{ t('rfp.fields.colorCode') }}
                  </label>
                  <select
                    :value="section.colorCode"
                    class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none"
                    :disabled="section.colorCode === 'black'"
                    @change="rfpStore.updateSection(section.id, { colorCode: ($event.target as HTMLSelectElement).value as any })"
                  >
                    <option value="">{{ t('common.select') || 'اختر التصنيف' }}</option>
                    <option value="black">{{ t('rfp.colorCodes.mandatory') }}</option>
                    <option value="green">{{ t('rfp.colorCodes.editable') }}</option>
                    <option value="red">{{ t('rfp.colorCodes.example') }}</option>
                    <option value="blue">{{ t('rfp.colorCodes.instruction') }}</option>
                  </select>
                </div>

                <!-- Assigned to -->
                <div>
                  <label class="mb-1 block text-xs font-medium text-tertiary">
                    {{ t('rfp.fields.assignedTo') }}
                  </label>
                  <input
                    :value="section.assignedTo || ''"
                    type="text"
                    class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none"
                    :placeholder="t('rfp.placeholders.assignedTo')"
                    @input="rfpStore.updateSection(section.id, { assignedTo: ($event.target as HTMLInputElement).value || null })"
                  />
                </div>
              </div>

              <!-- Content editor -->
              <label class="mb-1 block text-xs font-medium text-tertiary">
                {{ t('rfp.fields.sectionContent') }}
                <span class="text-danger">*</span>
              </label>
              <div :class="getSectionFieldError(index, 'content') ? 'rounded-lg border border-danger p-1' : ''">
                <RichTextEditor
                  :model-value="section.content || ''"
                  :placeholder="t('rfp.placeholders.sectionContent')"
                  dir="rtl"
                  min-height="180px"
                  max-height="400px"
                  @update:model-value="(val: string) => rfpStore.updateSection(section.id, { content: val })"
                />
              </div>
              <p v-if="getSectionFieldError(index, 'content')" class="mt-2 text-xs text-danger">
                <i class="pi pi-exclamation-circle me-1"></i>
                {{ getSectionFieldError(index, 'content') }}
              </p>

              <!-- Mark as completed -->
              <div class="mt-3 flex items-center gap-2">
                <input
                  :id="`completed-${section.id}`"
                  :checked="section.isCompleted"
                  type="checkbox"
                  class="h-4 w-4 rounded border-surface-dim text-primary accent-primary"
                  @change="rfpStore.updateSection(section.id, { isCompleted: ($event.target as HTMLInputElement).checked })"
                />
                <label :for="`completed-${section.id}`" class="text-sm text-secondary">
                  {{ t('rfp.labels.markAsCompleted') }}
                </label>
              </div>
            </div>
          </Transition>
        </div>
      </template>
    </draggable>

    <!-- Validation error -->
    <p v-if="errors.sections && rfpStore.formData.content.sections.length === 0" class="mt-2 text-sm text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ errors.sections }}
    </p>
  </div>
</template>

<style scoped>
.slide-enter-active,
.slide-leave-active {
  transition: all 0.3s ease;
  overflow: hidden;
}

.slide-enter-from,
.slide-leave-to {
  max-height: 0;
  opacity: 0;
  padding-top: 0;
  padding-bottom: 0;
}

.slide-enter-to,
.slide-leave-from {
  max-height: 800px;
  opacity: 1;
}
</style>
