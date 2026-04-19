<script setup lang="ts">
/**
 * TemplateLibraryView - Unified Template Library
 *
 * Merges the two previous template concepts into one cohesive page:
 * 1. "قوالب كراسات الشروط" (Booklet Templates) - uploaded DOCX files from EXPRO
 * 2. "قوالب من منافسات سابقة" (Competition Templates) - snapshots from previous competitions
 *
 * Features:
 * - Two clear tabs with distinct icons and descriptions
 * - Upload DOCX booklet templates with EXPRO color coding
 * - Create competition templates from existing competitions
 * - Use any template to create a new booklet/competition
 * - Search and filter by category
 * - Unified, user-friendly experience
 */
import { ref, onMounted, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet, httpPost } from '@/services/http'

const { locale } = useI18n()
const router = useRouter()

// ═══════════════════════════════════════════════════════════════
//  Types
// ═══════════════════════════════════════════════════════════════

interface BookletTemplate {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  category: string
  sourceReference: string | null
  originalFileName: string | null
  sectionCount: number
  usageCount: number
  isActive: boolean
  createdAt: string
}

interface CompetitionTemplate {
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

// ═══════════════════════════════════════════════════════════════
//  State
// ═══════════════════════════════════════════════════════════════

const activeTab = ref<'booklet' | 'competition'>('booklet')
const isLoading = ref(false)
const searchQuery = ref('')
const selectedCategory = ref('all')
const currentPage = ref(1)
const pageSize = ref(12)

// Booklet templates
const bookletTemplates = ref<BookletTemplate[]>([])
const bookletTotalCount = ref(0)

// Competition templates
const competitionTemplates = ref<CompetitionTemplate[]>([])
const competitionTotalCount = ref(0)

// Upload booklet dialog
const showUploadDialog = ref(false)
const isUploading = ref(false)
const uploadError = ref('')
const uploadSuccess = ref(false)
const uploadFile = ref<File | null>(null)
const uploadForm = ref({
  nameAr: '',
  nameEn: '',
  descriptionAr: '',
  descriptionEn: '',
  category: 'it',
  sourceReference: ''
})

// Create competition template dialog
const showCreateCompTemplateDialog = ref(false)
const isCreatingTemplate = ref(false)
const createTemplateMode = ref<'competition'>('competition')
const competitions = ref<CompetitionListItem[]>([])
const isLoadingCompetitions = ref(false)
const createTemplateForm = ref({
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

// Use template dialog (create booklet from booklet template)
const showUseBookletDialog = ref(false)
const isCreatingBooklet = ref(false)
const createBookletError = ref('')
const createBookletSuccess = ref(false)
const selectedBookletTemplate = ref<BookletTemplate | null>(null)
const useBookletForm = ref({
  projectNameAr: '',
  projectNameEn: '',
  descriptionAr: ''
})

// Use template dialog (create competition from competition template)
const showUseCompDialog = ref(false)
const isCopyingComp = ref(false)
const copyCompError = ref('')
const copyCompSuccess = ref(false)
const selectedCompTemplate = ref<CompetitionTemplate | null>(null)
const useCompForm = ref({
  projectNameAr: '',
  projectNameEn: '',
  description: ''
})

// ═══════════════════════════════════════════════════════════════
//  Constants
// ═══════════════════════════════════════════════════════════════

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

// ═══════════════════════════════════════════════════════════════
//  Computed
// ═══════════════════════════════════════════════════════════════

const totalCount = computed(() =>
  activeTab.value === 'booklet' ? bookletTotalCount.value : competitionTotalCount.value
)

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))

// ═══════════════════════════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════════════════════════

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

// ═══════════════════════════════════════════════════════════════
//  Data Loading
// ═══════════════════════════════════════════════════════════════

async function loadBookletTemplates(): Promise<void> {
  isLoading.value = true
  try {
    const params = new URLSearchParams({
      page: currentPage.value.toString(),
      pageSize: pageSize.value.toString()
    })
    if (selectedCategory.value !== 'all') params.set('category', selectedCategory.value)
    if (searchQuery.value) params.set('search', searchQuery.value)

    const data = await httpGet<{ items: BookletTemplate[]; totalCount: number }>(`/v1/booklet-templates?${params}`)
    bookletTemplates.value = data.items
    bookletTotalCount.value = data.totalCount
  } catch {
    bookletTemplates.value = []
    bookletTotalCount.value = 0
  } finally {
    isLoading.value = false
  }
}

async function loadCompetitionTemplates(): Promise<void> {
  isLoading.value = true
  try {
    const params = new URLSearchParams({
      page: currentPage.value.toString(),
      pageSize: pageSize.value.toString()
    })
    if (selectedCategory.value !== 'all') params.set('category', selectedCategory.value)
    if (searchQuery.value) params.set('search', searchQuery.value)

    const data = await httpGet<{ items: CompetitionTemplate[]; totalCount: number }>(`/v1/rfp/templates?${params}`)
    competitionTemplates.value = data.items
    competitionTotalCount.value = data.totalCount
  } catch {
    competitionTemplates.value = []
    competitionTotalCount.value = 0
  } finally {
    isLoading.value = false
  }
}

async function loadCompetitions(): Promise<void> {
  isLoadingCompetitions.value = true
  try {
    const data = await httpGet<{ items: CompetitionListItem[] }>('/v1/competitions?page=1&pageSize=100&status=5')
    competitions.value = data.items || []
  } catch {
    try {
      const data = await httpGet<{ items: CompetitionListItem[] }>('/v1/competitions?page=1&pageSize=100')
      competitions.value = data.items || []
    } catch {
      competitions.value = []
    }
  } finally {
    isLoadingCompetitions.value = false
  }
}

function loadCurrentTab(): void {
  if (activeTab.value === 'booklet') {
    loadBookletTemplates()
  } else {
    loadCompetitionTemplates()
  }
}

// ═══════════════════════════════════════════════════════════════
//  Upload Booklet Template
// ═══════════════════════════════════════════════════════════════

function openUploadDialog(): void {
  uploadForm.value = {
    nameAr: '',
    nameEn: '',
    descriptionAr: '',
    descriptionEn: '',
    category: 'it',
    sourceReference: ''
  }
  uploadFile.value = null
  uploadError.value = ''
  showUploadDialog.value = true
}

function handleFileSelect(event: Event): void {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    const file = input.files[0]
    if (!file.name.endsWith('.docx')) {
      uploadError.value = locale.value === 'ar' ? 'يجب أن يكون الملف بصيغة DOCX' : 'File must be in DOCX format'
      return
    }
    uploadFile.value = file
    uploadError.value = ''
    if (!uploadForm.value.nameAr) {
      uploadForm.value.nameAr = file.name.replace('.docx', '').replace(/[_-]/g, ' ')
    }
  }
}

async function handleUpload(): Promise<void> {
  if (!uploadFile.value || !uploadForm.value.nameAr.trim()) return

  isUploading.value = true
  uploadError.value = ''
  try {
    const formData = new FormData()
    formData.append('file', uploadFile.value)
    formData.append('nameAr', uploadForm.value.nameAr)
    formData.append('nameEn', uploadForm.value.nameEn || uploadForm.value.nameAr)
    if (uploadForm.value.descriptionAr) formData.append('descriptionAr', uploadForm.value.descriptionAr)
    if (uploadForm.value.descriptionEn) formData.append('descriptionEn', uploadForm.value.descriptionEn)
    formData.append('category', uploadForm.value.category)
    if (uploadForm.value.sourceReference) formData.append('sourceReference', uploadForm.value.sourceReference)

    await httpPost('/v1/booklet-templates/upload', formData)
    showUploadDialog.value = false
    uploadSuccess.value = true
    setTimeout(() => { uploadSuccess.value = false }, 5000)
    await loadBookletTemplates()
  } catch (err: unknown) {
    uploadError.value = err instanceof Error ? err.message : (locale.value === 'ar' ? 'حدث خطأ أثناء رفع القالب' : 'Error uploading template')
  } finally {
    isUploading.value = false
  }
}

// ═══════════════════════════════════════════════════════════════
//  Create Competition Template (from existing competition)
// ═══════════════════════════════════════════════════════════════

function openCreateCompTemplateDialog(): void {
  createTemplateForm.value = {
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
  createTemplateMode.value = 'competition'
  showCreateCompTemplateDialog.value = true
  if (competitions.value.length === 0) {
    loadCompetitions()
  }
}

async function handleCreateCompTemplate(): Promise<void> {
  if (!createTemplateForm.value.nameAr.trim() || !createTemplateForm.value.sourceCompetitionId) return

  isCreatingTemplate.value = true
  try {
    await httpPost('/v1/rfp/templates', {
      nameAr: createTemplateForm.value.nameAr,
      nameEn: createTemplateForm.value.nameEn || createTemplateForm.value.nameAr,
      descriptionAr: createTemplateForm.value.descriptionAr || null,
      descriptionEn: createTemplateForm.value.descriptionEn || null,
      category: createTemplateForm.value.category,
      competitionType: createTemplateForm.value.competitionType,
      tags: createTemplateForm.value.tags || null,
      isOfficial: createTemplateForm.value.isOfficial,
      sourceCompetitionId: createTemplateForm.value.sourceCompetitionId
    })
    showCreateCompTemplateDialog.value = false
    await loadCompetitionTemplates()
  } catch (err) {
    console.error('Failed to create template', err)
  } finally {
    isCreatingTemplate.value = false
  }
}

// ═══════════════════════════════════════════════════════════════
//  Use Booklet Template → Create Booklet
// ═══════════════════════════════════════════════════════════════

function openUseBookletDialog(tmpl: BookletTemplate): void {
  selectedBookletTemplate.value = tmpl
  useBookletForm.value = { projectNameAr: '', projectNameEn: '', descriptionAr: '' }
  createBookletError.value = ''
  createBookletSuccess.value = false
  showUseBookletDialog.value = true
}

async function handleCreateBooklet(): Promise<void> {
  if (!selectedBookletTemplate.value || !useBookletForm.value.projectNameAr.trim()) return

  isCreatingBooklet.value = true
  createBookletError.value = ''
  try {
    const result = await httpPost<{ rfpId: string }>(`/v1/booklet-templates/${selectedBookletTemplate.value.id}/create-booklet`, {
      projectNameAr: useBookletForm.value.projectNameAr,
      projectNameEn: useBookletForm.value.projectNameEn || useBookletForm.value.projectNameAr,
      descriptionAr: useBookletForm.value.descriptionAr || null
    })
    showUseBookletDialog.value = false
    createBookletSuccess.value = true
    setTimeout(() => {
      createBookletSuccess.value = false
      router.push({ name: 'BookletEditor', params: { id: result.rfpId } })
    }, 1000)
  } catch (err: unknown) {
    createBookletError.value = err instanceof Error ? err.message : (locale.value === 'ar' ? 'حدث خطأ أثناء إنشاء الكراسة' : 'Error creating booklet')
  } finally {
    isCreatingBooklet.value = false
  }
}

// ═══════════════════════════════════════════════════════════════
//  Use Competition Template → Create Competition
// ═══════════════════════════════════════════════════════════════

function openUseCompDialog(tmpl: CompetitionTemplate): void {
  selectedCompTemplate.value = tmpl
  useCompForm.value = { projectNameAr: '', projectNameEn: '', description: '' }
  copyCompError.value = ''
  copyCompSuccess.value = false
  showUseCompDialog.value = true
}

async function handleCopyFromCompTemplate(): Promise<void> {
  if (!selectedCompTemplate.value || !useCompForm.value.projectNameAr.trim()) return

  isCopyingComp.value = true
  copyCompError.value = ''
  try {
    const result = await httpPost<{ rfpId: string }>(
      `/v1/rfp/templates/${selectedCompTemplate.value.id}/copy`,
      {
        projectNameAr: useCompForm.value.projectNameAr,
        projectNameEn: useCompForm.value.projectNameEn || useCompForm.value.projectNameAr,
        description: useCompForm.value.description || null
      }
    )
    showUseCompDialog.value = false
    copyCompSuccess.value = true
    setTimeout(() => {
      copyCompSuccess.value = false
      router.push({ name: 'rfp-edit', params: { id: result.rfpId } })
    }, 1500)
  } catch (err: unknown) {
    copyCompError.value = err instanceof Error ? err.message : (locale.value === 'ar' ? 'حدث خطأ أثناء إنشاء المنافسة' : 'Error creating competition')
  } finally {
    isCopyingComp.value = false
  }
}

// ═══════════════════════════════════════════════════════════════
//  Watchers
// ═══════════════════════════════════════════════════════════════

watch(activeTab, () => {
  currentPage.value = 1
  searchQuery.value = ''
  selectedCategory.value = 'all'
  loadCurrentTab()
})

watch([selectedCategory, searchQuery], () => {
  currentPage.value = 1
  loadCurrentTab()
})

onMounted(() => {
  loadCurrentTab()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Success Notification -->
    <Transition name="fade">
      <div
        v-if="uploadSuccess"
        class="rounded-lg border border-green-200 bg-green-50 p-4 text-green-800"
      >
        <div class="flex items-center gap-2">
          <i class="pi pi-check-circle"></i>
          <span>{{ locale === 'ar' ? 'تم رفع القالب بنجاح' : 'Template uploaded successfully' }}</span>
        </div>
      </div>
    </Transition>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Header                                                     -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary-800">
          {{ locale === 'ar' ? 'مكتبة القوالب' : 'Template Library' }}
        </h1>
        <p class="mt-1 text-sm text-secondary-500">
          {{ locale === 'ar'
            ? 'إدارة قوالب كراسات الشروط والمنافسات السابقة لإعادة استخدامها في إنشاء كراسات جديدة'
            : 'Manage booklet and competition templates for reuse in creating new specifications' }}
        </p>
      </div>
      <!-- Action button changes based on active tab -->
      <button
        v-if="activeTab === 'booklet'"
        class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 hover:shadow-md"
        @click="openUploadDialog"
      >
        <i class="pi pi-upload text-xs"></i>
        {{ locale === 'ar' ? 'رفع قالب كراسة' : 'Upload Booklet Template' }}
      </button>
      <button
        v-else
        class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 hover:shadow-md"
        @click="openCreateCompTemplateDialog"
      >
        <i class="pi pi-plus text-xs"></i>
        {{ locale === 'ar' ? 'حفظ منافسة كقالب' : 'Save Competition as Template' }}
      </button>
    </div>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Tabs                                                       -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <div class="rounded-2xl border border-secondary-100 bg-white shadow-sm">
      <div class="flex border-b border-secondary-100">
        <button
          class="flex flex-1 items-center justify-center gap-3 px-6 py-4 text-sm font-semibold transition-all"
          :class="activeTab === 'booklet'
            ? 'border-b-2 border-primary text-primary bg-primary/5'
            : 'text-secondary-500 hover:text-secondary-700 hover:bg-secondary-50'"
          @click="activeTab = 'booklet'"
        >
          <i class="pi pi-file-word text-base"></i>
          <div class="text-start">
            <div>{{ locale === 'ar' ? 'قوالب كراسات الشروط' : 'Booklet Templates' }}</div>
            <div class="text-[10px] font-normal opacity-70">
              {{ locale === 'ar' ? 'قوالب DOCX معتمدة من هيئة كفاءة الإنفاق' : 'Official EXPRO DOCX templates' }}
            </div>
          </div>
          <span class="rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-bold text-primary">
            {{ bookletTotalCount }}
          </span>
        </button>
        <button
          class="flex flex-1 items-center justify-center gap-3 px-6 py-4 text-sm font-semibold transition-all"
          :class="activeTab === 'competition'
            ? 'border-b-2 border-primary text-primary bg-primary/5'
            : 'text-secondary-500 hover:text-secondary-700 hover:bg-secondary-50'"
          @click="activeTab = 'competition'"
        >
          <i class="pi pi-clone text-base"></i>
          <div class="text-start">
            <div>{{ locale === 'ar' ? 'قوالب من منافسات سابقة' : 'Competition Templates' }}</div>
            <div class="text-[10px] font-normal opacity-70">
              {{ locale === 'ar' ? 'نسخ محفوظة من منافسات مكتملة' : 'Saved copies from completed competitions' }}
            </div>
          </div>
          <span class="rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-bold text-primary">
            {{ competitionTotalCount }}
          </span>
        </button>
      </div>

      <!-- EXPRO Color Coding Legend (only for booklet tab) -->
      <div v-if="activeTab === 'booklet'" class="border-b border-secondary-100 px-6 py-3">
        <div class="flex flex-wrap items-center gap-4">
          <span class="text-xs font-semibold text-secondary-600">
            {{ locale === 'ar' ? 'نظام الألوان:' : 'Color Coding:' }}
          </span>
          <div class="flex items-center gap-1.5">
            <span class="inline-block h-3 w-3 rounded border border-gray-300 bg-gray-800"></span>
            <span class="text-[10px] text-secondary-500">{{ locale === 'ar' ? 'ثابت' : 'Fixed' }}</span>
          </div>
          <div class="flex items-center gap-1.5">
            <span class="inline-block h-3 w-3 rounded border border-green-300 bg-green-600"></span>
            <span class="text-[10px] text-secondary-500">{{ locale === 'ar' ? 'قابل للتعديل' : 'Editable' }}</span>
          </div>
          <div class="flex items-center gap-1.5">
            <span class="inline-block h-3 w-3 rounded border border-red-300 bg-red-600"></span>
            <span class="text-[10px] text-secondary-500">{{ locale === 'ar' ? 'أمثلة' : 'Examples' }}</span>
          </div>
          <div class="flex items-center gap-1.5">
            <span class="inline-block h-3 w-3 rounded border border-blue-300 bg-blue-600"></span>
            <span class="text-[10px] text-secondary-500">{{ locale === 'ar' ? 'إرشادات' : 'Guidance' }}</span>
          </div>
        </div>
      </div>

      <!-- Search + Categories -->
      <div class="px-6 py-4">
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
                ? 'bg-primary text-white shadow-sm'
                : 'bg-surface-subtle text-secondary-600 hover:bg-secondary-100'"
              @click="selectedCategory = cat.key"
            >
              {{ locale === 'ar' ? cat.labelAr : cat.labelEn }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Loading State                                              -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <div v-if="isLoading" class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div v-for="i in 6" :key="i" class="h-64 animate-pulse rounded-2xl border border-secondary-100 bg-white"></div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  TAB: Booklet Templates                                     -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'booklet'">
      <!-- Empty State -->
      <div v-if="bookletTemplates.length === 0" class="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-secondary-200 py-16">
        <div class="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-2xl bg-primary/10">
          <i class="pi pi-file-word text-4xl text-primary"></i>
        </div>
        <h3 class="text-lg font-bold text-secondary-700">
          {{ locale === 'ar' ? 'لا توجد قوالب كراسات شروط' : 'No Booklet Templates' }}
        </h3>
        <p class="mx-auto mt-2 max-w-md text-center text-sm text-secondary-500">
          {{ locale === 'ar'
            ? 'قم برفع نموذج كراسة شروط ومواصفات معتمد من هيئة كفاءة الإنفاق (EXPRO) بصيغة DOCX لإعادة استخدامه في إنشاء كراسات جديدة'
            : 'Upload an official EXPRO specification booklet template in DOCX format to reuse when creating new booklets' }}
        </p>
        <button
          class="mt-6 inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600"
          @click="openUploadDialog"
        >
          <i class="pi pi-upload text-xs"></i>
          {{ locale === 'ar' ? 'رفع أول قالب' : 'Upload First Template' }}
        </button>
      </div>

      <!-- Grid -->
      <div v-else class="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
        <div
          v-for="tmpl in bookletTemplates"
          :key="tmpl.id"
          class="group relative overflow-hidden rounded-2xl border border-secondary-100 bg-white shadow-sm transition-all hover:border-primary/30 hover:shadow-lg"
        >
          <!-- Header -->
          <div class="border-b border-secondary-100 bg-gradient-to-l from-primary/5 to-transparent p-5">
            <div class="flex items-start justify-between">
              <div class="flex items-center gap-3">
                <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-blue-50 transition-colors group-hover:bg-blue-100">
                  <i class="pi pi-file-word text-lg text-blue-600"></i>
                </div>
                <div class="flex-1">
                  <h3 class="text-sm font-bold text-secondary-800 line-clamp-2">
                    {{ locale === 'ar' ? tmpl.nameAr : tmpl.nameEn }}
                  </h3>
                  <p v-if="tmpl.descriptionAr" class="mt-0.5 text-[11px] text-secondary-500 line-clamp-1">
                    {{ locale === 'ar' ? tmpl.descriptionAr : (tmpl.descriptionEn || tmpl.descriptionAr) }}
                  </p>
                </div>
              </div>
              <span class="shrink-0 rounded-lg bg-primary/10 px-2 py-1 text-[10px] font-medium text-primary">
                {{ getCategoryLabel(tmpl.category) }}
              </span>
            </div>
          </div>

          <!-- Stats -->
          <div class="grid grid-cols-2 gap-3 p-4">
            <div class="rounded-lg bg-secondary-50 p-3 text-center">
              <div class="text-lg font-bold text-primary">{{ tmpl.sectionCount }}</div>
              <div class="text-[10px] text-secondary-500">{{ locale === 'ar' ? 'قسم' : 'Sections' }}</div>
            </div>
            <div class="rounded-lg bg-secondary-50 p-3 text-center">
              <div class="text-lg font-bold text-secondary-700">{{ tmpl.usageCount }}</div>
              <div class="text-[10px] text-secondary-500">{{ locale === 'ar' ? 'مرة استخدام' : 'Times Used' }}</div>
            </div>
          </div>

          <!-- Meta -->
          <div class="border-t border-secondary-100 px-5 py-2.5">
            <div class="flex items-center justify-between text-[10px] text-secondary-400">
              <span v-if="tmpl.originalFileName" class="flex items-center gap-1 truncate max-w-[60%]">
                <i class="pi pi-file-word"></i>
                {{ tmpl.originalFileName }}
              </span>
              <span>{{ formatDate(tmpl.createdAt) }}</span>
            </div>
          </div>

          <!-- Action -->
          <div class="border-t border-secondary-100 p-4">
            <button
              class="w-full rounded-xl bg-primary py-2.5 text-xs font-semibold text-white transition-all hover:bg-primary-600"
              @click="openUseBookletDialog(tmpl)"
            >
              <i class="pi pi-file-edit me-1.5 text-[10px]"></i>
              {{ locale === 'ar' ? 'إنشاء كراسة من هذا القالب' : 'Create Booklet from Template' }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  TAB: Competition Templates                                 -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <template v-else-if="activeTab === 'competition'">
      <!-- Empty State -->
      <div v-if="competitionTemplates.length === 0" class="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-secondary-200 py-16">
        <div class="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-2xl bg-purple-50">
          <i class="pi pi-clone text-4xl text-purple-500"></i>
        </div>
        <h3 class="text-lg font-bold text-secondary-700">
          {{ locale === 'ar' ? 'لا توجد قوالب من منافسات سابقة' : 'No Competition Templates' }}
        </h3>
        <p class="mx-auto mt-2 max-w-md text-center text-sm text-secondary-500">
          {{ locale === 'ar'
            ? 'يمكنك حفظ منافسة مكتملة كقالب لإعادة استخدام أقسامها وجدول كمياتها ومعايير تقييمها في منافسات جديدة'
            : 'Save a completed competition as a template to reuse its sections, BOQ, and evaluation criteria in new competitions' }}
        </p>
        <button
          class="mt-6 inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600"
          @click="openCreateCompTemplateDialog"
        >
          <i class="pi pi-plus text-xs"></i>
          {{ locale === 'ar' ? 'حفظ أول قالب' : 'Save First Template' }}
        </button>
      </div>

      <!-- Grid -->
      <div v-else class="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
        <div
          v-for="tmpl in competitionTemplates"
          :key="tmpl.id"
          class="group rounded-2xl border border-secondary-100 bg-white shadow-sm transition-all hover:border-primary/30 hover:shadow-lg"
        >
          <!-- Header -->
          <div class="p-5">
            <div class="mb-3 flex items-start justify-between">
              <div class="flex h-11 w-11 items-center justify-center rounded-xl bg-purple-50 transition-colors group-hover:bg-purple-100">
                <i class="pi pi-clone text-lg text-purple-600"></i>
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

            <!-- Competition Type -->
            <div class="mt-3 rounded-lg bg-surface-subtle px-3 py-1.5 text-[11px] text-secondary-600">
              <i class="pi pi-tag me-1 text-[10px]"></i>
              {{ getCompetitionTypeLabel(tmpl.competitionType) }}
            </div>
          </div>

          <!-- Action -->
          <div class="border-t border-secondary-100 p-4">
            <button
              class="w-full rounded-xl bg-primary py-2.5 text-xs font-semibold text-white transition-all hover:bg-primary-600"
              @click="openUseCompDialog(tmpl)"
            >
              <i class="pi pi-copy me-1.5 text-[10px]"></i>
              {{ locale === 'ar' ? 'إنشاء منافسة من هذا القالب' : 'Create Competition from Template' }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Pagination                                                 -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <div v-if="totalCount > pageSize" class="flex items-center justify-center gap-2">
      <button
        class="rounded-lg border border-secondary-200 px-3 py-1.5 text-xs transition-colors hover:bg-secondary-50 disabled:opacity-50"
        :disabled="currentPage <= 1"
        @click="currentPage--; loadCurrentTab()"
      >
        {{ locale === 'ar' ? 'السابق' : 'Previous' }}
      </button>
      <span class="text-xs text-secondary-500">
        {{ locale === 'ar' ? `صفحة ${currentPage} من ${totalPages}` : `Page ${currentPage} of ${totalPages}` }}
      </span>
      <button
        class="rounded-lg border border-secondary-200 px-3 py-1.5 text-xs transition-colors hover:bg-secondary-50 disabled:opacity-50"
        :disabled="currentPage >= totalPages"
        @click="currentPage++; loadCurrentTab()"
      >
        {{ locale === 'ar' ? 'التالي' : 'Next' }}
      </button>
    </div>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  DIALOG: Upload Booklet Template                            -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showUploadDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showUploadDialog = false">
          <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
            <div class="flex items-center justify-between border-b border-secondary-100 p-5">
              <h2 class="text-lg font-bold text-secondary-800">
                {{ locale === 'ar' ? 'رفع قالب كراسة شروط' : 'Upload Booklet Template' }}
              </h2>
              <button class="rounded-lg p-1 text-secondary-400 hover:bg-secondary-100" @click="showUploadDialog = false">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="max-h-[65vh] space-y-4 overflow-y-auto p-5">
              <!-- File Upload -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'ملف القالب (DOCX)' : 'Template File (DOCX)' }} *
                </label>
                <input
                  type="file"
                  accept=".docx"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 p-3 text-sm file:me-3 file:rounded-lg file:border-0 file:bg-primary/10 file:px-3 file:py-1 file:text-xs file:font-medium file:text-primary"
                  @change="handleFileSelect"
                />
                <p v-if="uploadFile" class="mt-1 text-xs text-green-600">
                  <i class="pi pi-check-circle me-1"></i>{{ uploadFile.name }} ({{ (uploadFile.size / 1024).toFixed(0) }} KB)
                </p>
              </div>

              <!-- Name AR -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم القالب (عربي)' : 'Template Name (Arabic)' }} *
                </label>
                <input
                  v-model="uploadForm.nameAr"
                  type="text"
                  :placeholder="locale === 'ar' ? 'مثال: نموذج كراسة الشروط والمواصفات (تقنية المعلومات)' : 'e.g., IT Specifications Booklet Template'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <!-- Name EN -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم القالب (إنجليزي)' : 'Template Name (English)' }}
                </label>
                <input
                  v-model="uploadForm.nameEn"
                  type="text"
                  dir="ltr"
                  placeholder="e.g., IT Specifications Booklet Template"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <!-- Category -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'التصنيف' : 'Category' }}
                </label>
                <select
                  v-model="uploadForm.category"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                >
                  <option v-for="cat in categories.filter(c => c.key !== 'all')" :key="cat.key" :value="cat.key">
                    {{ locale === 'ar' ? cat.labelAr : cat.labelEn }}
                  </option>
                </select>
              </div>

              <!-- Source Reference -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'المرجع النظامي' : 'Regulatory Reference' }}
                </label>
                <input
                  v-model="uploadForm.sourceReference"
                  type="text"
                  :placeholder="locale === 'ar' ? 'مثال: الدليل الاسترشادي لهيئة كفاءة الإنفاق' : 'e.g., EXPRO Guidelines'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <!-- Error -->
              <div v-if="uploadError" class="rounded-lg bg-red-50 p-3 text-sm text-red-600">
                <i class="pi pi-exclamation-triangle me-1"></i>{{ uploadError }}
              </div>
            </div>
            <div class="flex justify-end gap-3 border-t border-secondary-100 p-5">
              <button
                class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                @click="showUploadDialog = false"
              >
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 disabled:opacity-50"
                :disabled="isUploading || !uploadFile || !uploadForm.nameAr.trim()"
                @click="handleUpload"
              >
                <i v-if="isUploading" class="pi pi-spin pi-spinner me-2 text-xs"></i>
                {{ isUploading
                  ? (locale === 'ar' ? 'جاري الرفع والتحليل...' : 'Uploading & Parsing...')
                  : (locale === 'ar' ? 'رفع وتحليل القالب' : 'Upload & Parse Template') }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  DIALOG: Create Competition Template                        -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showCreateCompTemplateDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showCreateCompTemplateDialog = false">
          <div class="w-full max-w-2xl rounded-2xl bg-white shadow-2xl">
            <div class="flex items-center justify-between border-b border-secondary-100 px-6 py-4">
              <h2 class="text-lg font-bold text-secondary-800">
                {{ locale === 'ar' ? 'حفظ منافسة كقالب' : 'Save Competition as Template' }}
              </h2>
              <button class="rounded-lg p-1.5 text-secondary-400 hover:bg-secondary-50" @click="showCreateCompTemplateDialog = false">
                <i class="pi pi-times text-sm"></i>
              </button>
            </div>
            <div class="max-h-[65vh] overflow-y-auto px-6 py-5">
              <!-- Info Banner -->
              <div class="mb-5 rounded-xl bg-blue-50 p-4">
                <div class="flex items-start gap-3">
                  <i class="pi pi-info-circle mt-0.5 text-blue-600"></i>
                  <p class="text-xs text-blue-700">
                    {{ locale === 'ar'
                      ? 'اختر منافسة مكتملة لحفظ أقسامها وجدول كمياتها ومعايير تقييمها كقالب قابل لإعادة الاستخدام في منافسات مستقبلية.'
                      : 'Select a completed competition to save its sections, BOQ items, and evaluation criteria as a reusable template.' }}
                  </p>
                </div>
              </div>

              <!-- Source Competition -->
              <div class="mb-5">
                <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                  {{ locale === 'ar' ? 'اختر المنافسة المصدر' : 'Select Source Competition' }} *
                </label>
                <select
                  v-model="createTemplateForm.sourceCompetitionId"
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
                    v-model="createTemplateForm.nameAr"
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
                    v-model="createTemplateForm.nameEn"
                    type="text"
                    dir="ltr"
                    placeholder="e.g., IT Competition Template"
                    class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                  />
                </div>
              </div>

              <!-- Category & Competition Type -->
              <div class="mb-4 grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label class="mb-1.5 block text-sm font-semibold text-secondary-700">
                    {{ locale === 'ar' ? 'التصنيف' : 'Category' }}
                  </label>
                  <select
                    v-model="createTemplateForm.category"
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
                    v-model="createTemplateForm.competitionType"
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
                  v-model="createTemplateForm.tags"
                  type="text"
                  :placeholder="locale === 'ar' ? 'مثال: تقنية, برمجيات, أنظمة' : 'e.g., technology, software, systems'"
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>
            </div>
            <div class="flex items-center justify-end gap-3 border-t border-secondary-100 px-6 py-4">
              <button
                class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-semibold text-secondary-600 hover:bg-secondary-50"
                @click="showCreateCompTemplateDialog = false"
              >
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition-all hover:bg-primary-600 disabled:opacity-50"
                :disabled="isCreatingTemplate || !createTemplateForm.nameAr.trim() || !createTemplateForm.sourceCompetitionId"
                @click="handleCreateCompTemplate"
              >
                <i v-if="isCreatingTemplate" class="pi pi-spin pi-spinner text-xs"></i>
                <i v-else class="pi pi-check text-xs"></i>
                {{ locale === 'ar' ? 'حفظ كقالب' : 'Save as Template' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  DIALOG: Use Booklet Template                               -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showUseBookletDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showUseBookletDialog = false">
          <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
            <div class="flex items-center justify-between border-b border-secondary-100 p-5">
              <h2 class="text-lg font-bold text-secondary-800">
                {{ locale === 'ar' ? 'إنشاء كراسة من القالب' : 'Create Booklet from Template' }}
              </h2>
              <button class="rounded-lg p-1 text-secondary-400 hover:bg-secondary-100" @click="showUseBookletDialog = false">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="space-y-4 p-5">
              <div v-if="selectedBookletTemplate" class="rounded-lg bg-primary/5 p-3">
                <p class="text-sm font-semibold text-primary">
                  {{ locale === 'ar' ? selectedBookletTemplate.nameAr : selectedBookletTemplate.nameEn }}
                </p>
                <p class="mt-1 text-xs text-primary/70">
                  {{ selectedBookletTemplate.sectionCount }} {{ locale === 'ar' ? 'قسم' : 'sections' }}
                </p>
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم المشروع (عربي)' : 'Project Name (Arabic)' }} *
                </label>
                <input
                  v-model="useBookletForm.projectNameAr"
                  type="text"
                  :placeholder="locale === 'ar' ? 'أدخل اسم المشروع' : 'Enter project name'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم المشروع (إنجليزي)' : 'Project Name (English)' }}
                </label>
                <input
                  v-model="useBookletForm.projectNameEn"
                  type="text"
                  dir="ltr"
                  placeholder="Enter project name in English"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'وصف المشروع' : 'Project Description' }}
                </label>
                <textarea
                  v-model="useBookletForm.descriptionAr"
                  rows="3"
                  :placeholder="locale === 'ar' ? 'وصف مختصر للمشروع' : 'Brief project description'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                ></textarea>
              </div>

              <div v-if="createBookletError" class="rounded-lg bg-red-50 p-3 text-sm text-red-600">
                <i class="pi pi-exclamation-triangle me-1"></i>{{ createBookletError }}
              </div>
            </div>
            <div class="flex justify-end gap-3 border-t border-secondary-100 p-5">
              <button
                class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                @click="showUseBookletDialog = false"
              >
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 disabled:opacity-50"
                :disabled="isCreatingBooklet || !useBookletForm.projectNameAr.trim()"
                @click="handleCreateBooklet"
              >
                <i v-if="isCreatingBooklet" class="pi pi-spin pi-spinner me-2 text-xs"></i>
                {{ isCreatingBooklet
                  ? (locale === 'ar' ? 'جاري الإنشاء...' : 'Creating...')
                  : (locale === 'ar' ? 'إنشاء الكراسة والانتقال للمحرر' : 'Create & Open Editor') }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  DIALOG: Use Competition Template                           -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showUseCompDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showUseCompDialog = false">
          <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
            <div class="flex items-center justify-between border-b border-secondary-100 p-5">
              <h2 class="text-lg font-bold text-secondary-800">
                {{ locale === 'ar' ? 'إنشاء منافسة من القالب' : 'Create Competition from Template' }}
              </h2>
              <button class="rounded-lg p-1 text-secondary-400 hover:bg-secondary-100" @click="showUseCompDialog = false">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="space-y-4 p-5">
              <div v-if="selectedCompTemplate" class="rounded-lg bg-purple-50 p-3">
                <p class="text-sm font-semibold text-purple-700">
                  {{ locale === 'ar' ? selectedCompTemplate.nameAr : selectedCompTemplate.nameEn }}
                </p>
                <p class="mt-1 text-xs text-purple-500">
                  {{ selectedCompTemplate.sectionCount }} {{ locale === 'ar' ? 'قسم' : 'sections' }} |
                  {{ selectedCompTemplate.boqItemCount }} {{ locale === 'ar' ? 'بند' : 'BOQ items' }} |
                  {{ selectedCompTemplate.evaluationCriteriaCount }} {{ locale === 'ar' ? 'معيار' : 'criteria' }}
                </p>
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم المشروع (عربي)' : 'Project Name (Arabic)' }} *
                </label>
                <input
                  v-model="useCompForm.projectNameAr"
                  type="text"
                  :placeholder="locale === 'ar' ? 'أدخل اسم المشروع بالعربية' : 'Enter project name in Arabic'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم المشروع (إنجليزي)' : 'Project Name (English)' }}
                </label>
                <input
                  v-model="useCompForm.projectNameEn"
                  type="text"
                  dir="ltr"
                  placeholder="Enter project name in English"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'وصف المشروع' : 'Project Description' }}
                </label>
                <textarea
                  v-model="useCompForm.description"
                  rows="3"
                  :placeholder="locale === 'ar' ? 'وصف مختصر للمشروع...' : 'Brief project description...'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                ></textarea>
              </div>

              <div v-if="copyCompError" class="rounded-lg bg-red-50 p-3 text-sm text-red-600">
                <i class="pi pi-exclamation-triangle me-1"></i>{{ copyCompError }}
              </div>
            </div>
            <div class="flex justify-end gap-3 border-t border-secondary-100 p-5">
              <button
                class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                @click="showUseCompDialog = false"
              >
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition-all hover:bg-primary-600 disabled:opacity-50"
                :disabled="!useCompForm.projectNameAr.trim() || isCopyingComp"
                @click="handleCopyFromCompTemplate"
              >
                <i v-if="isCopyingComp" class="pi pi-spin pi-spinner text-xs"></i>
                <i v-else class="pi pi-copy text-xs"></i>
                {{ locale === 'ar' ? 'إنشاء المنافسة' : 'Create Competition' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Success Toasts                                             -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="slide-up">
        <div v-if="createBookletSuccess" class="fixed bottom-6 start-1/2 z-[60] -translate-x-1/2 rounded-xl bg-green-600 px-6 py-3 text-sm font-medium text-white shadow-lg">
          <i class="pi pi-check-circle me-2"></i>
          {{ locale === 'ar' ? 'تم إنشاء الكراسة بنجاح! جاري الانتقال للمحرر...' : 'Booklet created! Redirecting to editor...' }}
        </div>
      </Transition>
      <Transition name="slide-up">
        <div v-if="copyCompSuccess" class="fixed bottom-6 start-1/2 z-[60] -translate-x-1/2 rounded-xl bg-green-600 px-6 py-3 text-sm font-medium text-white shadow-lg">
          <i class="pi pi-check-circle me-2"></i>
          {{ locale === 'ar' ? 'تم إنشاء المنافسة بنجاح! جاري الانتقال...' : 'Competition created! Redirecting...' }}
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.3s ease;
}
.slide-up-enter-from,
.slide-up-leave-to {
  opacity: 0;
  transform: translate(-50%, 20px);
}
</style>
