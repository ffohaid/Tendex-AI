<script setup lang="ts">
/**
 * ComprehensiveEvaluationList - Lists competitions ready for comprehensive evaluation.
 * Shows competitions where financial evaluation has been completed/approved.
 * Entry point for generating award recommendations.
 */
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import { formatCurrency } from '@/utils/numbers'

const { locale } = useI18n()
const router = useRouter()
const store = useEvaluationStore()
const isRtl = computed(() => locale.value === 'ar')

const searchQuery = ref('')

onMounted(async () => {
  await store.loadCompetitions()
})

/* Filter competitions where financial evaluation is completed */
const eligibleCompetitions = computed(() => {
  return store.competitions.filter(c =>
    c.financialStatus === 'completed' || c.financialStatus === 'approved' || c.stage === 'completed'
  )
})

const filteredCompetitions = computed(() => {
  if (!searchQuery.value) return eligibleCompetitions.value
  const q = searchQuery.value.toLowerCase()
  return eligibleCompetitions.value.filter(c =>
    c.competitionName?.toLowerCase().includes(q) ||
    c.competitionNumber?.toLowerCase().includes(q)
  )
})

function navigateToDetail(competitionId: string) {
  router.push({ name: 'ComprehensiveEvaluationDetail', params: { id: competitionId } })
}
</script>

<template>
  <div class="space-y-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">
          <i class="pi pi-chart-pie me-2 text-indigo-600" />
          {{ isRtl ? 'التقييم الشامل وتوصية الترسية' : 'Comprehensive Evaluation & Award' }}
        </h1>
        <p class="mt-1 text-sm text-gray-500">
          {{ isRtl ? 'التقييم النهائي الشامل (فني + مالي) وإصدار توصية الترسية وفق نظام المنافسات والمشتريات الحكومية' : 'Final comprehensive evaluation (technical + financial) and award recommendation per government procurement regulations' }}
        </p>
      </div>
    </div>

    <!-- Info Banner -->
    <div class="rounded-lg border border-indigo-200 bg-indigo-50 p-4">
      <div class="flex items-start gap-3">
        <i class="pi pi-info-circle mt-0.5 text-indigo-600" />
        <div class="text-sm text-indigo-800">
          <p class="font-medium">
            {{ isRtl ? 'متطلبات التقييم الشامل' : 'Comprehensive Evaluation Requirements' }}
          </p>
          <ul class="mt-1 list-inside list-disc space-y-0.5 text-xs">
            <li>{{ isRtl ? 'يجب اعتماد التقييم الفني أولاً' : 'Technical evaluation must be approved first' }}</li>
            <li>{{ isRtl ? 'يجب اعتماد التقييم المالي' : 'Financial evaluation must be approved' }}</li>
            <li>{{ isRtl ? 'يتم حساب الدرجة المركبة بنسبة 70% فني و 30% مالي (افتراضياً)' : 'Combined score calculated at 70% technical and 30% financial (default)' }}</li>
            <li>{{ isRtl ? 'توصية الترسية تصدر تلقائياً بناءً على أعلى درجة مركبة' : 'Award recommendation is auto-generated based on highest combined score' }}</li>
          </ul>
        </div>
      </div>
    </div>

    <!-- Search -->
    <div class="relative">
      <i class="pi pi-search absolute top-3 text-gray-400" :class="isRtl ? 'end-3' : 'start-3'" />
      <input
        v-model="searchQuery"
        type="text"
        :placeholder="isRtl ? 'بحث بالاسم أو الرقم المرجعي...' : 'Search by name or reference number...'"
        class="w-full rounded-lg border border-gray-300 py-2.5 text-sm focus:border-indigo-500 focus:outline-none"
        :class="isRtl ? 'pe-10 ps-4' : 'pe-4 ps-10'"
      />
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-indigo-600" />
      <span class="ms-3 text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredCompetitions.length === 0" class="rounded-lg border border-dashed border-gray-300 py-16 text-center">
      <i class="pi pi-inbox text-4xl text-gray-300" />
      <p class="mt-3 text-sm text-gray-500">
        {{ isRtl ? 'لا توجد منافسات جاهزة للتقييم الشامل' : 'No competitions ready for comprehensive evaluation' }}
      </p>
      <p class="mt-1 text-xs text-gray-400">
        {{ isRtl ? 'يجب إكمال التقييم الفني والمالي أولاً' : 'Complete technical and financial evaluations first' }}
      </p>
    </div>

    <!-- Competition Cards -->
    <div v-else class="space-y-3">
      <div
        v-for="comp in filteredCompetitions"
        :key="comp.id"
        class="cursor-pointer rounded-lg border border-gray-200 bg-white p-5 transition-all hover:border-indigo-300 hover:shadow-md"
        @click="navigateToDetail(comp.id)"
      >
        <div class="flex items-center justify-between">
          <div class="flex-1">
            <div class="flex items-center gap-3">
              <h3 class="text-base font-bold text-gray-900">{{ comp.competitionName }}</h3>
              <span class="rounded-full bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-indigo-700">
                {{ comp.competitionNumber }}
              </span>
            </div>
            <div class="mt-2 flex items-center gap-4 text-xs text-gray-500">
              <span>
                <i class="pi pi-wallet me-1" />
                {{ formatCurrency(comp.estimatedBudget) }}
              </span>
              <span>
                <i class="pi pi-check-circle me-1 text-green-500" />
                {{ isRtl ? 'التقييم الفني: مكتمل' : 'Technical: Complete' }}
              </span>
              <span>
                <i class="pi pi-check-circle me-1 text-green-500" />
                {{ isRtl ? 'التقييم المالي: مكتمل' : 'Financial: Complete' }}
              </span>
            </div>
          </div>
          <div class="flex items-center gap-2">
            <span
              class="rounded-full px-3 py-1 text-xs font-medium"
              :class="comp.stage === 'completed' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'"
            >
              {{ comp.stage === 'completed'
                ? (isRtl ? 'مكتمل' : 'Completed')
                : (isRtl ? 'جاهز للتقييم الشامل' : 'Ready for Evaluation') }}
            </span>
            <i class="pi text-gray-400" :class="isRtl ? 'pi-angle-left' : 'pi-angle-right'" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
