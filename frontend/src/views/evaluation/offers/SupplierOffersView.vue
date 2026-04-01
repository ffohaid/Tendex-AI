<script setup lang="ts">
/**
 * Supplier Offers Management - List View
 * Shows all competitions that are in OffersClosed/PendingApproval+ status
 * and allows managing supplier offers for each.
 */
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { fetchCompetitionEvaluations } from '@/services/evaluationApi'
import type { CompetitionEvaluation } from '@/types/evaluation'

const { locale } = useI18n()
const router = useRouter()
const isRtl = computed(() => locale.value === 'ar')

const competitions = ref<CompetitionEvaluation[]>([])
const loading = ref(true)
const error = ref('')
const searchQuery = ref('')

const filteredCompetitions = computed(() => {
  if (!searchQuery.value) return competitions.value
  const q = searchQuery.value.toLowerCase()
  return competitions.value.filter(c =>
    c.competitionName.toLowerCase().includes(q) ||
    c.competitionNumber.toLowerCase().includes(q)
  )
})

async function loadCompetitions() {
  loading.value = true
  error.value = ''
  try {
    competitions.value = await fetchCompetitionEvaluations()
  } catch (e: any) {
    error.value = e?.message || 'حدث خطأ أثناء تحميل المنافسات'
  } finally {
    loading.value = false
  }
}

function openCompetition(id: string) {
  router.push({ name: 'SupplierOffersDetail', params: { competitionId: id } })
}

onMounted(loadCompetitions)
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">{{ isRtl ? 'إدارة عروض الموردين' : 'Supplier Offers Management' }}</h1>
      <p class="mt-1 text-sm text-gray-500">
        {{ isRtl ? 'إدارة وتسجيل عروض الموردين المقدمة للمنافسات' : 'Manage and register supplier offers for competitions' }}
      </p>
    </div>

    <!-- Search -->
    <div class="mb-4">
      <input
        v-model="searchQuery"
        type="text"
        :placeholder="isRtl ? 'البحث في المنافسات...' : 'Search competitions...'"
        class="w-full max-w-md rounded-lg border border-gray-300 px-4 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
      />
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-t-transparent"></div>
      <span class="ms-3 text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="rounded-lg border border-red-200 bg-red-50 p-4 text-red-700">
      {{ error }}
      <button @click="loadCompetitions" class="ms-2 text-red-600 underline hover:text-red-800">
        {{ isRtl ? 'إعادة المحاولة' : 'Retry' }}
      </button>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredCompetitions.length === 0" class="rounded-lg border border-gray-200 bg-white p-12 text-center">
      <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
      <h3 class="mt-2 text-sm font-medium text-gray-900">{{ isRtl ? 'لا توجد منافسات' : 'No competitions' }}</h3>
      <p class="mt-1 text-sm text-gray-500">{{ isRtl ? 'لم يتم العثور على منافسات لإدارة عروضها' : 'No competitions found to manage offers' }}</p>
    </div>

    <!-- Competition Cards -->
    <div v-else class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="comp in filteredCompetitions"
        :key="comp.id"
        @click="openCompetition(comp.id)"
        class="cursor-pointer rounded-lg border border-gray-200 bg-white p-5 shadow-sm transition-all hover:border-blue-300 hover:shadow-md"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <h3 class="font-semibold text-gray-900">{{ comp.competitionName }}</h3>
            <p class="mt-1 text-xs text-gray-500">{{ comp.competitionNumber }}</p>
          </div>
          <span
            :class="{
              'bg-yellow-100 text-yellow-800': comp.stage === 'technical',
              'bg-blue-100 text-blue-800': comp.stage === 'financial',
              'bg-green-100 text-green-800': comp.stage === 'completed',
            }"
            class="rounded-full px-2.5 py-0.5 text-xs font-medium"
          >
            {{ comp.stage === 'technical' ? (isRtl ? 'فني' : 'Technical')
              : comp.stage === 'financial' ? (isRtl ? 'مالي' : 'Financial')
              : (isRtl ? 'مكتمل' : 'Completed') }}
          </span>
        </div>

        <div class="mt-4 flex items-center justify-between border-t border-gray-100 pt-3">
          <div class="text-sm text-gray-500">
            <span class="font-medium text-gray-700">{{ comp.vendorCount }}</span>
            {{ isRtl ? 'عرض' : 'offers' }}
          </div>
          <div class="text-sm text-gray-500">
            {{ comp.estimatedBudget?.toLocaleString() }} {{ isRtl ? '﷼' : 'SAR' }}
          </div>
        </div>

        <div class="mt-3">
          <button class="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
            {{ isRtl ? 'إدارة العروض' : 'Manage Offers' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
