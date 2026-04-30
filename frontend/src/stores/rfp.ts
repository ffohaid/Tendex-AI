/**
 * RFP Pinia Store.
 *
 * Manages the full state of RFP creation/editing, including:
 * - Wizard step navigation
 * - Form data for all 6 steps
 * - Auto-save every 30 seconds
 * - Validation state tracking
 * - API integration (no mock data)
 */
import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import type {
  RfpFormData,
  RfpBasicInfo,
  RfpSettings,
  RfpContent,
  RfpBoq,
  RfpAttachments,
  RfpSection,
  BoqItem,
  RfpAttachment,
  AutoSaveStatus,
  WizardStep,
  EvaluationCriterion,
  CompetitionType,
} from '@/types/rfp'
import {
  createRfp,
  autoSaveDraft,
  fetchRfpById,
  saveStepData,
} from '@/services/rfpService'

/** Generate a unique ID */
function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`
}

function normalizePercentage(value: number | null | undefined): number {
  if (typeof value !== 'number' || Number.isNaN(value) || !Number.isFinite(value)) {
    return 0
  }

  return Math.min(100, Math.max(0, value))
}

/** Default empty form data */
function createEmptyFormData(): RfpFormData {
  return {
    id: null,
    basicInfo: {
      projectName: '',
      projectDescription: '',
      competitionType: '',
      estimatedValue: null,
      currency: 'SAR',
      bookletNumber: '',
      department: '',
      fiscalYear: '',
      bookletIssueDate: '',
      inquiriesStartDate: '',
      inquiryPeriodDays: null,
      offersStartDate: '',
      submissionDeadline: '',
      expectedAwardDate: '',
      workStartDate: '',
    },
    settings: {
      evaluationMethod: '',
      technicalWeight: 70,
      financialWeight: 30,
      minimumTechnicalScore: 60,
      allowPartialOffers: false,
      requireBankGuarantee: true,
      guaranteePercentage: 5,
      evaluationCriteria: [],
    },
    content: {
      sections: [],
      creationMethod: 'wizard',
      templateId: null,
      cloneFromId: null,
    },
    boq: {
      items: [],
      totalEstimatedValue: 0,
      includesVat: true,
      vatPercentage: 15,
    },
    attachments: {
      files: [],
      requiredAttachmentTypes: [],
    },
    status: 'draft',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    lastAutoSavedAt: null,
    currentStep: 1,
    completionPercentage: 0,
  }
}

export const useRfpStore = defineStore('rfp', () => {
  /* ---------------------------------------------------------------- */
  /*  State                                                           */
  /* ---------------------------------------------------------------- */
  const formData = ref<RfpFormData>(createEmptyFormData())
  const currentStep = ref(1)
  const totalSteps = 6
  const autoSaveStatus = ref<AutoSaveStatus>('idle')
  const isLoading = ref(false)
  const isSaving = ref(false)
  const errors = ref<string[]>([])
  const autoSaveTimer = ref<ReturnType<typeof setInterval> | null>(null)
  const isDirty = ref(false)

  /* ---------------------------------------------------------------- */
  /*  Wizard Steps Definition                                         */
  /* ---------------------------------------------------------------- */
  const wizardSteps = computed<WizardStep[]>(() => [
    {
      id: 1,
      titleKey: 'rfp.steps.basicInfo',
      descriptionKey: 'rfp.steps.basicInfoDesc',
      icon: 'pi pi-info-circle',
      isCompleted: currentStep.value > 1,
      isActive: currentStep.value === 1,
      isAccessible: true,
    },
    {
      id: 2,
      titleKey: 'rfp.steps.settings',
      descriptionKey: 'rfp.steps.settingsDesc',
      icon: 'pi pi-cog',
      isCompleted: currentStep.value > 2,
      isActive: currentStep.value === 2,
      isAccessible: currentStep.value >= 2,
    },
    {
      id: 3,
      titleKey: 'rfp.steps.content',
      descriptionKey: 'rfp.steps.contentDesc',
      icon: 'pi pi-file-edit',
      isCompleted: currentStep.value > 3,
      isActive: currentStep.value === 3,
      isAccessible: currentStep.value >= 3,
    },
    {
      id: 4,
      titleKey: 'rfp.steps.boq',
      descriptionKey: 'rfp.steps.boqDesc',
      icon: 'pi pi-table',
      isCompleted: currentStep.value > 4,
      isActive: currentStep.value === 4,
      isAccessible: currentStep.value >= 4,
    },
    {
      id: 5,
      titleKey: 'rfp.steps.attachments',
      descriptionKey: 'rfp.steps.attachmentsDesc',
      icon: 'pi pi-paperclip',
      isCompleted: currentStep.value > 5,
      isActive: currentStep.value === 5,
      isAccessible: currentStep.value >= 5,
    },
    {
      id: 6,
      titleKey: 'rfp.steps.review',
      descriptionKey: 'rfp.steps.reviewDesc',
      icon: 'pi pi-check-circle',
      isCompleted: false,
      isActive: currentStep.value === 6,
      isAccessible: currentStep.value >= 6,
    },
  ])

  /* ---------------------------------------------------------------- */
  /*  Computed                                                        */
  /* ---------------------------------------------------------------- */
  const isFirstStep = computed(() => currentStep.value === 1)
  const isLastStep = computed(() => currentStep.value === totalSteps)
  const progressPercentage = computed(
    () => Math.round(((currentStep.value - 1) / (totalSteps - 1)) * 100),
  )

  const boqTotal = computed(() => {
    const subtotal = formData.value.boq.items.reduce(
      (sum, item) => sum + item.quantity * item.estimatedPrice,
      0,
    )
    if (formData.value.boq.includesVat) {
      return subtotal * (1 + formData.value.boq.vatPercentage / 100)
    }
    return subtotal
  })

  const criteriaWeightTotal = computed(() =>
    formData.value.settings.evaluationCriteria.reduce(
      (sum, c) => sum + c.weight,
      0,
    ),
  )

  /* ---------------------------------------------------------------- */
  /*  Actions: Navigation                                             */
  /* ---------------------------------------------------------------- */
  function goToStep(step: number) {
    if (step >= 1 && step <= totalSteps) {
      currentStep.value = step
      formData.value.currentStep = step
      isDirty.value = true
    }
  }

  function nextStep() {
    if (currentStep.value < totalSteps) {
      goToStep(currentStep.value + 1)
    }
  }

  function prevStep() {
    if (currentStep.value > 1) {
      goToStep(currentStep.value - 1)
    }
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: Form Data                                              */
  /* ---------------------------------------------------------------- */
  function updateBasicInfo(data: Partial<RfpBasicInfo>) {
    formData.value.basicInfo = { ...formData.value.basicInfo, ...data }
    isDirty.value = true
  }

  function updateSettings(data: Partial<RfpSettings>) {
    formData.value.settings = { ...formData.value.settings, ...data }
    isDirty.value = true
  }

  function updateContent(data: Partial<RfpContent>) {
    formData.value.content = { ...formData.value.content, ...data }
    isDirty.value = true
  }

  function updateBoq(data: Partial<RfpBoq>) {
    formData.value.boq = { ...formData.value.boq, ...data }
    isDirty.value = true
  }

  function updateAttachments(data: Partial<RfpAttachments>) {
    formData.value.attachments = { ...formData.value.attachments, ...data }
    isDirty.value = true
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: Sections (Drag & Drop)                                 */
  /* ---------------------------------------------------------------- */
  function addSection(section?: Partial<RfpSection>) {
    const newSection: RfpSection = {
      id: generateId(),
      title: section?.title || '',
      content: section?.content || '',
      contentHtml: section?.contentHtml || '',
      order: formData.value.content.sections.length,
      isRequired: section?.isRequired || false,
      colorCode: section?.colorCode || 'green',
      assignedTo: section?.assignedTo || null,
      isCompleted: false,
    }
    formData.value.content.sections.push(newSection)
    isDirty.value = true
  }

  function removeSection(sectionId: string) {
    formData.value.content.sections = formData.value.content.sections
      .filter((s) => s.id !== sectionId)
      .map((s, index) => ({ ...s, order: index }))
    isDirty.value = true
  }

  function updateSection(sectionId: string, data: Partial<RfpSection>) {
    const index = formData.value.content.sections.findIndex(
      (s) => s.id === sectionId,
    )
    if (index !== -1) {
      formData.value.content.sections[index] = {
        ...formData.value.content.sections[index],
        ...data,
      }
      isDirty.value = true
    }
  }

  function reorderSections(sections: RfpSection[]) {
    formData.value.content.sections = sections.map((s, index) => ({
      ...s,
      order: index,
    }))
    isDirty.value = true
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: BOQ Items                                              */
  /* ---------------------------------------------------------------- */
  function addBoqItem(item?: Partial<BoqItem>) {
    const newItem: BoqItem = {
      id: generateId(),
      category: item?.category || '',
      description: item?.description || '',
      unit: item?.unit || 'unit',
      quantity: item?.quantity || 0,
      estimatedPrice: item?.estimatedPrice || 0,
      totalPrice: 0,
      notes: item?.notes || '',
      order: formData.value.boq.items.length,
    }
    newItem.totalPrice = newItem.quantity * newItem.estimatedPrice
    formData.value.boq.items.push(newItem)
    isDirty.value = true
  }

  function removeBoqItem(itemId: string) {
    formData.value.boq.items = formData.value.boq.items
      .filter((i) => i.id !== itemId)
      .map((i, index) => ({ ...i, order: index }))
    isDirty.value = true
  }

  function updateBoqItem(itemId: string, data: Partial<BoqItem>) {
    const index = formData.value.boq.items.findIndex((i) => i.id === itemId)
    if (index !== -1) {
      const updated = { ...formData.value.boq.items[index], ...data }
      updated.totalPrice = updated.quantity * updated.estimatedPrice
      formData.value.boq.items[index] = updated
      isDirty.value = true
    }
  }

  function reorderBoqItems(items: BoqItem[]) {
    formData.value.boq.items = items.map((i, index) => ({
      ...i,
      order: index,
    }))
    isDirty.value = true
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: Evaluation Criteria                                    */
  /* ---------------------------------------------------------------- */
  function addCriterion(criterion?: Partial<EvaluationCriterion>) {
    const newCriterion: EvaluationCriterion = {
      id: generateId(),
      name: criterion?.name || '',
      weight: normalizePercentage(criterion?.weight),
      description: criterion?.description || '',
      order: formData.value.settings.evaluationCriteria.length,
    }
    formData.value.settings.evaluationCriteria.push(newCriterion)
    isDirty.value = true
  }

  function removeCriterion(criterionId: string) {
    formData.value.settings.evaluationCriteria =
      formData.value.settings.evaluationCriteria
        .filter((c) => c.id !== criterionId)
        .map((c, index) => ({ ...c, order: index }))
    isDirty.value = true
  }

  function updateCriterion(
    criterionId: string,
    data: Partial<EvaluationCriterion>,
  ) {
    const index = formData.value.settings.evaluationCriteria.findIndex(
      (c) => c.id === criterionId,
    )
    if (index !== -1) {
      const currentCriterion = formData.value.settings.evaluationCriteria[index]
      const nextData: Partial<EvaluationCriterion> = { ...data }

      if (Object.prototype.hasOwnProperty.call(data, 'weight')) {
        const requestedWeight = normalizePercentage(data.weight)
        const totalWithoutCurrent = formData.value.settings.evaluationCriteria.reduce(
          (sum, criterion, criterionIndex) => {
            return criterionIndex === index ? sum : sum + criterion.weight
          },
          0,
        )
        nextData.weight = Math.max(0, Math.min(requestedWeight, 100 - totalWithoutCurrent))
      }

      formData.value.settings.evaluationCriteria[index] = {
        ...currentCriterion,
        ...nextData,
      }
      isDirty.value = true
    }
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: Attachments                                            */
  /* ---------------------------------------------------------------- */
  function addAttachment(attachment: RfpAttachment) {
    formData.value.attachments.files.push(attachment)
    isDirty.value = true
  }

  function removeAttachment(attachmentId: string) {
    formData.value.attachments.files =
      formData.value.attachments.files.filter((a) => a.id !== attachmentId)
    isDirty.value = true
  }

  function toggleRequiredAttachmentType(key: string) {
    const types = formData.value.attachments.requiredAttachmentTypes
    const idx = types.indexOf(key)
    if (idx >= 0) {
      types.splice(idx, 1)
    } else {
      types.push(key)
    }
    isDirty.value = true
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: Auto-save (every 30 seconds)                           */
  /* ---------------------------------------------------------------- */

  /**
   * Checks whether the minimum required fields are filled before
   * attempting to create a new competition via the backend.
   * This prevents 400 errors from the backend validation when the
   * user has only partially filled the form.
   */
  function hasMinimumRequiredFields(): boolean {
    const basic = formData.value.basicInfo
    const hasName = basic.projectName && basic.projectName.trim().length > 0
    const hasType = !!basic.competitionType && (basic.competitionType as string) !== ''
    return !!(hasName && hasType)
  }

  async function performAutoSave() {
    if (!isDirty.value || isSaving.value) return
    if (!formData.value.id) {
      /* First save: create the RFP — only if minimum fields are filled */
      if (!hasMinimumRequiredFields()) {
        // Not enough data yet; skip this auto-save cycle silently
        return
      }
      isSaving.value = true
      autoSaveStatus.value = 'saving'
      const response = await createRfp(formData.value)
      isSaving.value = false

      if (response.success && response.data) {
        formData.value.id = response.data.id
        formData.value.lastAutoSavedAt = new Date().toISOString()
        autoSaveStatus.value = 'saved'
        isDirty.value = false
      } else {
        autoSaveStatus.value = 'error'
        /* Auto-save errors are non-blocking: do NOT set errors.value
           so the wizard UI remains usable even when backend is offline. */
        console.warn('[AutoSave] Create failed:', response.message)
      }
      return
    }

    /* Subsequent saves: auto-save patch */
    isSaving.value = true
    autoSaveStatus.value = 'saving'
    const response = await autoSaveDraft(formData.value.id, formData.value)
    isSaving.value = false

    if (response.success) {
      formData.value.lastAutoSavedAt =
        response.data?.savedAt || new Date().toISOString()
      formData.value.updatedAt = new Date().toISOString()
      autoSaveStatus.value = 'saved'
      isDirty.value = false
    } else {
      autoSaveStatus.value = 'error'
      /* Auto-save errors are non-blocking: do NOT set errors.value
         so the wizard UI remains usable even when backend is offline. */
      console.warn('[AutoSave] Patch failed:', response.message)
    }
  }

  function startAutoSave() {
    stopAutoSave()
    autoSaveTimer.value = setInterval(performAutoSave, 30_000)
  }

  function stopAutoSave() {
    if (autoSaveTimer.value) {
      clearInterval(autoSaveTimer.value)
      autoSaveTimer.value = null
    }
  }

  /* ---------------------------------------------------------------- */
  /*  Actions: Load / Reset                                           */
  /* ---------------------------------------------------------------- */
  async function loadRfp(id: string) {
    isLoading.value = true
    errors.value = []
    const response = await fetchRfpById(id)
    isLoading.value = false

    if (response.success && response.data) {
      formData.value = response.data
      currentStep.value = response.data.currentStep || 1
      isDirty.value = false
    } else {
      errors.value = response.errors.length
        ? response.errors
        : [response.message]
    }
  }

  function resetForm() {
    stopAutoSave()
    formData.value = createEmptyFormData()
    currentStep.value = 1
    autoSaveStatus.value = 'idle'
    isDirty.value = false
    errors.value = []
  }

  /** Manual save */
  async function saveForm() {
    isDirty.value = true
    await saveCurrentStep()
  }

  /**
   * Pre-fill the form from extraction results (Upload & Extract flow).
   * Maps AI-extracted data into the store's RfpFormData shape.
   */
  function prefillFromExtraction(extraction: {
    projectNameAr: string
    projectNameEn?: string
    projectDescription?: string
    detectedCompetitionType?: string
    estimatedBudget?: number
    projectDurationDays?: number
    sections: Array<{
      titleAr: string
      titleEn?: string
      sectionType: string
      contentHtml: string
      isMandatory: boolean
      sortOrder: number
    }>
    boqItems: Array<{
      itemNumber: string
      descriptionAr: string
      unit: string
      quantity: number
      estimatedUnitPrice?: number
      category?: string
      sortOrder: number
    }>
  }) {
    // Reset form first
    resetForm()

    // Map competition type from backend string to frontend literal
    const competitionTypeMap: Record<string, CompetitionType> = {
      PublicTender: 'public_tender',
      LimitedTender: 'limited_tender',
      DirectPurchase: 'direct_purchase',
      FrameworkAgreement: 'framework_agreement',
      ReverseAuction: 'reverse_auction',
    }

    // Pre-fill basic info
    formData.value.basicInfo.projectName = extraction.projectNameAr || ''
    formData.value.basicInfo.projectDescription = extraction.projectDescription || ''
    formData.value.basicInfo.competitionType =
      competitionTypeMap[extraction.detectedCompetitionType || ''] || ''
    formData.value.basicInfo.estimatedValue = extraction.estimatedBudget || null

    // Pre-fill sections
    formData.value.content.creationMethod = 'upload_extract'
    formData.value.content.sections = []
    for (const section of extraction.sections) {
      addSection({
        title: section.titleAr,
        content: '',
        contentHtml: section.contentHtml,
        isRequired: section.isMandatory,
        colorCode: 'green',
      })
    }

    // Pre-fill BOQ items
    formData.value.boq.items = []
    for (const item of extraction.boqItems) {
      addBoqItem({
        category: item.category || '',
        description: item.descriptionAr,
        unit: 'unit', // Default; AI may not map exactly
        quantity: item.quantity,
        estimatedPrice: item.estimatedUnitPrice || 0,
      })
    }

    isDirty.value = true
  }

  /**
   * Save the current step's data to the backend.
   * Called by the wizard when the user clicks "Next".
   * Returns true if save was successful, false otherwise.
   */
  async function saveCurrentStep(): Promise<boolean> {
    // Ensure the competition is created first
    if (!formData.value.id) {
      if (!hasMinimumRequiredFields()) {
        errors.value = ['يرجى استكمال بيانات الخطوة الحالية قبل المتابعة.']
        console.warn('[SaveStep] Not enough data to create competition')
        return false
      }
      isSaving.value = true
      autoSaveStatus.value = 'saving'
      const createResponse = await createRfp(formData.value)
      isSaving.value = false
      if (createResponse.success && createResponse.data) {
        formData.value.id = createResponse.data.id
        formData.value.lastAutoSavedAt = new Date().toISOString()
        autoSaveStatus.value = 'saved'
        isDirty.value = false
        errors.value = []
      } else {
        autoSaveStatus.value = 'error'
        errors.value = createResponse.errors.length
          ? createResponse.errors
          : [createResponse.message || 'تعذر إنشاء الكراسة.']
        console.warn('[SaveStep] Create failed:', createResponse.message)
        return false
      }
    }

    // Now save the step-specific data
    isSaving.value = true
    autoSaveStatus.value = 'saving'
    const result = await saveStepData(
      formData.value.id!,
      currentStep.value,
      formData.value,
    )
    isSaving.value = false

    if (result.success) {
      formData.value.lastAutoSavedAt = new Date().toISOString()
      formData.value.updatedAt = new Date().toISOString()
      autoSaveStatus.value = 'saved'
      isDirty.value = false
      errors.value = []
      return true
    } else {
      autoSaveStatus.value = 'error'
      errors.value = result.errors.length
        ? result.errors
        : [result.message || 'تعذر حفظ بيانات الخطوة الحالية.']
      console.warn('[SaveStep] Step save failed:', result.message)
      return false
    }
  }

  /* ---------------------------------------------------------------- */
  /*  Watch for dirty state to update timestamp                       */
  /* ---------------------------------------------------------------- */
  watch(isDirty, (val) => {
    if (val) {
      autoSaveStatus.value = 'idle'
    }
  })

  return {
    /* State */
    formData,
    currentStep,
    totalSteps,
    autoSaveStatus,
    isLoading,
    isSaving,
    errors,
    isDirty,
    wizardSteps,

    /* Computed */
    isFirstStep,
    isLastStep,
    progressPercentage,
    boqTotal,
    criteriaWeightTotal,

    /* Navigation */
    goToStep,
    nextStep,
    prevStep,

    /* Form Data */
    updateBasicInfo,
    updateSettings,
    updateContent,
    updateBoq,
    updateAttachments,

    /* Sections */
    addSection,
    removeSection,
    updateSection,
    reorderSections,

    /* BOQ */
    addBoqItem,
    removeBoqItem,
    updateBoqItem,
    reorderBoqItems,

    /* Criteria */
    addCriterion,
    removeCriterion,
    updateCriterion,

    /* Attachments */
    addAttachment,
    removeAttachment,
    toggleRequiredAttachmentType,

    /* Auto-save */
    startAutoSave,
    stopAutoSave,
    performAutoSave,
    saveCurrentStep,

    /* Load / Reset */
    loadRfp,
    resetForm,
    saveForm,

    /* Upload & Extract */
    prefillFromExtraction,
  }
})
