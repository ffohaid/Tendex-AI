<script setup lang="ts">
/**
 * Operator Audit Log View.
 * Displays a comprehensive, immutable audit trail of all platform activities.
 * All data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, ref, computed, watch } from 'vue'
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
const filterAction = ref('')
const filterSeverity = ref('')
const searchQuery = ref('')

const actionTypes = [
  { value: '', label: 'جميع الإجراءات' },
  { value: 'Login', label: 'تسجيل دخول' },
  { value: 'Logout', label: 'تسجيل خروج' },
  { value: 'CreateTenant', label: 'إنشاء جهة' },
  { value: 'UpdateTenant', label: 'تحديث جهة' },
  { value: 'ProvisionTenant', label: 'تفعيل جهة' },
  { value: 'CreateUser', label: 'إنشاء مستخدم' },
  { value: 'UpdateUser', label: 'تحديث مستخدم' },
  { value: 'CreateRFP', label: 'إنشاء كراسة شروط' },
  { value: 'ApproveRFP', label: 'اعتماد كراسة شروط' },
  { value: 'CreateCommittee', label: 'إنشاء لجنة' },
  { value: 'ChangeRole', label: 'تغيير صلاحية' },
  { value: 'SystemConfig', label: 'تعديل إعدادات' },
  { value: 'SupportTicket', label: 'تذكرة دعم فني' },
]

const severityOptions = [
  { value: '', label: 'جميع المستويات' },
  { value: 'info', label: 'معلومات' },
  { value: 'warning', label: 'تحذير' },
  { value: 'error', label: 'خطأ' },
  { value: 'critical', label: 'حرج' },
]

const severityColor = (s: string) => ({
  info: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
  warning: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400',
  error: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
  critical: 'bg-red-200 text-red-900 dark:bg-red-900/50 dark:text-red-300',
}[s] || 'bg-gray-100 text-gray-800')

const severityIcon = (s: string) => ({
  info: 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
  warning: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z',
  error: 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
  critical: 'M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636',
}[s] || 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z')

const actionLabel = (a: string) => actionTypes.find(o => o.value === a)?.label || a
const severityLabel = (s: string) => severityOptions.find(o => o.value === s)?.label || s

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
    if (filterAction.value) p.append('action', filterAction.value)
    if (filterSeverity.value) p.append('severity', filterSeverity.value)
    if (searchQuery.value) p.append('search', searchQuery.value)
    const r = await http.get(`/api/v1/audit-logs?${p}`)
    logs.value = r.data.items || []
    totalCount.value = r.data.totalCount || 0
    totalPages.value = r.data.totalPages || 0
  } catch { logs.value = [] }
  finally { loading.value = false }
}

async function exportLogs() {
  try {
    const r = await http.get('/api/v1/audit-logs/export', { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([r.data]))
    const a = document.createElement('a')
    a.href = url
    a.setAttribute('download', `audit-log-${new Date().toISOString().split('T')[0]}.csv`)
    document.body.appendChild(a)
    a.click()
    a.remove()
  } catch { console.warn('Export not available') }
}

watch([filterAction, filterSeverity, searchQuery], () => { page.value = 1; fetchLogs() })
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
      <button @click="exportLogs" class="inline-flex items-center gap-2 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
        تصدير
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <input v-model="searchQuery" type="text" placeholder="بحث بالاسم أو البريد أو التفاصيل..." class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
        <select v-model="filterAction" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in actionTypes" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
        <select v-model="filterSeverity" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
          <option v-for="o in severityOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
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
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">المستوى</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الإجراء</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">المستخدم</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">الجهة</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">التفاصيل</th>
            <th class="px-4 py-3 text-start text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">التوقيت</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="log in logs" :key="log.id" @click="selectedLog = log; showDetailModal = true" class="hover:bg-gray-50 dark:hover:bg-gray-700/30 cursor-pointer transition-colors">
            <td class="px-4 py-3">
              <div class="flex items-center gap-2">
                <svg :class="['w-4 h-4', log.severity === 'error' || log.severity === 'critical' ? 'text-red-500' : log.severity === 'warning' ? 'text-amber-500' : 'text-blue-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="severityIcon(log.severity)" /></svg>
                <span :class="['px-2 py-0.5 rounded text-xs font-medium', severityColor(log.severity)]">{{ severityLabel(log.severity) }}</span>
              </div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-900 dark:text-white font-medium">{{ actionLabel(log.action) }}</td>
            <td class="px-4 py-3">
              <div class="text-sm text-gray-900 dark:text-white">{{ log.userName }}</div>
              <div class="text-xs text-gray-500">{{ log.userEmail }}</div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{{ log.tenantName || '-' }}</td>
            <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 max-w-xs truncate">{{ log.details }}</td>
            <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400">{{ formatTimeAgo(log.timestamp) }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200 dark:border-gray-700">
        <p class="text-sm text-gray-500">عرض {{ (page - 1) * pageSize + 1 }} - {{ Math.min(page * pageSize, totalCount) }} من {{ totalCount }}</p>
        <div class="flex gap-1">
          <button @click="page--; fetchLogs()" :disabled="page <= 1" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50 hover:bg-gray-50">السابق</button>
          <button @click="page++; fetchLogs()" :disabled="page >= totalPages" class="px-3 py-1 text-sm border rounded-lg disabled:opacity-50 hover:bg-gray-50">التالي</button>
        </div>
      </div>
    </div>

    <!-- Detail Modal -->
    <Teleport to="body">
      <div v-if="showDetailModal && selectedLog" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-black/50" @click="showDetailModal = false"></div>
        <div class="relative bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-lg p-6">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-bold text-gray-900 dark:text-white">تفاصيل السجل</h2>
            <button @click="showDetailModal = false" class="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>
          <dl class="space-y-3 text-sm">
            <div class="flex justify-between"><dt class="text-gray-500">الإجراء</dt><dd class="text-gray-900 dark:text-white font-medium">{{ actionLabel(selectedLog.action) }}</dd></div>
            <div class="flex justify-between"><dt class="text-gray-500">المستوى</dt><dd><span :class="['px-2 py-0.5 rounded text-xs font-medium', severityColor(selectedLog.severity)]">{{ severityLabel(selectedLog.severity) }}</span></dd></div>
            <div class="flex justify-between"><dt class="text-gray-500">المستخدم</dt><dd class="text-gray-900 dark:text-white">{{ selectedLog.userName }}</dd></div>
            <div class="flex justify-between"><dt class="text-gray-500">البريد</dt><dd class="text-gray-900 dark:text-white">{{ selectedLog.userEmail }}</dd></div>
            <div class="flex justify-between"><dt class="text-gray-500">الجهة</dt><dd class="text-gray-900 dark:text-white">{{ selectedLog.tenantName || '-' }}</dd></div>
            <div class="flex justify-between"><dt class="text-gray-500">IP</dt><dd class="text-gray-900 dark:text-white font-mono text-xs">{{ selectedLog.ipAddress }}</dd></div>
            <div class="flex justify-between"><dt class="text-gray-500">التوقيت</dt><dd class="text-gray-900 dark:text-white text-xs">{{ formatDate(selectedLog.timestamp) }}</dd></div>
            <div>
              <dt class="text-gray-500 mb-1">التفاصيل</dt>
              <dd class="text-gray-900 dark:text-white p-3 bg-gray-50 dark:bg-gray-700 rounded-lg text-xs">{{ selectedLog.details }}</dd>
            </div>
            <div v-if="selectedLog.userAgent">
              <dt class="text-gray-500 mb-1">المتصفح</dt>
              <dd class="text-gray-700 dark:text-gray-300 text-xs">{{ selectedLog.userAgent }}</dd>
            </div>
          </dl>
        </div>
      </div>
    </Teleport>
  </div>
</template>
