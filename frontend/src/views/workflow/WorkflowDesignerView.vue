<script setup lang="ts">
/**
 * WorkflowDesignerView — Workflow Definition Editor (Simplified Form-Based)
 *
 * Professional, user-friendly form for creating and editing approval workflow
 * definitions. All fields have clear Arabic labels, tooltips, and contextual help.
 *
 * Features:
 * - Create new workflow definitions with steps
 * - Edit existing workflow definitions
 * - Add/remove/reorder steps
 * - Configure step roles, SLA, and conditions
 * - Visual step timeline preview
 * - RTL/LTR support
 * - Clear field descriptions and tooltips
 */
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import {
  getWorkflowDefinitionById,
  createWorkflowDefinition,
  updateWorkflowDefinition,
  type WorkflowDefinitionDto,
  type CreateWorkflowStepRequest,
} from '@/services/workflowService'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const isLoading = ref(false)
const isSaving = ref(false)
const saveError = ref('')
const saveSuccess = ref('')
const isEditMode = computed(() => !!route.params.id)
const workflowId = computed(() => route.params.id as string | undefined)

// Form fields
const nameAr = ref('')
const nameEn = ref('')
const descriptionAr = ref('')
const descriptionEn = ref('')
const transitionFrom = ref(1) // UnderPreparation
const transitionTo = ref(2) // PendingApproval
const isActive = ref(true)

// Steps
interface StepForm {
  id?: string
  stepOrder: number
  requiredSystemRole: number
  requiredCommitteeRole: number
  stepNameAr: string
  stepNameEn: string
  slaHours: number | null
  isConditional: boolean
  conditionExpression: string
}

const steps = ref<StepForm[]>([])

// Tooltip state
const activeTooltip = ref('')

/* ------------------------------------------------------------------ */
/*  Enum Mappings                                                      */
/* ------------------------------------------------------------------ */

// Grouped competition statuses for better UX
const competitionStatusGroups = [
  {
    groupAr: 'مراحل إعداد الكراسة',
    groupEn: 'Booklet Preparation',
    statuses: [
      { value: 0, labelAr: 'مسودة', labelEn: 'Draft' },
      { value: 1, labelAr: 'قيد الإعداد', labelEn: 'Under Preparation' },
      { value: 2, labelAr: 'بانتظار الاعتماد', labelEn: 'Pending Approval' },
      { value: 3, labelAr: 'معتمدة', labelEn: 'Approved' },
    ],
  },
  {
    groupAr: 'مراحل النشر والعروض',
    groupEn: 'Publishing & Offers',
    statuses: [
      { value: 4, labelAr: 'منشورة', labelEn: 'Published' },
      { value: 5, labelAr: 'فترة الاستفسارات', labelEn: 'Inquiry Period' },
      { value: 6, labelAr: 'استقبال العروض', labelEn: 'Receiving Offers' },
      { value: 7, labelAr: 'إغلاق العروض', labelEn: 'Offers Closed' },
    ],
  },
  {
    groupAr: 'مراحل الفحص والتحليل',
    groupEn: 'Analysis & Examination',
    statuses: [
      { value: 8, labelAr: 'التحليل الفني', labelEn: 'Technical Analysis' },
      { value: 9, labelAr: 'اكتمال التحليل الفني', labelEn: 'Technical Analysis Completed' },
      { value: 10, labelAr: 'التحليل المالي', labelEn: 'Financial Analysis' },
      { value: 11, labelAr: 'اكتمال التحليل المالي', labelEn: 'Financial Analysis Completed' },
    ],
  },
  {
    groupAr: 'مراحل الترسية والعقد',
    groupEn: 'Award & Contract',
    statuses: [
      { value: 12, labelAr: 'إشعار الترسية', labelEn: 'Award Notification' },
      { value: 13, labelAr: 'اعتماد الترسية', labelEn: 'Award Approved' },
      { value: 14, labelAr: 'إجازة العقد', labelEn: 'Contract Approval' },
      { value: 15, labelAr: 'اعتماد العقد', labelEn: 'Contract Approved' },
      { value: 16, labelAr: 'توقيع العقد', labelEn: 'Contract Signed' },
    ],
  },
  {
    groupAr: 'حالات خاصة',
    groupEn: 'Special States',
    statuses: [
      { value: 90, labelAr: 'مرفوضة', labelEn: 'Rejected' },
      { value: 91, labelAr: 'ملغاة', labelEn: 'Cancelled' },
      { value: 92, labelAr: 'معلقة', labelEn: 'Suspended' },
    ],
  },
]

// Flat list for lookups
const competitionStatuses = competitionStatusGroups.flatMap(g => g.statuses)

const systemRoles = [
  { value: 1, labelAr: 'صاحب الصلاحية', labelEn: 'Authority Owner', descAr: 'المسؤول الأعلى الذي يملك صلاحية الاعتماد النهائي', descEn: 'Top authority with final approval power' },
  { value: 2, labelAr: 'مشرف النظام', labelEn: 'System Admin', descAr: 'مدير النظام المسؤول عن الإعدادات والتكوين', descEn: 'System administrator for settings and configuration' },
  { value: 3, labelAr: 'ممثل القطاع', labelEn: 'Sector Representative', descAr: 'ممثل الجهة أو القطاع المعني بالمنافسة', descEn: 'Representative of the relevant sector' },
  { value: 4, labelAr: 'المراقب المالي', labelEn: 'Financial Controller', descAr: 'المسؤول عن المراجعة والرقابة المالية', descEn: 'Responsible for financial review and oversight' },
  { value: 5, labelAr: 'عضو', labelEn: 'Member', descAr: 'عضو عادي في الفريق أو اللجنة', descEn: 'Regular team or committee member' },
  { value: 6, labelAr: 'مشاهد', labelEn: 'Viewer', descAr: 'صلاحية عرض فقط بدون إجراءات', descEn: 'View-only access without actions' },
]

const committeeRoles = [
  { value: 0, labelAr: 'غير مطلوب', labelEn: 'Not Required', descAr: 'لا يشترط أن يكون المعتمد عضواً في لجنة معينة', descEn: 'Approver does not need committee membership' },
  { value: 1, labelAr: 'رئيس لجنة الإعداد', labelEn: 'Preparation Committee Chair', descAr: 'رئيس اللجنة المسؤولة عن إعداد كراسة الشروط', descEn: 'Chair of the booklet preparation committee' },
  { value: 2, labelAr: 'عضو لجنة الإعداد', labelEn: 'Preparation Committee Member', descAr: 'عضو في لجنة إعداد كراسة الشروط', descEn: 'Member of the booklet preparation committee' },
  { value: 3, labelAr: 'رئيس لجنة الفحص الفني', labelEn: 'Technical Exam Committee Chair', descAr: 'رئيس لجنة الفحص والتقييم الفني للعروض', descEn: 'Chair of the technical examination committee' },
  { value: 4, labelAr: 'عضو لجنة الفحص الفني', labelEn: 'Technical Exam Committee Member', descAr: 'عضو في لجنة الفحص والتقييم الفني', descEn: 'Member of the technical examination committee' },
  { value: 5, labelAr: 'رئيس لجنة الفحص المالي', labelEn: 'Financial Exam Committee Chair', descAr: 'رئيس لجنة الفحص والتقييم المالي للعروض', descEn: 'Chair of the financial examination committee' },
  { value: 6, labelAr: 'عضو لجنة الفحص المالي', labelEn: 'Financial Exam Committee Member', descAr: 'عضو في لجنة الفحص والتقييم المالي', descEn: 'Member of the financial examination committee' },
  { value: 7, labelAr: 'رئيس لجنة مراجعة الاستفسارات', labelEn: 'Inquiry Review Committee Chair', descAr: 'رئيس لجنة مراجعة استفسارات المتنافسين', descEn: 'Chair of the inquiry review committee' },
  { value: 8, labelAr: 'عضو لجنة مراجعة الاستفسارات', labelEn: 'Inquiry Review Committee Member', descAr: 'عضو في لجنة مراجعة الاستفسارات', descEn: 'Member of the inquiry review committee' },
  { value: 9, labelAr: 'سكرتير اللجنة', labelEn: 'Committee Secretary', descAr: 'سكرتير اللجنة المسؤول عن التوثيق والتنسيق', descEn: 'Committee secretary for documentation and coordination' },
]

// Predefined condition templates for user-friendly conditional steps
const conditionTemplates = [
  { labelAr: 'القيمة التقديرية أكبر من مبلغ محدد', labelEn: 'Estimated value exceeds amount', expression: 'EstimatedValue > {amount}', placeholder: 'أدخل المبلغ بالريال' },
  { labelAr: 'نوع المنافسة عامة', labelEn: 'Public tender type', expression: 'CompetitionType == PublicTender', placeholder: '' },
  { labelAr: 'نوع المنافسة محدودة', labelEn: 'Limited tender type', expression: 'CompetitionType == LimitedTender', placeholder: '' },
  { labelAr: 'شراء مباشر', labelEn: 'Direct purchase', expression: 'CompetitionType == DirectPurchase', placeholder: '' },
  { labelAr: 'تعبير مخصص (متقدم)', labelEn: 'Custom expression (advanced)', expression: '', placeholder: 'أدخل التعبير الشرطي' },
]

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const transitionDescription = computed(() => {
  const from = competitionStatuses.find(s => s.value === transitionFrom.value)
  const to = competitionStatuses.find(s => s.value === transitionTo.value)
  if (!from || !to) return ''
  const isAr = locale.value === 'ar'
  return isAr
    ? `هذا المسار يُفعّل عند انتقال المنافسة من حالة "${from.labelAr}" إلى حالة "${to.labelAr}"`
    : `This workflow activates when competition transitions from "${from.labelEn}" to "${to.labelEn}"`
})

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function addStep() {
  const maxOrder = steps.value.length > 0 ? Math.max(...steps.value.map(s => s.stepOrder)) : 0
  steps.value.push({
    stepOrder: maxOrder + 1,
    requiredSystemRole: 5, // Member
    requiredCommitteeRole: 0, // None
    stepNameAr: '',
    stepNameEn: '',
    slaHours: 48,
    isConditional: false,
    conditionExpression: '',
  })
}

function removeStep(index: number) {
  steps.value.splice(index, 1)
  steps.value.forEach((s, i) => {
    s.stepOrder = i + 1
  })
}

function moveStepUp(index: number) {
  if (index <= 0) return
  const temp = steps.value[index]
  steps.value[index] = steps.value[index - 1]
  steps.value[index - 1] = temp
  steps.value.forEach((s, i) => {
    s.stepOrder = i + 1
  })
}

function moveStepDown(index: number) {
  if (index >= steps.value.length - 1) return
  const temp = steps.value[index]
  steps.value[index] = steps.value[index + 1]
  steps.value[index + 1] = temp
  steps.value.forEach((s, i) => {
    s.stepOrder = i + 1
  })
}

function toggleTooltip(id: string) {
  activeTooltip.value = activeTooltip.value === id ? '' : id
}

function getSystemRoleLabel(value: number): string {
  const role = systemRoles.find(r => r.value === value)
  return locale.value === 'ar' ? (role?.labelAr || '') : (role?.labelEn || '')
}

function getCommitteeRoleLabel(value: number): string {
  const role = committeeRoles.find(r => r.value === value)
  return locale.value === 'ar' ? (role?.labelAr || '') : (role?.labelEn || '')
}

function getStatusLabel(value: number): string {
  const status = competitionStatuses.find(s => s.value === value)
  return locale.value === 'ar' ? (status?.labelAr || '') : (status?.labelEn || '')
}

function applyConditionTemplate(step: StepForm, template: typeof conditionTemplates[0]) {
  if (template.expression) {
    step.conditionExpression = template.expression
  } else {
    step.conditionExpression = ''
  }
}

function formatSlaDisplay(hours: number | null): string {
  if (!hours) return locale.value === 'ar' ? 'غير محدد' : 'Not set'
  if (hours < 24) return locale.value === 'ar' ? `${hours} ساعة` : `${hours} hours`
  const days = Math.floor(hours / 24)
  const remainingHours = hours % 24
  if (remainingHours === 0) return locale.value === 'ar' ? `${days} يوم` : `${days} days`
  return locale.value === 'ar' ? `${days} يوم و ${remainingHours} ساعة` : `${days} days and ${remainingHours} hours`
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadWorkflow() {
  if (!workflowId.value) return
  isLoading.value = true
  try {
    const wf: WorkflowDefinitionDto = await getWorkflowDefinitionById(workflowId.value)
    nameAr.value = wf.nameAr
    nameEn.value = wf.nameEn
    descriptionAr.value = wf.descriptionAr || ''
    descriptionEn.value = wf.descriptionEn || ''
    transitionFrom.value = wf.transitionFrom
    transitionTo.value = wf.transitionTo
    isActive.value = wf.isActive

    steps.value = wf.steps
      .sort((a, b) => a.stepOrder - b.stepOrder)
      .map(s => ({
        id: s.id,
        stepOrder: s.stepOrder,
        requiredSystemRole: s.requiredSystemRole,
        requiredCommitteeRole: s.requiredCommitteeRole,
        stepNameAr: s.stepNameAr,
        stepNameEn: s.stepNameEn,
        slaHours: s.slaHours,
        isConditional: s.isConditional,
        conditionExpression: s.conditionExpression || '',
      }))
  } catch (err) {
    console.error('Failed to load workflow:', err)
    saveError.value = locale.value === 'ar' ? 'فشل في تحميل مسار الاعتماد' : 'Failed to load workflow'
  } finally {
    isLoading.value = false
  }
}

async function saveWorkflow() {
  // Validation
  if (!nameAr.value.trim() || !nameEn.value.trim()) {
    saveError.value = locale.value === 'ar' ? 'يرجى إدخال اسم المسار بالعربية والإنجليزية' : 'Please enter workflow name in both languages'
    return
  }
  if (steps.value.length === 0) {
    saveError.value = locale.value === 'ar' ? 'يرجى إضافة خطوة واحدة على الأقل' : 'Please add at least one step'
    return
  }
  for (const step of steps.value) {
    if (!step.stepNameAr.trim() || !step.stepNameEn.trim()) {
      saveError.value = locale.value === 'ar' ? 'يرجى إدخال اسم كل خطوة بالعربية والإنجليزية' : 'Please enter step names in both languages'
      return
    }
  }

  if (transitionFrom.value === transitionTo.value) {
    saveError.value = locale.value === 'ar' ? 'حالة البداية يجب أن تختلف عن حالة النهاية' : 'Start status must differ from end status'
    return
  }

  isSaving.value = true
  saveError.value = ''
  saveSuccess.value = ''

  try {
    const stepsPayload: CreateWorkflowStepRequest[] = steps.value.map(s => ({
      stepOrder: s.stepOrder,
      requiredSystemRole: s.requiredSystemRole,
      requiredCommitteeRole: s.requiredCommitteeRole,
      stepNameAr: s.stepNameAr,
      stepNameEn: s.stepNameEn,
      slaHours: s.slaHours,
      isConditional: s.isConditional,
      conditionExpression: s.conditionExpression || null,
    }))

    if (isEditMode.value && workflowId.value) {
      await updateWorkflowDefinition(workflowId.value, {
        nameAr: nameAr.value,
        nameEn: nameEn.value,
        descriptionAr: descriptionAr.value || null,
        descriptionEn: descriptionEn.value || null,
        isActive: isActive.value,
        steps: stepsPayload,
      })
      saveSuccess.value = locale.value === 'ar' ? 'تم تحديث مسار الاعتماد بنجاح' : 'Workflow updated successfully'
    } else {
      await createWorkflowDefinition({
        nameAr: nameAr.value,
        nameEn: nameEn.value,
        descriptionAr: descriptionAr.value || null,
        descriptionEn: descriptionEn.value || null,
        transitionFrom: transitionFrom.value,
        transitionTo: transitionTo.value,
        steps: stepsPayload,
      })
      saveSuccess.value = locale.value === 'ar' ? 'تم إنشاء مسار الاعتماد بنجاح' : 'Workflow created successfully'
    }

    setTimeout(() => {
      router.push({ name: 'WorkflowList' })
    }, 1500)
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    saveError.value = locale.value === 'ar' ? `فشل في الحفظ: ${msg}` : `Save failed: ${msg}`
  } finally {
    isSaving.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  if (isEditMode.value) {
    loadWorkflow()
  } else {
    addStep()
  }
})
</script>

<template>
  <div class="mx-auto max-w-4xl space-y-6 pb-12">
    <!-- Page Header -->
    <div class="flex items-center gap-4">
      <button
        class="rounded-lg p-2 text-secondary-500 hover:bg-secondary-100"
        @click="router.push({ name: 'WorkflowList' })"
      >
        <i class="pi" :class="locale === 'ar' ? 'pi-arrow-right' : 'pi-arrow-left'"></i>
      </button>
      <div>
        <h1 class="text-2xl font-bold text-secondary-800">
          {{ isEditMode
            ? (locale === 'ar' ? 'تعديل مسار الاعتماد' : 'Edit Workflow')
            : (locale === 'ar' ? 'إنشاء مسار اعتماد جديد' : 'Create New Workflow') }}
        </h1>
        <p class="mt-1 text-sm text-secondary-500">
          {{ locale === 'ar'
            ? 'حدد مراحل الاعتماد والمسؤولين عن كل مرحلة في مسار سير العمل'
            : 'Define approval stages and responsible parties for each stage in the workflow' }}
        </p>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-16">
      <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
    </div>

    <template v-else>
      <!-- Error/Success Banners -->
      <div v-if="saveError" class="flex items-center gap-3 rounded-lg border border-red-200 bg-red-50 p-4">
        <i class="pi pi-exclamation-triangle text-lg text-red-600"></i>
        <p class="flex-1 text-sm text-red-700">{{ saveError }}</p>
        <button class="text-xs font-medium text-red-600 hover:underline" @click="saveError = ''">
          {{ locale === 'ar' ? 'إغلاق' : 'Close' }}
        </button>
      </div>

      <div v-if="saveSuccess" class="flex items-center gap-3 rounded-lg border border-green-200 bg-green-50 p-4">
        <i class="pi pi-check-circle text-lg text-green-600"></i>
        <p class="flex-1 text-sm text-green-700">{{ saveSuccess }}</p>
      </div>

      <!-- ═══ Section 1: Basic Information ═══ -->
      <div class="rounded-xl border border-secondary-100 bg-white p-6 shadow-sm">
        <div class="mb-5 flex items-center gap-3">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10">
            <i class="pi pi-info-circle text-sm text-primary"></i>
          </div>
          <div>
            <h2 class="text-base font-bold text-secondary-800">
              {{ locale === 'ar' ? 'المعلومات الأساسية' : 'Basic Information' }}
            </h2>
            <p class="text-xs text-secondary-400">
              {{ locale === 'ar' ? 'عرّف اسم المسار ووصفه بكلتا اللغتين' : 'Define the workflow name and description in both languages' }}
            </p>
          </div>
        </div>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <!-- Name AR -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'اسم المسار (عربي)' : 'Workflow Name (Arabic)' }}
              <span class="text-red-500">*</span>
            </label>
            <input
              v-model="nameAr"
              type="text"
              dir="rtl"
              :placeholder="locale === 'ar' ? 'مثال: مسار اعتماد كراسة الشروط' : 'e.g., Booklet Approval Workflow'"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            />
          </div>

          <!-- Name EN -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'اسم المسار (إنجليزي)' : 'Workflow Name (English)' }}
              <span class="text-red-500">*</span>
            </label>
            <input
              v-model="nameEn"
              type="text"
              dir="ltr"
              placeholder="e.g., Booklet Approval Workflow"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            />
          </div>

          <!-- Description AR -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'الوصف (عربي)' : 'Description (Arabic)' }}
            </label>
            <textarea
              v-model="descriptionAr"
              dir="rtl"
              rows="2"
              :placeholder="locale === 'ar' ? 'وصف مختصر لهذا المسار والغرض منه...' : 'Brief description of this workflow...'"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            ></textarea>
          </div>

          <!-- Description EN -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'الوصف (إنجليزي)' : 'Description (English)' }}
            </label>
            <textarea
              v-model="descriptionEn"
              dir="ltr"
              rows="2"
              placeholder="Brief description of this workflow..."
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            ></textarea>
          </div>
        </div>
      </div>

      <!-- ═══ Section 2: Transition Configuration ═══ -->
      <div class="rounded-xl border border-secondary-100 bg-white p-6 shadow-sm">
        <div class="mb-5 flex items-center gap-3">
          <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-100">
            <i class="pi pi-arrow-right-arrow-left text-sm text-amber-600"></i>
          </div>
          <div>
            <h2 class="text-base font-bold text-secondary-800">
              {{ locale === 'ar' ? 'مرحلة التفعيل' : 'Activation Stage' }}
            </h2>
            <p class="text-xs text-secondary-400">
              {{ locale === 'ar'
                ? 'حدد متى يتم تفعيل هذا المسار تلقائياً أثناء دورة حياة المنافسة'
                : 'Define when this workflow is automatically triggered during the competition lifecycle' }}
            </p>
          </div>
        </div>

        <!-- Contextual explanation -->
        <div class="mb-4 rounded-lg border border-amber-100 bg-amber-50/50 p-3">
          <p class="text-xs leading-relaxed text-amber-700">
            <i class="pi pi-lightbulb me-1.5"></i>
            {{ locale === 'ar'
              ? 'عند انتقال المنافسة من المرحلة الأولى إلى المرحلة الثانية، سيتم تفعيل مسار الاعتماد هذا تلقائياً وإرسال طلبات الاعتماد للمسؤولين المحددين في الخطوات أدناه.'
              : 'When the competition transitions from the first stage to the second, this approval workflow will be automatically triggered and approval requests sent to the designated approvers.' }}
          </p>
        </div>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <!-- Transition From -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'عند الانتقال من مرحلة' : 'When transitioning from stage' }}
              <button
                class="ms-1 inline-flex h-4 w-4 items-center justify-center rounded-full bg-secondary-200 text-[9px] text-secondary-500 hover:bg-secondary-300"
                @click="toggleTooltip('transFrom')"
              >?</button>
            </label>
            <div v-if="activeTooltip === 'transFrom'" class="mb-2 rounded-lg bg-blue-50 p-2.5 text-xs text-blue-700">
              {{ locale === 'ar'
                ? 'المرحلة التي تكون فيها المنافسة حالياً قبل بدء عملية الاعتماد. مثال: إذا اخترت "قيد الإعداد"، فسيتم تفعيل المسار عندما تكون الكراسة في مرحلة الإعداد.'
                : 'The current stage of the competition before the approval process starts.' }}
            </div>
            <select
              v-model="transitionFrom"
              :disabled="isEditMode"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary disabled:opacity-50"
            >
              <optgroup
                v-for="group in competitionStatusGroups"
                :key="group.groupAr"
                :label="locale === 'ar' ? group.groupAr : group.groupEn"
              >
                <option v-for="s in group.statuses" :key="s.value" :value="s.value">
                  {{ locale === 'ar' ? s.labelAr : s.labelEn }}
                </option>
              </optgroup>
            </select>
          </div>

          <!-- Transition To -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'إلى مرحلة' : 'To stage' }}
              <button
                class="ms-1 inline-flex h-4 w-4 items-center justify-center rounded-full bg-secondary-200 text-[9px] text-secondary-500 hover:bg-secondary-300"
                @click="toggleTooltip('transTo')"
              >?</button>
            </label>
            <div v-if="activeTooltip === 'transTo'" class="mb-2 rounded-lg bg-blue-50 p-2.5 text-xs text-blue-700">
              {{ locale === 'ar'
                ? 'المرحلة التي ستنتقل إليها المنافسة بعد اكتمال جميع خطوات الاعتماد بنجاح. مثال: إذا اخترت "معتمدة"، فستصبح الكراسة معتمدة بعد موافقة جميع المعتمدين.'
                : 'The stage the competition will transition to after all approval steps are completed.' }}
            </div>
            <select
              v-model="transitionTo"
              :disabled="isEditMode"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary disabled:opacity-50"
            >
              <optgroup
                v-for="group in competitionStatusGroups"
                :key="group.groupAr"
                :label="locale === 'ar' ? group.groupAr : group.groupEn"
              >
                <option v-for="s in group.statuses" :key="s.value" :value="s.value">
                  {{ locale === 'ar' ? s.labelAr : s.labelEn }}
                </option>
              </optgroup>
            </select>
          </div>
        </div>

        <!-- Visual Transition Preview -->
        <div v-if="transitionDescription" class="mt-4 flex items-center justify-center gap-3 rounded-lg bg-secondary-50 p-3">
          <span class="rounded-lg bg-amber-100 px-3 py-1.5 text-xs font-semibold text-amber-700">
            {{ getStatusLabel(transitionFrom) }}
          </span>
          <div class="flex items-center gap-1">
            <div class="h-0.5 w-8 bg-secondary-300"></div>
            <i class="pi text-xs text-secondary-400" :class="locale === 'ar' ? 'pi-arrow-left' : 'pi-arrow-right'"></i>
          </div>
          <span class="rounded-lg bg-green-100 px-3 py-1.5 text-xs font-semibold text-green-700">
            {{ getStatusLabel(transitionTo) }}
          </span>
        </div>
      </div>

      <!-- ═══ Section 3: Approval Steps ═══ -->
      <div class="rounded-xl border border-secondary-100 bg-white p-6 shadow-sm">
        <div class="mb-5 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-100">
              <i class="pi pi-list text-sm text-blue-600"></i>
            </div>
            <div>
              <h2 class="text-base font-bold text-secondary-800">
                {{ locale === 'ar' ? 'خطوات الاعتماد' : 'Approval Steps' }}
                <span class="ms-2 text-xs font-normal text-secondary-400">
                  ({{ steps.length }} {{ locale === 'ar' ? 'خطوة' : 'steps' }})
                </span>
              </h2>
              <p class="text-xs text-secondary-400">
                {{ locale === 'ar'
                  ? 'حدد من يجب أن يعتمد في كل مرحلة والمدة الزمنية المسموحة'
                  : 'Define who must approve at each stage and the allowed timeframe' }}
              </p>
            </div>
          </div>
          <button
            class="flex items-center gap-1.5 rounded-lg border border-primary/30 bg-primary/5 px-3 py-1.5 text-xs font-medium text-primary transition-all hover:bg-primary/10"
            @click="addStep"
          >
            <i class="pi pi-plus text-[10px]"></i>
            {{ locale === 'ar' ? 'إضافة خطوة' : 'Add Step' }}
          </button>
        </div>

        <!-- Contextual Help -->
        <div class="mb-4 rounded-lg border border-blue-100 bg-blue-50/50 p-3">
          <p class="text-xs leading-relaxed text-blue-700">
            <i class="pi pi-info-circle me-1.5"></i>
            {{ locale === 'ar'
              ? 'الخطوات تُنفذ بالترتيب من الأولى إلى الأخيرة. كل خطوة تتطلب اعتماد الشخص المحدد قبل الانتقال للخطوة التالية. يمكنك تحديد المسؤول عن الاعتماد من خلال دوره في النظام أو عضويته في لجنة معينة.'
              : 'Steps are executed sequentially. Each step requires the designated approver before proceeding. You can specify the approver by their system role or committee membership.' }}
          </p>
        </div>

        <!-- Visual Timeline Preview -->
        <div v-if="steps.length > 0" class="mb-6 flex items-center gap-1 overflow-x-auto rounded-lg bg-secondary-50 p-3">
          <div class="flex shrink-0 items-center gap-1.5 rounded-lg bg-amber-50 px-3 py-1.5 shadow-sm">
            <span class="text-[10px] font-medium text-amber-600">{{ getStatusLabel(transitionFrom) }}</span>
          </div>
          <i class="pi text-[10px] text-secondary-300" :class="locale === 'ar' ? 'pi-arrow-left' : 'pi-arrow-right'"></i>
          <div
            v-for="(step, idx) in steps"
            :key="idx"
            class="flex items-center gap-1"
          >
            <div class="flex shrink-0 items-center gap-1.5 rounded-lg bg-white px-3 py-1.5 shadow-sm">
              <span class="flex h-5 w-5 items-center justify-center rounded-full bg-primary/10 text-[10px] font-bold text-primary">
                {{ step.stepOrder }}
              </span>
              <span class="max-w-[120px] truncate text-[10px] font-medium text-secondary-700">
                {{ step.stepNameAr || (locale === 'ar' ? 'خطوة جديدة' : 'New Step') }}
              </span>
            </div>
            <i v-if="idx < steps.length - 1" class="pi text-[10px] text-secondary-300" :class="locale === 'ar' ? 'pi-arrow-left' : 'pi-arrow-right'"></i>
          </div>
          <i class="pi text-[10px] text-secondary-300" :class="locale === 'ar' ? 'pi-arrow-left' : 'pi-arrow-right'"></i>
          <div class="flex shrink-0 items-center gap-1.5 rounded-lg bg-green-50 px-3 py-1.5 shadow-sm">
            <span class="text-[10px] font-medium text-green-600">{{ getStatusLabel(transitionTo) }}</span>
          </div>
        </div>

        <!-- Steps List -->
        <div class="space-y-4">
          <div
            v-for="(step, index) in steps"
            :key="index"
            class="rounded-xl border border-secondary-100 bg-secondary-50/30 p-5 transition-all hover:border-primary/20"
          >
            <!-- Step Header -->
            <div class="mb-4 flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-sm font-bold text-primary">
                  {{ step.stepOrder }}
                </span>
                <div>
                  <span class="text-sm font-semibold text-secondary-700">
                    {{ locale === 'ar' ? 'الخطوة' : 'Step' }} {{ step.stepOrder }}
                  </span>
                  <span v-if="step.stepNameAr" class="ms-2 text-xs text-secondary-400">
                    — {{ step.stepNameAr }}
                  </span>
                </div>
              </div>
              <div class="flex items-center gap-1">
                <button
                  v-if="index > 0"
                  class="rounded p-1.5 text-secondary-400 hover:bg-secondary-100 hover:text-secondary-600"
                  :title="locale === 'ar' ? 'نقل لأعلى' : 'Move Up'"
                  @click="moveStepUp(index)"
                >
                  <i class="pi pi-arrow-up text-xs"></i>
                </button>
                <button
                  v-if="index < steps.length - 1"
                  class="rounded p-1.5 text-secondary-400 hover:bg-secondary-100 hover:text-secondary-600"
                  :title="locale === 'ar' ? 'نقل لأسفل' : 'Move Down'"
                  @click="moveStepDown(index)"
                >
                  <i class="pi pi-arrow-down text-xs"></i>
                </button>
                <button
                  class="rounded p-1.5 text-red-400 hover:bg-red-50 hover:text-red-600"
                  :title="locale === 'ar' ? 'حذف الخطوة' : 'Remove Step'"
                  @click="removeStep(index)"
                >
                  <i class="pi pi-trash text-xs"></i>
                </button>
              </div>
            </div>

            <!-- Step Name Fields -->
            <div class="mb-4 grid grid-cols-1 gap-3 md:grid-cols-2">
              <div>
                <label class="mb-1 block text-[11px] font-semibold text-secondary-500">
                  {{ locale === 'ar' ? 'اسم الخطوة (عربي)' : 'Step Name (Arabic)' }}
                  <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="step.stepNameAr"
                  type="text"
                  dir="rtl"
                  :placeholder="locale === 'ar' ? 'مثال: مراجعة رئيس لجنة الإعداد' : 'e.g., Preparation Chair Review'"
                  class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                />
              </div>
              <div>
                <label class="mb-1 block text-[11px] font-semibold text-secondary-500">
                  {{ locale === 'ar' ? 'اسم الخطوة (إنجليزي)' : 'Step Name (English)' }}
                  <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="step.stepNameEn"
                  type="text"
                  dir="ltr"
                  placeholder="e.g., Preparation Chair Review"
                  class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                />
              </div>
            </div>

            <!-- Approver Configuration -->
            <div class="mb-4 rounded-lg border border-secondary-100 bg-white p-4">
              <h4 class="mb-3 flex items-center gap-2 text-xs font-bold text-secondary-600">
                <i class="pi pi-user text-[10px] text-primary"></i>
                {{ locale === 'ar' ? 'المسؤول عن الاعتماد' : 'Approval Authority' }}
              </h4>

              <div class="grid grid-cols-1 gap-3 md:grid-cols-2">
                <!-- Required System Role -->
                <div>
                  <label class="mb-1 block text-[11px] font-semibold text-secondary-500">
                    {{ locale === 'ar' ? 'الدور الوظيفي في النظام' : 'System Role' }}
                    <button
                      class="ms-1 inline-flex h-3.5 w-3.5 items-center justify-center rounded-full bg-secondary-200 text-[8px] text-secondary-500 hover:bg-secondary-300"
                      @click="toggleTooltip(`sysRole-${index}`)"
                    >?</button>
                  </label>
                  <div v-if="activeTooltip === `sysRole-${index}`" class="mb-2 rounded bg-blue-50 p-2 text-[10px] text-blue-700">
                    {{ locale === 'ar'
                      ? 'الدور الوظيفي للمستخدم في النظام. مثلاً: "صاحب الصلاحية" هو المسؤول الأعلى، و"المراقب المالي" مسؤول عن المراجعة المالية.'
                      : 'The user\'s functional role in the system.' }}
                  </div>
                  <select
                    v-model="step.requiredSystemRole"
                    class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                  >
                    <option v-for="r in systemRoles" :key="r.value" :value="r.value">
                      {{ locale === 'ar' ? r.labelAr : r.labelEn }}
                    </option>
                  </select>
                  <p class="mt-1 text-[10px] text-secondary-400">
                    {{ systemRoles.find(r => r.value === step.requiredSystemRole)?.[locale === 'ar' ? 'descAr' : 'descEn'] }}
                  </p>
                </div>

                <!-- Required Committee Role -->
                <div>
                  <label class="mb-1 block text-[11px] font-semibold text-secondary-500">
                    {{ locale === 'ar' ? 'العضوية في اللجنة (اختياري)' : 'Committee Membership (Optional)' }}
                    <button
                      class="ms-1 inline-flex h-3.5 w-3.5 items-center justify-center rounded-full bg-secondary-200 text-[8px] text-secondary-500 hover:bg-secondary-300"
                      @click="toggleTooltip(`comRole-${index}`)"
                    >?</button>
                  </label>
                  <div v-if="activeTooltip === `comRole-${index}`" class="mb-2 rounded bg-blue-50 p-2 text-[10px] text-blue-700">
                    {{ locale === 'ar'
                      ? 'إذا كانت هذه الخطوة تتطلب أن يكون المعتمد عضواً في لجنة معينة، اختر اللجنة هنا. إذا لم يكن ذلك مطلوباً، اختر "غير مطلوب".'
                      : 'If this step requires the approver to be a member of a specific committee, select it here.' }}
                  </div>
                  <select
                    v-model="step.requiredCommitteeRole"
                    class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                  >
                    <option v-for="r in committeeRoles" :key="r.value" :value="r.value">
                      {{ locale === 'ar' ? r.labelAr : r.labelEn }}
                    </option>
                  </select>
                  <p class="mt-1 text-[10px] text-secondary-400">
                    {{ committeeRoles.find(r => r.value === step.requiredCommitteeRole)?.[locale === 'ar' ? 'descAr' : 'descEn'] }}
                  </p>
                </div>
              </div>

              <!-- Summary of who approves -->
              <div class="mt-3 rounded-lg bg-primary/5 p-2.5">
                <p class="text-[11px] text-primary">
                  <i class="pi pi-check-circle me-1 text-[10px]"></i>
                  {{ locale === 'ar' ? 'المعتمد المطلوب: ' : 'Required approver: ' }}
                  <strong>{{ getSystemRoleLabel(step.requiredSystemRole) }}</strong>
                  <template v-if="step.requiredCommitteeRole > 0">
                    {{ locale === 'ar' ? ' + عضو في ' : ' + member of ' }}
                    <strong>{{ getCommitteeRoleLabel(step.requiredCommitteeRole) }}</strong>
                  </template>
                </p>
              </div>
            </div>

            <!-- SLA and Conditions Row -->
            <div class="grid grid-cols-1 gap-3 md:grid-cols-2">
              <!-- SLA Hours -->
              <div class="rounded-lg border border-secondary-100 bg-white p-3">
                <label class="mb-1 flex items-center gap-2 text-[11px] font-semibold text-secondary-500">
                  <i class="pi pi-clock text-[10px] text-amber-500"></i>
                  {{ locale === 'ar' ? 'المهلة الزمنية للإنجاز' : 'Time Limit (SLA)' }}
                </label>
                <div class="flex items-center gap-2">
                  <input
                    v-model.number="step.slaHours"
                    type="number"
                    min="1"
                    max="720"
                    class="w-24 rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                  />
                  <span class="text-xs text-secondary-500">{{ locale === 'ar' ? 'ساعة' : 'hours' }}</span>
                </div>
                <p class="mt-1 text-[10px] text-secondary-400">
                  {{ locale === 'ar' ? 'يعادل: ' : 'Equals: ' }}{{ formatSlaDisplay(step.slaHours) }}
                </p>
              </div>

              <!-- Conditional Step -->
              <div class="rounded-lg border border-secondary-100 bg-white p-3">
                <label class="mb-2 flex items-center gap-2 text-[11px] font-semibold text-secondary-500">
                  <i class="pi pi-filter text-[10px] text-purple-500"></i>
                  {{ locale === 'ar' ? 'شرط تفعيل الخطوة (متقدم)' : 'Step Activation Condition (Advanced)' }}
                </label>
                <label class="flex items-center gap-2 rounded-lg border border-secondary-200 bg-secondary-50/50 px-3 py-2 text-sm">
                  <input
                    v-model="step.isConditional"
                    type="checkbox"
                    class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
                  />
                  <span class="text-secondary-600">
                    {{ locale === 'ar' ? 'تفعيل فقط عند تحقق شرط معين' : 'Activate only when condition is met' }}
                  </span>
                </label>
                <p class="mt-1 text-[10px] text-secondary-400">
                  {{ locale === 'ar'
                    ? 'إذا مفعّل، هذه الخطوة ستُنفذ فقط عند تحقق الشرط المحدد'
                    : 'If enabled, this step only executes when the specified condition is met' }}
                </p>
              </div>
            </div>

            <!-- Condition Expression (if conditional) -->
            <div v-if="step.isConditional" class="mt-3 rounded-lg border border-purple-100 bg-purple-50/30 p-4">
              <label class="mb-2 block text-[11px] font-semibold text-purple-700">
                <i class="pi pi-filter me-1 text-[10px]"></i>
                {{ locale === 'ar' ? 'اختر نوع الشرط' : 'Select Condition Type' }}
              </label>

              <!-- Condition Templates -->
              <div class="mb-3 flex flex-wrap gap-2">
                <button
                  v-for="(tmpl, tIdx) in conditionTemplates"
                  :key="tIdx"
                  class="rounded-lg border px-2.5 py-1 text-[10px] font-medium transition-all"
                  :class="step.conditionExpression === tmpl.expression
                    ? 'border-purple-300 bg-purple-100 text-purple-700'
                    : 'border-secondary-200 bg-white text-secondary-600 hover:border-purple-200 hover:bg-purple-50'"
                  @click="applyConditionTemplate(step, tmpl)"
                >
                  {{ locale === 'ar' ? tmpl.labelAr : tmpl.labelEn }}
                </button>
              </div>

              <input
                v-model="step.conditionExpression"
                type="text"
                dir="ltr"
                :placeholder="locale === 'ar' ? 'التعبير الشرطي (مثال: EstimatedValue > 1000000)' : 'Condition expression (e.g., EstimatedValue > 1000000)'"
                class="w-full rounded-lg border border-purple-200 bg-white px-3 py-2 text-sm outline-none focus:border-purple-400 focus:ring-1 focus:ring-purple-400"
              />
              <p class="mt-1.5 text-[10px] text-purple-600">
                <i class="pi pi-info-circle me-1"></i>
                {{ locale === 'ar'
                  ? 'هذا الشرط يُقيّم تلقائياً عند بدء المسار لتحديد ما إذا كانت هذه الخطوة مطلوبة أم يتم تخطيها'
                  : 'This condition is automatically evaluated when the workflow starts to determine if this step is required or skipped' }}
              </p>
            </div>
          </div>
        </div>

        <!-- Empty Steps -->
        <div v-if="steps.length === 0" class="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-secondary-200 py-12">
          <i class="pi pi-list text-3xl text-secondary-300"></i>
          <p class="mt-3 text-sm text-secondary-500">
            {{ locale === 'ar' ? 'لا توجد خطوات بعد. أضف خطوة للبدء في تصميم مسار الاعتماد.' : 'No steps yet. Add a step to begin designing the approval workflow.' }}
          </p>
          <button
            class="mt-4 flex items-center gap-1.5 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white"
            @click="addStep"
          >
            <i class="pi pi-plus text-xs"></i>
            {{ locale === 'ar' ? 'إضافة خطوة' : 'Add Step' }}
          </button>
        </div>
      </div>

      <!-- ═══ Action Buttons ═══ -->
      <div class="flex items-center justify-between rounded-xl border border-secondary-100 bg-white p-4 shadow-sm">
        <button
          class="rounded-lg border border-secondary-200 px-4 py-2.5 text-sm font-medium text-secondary-600 transition-all hover:bg-secondary-50"
          @click="router.push({ name: 'WorkflowList' })"
        >
          {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
        </button>
        <div class="flex items-center gap-3">
          <!-- Active Toggle -->
          <label class="flex items-center gap-2 rounded-lg border border-secondary-200 px-3 py-2 text-sm text-secondary-600">
            <input
              v-model="isActive"
              type="checkbox"
              class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
            />
            {{ locale === 'ar' ? 'تفعيل المسار' : 'Activate Workflow' }}
          </label>

          <!-- Save Button -->
          <button
            class="flex items-center gap-2 rounded-xl bg-primary px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 disabled:opacity-50"
            :disabled="isSaving"
            @click="saveWorkflow"
          >
            <i v-if="isSaving" class="pi pi-spin pi-spinner text-xs"></i>
            <i v-else class="pi pi-save text-xs"></i>
            {{ isSaving
              ? (locale === 'ar' ? 'جاري الحفظ...' : 'Saving...')
              : (locale === 'ar' ? 'حفظ المسار' : 'Save Workflow') }}
          </button>
        </div>
      </div>

      <!-- Edit Mode Notice -->
      <div v-if="isEditMode" class="rounded-lg border border-blue-200 bg-blue-50 p-3">
        <p class="text-xs text-blue-700">
          <i class="pi pi-info-circle me-1.5"></i>
          {{ locale === 'ar'
            ? 'ملاحظة: في وضع التعديل، يمكنك تحديث جميع بيانات المسار بما في ذلك الخطوات. مراحل الانتقال (من/إلى) لا يمكن تغييرها بعد الإنشاء.'
            : 'Note: In edit mode, you can update all workflow data including steps. Transition stages (from/to) cannot be changed after creation.' }}
        </p>
      </div>
    </template>
  </div>
</template>
