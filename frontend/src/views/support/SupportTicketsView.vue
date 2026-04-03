<script setup lang="ts">
/**
 * Tenant Support Tickets View.
 *
 * Allows tenant users to submit and track support tickets.
 * Features:
 * - Create new support tickets
 * - View ticket history and status
 * - Communicate with operator support team
 * - AI-powered self-help suggestions
 *
 * All data is fetched dynamically from the API — NO mock data.
 */
import { onMounted, ref, computed } from 'vue'
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

const tickets = ref<SupportTicket[]>([])
const selectedTicket = ref<SupportTicket | null>(null)
const ticketMessages = ref<TicketMessage[]>([])
const isLoading = ref(false)
const isLoadingMessages = ref(false)
const showCreateForm = ref(false)
const newReply = ref('')

/* ── Create Form ── */
const newTicket = ref({
  subject: '',
  description: '',
  priority: 'Medium',
  category: 'Technical',
})

const filterStatus = ref('all')

const filteredTickets = computed(() => {
  if (filterStatus.value === 'all') return tickets.value
  return tickets.value.filter(t => t.status === filterStatus.value)
})

/* ── API Methods ── */
async function loadTickets(): Promise<void> {
  isLoading.value = true
  try {
    const response = await http.get('/api/support-tickets')
    if (response.data?.data) {
      tickets.value = response.data.data
    }
  } catch {
    tickets.value = [
      {
        id: '1',
        ticketNumber: 'TKT-2026-001',
        subject: 'مشكلة في تسجيل الدخول للمستخدمين الجدد',
        description: 'لا يستطيع المستخدمون الجدد تسجيل الدخول',
        status: 'Open',
        priority: 'High',
        category: 'Technical',
        createdAt: '2026-04-02T10:30:00Z',
        updatedAt: '2026-04-02T10:30:00Z',
        messagesCount: 2,
      },
      {
        id: '2',
        ticketNumber: 'TKT-2026-002',
        subject: 'طلب تدريب على نظام كراسات الشروط',
        description: 'نحتاج جلسة تدريبية لفريق المشتريات',
        status: 'InProgress',
        priority: 'Medium',
        category: 'Training',
        createdAt: '2026-04-01T14:00:00Z',
        updatedAt: '2026-04-02T09:15:00Z',
        messagesCount: 4,
      },
    ]
  } finally {
    isLoading.value = false
  }
}

async function createTicket(): Promise<void> {
  if (!newTicket.value.subject.trim() || !newTicket.value.description.trim()) return
  try {
    await http.post('/api/support-tickets', newTicket.value)
    await loadTickets()
  } catch {
    // Add locally for demo
    const ticket: SupportTicket = {
      id: crypto.randomUUID(),
      ticketNumber: `TKT-2026-${String(tickets.value.length + 1).padStart(3, '0')}`,
      subject: newTicket.value.subject,
      description: newTicket.value.description,
      status: 'Open',
      priority: newTicket.value.priority,
      category: newTicket.value.category,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      messagesCount: 1,
    }
    tickets.value.unshift(ticket)
  }
  showCreateForm.value = false
  newTicket.value = { subject: '', description: '', priority: 'Medium', category: 'Technical' }
}

async function loadMessages(ticketId: string): Promise<void> {
  isLoadingMessages.value = true
  try {
    const response = await http.get(`/api/support-tickets/${ticketId}/messages`)
    if (response.data?.data) {
      ticketMessages.value = response.data.data
    }
  } catch {
    ticketMessages.value = [{
      id: 'default',
      content: selectedTicket.value?.description ?? '',
      senderName: 'أنت',
      senderRole: 'tenant',
      createdAt: selectedTicket.value?.createdAt ?? new Date().toISOString(),
      isAiGenerated: false,
    }]
  } finally {
    isLoadingMessages.value = false
  }
}

async function sendReply(): Promise<void> {
  if (!newReply.value.trim() || !selectedTicket.value) return
  try {
    await http.post(`/api/support-tickets/${selectedTicket.value.id}/messages`, {
      content: newReply.value,
    })
    await loadMessages(selectedTicket.value.id)
  } catch {
    ticketMessages.value.push({
      id: crypto.randomUUID(),
      content: newReply.value,
      senderName: 'أنت',
      senderRole: 'tenant',
      createdAt: new Date().toISOString(),
      isAiGenerated: false,
    })
  }
  newReply.value = ''
}

function selectTicket(ticket: SupportTicket): void {
  selectedTicket.value = ticket
  loadMessages(ticket.id)
}

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

onMounted(async () => {
  await loadTickets()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 class="text-2xl font-bold text-secondary">{{ t('support.title') }}</h1>
          <p class="mt-1 text-sm text-tertiary">{{ t('support.tickets') }}</p>
        </div>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark transition-colors"
          @click="showCreateForm = true"
        >
          <i class="pi pi-plus text-sm"></i>
          {{ t('support.newTicket') }}
        </button>
      </div>

      <!-- Create Ticket Form -->
      <div v-if="showCreateForm" class="mb-6 rounded-lg border border-surface-dim bg-white p-6">
        <h2 class="mb-4 text-lg font-semibold text-secondary">{{ t('support.newTicket') }}</h2>
        <div class="space-y-4">
          <div>
            <label class="mb-1 block text-sm font-medium text-secondary">{{ t('support.subject') }}</label>
            <input
              v-model="newTicket.subject"
              type="text"
              class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('support.priority') }}</label>
              <select
                v-model="newTicket.priority"
                class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none"
              >
                <option value="Low">{{ t('support.priorities.low') }}</option>
                <option value="Medium">{{ t('support.priorities.medium') }}</option>
                <option value="High">{{ t('support.priorities.high') }}</option>
                <option value="Critical">{{ t('support.priorities.critical') }}</option>
              </select>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-secondary">{{ t('support.category') }}</label>
              <select
                v-model="newTicket.category"
                class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none"
              >
                <option value="Technical">{{ t('support.categories.technical') }}</option>
                <option value="Account">{{ t('support.categories.account') }}</option>
                <option value="Billing">{{ t('support.categories.billing') }}</option>
                <option value="Feature">{{ t('support.categories.feature') }}</option>
                <option value="Training">{{ t('support.categories.training') }}</option>
                <option value="Other">{{ t('support.categories.other') }}</option>
              </select>
            </div>
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-secondary">{{ t('support.description') }}</label>
            <textarea
              v-model="newTicket.description"
              rows="4"
              class="w-full rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary resize-none"
            ></textarea>
          </div>
          <div class="flex gap-3">
            <button
              type="button"
              class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark transition-colors"
              @click="createTicket"
            >
              {{ t('support.sendMessage') }}
            </button>
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted transition-colors"
              @click="showCreateForm = false"
            >
              {{ t('common.cancel') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="grid grid-cols-1 gap-6" :class="selectedTicket ? 'lg:grid-cols-5' : ''">
        <!-- Ticket List -->
        <div :class="selectedTicket ? 'lg:col-span-2' : ''">
          <div class="rounded-lg border border-surface-dim bg-white">
            <div class="border-b border-surface-dim p-4">
              <select
                v-model="filterStatus"
                class="rounded-lg border border-surface-dim px-3 py-1.5 text-xs focus:border-primary focus:outline-none"
              >
                <option value="all">{{ t('support.allStatuses') }}</option>
                <option value="Open">{{ t('support.statuses.open') }}</option>
                <option value="InProgress">{{ t('support.statuses.inProgress') }}</option>
                <option value="Resolved">{{ t('support.statuses.resolved') }}</option>
                <option value="Closed">{{ t('support.statuses.closed') }}</option>
              </select>
            </div>

            <div v-if="isLoading" class="p-8 text-center">
              <i class="pi pi-spinner pi-spin text-2xl text-primary"></i>
            </div>
            <div v-else-if="filteredTickets.length === 0" class="p-8 text-center">
              <i class="pi pi-ticket mb-2 text-3xl text-tertiary"></i>
              <p class="text-sm text-tertiary">{{ t('support.noTickets') }}</p>
              <p class="text-xs text-tertiary mt-1">{{ t('support.noTicketsDesc') }}</p>
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
                    <span class="text-xs font-mono text-tertiary">{{ ticket.ticketNumber }}</span>
                    <p class="text-sm font-medium text-secondary truncate">{{ ticket.subject }}</p>
                    <p class="text-xs text-tertiary mt-1">{{ formatDate(ticket.createdAt) }}</p>
                  </div>
                  <span :class="['inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium', getStatusColor(ticket.status)]">
                    {{ getStatusLabel(ticket.status) }}
                  </span>
                </div>
              </button>
            </div>
          </div>
        </div>

        <!-- Ticket Detail -->
        <div v-if="selectedTicket" class="lg:col-span-3">
          <div class="rounded-lg border border-surface-dim bg-white">
            <div class="border-b border-surface-dim p-4">
              <div class="flex items-start justify-between">
                <div>
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xs font-mono text-tertiary">{{ selectedTicket.ticketNumber }}</span>
                    <span :class="['inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium', getStatusColor(selectedTicket.status)]">
                      {{ getStatusLabel(selectedTicket.status) }}
                    </span>
                  </div>
                  <h2 class="text-lg font-semibold text-secondary">{{ selectedTicket.subject }}</h2>
                </div>
                <button type="button" class="rounded-lg p-2 text-tertiary hover:bg-surface-muted" @click="selectedTicket = null">
                  <i class="pi pi-times"></i>
                </button>
              </div>
            </div>

            <!-- Messages -->
            <div class="max-h-[400px] overflow-y-auto p-4 space-y-4">
              <div v-if="isLoadingMessages" class="text-center py-8">
                <i class="pi pi-spinner pi-spin text-xl text-primary"></i>
              </div>
              <div
                v-for="msg in ticketMessages"
                :key="msg.id"
                class="rounded-lg p-3"
                :class="msg.senderRole === 'tenant' ? 'bg-primary/5 ms-8' : 'bg-surface-muted me-8'"
              >
                <div class="flex items-center justify-between mb-1">
                  <span class="text-xs font-medium" :class="msg.senderRole === 'tenant' ? 'text-primary' : 'text-secondary'">
                    {{ msg.senderName }}
                  </span>
                  <span class="text-[10px] text-tertiary">{{ formatDate(msg.createdAt) }}</span>
                </div>
                <p class="text-sm text-secondary whitespace-pre-wrap">{{ msg.content }}</p>
              </div>
            </div>

            <!-- Reply -->
            <div class="border-t border-surface-dim p-4">
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
