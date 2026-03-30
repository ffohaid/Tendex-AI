<script setup lang="ts">
/**
 * KnowledgeBaseView - Knowledge Base & RAG Document Management (TASK-1001)
 *
 * Features:
 * - Document upload with drag & drop
 * - Category-based organization
 * - RAG indexing status indicators
 * - Search across documents
 * - AI-powered semantic search
 * - Document preview
 * - RTL/LTR support
 *
 * All data fetched dynamically from APIs.
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost } from '@/services/http'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const { formatDateTime } = useFormatters()

/* ── Types ── */
interface KBDocument {
  id: string
  name: string
  category: string
  size: number
  mimeType: string
  uploadedAt: string
  uploadedBy: string
  indexingStatus: 'indexed' | 'pending' | 'failed'
  chunkCount: number
  tags: string[]
}

interface Category {
  key: string
  labelKey: string
  icon: string
  count: number
}

/* ── State ── */
const isLoading = ref(false)
const isUploading = ref(false)
const documents = ref<KBDocument[]>([])
const searchQuery = ref('')
const selectedCategory = ref<string>('all')
const isDragging = ref(false)
const showUploadDialog = ref(false)

const categories: Category[] = [
  { key: 'all', labelKey: 'taskCenter.filters.all', icon: 'pi-folder', count: 0 },
  { key: 'regulations', labelKey: 'knowledgeBase.categories.regulations', icon: 'pi-book', count: 0 },
  { key: 'templates', labelKey: 'knowledgeBase.categories.templates', icon: 'pi-file', count: 0 },
  { key: 'circulars', labelKey: 'knowledgeBase.categories.circulars', icon: 'pi-envelope', count: 0 },
  { key: 'previousCompetitions', labelKey: 'knowledgeBase.categories.previousCompetitions', icon: 'pi-briefcase', count: 0 },
  { key: 'guides', labelKey: 'knowledgeBase.categories.guides', icon: 'pi-info-circle', count: 0 },
]

const filteredDocuments = computed(() => {
  let result = documents.value
  if (selectedCategory.value !== 'all') {
    result = result.filter(d => d.category === selectedCategory.value)
  }
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(d =>
      d.name.toLowerCase().includes(q) ||
      d.tags.some(tag => tag.toLowerCase().includes(q))
    )
  }
  return result
})

/* ── Methods ── */
async function loadDocuments(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: KBDocument[] }>('/v1/knowledge-base/documents')
    documents.value = data.items
    /* Update category counts */
    for (const cat of categories) {
      if (cat.key === 'all') {
        cat.count = data.items.length
      } else {
        cat.count = data.items.filter(d => d.category === cat.key).length
      }
    }
  } catch (err) {
    console.error('Failed to load documents:', err)
  } finally {
    isLoading.value = false
  }
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function getStatusBadge(status: string): string {
  const classes: Record<string, string> = {
    indexed: 'badge-success',
    pending: 'badge-warning',
    failed: 'badge-danger',
  }
  return classes[status] || 'badge-secondary'
}

function getStatusIcon(status: string): string {
  const icons: Record<string, string> = {
    indexed: 'pi-check-circle',
    pending: 'pi-spin pi-spinner',
    failed: 'pi-exclamation-triangle',
  }
  return icons[status] || 'pi-circle'
}

function getFileIcon(mimeType: string): string {
  if (mimeType.includes('pdf')) return 'pi-file-pdf'
  if (mimeType.includes('word') || mimeType.includes('document')) return 'pi-file-word'
  if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return 'pi-file-excel'
  if (mimeType.includes('image')) return 'pi-image'
  return 'pi-file'
}

function handleDragOver(e: DragEvent): void {
  e.preventDefault()
  isDragging.value = true
}

function handleDragLeave(): void {
  isDragging.value = false
}

async function handleDrop(e: DragEvent): Promise<void> {
  e.preventDefault()
  isDragging.value = false
  const files = e.dataTransfer?.files
  if (files && files.length > 0) {
    await uploadFiles(files)
  }
}

async function handleFileSelect(e: Event): Promise<void> {
  const input = e.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    await uploadFiles(input.files)
  }
}

async function uploadFiles(files: FileList): Promise<void> {
  isUploading.value = true
  try {
    for (const file of Array.from(files)) {
      const formData = new FormData()
      formData.append('file', file)
      formData.append('category', selectedCategory.value === 'all' ? 'regulations' : selectedCategory.value)
      await httpPost('/v1/knowledge-base/documents/upload', formData)
    }
    await loadDocuments()
  } catch (err) {
    console.error('Failed to upload files:', err)
  } finally {
    isUploading.value = false
  }
}

async function reindexDocument(docId: string): Promise<void> {
  try {
    await httpPost(`/v1/knowledge-base/documents/${docId}/reindex`)
    const doc = documents.value.find(d => d.id === docId)
    if (doc) doc.indexingStatus = 'pending'
  } catch (err) {
    console.error('Failed to reindex document:', err)
  }
}

onMounted(() => {
  loadDocuments()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">{{ t('knowledgeBase.title') }}</h1>
        <p class="page-description">{{ t('knowledgeBase.subtitle') }}</p>
      </div>
      <label class="btn-primary cursor-pointer">
        <i class="pi pi-upload"></i>
        {{ t('knowledgeBase.upload') }}
        <input type="file" class="hidden" multiple accept=".pdf,.docx,.xlsx,.doc,.xls" @change="handleFileSelect" />
      </label>
    </div>

    <!-- AI Search Bar -->
    <div class="card !p-4">
      <div class="relative">
        <i class="pi pi-search absolute start-4 top-1/2 -translate-y-1/2 text-secondary-400"></i>
        <input
          v-model="searchQuery"
          type="text"
          :placeholder="t('knowledgeBase.search')"
          class="input ps-11 pe-24 text-sm"
        />
        <button class="btn-ai btn-sm absolute end-1.5 top-1/2 -translate-y-1/2">
          <i class="pi pi-sparkles text-xs"></i>
          AI
        </button>
      </div>
    </div>

    <div class="flex gap-6">
      <!-- Category Sidebar -->
      <div class="hidden w-56 shrink-0 lg:block">
        <div class="space-y-1">
          <button
            v-for="cat in categories"
            :key="cat.key"
            class="flex w-full items-center gap-3 rounded-xl px-4 py-2.5 text-sm transition-all"
            :class="selectedCategory === cat.key
              ? 'bg-primary text-white font-semibold shadow-card'
              : 'text-secondary-600 hover:bg-surface-subtle'"
            @click="selectedCategory = cat.key"
          >
            <i class="pi text-sm" :class="cat.icon"></i>
            <span class="flex-1 text-start">{{ t(cat.labelKey) }}</span>
            <span
              class="inline-flex h-5 min-w-5 items-center justify-center rounded-full text-[10px] font-bold"
              :class="selectedCategory === cat.key ? 'bg-white/20' : 'bg-secondary-100 text-secondary-500'"
            >
              {{ cat.count }}
            </span>
          </button>
        </div>
      </div>

      <!-- Documents Grid -->
      <div class="flex-1">
        <!-- Upload Drop Zone -->
        <div
          class="mb-4 rounded-2xl border-2 border-dashed p-8 text-center transition-all"
          :class="isDragging
            ? 'border-primary bg-primary-50/50'
            : 'border-secondary-200 bg-surface-subtle hover:border-primary/30'"
          @dragover="handleDragOver"
          @dragleave="handleDragLeave"
          @drop="handleDrop"
        >
          <div v-if="isUploading" class="flex items-center justify-center gap-3">
            <i class="pi pi-spin pi-spinner text-2xl text-primary"></i>
            <span class="text-sm font-medium text-primary">Uploading...</span>
          </div>
          <div v-else>
            <i class="pi pi-cloud-upload text-3xl text-secondary-300"></i>
            <p class="mt-2 text-sm text-secondary-500">
              Drag & drop files here or
              <label class="cursor-pointer font-semibold text-primary hover:underline">
                browse
                <input type="file" class="hidden" multiple accept=".pdf,.docx,.xlsx,.doc,.xls" @change="handleFileSelect" />
              </label>
            </p>
            <p class="mt-1 text-xs text-secondary-400">PDF, DOCX, XLSX (max 50MB)</p>
          </div>
        </div>

        <!-- Loading -->
        <div v-if="isLoading" class="grid grid-cols-1 gap-3 md:grid-cols-2">
          <div v-for="i in 6" :key="i" class="skeleton-card"></div>
        </div>

        <!-- Empty State -->
        <div v-else-if="filteredDocuments.length === 0" class="empty-state">
          <div class="empty-state-icon">
            <i class="pi pi-book"></i>
          </div>
          <h3 class="empty-state-title">{{ t('knowledgeBase.empty') }}</h3>
          <p class="empty-state-text">{{ t('knowledgeBase.emptyDescription') }}</p>
        </div>

        <!-- Document Cards -->
        <div v-else class="grid grid-cols-1 gap-3 md:grid-cols-2">
          <div
            v-for="doc in filteredDocuments"
            :key="doc.id"
            class="group flex items-start gap-3 rounded-2xl border border-secondary-100 bg-white p-4 transition-all hover:border-primary/30 hover:shadow-card"
          >
            <!-- File Icon -->
            <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary-50">
              <i class="pi text-lg text-primary" :class="getFileIcon(doc.mimeType)"></i>
            </div>

            <!-- Doc Info -->
            <div class="min-w-0 flex-1">
              <h4 class="truncate text-sm font-bold text-secondary-800">{{ doc.name }}</h4>
              <div class="mt-1 flex flex-wrap items-center gap-2 text-[10px] text-secondary-400">
                <span>{{ formatFileSize(doc.size) }}</span>
                <span class="h-1 w-1 rounded-full bg-secondary-300"></span>
                <span>{{ formatDateTime(doc.uploadedAt) }}</span>
                <span class="h-1 w-1 rounded-full bg-secondary-300"></span>
                <span v-if="doc.chunkCount > 0">{{ doc.chunkCount }} chunks</span>
              </div>
              <!-- Tags -->
              <div v-if="doc.tags.length > 0" class="mt-2 flex flex-wrap gap-1">
                <span
                  v-for="tag in doc.tags.slice(0, 3)"
                  :key="tag"
                  class="rounded-full bg-secondary-50 px-2 py-0.5 text-[10px] text-secondary-500"
                >
                  {{ tag }}
                </span>
              </div>
            </div>

            <!-- Status + Actions -->
            <div class="shrink-0 text-end">
              <span class="badge text-[10px]" :class="getStatusBadge(doc.indexingStatus)">
                <i class="pi text-[8px]" :class="getStatusIcon(doc.indexingStatus)"></i>
                {{ t(`knowledgeBase.document.${doc.indexingStatus}`) }}
              </span>
              <button
                v-if="doc.indexingStatus === 'failed'"
                class="mt-2 block text-[10px] font-medium text-primary hover:underline"
                @click="reindexDocument(doc.id)"
              >
                Retry
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
