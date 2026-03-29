<script setup lang="ts">
/**
 * InquiriesView — Inquiries Management Page.
 *
 * TASK-904: Displays and manages inquiries on specification booklets (RFPs).
 *
 * Features:
 * - Inquiry statistics cards (total, pending, answered, overdue)
 * - Paginated inquiry list with filters
 * - Status and priority badges
 * - SLA deadline indicators
 * - Inquiry detail modal with answer capability
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 *
 * All data is fetched dynamically from the API (no mock data).
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import { fetchInquiries, fetchInquiryStats, answerInquiry } from '@/services/inquiryService'
import type {
  Inquiry,
  InquiryFilters,
  InquiryStats,
} from '@/types/inquiry'

const { t, locale } = useI18n()
const { formatDateTime, formatNumber } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const isLoading = ref(false)
const error = ref('')
const inquiries = ref<Inquiry[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
const isRtl = computed(() => locale.value === 'ar')

const stats = ref<InquiryStats>({
  total: 0,
  pending: 0,
  answered: 0,
  overdue: 0,
  averageResponseTimeHours: 0,
})

/* Filters */
const filters = ref<InquiryFilters>({
  status: '',
  priority: '',
  competitionId: '',
  search: '',
})

/* Detail Modal */
const showDetailModal = ref(false)
const selectedInquiry = ref<Inquiry | null>(null)
const answerText = ref('')
const isSubmitting = ref(false)

/* ------------------------------------------------------------------ */
/*  Stats Cards Configuration                                          */
/* ------------------------------------------------------------------ */
const statsCards = computed(() => [
  {
    key: 'total',
    icon: 'pi-comments',
    color: 'text-primary',
    bgColor: 'bg-primary/10',
    value: formatNumber(stats.value.total),
    label: t('inquiries.stats.total'),
  },
  {
    key: 'pending',
    icon: 'pi-clock',
    color: 'text-warning',
    bgColor: 'bg-warning/10',
    value: formatNumber(stats.value.pending),
    label: t('inquiries.stats.pending'),
  },
  {
    key: 'answered',
    icon: 'pi-check-circle',
    color: 'text-success',
    bgColor: 'bg-success/10',
    value: formatNumber(stats.value.answered),
    label: t('inquiries.stats.answered'),
  },
  {
    key: 'overdue',
    icon: 'pi-exclamation-triangle',
    color: 'text-danger',
    bgColor: 'bg-danger/10',
    value: formatNumber(stats.value.overdue),
    label: t('inquiries.stats.overdue'),
  },
])

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getStatusBadge(status: string) {
  const config: Record<string, { bgClass: string; textClass: string }> = {
    pending: { bgClass: 'bg-warning/10', textClass: 'text-warning' },
    answered: { bgClass: 'bg-success/10', textClass: 'text-success' },
    closed: { bgClass: 'bg-surface-muted', textClass: 'text-tertiary' },
    escalated: { bgClass: 'bg-danger/10', textClass: 'text-danger' },
  }
  return config[status] || config.pending
}

function getPriorityBadge(priority: string) {
  const config: Record<string, { bgClass: string; textClass: string }> = {
    critical: { bgClass: 'bg-danger/10', textClass: 'text-danger' },
    high: { bgClass: 'bg-warning/10', textClass: 'text-warning' },
    medium: { bgClass: 'bg-info/10', textClass: 'text-info' },
    low: { bgClass: 'bg-surface-muted', textClass: 'text-tertiary' },
  }
  return config[priority] || config.medium
}

/* ------------------------------------------------------------------ */
/*  Data Loading                                                       */
/* ------------------------------------------------------------------ */
async function loadInquiries(): Promise<void> {
  isLoading.value = true
  error.value = ''
  try {
    const [inquiriesResult, statsResult] = await Promise.all([
      fetchInquiries(currentPage.value, pageSize.value, filters.value),
      fetchInquiryStats(),
    ])
    inquiries.value = inquiriesResult.items
    totalCount.value = inquiriesResult.totalCount
    stats.value = statsResult
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err)
    error.value = message
    console.error('[InquiriesView] Failed to load inquiries:', err)
  } finally {
    isLoading.value = false
  }
}

function applyFilters(): void {
  currentPage.value = 1
  loadInquiries()
}

function resetFilters(): void {
  filters.value = { status: '', priority: '', competitionId: '', search: '' }
  currentPage.value = 1
  loadInquiries()
}

function goToPage(page: number): void {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    loadInquiries()
  }
}

function openDetail(inquiry: Inquiry): void {
  selectedInquiry.value = inquiry
  answerText.value = ''
  showDetailModal.value = true
}

function closeDetail(): void {
  showDetailModal.value = false
  selectedInquiry.value = null
  answerText.value = ''
}

async function submitAnswer(): Promise<void> {
  if (!selectedInquiry.value || !answerText.value.trim()) return
  isSubmitting.value = true
  try {
    await answerInquiry(selectedInquiry.value.id, answerText.value, answerText.value)
    closeDetail()
    await loadInquiries()
  } catch (err: unknown) {
    console.error('[InquiriesView] Failed to submit answer:', err)
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  loadInquiries()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('inquiries.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('inquiries.subtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
          :disabled="isLoading"
          @click="loadInquiries"
        >
          <i class="pi" :class="isLoading ? 'pi-spinner pi-spin' : 'pi-refresh'" />
          {{ t('inquiries.refresh') }}
        </button>
      </div>
    </div>

    <!-- Error Alert -->
    <div
      v-if="error"
      class="flex items-center gap-3 rounded-xl border border-danger/20 bg-danger/5 p-4"
    >
      <i class="pi pi-exclamation-triangle text-danger" />
      <div class="flex-1">
        <p class="text-sm font-medium text-danger">{{ t('inquiries.errorLoading') }}</p>
        <p class="mt-0.5 text-xs text-danger/70">{{ error }}</p>
      </div>
      <button
        class="rounded-lg px-3 py-1.5 text-sm font-medium text-danger hover:bg-danger/10"
        @click="loadInquiries"
      >
        {{ t('inquiries.retry') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-2 gap-3 md:grid-cols-4">
      <div
        v-for="card in statsCards"
        :key="card.key"
        class="card flex items-center gap-3 !p-4"
      >
        <template v-if="isLoading">
          <div class="h-10 w-10 animate-pulse rounded-lg bg-surface-dim" />
          <div class="flex-1">
            <div class="mb-1 h-3 w-16 animate-pulse rounded bg-surface-dim" />
            <div class="h-5 w-12 animate-pulse rounded bg-surface-muted" />
          </div>
        </template>
        <template v-else>
          <div
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
            :class="card.bgColor"
          >
            <i class="pi text-base" :class="[card.icon, card.color]" />
          </div>
          <div>
            <p class="text-[11px] text-tertiary">{{ card.label }}</p>
            <p class="text-lg font-bold text-secondary">{{ card.value }}</p>
          </div>
        </template>
      </div>
    </div>

    <!-- Filters -->
    <div class="card">
      <div class="flex flex-wrap items-end gap-4">
        <div class="flex-1 min-w-[200px]">
          <label class="mb-1 block text-xs font-medium text-tertiary">
            {{ t('inquiries.filters.search') }}
          </label>
          <div class="relative">
            <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary" />
            <input
              v-model="filters.search"
              type="text"
              :placeholder="t('inquiries.filters.searchPlaceholder')"
              class="w-full rounded-lg border border-surface-dim bg-white py-2 pe-3 ps-9 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              @keyup.enter="applyFilters"
            />
          </div>
        </div>
        <div class="min-w-[160px]">
          <label class="mb-1 block text-xs font-medium text-tertiary">
            {{ t('inquiries.filters.status') }}
          </label>
          <select
            v-model="filters.status"
            class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option value="">{{ t('inquiries.filters.allStatuses') }}</option>
            <option value="pending">{{ t('inquiries.status.pending') }}</option>
            <option value="answered">{{ t('inquiries.status.answered') }}</option>
            <option value="closed">{{ t('inquiries.status.closed') }}</option>
            <option value="escalated">{{ t('inquiries.status.escalated') }}</option>
          </select>
        </div>
        <div class="min-w-[160px]">
          <label class="mb-1 block text-xs font-medium text-tertiary">
            {{ t('inquiries.filters.priority') }}
          </label>
          <select
            v-model="filters.priority"
            class="w-full rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <option value="">{{ t('inquiries.filters.allPriorities') }}</option>
            <option value="critical">{{ t('inquiries.priority.critical') }}</option>
            <option value="high">{{ t('inquiries.priority.high') }}</option>
            <option value="medium">{{ t('inquiries.priority.medium') }}</option>
            <option value="low">{{ t('inquiries.priority.low') }}</option>
          </select>
        </div>
        <div class="flex gap-2">
          <button
            class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary/90"
            @click="applyFilters"
          >
            <i class="pi pi-search me-1" />
            {{ t('inquiries.filters.apply') }}
          </button>
          <button
            class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
            @click="resetFilters"
          >
            <i class="pi pi-filter-slash me-1" />
            {{ t('inquiries.filters.reset') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Inquiries List -->
    <div class="card">
      <!-- Loading State -->
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-20 animate-pulse rounded-lg bg-surface-muted" />
      </div>

      <!-- Empty State -->
      <div v-else-if="inquiries.length === 0" class="flex flex-col items-center justify-center py-16">
        <i class="pi pi-comments text-5xl text-surface-dim" />
        <p class="mt-4 text-base font-medium text-secondary">{{ t('inquiries.empty.title') }}</p>
        <p class="mt-1 text-sm text-tertiary">{{ t('inquiries.empty.subtitle') }}</p>
      </div>

      <!-- Inquiry Items -->
      <div v-else class="space-y-3">
        <div
          v-for="inquiry in inquiries"
          :key="inquiry.id"
          class="cursor-pointer rounded-xl border border-surface-dim p-4 transition-all hover:border-primary/30 hover:shadow-sm"
          @click="openDetail(inquiry)"
        >
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1 min-w-0">
              <!-- Subject -->
              <div class="flex items-center gap-2">
                <h4 class="truncate text-sm font-semibold text-secondary">
                  {{ isRtl ? inquiry.subjectAr : inquiry.subjectEn }}
                </h4>
                <span class="shrink-0 text-xs text-tertiary">
                  #{{ inquiry.referenceNumber }}
                </span>
              </div>
              <!-- Competition -->
              <p class="mt-1 text-xs text-tertiary">
                <i class="pi pi-file-edit me-1" />
                {{ isRtl ? inquiry.competitionTitleAr : inquiry.competitionTitleEn }}
                <span class="ms-1 text-secondary/50">({{ inquiry.competitionReferenceNumber }})</span>
              </p>
              <!-- Submitted info -->
              <p class="mt-1 text-xs text-tertiary">
                <i class="pi pi-user me-1" />
                {{ isRtl ? inquiry.submittedByNameAr : inquiry.submittedByNameEn }}
                <span class="ms-2">
                  <i class="pi pi-calendar me-1" />
                  {{ formatDateTime(inquiry.submittedAt) }}
                </span>
              </p>
            </div>
            <!-- Badges -->
            <div class="flex flex-col items-end gap-2">
              <span
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="[getStatusBadge(inquiry.status).bgClass, getStatusBadge(inquiry.status).textClass]"
              >
                {{ t(`inquiries.status.${inquiry.status}`) }}
              </span>
              <span
                class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="[getPriorityBadge(inquiry.priority).bgClass, getPriorityBadge(inquiry.priority).textClass]"
              >
                {{ t(`inquiries.priority.${inquiry.priority}`) }}
              </span>
              <!-- Overdue indicator -->
              <span
                v-if="inquiry.isOverdue"
                class="flex items-center gap-1 text-xs font-medium text-danger"
              >
                <i class="pi pi-exclamation-circle text-xs" />
                {{ t('inquiries.overdue') }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div
        v-if="totalPages > 1"
        class="mt-4 flex items-center justify-between border-t border-surface-dim pt-4"
      >
        <p class="text-xs text-tertiary">
          {{ t('inquiries.pagination.showing', {
            from: (currentPage - 1) * pageSize + 1,
            to: Math.min(currentPage * pageSize, totalCount),
            total: totalCount,
          }) }}
        </p>
        <div class="flex items-center gap-1">
          <button
            class="rounded-lg px-3 py-1.5 text-sm text-secondary transition-colors hover:bg-surface-muted disabled:opacity-50"
            :disabled="currentPage <= 1"
            @click="goToPage(currentPage - 1)"
          >
            <i class="pi pi-chevron-left rtl:pi-chevron-right" />
          </button>
          <template v-for="page in totalPages" :key="page">
            <button
              v-if="page === 1 || page === totalPages || (page >= currentPage - 1 && page <= currentPage + 1)"
              class="min-w-[2rem] rounded-lg px-2 py-1.5 text-sm font-medium transition-colors"
              :class="page === currentPage
                ? 'bg-primary text-white'
                : 'text-secondary hover:bg-surface-muted'"
              @click="goToPage(page)"
            >
              {{ page }}
            </button>
            <span
              v-else-if="page === currentPage - 2 || page === currentPage + 2"
              class="px-1 text-tertiary"
            >
              ...
            </span>
          </template>
          <button
            class="rounded-lg px-3 py-1.5 text-sm text-secondary transition-colors hover:bg-surface-muted disabled:opacity-50"
            :disabled="currentPage >= totalPages"
            @click="goToPage(currentPage + 1)"
          >
            <i class="pi pi-chevron-right rtl:pi-chevron-left" />
          </button>
        </div>
      </div>
    </div>

    <!-- Detail Modal -->
    <Teleport to="body">
      <div
        v-if="showDetailModal && selectedInquiry"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
        @click.self="closeDetail"
      >
        <div class="w-full max-w-2xl overflow-hidden rounded-2xl bg-white shadow-xl">
          <!-- Modal Header -->
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <div>
              <h3 class="text-lg font-bold text-secondary">
                {{ isRtl ? selectedInquiry.subjectAr : selectedInquiry.subjectEn }}
              </h3>
              <p class="mt-0.5 text-xs text-tertiary">
                #{{ selectedInquiry.referenceNumber }}
              </p>
            </div>
            <button
              class="rounded-lg p-2 text-tertiary transition-colors hover:bg-surface-muted hover:text-secondary"
              @click="closeDetail"
            >
              <i class="pi pi-times" />
            </button>
          </div>

          <!-- Modal Body -->
          <div class="max-h-[60vh] overflow-y-auto px-6 py-4">
            <!-- Inquiry Info -->
            <div class="mb-4 grid grid-cols-2 gap-4">
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.competition') }}</p>
                <p class="text-sm font-medium text-secondary">
                  {{ isRtl ? selectedInquiry.competitionTitleAr : selectedInquiry.competitionTitleEn }}
                </p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.submittedBy') }}</p>
                <p class="text-sm font-medium text-secondary">
                  {{ isRtl ? selectedInquiry.submittedByNameAr : selectedInquiry.submittedByNameEn }}
                </p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.submittedAt') }}</p>
                <p class="text-sm font-medium text-secondary">
                  {{ formatDateTime(selectedInquiry.submittedAt) }}
                </p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('inquiries.detail.status') }}</p>
                <span
                  class="rounded-full px-2.5 py-0.5 text-xs font-medium"
                  :class="[getStatusBadge(selectedInquiry.status).bgClass, getStatusBadge(selectedInquiry.status).textClass]"
                >
                  {{ t(`inquiries.status.${selectedInquiry.status}`) }}
                </span>
              </div>
            </div>

            <!-- Inquiry Body -->
            <div class="mb-4">
              <p class="mb-1 text-xs font-medium text-tertiary">{{ t('inquiries.detail.question') }}</p>
              <div class="rounded-lg bg-surface-muted p-4 text-sm leading-relaxed text-secondary">
                {{ isRtl ? selectedInquiry.bodyAr : selectedInquiry.bodyEn }}
              </div>
            </div>

            <!-- Existing Answer -->
            <div v-if="selectedInquiry.answerAr" class="mb-4">
              <p class="mb-1 text-xs font-medium text-tertiary">{{ t('inquiries.detail.answer') }}</p>
              <div class="rounded-lg border border-success/20 bg-success/5 p-4 text-sm leading-relaxed text-secondary">
                {{ isRtl ? selectedInquiry.answerAr : selectedInquiry.answerEn }}
              </div>
              <p class="mt-1 text-xs text-tertiary">
                {{ t('inquiries.detail.answeredBy') }}:
                {{ isRtl ? selectedInquiry.answeredByNameAr : selectedInquiry.answeredByNameEn }}
                — {{ formatDateTime(selectedInquiry.answeredAt) }}
              </p>
            </div>

            <!-- Answer Form (if pending) -->
            <div v-if="selectedInquiry.status === 'pending'" class="mt-4">
              <p class="mb-1 text-xs font-medium text-tertiary">{{ t('inquiries.detail.writeAnswer') }}</p>
              <textarea
                v-model="answerText"
                rows="4"
                :placeholder="t('inquiries.detail.answerPlaceholder')"
                class="w-full rounded-lg border border-surface-dim bg-white p-3 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-surface-dim px-6 py-4">
            <button
              class="rounded-lg border border-surface-dim bg-white px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
              @click="closeDetail"
            >
              {{ t('inquiries.detail.close') }}
            </button>
            <button
              v-if="selectedInquiry.status === 'pending'"
              class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary/90 disabled:opacity-50"
              :disabled="!answerText.trim() || isSubmitting"
              @click="submitAnswer"
            >
              <i class="pi" :class="isSubmitting ? 'pi-spinner pi-spin' : 'pi-send'" />
              {{ t('inquiries.detail.submitAnswer') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
