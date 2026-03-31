<script setup lang="ts">
/**
 * WorkflowListView — Approval Workflow Definitions Management
 *
 * Displays all configured approval workflow definitions for the current tenant.
 * Connects to the new /api/v1/workflow-definitions backend.
 *
 * Features:
 * - List workflow definitions with status badges
 * - View steps count and transition info
 * - Create / Edit / Activate / Deactivate workflows
 * - Seed default workflows
 * - Search and filter by status
 * - RTL/LTR support with Tailwind logical properties
 */
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import {
  getWorkflowDefinitions,
  seedDefaultWorkflows,
  updateWorkflowDefinition,
  type WorkflowDefinitionDto,
} from '@/services/workflowService'

const { t, locale } = useI18n()
const router = useRouter()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const isLoading = ref(false)
const workflows = ref<WorkflowDefinitionDto[]>([])
const searchQuery = ref('')
const statusFilter = ref<string>('all')
const isSeeding = ref(false)
const seedError = ref('')
const seedSuccess = ref('')

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const filteredWorkflows = computed(() => {
  let result = workflows.value
  if (statusFilter.value !== 'all') {
    const isActive = statusFilter.value === 'active'
    result = result.filter(w => w.isActive === isActive)
  }
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(
      w =>
        w.nameAr.toLowerCase().includes(q) ||
        w.nameEn.toLowerCase().includes(q),
    )
  }
  return result
})


/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
const statusMap: Record<number, { ar: string; en: string }> = {
  0: { ar: 'مسودة', en: 'Draft' },
  1: { ar: 'قيد الإعداد', en: 'Under Preparation' },
  2: { ar: 'بانتظار الاعتماد', en: 'Pending Approval' },
  3: { ar: 'معتمدة', en: 'Approved' },
  4: { ar: 'منشورة', en: 'Published' },
  5: { ar: 'فترة الاستفسارات', en: 'Inquiry Period' },
  6: { ar: 'استقبال العروض', en: 'Receiving Offers' },
  7: { ar: 'إغلاق العروض', en: 'Offers Closed' },
  8: { ar: 'التحليل الفني', en: 'Technical Analysis' },
  9: { ar: 'اكتمال التحليل الفني', en: 'Technical Analysis Completed' },
  10: { ar: 'التحليل المالي', en: 'Financial Analysis' },
  11: { ar: 'اكتمال التحليل المالي', en: 'Financial Analysis Completed' },
  12: { ar: 'إشعار الترسية', en: 'Award Notification' },
  13: { ar: 'اعتماد الترسية', en: 'Award Approved' },
  14: { ar: 'إجازة العقد', en: 'Contract Approval' },
  15: { ar: 'اعتماد العقد', en: 'Contract Approved' },
  16: { ar: 'توقيع العقد', en: 'Contract Signed' },
  90: { ar: 'مرفوضة', en: 'Rejected' },
  91: { ar: 'ملغاة', en: 'Cancelled' },
  92: { ar: 'معلقة', en: 'Suspended' },
}

function getTransitionLabel(from: number, to: number): string {
  const fromLabel = statusMap[from]
  const toLabel = statusMap[to]
  if (!fromLabel || !toLabel) return `${from} → ${to}`
  const isAr = locale.value === 'ar'
  return `${isAr ? fromLabel.ar : fromLabel.en} → ${isAr ? toLabel.ar : toLabel.en}`
}

function getStatusBadge(isActive: boolean) {
  return isActive
    ? {
        label: locale.value === 'ar' ? 'نشط' : 'Active',
        class: 'bg-green-50 text-green-700 border-green-200',
      }
    : {
        label: locale.value === 'ar' ? 'غير نشط' : 'Inactive',
        class: 'bg-gray-50 text-gray-500 border-gray-200',
      }
}

function getWorkflowName(wf: WorkflowDefinitionDto): string {
  return locale.value === 'ar' ? wf.nameAr : wf.nameEn
}

function getWorkflowDescription(wf: WorkflowDefinitionDto): string {
  const desc = locale.value === 'ar' ? wf.descriptionAr : wf.descriptionEn
  return desc || ''
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadWorkflows(): Promise<void> {
  isLoading.value = true
  try {
    workflows.value = await getWorkflowDefinitions()
  } catch (err) {
    console.error('Failed to load workflow definitions:', err)
    workflows.value = []
  } finally {
    isLoading.value = false
  }
}

async function toggleWorkflowStatus(wf: WorkflowDefinitionDto): Promise<void> {
  try {
    await updateWorkflowDefinition(wf.id, {
      nameAr: wf.nameAr,
      nameEn: wf.nameEn,
      descriptionAr: wf.descriptionAr,
      descriptionEn: wf.descriptionEn,
      isActive: !wf.isActive,
    })
    await loadWorkflows()
  } catch (err) {
    console.error('Failed to toggle workflow status:', err)
  }
}

async function handleSeedDefaults(): Promise<void> {
  isSeeding.value = true
  seedError.value = ''
  seedSuccess.value = ''
  try {
    const result = await seedDefaultWorkflows()
    seedSuccess.value = result.message
    await loadWorkflows()
    setTimeout(() => {
      seedSuccess.value = ''
    }, 5000)
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err)
    seedError.value =
      locale.value === 'ar'
        ? `فشل في إنشاء المسارات الافتراضية: ${msg}`
        : `Failed to seed defaults: ${msg}`
  } finally {
    isSeeding.value = false
  }
}

function editWorkflow(id: string): void {
  router.push(`/workflow/designer/${id}`)
}

function createWorkflow(): void {
  router.push('/workflow/designer')
}

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  loadWorkflows()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary-800">
          {{ t('workflow.title') }}
        </h1>
        <p class="mt-1 text-sm text-secondary-500">
          {{ t('workflow.description') }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <button
          v-if="workflows.length === 0 && !isLoading"
          class="flex items-center gap-2 rounded-xl border border-primary/30 bg-primary/5 px-4 py-2.5 text-sm font-medium text-primary transition-all hover:bg-primary/10 disabled:opacity-50"
          :disabled="isSeeding"
          @click="handleSeedDefaults"
        >
          <i v-if="isSeeding" class="pi pi-spin pi-spinner text-xs"></i>
          <i v-else class="pi pi-database text-xs"></i>
          {{ locale === 'ar' ? 'إنشاء المسارات الافتراضية' : 'Seed Default Workflows' }}
        </button>
        <button
          class="flex items-center gap-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600"
          @click="createWorkflow"
        >
          <i class="pi pi-plus text-xs"></i>
          {{ t('workflow.create') }}
        </button>
      </div>
    </div>

    <!-- Success/Error Banners -->
    <div
      v-if="seedSuccess"
      class="flex items-center gap-3 rounded-lg border border-green-200 bg-green-50 p-4"
    >
      <i class="pi pi-check-circle text-lg text-green-600"></i>
      <p class="flex-1 text-sm text-green-700">{{ seedSuccess }}</p>
      <button class="text-xs font-medium text-green-600 hover:underline" @click="seedSuccess = ''">
        {{ locale === 'ar' ? 'إغلاق' : 'Close' }}
      </button>
    </div>

    <div
      v-if="seedError"
      class="flex items-center gap-3 rounded-lg border border-red-200 bg-red-50 p-4"
    >
      <i class="pi pi-exclamation-triangle text-lg text-red-600"></i>
      <p class="flex-1 text-sm text-red-700">{{ seedError }}</p>
      <button class="text-xs font-medium text-red-600 hover:underline" @click="seedError = ''">
        {{ locale === 'ar' ? 'إغلاق' : 'Close' }}
      </button>
    </div>

    <!-- Filters -->
    <div class="flex flex-col gap-4 rounded-xl border border-secondary-100 bg-white p-4 sm:flex-row sm:items-center">
      <div class="relative flex-1">
        <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-secondary-400"></i>
        <input
          v-model="searchQuery"
          type="text"
          :placeholder="locale === 'ar' ? 'بحث في مسارات الاعتماد...' : 'Search workflows...'"
          class="w-full rounded-lg border border-secondary-200 bg-secondary-50/50 py-2.5 ps-10 pe-4 text-sm text-secondary-700 outline-none transition-all focus:border-primary focus:ring-1 focus:ring-primary"
        />
      </div>
      <div class="flex gap-2">
        <button
          v-for="status in ['all', 'active', 'inactive']"
          :key="status"
          class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors"
          :class="
            statusFilter === status
              ? 'bg-primary text-white'
              : 'bg-secondary-50 text-secondary-600 hover:bg-secondary-100'
          "
          @click="statusFilter = status"
        >
          {{
            status === 'all'
              ? (locale === 'ar' ? 'الكل' : 'All')
              : status === 'active'
                ? (locale === 'ar' ? 'نشط' : 'Active')
                : (locale === 'ar' ? 'غير نشط' : 'Inactive')
          }}
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div v-for="i in 6" :key="i" class="h-48 animate-pulse rounded-xl border border-secondary-100 bg-secondary-50"></div>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="filteredWorkflows.length === 0"
      class="flex flex-col items-center justify-center rounded-xl border border-secondary-100 bg-white py-16"
    >
      <div class="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10">
        <i class="pi pi-sitemap text-3xl text-primary/60"></i>
      </div>
      <h3 class="mt-4 text-sm font-bold text-secondary-800">
        {{ t('workflow.empty') }}
      </h3>
      <p class="mt-1 max-w-sm text-center text-xs text-secondary-500">
        {{ t('workflow.emptyDescription') }}
      </p>
      <div class="mt-6 flex gap-3">
        <button
          class="flex items-center gap-2 rounded-xl border border-primary/30 bg-primary/5 px-4 py-2.5 text-sm font-medium text-primary transition-all hover:bg-primary/10 disabled:opacity-50"
          :disabled="isSeeding"
          @click="handleSeedDefaults"
        >
          <i v-if="isSeeding" class="pi pi-spin pi-spinner text-xs"></i>
          <i v-else class="pi pi-database text-xs"></i>
          {{ locale === 'ar' ? 'إنشاء المسارات الافتراضية' : 'Seed Default Workflows' }}
        </button>
        <button
          class="flex items-center gap-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600"
          @click="createWorkflow"
        >
          <i class="pi pi-plus text-xs"></i>
          {{ t('workflow.create') }}
        </button>
      </div>
    </div>

    <!-- Workflow Cards -->
    <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="wf in filteredWorkflows"
        :key="wf.id"
        class="group cursor-pointer rounded-xl border border-secondary-100 bg-white p-5 shadow-sm transition-all hover:border-primary/30 hover:shadow-md"
        @click="editWorkflow(wf.id)"
      >
        <!-- Header -->
        <div class="mb-3 flex items-start justify-between">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10">
            <i class="pi pi-sitemap text-lg text-primary"></i>
          </div>
          <span
            class="inline-flex items-center rounded-md border px-2 py-0.5 text-xs font-medium"
            :class="getStatusBadge(wf.isActive).class"
          >
            {{ getStatusBadge(wf.isActive).label }}
          </span>
        </div>

        <!-- Name & Description -->
        <h3 class="text-sm font-bold text-secondary-800 group-hover:text-primary">
          {{ getWorkflowName(wf) }}
        </h3>
        <p v-if="getWorkflowDescription(wf)" class="mt-1 line-clamp-2 text-xs text-secondary-500">
          {{ getWorkflowDescription(wf) }}
        </p>

        <!-- Transition Info -->
        <div class="mt-3 rounded-lg bg-secondary-50 px-3 py-2">
          <div class="flex items-center gap-2 text-xs text-secondary-600">
            <i class="pi pi-arrow-right-arrow-left text-[10px] text-primary"></i>
            <span>{{ getTransitionLabel(wf.transitionFrom, wf.transitionTo) }}</span>
          </div>
        </div>

        <!-- Footer -->
        <div class="mt-3 flex items-center justify-between border-t border-secondary-50 pt-3">
          <div class="flex items-center gap-3 text-[10px] text-secondary-400">
            <span>
              <i class="pi pi-list me-1"></i>
              {{ wf.steps.length }} {{ locale === 'ar' ? 'خطوة' : 'steps' }}
            </span>
            <span>
              <i class="pi pi-tag me-1"></i>
              v{{ wf.version }}
            </span>
          </div>

          <!-- Toggle Active -->
          <button
            class="rounded-lg px-2 py-1 text-[10px] font-medium transition-colors"
            :class="
              wf.isActive
                ? 'text-red-500 hover:bg-red-50'
                : 'text-green-600 hover:bg-green-50'
            "
            @click.stop="toggleWorkflowStatus(wf)"
          >
            {{ wf.isActive ? (locale === 'ar' ? 'تعطيل' : 'Deactivate') : (locale === 'ar' ? 'تفعيل' : 'Activate') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
