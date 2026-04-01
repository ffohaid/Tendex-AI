<script setup lang="ts">
/**
 * Step 6: Review & Submit.
 *
 * Displays a comprehensive summary of all entered data
 * before final submission for approval.
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRfpStore } from '@/stores/rfp'
import { formatCurrency } from '@/utils/numbers'

const { t } = useI18n()
const rfpStore = useRfpStore()

const emit = defineEmits<{
  (e: 'submit'): void
  (e: 'edit-step', step: number): void
}>()

/** Competition type label */
const competitionTypeLabel = computed(() => {
  const map: Record<string, string> = {
    public_tender: t('rfp.competitionTypes.publicTender'),
    limited_tender: t('rfp.competitionTypes.limitedTender'),
    direct_purchase: t('rfp.competitionTypes.directPurchase'),
    framework_agreement: t('rfp.competitionTypes.frameworkAgreement'),
    reverse_auction: t('rfp.competitionTypes.reverseAuction'),
  }
  return map[rfpStore.formData.basicInfo.competitionType] || '-'
})

/** Evaluation method label */
const evaluationMethodLabel = computed(() => {
  const map: Record<string, string> = {
    lowest_price: t('rfp.evaluationMethods.lowestPrice'),
    weighted_criteria: t('rfp.evaluationMethods.weightedCriteria'),
    quality_cost_based: t('rfp.evaluationMethods.qualityCostBased'),
  }
  return map[rfpStore.formData.settings.evaluationMethod] || '-'
})

/** Completed sections count */
const completedSections = computed(
  () => rfpStore.formData.content.sections.filter((s) => s.isCompleted || (s.content && s.content.trim().length > 0)).length,
)

/** BOQ subtotal */
const boqSubtotal = computed(() =>
  rfpStore.formData.boq.items.reduce(
    (sum, item) => sum + item.quantity * item.estimatedPrice,
    0,
  ),
)

/** Completeness checks */
const checks = computed(() => [
  {
    label: t('rfp.review.basicInfoComplete'),
    passed: !!(
      rfpStore.formData.basicInfo.projectName &&
      rfpStore.formData.basicInfo.competitionType &&
      rfpStore.formData.basicInfo.estimatedValue &&
      rfpStore.formData.basicInfo.startDate &&
      rfpStore.formData.basicInfo.endDate
    ),
    step: 1,
  },
  {
    label: t('rfp.review.settingsComplete'),
    passed: !!(rfpStore.formData.settings.evaluationMethod),
    step: 2,
  },
  {
    label: t('rfp.review.contentComplete'),
    passed: rfpStore.formData.content.sections.length > 0,
    step: 3,
  },
  {
    label: t('rfp.review.boqComplete'),
    passed: true, // BOQ is optional — can be added later
    step: 4,
  },
  {
    label: t('rfp.review.attachmentsComplete'),
    passed: true, // File attachments are optional — required attachment types are selected via checkboxes
    step: 5,
  },
])

const allChecksPassed = computed(() => checks.value.every((c) => c.passed))

function handleSubmit() {
  if (allChecksPassed.value) {
    emit('submit')
  }
}
</script>

<template>
  <div class="space-y-6">
    <div class="mb-6">
      <h2 class="text-xl font-bold text-secondary">
        {{ t('rfp.steps.review') }}
      </h2>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('rfp.steps.reviewDesc') }}
      </p>
    </div>

    <!-- Completeness checklist -->
    <div class="rounded-lg border border-surface-dim p-4">
      <h3 class="mb-3 text-sm font-bold text-secondary">
        {{ t('rfp.review.completenessCheck') }}
      </h3>
      <div class="space-y-2">
        <div
          v-for="check in checks"
          :key="check.label"
          class="flex items-center justify-between"
        >
          <div class="flex items-center gap-2">
            <i
              :class="check.passed
                ? 'pi pi-check-circle text-success'
                : 'pi pi-times-circle text-danger'"
            ></i>
            <span class="text-sm" :class="check.passed ? 'text-secondary' : 'text-danger'">
              {{ check.label }}
            </span>
          </div>
          <button
            v-if="!check.passed"
            type="button"
            class="text-xs text-primary hover:underline"
            @click="emit('edit-step', check.step)"
          >
            {{ t('rfp.actions.goToStep') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Basic Info Summary -->
    <div class="rounded-lg border border-surface-dim">
      <div class="flex items-center justify-between border-b border-surface-dim bg-surface-muted px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">{{ t('rfp.steps.basicInfo') }}</h3>
        <button
          type="button"
          class="text-xs text-primary hover:underline"
          @click="emit('edit-step', 1)"
        >
          <i class="pi pi-pencil me-1 text-xs"></i>{{ t('common.edit') }}
        </button>
      </div>
      <div class="grid grid-cols-1 gap-4 p-4 sm:grid-cols-2">
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.projectName') }}</span>
          <p class="text-sm font-medium text-secondary">{{ rfpStore.formData.basicInfo.projectName || '-' }}</p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.competitionType') }}</span>
          <p class="text-sm font-medium text-secondary">{{ competitionTypeLabel }}</p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.estimatedValue') }}</span>
          <p class="text-sm font-medium text-secondary">
            {{ rfpStore.formData.basicInfo.estimatedValue ? formatCurrency(rfpStore.formData.basicInfo.estimatedValue) : '-' }}
          </p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.referenceNumber') }}</span>
          <p class="text-sm font-medium text-secondary">{{ rfpStore.formData.basicInfo.referenceNumber || '-' }}</p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.startDate') }}</span>
          <p class="text-sm font-medium text-secondary">{{ rfpStore.formData.basicInfo.startDate || '-' }}</p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.endDate') }}</span>
          <p class="text-sm font-medium text-secondary">{{ rfpStore.formData.basicInfo.endDate || '-' }}</p>
        </div>
      </div>
    </div>

    <!-- Settings Summary -->
    <div class="rounded-lg border border-surface-dim">
      <div class="flex items-center justify-between border-b border-surface-dim bg-surface-muted px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">{{ t('rfp.steps.settings') }}</h3>
        <button
          type="button"
          class="text-xs text-primary hover:underline"
          @click="emit('edit-step', 2)"
        >
          <i class="pi pi-pencil me-1 text-xs"></i>{{ t('common.edit') }}
        </button>
      </div>
      <div class="grid grid-cols-1 gap-4 p-4 sm:grid-cols-2">
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.evaluationMethod') }}</span>
          <p class="text-sm font-medium text-secondary">{{ evaluationMethodLabel }}</p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.technicalWeight') }} / {{ t('rfp.fields.financialWeight') }}</span>
          <p class="text-sm font-medium text-secondary">
            {{ rfpStore.formData.settings.technicalWeight }}% / {{ rfpStore.formData.settings.financialWeight }}%
          </p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.evaluationCriteria') }}</span>
          <p class="text-sm font-medium text-secondary">
            {{ rfpStore.formData.settings.evaluationCriteria.length }} {{ t('rfp.labels.criteria') }}
          </p>
        </div>
        <div>
          <span class="text-xs text-tertiary">{{ t('rfp.fields.inquiryPeriodDays') }}</span>
          <p class="text-sm font-medium text-secondary">
            {{ rfpStore.formData.settings.inquiryPeriodDays }} {{ t('rfp.labels.days') }}
          </p>
        </div>
      </div>
    </div>

    <!-- Content Summary -->
    <div class="rounded-lg border border-surface-dim">
      <div class="flex items-center justify-between border-b border-surface-dim bg-surface-muted px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">{{ t('rfp.steps.content') }}</h3>
        <button
          type="button"
          class="text-xs text-primary hover:underline"
          @click="emit('edit-step', 3)"
        >
          <i class="pi pi-pencil me-1 text-xs"></i>{{ t('common.edit') }}
        </button>
      </div>
      <div class="p-4">
        <p class="text-sm text-secondary">
          {{ rfpStore.formData.content.sections.length }} {{ t('rfp.labels.sections') }}
          ({{ completedSections }} {{ t('rfp.labels.completed') }})
        </p>
        <div class="mt-2 space-y-1">
          <div
            v-for="section in rfpStore.formData.content.sections"
            :key="section.id"
            class="flex items-center gap-2 text-sm"
          >
            <i
              :class="(section.isCompleted || (section.content && section.content.trim().length > 0)) ? 'pi pi-check-circle text-success' : 'pi pi-circle text-tertiary'"
              class="text-xs"
            ></i>
            <span :class="(section.isCompleted || (section.content && section.content.trim().length > 0)) ? 'text-secondary' : 'text-tertiary'">
              {{ section.title || t('rfp.labels.untitledSection') }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- BOQ Summary -->
    <div class="rounded-lg border border-surface-dim">
      <div class="flex items-center justify-between border-b border-surface-dim bg-surface-muted px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">{{ t('rfp.steps.boq') }}</h3>
        <button
          type="button"
          class="text-xs text-primary hover:underline"
          @click="emit('edit-step', 4)"
        >
          <i class="pi pi-pencil me-1 text-xs"></i>{{ t('common.edit') }}
        </button>
      </div>
      <div class="p-4">
        <p class="text-sm text-secondary">
          {{ rfpStore.formData.boq.items.length }} {{ t('rfp.labels.items') }}
        </p>
        <p class="mt-1 text-lg font-bold text-primary">
          {{ formatCurrency(boqSubtotal) }}
        </p>
      </div>
    </div>

    <!-- Attachments Summary -->
    <div class="rounded-lg border border-surface-dim">
      <div class="flex items-center justify-between border-b border-surface-dim bg-surface-muted px-4 py-3">
        <h3 class="text-sm font-bold text-secondary">{{ t('rfp.steps.attachments') }}</h3>
        <button
          type="button"
          class="text-xs text-primary hover:underline"
          @click="emit('edit-step', 5)"
        >
          <i class="pi pi-pencil me-1 text-xs"></i>{{ t('common.edit') }}
        </button>
      </div>
      <div class="p-4">
        <p class="text-sm text-secondary">
          {{ rfpStore.formData.attachments.files.length }} {{ t('rfp.labels.filesUploaded') }}
        </p>
      </div>
    </div>

    <!-- Submit button -->
    <div class="flex justify-center pt-4">
      <button
        type="button"
        class="flex items-center gap-2 rounded-lg px-8 py-3 text-sm font-bold text-white transition-all"
        :class="allChecksPassed
          ? 'bg-primary hover:bg-primary-dark shadow-lg hover:shadow-xl'
          : 'cursor-not-allowed bg-surface-dim text-tertiary'"
        :disabled="!allChecksPassed"
        @click="handleSubmit"
      >
        <i class="pi pi-send"></i>
        {{ t('rfp.actions.submitForApproval') }}
      </button>
    </div>

    <p v-if="!allChecksPassed" class="text-center text-sm text-warning">
      <i class="pi pi-exclamation-triangle me-1"></i>
      {{ t('rfp.messages.completeAllSteps') }}
    </p>
  </div>
</template>
