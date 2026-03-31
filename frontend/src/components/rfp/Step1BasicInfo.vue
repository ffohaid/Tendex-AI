<script setup lang="ts">
/**
 * Step 1: Basic Information.
 *
 * Collects project name, description, competition type,
 * estimated value, dates, reference number, and department.
 * Uses VeeValidate + Zod for real-time validation.
 */
import { watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { basicInfoSchema } from '@/validations/rfp'
import { useRfpStore } from '@/stores/rfp'
import FormField from './FormField.vue'
import AiTextHelper from '@/components/ai/AiTextHelper.vue'
import { formatCurrency } from '@/utils/numbers'

const { t, locale } = useI18n()
const rfpStore = useRfpStore()

const schema = toTypedSchema(basicInfoSchema)

const { errors, validate } = useForm({
  validationSchema: schema,
  initialValues: { ...rfpStore.formData.basicInfo },
  validateOnMount: false,
})

/* Individual fields with real-time validation */
const { value: projectName } = useField<string>('projectName')
const { value: projectDescription } = useField<string>('projectDescription')
const { value: competitionType } = useField<string>('competitionType')
const { value: estimatedValue } = useField<number | null>('estimatedValue')
const { value: startDate } = useField<string>('startDate')
const { value: endDate } = useField<string>('endDate')
const { value: submissionDeadline } = useField<string>('submissionDeadline')
const { value: referenceNumber } = useField<string>('referenceNumber')
const { value: department } = useField<string>('department')
const { value: fiscalYear } = useField<string>('fiscalYear')

/** Competition type options */
const competitionTypes = computed(() => [
  { value: 'public_tender', label: t('rfp.competitionTypes.publicTender') },
  { value: 'limited_tender', label: t('rfp.competitionTypes.limitedTender') },
  { value: 'direct_purchase', label: t('rfp.competitionTypes.directPurchase') },
  { value: 'framework_agreement', label: t('rfp.competitionTypes.frameworkAgreement') },
  { value: 'reverse_auction', label: t('rfp.competitionTypes.reverseAuction') },
])

/** Fiscal year options */
const fiscalYears = computed(() => {
  const currentYear = new Date().getFullYear()
  return [
    { value: String(currentYear - 1), label: String(currentYear - 1) },
    { value: String(currentYear), label: String(currentYear) },
    { value: String(currentYear + 1), label: String(currentYear + 1) },
  ]
})

/** Sync field changes back to store */
watch(
  [
    projectName, projectDescription, competitionType, estimatedValue,
    startDate, endDate, submissionDeadline, referenceNumber,
    department, fiscalYear,
  ],
  () => {
    rfpStore.updateBasicInfo({
      projectName: projectName.value,
      projectDescription: projectDescription.value,
      competitionType: competitionType.value as any,
      estimatedValue: estimatedValue.value,
      startDate: startDate.value,
      endDate: endDate.value,
      submissionDeadline: submissionDeadline.value,
      referenceNumber: referenceNumber.value,
      department: department.value,
      fiscalYear: fiscalYear.value,
    })
  },
  { deep: true },
)

/** Expose validate for parent wizard */
defineExpose({
  validate: async () => {
    const result = await validate()
    return result.valid
  },
})
</script>

<template>
  <div class="space-y-6">
    <div class="mb-6">
      <h2 class="text-xl font-bold text-secondary">
        {{ t('rfp.steps.basicInfo') }}
      </h2>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('rfp.steps.basicInfoDesc') }}
      </p>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <!-- Project Name -->
      <div class="lg:col-span-2">
        <FormField
          :label="t('rfp.fields.projectName')"
          field-id="projectName"
          :error="errors.projectName"
          :required="true"
        >
          <input
            id="projectName"
            v-model="projectName"
            type="text"
            class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            :class="errors.projectName ? 'border-danger' : 'border-surface-dim'"
            :placeholder="t('rfp.placeholders.projectName')"
            :aria-invalid="!!errors.projectName"
            :aria-describedby="errors.projectName ? 'projectName-error' : undefined"
          />
        </FormField>
      </div>

      <!-- Project Description -->
      <div class="lg:col-span-2">
        <FormField
          :label="t('rfp.fields.projectDescription')"
          field-id="projectDescription"
          :error="errors.projectDescription"
          :required="true"
        >
          <textarea
            id="projectDescription"
            v-model="projectDescription"
            rows="4"
            class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            :class="errors.projectDescription ? 'border-danger' : 'border-surface-dim'"
            :placeholder="t('rfp.placeholders.projectDescription')"
            :aria-invalid="!!errors.projectDescription"
          ></textarea>
          <div class="mt-2">
            <AiTextHelper
              :context="{
                fieldName: locale === 'ar' ? 'وصف المشروع' : 'Project Description',
                fieldPurpose: locale === 'ar' ? 'وصف تفصيلي للمشروع المطلوب تنفيذه في كراسة الشروط والمواصفات' : 'Detailed description of the project for the RFP',
                projectName: projectName || '',
                competitionType: competitionType || '',
              }"
              :current-text="projectDescription || ''"
              @text-generated="(text) => { projectDescription = text }"
            />
          </div>
        </FormField>
      </div>

      <!-- Competition Type -->
      <FormField
        :label="t('rfp.fields.competitionType')"
        field-id="competitionType"
        :error="errors.competitionType"
        :required="true"
      >
        <select
          id="competitionType"
          v-model="competitionType"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.competitionType ? 'border-danger' : 'border-surface-dim'"
        >
          <option value="" disabled>{{ t('rfp.placeholders.selectCompetitionType') }}</option>
          <option
            v-for="option in competitionTypes"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </option>
        </select>
      </FormField>

      <!-- Estimated Value -->
      <FormField
        :label="t('rfp.fields.estimatedValue')"
        field-id="estimatedValue"
        :error="errors.estimatedValue"
        :required="true"
        :help-text="estimatedValue ? formatCurrency(Number(estimatedValue)) : ''"
      >
        <div class="relative">
          <input
            id="estimatedValue"
            v-model.number="estimatedValue"
            type="number"
            min="0"
            step="0.01"
            class="w-full rounded-lg border pe-16 ps-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            :class="errors.estimatedValue ? 'border-danger' : 'border-surface-dim'"
            :placeholder="t('rfp.placeholders.estimatedValue')"
          />
          <span class="pointer-events-none absolute inset-y-0 end-0 flex items-center pe-4 text-sm text-tertiary">
            &#xFDFC;
          </span>
        </div>
      </FormField>

      <!-- Reference Number -->
      <FormField
        :label="t('rfp.fields.referenceNumber')"
        field-id="referenceNumber"
        :error="errors.referenceNumber"
        :required="true"
      >
        <input
          id="referenceNumber"
          v-model="referenceNumber"
          type="text"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.referenceNumber ? 'border-danger' : 'border-surface-dim'"
          :placeholder="t('rfp.placeholders.referenceNumber')"
        />
      </FormField>

      <!-- Department -->
      <FormField
        :label="t('rfp.fields.department')"
        field-id="department"
        :error="errors.department"
        :required="true"
      >
        <input
          id="department"
          v-model="department"
          type="text"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.department ? 'border-danger' : 'border-surface-dim'"
          :placeholder="t('rfp.placeholders.department')"
        />
      </FormField>

      <!-- Fiscal Year -->
      <FormField
        :label="t('rfp.fields.fiscalYear')"
        field-id="fiscalYear"
        :error="errors.fiscalYear"
        :required="true"
      >
        <select
          id="fiscalYear"
          v-model="fiscalYear"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.fiscalYear ? 'border-danger' : 'border-surface-dim'"
        >
          <option value="" disabled>{{ t('rfp.placeholders.selectFiscalYear') }}</option>
          <option
            v-for="year in fiscalYears"
            :key="year.value"
            :value="year.value"
          >
            {{ year.label }}
          </option>
        </select>
      </FormField>

      <!-- Start Date -->
      <FormField
        :label="t('rfp.fields.startDate')"
        field-id="startDate"
        :error="errors.startDate"
        :required="true"
      >
        <input
          id="startDate"
          v-model="startDate"
          type="date"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.startDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <!-- End Date -->
      <FormField
        :label="t('rfp.fields.endDate')"
        field-id="endDate"
        :error="errors.endDate"
        :required="true"
      >
        <input
          id="endDate"
          v-model="endDate"
          type="date"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.endDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <!-- Submission Deadline -->
      <FormField
        :label="t('rfp.fields.submissionDeadline')"
        field-id="submissionDeadline"
        :error="errors.submissionDeadline"
        :required="true"
      >
        <input
          id="submissionDeadline"
          v-model="submissionDeadline"
          type="date"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.submissionDeadline ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>
    </div>
  </div>
</template>
