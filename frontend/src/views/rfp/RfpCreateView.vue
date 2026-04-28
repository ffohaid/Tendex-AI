<script setup lang="ts">
/**
 * RFP Create View.
 *
 * Main wizard page for creating a new specifications booklet.
 * Orchestrates the 6-step wizard with:
 * - Step navigation via WizardStepper
 * - Auto-save every 30 seconds
 * - Per-step validation before advancing
 * - Final submission for approval
 *
 * Performance optimizations (TASK-703):
 * - Wizard steps loaded via defineAsyncComponent for code splitting.
 * - Only the active step component is loaded, reducing initial bundle size.
 * - v-if ensures inactive steps are not rendered in the DOM.
 */
import { ref, onMounted, onUnmounted, defineAsyncComponent } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { useRfpStore } from '@/stores/rfp'
import WizardStepper from '@/components/rfp/WizardStepper.vue'
import AutoSaveIndicator from '@/components/rfp/AutoSaveIndicator.vue'
import { submitRfpForApproval } from '@/services/rfpService'

/* Lazy-loaded step components (TASK-703: Code splitting per wizard step) */
const Step1BasicInfo = defineAsyncComponent(
  () => import('@/components/rfp/Step1BasicInfo.vue'),
)
const Step2Settings = defineAsyncComponent(
  () => import('@/components/rfp/Step2Settings.vue'),
)
const Step3Content = defineAsyncComponent(
  () => import('@/components/rfp/Step3Content.vue'),
)
const Step4Boq = defineAsyncComponent(
  () => import('@/components/rfp/Step4Boq.vue'),
)
const Step5Attachments = defineAsyncComponent(
  () => import('@/components/rfp/Step5Attachments.vue'),
)
const Step6Review = defineAsyncComponent(
  () => import('@/components/rfp/Step6Review.vue'),
)

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const rfpStore = useRfpStore()

/** Step component refs for validation */
const step1Ref = ref<InstanceType<typeof Step1BasicInfo> | null>(null)
const step2Ref = ref<InstanceType<typeof Step2Settings> | null>(null)
const step3Ref = ref<InstanceType<typeof Step3Content> | null>(null)
const step4Ref = ref<InstanceType<typeof Step4Boq> | null>(null)
const step5Ref = ref<InstanceType<typeof Step5Attachments> | null>(null)

const isSubmitting = ref(false)
const submitError = ref('')

/** Load existing RFP if editing */
onMounted(async () => {
  const rfpId = route.params.id as string
  if (rfpId) {
    await rfpStore.loadRfp(rfpId)
    // Always open explicit edit sessions from the first wizard step.
    // Saved progress remains available through the stepper after load.
    rfpStore.goToStep(1)
  } else {
    rfpStore.resetForm()
  }
  rfpStore.startAutoSave()
})

onUnmounted(() => {
  rfpStore.stopAutoSave()
})

/** Validate current step before advancing */
async function validateCurrentStep(): Promise<boolean> {
  switch (rfpStore.currentStep) {
    case 1:
      return step1Ref.value ? await step1Ref.value.validate() : true
    case 2:
      return step2Ref.value ? await step2Ref.value.validate() : true
    case 3:
      return step3Ref.value ? await step3Ref.value.validate() : true
    case 4:
      return step4Ref.value ? await step4Ref.value.validate() : true
    case 5:
      return step5Ref.value ? await step5Ref.value.validate() : true
    default:
      return true
  }
}

/** Handle next step */
async function handleNext() {
  const isValid = await validateCurrentStep()
  if (!isValid) return

  const saveSucceeded = await rfpStore.saveCurrentStep()
  if (!saveSucceeded) {
    submitError.value = rfpStore.errors[0] || t('rfp.errors.saveFirst')
    return
  }

  submitError.value = ''
  rfpStore.nextStep()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

/** Handle previous step */
function handlePrev() {
  rfpStore.prevStep()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

/** Handle step click from stepper */
async function handleStepClick(step: number) {
  if (step < rfpStore.currentStep) {
    rfpStore.goToStep(step)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } else if (step === rfpStore.currentStep + 1) {
    await handleNext()
  }
}

/** Handle edit step from review */
function handleEditStep(step: number) {
  rfpStore.goToStep(step)
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

/** Handle final submission */
async function handleSubmit() {
  isSubmitting.value = true
  submitError.value = ''

  try {
    // Ensure data is saved first
    await rfpStore.performAutoSave()

    if (rfpStore.formData.id) {
      const response = await submitRfpForApproval(rfpStore.formData.id)
      if (response.success) {
        router.push({ name: 'rfp-list' })
      } else {
        submitError.value = response.message || t('rfp.errors.submitFailed')
      }
    } else {
      submitError.value = t('rfp.errors.saveFirst')
    }
  } catch (error) {
    submitError.value = t('rfp.errors.submitFailed')
  } finally {
    isSubmitting.value = false
  }
}

/** Handle manual save */
async function handleManualSave() {
  await rfpStore.saveForm()
}
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <!-- Header -->
    <div class="sticky top-0 z-10 border-b border-surface-dim bg-white shadow-sm">
      <div class="mx-auto max-w-6xl px-4 py-4 sm:px-6">
        <div class="flex items-center justify-between">
          <div>
            <h1 class="text-xl font-bold text-secondary">
              {{ route.params.id ? t('rfp.titles.edit') : t('rfp.titles.create') }}
            </h1>
            <p class="mt-0.5 text-sm text-tertiary">
              {{ t('rfp.titles.subtitle') }}
            </p>
          </div>

          <div class="flex items-center gap-3">
            <!-- Auto-save indicator -->
            <AutoSaveIndicator
              :status="rfpStore.autoSaveStatus"
              :last-saved-at="rfpStore.formData.lastAutoSavedAt"
            />

            <!-- Manual save button -->
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
              :disabled="rfpStore.isSaving"
              @click="handleManualSave"
            >
              <i class="pi pi-save text-sm"></i>
              {{ t('rfp.actions.saveDraft') }}
            </button>

            <!-- Back to list -->
            <router-link
              :to="{ name: 'rfp-list' }"
              class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-tertiary transition-colors hover:bg-surface-muted hover:text-secondary"
            >
              <i class="pi pi-times text-sm"></i>
              {{ t('common.cancel') }}
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Wizard content -->
    <div class="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <!-- Stepper -->
      <WizardStepper
        :steps="rfpStore.wizardSteps"
        :current-step="rfpStore.currentStep"
        @step-click="handleStepClick"
      />

      <!-- Loading state -->
      <div v-if="rfpStore.isLoading" class="flex items-center justify-center py-20">
        <div class="text-center">
          <i class="pi pi-spin pi-spinner mb-3 text-3xl text-primary"></i>
          <p class="text-sm text-tertiary">{{ t('common.loading') }}</p>
        </div>
      </div>

      <!-- Error state -->
      <div
        v-else-if="rfpStore.errors.length > 0"
        class="rounded-lg border border-danger/20 bg-danger/5 p-6 text-center"
      >
        <i class="pi pi-exclamation-triangle mb-3 text-3xl text-danger"></i>
        <p class="text-sm text-danger">{{ rfpStore.errors[0] }}</p>
        <button
          type="button"
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="rfpStore.errors = []"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Step content -->
      <div v-else class="rounded-xl border border-surface-dim bg-white p-6 shadow-sm sm:p-8">
        <!-- Step 1: Basic Info -->
        <Step1BasicInfo v-if="rfpStore.currentStep === 1" ref="step1Ref" />

        <!-- Step 2: Settings -->
        <Step2Settings v-if="rfpStore.currentStep === 2" ref="step2Ref" />

        <!-- Step 3: Content -->
        <Step3Content v-if="rfpStore.currentStep === 3" ref="step3Ref" />

        <!-- Step 4: BOQ -->
        <Step4Boq v-if="rfpStore.currentStep === 4" ref="step4Ref" />

        <!-- Step 5: Attachments -->
        <Step5Attachments v-if="rfpStore.currentStep === 5" ref="step5Ref" />

        <!-- Step 6: Review -->
        <Step6Review
          v-if="rfpStore.currentStep === 6"
          @submit="handleSubmit"
          @edit-step="handleEditStep"
        />

        <!-- Submit error -->
        <div
          v-if="submitError"
          class="mt-4 rounded-lg border border-danger/20 bg-danger/5 p-3 text-sm text-danger"
        >
          <i class="pi pi-exclamation-circle me-1"></i>
          {{ submitError }}
        </div>

        <!-- Navigation buttons -->
        <div
          class="mt-8 flex items-center border-t border-surface-dim pt-6"
          :class="rfpStore.isFirstStep ? 'justify-end' : 'justify-between'"
        >
          <button
            v-if="!rfpStore.isFirstStep"
            type="button"
            class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-6 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
            @click="handlePrev"
          >
            <i class="pi pi-arrow-right text-sm"></i>
            {{ t('rfp.actions.previous') }}
          </button>

          <button
            v-if="!rfpStore.isLastStep"
            type="button"
            class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-bold text-white transition-colors hover:bg-primary-dark"
            @click="handleNext"
          >
            {{ t('rfp.actions.next') }}
            <i class="pi pi-arrow-left text-sm"></i>
          </button>
        </div>
      </div>

      <!-- Progress bar -->
      <div class="mt-6 flex items-center justify-center gap-2 text-sm text-tertiary">
        <span>{{ t('rfp.labels.step') }} {{ rfpStore.currentStep }} {{ t('rfp.labels.of') }} {{ rfpStore.totalSteps }}</span>
        <div class="h-2 w-48 overflow-hidden rounded-full bg-surface-dim">
          <div
            class="h-full rounded-full bg-primary transition-all duration-500"
            :style="{ width: `${rfpStore.progressPercentage}%` }"
          ></div>
        </div>
        <span>{{ rfpStore.progressPercentage }}%</span>
      </div>
    </div>
  </div>
</template>
