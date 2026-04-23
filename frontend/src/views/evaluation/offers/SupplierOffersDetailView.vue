<script setup lang="ts">
/**
 * Supplier Offers Detail View
 * Manage supplier offers for a specific competition.
 * Allows adding, viewing, and deleting supplier offers.
 */
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  fetchSupplierOffers,
  createSupplierOffer,
  deleteSupplierOffer,
  fetchCompetitionEvaluation,
  startTechnicalEvaluation,
} from '@/services/evaluationApi'
import type { SupplierOffer } from '@/types/evaluation'

const { locale } = useI18n()
const route = useRoute()
const router = useRouter()
const isRtl = computed(() => locale.value === 'ar')
const competitionId = computed(() => route.params.competitionId as string)

const competition = ref<any>(null)
const offers = ref<SupplierOffer[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const successMsg = ref('')
const showAddForm = ref(false)
const deleteConfirmId = ref<string | null>(null)

// New offer form
const newOffer = ref({
  supplierName: '',
  supplierIdentifier: '',
  offerReferenceNumber: '',
  submissionDate: new Date().toISOString().split('T')[0],
})

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    const [offersData, compData] = await Promise.all([
      fetchSupplierOffers(competitionId.value),
      fetchCompetitionEvaluation(competitionId.value).catch(() => null),
    ])
    offers.value = Array.isArray(offersData) ? offersData : []
    competition.value = compData
  } catch (e: any) {
    error.value = e?.message || 'حدث خطأ أثناء تحميل البيانات'
  } finally {
    loading.value = false
  }
}

async function addOffer() {
  if (!newOffer.value.supplierName || !newOffer.value.supplierIdentifier || !newOffer.value.offerReferenceNumber) {
    error.value = isRtl.value ? 'يرجى تعبئة جميع الحقول المطلوبة' : 'Please fill all required fields'
    return
  }

  saving.value = true
  error.value = ''
  try {
    const created = await createSupplierOffer(competitionId.value, {
      supplierName: newOffer.value.supplierName,
      supplierIdentifier: newOffer.value.supplierIdentifier,
      offerReferenceNumber: newOffer.value.offerReferenceNumber,
      submissionDate: newOffer.value.submissionDate + 'T00:00:00Z',
    })
    offers.value.push(created)
    newOffer.value = {
      supplierName: '',
      supplierIdentifier: '',
      offerReferenceNumber: '',
      submissionDate: new Date().toISOString().split('T')[0],
    }
    showAddForm.value = false
    successMsg.value = isRtl.value ? 'تم إضافة العرض بنجاح' : 'Offer added successfully'
    setTimeout(() => successMsg.value = '', 3000)
  } catch (e: any) {
    error.value = e?.response?.data?.detail || e?.message || 'حدث خطأ أثناء إضافة العرض'
  } finally {
    saving.value = false
  }
}

async function confirmDelete(offerId: string) {
  deleteConfirmId.value = offerId
}

async function doDelete() {
  if (!deleteConfirmId.value) return
  saving.value = true
  error.value = ''
  try {
    await deleteSupplierOffer(competitionId.value, deleteConfirmId.value)
    offers.value = offers.value.filter(o => o.id !== deleteConfirmId.value)
    deleteConfirmId.value = null
    successMsg.value = isRtl.value ? 'تم حذف العرض بنجاح' : 'Offer deleted successfully'
    setTimeout(() => successMsg.value = '', 3000)
  } catch (e: any) {
    error.value = e?.response?.data?.detail || e?.response?.data?.error || e?.message || 'حدث خطأ أثناء حذف العرض'
  } finally {
    saving.value = false
  }
}

async function goToTechnicalEvaluation() {
  saving.value = true
  error.value = ''
  try {
    await startTechnicalEvaluation(competitionId.value)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.error || e?.message || ''
    const alreadyExists = typeof detail === 'string' && detail.toLowerCase().includes('already exists')

    if (!alreadyExists) {
      error.value = detail || (isRtl.value ? 'تعذر بدء التقييم الفني' : 'Failed to start technical evaluation')
      return
    }
  } finally {
    saving.value = false
  }

  router.push({ name: 'TechnicalEvaluationDetail', params: { id: competitionId.value } })
}

function getTechnicalResultLabel(result: number): string {
  if (result === 1) return isRtl.value ? 'ناجح' : 'Passed'
  if (result === 2) return isRtl.value ? 'راسب' : 'Failed'
  return isRtl.value ? 'قيد التقييم' : 'Pending'
}

function getTechnicalResultClass(result: number): string {
  if (result === 1) return 'bg-green-100 text-green-800'
  if (result === 2) return 'bg-red-100 text-red-800'
  return 'bg-gray-100 text-gray-800'
}

onMounted(loadData)
</script>

<template>
  <div class="min-h-screen bg-gray-50 p-6" :dir="isRtl ? 'rtl' : 'ltr'">
    <!-- Back Button & Header -->
    <div class="mb-6">
      <button
        @click="router.push({ name: 'SupplierOffers' })"
        class="mb-3 flex items-center text-sm text-gray-500 hover:text-gray-700"
      >
        <svg class="h-4 w-4" :class="isRtl ? 'ms-1 rotate-180' : 'me-1'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
        {{ isRtl ? 'العودة للقائمة' : 'Back to list' }}
      </button>
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ isRtl ? 'عروض الموردين' : 'Supplier Offers' }}</h1>
          <p v-if="competition" class="mt-1 text-sm text-gray-500">
            {{ competition.projectNameAr || competition.competitionName || '' }}
          </p>
        </div>
        <div class="flex gap-2">
          <button
            v-if="offers.length >= 2"
            @click="goToTechnicalEvaluation"
            :disabled="saving"
            class="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {{ saving ? (isRtl ? 'جارٍ البدء...' : 'Starting...') : (isRtl ? 'بدء التقييم الفني' : 'Start Technical Evaluation') }}
          </button>
          <button
            @click="showAddForm = !showAddForm"
            class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
          >
            {{ showAddForm ? (isRtl ? 'إلغاء' : 'Cancel') : (isRtl ? 'إضافة عرض جديد' : 'Add New Offer') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Success Message -->
    <div v-if="successMsg" class="mb-4 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
      {{ successMsg }}
    </div>

    <!-- Error Message -->
    <div v-if="error" class="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
      {{ error }}
      <button @click="error = ''" class="ms-2 font-medium underline">{{ isRtl ? 'إغلاق' : 'Close' }}</button>
    </div>

    <!-- Add Offer Form -->
    <div v-if="showAddForm" class="mb-6 rounded-lg border border-blue-200 bg-white p-6 shadow-sm">
      <h3 class="mb-4 text-lg font-semibold text-gray-900">{{ isRtl ? 'إضافة عرض مورد جديد' : 'Add New Supplier Offer' }}</h3>
      <div class="grid gap-4 md:grid-cols-2">
        <div>
          <label class="mb-1 block text-sm font-medium text-gray-700">
            {{ isRtl ? 'اسم المورد' : 'Supplier Name' }} <span class="text-red-500">*</span>
          </label>
          <input
            v-model="newOffer.supplierName"
            type="text"
            :placeholder="isRtl ? 'مثال: شركة التقنية المتقدمة' : 'e.g., Advanced Tech Co.'"
            class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-gray-700">
            {{ isRtl ? 'رقم السجل التجاري / المعرف' : 'Commercial Registration / ID' }} <span class="text-red-500">*</span>
          </label>
          <input
            v-model="newOffer.supplierIdentifier"
            type="text"
            :placeholder="isRtl ? 'مثال: 1010XXXXXX' : 'e.g., 1010XXXXXX'"
            class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-gray-700">
            {{ isRtl ? 'رقم مرجع العرض' : 'Offer Reference Number' }} <span class="text-red-500">*</span>
          </label>
          <input
            v-model="newOffer.offerReferenceNumber"
            type="text"
            :placeholder="isRtl ? 'مثال: OFR-2026-001' : 'e.g., OFR-2026-001'"
            class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-gray-700">
            {{ isRtl ? 'تاريخ تقديم العرض' : 'Submission Date' }}
          </label>
          <input
            v-model="newOffer.submissionDate"
            type="date"
            :min="new Date().toISOString().split('T')[0]"
            class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
      </div>
      <div class="mt-4 flex justify-end gap-2">
        <button
          @click="showAddForm = false"
          class="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
        >
          {{ isRtl ? 'إلغاء' : 'Cancel' }}
        </button>
        <button
          @click="addOffer"
          :disabled="saving"
          class="rounded-lg bg-blue-600 px-6 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          <span v-if="saving" class="flex items-center">
            <svg class="me-2 h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ isRtl ? 'جارٍ الحفظ...' : 'Saving...' }}
          </span>
          <span v-else>{{ isRtl ? 'إضافة العرض' : 'Add Offer' }}</span>
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-t-transparent"></div>
      <span class="ms-3 text-gray-500">{{ isRtl ? 'جارٍ التحميل...' : 'Loading...' }}</span>
    </div>

    <!-- Offers Table -->
    <div v-else class="overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">#</th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'الرمز المجهول' : 'Blind Code' }}
            </th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'اسم المورد' : 'Supplier Name' }}
            </th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'السجل التجاري' : 'CR Number' }}
            </th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'رقم المرجع' : 'Reference' }}
            </th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'تاريخ التقديم' : 'Submission Date' }}
            </th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'نتيجة الفحص الفني' : 'Technical Result' }}
            </th>
            <th class="px-4 py-3 text-start text-xs font-medium uppercase tracking-wider text-gray-500">
              {{ isRtl ? 'الإجراءات' : 'Actions' }}
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200">
          <tr v-if="offers.length === 0">
            <td colspan="8" class="px-4 py-12 text-center text-sm text-gray-500">
              {{ isRtl ? 'لا توجد عروض مسجلة. اضغط "إضافة عرض جديد" لبدء تسجيل العروض.' : 'No offers registered. Click "Add New Offer" to start.' }}
            </td>
          </tr>
          <tr v-for="(offer, idx) in offers" :key="offer.id" class="hover:bg-gray-50">
            <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500">{{ idx + 1 }}</td>
            <td class="whitespace-nowrap px-4 py-3">
              <span class="rounded-full bg-indigo-100 px-2.5 py-0.5 text-xs font-bold text-indigo-800">
                {{ offer.blindCode }}
              </span>
            </td>
            <td class="whitespace-nowrap px-4 py-3 text-sm font-medium text-gray-900">{{ offer.supplierName }}</td>
            <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500">{{ offer.supplierIdentifier }}</td>
            <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500">{{ offer.offerReferenceNumber }}</td>
            <td class="whitespace-nowrap px-4 py-3 text-sm text-gray-500">
              {{ new Date(offer.submissionDate).toLocaleDateString(isRtl ? 'ar-SA' : 'en-US') }}
            </td>
            <td class="whitespace-nowrap px-4 py-3">
              <span
                :class="getTechnicalResultClass(offer.technicalResult)"
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
              >
                {{ getTechnicalResultLabel(offer.technicalResult) }}
              </span>
            </td>
            <td class="whitespace-nowrap px-4 py-3">
              <button
                @click="confirmDelete(offer.id)"
                class="text-sm text-red-600 hover:text-red-800"
                :title="isRtl ? 'حذف العرض' : 'Delete offer'"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Summary Stats -->
    <div v-if="offers.length > 0" class="mt-4 grid gap-4 md:grid-cols-3">
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-sm text-gray-500">{{ isRtl ? 'إجمالي العروض' : 'Total Offers' }}</div>
        <div class="mt-1 text-2xl font-bold text-gray-900">{{ offers.length }}</div>
      </div>
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-sm text-gray-500">{{ isRtl ? 'ناجح فنياً' : 'Technically Passed' }}</div>
        <div class="mt-1 text-2xl font-bold text-green-600">{{ offers.filter(o => o.technicalResult === 1).length }}</div>
      </div>
      <div class="rounded-lg border border-gray-200 bg-white p-4">
        <div class="text-sm text-gray-500">{{ isRtl ? 'قيد التقييم' : 'Pending Evaluation' }}</div>
        <div class="mt-1 text-2xl font-bold text-yellow-600">{{ offers.filter(o => o.technicalResult === 0).length }}</div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="deleteConfirmId" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div class="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <h3 class="text-lg font-semibold text-gray-900">{{ isRtl ? 'تأكيد الحذف' : 'Confirm Delete' }}</h3>
        <p class="mt-2 text-sm text-gray-500">
          {{ isRtl ? 'هل أنت متأكد من حذف هذا العرض؟ لا يمكن التراجع عن هذا الإجراء.' : 'Are you sure you want to delete this offer? This action cannot be undone.' }}
        </p>
        <div class="mt-4 flex justify-end gap-2">
          <button
            @click="deleteConfirmId = null"
            class="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {{ isRtl ? 'إلغاء' : 'Cancel' }}
          </button>
          <button
            @click="doDelete"
            :disabled="saving"
            class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
          >
            {{ isRtl ? 'حذف' : 'Delete' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
