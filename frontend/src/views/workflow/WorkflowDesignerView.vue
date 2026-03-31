<script setup lang="ts">
/**
 * WorkflowDesignerView — Workflow Definition Editor (Simplified Form-Based)
 *
 * Replaces the visual canvas designer with a simple, functional form-based editor
 * for creating and editing workflow definitions. Connects to /api/v1/workflow-definitions.
 *
 * Features:
 * - Create new workflow definitions with steps
 * - Edit existing workflow definitions
 * - Add/remove/reorder steps
 * - Configure step roles, SLA, and conditions
 * - Visual step timeline preview
 * - RTL/LTR support
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

/* ------------------------------------------------------------------ */
/*  Enum Mappings                                                      */
/* ------------------------------------------------------------------ */
const competitionStatuses = [
  { value: 0, labelAr: 'مسودة', labelEn: 'Draft' },
  { value: 1, labelAr: 'قيد الإعداد', labelEn: 'Under Preparation' },
  { value: 2, labelAr: 'بانتظار الاعتماد', labelEn: 'Pending Approval' },
  { value: 3, labelAr: 'معتمدة', labelEn: 'Approved' },
  { value: 4, labelAr: 'منشورة', labelEn: 'Published' },
  { value: 5, labelAr: 'فترة الاستفسارات', labelEn: 'Inquiry Period' },
  { value: 6, labelAr: 'استقبال العروض', labelEn: 'Receiving Offers' },
  { value: 7, labelAr: 'التحليل الفني', labelEn: 'Technical Analysis' },
  { value: 8, labelAr: 'التحليل الفني مكتمل', labelEn: 'Technical Analysis Completed' },
  { value: 9, labelAr: 'التحليل المالي', labelEn: 'Financial Analysis' },
  { value: 10, labelAr: 'التحليل المالي مكتمل', labelEn: 'Financial Analysis Completed' },
  { value: 11, labelAr: 'إشعار الترسية', labelEn: 'Award Notification' },
  { value: 12, labelAr: 'الترسية معتمدة', labelEn: 'Award Approved' },
  { value: 13, labelAr: 'اعتماد العقد', labelEn: 'Contract Approval' },
  { value: 14, labelAr: 'العقد معتمد', labelEn: 'Contract Approved' },
  { value: 15, labelAr: 'العقد موقع', labelEn: 'Contract Signed' },
  { value: 99, labelAr: 'ملغاة', labelEn: 'Cancelled' },
]

const systemRoles = [
  { value: 1, labelAr: 'صاحب الصلاحية', labelEn: 'Owner' },
  { value: 2, labelAr: 'مشرف النظام', labelEn: 'Admin' },
  { value: 3, labelAr: 'ممثل القطاع', labelEn: 'Sector Rep' },
  { value: 4, labelAr: 'المراقب المالي', labelEn: 'Financial Controller' },
  { value: 5, labelAr: 'عضو', labelEn: 'Member' },
  { value: 6, labelAr: 'مشاهد', labelEn: 'Viewer' },
]

const committeeRoles = [
  { value: 0, labelAr: 'لا يوجد (دور نظام فقط)', labelEn: 'None (System role only)' },
  { value: 1, labelAr: 'رئيس لجنة الإعداد', labelEn: 'Preparation Committee Chair' },
  { value: 2, labelAr: 'عضو لجنة الإعداد', labelEn: 'Preparation Committee Member' },
  { value: 3, labelAr: 'رئيس لجنة الفحص الفني', labelEn: 'Technical Exam Committee Chair' },
  { value: 4, labelAr: 'عضو لجنة الفحص الفني', labelEn: 'Technical Exam Committee Member' },
  { value: 5, labelAr: 'رئيس لجنة الفحص المالي', labelEn: 'Financial Exam Committee Chair' },
  { value: 6, labelAr: 'عضو لجنة الفحص المالي', labelEn: 'Financial Exam Committee Member' },
  { value: 7, labelAr: 'رئيس لجنة مراجعة الاستفسارات', labelEn: 'Inquiry Review Committee Chair' },
  { value: 8, labelAr: 'عضو لجنة مراجعة الاستفسارات', labelEn: 'Inquiry Review Committee Member' },
  { value: 9, labelAr: 'سكرتير اللجنة', labelEn: 'Committee Secretary' },
]

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
  // Recalculate step orders
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

  isSaving.value = true
  saveError.value = ''
  saveSuccess.value = ''

  try {
    if (isEditMode.value && workflowId.value) {
      await updateWorkflowDefinition(workflowId.value, {
        nameAr: nameAr.value,
        nameEn: nameEn.value,
        descriptionAr: descriptionAr.value || null,
        descriptionEn: descriptionEn.value || null,
        isActive: isActive.value,
      })
      saveSuccess.value = locale.value === 'ar' ? 'تم تحديث مسار الاعتماد بنجاح' : 'Workflow updated successfully'
    } else {
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
            ? 'حدد خطوات الاعتماد والأدوار المطلوبة لكل خطوة'
            : 'Define approval steps and required roles for each step' }}
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

      <!-- Basic Info Card -->
      <div class="rounded-xl border border-secondary-100 bg-white p-6 shadow-sm">
        <h2 class="mb-4 text-base font-bold text-secondary-800">
          <i class="pi pi-info-circle me-2 text-primary"></i>
          {{ locale === 'ar' ? 'المعلومات الأساسية' : 'Basic Information' }}
        </h2>

        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <!-- Name AR -->
          <div>
            <label class="mb-1 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'اسم المسار (عربي)' : 'Workflow Name (Arabic)' }}
              <span class="text-red-500">*</span>
            </label>
            <input
              v-model="nameAr"
              type="text"
              dir="rtl"
              :placeholder="locale === 'ar' ? 'مثال: مسار اعتماد الكراسة' : 'e.g., Booklet Approval Workflow'"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            />
          </div>

          <!-- Name EN -->
          <div>
            <label class="mb-1 block text-xs font-semibold text-secondary-600">
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
            <label class="mb-1 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'الوصف (عربي)' : 'Description (Arabic)' }}
            </label>
            <textarea
              v-model="descriptionAr"
              dir="rtl"
              rows="2"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            ></textarea>
          </div>

          <!-- Description EN -->
          <div>
            <label class="mb-1 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'الوصف (إنجليزي)' : 'Description (English)' }}
            </label>
            <textarea
              v-model="descriptionEn"
              dir="ltr"
              rows="2"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary"
            ></textarea>
          </div>

          <!-- Transition From -->
          <div>
            <label class="mb-1 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'الانتقال من حالة' : 'Transition From' }}
            </label>
            <select
              v-model="transitionFrom"
              :disabled="isEditMode"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary disabled:opacity-50"
            >
              <option v-for="s in competitionStatuses" :key="s.value" :value="s.value">
                {{ locale === 'ar' ? s.labelAr : s.labelEn }}
              </option>
            </select>
          </div>

          <!-- Transition To -->
          <div>
            <label class="mb-1 block text-xs font-semibold text-secondary-600">
              {{ locale === 'ar' ? 'الانتقال إلى حالة' : 'Transition To' }}
            </label>
            <select
              v-model="transitionTo"
              :disabled="isEditMode"
              class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 px-4 py-2.5 text-sm text-secondary-700 outline-none focus:border-primary focus:ring-1 focus:ring-primary disabled:opacity-50"
            >
              <option v-for="s in competitionStatuses" :key="s.value" :value="s.value">
                {{ locale === 'ar' ? s.labelAr : s.labelEn }}
              </option>
            </select>
          </div>
        </div>
      </div>

      <!-- Steps Card -->
      <div class="rounded-xl border border-secondary-100 bg-white p-6 shadow-sm">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-base font-bold text-secondary-800">
            <i class="pi pi-list me-2 text-primary"></i>
            {{ locale === 'ar' ? 'خطوات الاعتماد' : 'Approval Steps' }}
            <span class="ms-2 text-xs font-normal text-secondary-400">
              ({{ steps.length }} {{ locale === 'ar' ? 'خطوة' : 'steps' }})
            </span>
          </h2>
          <button
            class="flex items-center gap-1.5 rounded-lg border border-primary/30 bg-primary/5 px-3 py-1.5 text-xs font-medium text-primary transition-all hover:bg-primary/10"
            @click="addStep"
          >
            <i class="pi pi-plus text-[10px]"></i>
            {{ locale === 'ar' ? 'إضافة خطوة' : 'Add Step' }}
          </button>
        </div>

        <!-- Visual Timeline Preview -->
        <div v-if="steps.length > 0" class="mb-6 flex items-center gap-1 overflow-x-auto rounded-lg bg-secondary-50 p-3">
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
        </div>

        <!-- Steps List -->
        <div class="space-y-4">
          <div
            v-for="(step, index) in steps"
            :key="index"
            class="rounded-xl border border-secondary-100 bg-secondary-50/30 p-4 transition-all hover:border-primary/20"
          >
            <!-- Step Header -->
            <div class="mb-3 flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-primary/10 text-xs font-bold text-primary">
                  {{ step.stepOrder }}
                </span>
                <span class="text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'الخطوة' : 'Step' }} {{ step.stepOrder }}
                </span>
              </div>
              <div class="flex items-center gap-1">
                <button
                  v-if="index > 0"
                  class="rounded p-1 text-secondary-400 hover:bg-secondary-100 hover:text-secondary-600"
                  :title="locale === 'ar' ? 'نقل لأعلى' : 'Move Up'"
                  @click="moveStepUp(index)"
                >
                  <i class="pi pi-arrow-up text-xs"></i>
                </button>
                <button
                  v-if="index < steps.length - 1"
                  class="rounded p-1 text-secondary-400 hover:bg-secondary-100 hover:text-secondary-600"
                  :title="locale === 'ar' ? 'نقل لأسفل' : 'Move Down'"
                  @click="moveStepDown(index)"
                >
                  <i class="pi pi-arrow-down text-xs"></i>
                </button>
                <button
                  class="rounded p-1 text-red-400 hover:bg-red-50 hover:text-red-600"
                  :title="locale === 'ar' ? 'حذف الخطوة' : 'Remove Step'"
                  @click="removeStep(index)"
                >
                  <i class="pi pi-trash text-xs"></i>
                </button>
              </div>
            </div>

            <!-- Step Fields -->
            <div class="grid grid-cols-1 gap-3 md:grid-cols-2">
              <!-- Step Name AR -->
              <div>
                <label class="mb-1 block text-[10px] font-semibold uppercase tracking-wider text-secondary-400">
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

              <!-- Step Name EN -->
              <div>
                <label class="mb-1 block text-[10px] font-semibold uppercase tracking-wider text-secondary-400">
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

              <!-- Required System Role -->
              <div>
                <label class="mb-1 block text-[10px] font-semibold uppercase tracking-wider text-secondary-400">
                  {{ locale === 'ar' ? 'دور النظام المطلوب' : 'Required System Role' }}
                </label>
                <select
                  v-model="step.requiredSystemRole"
                  class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                >
                  <option v-for="r in systemRoles" :key="r.value" :value="r.value">
                    {{ locale === 'ar' ? r.labelAr : r.labelEn }}
                  </option>
                </select>
              </div>

              <!-- Required Committee Role -->
              <div>
                <label class="mb-1 block text-[10px] font-semibold uppercase tracking-wider text-secondary-400">
                  {{ locale === 'ar' ? 'دور اللجنة المطلوب' : 'Required Committee Role' }}
                </label>
                <select
                  v-model="step.requiredCommitteeRole"
                  class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                >
                  <option v-for="r in committeeRoles" :key="r.value" :value="r.value">
                    {{ locale === 'ar' ? r.labelAr : r.labelEn }}
                  </option>
                </select>
              </div>

              <!-- SLA Hours -->
              <div>
                <label class="mb-1 block text-[10px] font-semibold uppercase tracking-wider text-secondary-400">
                  {{ locale === 'ar' ? 'مدة الإنجاز (ساعات)' : 'SLA (Hours)' }}
                </label>
                <input
                  v-model.number="step.slaHours"
                  type="number"
                  min="1"
                  max="720"
                  class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
                />
              </div>

              <!-- Conditional -->
              <div class="flex items-end gap-3">
                <label class="flex items-center gap-2 rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm">
                  <input
                    v-model="step.isConditional"
                    type="checkbox"
                    class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
                  />
                  <span class="text-secondary-600">
                    {{ locale === 'ar' ? 'خطوة مشروطة' : 'Conditional Step' }}
                  </span>
                </label>
              </div>
            </div>

            <!-- Condition Expression (if conditional) -->
            <div v-if="step.isConditional" class="mt-3">
              <label class="mb-1 block text-[10px] font-semibold uppercase tracking-wider text-secondary-400">
                {{ locale === 'ar' ? 'التعبير الشرطي' : 'Condition Expression' }}
              </label>
              <input
                v-model="step.conditionExpression"
                type="text"
                dir="ltr"
                :placeholder="locale === 'ar' ? 'مثال: EstimatedValue > 1000000' : 'e.g., EstimatedValue > 1000000'"
                class="w-full rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm outline-none focus:border-primary focus:ring-1 focus:ring-primary"
              />
              <p class="mt-1 text-[10px] text-secondary-400">
                {{ locale === 'ar'
                  ? 'يتم تقييم هذا التعبير لتحديد ما إذا كانت الخطوة مطلوبة'
                  : 'This expression is evaluated to determine if the step is required' }}
              </p>
            </div>
          </div>
        </div>

        <!-- Empty Steps -->
        <div v-if="steps.length === 0" class="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-secondary-200 py-12">
          <i class="pi pi-list text-3xl text-secondary-300"></i>
          <p class="mt-3 text-sm text-secondary-500">
            {{ locale === 'ar' ? 'لا توجد خطوات بعد. أضف خطوة للبدء.' : 'No steps yet. Add a step to begin.' }}
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

      <!-- Action Buttons -->
      <div class="flex items-center justify-between rounded-xl border border-secondary-100 bg-white p-4 shadow-sm">
        <button
          class="rounded-lg border border-secondary-200 px-4 py-2.5 text-sm font-medium text-secondary-600 transition-all hover:bg-secondary-50"
          @click="router.push({ name: 'WorkflowList' })"
        >
          {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
        </button>
        <div class="flex items-center gap-3">
          <!-- Active Toggle -->
          <label class="flex items-center gap-2 text-sm text-secondary-600">
            <input
              v-model="isActive"
              type="checkbox"
              class="h-4 w-4 rounded border-secondary-300 text-primary focus:ring-primary"
            />
            {{ locale === 'ar' ? 'نشط' : 'Active' }}
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
    </template>
  </div>
</template>
