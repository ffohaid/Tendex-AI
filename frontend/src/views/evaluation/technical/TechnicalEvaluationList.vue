<script setup lang="ts">
/**
 * TechnicalEvaluationList - Lists competitions available for technical evaluation.
 * Shows status, progress, and quick actions for each competition.
 * Includes ability to start technical evaluation for competitions with offers.
 */
import { onMounted, computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { startTechnicalEvaluation, fetchSupplierOffers } from '@/services/evaluationApi'
const { locale } = useI18n()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const searchQuery = ref('')
const statusFilter = ref<string>('all')
const startingEvalId = ref<string | null>(null)
const startError = ref('')

onMounted(() => {
  store.loadCompetitions()
})

const filteredCompetitions = computed(() => {
  let result = store.competitions

  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(
      c =>
        c.competitionName.toLowerCase().includes(q) ||
        c.projectName.toLowerCase().includes(q) ||
        c.competitionNumber.includes(q)
    )
  }

  if (statusFilter.value !== 'all') {
    result = result.filter(c => c.technicalStatus === statusFilter.value)
  }

  return result
})

const stats = computed(() => {
  const all = store.competitions
  return {
    total: all.length,
    pending: all.filter(c => c.technicalStatus === 'pending').length,
    inProgress: all.filter(c => c.technicalStatus === 'in_progress').length,
    completed: all.filter(c => c.technicalStatus === 'completed' || c.technicalStatus === 'approved').length,
  }
})

function openEvaluation(id: string) {
  router.push({ name: 'TechnicalEvaluationDetail', params: { id } })
}

function openOffers(id: string) {
  router.push({ name: 'SupplierOffersDetail', params: { competitionId: id } })
}

async function handleStartEvaluation(competitionId: string, event: Event) {
  event.stopPropagation()
  startingEvalId.value = competitionId
  startError.value = ''
  try {
    // Check if there are supplier offers first
    const offers = await fetchSupplierOffers(competitionId)
    if (!offers || offers.length < 2) {
      startError.value = isRtl.value
        ? 'يجب تسجيل عرضين على الأقل قبل بدء التقييم الفني'
        : 'At least 2 supplier offers are required to start technical evaluation'
      return
    }
    // Start the evaluation (using empty committeeId for now - backend will handle)
    await startTechnicalEvaluation(competitionId, '')
    // Navigate to evaluation detail
    router.push({ name: 'TechnicalEvaluationDetail', params: { id: competitionId } })
  } catch (e: any) {
    const rawError = e?.response?.data?.detail || e?.message || ''
    // Translate common backend error messages to Arabic when in RTL mode
    if (isRtl.value) {
      const errorTranslations: Record<string, string> = {
        'Competition must be in OffersClosed or TechnicalAnalysis status to start technical evaluation.': 'يجب أن تكون المنافسة في حالة "إغلاق العروض" أو "التحليل الفني" لبدء التقييم الفني.',
        'At least 2 supplier offers are required': 'يجب تسجيل عرضين على الأقل قبل بدء التقييم الفني',
        'Technical evaluation already exists': 'التقييم الفني موجود بالفعل لهذه المنافسة',
        'Financial evaluation already exists': 'التقييم المالي موجود بالفعل لهذه المنافسة',
      }
      startError.value = Object.entries(errorTranslations).find(([key]) => rawError.includes(key))?.[1] || rawError || 'حدث خطأ أثناء بدء التقييم'
    } else {
      startError.value = rawError || 'An error occurred while starting the evaluation'
    }
  } finally {
    startingEvalId.value = null
  }
}

function getStatusLabel(status: string): string {
  const labels: Record<string, string> = isRtl.value ? {
    pending: 'بانتظار البدء',
    in_progress: 'قيد التقييم',
    completed: 'مكتمل',
    approved: 'معتمد',
  } : {
    pending: 'Pending',
    in_progress: 'In Progress',
    completed: 'Completed',
    approved: 'Approved',
  }
  return labels[status] || status
}

function getStatusClass(status: string): string {
  const classes: Record<string, string> = {
    pending: 'bg-gray-100 text-gray-800',
    in_progress: 'bg-yellow-100 text-yellow-800',
    completed: 'bg-green-100 text-green-800',
    approved: 'bg-blue-100 text-blue-800',
  }
  return classes[status] || 'bg-gray-100 text-gray-800'
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Page header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">
          <i class="pi pi-check-square me-2 text-blue-600" />
          {{ isRtl ? 'التقييم الفني' : 'Technical Evaluation' }}
        </h1>
        <p class="mt-1 text-sm text-gray-500">
          {{ isRtl ? 'فحص وتقييم العروض الفنية المقدمة للمنافسات' : 'Inspect and evaluate technical offers for competitions' }}
        </p>
      </div>
      <button
        @click="router.push({ name: 'SupplierOffers' })"
        class="rounded-lg border border-blue-200 bg-blue-50 px-4 py-2 text-sm font-medium text-blue-700 hover:bg-blue-100"
      >
        <i class="pi pi-box me-2" />
        {{ isRtl ? 'إدارة عروض الموردين' : 'Manage Supplier Offers' }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-xs text-gray-500">{{ isRtl ? 'إجمالي المنافسات' : 'Total' }}</div>
        <div class="mt-1 text-2xl font-bold text-gray-900">{{ stats.total }}</div>
      </div>
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-xs text-gray-500">{{ isRtl ? 'بانتظار البدء' : 'Pending' }}</div>
        <div class="mt-1 text-2xl font-bold text-gray-500">{{ stats.pending }}</div>
      </div>
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-xs text-gray-500">{{ isRtl ? 'قيد التقييم' : 'In Progress' }}</div>
        <div class="mt-1 text-2xl font-bold text-yellow-600">{{ stats.inProgress }}</div>
      </div>
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-xs text-gray-500">{{ isRtl ? 'مكتمل' : 'Completed' }}</div>
        <div class="mt-1 text-2xl font-bold text-green-600">{{ stats.completed }}</div>
      </div>
    </div>

    <!-- Error from start evaluation -->
    <div v-if="startError" class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
      {{ startError }}
      <button @click="startError = ''" class="ms-2 font-medium underline">{{ isRtl ? 'إغلاق' : 'Close' }}</button>
    </div>

    <!-- Filters -->
    <div class="rounded-lg border border-gray-200 bg-white p-4">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div class="relative flex-1">
          <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-gray-400" />
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="isRtl ? 'البحث في المنافسات...' : 'Search competitions...'"
            class="w-full rounded-lg border border-gray-300 bg-white py-2 pe-4 ps-10 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
        <select
          v-model="statusFilter"
          class="rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
        >
          <option value="all">{{ isRtl ? 'جميع الحالات' : 'All Statuses' }}</option>
          <option value="pending">{{ isRtl ? 'بانتظار البدء' : 'Pending' }}</option>
          <option value="in_progress">{{ isRtl ? 'قيد التقييم' : 'In Progress' }}</option>
          <option value="completed">{{ isRtl ? 'مكتمل' : 'Completed' }}</option>
          <option value="approved">{{ isRtl ? 'معتمد' : 'Approved' }}</option>
        </select>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-blue-600" />
      <span class="ms-3 text-sm text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Error / Empty state when API unavailable -->
    <div v-else-if="store.error" class="rounded-lg border border-gray-200 bg-white py-12 text-center">
      <i class="pi pi-inbox text-5xl text-gray-300" />
      <h3 class="mt-4 text-lg font-semibold text-gray-500">{{ isRtl ? 'لا توجد منافسات' : 'No Competitions' }}</h3>
      <p class="mt-2 text-sm text-gray-400">{{ store.error }}</p>
      <button
        class="mt-4 rounded-lg bg-blue-600 px-5 py-2 text-sm font-medium text-white hover:bg-blue-700"
        @click="store.loadCompetitions()"
      >
        <i class="pi pi-refresh me-2 text-xs" />
        {{ isRtl ? 'إعادة المحاولة' : 'Retry' }}
      </button>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="filteredCompetitions.length === 0"
      class="rounded-lg border border-gray-200 bg-white py-12 text-center"
    >
      <i class="pi pi-inbox text-4xl text-gray-300" />
      <p class="mt-3 text-sm text-gray-500">{{ isRtl ? 'لا توجد منافسات تطابق البحث' : 'No competitions match your search' }}</p>
    </div>

    <!-- Competition cards -->
    <div v-else class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div
        v-for="comp in filteredCompetitions"
        :key="comp.id"
        class="cursor-pointer rounded-lg border border-gray-200 bg-white p-5 shadow-sm transition-all hover:border-blue-300 hover:shadow-md"
        @click="openEvaluation(comp.id)"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="mb-2 flex items-center gap-2">
              <h3 class="text-base font-bold text-gray-900">{{ comp.projectName }}</h3>
              <span
                :class="getStatusClass(comp.technicalStatus)"
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
              >
                {{ getStatusLabel(comp.technicalStatus) }}
              </span>
            </div>
            <p class="text-xs text-gray-500">
              {{ isRtl ? 'رقم المنافسة' : 'Competition #' }}: {{ comp.competitionNumber }}
            </p>
          </div>
        </div>

        <div class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'عدد العروض' : 'Offers' }}</span>
            <p class="text-sm font-semibold text-gray-900">{{ comp.vendorCount }}</p>
          </div>
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'ناجح فنياً' : 'Passed' }}</span>
            <p class="text-sm font-semibold text-green-600">{{ comp.passedVendorCount }}</p>
          </div>
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'الميزانية' : 'Budget' }}</span>
            <p class="text-sm font-medium text-gray-900">{{ comp.estimatedBudget?.toLocaleString() }} {{ isRtl ? '﷼' : 'SAR' }}</p>
          </div>
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'اللجنة' : 'Committee' }}</span>
            <p class="text-sm font-medium text-gray-700">{{ comp.assignedCommittee }}</p>
          </div>
        </div>

        <!-- Progress bar -->
        <div class="mt-4">
          <div class="mb-1 flex items-center justify-between">
            <span class="text-xs text-gray-400">{{ isRtl ? 'نسبة الإنجاز' : 'Progress' }}</span>
            <span class="text-xs font-semibold text-blue-600">{{ comp.progress }}%</span>
          </div>
          <div class="h-1.5 w-full overflow-hidden rounded-full bg-gray-100">
            <div
              class="h-full rounded-full bg-blue-600 transition-all duration-500"
              :style="{ width: `${comp.progress}%` }"
            />
          </div>
        </div>

        <!-- Action buttons -->
        <div class="mt-4 flex gap-2 border-t border-gray-100 pt-3">
          <button
            v-if="comp.technicalStatus === 'pending'"
            @click="handleStartEvaluation(comp.id, $event)"
            :disabled="startingEvalId === comp.id"
            class="flex-1 rounded-lg bg-blue-600 px-3 py-2 text-xs font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            <span v-if="startingEvalId === comp.id">
              <i class="pi pi-spinner pi-spin me-1" />
              {{ isRtl ? 'جارٍ البدء...' : 'Starting...' }}
            </span>
            <span v-else>
              <i class="pi pi-play me-1" />
              {{ isRtl ? 'بدء التقييم الفني' : 'Start Technical Evaluation' }}
            </span>
          </button>
          <button
            v-else
            @click.stop="openEvaluation(comp.id)"
            class="flex-1 rounded-lg bg-blue-600 px-3 py-2 text-xs font-medium text-white hover:bg-blue-700"
          >
            <i class="pi pi-eye me-1" />
            {{ isRtl ? 'عرض التقييم' : 'View Evaluation' }}
          </button>
          <button
            @click.stop="openOffers(comp.id)"
            class="rounded-lg border border-gray-300 px-3 py-2 text-xs font-medium text-gray-700 hover:bg-gray-50"
          >
            <i class="pi pi-box me-1" />
            {{ isRtl ? 'العروض' : 'Offers' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
