<script setup lang="ts">
/**
 * Operator Support Tickets View.
 * Displays all support tickets from all tenants for the operator to manage.
 * All data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, ref, watch } from 'vue'
import http from '@/services/http'

// State
const tickets = ref<any[]>([])
const stats = ref<any>({ total: 0, totalOpen: 0, totalInProgress: 0, totalResolved: 0, totalClosed: 0, unreadMessages: 0 })
const loading = ref(false)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)

// Filters
const statusFilter = ref('')
const categoryFilter = ref('')
const priorityFilter = ref('')
const searchQuery = ref('')

// Detail
const showDetailModal = ref(false)
const selectedTicket = ref<any>(null)
const detailLoading = ref(false)
const newMessage = ref('')
const sendingMessage = ref(false)
const aiAnalyzing = ref(false)
const aiGeneratingReply = ref(false)

// Assign
const showAssignModal = ref(false)
const assignUserName = ref('')

const statusOptions = [
  { value: '', label: 'جميع الحالات' },
  { value: 'Open', label: 'مفتوحة' },
  { value: 'InProgress', label: 'قيد المعالجة' },
  { value: 'WaitingForCustomer', label: 'بانتظار العميل' },
  { value: 'WaitingForOperator', label: 'بانتظار المشغل' },
  { value: 'Resolved', label: 'تم الحل' },
  { value: 'Closed', label: 'مغلقة' },
]

const categoryOptions = [
  { value: '', label: 'جميع التصنيفات' },
  { value: 'TechnicalIssue', label: 'مشكلة تقنية' },
  { value: 'FeatureRequest', label: 'طلب ميزة' },
  { value: 'AccountAccess', label: 'الوصول للحساب' },
  { value: 'BillingSubscription', label: 'الاشتراك والفوترة' },
  { value: 'TrainingDocumentation', label: 'التدريب والتوثيق' },
  { value: 'IntegrationApi', label: 'التكامل والربط' },
  { value: 'PerformanceIssue', label: 'مشكلة أداء' },
  { value: 'DataReporting', label: 'البيانات والتقارير' },
  { value: 'GeneralInquiry', label: 'استفسار عام' },
]

const priorityOptions = [
  { value: '', label: 'جميع الأولويات' },
  { value: 'Low', label: 'منخفضة' },
  { value: 'Medium', label: 'متوسطة' },
  { value: 'High', label: 'عالية' },
  { value: 'Critical', label: 'حرجة' },
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
  Low: 'bg-slate-100 text-slate-700 dark:bg-slate-900/30 dark:text-slate-400',
  Medium: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  High: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  Critical: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
}[p] || 'bg-gray-100 text-gray-700')

const statusLabel = (s: string) => statusOptions.find(o => o.value === s)?.label || s
const categoryLabel = (c: string) => categoryOptions.find(o => o.value === c)?.label || c
const priorityLabel = (p: string) => priorityOptions.find(o => o.value === p)?.label || p

const formatTimeAgo = (d: string) => {
  if (!d) return '-'
  const ms = Date.now() - new Date(d).getTime()
  const m = Math.floor(ms / 60000), h = Math.floor(ms / 3600000), dy = Math.floor(ms / 86400000)
  if (m < 1) return 'الآن'
  if (m < 60) return `منذ ${m} دقيقة`
  if (h < 24) return `منذ ${h} ساعة`
  return `منذ ${dy} يوم`
}

const formatDate = (d: string) => d ? new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) : '-'

// API
async function fetchTickets() {
  loading.value = true
  try {
    const p = new URLSearchParams({ page: page.value.toString(), pageSize: pageSize.value.toString() })
    if (statusFilter.value) p.append('status', statusFilter.value)
    if (categoryFilter.value) p.append('category', categoryFilter.value)
    if (priorityFilter.value) p.append('priority', priorityFilter.value)
    if (searchQuery.value) p.append('search', searchQuery.value)
    const r = await http.get(`/v1/support-tickets?${p}`)
    tickets.value = r.data.items || []
    totalCount.value = r.data.totalCount || 0
    totalPages.value = r.data.totalPages || 0
  } catch { tickets.value = [] }
  finally { loading.value = false }
}

async function fetchStats() {
  try { const r = await http.get('/v1/support-tickets/stats'); stats.value = r.data } catch {}
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
    fetchTickets(); fetchStats()
  } catch {} finally { sendingMessage.value = false }
}

async function aiAnalyze() {
  if (!selectedTicket.value) return
  aiAnalyzing.value = true
  try {
    const r = await http.post(`/v1/support-tickets/${selectedTicket.value.id}/ai-analyze`)
    Object.assign(selectedTicket.value, { aiSummary: r.data.summary, aiSentiment: r.data.sentiment, aiSuggestedCategory: r.data.suggestedCategory, aiSuggestedPriority: r.data.suggestedPriority, aiSuggestedResolution: r.data.suggestedResolution })
  } catch {} finally { aiAnalyzing.value = false }
}

async function aiReply() {
  if (!selectedTicket.value) return
  aiGeneratingReply.value = true
  try {
    const r = await http.post(`/v1/support-tickets/${selectedTicket.value.id}/ai-reply`, { tone: 'professional', autoSend: false })
    newMessage.value = r.data.suggestedReply
  } catch {} finally { aiGeneratingReply.value = false }
}

async function updateStatus(id: string, status: string) {
  try { await http.put(`/v1/support-tickets/${id}/status`, { status }); fetchTickets(); fetchStats(); if (selectedTicket.value?.id === id) selectedTicket.value.status = status } catch {}
}

async function assignTicket() {
  if (!selectedTicket.value || !assignUserName.value) return
  try {
    await http.put(`/v1/support-tickets/${selectedTicket.value.id}/assign`, { userId: '00000000-0000-0000-0000-000000000000', userName: assignUserName.value })
    selectedTicket.value.assignedToUserName = assignUserName.value
    selectedTicket.value.status = 'InProgress'
    showAssignModal.value = false; assignUserName.value = ''
    fetchTickets(); fetchStats()
  } catch {}
}

async function resolveTicket() {
  if (!selectedTicket.value) return
  try { await http.put(`/v1/support-tickets/${selectedTicket.value.id}/resolve`, { resolutionNotes: 'تم حل المشكلة بنجاح' }); selectedTicket.value.status = 'Resolved'; fetchTickets(); fetchStats() } catch {}
}

watch([statusFilter, categoryFilter, priorityFilter, searchQuery], () => { page.value = 1; fetchTickets() })
onMounted(() => { fetchTickets(); fetchStats() })
</script>

<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">الدعم الفني</h1>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">إدارة تذاكر الدعم الفني من جميع الجهات</p>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
      <div v-for="(s, i) in [
        { label: 'الإجمالي', val: stats.total || 0, icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z', color: 'blue' },
        { label: 'مفتوحة', val: stats.totalOpen || 0, icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z', color: 'yellow' },
        { label: 'قيد المعالجة', val: stats.totalInProgress || 0, icon: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15', color: 'purple' },
        { label: 'تم الحل', val: stats.totalResolved || 0, icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'green' },
        { label: 'غير مقروءة', val: stats.unreadMessages || 0, icon: 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9', color: 'red' },
      ]" :key="i" class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center gap-3">
          <div :class="`w-10 h-10 rounded-lg bg-${s.color}-100 dark:bg-${s.color}-900/30 flex items-center justify-center`">
            <svg :class="`w-5 h-5 text-${s.color}-600`" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="s.icon" /></svg>
          </div>
          <div>
            <p class="text-xs text-gray-500 dark:text-gray-400">{{ s.label }}</p>
            <p :class="`text-xl font-bold text-${s.color}-600`">{{ s.val }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <input v-model="searchQuery" type="text" placeholder="بحث في التذاكر..." class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary-500" />
        <select v-model="statusFilter" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in statusOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
        <select v-model="categoryFilter" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in categoryOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
        <select v-model="priorityFilter" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in priorityOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
      </div>
    </div>

    <!-- Tickets Table -->
    <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div v-if="loading" class="p-8 text-center">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
        <p class="mt-2 text-sm text-gray-500">جاري التحميل...</p>
      </div>
      <div v-else-if="tickets.length === 0" class="p-8 text-center">
        <svg class="w-12 h-12 text-gray-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" /></svg>
        <p class="text-gray-500 dark:text-gray-400">لا توجد تذاكر دعم فني</p>
      </div>
      <table v-else class="w-full">
        <thead class="bg-gray-50 dark:bg-gray-700/50">
          <tr>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">رقم التذكرة</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الموضوع</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الجهة</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">التصنيف</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الأولوية</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الحالة</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">التاريخ</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">إجراء</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="tk in tickets" :key="tk.id" class="hover:bg-gray-50 dark:hover:bg-gray-700/30 cursor-pointer transition-colors" @click="openDetail(tk.id)">
            <td class="px-4 py-3 text-sm font-mono text-primary-600 dark:text-primary-400">{{ tk.ticketNumber }}</td>
            <td class="px-4 py-3"><div class="text-sm font-medium text-gray-900 dark:text-white">{{ tk.subject }}</div><div class="text-xs text-gray-500">{{ tk.createdByUserName }}</div></td>
            <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{{ tk.tenantName || '-' }}</td>
            <td class="px-4 py-3"><span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300">{{ categoryLabel(tk.category) }}</span></td>
            <td class="px-4 py-3"><span :class="['inline-flex items-center px-2 py-0.5 rounded text-xs font-medium', priorityColor(tk.priority)]">{{ priorityLabel(tk.priority) }}</span></td>
            <td class="px-4 py-3"><span :class="['inline-flex items-center px-2 py-0.5 rounded text-xs font-medium', statusColor(tk.status)]">{{ statusLabel(tk.status) }}</span></td>
            <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400">{{ formatTimeAgo(tk.createdAt) }}</td>
            <td class="px-4 py-3" @click.stop>
              <button v-if="tk.status === 'Open'" @click="updateStatus(tk.id, 'InProgress')" class="p-1.5 text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg" title="بدء المعالجة">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200 dark:border-gray-700">
        <p class="text-sm text-gray-500">عرض {{ (page - 1) * pageSize + 1 }} - {{ Math.min(page * pageSize, totalCount) }} من {{ totalCount }}</p>
        <div class="flex gap-1">
          <button @click="page--; fetchTickets()" :disabled="page <= 1" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50 hover:bg-gray-50 dark:hover:bg-gray-700">السابق</button>
          <button @click="page++; fetchTickets()" :disabled="page >= totalPages" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50 hover:bg-gray-50 dark:hover:bg-gray-700">التالي</button>
        </div>
      </div>
    </div>

    <!-- Detail Modal -->
    <Teleport to="body">
      <div v-if="showDetailModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showDetailModal = false"></div>
        <div class="relative bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
          <!-- Header -->
          <div class="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
            <div v-if="selectedTicket">
              <div class="flex items-center gap-3">
                <h2 class="text-lg font-bold text-gray-900 dark:text-white">{{ selectedTicket.ticketNumber }}</h2>
                <span :class="['px-2 py-0.5 rounded text-xs font-medium', statusColor(selectedTicket.status)]">{{ statusLabel(selectedTicket.status) }}</span>
                <span :class="['px-2 py-0.5 rounded text-xs font-medium', priorityColor(selectedTicket.priority)]">{{ priorityLabel(selectedTicket.priority) }}</span>
              </div>
              <p class="text-sm text-gray-500 mt-1">{{ selectedTicket.subject }}</p>
            </div>
            <button @click="showDetailModal = false" class="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>
          <div v-if="detailLoading" class="flex-1 flex items-center justify-center p-8"><div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div></div>
          <div v-else-if="selectedTicket" class="flex-1 overflow-y-auto">
            <div class="grid grid-cols-1 lg:grid-cols-3 h-full">
              <!-- Messages -->
              <div class="lg:col-span-2 flex flex-col border-e border-gray-200 dark:border-gray-700">
                <div class="flex-1 overflow-y-auto p-4 space-y-4 max-h-[400px]">
                  <div v-for="msg in selectedTicket.messages" :key="msg.id" :class="['flex gap-3', msg.isOperatorMessage ? 'flex-row-reverse' : '']">
                    <div :class="['w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 text-xs font-bold', msg.isOperatorMessage ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/30' : 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300']">
                      {{ msg.isAiGenerated ? 'AI' : msg.senderName?.charAt(0) || '?' }}
                    </div>
                    <div :class="['max-w-[75%] rounded-xl p-3', msg.isOperatorMessage ? 'bg-primary-50 dark:bg-primary-900/20 border border-primary-200 dark:border-primary-800' : 'bg-gray-100 dark:bg-gray-700 border border-gray-200 dark:border-gray-600']">
                      <div class="flex items-center gap-2 mb-1">
                        <span class="text-xs font-medium text-gray-700 dark:text-gray-300">{{ msg.senderName }}</span>
                        <span v-if="msg.isAiGenerated" class="px-1.5 py-0.5 rounded text-[10px] font-medium bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-400">AI</span>
                        <span class="text-[10px] text-gray-400">{{ formatTimeAgo(msg.createdAt) }}</span>
                      </div>
                      <p class="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap">{{ msg.content }}</p>
                    </div>
                  </div>
                </div>
                <!-- Input -->
                <div class="p-4 border-t border-gray-200 dark:border-gray-700">
                  <div class="flex gap-2 mb-2">
                    <button @click="aiReply" :disabled="aiGeneratingReply" class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-violet-700 bg-violet-50 hover:bg-violet-100 dark:bg-violet-900/20 dark:text-violet-400 rounded-lg disabled:opacity-50">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>
                      {{ aiGeneratingReply ? 'جاري التوليد...' : 'اقتراح رد ذكي' }}
                    </button>
                  </div>
                  <div class="flex gap-2">
                    <textarea v-model="newMessage" rows="2" placeholder="اكتب ردك هنا..." class="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm resize-none focus:ring-2 focus:ring-primary-500"></textarea>
                    <button @click="sendMsg" :disabled="!newMessage.trim() || sendingMessage" class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 self-end">
                      <svg class="w-5 h-5 rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" /></svg>
                    </button>
                  </div>
                </div>
              </div>
              <!-- Sidebar -->
              <div class="p-4 space-y-4 overflow-y-auto max-h-[500px]">
                <div>
                  <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-2">معلومات التذكرة</h3>
                  <dl class="space-y-2 text-sm">
                    <div class="flex justify-between"><dt class="text-gray-500">المُنشئ</dt><dd class="text-gray-900 dark:text-white">{{ selectedTicket.createdByUserName }}</dd></div>
                    <div class="flex justify-between"><dt class="text-gray-500">البريد</dt><dd class="text-gray-900 dark:text-white text-xs">{{ selectedTicket.createdByUserEmail }}</dd></div>
                    <div class="flex justify-between"><dt class="text-gray-500">الجهة</dt><dd class="text-gray-900 dark:text-white">{{ selectedTicket.tenantName || '-' }}</dd></div>
                    <div class="flex justify-between"><dt class="text-gray-500">التصنيف</dt><dd class="text-gray-900 dark:text-white">{{ categoryLabel(selectedTicket.category) }}</dd></div>
                    <div class="flex justify-between"><dt class="text-gray-500">المسؤول</dt><dd class="text-gray-900 dark:text-white">{{ selectedTicket.assignedToUserName || 'غير مُعيّن' }}</dd></div>
                    <div class="flex justify-between"><dt class="text-gray-500">الإنشاء</dt><dd class="text-gray-900 dark:text-white text-xs">{{ formatDate(selectedTicket.createdAt) }}</dd></div>
                  </dl>
                </div>
                <div class="space-y-2">
                  <h3 class="text-sm font-semibold text-gray-900 dark:text-white">الإجراءات</h3>
                  <button @click="showAssignModal = true" class="w-full px-3 py-2 text-sm text-start bg-blue-50 hover:bg-blue-100 dark:bg-blue-900/20 text-blue-700 dark:text-blue-400 rounded-lg">تعيين مسؤول</button>
                  <button v-if="['Open','InProgress','WaitingForCustomer','WaitingForOperator'].includes(selectedTicket.status)" @click="resolveTicket" class="w-full px-3 py-2 text-sm text-start bg-green-50 hover:bg-green-100 dark:bg-green-900/20 text-green-700 dark:text-green-400 rounded-lg">حل التذكرة</button>
                  <button v-if="selectedTicket.status === 'Resolved'" @click="updateStatus(selectedTicket.id, 'Closed')" class="w-full px-3 py-2 text-sm text-start bg-gray-50 hover:bg-gray-100 dark:bg-gray-900/20 text-gray-700 dark:text-gray-400 rounded-lg">إغلاق التذكرة</button>
                </div>
                <!-- AI -->
                <div>
                  <div class="flex items-center justify-between mb-2">
                    <h3 class="text-sm font-semibold text-gray-900 dark:text-white">تحليل الذكاء الاصطناعي</h3>
                    <button @click="aiAnalyze" :disabled="aiAnalyzing" class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-violet-700 bg-violet-50 hover:bg-violet-100 dark:bg-violet-900/20 dark:text-violet-400 rounded-lg disabled:opacity-50">
                      <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>
                      {{ aiAnalyzing ? 'جاري...' : 'تحليل' }}
                    </button>
                  </div>
                  <div v-if="selectedTicket.aiSummary" class="space-y-2 text-xs">
                    <div class="p-2 bg-violet-50 dark:bg-violet-900/20 rounded-lg">
                      <p class="font-medium text-violet-700 dark:text-violet-400 mb-1">الملخص</p>
                      <p class="text-gray-700 dark:text-gray-300">{{ selectedTicket.aiSummary }}</p>
                    </div>
                    <div class="flex gap-2 flex-wrap">
                      <span v-if="selectedTicket.aiSentiment" :class="['px-2 py-0.5 rounded text-[10px] font-medium', selectedTicket.aiSentiment === 'إيجابي' ? 'bg-green-100 text-green-700' : selectedTicket.aiSentiment === 'سلبي' ? 'bg-red-100 text-red-700' : 'bg-gray-100 text-gray-700']">{{ selectedTicket.aiSentiment }}</span>
                      <span v-if="selectedTicket.aiSuggestedPriority" class="px-2 py-0.5 rounded text-[10px] font-medium bg-orange-100 text-orange-700">أولوية: {{ priorityLabel(selectedTicket.aiSuggestedPriority) }}</span>
                    </div>
                    <div v-if="selectedTicket.aiSuggestedResolution" class="p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
                      <p class="font-medium text-green-700 dark:text-green-400 mb-1">الحل المقترح</p>
                      <p class="text-gray-700 dark:text-gray-300">{{ selectedTicket.aiSuggestedResolution }}</p>
                    </div>
                  </div>
                  <p v-else class="text-xs text-gray-400">اضغط "تحليل" للحصول على تحليل ذكي</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Assign Modal -->
    <Teleport to="body">
      <div v-if="showAssignModal" class="fixed inset-0 z-[60] flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showAssignModal = false"></div>
        <div class="relative bg-white dark:bg-gray-800 rounded-xl shadow-2xl w-full max-w-sm p-6">
          <h3 class="text-lg font-bold text-gray-900 dark:text-white mb-4">تعيين مسؤول</h3>
          <input v-model="assignUserName" type="text" placeholder="اسم المسؤول" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm mb-4" />
          <div class="flex gap-2 justify-end">
            <button @click="showAssignModal = false" class="px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-lg">إلغاء</button>
            <button @click="assignTicket" :disabled="!assignUserName" class="px-4 py-2 text-sm bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50">تعيين</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
