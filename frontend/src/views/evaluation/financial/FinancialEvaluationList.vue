<script setup lang="ts">
/**
 * FinancialEvaluationList - Lists competitions for financial evaluation.
 * Only shows competitions that have completed technical evaluation.
 * Aligned with backend API endpoints.
 */
import { onMounted, computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'

const { locale } = useI18n()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const searchQuery = ref('')
const statusFilter = ref<string>('all')

onMounted(() => {
  store.loadCompetitions()
})

/**
 * Financial evaluation is only available for competitions
 * where technical evaluation has been approved.
 */
const eligibleCompetitions = computed(() => {
  let result = store.competitions.filter(
    c => c.technicalStatus === 'completed' || c.technicalStatus === 'approved'
  )

  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(
      c =>
        c.competitionName?.toLowerCase().includes(q) ||
        c.projectName?.toLowerCase().includes(q) ||
        c.competitionNumber?.includes(q)
    )
  }

  if (statusFilter.value !== 'all') {
    result = result.filter(c => c.financialStatus === statusFilter.value)
  }

  return result
})

function openEvaluation(id: string) {
  router.push({ name: 'FinancialEvaluationDetail', params: { id } })
}

function startFinancialEvaluation(id: string) {
  store.startFinancialEvaluation(id)
}

function getStatusColor(status: string): string {
  switch (status?.toLowerCase()) {
    case 'approved': return 'bg-green-100 text-green-700'
    case 'completed': return 'bg-blue-100 text-blue-700'
    case 'in_progress': return 'bg-yellow-100 text-yellow-700'
    case 'pending': return 'bg-gray-100 text-gray-700'
    default: return 'bg-gray-100 text-gray-600'
  }
}

function getStatusLabel(status: string): string {
  switch (status?.toLowerCase()) {
    case 'approved': return isRtl.value ? 'معتمد' : 'Approved'
    case 'completed': return isRtl.value ? 'مكتمل' : 'Completed'
    case 'in_progress': return isRtl.value ? 'قيد التنفيذ' : 'In Progress'
    case 'pending': return isRtl.value ? 'بانتظار البدء' : 'Pending'
    default: return status || (isRtl.value ? 'غير محدد' : 'Unknown')
  }
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Page header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">
          <i class="pi pi-wallet me-2 text-green-600" />
          {{ isRtl ? 'التقييم المالي' : 'Financial Evaluation' }}
        </h1>
        <p class="mt-1 text-sm text-gray-500">
          {{ isRtl ? 'تقييم العروض المالية للمنافسات التي اجتازت التقييم الفني' : 'Evaluate financial offers for competitions that passed technical evaluation' }}
        </p>
      </div>
    </div>

    <!-- Info banner: Financial evaluation prerequisite -->
    <div class="flex items-start gap-3 rounded-xl border border-blue-200 bg-blue-50 p-4">
      <i class="pi pi-info-circle mt-0.5 text-blue-600" />
      <p class="text-sm text-blue-800">
        {{ isRtl 
          ? 'التقييم المالي متاح فقط للمنافسات التي تم اعتماد تقييمها الفني. يتم فحص الأسعار والتحقق الحسابي وترتيب العروض.' 
          : 'Financial evaluation is only available for competitions with approved technical evaluation. Prices are examined, arithmetic is verified, and offers are ranked.' 
        }}
      </p>
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
            class="w-full rounded-lg border border-gray-300 bg-white py-2 pe-4 ps-10 text-sm focus:border-green-500 focus:outline-none focus:ring-1 focus:ring-green-500"
          />
        </div>
        <select
          v-model="statusFilter"
          class="rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm focus:border-green-500 focus:outline-none focus:ring-1 focus:ring-green-500"
        >
          <option value="all">{{ isRtl ? 'جميع الحالات' : 'All Statuses' }}</option>
          <option value="pending">{{ isRtl ? 'بانتظار البدء' : 'Pending' }}</option>
          <option value="in_progress">{{ isRtl ? 'قيد التنفيذ' : 'In Progress' }}</option>
          <option value="completed">{{ isRtl ? 'مكتمل' : 'Completed' }}</option>
          <option value="approved">{{ isRtl ? 'معتمد' : 'Approved' }}</option>
        </select>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-green-600" />
      <span class="ms-3 text-sm text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="eligibleCompetitions.length === 0"
      class="rounded-lg border border-gray-200 bg-white py-12 text-center"
    >
      <i class="pi pi-inbox text-4xl text-gray-300" />
      <p class="mt-3 text-sm text-gray-500">
        {{ isRtl ? 'لا توجد منافسات مؤهلة للتقييم المالي حالياً' : 'No competitions eligible for financial evaluation' }}
      </p>
      <p class="mt-1 text-xs text-gray-400">
        {{ isRtl ? 'يجب اعتماد التقييم الفني أولاً' : 'Technical evaluation must be approved first' }}
      </p>
    </div>

    <!-- Competition cards -->
    <div v-else class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div
        v-for="comp in eligibleCompetitions"
        :key="comp.id"
        class="cursor-pointer rounded-lg border border-gray-200 bg-white p-5 transition-all hover:border-green-300 hover:shadow-md"
        @click="openEvaluation(comp.id)"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="mb-2 flex items-center gap-2">
              <h3 class="text-base font-bold text-gray-900">{{ comp.projectName || comp.competitionName }}</h3>
              <span
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="getStatusColor(comp.financialStatus)"
              >
                {{ getStatusLabel(comp.financialStatus) }}
              </span>
            </div>
            <p class="text-xs text-gray-500">
              {{ isRtl ? 'رقم المنافسة' : 'Competition #' }}: {{ comp.competitionNumber }}
            </p>
          </div>
          <div class="rounded-full bg-green-100 px-3 py-1 text-xs font-medium text-green-700">
            <i class="pi pi-check me-1" />
            {{ isRtl ? 'الفني معتمد' : 'Technical Approved' }}
          </div>
        </div>

        <div class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'العروض الناجحة' : 'Passed Offers' }}</span>
            <p class="text-sm font-semibold text-green-600">{{ comp.passedVendorCount || comp.vendorCount || 0 }}</p>
          </div>
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'الميزانية التقديرية' : 'Est. Budget' }}</span>
            <p class="text-sm font-semibold text-gray-900">{{ formatCurrency(comp.estimatedBudget || 0) }}</p>
          </div>
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'عدد العروض المؤهلة' : 'Passed Offers' }}</span>
            <p class="text-sm font-semibold text-blue-600">{{ comp.passedVendorCount || 0 }}</p>
          </div>
          <div>
            <span class="text-xs text-gray-400">{{ isRtl ? 'اللجنة' : 'Committee' }}</span>
            <p class="text-sm font-medium text-gray-700">{{ comp.assignedCommittee || (isRtl ? 'غير محدد' : 'Not assigned') }}</p>
          </div>
        </div>

        <!-- Progress -->
        <div class="mt-4">
          <div class="mb-1 flex items-center justify-between">
            <span class="text-xs text-gray-400">{{ isRtl ? 'تقدم التقييم' : 'Evaluation Progress' }}</span>
            <span class="text-xs font-semibold text-green-600">{{ comp.progress || 0 }}%</span>
          </div>
          <div class="h-1.5 w-full overflow-hidden rounded-full bg-gray-100">
            <div
              class="h-full rounded-full bg-green-500 transition-all duration-500"
              :style="{ width: `${comp.progress || 0}%` }"
            />
          </div>
        </div>

        <!-- Action buttons -->
        <div class="mt-4 flex gap-2">
          <button
            v-if="!comp.financialStatus || comp.financialStatus === 'pending'"
            @click.stop="startFinancialEvaluation(comp.id)"
            class="flex-1 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700"
          >
            <i class="pi pi-play me-1" />
            {{ isRtl ? 'بدء التقييم المالي' : 'Start Financial Evaluation' }}
          </button>
          <button
            v-else
            @click.stop="openEvaluation(comp.id)"
            class="flex-1 rounded-lg border border-green-300 bg-green-50 px-4 py-2 text-sm font-medium text-green-700 hover:bg-green-100"
          >
            <i class="pi pi-external-link me-1" />
            {{ isRtl ? 'فتح التقييم' : 'Open Evaluation' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
