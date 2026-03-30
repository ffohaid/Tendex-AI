<script setup lang="ts">
/**
 * WorkflowListView - List of Approval Workflows (TASK-1001)
 *
 * Displays all configured approval workflows with:
 * - Status badges (active/draft/inactive)
 * - Quick actions (edit, activate, deactivate, delete)
 * - Create new workflow button
 * - Search and filter
 */
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet } from '@/services/http'

const { t } = useI18n()
const router = useRouter()

interface WorkflowSummary {
  id: string
  nameAr: string
  nameEn: string
  description: string
  status: 'draft' | 'active' | 'inactive'
  nodeCount: number
  createdAt: string
  updatedAt: string
}

const isLoading = ref(false)
const workflows = ref<WorkflowSummary[]>([])
const searchQuery = ref('')
const statusFilter = ref<string>('all')

const filteredWorkflows = computed(() => {
  let result = workflows.value
  if (statusFilter.value !== 'all') {
    result = result.filter(w => w.status === statusFilter.value)
  }
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(w =>
      w.nameAr.toLowerCase().includes(q) ||
      w.nameEn.toLowerCase().includes(q)
    )
  }
  return result
})

async function loadWorkflows(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: WorkflowSummary[] }>('/v1/workflows')
    workflows.value = data.items
  } catch (err) {
    console.error('Failed to load workflows:', err)
  } finally {
    isLoading.value = false
  }
}

function createWorkflow(): void {
  router.push('/workflow/designer')
}

function editWorkflow(id: string): void {
  router.push(`/workflow/designer/${id}`)
}

function getStatusBadgeClass(status: string): string {
  const classes: Record<string, string> = {
    active: 'badge-success',
    draft: 'badge-warning',
    inactive: 'badge-secondary',
  }
  return classes[status] || 'badge-secondary'
}

onMounted(() => {
  loadWorkflows()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">{{ t('workflow.title') }}</h1>
        <p class="page-description">{{ t('workflow.list') }}</p>
      </div>
      <button class="btn-primary" @click="createWorkflow">
        <i class="pi pi-plus"></i>
        {{ t('workflow.create') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="card !p-4">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div class="relative flex-1">
          <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-secondary-400"></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('common.search')"
            class="input ps-10 text-sm"
          />
        </div>
        <div class="flex gap-2">
          <button
            v-for="status in ['all', 'active', 'draft', 'inactive']"
            :key="status"
            class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors"
            :class="statusFilter === status
              ? 'bg-primary text-white'
              : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
            @click="statusFilter = status"
          >
            {{ status === 'all' ? t('taskCenter.filters.all') : t(`workflow.${status === 'active' ? 'activate' : status === 'draft' ? 'saveDraft' : 'deactivate'}`) }}
          </button>
        </div>
      </div>
    </div>

    <!-- Workflow Cards -->
    <div v-if="isLoading" class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div v-for="i in 6" :key="i" class="skeleton-card"></div>
    </div>

    <div v-else-if="filteredWorkflows.length === 0" class="empty-state">
      <div class="empty-state-icon">
        <i class="pi pi-sitemap"></i>
      </div>
      <h3 class="empty-state-title">{{ t('workflow.empty') }}</h3>
      <p class="empty-state-text">{{ t('workflow.emptyDescription') }}</p>
      <button class="btn-primary mt-4" @click="createWorkflow">
        <i class="pi pi-plus"></i>
        {{ t('workflow.create') }}
      </button>
    </div>

    <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="wf in filteredWorkflows"
        :key="wf.id"
        class="card-interactive"
        @click="editWorkflow(wf.id)"
      >
        <div class="flex items-start justify-between mb-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-50">
            <i class="pi pi-sitemap text-primary"></i>
          </div>
          <span class="badge" :class="getStatusBadgeClass(wf.status)">
            {{ wf.status }}
          </span>
        </div>
        <h3 class="text-sm font-bold text-secondary-800">{{ wf.nameAr }}</h3>
        <p class="mt-1 text-xs text-secondary-500 line-clamp-2">{{ wf.description }}</p>
        <div class="mt-3 flex items-center gap-3 text-[10px] text-secondary-400">
          <span><i class="pi pi-circle-fill me-1"></i>{{ wf.nodeCount }} nodes</span>
          <span><i class="pi pi-clock me-1"></i>{{ wf.updatedAt }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
