<script setup lang="ts">
/**
 * Step 1: Basic Information.
 *
 * Collects the core booklet metadata using the redesigned two-column layout.
 * Project name and description remain full width, while the rest of the fields
 * are arranged in paired rows to reduce visual complexity.
 *
 * Validations are powered by VeeValidate + Zod, with an additional asynchronous
 * uniqueness check for the booklet number when the user provides a value.
 */
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { basicInfoSchema } from '@/validations/rfp'
import { useRfpStore } from '@/stores/rfp'
import FormField from './FormField.vue'
import AiTextHelper from '@/components/ai/AiTextHelper.vue'
import { formatCurrency } from '@/utils/numbers'
import { checkBookletNumberAvailability } from '@/services/rfpService'

const { t, locale } = useI18n()
const rfpStore = useRfpStore()

const schema = toTypedSchema(basicInfoSchema)

const { errors, validate } = useForm({
  validationSchema: schema,
  initialValues: { ...rfpStore.formData.basicInfo },
  validateOnMount: false,
})

const { value: projectName } = useField<string>('projectName')
const { value: projectDescription } = useField<string>('projectDescription')
const { value: competitionType } = useField<string>('competitionType')
const { value: estimatedValue } = useField<number | null>('estimatedValue')
const { value: bookletNumber } = useField<string>('bookletNumber')
const { value: department } = useField<string>('department')
const { value: fiscalYear } = useField<string>('fiscalYear')
const { value: bookletIssueDate } = useField<string>('bookletIssueDate')
const { value: inquiriesStartDate } = useField<string>('inquiriesStartDate')
const { value: inquiryPeriodDays } = useField<number | null>('inquiryPeriodDays')
const { value: offersStartDate } = useField<string>('offersStartDate')
const { value: submissionDeadline } = useField<string>('submissionDeadline')
const { value: expectedAwardDate } = useField<string>('expectedAwardDate')
const { value: workStartDate } = useField<string>('workStartDate')

const bookletNumberError = ref('')
const isCheckingBookletNumber = ref(false)

const competitionTypes = computed(() => [
  { value: 'public_tender', label: t('rfp.competitionTypes.publicTender') },
  { value: 'limited_tender', label: t('rfp.competitionTypes.limitedTender') },
  { value: 'direct_purchase', label: t('rfp.competitionTypes.directPurchase') },
  { value: 'framework_agreement', label: t('rfp.competitionTypes.frameworkAgreement') },
  { value: 'reverse_auction', label: t('rfp.competitionTypes.reverseAuction') },
])

const fiscalYears = computed(() => {
  const currentYear = new Date().getFullYear()
  return [
    { value: String(currentYear - 1), label: String(currentYear - 1) },
    { value: String(currentYear), label: String(currentYear) },
    { value: String(currentYear + 1), label: String(currentYear + 1) },
    { value: String(currentYear + 2), label: String(currentYear + 2) },
    { value: String(currentYear + 3), label: String(currentYear + 3) },
  ]
})

const todayStr = computed(() => {
  const now = new Date()
  return now.toISOString().split('T')[0]
})

const dateMinForFiscalYear = computed(() => {
  if (!fiscalYear.value) return todayStr.value
  const yearStart = `${fiscalYear.value}-01-01`
  return yearStart > todayStr.value ? yearStart : todayStr.value
})

const dateMaxForFiscalYear = computed(() => {
  if (!fiscalYear.value) return ''
  return `${fiscalYear.value}-12-31`
})

const inquiriesStartMinDate = computed(() => bookletIssueDate.value || dateMinForFiscalYear.value)
const offersStartMinDate = computed(() => inquiriesStartDate.value || bookletIssueDate.value || dateMinForFiscalYear.value)
const submissionDeadlineMinDate = computed(() => offersStartDate.value || inquiriesStartDate.value || bookletIssueDate.value || dateMinForFiscalYear.value)
const expectedAwardMinDate = computed(() => submissionDeadline.value || offersStartDate.value || inquiriesStartDate.value || bookletIssueDate.value || dateMinForFiscalYear.value)
const workStartMinDate = computed(() => expectedAwardDate.value || submissionDeadline.value || offersStartDate.value || inquiriesStartDate.value || bookletIssueDate.value || dateMinForFiscalYear.value)

if (!bookletIssueDate.value) {
  bookletIssueDate.value = todayStr.value
}

async function validateBookletNumberUniqueness(): Promise<boolean> {
  bookletNumberError.value = ''
  const normalized = (bookletNumber.value || '').trim()

  if (!normalized) return true

  isCheckingBookletNumber.value = true
  const result = await checkBookletNumberAvailability(normalized, rfpStore.formData.id)
  isCheckingBookletNumber.value = false

  if (!result.success || !result.data?.isAvailable) {
    bookletNumberError.value = result.success
      ? (result.data?.message || t('rfp.validation.bookletNumberAlreadyExists'))
      : (result.message || t('rfp.validation.bookletNumberCheckFailed'))
    return false
  }

  return true
}

watch(bookletNumber, () => {
  bookletNumberError.value = ''
})

watch(
  [
    projectName,
    projectDescription,
    competitionType,
    estimatedValue,
    bookletNumber,
    department,
    fiscalYear,
    bookletIssueDate,
    inquiriesStartDate,
    inquiryPeriodDays,
    offersStartDate,
    submissionDeadline,
    expectedAwardDate,
    workStartDate,
  ],
  () => {
    rfpStore.updateBasicInfo({
      projectName: projectName.value,
      projectDescription: projectDescription.value,
      competitionType: competitionType.value as any,
      estimatedValue: estimatedValue.value,
      bookletNumber: bookletNumber.value,
      department: department.value,
      fiscalYear: fiscalYear.value,
      bookletIssueDate: bookletIssueDate.value,
      inquiriesStartDate: inquiriesStartDate.value,
      inquiryPeriodDays: inquiryPeriodDays.value,
      offersStartDate: offersStartDate.value,
      submissionDeadline: submissionDeadline.value,
      expectedAwardDate: expectedAwardDate.value,
      workStartDate: workStartDate.value,
    })
  },
  { deep: true },
)

watch(fiscalYear, (newYear) => {
  if (!newYear) return

  const yearStart = `${newYear}-01-01`
  const yearEnd = `${newYear}-12-31`
  const dateFields = [
    bookletIssueDate,
    inquiriesStartDate,
    offersStartDate,
    submissionDeadline,
  ]

  dateFields.forEach((field) => {
    if (field.value && (field.value < yearStart || field.value > yearEnd)) {
      field.value = ''
    }
  })
})

function applyProjectDescription(text: string) {
  projectDescription.value = text
}

defineExpose({
  validate: async () => {
    const result = await validate()
    const bookletNumberValid = await validateBookletNumberUniqueness()
    return result.valid && bookletNumberValid
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
          />
        </FormField>
      </div>

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
              :max-characters="2000"
              @text-generated="applyProjectDescription"
            />
          </div>
        </FormField>
      </div>

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

      <FormField
        :label="t('rfp.fields.bookletNumber')"
        field-id="bookletNumber"
        :error="bookletNumberError || errors.bookletNumber"
        :help-text="isCheckingBookletNumber ? t('rfp.hints.checkingBookletNumber') : t('rfp.hints.optionalBookletNumber')"
      >
        <input
          id="bookletNumber"
          v-model="bookletNumber"
          type="text"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="(bookletNumberError || errors.bookletNumber) ? 'border-danger' : 'border-surface-dim'"
          :placeholder="t('rfp.placeholders.bookletNumber')"
          @blur="validateBookletNumberUniqueness"
        />
      </FormField>

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

      <FormField
        :label="t('rfp.fields.bookletIssueDate')"
        field-id="bookletIssueDate"
        :error="errors.bookletIssueDate"
        :help-text="t('rfp.hints.autoGeneratedIssueDate')"
      >
        <input
          id="bookletIssueDate"
          v-model="bookletIssueDate"
          type="date"
          :min="dateMinForFiscalYear"
          :max="dateMaxForFiscalYear"
          readonly
          class="w-full rounded-lg border bg-surface-muted px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.bookletIssueDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <FormField
        :label="t('rfp.fields.inquiriesStartDate')"
        field-id="inquiriesStartDate"
        :error="errors.inquiriesStartDate"
      >
        <input
          id="inquiriesStartDate"
          v-model="inquiriesStartDate"
          type="date"
          :min="inquiriesStartMinDate"
          :max="dateMaxForFiscalYear"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.inquiriesStartDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <FormField
        :label="t('rfp.fields.inquiryPeriodDays')"
        field-id="inquiryPeriodDays"
        :error="errors.inquiryPeriodDays"
      >
        <input
          id="inquiryPeriodDays"
          v-model.number="inquiryPeriodDays"
          type="number"
          min="1"
          max="365"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.inquiryPeriodDays ? 'border-danger' : 'border-surface-dim'"
          :placeholder="t('rfp.placeholders.inquiryPeriodDays')"
        />
      </FormField>

      <FormField
        :label="t('rfp.fields.offersStartDate')"
        field-id="offersStartDate"
        :error="errors.offersStartDate"
      >
        <input
          id="offersStartDate"
          v-model="offersStartDate"
          type="date"
          :min="offersStartMinDate"
          :max="dateMaxForFiscalYear"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.offersStartDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <FormField
        :label="t('rfp.fields.submissionDeadline')"
        field-id="submissionDeadline"
        :error="errors.submissionDeadline"
      >
        <input
          id="submissionDeadline"
          v-model="submissionDeadline"
          type="date"
          :min="submissionDeadlineMinDate"
          :max="dateMaxForFiscalYear"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.submissionDeadline ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <FormField
        :label="t('rfp.fields.expectedAwardDate')"
        field-id="expectedAwardDate"
        :error="errors.expectedAwardDate"
      >
        <input
          id="expectedAwardDate"
          v-model="expectedAwardDate"
          type="date"
          :min="expectedAwardMinDate"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.expectedAwardDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>

      <FormField
        :label="t('rfp.fields.workStartDate')"
        field-id="workStartDate"
        :error="errors.workStartDate"
      >
        <input
          id="workStartDate"
          v-model="workStartDate"
          type="date"
          :min="workStartMinDate"
          class="w-full rounded-lg border px-4 py-2.5 text-sm transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          :class="errors.workStartDate ? 'border-danger' : 'border-surface-dim'"
        />
      </FormField>
    </div>
  </div>
</template>
