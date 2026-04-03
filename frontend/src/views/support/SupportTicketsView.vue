<script setup lang="ts">
/**
 * Tenant Support Tickets View.
 * Allows tenant users to submit and track support tickets.
 * All data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '@/services/http'

const { t } = useI18n()

const tickets = ref<any[]>([])
const loading = ref(false)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const statusFilter = ref('')

// Create ticket
const showCreateModal = ref(false)
const createForm = ref({ subject: '', description: '', category: 8, priority: 1 })
const creating = ref(false)

// Detail
const showDetailModal = ref(false)
const selectedTicket = ref<any>(null)
const detailLoading = ref(false)
const newMessage = ref('')
const sendingMessage = ref(false)

const statusOptions = [
  { value: '', label: 'جميع الحالات' },
  { value: 'Open', label: 'مفتوحة' },
  { value: 'InProgress', label: 'قيد المعالجة' },
  { value: 'WaitingForCustomer', label: 'بانتظار ردك' },
  { value: 'WaitingForOperator', label: 'بانتظار الدعم' },
  { value: 'Resolved', label: 'تم الحل' },
  { value: 'Closed', label: 'مغلقة' },
]

const categoryOptions = [
  { value: 0, label: 'مشكلة تقنية' },
  { value: 1, label: 'طلب ميزة' },
  { value: 2, label: 'الوصول للحساب' },
  { value: 3, label: 'الاشتراك والفوترة' },
  { value: 4, label: 'التدريب والتوثيق' },
  { value: 5, label: 'التكامل والربط' },
  { value: 6, label: 'مشكلة أداء' },
  { value: 7, label: 'البيانات والتقارير' },
  { value: 8, label: 'استفسار عام' },
]

const priorityOptions = [
  { value: 0, label: 'منخفضة' },
  { value: 1, label: 'متوسطة' },
  { value: 2, label: 'عالية' },
  { value: 3, label: 'حرجة' },
]

const statusColor = (s: string) => ({
  Open: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
  InProgress: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  WaitingForCustomer: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
  WaitingForOperator: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400',
  Resolved: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  Closed: 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400',
}[s] || 'bg-gray-100 text-gray-800')

const priorityColor = (p: string) => ({
  Low: 'bg-slate-100 text-slate-700', Medium: 'bg-blue-100 text-blue-700',
  High: 'bg-orange-100 text-orange-700', Critical: 'bg-red-100 text-red-700',
}[p] || 'bg-gray-100 text-gray-700')

const statusLabel = (s: string) => statusOptions.find(o => o.value === s)?.label || s

// Map enum names from API to Arabic labels
const categoryNameMap: Record<string, string> = {
  TechnicalIssue: 'مشكلة تقنية', FeatureRequest: 'طلب ميزة', AccountAccess: 'الوصول للحساب',
  BillingSubscription: 'الاشتراك والفوترة', TrainingDocumentation: 'التدريب والتوثيق',
  IntegrationApi: 'التكامل والربط', PerformanceIssue: 'مشكلة أداء', DataReporting: 'البيانات والتقارير',
  GeneralInquiry: 'استفسار عام',
}
const priorityNameMap: Record<string, string> = {
  Low: 'منخفضة', Medium: 'متوسطة', High: 'عالية', Critical: 'حرجة',
}
const categoryLabel = (c: any) => {
  if (typeof c === 'number') return categoryOptions.find(o => o.value === c)?.label || c
  return categoryNameMap[c] || categoryOptions.find(o => o.value === c)?.label || c
}
const priorityLabel = (p: any) => {
  if (typeof p === 'number') return priorityOptions.find(o => o.value === p)?.label || p
  return priorityNameMap[p] || priorityOptions.find(o => o.value === p)?.label || p
}

const formatTimeAgo = (d: string) => {
  if (!d) return '-'
  const ms = Date.now() - new Date(d).getTime()
  const m = Math.floor(ms / 60000), h = Math.floor(ms / 3600000), dy = Math.floor(ms / 86400000)
  if (m < 1) return 'الآن'
  if (m < 60) return `منذ ${m} دقيقة`
  if (h < 24) return `منذ ${h} ساعة`
  return `منذ ${dy} يوم`
}

async function fetchTickets() {
  loading.value = true
  try {
    const p = new URLSearchParams({ page: page.value.toString(), pageSize: pageSize.value.toString() })
    if (statusFilter.value) p.append('status', statusFilter.value)
    const r = await http.get(`/v1/support-tickets?${p}`)
    tickets.value = r.data.items || []
    totalCount.value = r.data.totalCount || 0
    totalPages.value = r.data.totalPages || 0
  } catch { tickets.value = [] }
  finally { loading.value = false }
}

async function createTicket() {
  if (!createForm.value.subject.trim() || !createForm.value.description.trim()) return
  creating.value = true
  try {
    await http.post('/v1/support-tickets', createForm.value)
    showCreateModal.value = false
    createForm.value = { subject: '', description: '', category: 8, priority: 1 }
    fetchTickets()
  } catch (e) { console.error('Failed to create ticket:', e) }
  finally { creating.value = false }
}

async function openDetail(id: string) {
  detailLoading.value = true; showDetailModal.value = true
  try {
    const r = await http.get(`/v1/support-tickets/${id}`)
    selectedTicket.value = r.data
    await http.put(`/v1/support-tickets/${id}/messages/read`)
  } catch {} finally { detailLoading.value = false }
}

async function sendMsg() {
  if (!newMessage.value.trim() || !selectedTicket.value) return
  sendingMessage.value = true
  try {
    const r = await http.post(`/v1/support-tickets/${selectedTicket.value.id}/messages`, { content: newMessage.value })
    selectedTicket.value.messages.push(r.data)
    newMessage.value = ''
    fetchTickets()
  } catch {} finally { sendingMessage.value = false }
}

watch(statusFilter, () => { page.value = 1; fetchTickets() })
onMounted(() => fetchTickets())
</script>

<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">الدعم الفني</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">تواصل مع فريق الدعم الفني لمنصة نتاق</p>
      </div>
      <button @click="showCreateModal = true" class="inline-flex items-center gap-2 px-4 py-2.5 bg-primary-600 text-white rounded-xl hover:bg-primary-700 text-sm font-medium transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
        تذكرة جديدة
      </button>
    </div>

    <!-- Filter -->
    <div class="flex gap-2">
      <select v-model="statusFilter" class="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
        <option v-for="o in statusOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
      </select>
    </div>

    <!-- Tickets -->
    <div class="space-y-3">
      <div v-if="loading" class="p-8 text-center">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
      </div>
      <div v-else-if="tickets.length === 0" class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-8 text-center">
        <svg class="w-12 h-12 text-gray-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" /></svg>
        <p class="text-gray-500 dark:text-gray-400 mb-3">لا توجد تذاكر دعم فني</p>
        <button @click="showCreateModal = true" class="text-primary-600 hover:text-primary-700 text-sm font-medium">أنشئ تذكرة جديدة</button>
      </div>
      <div v-else v-for="tk in tickets" :key="tk.id" @click="openDetail(tk.id)" class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-primary-300 dark:hover:border-primary-600 cursor-pointer transition-colors">
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="flex items-center gap-2 mb-1">
              <span class="text-xs font-mono text-primary-600 dark:text-primary-400">{{ tk.ticketNumber }}</span>
              <span :class="['px-2 py-0.5 rounded text-xs font-medium', statusColor(tk.status)]">{{ statusLabel(tk.status) }}</span>
              <span :class="['px-2 py-0.5 rounded text-xs font-medium', priorityColor(tk.priority)]">{{ priorityLabel(tk.priority) }}</span>
            </div>
            <h3 class="text-sm font-semibold text-gray-900 dark:text-white">{{ tk.subject }}</h3>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">{{ categoryLabel(tk.category) }} &middot; {{ formatTimeAgo(tk.createdAt) }}</p>
          </div>
          <svg class="w-5 h-5 text-gray-400 mt-1 rtl:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex items-center justify-center gap-2">
      <button @click="page--; fetchTickets()" :disabled="page <= 1" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50">السابق</button>
      <span class="text-sm text-gray-500">{{ page }} / {{ totalPages }}</span>
      <button @click="page++; fetchTickets()" :disabled="page >= totalPages" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50">التالي</button>
    </div>

    <!-- Create Modal -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showCreateModal = false"></div>
        <div class="relative bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-lg p-6">
          <h2 class="text-lg font-bold text-gray-900 dark:text-white mb-4">تذكرة دعم فني جديدة</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">الموضوع</label>
              <input v-model="createForm.subject" type="text" placeholder="عنوان مختصر للمشكلة" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">الوصف</label>
              <textarea v-model="createForm.description" rows="4" placeholder="اشرح المشكلة بالتفصيل..." class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm resize-none"></textarea>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">التصنيف</label>
                <select v-model="createForm.category" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
                  <option v-for="o in categoryOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">الأولوية</label>
                <select v-model="createForm.priority" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
                  <option v-for="o in priorityOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
                </select>
              </div>
            </div>
          </div>
          <div class="flex gap-2 justify-end mt-6">
            <button @click="showCreateModal = false" class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">إلغاء</button>
            <button @click="createTicket" :disabled="!createForm.subject.trim() || !createForm.description.trim() || creating" class="px-4 py-2 text-sm bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50">
              {{ creating ? 'جاري الإرسال...' : 'إرسال التذكرة' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Detail Modal -->
    <Teleport to="body">
      <div v-if="showDetailModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showDetailModal = false"></div>
        <div class="relative bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-3xl max-h-[85vh] overflow-hidden flex flex-col">
          <div class="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
            <div v-if="selectedTicket">
              <div class="flex items-center gap-2">
                <span class="text-sm font-mono text-primary-600">{{ selectedTicket.ticketNumber }}</span>
                <span :class="['px-2 py-0.5 rounded text-xs font-medium', statusColor(selectedTicket.status)]">{{ statusLabel(selectedTicket.status) }}</span>
              </div>
              <h2 class="text-base font-bold text-gray-900 dark:text-white mt-1">{{ selectedTicket.subject }}</h2>
            </div>
            <button @click="showDetailModal = false" class="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>
          <div v-if="detailLoading" class="flex-1 flex items-center justify-center p-8"><div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div></div>
          <div v-else-if="selectedTicket" class="flex-1 overflow-y-auto">
            <!-- Messages -->
            <div class="p-4 space-y-4 max-h-[400px] overflow-y-auto">
              <div v-for="msg in selectedTicket.messages" :key="msg.id" :class="['flex gap-3', msg.isOperatorMessage ? 'flex-row-reverse' : '']">
                <div :class="['w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 text-xs font-bold', msg.isOperatorMessage ? 'bg-primary-100 text-primary-700' : 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300']">
                  {{ msg.isAiGenerated ? 'AI' : msg.senderName?.charAt(0) || '?' }}
                </div>
                <div :class="['max-w-[75%] rounded-xl p-3', msg.isOperatorMessage ? 'bg-primary-50 dark:bg-primary-900/20 border border-primary-200' : 'bg-gray-100 dark:bg-gray-700 border border-gray-200 dark:border-gray-600']">
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xs font-medium text-gray-700 dark:text-gray-300">{{ msg.senderName }}</span>
                    <span v-if="msg.isAiGenerated" class="px-1.5 py-0.5 rounded text-[10px] font-medium bg-violet-100 text-violet-700">AI</span>
                    <span class="text-[10px] text-gray-400">{{ formatTimeAgo(msg.createdAt) }}</span>
                  </div>
                  <p class="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap">{{ msg.content }}</p>
                </div>
              </div>
            </div>
            <!-- Input -->
            <div class="p-4 border-t border-gray-200 dark:border-gray-700">
              <div class="flex gap-2">
                <textarea v-model="newMessage" rows="2" placeholder="اكتب رسالتك..." class="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm resize-none"></textarea>
                <button @click="sendMsg" :disabled="!newMessage.trim() || sendingMessage" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 self-end">
                  <svg class="w-5 h-5 rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" /></svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
