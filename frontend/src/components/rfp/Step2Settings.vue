<script setup lang="ts">
/**
 * Step 2: Competition Settings.
 *
 * Configures evaluation method, weights, criteria,
 * guarantee requirements, and inquiry period.
 *
 * FIX: Validation now syncs Pinia store criteria into VeeValidate
 * before running validation, ensuring criteria added/edited via
 * the store are properly validated.
 */
import { watch, computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { settingsSchema } from '@/validations/rfp'
import { useRfpStore } from '@/stores/rfp'
import FormField from './FormField.vue'
import AiCriteriaSuggester from './AiCriteriaSuggester.vue'

const { t } = useI18n()
const rfpStore = useRfpStore()

const schema = toTypedSchema(settingsSchema)

const { errors, validate, setFieldValue } = useForm({
  validationSchema: schema,
  initialValues: {
    ...rfpStore.formData.settings,
    evaluationCriteria: rfpStore.formData.settings.evaluationCriteria || [],
  },
  validateOnMount: false,
})

const { value: evaluationMethod } = useField<string>('evaluationMethod')
const { value: technicalWeight } = useField<number>('technicalWeight')
const { value: financialWeight } = useField<number>('financialWeight')
const { value: minimumTechnicalScore } = useField<number>('minimumTechnicalScore')
const { value: allowPartialOffers } = useField<boolean>('allowPartialOffers')
const { value: requireBankGuarantee } = useField<boolean>('requireBankGuarantee')
const { value: guaranteePercentage } = useField<number>('guaranteePercentage')

const evaluationMethods = computed(() => [
  { value: 'lowest_price', label: t('rfp.evaluationMethods.lowestPrice') },
  { value: 'weighted_criteria', label: t('rfp.evaluationMethods.weightedCriteria') },
  { value: 'quality_cost_based', label: t('rfp.evaluationMethods.qualityCostBased') },
])

const criteriaWeightError = ref('')
const evaluationCriteriaErrorMessage = computed(
  () => errors.value.evaluationCriteria || criteriaWeightError.value || '',
)

function getMaxAllowedCriterionWeight(criterionId: string): number {
  const criteria = rfpStore.formData.settings.evaluationCriteria
  const totalWithoutCurrent = criteria.reduce((sum, criterion) => {
    return criterion.id === criterionId ? sum : sum + criterion.weight
  }, 0)

  return Math.max(0, 100 - totalWithoutCurrent)
}

/** Auto-adjust financial weight when technical weight changes */
watch(technicalWeight, (val) => {
  if (val !== undefined && val >= 0 && val <= 100) {
    financialWeight.value = 100 - val
  }
})

/** Sync to store */
watch(
  [
    evaluationMethod, technicalWeight, financialWeight,
    minimumTechnicalScore, allowPartialOffers, requireBankGuarantee,
    guaranteePercentage,
  ],
  () => {
    rfpStore.updateSettings({
      evaluationMethod: evaluationMethod.value as any,
      technicalWeight: technicalWeight.value,
      financialWeight: financialWeight.value,
      minimumTechnicalScore: minimumTechnicalScore.value,
      allowPartialOffers: allowPartialOffers.value,
      requireBankGuarantee: requireBankGuarantee.value,
      guaranteePercentage: guaranteePercentage.value,
    })
  },
  { deep: true },
)

/** Criteria management */
function addCriterion() {
  rfpStore.addCriterion({ name: '', weight: 0, description: '' })
}

function removeCriterion(id: string) {
  rfpStore.removeCriterion(id)
  if (rfpStore.criteriaWeightTotal <= 100) {
    criteriaWeightError.value = ''
  }
}

function updateCriterionField(id: string, field: string, value: string | number) {
  if (field === 'weight') {
    const numVal = Number(value)
    const normalizedValue = Number.isFinite(numVal) ? Math.max(0, numVal) : 0
    const maxAllowed = getMaxAllowedCriterionWeight(id)

    if (normalizedValue > maxAllowed) {
      criteriaWeightError.value = t('rfp.validation.criteriaWeightTotalExceeded', { maxAllowed })
      value = maxAllowed
    } else {
      criteriaWeightError.value = ''
      value = normalizedValue
    }
  }

  rfpStore.updateCriterion(id, { [field]: value })
}

/** Handle AI-suggested criteria */
function handleAiCriteria(criteria: Array<{ name: string; weight: number; description: string }>) {
  criteriaWeightError.value = ''

  // Clear existing criteria and add AI-suggested ones
  rfpStore.formData.settings.evaluationCriteria.forEach(c => rfpStore.removeCriterion(c.id))
  criteria.forEach(c => {
    rfpStore.addCriterion({ name: c.name, weight: c.weight, description: c.description })
  })
}

defineExpose({
  validate: async () => {
    /**
     * CRITICAL FIX: Sync the Pinia store criteria into VeeValidate
     * before running validation. Criteria are managed directly in
     * the Pinia store (addCriterion, removeCriterion, updateCriterion),
     * so VeeValidate's internal state becomes stale. We must update
     * VeeValidate's evaluationCriteria field with the current store data.
     */
    const storeCriteria = rfpStore.formData.settings.evaluationCriteria
    setFieldValue('evaluationCriteria', storeCriteria)

    const result = await validate()
    return result.valid
  },
})
</script>

<template>
  <div class="space-y-6">
    <div class="mb-6">
      <h2 class="text-xl font-bold text-secondary">
        {{ t('rfp.steps.settings') }}
      </h2>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('rfp.steps.settingsDesc') }}
      </p>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <!-- Evaluation Method -->
      <div class="lg:col-span-2">
        <FormField
          :label="t('rfp.fields.evaluationMethod')"
          field-id="evaluationMethod"
          :error="errors.evaluationMethod"
          :required="true"
        >
          <select
            id="evaluationMethod"
            v-model="evaluationMethod"
            class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            :class="errors.evaluationMethod ? 'border-danger' : 'border-surface-dim'"
          >
            <option value="" disabled>{{ t('rfp.placeholders.selectEvaluationMethod') }}</option>
            <option
              v-for="method in evaluationMethods"
              :key="method.value"
              :value="method.value"
            >
              {{ method.label }}
            </option>
          </select>
        </FormField>
      </div>

      <!-- Technical Weight -->
      <FormField
        :label="t('rfp.fields.technicalWeight')"
        field-id="technicalWeight"
        :error="errors.technicalWeight"
        :required="true"
        :help-text="t('rfp.hints.weightSum')"
      >
        <div class="flex items-center gap-3">
          <input
            id="technicalWeight"
            v-model.number="technicalWeight"
            type="range"
            min="0"
            max="100"
            step="5"
            class="h-2 flex-1 cursor-pointer appearance-none rounded-lg bg-surface-dim accent-primary"
          />
          <span class="w-12 text-center text-sm font-bold text-primary">
            {{ technicalWeight }}%
          </span>
        </div>
      </FormField>

      <!-- Financial Weight -->
      <FormField
        :label="t('rfp.fields.financialWeight')"
        field-id="financialWeight"
        :error="errors.financialWeight"
        :required="true"
      >
        <div class="flex items-center gap-3">
          <input
            id="financialWeight"
            v-model.number="financialWeight"
            type="range"
            min="0"
            max="100"
            step="5"
            class="h-2 flex-1 cursor-pointer appearance-none rounded-lg bg-surface-dim accent-primary"
            disabled
          />
          <span class="w-12 text-center text-sm font-bold text-info">
            {{ financialWeight }}%
          </span>
        </div>
      </FormField>

      <!-- Minimum Technical Score -->
      <FormField
        :label="t('rfp.fields.minimumTechnicalScore')"
        field-id="minimumTechnicalScore"
        :error="errors.minimumTechnicalScore"
        :required="true"
      >
        <input
          id="minimumTechnicalScore"
          v-model.number="minimumTechnicalScore"
          type="number"
          min="0"
          max="100"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.minimumTechnicalScore ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <!-- Allow Partial Offers -->
      <div class="flex items-center gap-3 rounded-lg border border-surface-dim p-4">
        <input
          id="allowPartialOffers"
          v-model="allowPartialOffers"
          type="checkbox"
          class="h-5 w-5 rounded border-surface-dim text-primary accent-primary focus:ring-primary"
        />
        <label for="allowPartialOffers" class="text-sm text-secondary">
          {{ t('rfp.fields.allowPartialOffers') }}
        </label>
      </div>

      <!-- Require Bank Guarantee -->
      <div class="flex items-center gap-3 rounded-lg border border-surface-dim p-4">
        <input
          id="requireBankGuarantee"
          v-model="requireBankGuarantee"
          type="checkbox"
          class="h-5 w-5 rounded border-surface-dim text-primary accent-primary focus:ring-primary"
        />
        <label for="requireBankGuarantee" class="text-sm text-secondary">
          {{ t('rfp.fields.requireBankGuarantee') }}
        </label>
      </div>

      <!-- Guarantee Percentage (conditional) -->
      <FormField
        v-if="requireBankGuarantee"
        :label="t('rfp.fields.guaranteePercentage')"
        field-id="guaranteePercentage"
        :error="errors.guaranteePercentage"
      >
        <input
          id="guaranteePercentage"
          v-model.number="guaranteePercentage"
          type="number"
          min="0"
          max="100"
          step="0.5"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.guaranteePercentage ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>
    </div>

    <!-- Evaluation Criteria -->
    <div class="mt-8">
      <div class="mb-4 flex items-center justify-between">
        <h3 class="text-lg font-bold text-secondary">
          {{ t('rfp.fields.evaluationCriteria') }}
          <span class="text-danger">*</span>
        </h3>
        <div class="flex items-center gap-3">
          <button
            type="button"
            class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
            @click="addCriterion"
          >
            <i class="pi pi-plus text-xs"></i>
            {{ t('rfp.actions.addCriterion') }}
          </button>
          <AiCriteriaSuggester @criteria-suggested="handleAiCriteria" />
        </div>
      </div>

      <div
        v-if="rfpStore.formData.settings.evaluationCriteria.length === 0"
        class="rounded-lg border-2 border-dashed p-8 text-center"
        :class="evaluationCriteriaErrorMessage ? 'border-danger bg-danger/5' : 'border-surface-dim'"
      >
        <i class="pi pi-list mb-2 text-3xl text-tertiary"></i>
        <p class="text-sm text-tertiary">{{ t('rfp.messages.noCriteria') }}</p>
        <p v-if="evaluationCriteriaErrorMessage" class="mt-3 text-sm font-medium text-danger">
          <i class="pi pi-exclamation-circle me-1"></i>
          {{ evaluationCriteriaErrorMessage }}
        </p>
      </div>

      <div v-else class="space-y-3">
        <div
          v-for="criterion in rfpStore.formData.settings.evaluationCriteria"
          :key="criterion.id"
          class="flex items-start gap-3 rounded-lg border border-surface-dim bg-white p-4"
        >
          <div class="flex-1 grid grid-cols-1 gap-3 sm:grid-cols-3">
            <input
              :value="criterion.name"
              type="text"
              :placeholder="t('rfp.placeholders.criterionName')"
              class="rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              @input="updateCriterionField(criterion.id, 'name', ($event.target as HTMLInputElement).value)"
            />
            <input
              :value="criterion.weight"
              type="number"
              min="0"
              :max="getMaxAllowedCriterionWeight(criterion.id)"
              placeholder="%"
              class="rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              @input="updateCriterionField(criterion.id, 'weight', Number(($event.target as HTMLInputElement).value))"
            />
            <input
              :value="criterion.description"
              type="text"
              :placeholder="t('rfp.placeholders.criterionDescription')"
              :title="criterion.description"
              class="rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              @input="updateCriterionField(criterion.id, 'description', ($event.target as HTMLInputElement).value)"
            />
          </div>
          <button
            type="button"
            class="mt-1 flex h-8 w-8 items-center justify-center rounded-lg text-danger transition-colors hover:bg-danger/10"
            :aria-label="t('common.delete')"
            @click="removeCriterion(criterion.id)"
          >
            <i class="pi pi-trash text-sm"></i>
          </button>
        </div>

        <!-- Weight total indicator -->
        <div
          class="flex items-center justify-end gap-2 text-sm"
          :class="rfpStore.criteriaWeightTotal === 100 ? 'text-success' : 'text-warning'"
        >
          <i :class="rfpStore.criteriaWeightTotal === 100 ? 'pi pi-check-circle' : 'pi pi-exclamation-circle'"></i>
          <span>
            {{ t('rfp.messages.criteriaWeightTotal') }}: {{ rfpStore.criteriaWeightTotal }}%
          </span>
        </div>

        <div
          v-if="evaluationCriteriaErrorMessage"
          class="rounded-lg border border-danger/20 bg-danger/5 p-3 text-sm text-danger"
        >
          <i class="pi pi-exclamation-circle me-1"></i>
          {{ evaluationCriteriaErrorMessage }}
        </div>
      </div>
    </div>
  </div>
</template>
