<script setup lang="ts">
/**
 * Operator Audit Log View.
 * Displays a comprehensive, immutable audit trail of all platform activities.
 * All data is fetched dynamically from the API — NO mock data.
 * Fields aligned with AuditLogEntry entity: id, timestampUtc, userId, userName,
 * ipAddress, actionType, entityType, entityId, oldValues, newValues, reason, sessionId, tenantId.
 */
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '@/services/http'

const { t } = useI18n()

const logs = ref<any[]>([])
const loading = ref(false)
const page = ref(1)
const pageSize = ref(25)
const totalCount = ref(0)
const totalPages = ref(0)

const selectedLog = ref<any>(null)
const showDetailModal = ref(false)

// Filters
const filterActionType = ref<number | ''>('')
const filterEntityType = ref('')
const dateFrom = ref('')
const dateTo = ref('')

// Action types from AuditActionType enum
const actionTypes = [
  { value: '', label: 'جميع الإجراءات' },
  { value: 1, label: 'إنشاء' },
  { value: 2, label: 'تحديث' },
  { value: 3, label: 'حذف' },
  { value: 4, label: 'اعتماد' },
  { value: 5, label: 'رفض' },
  { value: 6, label: 'تسجيل دخول' },
  { value: 7, label: 'تسجيل خروج' },
  { value: 8, label: 'وصول' },
  { value: 9, label: 'تصدير' },
  { value: 10, label: 'انتحال هوية' },
  { value: 11, label: 'انتقال حالة' },
]

const entityTypes = [
  { value: '', label: 'جميع الكيانات' },
  { value: 'Tenant', label: 'جهة' },
  { value: 'User', label: 'مستخدم' },
  { value: 'Role', label: 'دور' },
  { value: 'Rfp', label: 'كراسة شروط' },
  { value: 'Committee', label: 'لجنة' },
  { value: 'SupportTicket', label: 'تذكرة دعم' },
  { value: 'AiConfiguration', label: 'إعدادات الذكاء الاصطناعي' },
  { value: 'PurchaseOrder', label: 'أمر شراء' },
]

const actionTypeLabel = (at: string | number) => {
  // Handle both string enum name and numeric value
  const nameMap: Record<string, string> = {
    Create: 'إنشاء', Update: 'تحديث', Delete: 'حذف',
    Approve: 'اعتماد', Reject: 'رفض', Login: 'تسجيل دخول',
    Logout: 'تسجيل خروج', Access: 'وصول', Export: 'تصدير',
    Impersonate: 'انتحال هوية', StateTransition: 'انتقال حالة',
  }
  if (typeof at === 'string' && nameMap[at]) return nameMap[at]
  const numMap: Record<number, string> = {
    1: 'إنشاء', 2: 'تحديث', 3: 'حذف', 4: 'اعتماد', 5: 'رفض',
    6: 'تسجيل دخول', 7: 'تسجيل خروج', 8: 'وصول', 9: 'تصدير',
    10: 'انتحال هوية', 11: 'انتقال حالة',
  }
  if (typeof at === 'number' && numMap[at]) return numMap[at]
  return String(at)
}

const entityTypeLabel = (et: string) => {
  const map: Record<string, string> = {
    Tenant: 'جهة', User: 'مستخدم', Role: 'دور', Rfp: 'كراسة شروط',
    Committee: 'لجنة', SupportTicket: 'تذكرة دعم', AiConfiguration: 'إعدادات AI',
    PurchaseOrder: 'أمر شراء', Permission: 'صلاحية', Session: 'جلسة',
  }
  return map[et] || et
}

const actionTypeColor = (at: string | number) => {
  const colorMap: Record<string, string> = {
    Create: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
    Update: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
    Delete: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
    Approve: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400',
    Reject: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
    Login: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-400',
    Logout: 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400',
    Access: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-900/30 dark:text-cyan-400',
    Export: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400',
    Impersonate: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400',
    StateTransition: 'bg-teal-100 text-teal-800 dark:bg-teal-900/30 dark:text-teal-400',
  }
  // Handle numeric values
  const numToName: Record<number, string> = {
    1: 'Create', 2: 'Update', 3: 'Delete', 4: 'Approve', 5: 'Reject',
    6: 'Login', 7: 'Logout', 8: 'Access', 9: 'Export', 10: 'Impersonate', 11: 'StateTransition',
  }
  const name = typeof at === 'number' ? numToName[at] : at
  return colorMap[name] || 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400'
}

const actionTypeIcon = (at: string | number) => {
  const iconMap: Record<string, string> = {
    Create: 'M12 4v16m8-8H4',
    Update: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
    Delete: 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16',
    Approve: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
    Reject: 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
    Login: 'M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1',
    Logout: 'M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1',
    Access: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z',
    Export: 'M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    Impersonate: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z',
    StateTransition: 'M13 9l3 3m0 0l-3 3m3-3H8m13 0a9 9 0 11-18 0 9 9 0 0118 0z',
  }
  const numToName: Record<number, string> = {
    1: 'Create', 2: 'Update', 3: 'Delete', 4: 'Approve', 5: 'Reject',
    6: 'Login', 7: 'Logout', 8: 'Access', 9: 'Export', 10: 'Impersonate', 11: 'StateTransition',
  }
  const name = typeof at === 'number' ? numToName[at] : at
  return iconMap[name] || 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
}

const formatDate = (d: string) => d ? new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '-'

const formatTimeAgo = (d: string) => {
  if (!d) return '-'
  const ms = Date.now() - new Date(d).getTime()
  const m = Math.floor(ms / 60000), h = Math.floor(ms / 3600000), dy = Math.floor(ms / 86400000)
  if (m < 1) return 'الآن'
  if (m < 60) return `منذ ${m} دقيقة`
  if (h < 24) return `منذ ${h} ساعة`
  return `منذ ${dy} يوم`
}

async function fetchLogs() {
  loading.value = true
  try {
    const p = new URLSearchParams({ page: page.value.toString(), pageSize: pageSize.value.toString() })
    if (filterActionType.value !== '') p.append('actionType', filterActionType.value.toString())
    if (filterEntityType.value) p.append('entityType', filterEntityType.value)
    if (dateFrom.value) p.append('fromUtc', new Date(dateFrom.value).toISOString())
    if (dateTo.value) p.append('toUtc', new Date(dateTo.value).toISOString())
    const r = await http.get(`/v1/audit-logs?${p}`)
    logs.value = r.data.items || []
    totalCount.value = r.data.totalCount || 0
    totalPages.value = r.data.totalPages || 0
  } catch { logs.value = [] }
  finally { loading.value = false }
}

async function exportLogs() {
  try {
    const p = new URLSearchParams()
    if (filterActionType.value !== '') p.append('actionType', filterActionType.value.toString())
    if (filterEntityType.value) p.append('entityType', filterEntityType.value)
    if (dateFrom.value) p.append('fromUtc', new Date(dateFrom.value).toISOString())
    if (dateTo.value) p.append('toUtc', new Date(dateTo.value).toISOString())
    const r = await http.get(`/v1/audit-logs/export?${p}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([r.data]))
    const a = document.createElement('a')
    a.href = url
    a.setAttribute('download', `audit-log-${new Date().toISOString().split('T')[0]}.csv`)
    document.body.appendChild(a)
    a.click()
    a.remove()
    window.URL.revokeObjectURL(url)
  } catch { console.warn('Export not available') }
}

function parseJsonSafe(json: string | null | undefined) {
  if (!json) return null
  try { return JSON.parse(json) } catch { return null }
}

watch([filterActionType, filterEntityType, dateFrom, dateTo], () => { page.value = 1; fetchLogs() })
onMounted(() => fetchLogs())
</script>

<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">سجل التدقيق</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">سجل شامل وغير قابل للتعديل لجميع أنشطة المنصة</p>
      </div>
      <div class="flex gap-2">
        <button @click="fetchLogs" class="inline-flex items-center gap-2 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
          تحديث
        </button>
        <button @click="exportLogs" class="inline-flex items-center gap-2 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
          تصدير CSV
        </button>
      </div>
    </div>

    <!-- Stats Summary -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
            <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
          </div>
          <div>
            <p class="text-xs text-gray-500 dark:text-gray-400">إجمالي السجلات</p>
            <p class="text-xl font-bold text-blue-600">{{ totalCount }}</p>
          </div>
        </div>
      </div>
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-green-100 dark:bg-green-900/30 flex items-center justify-center">
            <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
          </div>
          <div>
            <p class="text-xs text-gray-500 dark:text-gray-400">عمليات الإنشاء</p>
            <p class="text-xl font-bold text-green-600">{{ logs.filter(l => l.actionType === 'Create' || l.actionType === 1).length }}</p>
          </div>
        </div>
      </div>
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center">
            <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" /></svg>
          </div>
          <div>
            <p class="text-xs text-gray-500 dark:text-gray-400">عمليات الدخول</p>
            <p class="text-xl font-bold text-indigo-600">{{ logs.filter(l => l.actionType === 'Login' || l.actionType === 6).length }}</p>
          </div>
        </div>
      </div>
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-red-100 dark:bg-red-900/30 flex items-center justify-center">
            <svg class="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
          </div>
          <div>
            <p class="text-xs text-gray-500 dark:text-gray-400">عمليات الحذف</p>
            <p class="text-xl font-bold text-red-600">{{ logs.filter(l => l.actionType === 'Delete' || l.actionType === 3).length }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <select v-model="filterActionType" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in actionTypes" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
        <select v-model="filterEntityType" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in entityTypes" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
        <input v-model="dateFrom" type="date" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" placeholder="من تاريخ" />
        <input v-model="dateTo" type="date" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" placeholder="إلى تاريخ" />
      </div>
    </div>

    <!-- Logs Table -->
    <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div v-if="loading" class="p-8 text-center">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto"></div>
        <p class="mt-2 text-sm text-gray-500">جاري التحميل...</p>
      </div>
      <div v-else-if="logs.length === 0" class="p-8 text-center">
        <svg class="w-12 h-12 text-gray-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
        <p class="text-gray-500 dark:text-gray-400">لا توجد سجلات تدقيق</p>
      </div>
      <table v-else class="w-full">
        <thead class="bg-gray-50 dark:bg-gray-700/50">
          <tr>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الإجراء</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الكيان</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">المستخدم</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">عنوان IP</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">السبب</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">التوقيت</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="log in logs" :key="log.id" @click="selectedLog = log; showDetailModal = true" class="hover:bg-gray-50 dark:hover:bg-gray-700/30 cursor-pointer transition-colors">
            <td class="px-4 py-3">
              <div class="flex items-center gap-2">
                <svg :class="['w-4 h-4', actionTypeColor(log.actionType).includes('red') ? 'text-red-500' : actionTypeColor(log.actionType).includes('green') ? 'text-green-500' : 'text-blue-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="actionTypeIcon(log.actionType)" /></svg>
                <span :class="['px-2 py-0.5 rounded text-xs font-medium', actionTypeColor(log.actionType)]">{{ actionTypeLabel(log.actionType) }}</span>
              </div>
            </td>
            <td class="px-4 py-3">
              <div class="text-sm font-medium text-gray-900 dark:text-white">{{ entityTypeLabel(log.entityType) }}</div>
              <div class="text-xs text-gray-500 font-mono">{{ log.entityId?.substring(0, 8) }}...</div>
            </td>
            <td class="px-4 py-3">
              <div class="text-sm text-gray-900 dark:text-white">{{ log.userName }}</div>
            </td>
            <td class="px-4 py-3 text-xs font-mono text-gray-500 dark:text-gray-400">{{ log.ipAddress || '-' }}</td>
            <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 max-w-xs truncate">{{ log.reason || '-' }}</td>
            <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400">{{ formatTimeAgo(log.timestampUtc) }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200 dark:border-gray-700">
        <p class="text-sm text-gray-500">عرض {{ (page - 1) * pageSize + 1 }} - {{ Math.min(page * pageSize, totalCount) }} من {{ totalCount }}</p>
        <div class="flex gap-1">
          <button @click="page--; fetchLogs()" :disabled="page <= 1" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50 hover:bg-gray-50 dark:hover:bg-gray-700 dark:border-gray-600 dark:text-gray-300">السابق</button>
          <span class="px-3 py-1 text-sm text-gray-500">{{ page }} / {{ totalPages }}</span>
          <button @click="page++; fetchLogs()" :disabled="page >= totalPages" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50 hover:bg-gray-50 dark:hover:bg-gray-700 dark:border-gray-600 dark:text-gray-300">التالي</button>
        </div>
      </div>
    </div>

    <!-- Detail Modal -->
    <Teleport to="body">
      <div v-if="showDetailModal && selectedLog" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showDetailModal = false"></div>
        <div class="relative bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[85vh] overflow-hidden flex flex-col">
          <div class="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
            <div class="flex items-center gap-3">
              <h2 class="text-lg font-bold text-gray-900 dark:text-white">تفاصيل السجل</h2>
              <span :class="['px-2 py-0.5 rounded text-xs font-medium', actionTypeColor(selectedLog.actionType)]">{{ actionTypeLabel(selectedLog.actionType) }}</span>
            </div>
            <button @click="showDetailModal = false" class="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>
          <div class="flex-1 overflow-y-auto p-4 space-y-4">
            <dl class="grid grid-cols-2 gap-3 text-sm">
              <div><dt class="text-gray-500 dark:text-gray-400">الإجراء</dt><dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ actionTypeLabel(selectedLog.actionType) }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">الكيان</dt><dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ entityTypeLabel(selectedLog.entityType) }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">معرف الكيان</dt><dd class="text-gray-900 dark:text-white font-mono text-xs mt-0.5">{{ selectedLog.entityId }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">المستخدم</dt><dd class="text-gray-900 dark:text-white mt-0.5">{{ selectedLog.userName }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">معرف المستخدم</dt><dd class="text-gray-900 dark:text-white font-mono text-xs mt-0.5">{{ selectedLog.userId }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">عنوان IP</dt><dd class="text-gray-900 dark:text-white font-mono text-xs mt-0.5">{{ selectedLog.ipAddress || '-' }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">معرف الجلسة</dt><dd class="text-gray-900 dark:text-white font-mono text-xs mt-0.5">{{ selectedLog.sessionId || '-' }}</dd></div>
              <div><dt class="text-gray-500 dark:text-gray-400">معرف الجهة</dt><dd class="text-gray-900 dark:text-white font-mono text-xs mt-0.5">{{ selectedLog.tenantId || '-' }}</dd></div>
              <div class="col-span-2"><dt class="text-gray-500 dark:text-gray-400">التوقيت</dt><dd class="text-gray-900 dark:text-white text-xs mt-0.5">{{ formatDate(selectedLog.timestampUtc) }}</dd></div>
            </dl>

            <!-- Reason -->
            <div v-if="selectedLog.reason">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">السبب</h3>
              <div class="p-3 bg-gray-50 dark:bg-gray-700 rounded-lg text-sm text-gray-700 dark:text-gray-300">{{ selectedLog.reason }}</div>
            </div>

            <!-- Old Values -->
            <div v-if="selectedLog.oldValues">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">القيم السابقة</h3>
              <div class="p-3 bg-red-50 dark:bg-red-900/20 rounded-lg text-xs font-mono text-gray-700 dark:text-gray-300 overflow-x-auto max-h-48 overflow-y-auto">
                <pre class="whitespace-pre-wrap">{{ JSON.stringify(parseJsonSafe(selectedLog.oldValues), null, 2) || selectedLog.oldValues }}</pre>
              </div>
            </div>

            <!-- New Values -->
            <div v-if="selectedLog.newValues">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">القيم الجديدة</h3>
              <div class="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg text-xs font-mono text-gray-700 dark:text-gray-300 overflow-x-auto max-h-48 overflow-y-auto">
                <pre class="whitespace-pre-wrap">{{ JSON.stringify(parseJsonSafe(selectedLog.newValues), null, 2) || selectedLog.newValues }}</pre>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
