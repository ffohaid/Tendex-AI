<script setup lang="ts">
/**
 * RfpTemplatesView - Competition Templates Management
 *
 * Features:
 * - Browse and search competition templates
 * - Create new template (from scratch or from existing competition)
 * - Copy competition from template
 * - Template categories (IT, Construction, Consulting, etc.)
 * - Template preview with section/BOQ/criteria counts
 */
import { ref, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet, httpPost } from '@/services/http'

const { locale } = useI18n()
const router = useRouter()

interface RfpTemplate {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  category: string
  competitionType: number
  sectionCount: number
  boqItemCount: number
  evaluationCriteriaCount: number
  usageCount: number
  isOfficial: boolean
  isActive: boolean
  tags: string | null
  createdAt: string
  createdBy: string
}

interface CompetitionListItem {
  id: string
  projectNameAr: string
  projectNameEn: string
  referenceNumber: string
  status: number
}

const isLoading = ref(false)
const isCopying = ref<string | null>(null)
const templates = ref<RfpTemplate[]>([])
const totalCount = ref(0)
const searchQuery = ref('')
const selectedCategory = ref('all')
const currentPage = ref(1)
const pageSize = ref(12)

// Create template dialog
const showCreateDialog = ref(false)
const isCreating = ref(false)
const createForm = ref({
  nameAr: '',
  nameEn: '',
  descriptionAr: '',
  descriptionEn: '',
  category: 'it',
  competitionType: 0,
  tags: '',
  isOfficial: false,
  sourceCompetitionId: null as string | null
})
const createMode = ref<'scratch' | 'competition'>('scratch')
const competitions = ref<CompetitionListItem[]>([])
const isLoadingCompetitions = ref(false)

// Copy from template dialog
const showCopyDialog = ref(false)
const selectedTemplate = ref<RfpTemplate | null>(null)
const copyForm = ref({
  projectNameAr: '',
  projectNameEn: '',
  description: ''
})

const categories = [
  { key: 'all', labelAr: 'الكل', labelEn: 'All' },
  { key: 'it', labelAr: 'تقنية المعلومات', labelEn: 'Information Technology' },
  { key: 'construction', labelAr: 'الإنشاءات', labelEn: 'Construction' },
  { key: 'consulting', labelAr: 'الاستشارات', labelEn: 'Consulting' },
  { key: 'maintenance', labelAr: 'الصيانة والتشغيل', labelEn: 'Maintenance' },
  { key: 'supplies', labelAr: 'التوريدات والمعدات', labelEn: 'Supplies & Equipment' },
  { key: 'services', labelAr: 'الخدمات العامة', labelEn: 'General Services' },
]

const competitionTypes = [
  { value: 0, labelAr: 'منافسة عامة', labelEn: 'Public Competition' },
  { value: 1, labelAr: 'منافسة محدودة', labelEn: 'Limited Competition' },
  { value: 2, labelAr: 'شراء مباشر', labelEn: 'Direct Purchase' },
  { value: 3, labelAr: 'اتفاقية إطارية', labelEn: 'Framework Agreement' },
  { value: 4, labelAr: 'مسابقة', labelEn: 'Contest' },
  { value: 5, labelAr: 'مزايدة', labelEn: 'Auction' },
]

function getCategoryLabel(key: string): string {
  const cat = categories.find(c => c.key === key)
  if (!cat) return key
  return locale.value === 'ar' ? cat.labelAr : cat.labelEn
}

function getCompetitionTypeLabel(value: number): string {
  const ct = competitionTypes.find(c => c.value === value)
  if (!ct) return ''
  return locale.value === 'ar' ? ct.labelAr : ct.labelEn
}

function formatDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString(locale.value === 'ar' ? 'ar-SA' : 'en-US', {
      year: 'numeric', month: 'short', day: 'numeric'
    })
  } catch {
    return dateStr
  }
}

async function loadTemplates(): Promise<void> {
  isLoading.value = true
  try {
    const params = new URLSearchParams({
      page: currentPage.value.toString(),
      pageSize: pageSize.value.toString()
    })
    if (selectedCategory.value !== 'all') params.set('category', selectedCategory.value)
    if (searchQuery.value) params.set('search', searchQuery.value)

    const data = await httpGet<{ items: RfpTemplate[]; totalCount: number }>(`/v1/rfp/templates?${params}`)
    templates.value = data.items
    totalCount.value = data.totalCount
  } catch {
    console.warn('Templates API not available yet')
    templates.value = []
    totalCount.value = 0
  } finally {
    isLoading.value = false
  }
}

async function loadCompetitions(): Promise<void> {
  isLoadingCompetitions.value = true
  try {
    const data = await httpGet<{ items: CompetitionListItem[] }>('/v1/competitions?page=1&pageSize=100')
    competitions.value = data.items || []
  } catch {
    competitions.value = []
  } finally {
    isLoadingCompetitions.value = false
  }
}

function openCreateDialog(): void {
  createForm.value = {
    nameAr: '',
    nameEn: '',
    descriptionAr: '',
    descriptionEn: '',
    category: 'it',
    competitionType: 0,
    tags: '',
    isOfficial: false,
    sourceCompetitionId: null
  }
  createMode.value = 'scratch'
  showCreateDialog.value = true
}

async function handleCreate(): Promise<void> {
  if (!createForm.value.nameAr.trim()) return

  isCreating.value = true
  try {
    await httpPost('/v1/rfp/templates', {
      nameAr: createForm.value.nameAr,
      nameEn: createForm.value.nameEn || createForm.value.nameAr,
      descriptionAr: createForm.value.descriptionAr || null,
      descriptionEn: createForm.value.descriptionEn || null,
      category: createForm.value.category,
      competitionType: createForm.value.competitionType,
      tags: createForm.value.tags || null,
      isOfficial: createForm.value.isOfficial,
      sourceCompetitionId: createMode.value === 'competition' ? createForm.value.sourceCompetitionId : null
    })
    showCreateDialog.value = false
    await loadTemplates()
  } catch (err) {
    console.error('Failed to create template', err)
  } finally {
    isCreating.value = false
  }
}

function openCopyDialog(tmpl: RfpTemplate): void {
  selectedTemplate.value = tmpl
  copyForm.value = { projectNameAr: '', projectNameEn: '', description: '' }
  showCopyDialog.value = true
}

async function handleCopyFromTemplate(): Promise<void> {
  if (!selectedTemplate.value || !copyForm.value.projectNameAr.trim()) return

  isCopying.value = selectedTemplate.value.id
  try {
    const result = await httpPost<{ rfpId: string }>(
      `/v1/rfp/templates/${selectedTemplate.value.id}/copy`,
      {
        projectNameAr: copyForm.value.projectNameAr,
        projectNameEn: copyForm.value.projectNameEn || copyForm.value.projectNameAr,
        description: copyForm.value.description || null
      }
    )
    showCopyDialog.value = false
    router.push({ name: 'RfpCreate', query: { id: result.rfpId } })
  } catch {
    console.error('Failed to copy from template')
  } finally {
    isCopying.value = null
  }
}

watch([selectedCategory, searchQuery], () => {
  currentPage.value = 1
  loadTemplates()
})

watch(createMode, (mode) => {
  if (mode === 'competition' && competitions.value.length === 0) {
    loadCompetitions()
  }
})

onMounted(() => {
  loadTemplates()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary-800">
          {{ locale === 'ar' ? 'قوالب المنافسات' : 'Competition Templates' }}
        </h1>
        <p class="mt-1 text-sm text-secondary-500">
          {{ locale === 'ar' ? 'إنشاء وإدارة قوالب كراسات الشروط والمواصفات الجاهزة' : 'Create and manage pre-built competition templates' }}
        </p>
      </div>
      <button
        class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 hover:shadow-md"
        @click="openCreateDialog"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ locale === 'ar' ? 'إضافة قالب جديد' : 'Add New Template' }}
      </button>
    </div>

    <!-- Search + Categories -->
    <div class="rounded-2xl border border-secondary-100 bg-white p-4 shadow-sm">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-center">
        <div class="relative flex-1">
          <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-secondary-400"></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="locale === 'ar' ? 'البحث في القوالب...' : 'Search templates...'"
            class="w-full rounded-xl border border-secondary-200 bg-surface-subtle py-2.5 ps-10 pe-4 text-sm outline-none transition-colors focus:border-primary focus:ring-2 focus:ring-primary/20"
          />
        </div>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="cat in categories"
            :key="cat.key"
            class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors"
            :class="selectedCategory === cat.key
              ? 'bg-primary text-white'
              : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
            @click="selectedCategory = cat.key"
          >
            {{ locale === 'ar' ? cat.labelAr : cat.labelEn }}
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div v-for="i in 6" :key="i" class="h-64 animate-pulse rounded-2xl border border-secondary-100 bg-white"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="templates.length === 0" class="rounded-2xl border border-secondary-100 bg-white py-16 text-center shadow-sm">
      <div class="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-2xl bg-primary-50">
        <i class="pi pi-copy text-3xl text-primary"></i>
      </div>
      <h3 class="text-lg font-bold text-secondary-700">
        {{ locale === 'ar' ? 'لا توجد قوالب حالياً' : 'No templates found' }}
      </h3>
      <p class="mx-auto mt-2 max-w-md text-sm text-secondary-500">
        {{ locale === 'ar'
          ? 'يمكنك إنشاء قالب جديد من البداية أو تحويل منافسة موجودة إلى قالب قابل لإعادة الاستخدام.'
          : 'You can create a new template from scratch or convert an existing competition into a reusable template.' }}
      </p>
      <button
        class="mx-auto mt-6 inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600"
        @click="openCreateDialog"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ locale === 'ar' ? 'إضافة أول قالب' : 'Add First Template' }}
      </button>
    </div>

    <!-- Templates Grid -->
    <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="tmpl in templates"
        :key="tmpl.id"
        class="group rounded-2xl border border-secondary-100 bg-white p-5 shadow-sm transition-all hover:border-primary/30 hover:shadow-md"
      >
        <!-- Header -->
        <div class="mb-3 flex items-start justify-between">
          <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-primary-50 transition-colors group-hover:bg-primary-100">
            <i class="pi pi-file-edit text-lg text-primary"></i>
          </div>
          <div class="flex items-center gap-1.5">
            <span v-if="tmpl.isOfficial" class="inline-flex items-center gap-1 rounded-full bg-green-50 px-2 py-0.5 text-[10px] font-semibold text-green-700">
              <i class="pi pi-verified text-[8px]"></i>
              {{ locale === 'ar' ? 'رسمي' : 'Official' }}
            </span>
            <span class="rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-semibold text-blue-700">
              {{ getCategoryLabel(tmpl.category) }}
            </span>
          </div>
        </div>

        <!-- Title & Description -->
        <h3 class="text-sm font-bold text-secondary-800">
          {{ locale === 'ar' ? tmpl.nameAr : tmpl.nameEn }}
        </h3>
        <p class="mt-1 line-clamp-2 text-xs text-secondary-500">
          {{ locale === 'ar' ? (tmpl.descriptionAr || 'بدون وصف') : (tmpl.descriptionEn || 'No description') }}
        </p>

        <!-- Stats -->
        <div class="mt-3 flex items-center gap-3 text-[10px] text-secondary-400">
          <span class="inline-flex items-center gap-1">
            <i class="pi pi-list"></i>
            {{ tmpl.sectionCount }} {{ locale === 'ar' ? 'قسم' : 'sections' }}
          </span>
          <span class="inline-flex items-center gap-1">
            <i class="pi pi-table"></i>
            {{ tmpl.boqItemCount }} {{ locale === 'ar' ? 'بند' : 'BOQ items' }}
          </span>
          <span class="inline-flex items-center gap-1">
            <i class="pi pi-chart-bar"></i>
            {{ tmpl.evaluationCriteriaCount }} {{ locale === 'ar' ? 'معيار' : 'criteria' }}
          </span>
        </div>

        <div class="mt-2 flex items-center gap-3 text-[10px] text-secondary-400">
          <span class="inline-flex items-center gap-1">
            <i class="pi pi-copy"></i>
            {{ locale === 'ar' ? `استُخدم ${tmpl.usageCount} مرة` : `Used ${tmpl.usageCount} times` }}
          </span>
          <span class="inline-flex items-center gap-1">
            <i class="pi pi-calendar"></i>
            {{ formatDate(tmpl.createdAt) }}
          </span>
        </div>

        <!-- Tags -->
        <div v-if="tmpl.tags" class="mt-2 flex flex-wrap gap-1">
          <span
            v-for="tag in tmpl.tags.split(',').slice(0, 4)"
            :key="tag"
            class="rounded-full bg-secondary-50 px-2 py-0.5 text-[10px] text-secondary-500"
          >
            {{ tag.trim() }}
          </span>
        </div>

        <!-- Competition Type -->
        <div class="mt-3 rounded-lg bg-surface-subtle px-3 py-1.5 text-[11px] text-secondary-600">
          <i class="pi pi-tag me-1 text-[10px]"></i>
          {{ getCompetitionTypeLabel(tmpl.competitionType) }}
        </div>

        <!-- Use Template Button -->
        <button
          class="mt-4 w-full rounded-xl bg-primary py-2.5 text-xs font-semibold text-white transition-all hover:bg-primary-600 disabled:opacity-50"
          :disabled="isCopying === tmpl.id"
          @click="openCopyDialog(tmpl)"
        >
          <i class="pi me-1 text-[10px]" :class="isCopying === tmpl.id ? 'pi-spin pi-spinner' : 'pi-copy'"></i>
          {{ locale === 'ar' ? 'استخدام هذا القالب' : 'Use This Template' }}
        </button>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalCount > pageSize" class="flex items-center justify-center gap-2">
      <button
        class="rounded-lg border border-secondary-200 px-3 py-1.5 text-xs transition-colors hover:bg-secondary-50 disabled:opacity-50"
        :disabled="currentPage <= 1"
        @click="currentPage--; loadTemplates()"
      >
        {{ locale === 'ar' ? 'السابق' : 'Previous' }}
      </button>
      <span class="text-xs text-secondary-500">
        {{ locale === 'ar' ? `صفحة ${currentPage} من ${Math.ceil(totalCount / pageSize)}` : `Page ${currentPage} of ${Math.ceil(totalCount / pageSize)}` }}
      </span>
      <button
        class="rounded-lg border border-secondary-200 px-3 py-1.5 text-xs transition-colors hover:bg-secondary-50 disabled:opacity-50"
        :disabled="currentPage >= Math.ceil(totalCount / pageSize)"
        @click="currentPage++; loadTemplates()"
      >
        {{ locale === 'ar' ? 'التالي' : 'Next' }}
      </button>
    </div>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Create Template Dialog -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <div v-if="showCreateDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" @click.self="showCreateDialog = false">
        <div class="w-full max-w-2xl rounded-2xl bg-white shadow-2xl" dir="rtl">
          <!-- Dialog Header -->
          <div class="flex items-center justify-between border-b border-secondary-100 px-6 py-4">
            <h2 class="text-lg font-bold text-secondary-800">
              {{ locale === 'ar' ? 'إضافة قالب جديد' : 'Add New Template' }}
            </h2>
            <button class="rounded-lg p-1.5 text-secondary-400 transition-colors hover:bg-secondary-50 hover:text-secondary-600" @click="showCreateDialog = false">
              <i class="pi pi-times text-sm"></i>
            </button>
          </div>

          <!-- Dialog Body -->
          <div class="max-h-[70vh] overflow-y-auto px-6 py-5">
            <!-- Creation Mode -->
            <div class="mb-5">
              <label class="mb-2 block text-sm font-semibold text-secondary-700">
                {{ locale === 'ar' ? 'طريقة الإنشاء' : 'Creation Method' }}
              </label>
              <div class="flex gap-3">
                <button
                  class="flex-1 rounded-xl border-2 p-3 text-center text-sm transition-all"
                  :class="createMode === 'scratch' ? 'border-primary bg-primary-50 text-primary' : 'border-secondary-200 text-secondary-600 hover:border-secondary-300'"
                  @click="createMode = 'scratch'"
                >
                  <i class="pi pi-pencil mb-1 block text-lg"></i>
                  {{ locale === 'ar' ? 'من البداية' : 'From Scratch' }}
                </button>
                <button
                  class="flex-1 rounded-xl border-2 p-3 text-center text-sm transition-all"
                  :class="createMode === 'competition' ? 'border-primary bg-primary-50 text-primary' : 'border-secondary-200 text-secondary-600 hover:border-secondary-300'"
                  @click="createMode = 'competition'"
                >
                  <i class="pi pi-clone mb-1 block text-lg"></i>
                  {{ locale === 'ar' ? 'من منافسة موجودة' : 'From Existing Competition' }}
                </button>
              </div>
            </div>

            <!-- Source Competition (if from competition) -->
            <div v-if="createMode === 'competition'" class="mb-5">
              <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                {{ locale === 'ar' ? 'اختر المنافسة المصدر' : 'Select Source Competition' }}
              </label>
              <select
                v-model="createForm.sourceCompetitionId"
                class="w-full rounded-xl border border-secondary-200 bg-white px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              >
                <option :value="null">{{ locale === 'ar' ? '-- اختر منافسة --' : '-- Select Competition --' }}</option>
                <option v-for="comp in competitions" :key="comp.id" :value="comp.id">
                  {{ comp.referenceNumber }} - {{ locale === 'ar' ? comp.projectNameAr : comp.projectNameEn }}
                </option>
              </select>
              <p v-if="isLoadingCompetitions" class="mt-1 text-xs text-secondary-400">
                <i class="pi pi-spin pi-spinner me-1"></i>
                {{ locale === 'ar' ? 'جارٍ تحميل المنافسات...' : 'Loading competitions...' }}
              </p>
            </div>

            <!-- Template Name -->
            <div class="mb-4 grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'اسم القالب (عربي)' : 'Template Name (Arabic)' }} *
                </label>
                <input
                  v-model="createForm.nameAr"
                  type="text"
                  :placeholder="locale === 'ar' ? 'مثال: قالب منافسة تقنية المعلومات' : 'e.g., IT Competition Template'"
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'اسم القالب (إنجليزي)' : 'Template Name (English)' }}
                </label>
                <input
                  v-model="createForm.nameEn"
                  type="text"
                  dir="ltr"
                  placeholder="e.g., IT Competition Template"
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>
            </div>

            <!-- Description -->
            <div class="mb-4 grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'الوصف (عربي)' : 'Description (Arabic)' }}
                </label>
                <textarea
                  v-model="createForm.descriptionAr"
                  rows="3"
                  :placeholder="locale === 'ar' ? 'وصف مختصر للقالب...' : 'Brief template description...'"
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                ></textarea>
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'الوصف (إنجليزي)' : 'Description (English)' }}
                </label>
                <textarea
                  v-model="createForm.descriptionEn"
                  rows="3"
                  dir="ltr"
                  placeholder="Brief template description..."
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                ></textarea>
              </div>
            </div>

            <!-- Category & Competition Type -->
            <div class="mb-4 grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'التصنيف' : 'Category' }}
                </label>
                <select
                  v-model="createForm.category"
                  class="w-full rounded-xl border border-secondary-200 bg-white px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                >
                  <option v-for="cat in categories.filter(c => c.key !== 'all')" :key="cat.key" :value="cat.key">
                    {{ locale === 'ar' ? cat.labelAr : cat.labelEn }}
                  </option>
                </select>
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'نوع المنافسة' : 'Competition Type' }}
                </label>
                <select
                  v-model="createForm.competitionType"
                  class="w-full rounded-xl border border-secondary-200 bg-white px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                >
                  <option v-for="ct in competitionTypes" :key="ct.value" :value="ct.value">
                    {{ locale === 'ar' ? ct.labelAr : ct.labelEn }}
                  </option>
                </select>
              </div>
            </div>

            <!-- Tags -->
            <div class="mb-4">
              <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                {{ locale === 'ar' ? 'الوسوم (مفصولة بفاصلة)' : 'Tags (comma-separated)' }}
              </label>
              <input
                v-model="createForm.tags"
                type="text"
                :placeholder="locale === 'ar' ? 'مثال: تقنية, برمجيات, أنظمة' : 'e.g., technology, software, systems'"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <!-- Official Toggle -->
            <div class="mb-2 flex items-center gap-3">
              <label class="relative inline-flex cursor-pointer items-center">
                <input v-model="createForm.isOfficial" type="checkbox" class="peer sr-only" />
                <div class="peer h-5 w-9 rounded-full bg-secondary-200 after:absolute after:start-[2px] after:top-[2px] after:h-4 after:w-4 after:rounded-full after:bg-white after:transition-all peer-checked:bg-primary peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full"></div>
              </label>
              <span class="text-sm text-secondary-700">
                {{ locale === 'ar' ? 'قالب رسمي معتمد' : 'Official approved template' }}
              </span>
            </div>
          </div>

          <!-- Dialog Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 px-6 py-4">
            <button
              class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-semibold text-secondary-600 transition-colors hover:bg-secondary-50"
              @click="showCreateDialog = false"
            >
              {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
            </button>
            <button
              class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition-all hover:bg-primary-600 disabled:opacity-50"
              :disabled="isCreating || !createForm.nameAr.trim()"
              @click="handleCreate"
            >
              <i v-if="isCreating" class="pi pi-spin pi-spinner text-xs"></i>
              <i v-else class="pi pi-check text-xs"></i>
              {{ locale === 'ar' ? 'إنشاء القالب' : 'Create Template' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Copy From Template Dialog -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <div v-if="showCopyDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" @click.self="showCopyDialog = false">
        <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl" dir="rtl">
          <!-- Dialog Header -->
          <div class="flex items-center justify-between border-b border-secondary-100 px-6 py-4">
            <h2 class="text-lg font-bold text-secondary-800">
              {{ locale === 'ar' ? 'إنشاء منافسة من القالب' : 'Create Competition from Template' }}
            </h2>
            <button class="rounded-lg p-1.5 text-secondary-400 transition-colors hover:bg-secondary-50" @click="showCopyDialog = false">
              <i class="pi pi-times text-sm"></i>
            </button>
          </div>

          <!-- Dialog Body -->
          <div class="px-6 py-5">
            <!-- Template Info -->
            <div v-if="selectedTemplate" class="mb-5 rounded-xl bg-primary-50 p-3">
              <p class="text-sm font-semibold text-primary">
                {{ locale === 'ar' ? selectedTemplate.nameAr : selectedTemplate.nameEn }}
              </p>
              <p class="mt-1 text-xs text-primary/70">
                {{ selectedTemplate.sectionCount }} {{ locale === 'ar' ? 'قسم' : 'sections' }} |
                {{ selectedTemplate.boqItemCount }} {{ locale === 'ar' ? 'بند' : 'BOQ items' }} |
                {{ selectedTemplate.evaluationCriteriaCount }} {{ locale === 'ar' ? 'معيار' : 'criteria' }}
              </p>
            </div>

            <div class="mb-4">
              <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                {{ locale === 'ar' ? 'اسم المشروع (عربي)' : 'Project Name (Arabic)' }} *
              </label>
              <input
                v-model="copyForm.projectNameAr"
                type="text"
                :placeholder="locale === 'ar' ? 'أدخل اسم المشروع بالعربية' : 'Enter project name in Arabic'"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <div class="mb-4">
              <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                {{ locale === 'ar' ? 'اسم المشروع (إنجليزي)' : 'Project Name (English)' }}
              </label>
              <input
                v-model="copyForm.projectNameEn"
                type="text"
                dir="ltr"
                placeholder="Enter project name in English"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <div class="mb-4">
              <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                {{ locale === 'ar' ? 'وصف المشروع' : 'Project Description' }}
              </label>
              <textarea
                v-model="copyForm.description"
                rows="3"
                :placeholder="locale === 'ar' ? 'وصف مختصر للمشروع...' : 'Brief project description...'"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              ></textarea>
            </div>
          </div>

          <!-- Dialog Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 px-6 py-4">
            <button
              class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-semibold text-secondary-600 transition-colors hover:bg-secondary-50"
              @click="showCopyDialog = false"
            >
              {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
            </button>
            <button
              class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition-all hover:bg-primary-600 disabled:opacity-50"
              :disabled="!copyForm.projectNameAr.trim() || isCopying !== null"
              @click="handleCopyFromTemplate"
            >
              <i v-if="isCopying" class="pi pi-spin pi-spinner text-xs"></i>
              <i v-else class="pi pi-copy text-xs"></i>
              {{ locale === 'ar' ? 'إنشاء المنافسة' : 'Create Competition' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
