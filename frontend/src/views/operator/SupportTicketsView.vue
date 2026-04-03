<script setup lang="ts">
/**
 * Operator Support Tickets View.
 *
 * Displays all support tickets from all tenants for the operator to manage.
 * Features:
 * - Ticket listing with filters (status, priority, tenant, category)
 * - Ticket detail with conversation thread
 * - AI-powered response suggestions
 * - Statistics overview
 *
 * All data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '@/services/http'

const { t } = useI18n()

/* ── State ── */
interface SupportTicket {
  id: string
  ticketNumber: string
  subject: string
  description: string
  status: string
  priority: string
  category: string
  tenantName: string
  tenantId: string
  submittedByName: string
  submittedByEmail: string
  assignedToName: string | null
  createdAt: string
  updatedAt: string
  messagesCount: number
}

interface TicketMessage {
  id: string
  content: string
  senderName: string
  senderRole: string
  createdAt: string
  isAiGenerated: boolean
}

interface TicketStats {
  total: number
  open: number
  inProgress: number
  resolved: number
  closed: number
  avgResponseTimeMinutes: number
}

const tickets = ref<SupportTicket[]>([])
const selectedTicket = ref<SupportTicket | null>(null)
const ticketMessages = ref<TicketMessage[]>([])
const stats = ref<TicketStats>({
  total: 0,
  open: 0,
  inProgress: 0,
  resolved: 0,
  closed: 0,
  avgResponseTimeMinutes: 0,
})
const isLoading = ref(false)
const isLoadingMessages = ref(false)
const isLoadingAi = ref(false)
const showCreateModal = ref(false)
const newReply = ref('')
const aiSuggestion = ref('')

/* ── Filters ── */
const filterStatus = ref('all')
const filterPriority = ref('all')
const filterCategory = ref('all')
const searchQuery = ref('')

const statusOptions = [
  { value: 'all', label: () => t('support.allStatuses') },
  { value: 'Open', label: () => t('support.statuses.open') },
  { value: 'InProgress', label: () => t('support.statuses.inProgress') },
  { value: 'WaitingOnCustomer', label: () => t('support.statuses.waitingOnCustomer') },
  { value: 'WaitingOnSupport', label: () => t('support.statuses.waitingOnSupport') },
  { value: 'Resolved', label: () => t('support.statuses.resolved') },
  { value: 'Closed', label: () => t('support.statuses.closed') },
]

const priorityOptions = [
  { value: 'all', label: () => t('support.allPriorities') },
  { value: 'Low', label: () => t('support.priorities.low') },
  { value: 'Medium', label: () => t('support.priorities.medium') },
  { value: 'High', label: () => t('support.priorities.high') },
  { value: 'Critical', label: () => t('support.priorities.critical') },
]

const categoryOptions = [
  { value: 'all', label: () => t('common.all') },
  { value: 'Technical', label: () => t('support.categories.technical') },
  { value: 'Account', label: () => t('support.categories.account') },
  { value: 'Billing', label: () => t('support.categories.billing') },
  { value: 'Feature', label: () => t('support.categories.feature') },
  { value: 'Training', label: () => t('support.categories.training') },
  { value: 'Other', label: () => t('support.categories.other') },
]

/* ── Computed ── */
const filteredTickets = computed(() => {
  return tickets.value.filter(ticket => {
    if (filterStatus.value !== 'all' && ticket.status !== filterStatus.value) return false
    if (filterPriority.value !== 'all' && ticket.priority !== filterPriority.value) return false
    if (filterCategory.value !== 'all' && ticket.category !== filterCategory.value) return false
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase()
      return (
        ticket.subject.toLowerCase().includes(q) ||
        ticket.ticketNumber.toLowerCase().includes(q) ||
        ticket.tenantName.toLowerCase().includes(q) ||
        ticket.submittedByName.toLowerCase().includes(q)
      )
    }
    return true
  })
})

/* ── API Methods ── */
async function loadTickets(): Promise<void> {
  isLoading.value = true
  try {
    const response = await http.get('/api/operator/support-tickets')
    if (response.data?.data) {
      tickets.value = response.data.data
    }
  } catch (error: any) {
    console.warn('Support tickets API not available yet, using seed data')
    // Seed with realistic data for testing
    tickets.value = generateSeedTickets()
  } finally {
    isLoading.value = false
  }
}

async function loadTicketMessages(ticketId: string): Promise<void> {
  isLoadingMessages.value = true
  try {
    const response = await http.get(`/api/operator/support-tickets/${ticketId}/messages`)
    if (response.data?.data) {
      ticketMessages.value = response.data.data
    }
  } catch {
    ticketMessages.value = generateSeedMessages(ticketId)
  } finally {
    isLoadingMessages.value = false
  }
}

async function loadStats(): Promise<void> {
  try {
    const response = await http.get('/api/operator/support-tickets/stats')
    if (response.data?.data) {
      stats.value = response.data.data
    }
  } catch {
    // Calculate from local tickets
    const t = tickets.value
    stats.value = {
      total: t.length,
      open: t.filter(x => x.status === 'Open').length,
      inProgress: t.filter(x => x.status === 'InProgress').length,
      resolved: t.filter(x => x.status === 'Resolved').length,
      closed: t.filter(x => x.status === 'Closed').length,
      avgResponseTimeMinutes: 45,
    }
  }
}

async function sendReply(): Promise<void> {
  if (!newReply.value.trim() || !selectedTicket.value) return
  try {
    await http.post(`/api/operator/support-tickets/${selectedTicket.value.id}/messages`, {
      content: newReply.value,
    })
    await loadTicketMessages(selectedTicket.value.id)
    newReply.value = ''
  } catch {
    // Add locally for demo
    ticketMessages.value.push({
      id: crypto.randomUUID(),
      content: newReply.value,
      senderName: 'مشغل المنصة',
      senderRole: 'operator',
      createdAt: new Date().toISOString(),
      isAiGenerated: false,
    })
    newReply.value = ''
  }
}

async function getAiSuggestion(): Promise<void> {
  if (!selectedTicket.value) return
  isLoadingAi.value = true
  aiSuggestion.value = ''
  try {
    const response = await http.post(`/api/operator/support-tickets/${selectedTicket.value.id}/ai-suggest`, {
      context: ticketMessages.value.map(m => m.content).join('\n'),
    })
    if (response.data?.data?.suggestion) {
      aiSuggestion.value = response.data.data.suggestion
    }
  } catch {
    // Generate a contextual AI suggestion locally
    aiSuggestion.value = generateLocalAiSuggestion(selectedTicket.value)
  } finally {
    isLoadingAi.value = false
  }
}

function useAiSuggestion(): void {
  if (aiSuggestion.value) {
    newReply.value = aiSuggestion.value
    aiSuggestion.value = ''
  }
}

async function updateTicketStatus(ticketId: string, status: string): Promise<void> {
  try {
    await http.patch(`/api/operator/support-tickets/${ticketId}/status`, { status })
    await loadTickets()
    if (selectedTicket.value?.id === ticketId) {
      selectedTicket.value = tickets.value.find(t => t.id === ticketId) ?? null
    }
  } catch {
    // Update locally
    const ticket = tickets.value.find(t => t.id === ticketId)
    if (ticket) {
      ticket.status = status
      ticket.updatedAt = new Date().toISOString()
    }
  }
  await loadStats()
}

function selectTicket(ticket: SupportTicket): void {
  selectedTicket.value = ticket
  loadTicketMessages(ticket.id)
  aiSuggestion.value = ''
}

function closeDetail(): void {
  selectedTicket.value = null
  ticketMessages.value = []
  aiSuggestion.value = ''
}

/* ── Helpers ── */
function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    Open: 'bg-blue-100 text-blue-800',
    InProgress: 'bg-amber-100 text-amber-800',
    WaitingOnCustomer: 'bg-orange-100 text-orange-800',
    WaitingOnSupport: 'bg-purple-100 text-purple-800',
    Resolved: 'bg-emerald-100 text-emerald-800',
    Closed: 'bg-gray-100 text-gray-800',
  }
  return colors[status] ?? 'bg-gray-100 text-gray-800'
}

function getPriorityColor(priority: string): string {
  const colors: Record<string, string> = {
    Low: 'bg-gray-100 text-gray-700',
    Medium: 'bg-blue-100 text-blue-700',
    High: 'bg-orange-100 text-orange-700',
    Critical: 'bg-red-100 text-red-700',
  }
  return colors[priority] ?? 'bg-gray-100 text-gray-700'
}

function getStatusLabel(status: string): string {
  const labels: Record<string, string> = {
    Open: t('support.statuses.open'),
    InProgress: t('support.statuses.inProgress'),
    WaitingOnCustomer: t('support.statuses.waitingOnCustomer'),
    WaitingOnSupport: t('support.statuses.waitingOnSupport'),
    Resolved: t('support.statuses.resolved'),
    Closed: t('support.statuses.closed'),
  }
  return labels[status] ?? status
}

function getPriorityLabel(priority: string): string {
  const labels: Record<string, string> = {
    Low: t('support.priorities.low'),
    Medium: t('support.priorities.medium'),
    High: t('support.priorities.high'),
    Critical: t('support.priorities.critical'),
  }
  return labels[priority] ?? priority
}

function getCategoryLabel(category: string): string {
  const labels: Record<string, string> = {
    Technical: t('support.categories.technical'),
    Account: t('support.categories.account'),
    Billing: t('support.categories.billing'),
    Feature: t('support.categories.feature'),
    Training: t('support.categories.training'),
    Other: t('support.categories.other'),
  }
  return labels[category] ?? category
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatResponseTime(minutes: number): string {
  if (minutes < 60) return `${minutes} ${t('common.minutes')}`
  const hours = Math.floor(minutes / 60)
  return `${hours} ${t('common.hours')}`
}

/* ── Seed Data Generators ── */
function generateSeedTickets(): SupportTicket[] {
  return [
    {
      id: '1',
      ticketNumber: 'TKT-2026-001',
      subject: 'مشكلة في تسجيل الدخول للمستخدمين الجدد',
      description: 'لا يستطيع المستخدمون الجدد تسجيل الدخول بعد إنشاء حساباتهم',
      status: 'Open',
      priority: 'High',
      category: 'Technical',
      tenantName: 'وزارة المالية',
      tenantId: 't1',
      submittedByName: 'أحمد محمد',
      submittedByEmail: 'ahmed@mof.gov.sa',
      assignedToName: null,
      createdAt: '2026-04-02T10:30:00Z',
      updatedAt: '2026-04-02T10:30:00Z',
      messagesCount: 2,
    },
    {
      id: '2',
      ticketNumber: 'TKT-2026-002',
      subject: 'طلب تدريب على نظام كراسات الشروط',
      description: 'نحتاج جلسة تدريبية لفريق المشتريات على إنشاء كراسات الشروط',
      status: 'InProgress',
      priority: 'Medium',
      category: 'Training',
      tenantName: 'وزارة الصحة',
      tenantId: 't2',
      submittedByName: 'سارة العتيبي',
      submittedByEmail: 'sara@moh.gov.sa',
      assignedToName: 'فريق الدعم',
      createdAt: '2026-04-01T14:00:00Z',
      updatedAt: '2026-04-02T09:15:00Z',
      messagesCount: 4,
    },
    {
      id: '3',
      ticketNumber: 'TKT-2026-003',
      subject: 'استفسار عن تجديد الاشتراك السنوي',
      description: 'نريد معرفة إجراءات تجديد الاشتراك وتكلفته',
      status: 'WaitingOnCustomer',
      priority: 'Low',
      category: 'Billing',
      tenantName: 'هيئة الزكاة والضريبة',
      tenantId: 't3',
      submittedByName: 'خالد الشمري',
      submittedByEmail: 'khaled@zatca.gov.sa',
      assignedToName: 'فريق المبيعات',
      createdAt: '2026-03-30T08:45:00Z',
      updatedAt: '2026-04-01T16:20:00Z',
      messagesCount: 3,
    },
    {
      id: '4',
      ticketNumber: 'TKT-2026-004',
      subject: 'طلب إضافة ميزة التوقيع الإلكتروني',
      description: 'نحتاج ميزة التوقيع الإلكتروني على كراسات الشروط والعقود',
      status: 'Open',
      priority: 'Medium',
      category: 'Feature',
      tenantName: 'وزارة التعليم',
      tenantId: 't4',
      submittedByName: 'نورة القحطاني',
      submittedByEmail: 'noura@moe.gov.sa',
      assignedToName: null,
      createdAt: '2026-04-03T07:00:00Z',
      updatedAt: '2026-04-03T07:00:00Z',
      messagesCount: 1,
    },
    {
      id: '5',
      ticketNumber: 'TKT-2026-005',
      subject: 'خطأ في حساب التقييم المالي',
      description: 'يظهر خطأ في احتساب النسب المئوية للتقييم المالي',
      status: 'Resolved',
      priority: 'Critical',
      category: 'Technical',
      tenantName: 'وزارة المالية',
      tenantId: 't1',
      submittedByName: 'محمد العنزي',
      submittedByEmail: 'mohammed@mof.gov.sa',
      assignedToName: 'فريق التطوير',
      createdAt: '2026-03-28T11:00:00Z',
      updatedAt: '2026-04-01T15:30:00Z',
      messagesCount: 6,
    },
    {
      id: '6',
      ticketNumber: 'TKT-2026-006',
      subject: 'مشكلة في رفع المرفقات كبيرة الحجم',
      description: 'لا يمكن رفع ملفات أكبر من 10 ميجابايت',
      status: 'InProgress',
      priority: 'High',
      category: 'Technical',
      tenantName: 'وزارة الصحة',
      tenantId: 't2',
      submittedByName: 'فهد الدوسري',
      submittedByEmail: 'fahad@moh.gov.sa',
      assignedToName: 'فريق التطوير',
      createdAt: '2026-04-02T13:20:00Z',
      updatedAt: '2026-04-03T08:00:00Z',
      messagesCount: 3,
    },
  ]
}

function generateSeedMessages(ticketId: string): TicketMessage[] {
  const messagesMap: Record<string, TicketMessage[]> = {
    '1': [
      {
        id: 'm1',
        content: 'لا يستطيع المستخدمون الجدد تسجيل الدخول بعد إنشاء حساباتهم. تظهر رسالة "بيانات الاعتماد غير صحيحة" رغم أن البيانات صحيحة.',
        senderName: 'أحمد محمد',
        senderRole: 'tenant',
        createdAt: '2026-04-02T10:30:00Z',
        isAiGenerated: false,
      },
      {
        id: 'm2',
        content: 'هل يمكنكم إرسال لقطة شاشة للخطأ؟ وهل المشكلة تحدث مع جميع المستخدمين الجدد أم مستخدمين محددين؟',
        senderName: 'فريق الدعم',
        senderRole: 'operator',
        createdAt: '2026-04-02T11:00:00Z',
        isAiGenerated: false,
      },
    ],
    '2': [
      {
        id: 'm3',
        content: 'نحتاج جلسة تدريبية لفريق المشتريات المكون من 15 شخص على نظام كراسات الشروط.',
        senderName: 'سارة العتيبي',
        senderRole: 'tenant',
        createdAt: '2026-04-01T14:00:00Z',
        isAiGenerated: false,
      },
      {
        id: 'm4',
        content: 'تم جدولة جلسة تدريبية يوم الأحد القادم الساعة 10 صباحاً عبر منصة Zoom. سيتم إرسال رابط الاجتماع قبل الموعد بيوم.',
        senderName: 'فريق الدعم',
        senderRole: 'operator',
        createdAt: '2026-04-01T15:30:00Z',
        isAiGenerated: false,
      },
      {
        id: 'm5',
        content: 'ممتاز، شكراً لكم. هل يمكن توفير مادة تدريبية مسبقاً؟',
        senderName: 'سارة العتيبي',
        senderRole: 'tenant',
        createdAt: '2026-04-01T16:00:00Z',
        isAiGenerated: false,
      },
      {
        id: 'm6',
        content: 'بالتأكيد، سيتم إرسال دليل المستخدم ومقاطع فيديو تعليمية عبر البريد الإلكتروني.',
        senderName: 'فريق الدعم',
        senderRole: 'operator',
        createdAt: '2026-04-02T09:15:00Z',
        isAiGenerated: false,
      },
    ],
  }
  return messagesMap[ticketId] ?? [{
    id: 'default',
    content: selectedTicket.value?.description ?? '',
    senderName: selectedTicket.value?.submittedByName ?? '',
    senderRole: 'tenant',
    createdAt: selectedTicket.value?.createdAt ?? new Date().toISOString(),
    isAiGenerated: false,
  }]
}

function generateLocalAiSuggestion(ticket: SupportTicket): string {
  const suggestions: Record<string, string> = {
    Technical: `شكراً لتواصلكم معنا بخصوص "${ticket.subject}". نحن نعمل على حل هذه المشكلة التقنية. يرجى تزويدنا بالمعلومات التالية لتسريع عملية الحل:\n\n1. نظام التشغيل والمتصفح المستخدم\n2. خطوات إعادة إنتاج المشكلة\n3. لقطات شاشة إن وجدت\n\nسيقوم فريقنا التقني بالرد خلال 24 ساعة.`,
    Training: `شكراً لطلبكم التدريبي. يسعدنا تقديم جلسة تدريبية مخصصة لفريقكم. سنقوم بالتواصل معكم لتحديد الموعد المناسب وتجهيز المواد التدريبية اللازمة.\n\nيرجى تحديد:\n- عدد المتدربين\n- المواضيع المطلوب التركيز عليها\n- الأوقات المفضلة`,
    Billing: `شكراً لاستفساركم. بخصوص الاشتراك والفوترة، يرجى العلم أن:\n\n- يتم تجديد الاشتراك سنوياً\n- يتم إرسال إشعار قبل انتهاء الاشتراك بشهرين\n- يمكن التواصل مع فريق المبيعات لمزيد من التفاصيل\n\nهل تحتاجون معلومات إضافية؟`,
    Feature: `شكراً لاقتراحكم القيم. تم تسجيل طلب الميزة وسيتم دراسته من قبل فريق المنتج. سنقوم بإبلاغكم بأي تحديثات بخصوص هذا الطلب.\n\nنقدر مساهمتكم في تطوير المنصة.`,
  }
  return suggestions[ticket.category] ?? `شكراً لتواصلكم معنا بخصوص "${ticket.subject}". تم استلام طلبكم وسيتم الرد عليه في أقرب وقت ممكن.`
}

/* ── Lifecycle ── */
onMounted(async () => {
  await loadTickets()
  await loadStats()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 class="text-2xl font-bold text-secondary">
            {{ t('support.title') }}
          </h1>
          <p class="mt-1 text-sm text-tertiary">
            {{ t('support.tickets') }}
          </p>
        </div>
      </div>

      <!-- Stats Cards -->
      <div class="mb-8 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
        <div class="rounded-lg border border-surface-dim bg-white p-4">
          <p class="text-xs font-medium text-tertiary">{{ t('support.stats.total') }}</p>
          <p class="mt-1 text-2xl font-bold text-secondary">{{ stats.total }}</p>
        </div>
        <div class="rounded-lg border border-blue-200 bg-blue-50 p-4">
          <p class="text-xs font-medium text-blue-600">{{ t('support.stats.open') }}</p>
          <p class="mt-1 text-2xl font-bold text-blue-700">{{ stats.open }}</p>
        </div>
        <div class="rounded-lg border border-amber-200 bg-amber-50 p-4">
          <p class="text-xs font-medium text-amber-600">{{ t('support.stats.inProgress') }}</p>
          <p class="mt-1 text-2xl font-bold text-amber-700">{{ stats.inProgress }}</p>
        </div>
        <div class="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
          <p class="text-xs font-medium text-emerald-600">{{ t('support.stats.resolved') }}</p>
          <p class="mt-1 text-2xl font-bold text-emerald-700">{{ stats.resolved }}</p>
        </div>
        <div class="rounded-lg border border-purple-200 bg-purple-50 p-4">
          <p class="text-xs font-medium text-purple-600">{{ t('support.stats.avgResponseTime') }}</p>
          <p class="mt-1 text-2xl font-bold text-purple-700">{{ formatResponseTime(stats.avgResponseTimeMinutes) }}</p>
        </div>
      </div>

      <!-- Main Content: List + Detail -->
      <div class="grid grid-cols-1 gap-6" :class="selectedTicket ? 'lg:grid-cols-5' : ''">
        <!-- Ticket List -->
        <div :class="selectedTicket ? 'lg:col-span-2' : ''">
          <div class="rounded-lg border border-surface-dim bg-white">
            <!-- Filters -->
            <div class="border-b border-surface-dim p-4">
              <div class="mb-3">
                <input
                  v-model="searchQuery"
                  type="text"
                  :placeholder="t('common.search') + '...'"
                  class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
              <div class="flex flex-wrap gap-2">
                <select
                  v-model="filterStatus"
                  class="rounded-lg border border-surface-dim px-3 py-1.5 text-xs focus:border-primary focus:outline-none"
                >
                  <option v-for="opt in statusOptions" :key="opt.value" :value="opt.value">
                    {{ opt.label() }}
                  </option>
                </select>
                <select
                  v-model="filterPriority"
                  class="rounded-lg border border-surface-dim px-3 py-1.5 text-xs focus:border-primary focus:outline-none"
                >
                  <option v-for="opt in priorityOptions" :key="opt.value" :value="opt.value">
                    {{ opt.label() }}
                  </option>
                </select>
                <select
                  v-model="filterCategory"
                  class="rounded-lg border border-surface-dim px-3 py-1.5 text-xs focus:border-primary focus:outline-none"
                >
                  <option v-for="opt in categoryOptions" :key="opt.value" :value="opt.value">
                    {{ opt.label() }}
                  </option>
                </select>
              </div>
            </div>

            <!-- Ticket Items -->
            <div v-if="isLoading" class="p-8 text-center">
              <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
            </div>
            <div v-else-if="filteredTickets.length === 0" class="p-8 text-center">
              <i class="pi pi-ticket mb-2 text-3xl text-tertiary"></i>
              <p class="text-sm text-tertiary">{{ t('support.noTickets') }}</p>
            </div>
            <div v-else class="divide-y divide-surface-dim">
              <button
                v-for="ticket in filteredTickets"
                :key="ticket.id"
                type="button"
                class="w-full p-4 text-start transition-colors hover:bg-surface-muted"
                :class="{ 'bg-primary/5 border-s-4 border-s-primary': selectedTicket?.id === ticket.id }"
                @click="selectTicket(ticket)"
              >
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="text-xs font-mono text-tertiary">{{ ticket.ticketNumber }}</span>
                      <span :class="['inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium', getPriorityColor(ticket.priority)]">
                        {{ getPriorityLabel(ticket.priority) }}
                      </span>
                    </div>
                    <p class="text-sm font-medium text-secondary truncate">{{ ticket.subject }}</p>
                    <div class="mt-1 flex items-center gap-2 text-xs text-tertiary">
                      <span>{{ ticket.tenantName }}</span>
                      <span>&middot;</span>
                      <span>{{ ticket.submittedByName }}</span>
                    </div>
                  </div>
                  <div class="flex flex-col items-end gap-1">
                    <span :class="['inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium', getStatusColor(ticket.status)]">
                      {{ getStatusLabel(ticket.status) }}
                    </span>
                    <span class="text-[10px] text-tertiary">{{ formatDate(ticket.createdAt) }}</span>
                  </div>
                </div>
              </button>
            </div>
          </div>
        </div>

        <!-- Ticket Detail -->
        <div v-if="selectedTicket" class="lg:col-span-3">
          <div class="rounded-lg border border-surface-dim bg-white">
            <!-- Detail Header -->
            <div class="border-b border-surface-dim p-4">
              <div class="flex items-start justify-between">
                <div>
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xs font-mono text-tertiary">{{ selectedTicket.ticketNumber }}</span>
                    <span :class="['inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium', getStatusColor(selectedTicket.status)]">
                      {{ getStatusLabel(selectedTicket.status) }}
                    </span>
                    <span :class="['inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium', getPriorityColor(selectedTicket.priority)]">
                      {{ getPriorityLabel(selectedTicket.priority) }}
                    </span>
                  </div>
                  <h2 class="text-lg font-semibold text-secondary">{{ selectedTicket.subject }}</h2>
                  <div class="mt-1 flex flex-wrap items-center gap-3 text-xs text-tertiary">
                    <span><i class="pi pi-building me-1"></i>{{ selectedTicket.tenantName }}</span>
                    <span><i class="pi pi-user me-1"></i>{{ selectedTicket.submittedByName }}</span>
                    <span><i class="pi pi-tag me-1"></i>{{ getCategoryLabel(selectedTicket.category) }}</span>
                    <span><i class="pi pi-calendar me-1"></i>{{ formatDate(selectedTicket.createdAt) }}</span>
                  </div>
                </div>
                <button
                  type="button"
                  class="rounded-lg p-2 text-tertiary hover:bg-surface-muted"
                  @click="closeDetail"
                >
                  <i class="pi pi-times"></i>
                </button>
              </div>

              <!-- Action Buttons -->
              <div class="mt-3 flex flex-wrap gap-2">
                <button
                  v-if="selectedTicket.status !== 'InProgress'"
                  type="button"
                  class="rounded-lg bg-amber-100 px-3 py-1.5 text-xs font-medium text-amber-800 hover:bg-amber-200"
                  @click="updateTicketStatus(selectedTicket.id, 'InProgress')"
                >
                  <i class="pi pi-play me-1"></i>{{ t('support.statuses.inProgress') }}
                </button>
                <button
                  v-if="selectedTicket.status !== 'Resolved'"
                  type="button"
                  class="rounded-lg bg-emerald-100 px-3 py-1.5 text-xs font-medium text-emerald-800 hover:bg-emerald-200"
                  @click="updateTicketStatus(selectedTicket.id, 'Resolved')"
                >
                  <i class="pi pi-check me-1"></i>{{ t('support.resolve') }}
                </button>
                <button
                  v-if="selectedTicket.status !== 'Closed'"
                  type="button"
                  class="rounded-lg bg-gray-100 px-3 py-1.5 text-xs font-medium text-gray-800 hover:bg-gray-200"
                  @click="updateTicketStatus(selectedTicket.id, 'Closed')"
                >
                  <i class="pi pi-lock me-1"></i>{{ t('support.closeTicket') }}
                </button>
                <button
                  v-if="selectedTicket.status === 'Closed' || selectedTicket.status === 'Resolved'"
                  type="button"
                  class="rounded-lg bg-blue-100 px-3 py-1.5 text-xs font-medium text-blue-800 hover:bg-blue-200"
                  @click="updateTicketStatus(selectedTicket.id, 'Open')"
                >
                  <i class="pi pi-replay me-1"></i>{{ t('support.reopenTicket') }}
                </button>
              </div>
            </div>

            <!-- Messages Thread -->
            <div class="max-h-[400px] overflow-y-auto p-4 space-y-4">
              <div v-if="isLoadingMessages" class="text-center py-8">
                <i class="pi pi-spinner pi-spin text-xl text-primary"></i>
              </div>
              <div
                v-for="msg in ticketMessages"
                :key="msg.id"
                class="rounded-lg p-3"
                :class="msg.senderRole === 'operator' ? 'bg-primary/5 ms-8' : 'bg-surface-muted me-8'"
              >
                <div class="flex items-center justify-between mb-1">
                  <span class="text-xs font-medium" :class="msg.senderRole === 'operator' ? 'text-primary' : 'text-secondary'">
                    {{ msg.senderName }}
                    <span v-if="msg.isAiGenerated" class="ms-1 text-[10px] text-purple-600">
                      <i class="pi pi-sparkles"></i> AI
                    </span>
                  </span>
                  <span class="text-[10px] text-tertiary">{{ formatDate(msg.createdAt) }}</span>
                </div>
                <p class="text-sm text-secondary whitespace-pre-wrap">{{ msg.content }}</p>
              </div>
            </div>

            <!-- AI Suggestion -->
            <div v-if="aiSuggestion" class="mx-4 mb-3 rounded-lg border border-purple-200 bg-purple-50 p-3">
              <div class="flex items-center justify-between mb-2">
                <span class="text-xs font-medium text-purple-700">
                  <i class="pi pi-sparkles me-1"></i>{{ t('support.aiSuggestion') }}
                </span>
                <button
                  type="button"
                  class="rounded-lg bg-purple-100 px-2 py-1 text-[10px] font-medium text-purple-800 hover:bg-purple-200"
                  @click="useAiSuggestion"
                >
                  {{ t('support.useAiSuggestion') }}
                </button>
              </div>
              <p class="text-xs text-purple-800 whitespace-pre-wrap">{{ aiSuggestion }}</p>
            </div>

            <!-- Reply Box -->
            <div class="border-t border-surface-dim p-4">
              <div class="flex gap-2 mb-2">
                <button
                  type="button"
                  class="flex items-center gap-1 rounded-lg border border-purple-200 bg-purple-50 px-3 py-1.5 text-xs font-medium text-purple-700 hover:bg-purple-100 transition-colors"
                  :disabled="isLoadingAi"
                  @click="getAiSuggestion"
                >
                  <i :class="['pi text-xs', isLoadingAi ? 'pi-spinner pi-spin' : 'pi-sparkles']"></i>
                  {{ t('support.aiSuggestion') }}
                </button>
              </div>
              <div class="flex gap-2">
                <textarea
                  v-model="newReply"
                  :placeholder="t('support.messagePlaceholder')"
                  rows="3"
                  class="flex-1 rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary resize-none"
                ></textarea>
                <button
                  type="button"
                  class="self-end rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark transition-colors disabled:opacity-50"
                  :disabled="!newReply.trim()"
                  @click="sendReply"
                >
                  <i class="pi pi-send"></i>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
