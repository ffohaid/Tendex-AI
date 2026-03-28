/**
 * Composable for RFP form validation using VeeValidate + Zod.
 *
 * Provides step-specific validation with real-time error feedback
 * and Arabic error messages.
 */
import { computed } from 'vue'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import {
  basicInfoSchema,
  settingsSchema,
  contentSchema,
  boqSchema,
  attachmentsSchema,
} from '@/validations/rfp'
import type { ZodSchema } from 'zod'

/**
 * Returns the Zod schema for a given wizard step number.
 */
export function getStepSchema(step: number): ZodSchema {
  switch (step) {
    case 1:
      return basicInfoSchema
    case 2:
      return settingsSchema
    case 3:
      return contentSchema
    case 4:
      return boqSchema
    case 5:
      return attachmentsSchema
    default:
      return basicInfoSchema
  }
}

/**
 * Creates a VeeValidate form instance for a specific wizard step.
 *
 * @param step - The wizard step number (1-5)
 * @param initialValues - Initial form values
 */
export function useStepValidation<T extends Record<string, unknown>>(
  step: number,
  initialValues?: T,
) {
  const schema = getStepSchema(step)
  const typedSchema = toTypedSchema(schema)

  const {
    handleSubmit,
    errors,
    meta,
    validate,
    resetForm,
    setFieldValue,
    setValues,
    values,
  } = useForm({
    validationSchema: typedSchema,
    initialValues: initialValues as Record<string, unknown>,
    validateOnMount: false,
  })

  const isValid = computed(() => meta.value.valid)
  const isDirty = computed(() => meta.value.dirty)
  const hasErrors = computed(() => Object.keys(errors.value).length > 0)

  /**
   * Validate the entire step and return whether it passed.
   */
  async function validateStep(): Promise<boolean> {
    const result = await validate()
    return result.valid
  }

  return {
    handleSubmit,
    errors,
    meta,
    validate,
    validateStep,
    resetForm,
    setFieldValue,
    setValues,
    values,
    isValid,
    isDirty,
    hasErrors,
  }
}

/**
 * Creates a single validated field with real-time validation.
 *
 * @param name - Field name
 * @param label - Field label (for accessibility)
 */
export function useValidatedField(name: string, label?: string) {
  const { value, errorMessage, handleBlur, handleChange, meta } = useField(
    name,
    undefined,
    { label },
  )

  return {
    value,
    errorMessage,
    handleBlur,
    handleChange,
    meta,
  }
}
