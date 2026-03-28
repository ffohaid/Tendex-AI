<script setup lang="ts">
/**
 * RFP List View.
 *
 * Displays a paginated, filterable list of all specifications booklets.
 * Data is fetched dynamically from the API (no mock data).
 */
import { ref, onMounted, computed, watch, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { fetchRfpList, deleteRfp } from '@/services/rfpService'
import { formatCurrency } from '@/utils/numbers'
import type { RfpListItem, RfpStatus } from '@/types/rfp'

const { t } = useI18n()
const router = useRouter()

/** State */
const rfpItems = ref<RfpListItem[]>([])
const isLoading = ref(false)
const error = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const searchQuery = ref('')
const statusFilter = ref('')
const showDeleteDialog = ref(false)
const deletingId = ref<string | null>(null)

/** Status options */
const statusOptions = computed(() => [
  { value: '', label: t('rfp.statuses.all') },
  { value: 'draft', label: t('rfp.statuses.draft') },
  { value: 'pending_approval', label: t('rfp.statuses.pendingApproval') },
  { value: 'approved', label: t('rfp.statuses.approved') },
  { value: 'published', label: t('rfp.statuses.published') },
  { value: 'cancelled', label: t('rfp.statuses.cancelled') },
])

/** Status badge config */
function getStatusBadge(status: RfpStatus) {
  const config: Record<string, { label: string; bgClass: string; textClass: string }> = {
    draft: { label: t('rfp.statuses.draft'), bgClass: 'bg-surface-muted', textClass: 'text-tertiary' },
    pending_approval: { label: t('rfp.statuses.pendingApproval'), bgClass: 'bg-warning/10', textClass: 'text-warning' },
    approved: { label: t('rfp.statuses.approved'), bgClass: 'bg-success/10', textClass: 'text-success' },
    published: { label: t('rfp.statuses.published'), bgClass: 'bg-info/10', textClass: 'text-info' },
    receiving_offers: { label: t('rfp.statuses.receivingOffers'), bgClass: 'bg-primary/10', textClass: 'text-primary' },
    cancelled: { label: t('rfp.statuses.cancelled'), bgClass: 'bg-danger/10', textClass: 'text-danger' },
  }
  return config[status] || config.draft
}

/** Fetch RFP list from API */
async function loadRfpList() {
  isLoading.value = true
  error.value = ''

  const response = await fetchRfpList({
    page: currentPage.value,
    pageSize: pageSize.value,
    status: statusFilter.value || undefined,
    search: searchQuery.value || undefined,
  })

  isLoading.value = false

  if (response.success && response.data) {
    rfpItems.value = response.data.items
    totalCount.value = response.data.totalCount
    totalPages.value = response.data.totalPages
  } else {
    error.value = response.message
    rfpItems.value = []
  }
}

/** Handle delete */
async function confirmDelete() {
  if (!deletingId.value) return

  const response = await deleteRfp(deletingId.value)
  if (response.success) {
    await loadRfpList()
  }
  showDeleteDialog.value = false
  deletingId.value = null
}

function openDeleteDialog(id: string) {
  deletingId.value = id
  showDeleteDialog.value = true
}

/** Navigate to create */
function goToCreate() {
  router.push({ name: 'rfp-create' })
}

/** Navigate to edit */
function goToEdit(id: string) {
  router.push({ name: 'rfp-edit', params: { id } })
}

/** Format date */
function formatDate(dateStr: string): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-SA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

/**
 * Watch filters with debounce for search input (TASK-703: Performance optimization).
 * Search input is debounced to prevent excessive API calls on every keystroke.
 * Status filter changes trigger immediate reload.
 */
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

watch(searchQuery, () => {
  if (searchDebounceTimer) clearTimeout(searchDebounceTimer)
  searchDebounceTimer = setTimeout(() => {
    currentPage.value = 1
    loadRfpList()
  }, 350)
})

watch(statusFilter, () => {
  currentPage.value = 1
  loadRfpList()
})

onUnmounted(() => {
  if (searchDebounceTimer) clearTimeout(searchDebounceTimer)
})

watch(currentPage, () => {
  loadRfpList()
})

onMounted(() => {
  loadRfpList()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8 flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-secondary">
            {{ t('rfp.titles.list') }}
          </h1>
          <p class="mt-1 text-sm text-tertiary">
            {{ t('rfp.titles.listSubtitle') }}
          </p>
        </div>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-bold text-white shadow-md transition-all hover:bg-primary-dark hover:shadow-lg"
          @click="goToCreate"
        >
          <i class="pi pi-plus text-sm"></i>
          {{ t('rfp.actions.createNew') }}
        </button>
      </div>

      <!-- Filters -->
      <div class="mb-6 flex flex-wrap items-center gap-4 rounded-lg border border-surface-dim bg-white p-4">
        <!-- Search -->
        <div class="relative min-w-[250px] flex-1">
          <i class="pi pi-search pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-sm text-tertiary"></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('rfp.placeholders.search')"
            class="w-full rounded-lg border border-surface-dim py-2 pe-4 ps-10 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          />
        </div>

        <!-- Status filter -->
        <select
          v-model="statusFilter"
          class="rounded-lg border border-surface-dim px-4 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
        >
          <option
            v-for="option in statusOptions"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </option>
        </select>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="rounded-lg border border-danger/20 bg-danger/5 p-8 text-center"
      >
        <i class="pi pi-exclamation-triangle mb-3 text-3xl text-danger"></i>
        <p class="text-sm text-danger">{{ error }}</p>
        <button
          type="button"
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="loadRfpList"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Empty state -->
      <div
        v-else-if="rfpItems.length === 0"
        class="rounded-lg border-2 border-dashed border-surface-dim p-16 text-center"
      >
        <i class="pi pi-file-edit mb-4 text-5xl text-tertiary"></i>
        <h3 class="text-lg font-bold text-secondary">{{ t('rfp.messages.noRfps') }}</h3>
        <p class="mt-2 text-sm text-tertiary">{{ t('rfp.messages.noRfpsDesc') }}</p>
        <button
          type="button"
          class="mt-6 rounded-lg bg-primary px-6 py-2.5 text-sm font-bold text-white hover:bg-primary-dark"
          @click="goToCreate"
        >
          <i class="pi pi-plus me-2 text-sm"></i>
          {{ t('rfp.actions.createFirst') }}
        </button>
      </div>

      <!-- Table -->
      <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white shadow-sm">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="border-b border-surface-dim bg-surface-muted">
              <tr>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.projectName') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.referenceNumber') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.status') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.estimatedValue') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.createdAt') }}</th>
                <th class="px-4 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.completion') }}</th>
                <th class="px-4 py-3 text-center text-xs font-medium text-tertiary">{{ t('rfp.tableHeaders.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in rfpItems"
                :key="item.id"
                class="border-b border-surface-dim transition-colors hover:bg-surface-muted/50"
              >
                <td class="px-4 py-3">
                  <button
                    type="button"
                    class="text-sm font-medium text-primary hover:underline"
                    @click="goToEdit(item.id)"
                  >
                    {{ item.projectName }}
                  </button>
                </td>
                <td class="px-4 py-3 text-sm text-secondary">{{ item.referenceNumber }}</td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex rounded-full px-2.5 py-1 text-xs font-medium"
                    :class="[getStatusBadge(item.status).bgClass, getStatusBadge(item.status).textClass]"
                  >
                    {{ getStatusBadge(item.status).label }}
                  </span>
                </td>
                <td class="px-4 py-3 text-sm text-secondary">{{ formatCurrency(item.estimatedValue) }}</td>
                <td class="px-4 py-3 text-sm text-tertiary">{{ formatDate(item.createdAt) }}</td>
                <td class="px-4 py-3">
                  <div class="flex items-center gap-2">
                    <div class="h-2 w-20 overflow-hidden rounded-full bg-surface-dim">
                      <div
                        class="h-full rounded-full bg-primary transition-all"
                        :style="{ width: `${item.completionPercentage}%` }"
                      ></div>
                    </div>
                    <span class="text-xs text-tertiary">{{ item.completionPercentage }}%</span>
                  </div>
                </td>
                <td class="px-4 py-3 text-center">
                  <div class="flex items-center justify-center gap-1">
                    <button
                      type="button"
                      class="flex h-8 w-8 items-center justify-center rounded-lg transition-colors hover:bg-surface-muted"
                      :title="t('common.edit')"
                      @click="goToEdit(item.id)"
                    >
                      <i class="pi pi-pencil text-sm text-primary"></i>
                    </button>
                    <button
                      v-if="item.status === 'draft'"
                      type="button"
                      class="flex h-8 w-8 items-center justify-center rounded-lg transition-colors hover:bg-danger/10"
                      :title="t('common.delete')"
                      @click="openDeleteDialog(item.id)"
                    >
                      <i class="pi pi-trash text-sm text-danger"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="flex items-center justify-between border-t border-surface-dim px-4 py-3">
          <span class="text-sm text-tertiary">
            {{ t('rfp.pagination.showing') }} {{ rfpItems.length }} {{ t('rfp.pagination.of') }} {{ totalCount }}
          </span>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-3 py-1.5 text-sm transition-colors hover:bg-surface-muted disabled:opacity-50"
              :disabled="currentPage <= 1"
              @click="currentPage--"
            >
              {{ t('rfp.pagination.previous') }}
            </button>
            <span class="text-sm text-secondary">{{ currentPage }} / {{ totalPages || 1 }}</span>
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-3 py-1.5 text-sm transition-colors hover:bg-surface-muted disabled:opacity-50"
              :disabled="currentPage >= totalPages"
              @click="currentPage++"
            >
              {{ t('rfp.pagination.next') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete confirmation dialog -->
    <Teleport to="body">
      <div
        v-if="showDeleteDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
        @click.self="showDeleteDialog = false"
      >
        <div class="mx-4 w-full max-w-md rounded-xl bg-white p-6 shadow-2xl">
          <div class="mb-4 text-center">
            <i class="pi pi-exclamation-triangle mb-3 text-4xl text-warning"></i>
            <h3 class="text-lg font-bold text-secondary">{{ t('rfp.dialogs.deleteTitle') }}</h3>
            <p class="mt-2 text-sm text-tertiary">{{ t('rfp.dialogs.deleteMessage') }}</p>
          </div>
          <div class="flex items-center justify-center gap-3">
            <button
              type="button"
              class="rounded-lg border border-surface-dim px-6 py-2 text-sm font-medium text-secondary hover:bg-surface-muted"
              @click="showDeleteDialog = false"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="rounded-lg bg-danger px-6 py-2 text-sm font-medium text-white hover:bg-danger/90"
              @click="confirmDelete"
            >
              {{ t('common.delete') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
