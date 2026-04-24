<script setup lang="ts">
/**
 * BookletTemplatesView - EXPRO Official Booklet Templates Management
 *
 * Features:
 * - Upload DOCX booklet templates from EXPRO (هيئة كفاءة الإنفاق)
 * - Browse and search uploaded templates
 * - Create editable booklet from template with color-coded smart editor
 * - View template sections and block counts
 */
import { ref, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet, httpPost } from '@/services/http'

const { locale } = useI18n()
const router = useRouter()

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

const isLoading = ref(false)
const templates = ref<BookletTemplate[]>([])
const totalCount = ref(0)
const searchQuery = ref('')
const selectedCategory = ref('all')
const currentPage = ref(1)
const pageSize = ref(12)

// Upload dialog
const showUploadDialog = ref(false)
const isUploading = ref(false)
const uploadError = ref('')
const uploadFile = ref<File | null>(null)
const uploadForm = ref({
  nameAr: '',
  nameEn: '',
  descriptionAr: '',
  descriptionEn: '',
  category: 'it',
  sourceReference: ''
})

// Create booklet dialog
const showCreateDialog = ref(false)
const isCreating = ref(false)
const createError = ref('')
const createSuccess = ref(false)
const selectedTemplate = ref<BookletTemplate | null>(null)
const createForm = ref({
  projectNameAr: '',
  projectNameEn: '',
  descriptionAr: '',
  competitionType: 1,
  estimatedBudget: '',
  referenceNumber: '',
  department: '',
  fiscalYear: '',
  startDate: '',
  endDate: '',
  submissionDeadline: '',
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

function getCategoryLabel(key: string): string {
  const cat = categories.find(c => c.key === key)
  if (!cat) return key
  return locale.value === 'ar' ? cat.labelAr : cat.labelEn
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

    const data = await httpGet<{ items: BookletTemplate[]; totalCount: number }>(`/v1/booklet-templates?${params}`)
    templates.value = data.items
    totalCount.value = data.totalCount
  } catch {
    console.warn('Booklet Templates API not available yet')
    templates.value = []
    totalCount.value = 0
  } finally {
    isLoading.value = false
  }
}

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
    // Auto-fill name from file name
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
    await loadTemplates()
  } catch (err: unknown) {
    uploadError.value = err instanceof Error ? err.message : (locale.value === 'ar' ? 'حدث خطأ أثناء رفع القالب' : 'Error uploading template')
  } finally {
    isUploading.value = false
  }
}

function openCreateDialog(tmpl: BookletTemplate): void {
  selectedTemplate.value = tmpl
  createForm.value = {
    projectNameAr: '',
    projectNameEn: '',
    descriptionAr: '',
    competitionType: 1,
    estimatedBudget: '',
    referenceNumber: '',
    department: '',
    fiscalYear: '',
    startDate: '',
    endDate: '',
    submissionDeadline: '',
  }
  createError.value = ''
  createSuccess.value = false
  showCreateDialog.value = true
}

async function handleCreateBooklet(): Promise<void> {
  if (
    !selectedTemplate.value ||
    !createForm.value.projectNameAr.trim() ||
    !createForm.value.descriptionAr.trim() ||
    !createForm.value.referenceNumber.trim() ||
    !createForm.value.department.trim() ||
    !createForm.value.fiscalYear.trim() ||
    !createForm.value.estimatedBudget ||
    !createForm.value.startDate ||
    !createForm.value.endDate ||
    !createForm.value.submissionDeadline
  ) return

  isCreating.value = true
  createError.value = ''
  try {
    const result = await httpPost<{ rfpId: string }>(`/v1/booklet-templates/${selectedTemplate.value.id}/create-booklet`, {
      projectNameAr: createForm.value.projectNameAr,
      projectNameEn: createForm.value.projectNameEn || createForm.value.projectNameAr,
      descriptionAr: createForm.value.descriptionAr,
      competitionType: createForm.value.competitionType,
      estimatedBudget: Number(createForm.value.estimatedBudget),
      referenceNumber: createForm.value.referenceNumber,
      department: createForm.value.department,
      fiscalYear: createForm.value.fiscalYear,
      startDate: createForm.value.startDate,
      endDate: createForm.value.endDate,
      submissionDeadline: createForm.value.submissionDeadline,
    })
    showCreateDialog.value = false
    createSuccess.value = true
    setTimeout(() => {
      createSuccess.value = false
      router.push({ name: 'BookletEditor', params: { id: result.rfpId } })
    }, 1000)
  } catch (err: unknown) {
    createError.value = err instanceof Error ? err.message : (locale.value === 'ar' ? 'حدث خطأ أثناء إنشاء الكراسة' : 'Error creating booklet')
  } finally {
    isCreating.value = false
  }
}

watch([selectedCategory, searchQuery], () => {
  currentPage.value = 1
  loadTemplates()
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
          {{ locale === 'ar' ? 'قوالب كراسات الشروط والمواصفات' : 'Specification Booklet Templates' }}
        </h1>
        <p class="mt-1 text-sm text-secondary-500">
          {{ locale === 'ar' ? 'رفع وإدارة قوالب كراسات الشروط والمواصفات المعتمدة من هيئة كفاءة الإنفاق (EXPRO)' : 'Upload and manage official EXPRO specification booklet templates' }}
        </p>
      </div>
      <button
        class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 hover:shadow-md"
        @click="openUploadDialog"
      >
        <i class="pi pi-upload text-xs"></i>
        {{ locale === 'ar' ? 'رفع قالب جديد' : 'Upload New Template' }}
      </button>
    </div>

    <!-- Color Coding Legend -->
    <div class="rounded-2xl border border-secondary-100 bg-white p-4 shadow-sm">
      <h3 class="mb-3 text-sm font-semibold text-secondary-700">
        {{ locale === 'ar' ? 'دليل نظام الألوان (هيئة كفاءة الإنفاق)' : 'EXPRO Color Coding Guide' }}
      </h3>
      <div class="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <div class="flex items-center gap-2 rounded-lg bg-gray-50 p-2">
          <span class="inline-block h-4 w-4 rounded border border-gray-300 bg-gray-800"></span>
          <span class="text-xs text-secondary-600">
            {{ locale === 'ar' ? 'نصوص ثابتة (لا يجوز التغيير)' : 'Fixed text (cannot modify)' }}
          </span>
        </div>
        <div class="flex items-center gap-2 rounded-lg bg-green-50 p-2">
          <span class="inline-block h-4 w-4 rounded border border-green-300 bg-green-600"></span>
          <span class="text-xs text-secondary-600">
            {{ locale === 'ar' ? 'نصوص قابلة للتعديل' : 'Editable text' }}
          </span>
        </div>
        <div class="flex items-center gap-2 rounded-lg bg-red-50 p-2">
          <span class="inline-block h-4 w-4 rounded border border-red-300 bg-red-600"></span>
          <span class="text-xs text-secondary-600">
            {{ locale === 'ar' ? 'أمثلة (يجب استبدالها)' : 'Examples (replace)' }}
          </span>
        </div>
        <div class="flex items-center gap-2 rounded-lg bg-blue-50 p-2">
          <span class="inline-block h-4 w-4 rounded border border-blue-300 bg-blue-600"></span>
          <span class="text-xs text-secondary-600">
            {{ locale === 'ar' ? 'إرشادات (تُحذف من النسخة النهائية)' : 'Guidance (remove from final)' }}
          </span>
        </div>
      </div>
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
            class="w-full rounded-xl border border-secondary-200 bg-secondary-50 py-2.5 ps-10 pe-4 text-sm outline-none transition-colors focus:border-primary focus:bg-white focus:ring-2 focus:ring-primary/20"
          />
        </div>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="cat in categories"
            :key="cat.key"
            class="rounded-lg px-3 py-1.5 text-xs font-medium transition-all"
            :class="selectedCategory === cat.key
              ? 'bg-primary text-white shadow-sm'
              : 'bg-secondary-100 text-secondary-600 hover:bg-secondary-200'"
            @click="selectedCategory = cat.key"
          >
            {{ locale === 'ar' ? cat.labelAr : cat.labelEn }}
          </button>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-16">
      <div class="text-center">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        <p class="mt-2 text-sm text-secondary-500">{{ locale === 'ar' ? 'جاري التحميل...' : 'Loading...' }}</p>
      </div>
    </div>

    <!-- Templates Grid -->
    <div v-else-if="templates.length > 0" class="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="tmpl in templates"
        :key="tmpl.id"
        class="group relative overflow-hidden rounded-2xl border border-secondary-100 bg-white shadow-sm transition-all hover:shadow-lg"
      >
        <!-- Template Header -->
        <div class="border-b border-secondary-100 bg-gradient-to-l from-primary/5 to-transparent p-5">
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <h3 class="text-base font-bold text-secondary-800 line-clamp-2">
                {{ locale === 'ar' ? tmpl.nameAr : tmpl.nameEn }}
              </h3>
              <p v-if="tmpl.descriptionAr" class="mt-1 text-xs text-secondary-500 line-clamp-2">
                {{ locale === 'ar' ? tmpl.descriptionAr : (tmpl.descriptionEn || tmpl.descriptionAr) }}
              </p>
            </div>
            <span class="ms-2 shrink-0 rounded-lg bg-primary/10 px-2 py-1 text-xs font-medium text-primary">
              {{ getCategoryLabel(tmpl.category) }}
            </span>
          </div>
        </div>

        <!-- Template Stats -->
        <div class="grid grid-cols-2 gap-3 p-5">
          <div class="rounded-lg bg-secondary-50 p-3 text-center">
            <div class="text-lg font-bold text-primary">{{ tmpl.sectionCount }}</div>
            <div class="text-xs text-secondary-500">{{ locale === 'ar' ? 'قسم' : 'Sections' }}</div>
          </div>
          <div class="rounded-lg bg-secondary-50 p-3 text-center">
            <div class="text-lg font-bold text-secondary-700">{{ tmpl.usageCount }}</div>
            <div class="text-xs text-secondary-500">{{ locale === 'ar' ? 'مرة استخدام' : 'Times Used' }}</div>
          </div>
        </div>

        <!-- Template Meta -->
        <div class="border-t border-secondary-100 px-5 py-3">
          <div class="flex items-center justify-between text-xs text-secondary-400">
            <span v-if="tmpl.originalFileName" class="flex items-center gap-1">
              <i class="pi pi-file-word"></i>
              {{ tmpl.originalFileName }}
            </span>
            <span>{{ formatDate(tmpl.createdAt) }}</span>
          </div>
          <div v-if="tmpl.sourceReference" class="mt-1 text-xs text-secondary-400">
            {{ tmpl.sourceReference }}
          </div>
        </div>

        <!-- Actions -->
        <div class="border-t border-secondary-100 p-4">
          <button
            class="w-full rounded-xl bg-primary px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 hover:shadow-md"
            @click="openCreateDialog(tmpl)"
          >
            <i class="pi pi-file-edit me-2 text-xs"></i>
            {{ locale === 'ar' ? 'إنشاء كراسة من هذا القالب' : 'Create Booklet from Template' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-secondary-200 py-16">
      <i class="pi pi-file-word text-5xl text-secondary-300"></i>
      <h3 class="mt-4 text-lg font-semibold text-secondary-600">
        {{ locale === 'ar' ? 'لا توجد قوالب كراسات' : 'No Booklet Templates' }}
      </h3>
      <p class="mt-2 text-sm text-secondary-400">
        {{ locale === 'ar' ? 'قم برفع نموذج كراسة شروط ومواصفات معتمد من هيئة كفاءة الإنفاق (EXPRO)' : 'Upload an official EXPRO specification booklet template' }}
      </p>
      <button
        class="mt-4 inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white"
        @click="openUploadDialog"
      >
        <i class="pi pi-upload text-xs"></i>
        {{ locale === 'ar' ? 'رفع قالب' : 'Upload Template' }}
      </button>
    </div>

    <!-- Success Toast -->
    <Teleport to="body">
      <Transition name="slide-up">
        <div v-if="createSuccess" class="fixed bottom-6 start-1/2 z-50 -translate-x-1/2 rounded-xl bg-green-600 px-6 py-3 text-sm font-medium text-white shadow-lg">
          <i class="pi pi-check-circle me-2"></i>
          {{ locale === 'ar' ? 'تم إنشاء الكراسة بنجاح! جاري الانتقال للمحرر...' : 'Booklet created! Redirecting to editor...' }}
        </div>
      </Transition>
    </Teleport>

    <!-- Upload Dialog -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showUploadDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showUploadDialog = false">
          <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
            <div class="flex items-center justify-between border-b border-secondary-100 p-5">
              <h2 class="text-lg font-bold text-secondary-800">
                {{ locale === 'ar' ? 'رفع قالب كراسة جديد' : 'Upload New Booklet Template' }}
              </h2>
              <button class="rounded-lg p-1 text-secondary-400 hover:bg-secondary-100" @click="showUploadDialog = false">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="space-y-4 p-5">
              <!-- File Upload -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'ملف القالب (DOCX)' : 'Template File (DOCX)' }} *
                </label>
                <div class="relative">
                  <input
                    type="file"
                    accept=".docx"
                    class="w-full rounded-xl border border-secondary-200 bg-secondary-50 p-3 text-sm file:me-3 file:rounded-lg file:border-0 file:bg-primary/10 file:px-3 file:py-1 file:text-xs file:font-medium file:text-primary"
                    @change="handleFileSelect"
                  />
                </div>
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
                  :placeholder="locale === 'ar' ? 'مثال: قرار وزير المالية رقم (1440) وتاريخ 12/4/1441هـ' : 'e.g., MOF Decision No. 1440'"
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

    <!-- Create Booklet Dialog -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showCreateDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showCreateDialog = false">
          <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
            <div class="flex items-center justify-between border-b border-secondary-100 p-5">
              <h2 class="text-lg font-bold text-secondary-800">
                {{ locale === 'ar' ? 'إنشاء كراسة من القالب' : 'Create Booklet from Template' }}
              </h2>
              <button class="rounded-lg p-1 text-secondary-400 hover:bg-secondary-100" @click="showCreateDialog = false">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="space-y-4 p-5">
              <div v-if="selectedTemplate" class="rounded-lg bg-primary/5 p-3">
                <p class="text-sm font-medium text-primary">
                  {{ locale === 'ar' ? selectedTemplate.nameAr : selectedTemplate.nameEn }}
                </p>
                <p class="mt-1 text-xs text-secondary-500">
                  {{ selectedTemplate.sectionCount }} {{ locale === 'ar' ? 'قسم' : 'sections' }}
                </p>
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'اسم المشروع (عربي)' : 'Project Name (Arabic)' }} *
                </label>
                <input
                  v-model="createForm.projectNameAr"
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
                  v-model="createForm.projectNameEn"
                  type="text"
                  dir="ltr"
                  placeholder="Enter project name in English"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-secondary-700">
                  {{ locale === 'ar' ? 'وصف المشروع' : 'Project Description' }} *
                </label>
                <textarea
                  v-model="createForm.descriptionAr"
                  rows="3"
                  :placeholder="locale === 'ar' ? 'وصف مختصر للمشروع' : 'Brief project description'"
                  class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                ></textarea>
              </div>

              <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'الرقم المرجعي' : 'Reference Number' }} *
                  </label>
                  <input v-model="createForm.referenceNumber" type="text" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'الإدارة' : 'Department' }} *
                  </label>
                  <input v-model="createForm.department" type="text" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'السنة المالية' : 'Fiscal Year' }} *
                  </label>
                  <input v-model="createForm.fiscalYear" type="text" inputmode="numeric" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'القيمة التقديرية' : 'Estimated Budget' }} *
                  </label>
                  <input v-model="createForm.estimatedBudget" type="number" min="0" step="0.01" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'تاريخ البداية' : 'Start Date' }} *
                  </label>
                  <input v-model="createForm.startDate" type="date" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'تاريخ الانتهاء' : 'End Date' }} *
                  </label>
                  <input v-model="createForm.endDate" type="date" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'آخر موعد لتقديم العروض' : 'Submission Deadline' }} *
                  </label>
                  <input v-model="createForm.submissionDeadline" type="date" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary-700">
                    {{ locale === 'ar' ? 'نوع المنافسة' : 'Competition Type' }} *
                  </label>
                  <select v-model="createForm.competitionType" class="w-full rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-2.5 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20">
                    <option :value="1">{{ locale === 'ar' ? 'منافسة عامة' : 'Public Tender' }}</option>
                    <option :value="2">{{ locale === 'ar' ? 'منافسة محدودة' : 'Limited Tender' }}</option>
                    <option :value="3">{{ locale === 'ar' ? 'ممارسة' : 'Practice' }}</option>
                    <option :value="4">{{ locale === 'ar' ? 'شراء مباشر' : 'Direct Purchase' }}</option>
                  </select>
                </div>
              </div>

              <div v-if="createError" class="rounded-lg bg-red-50 p-3 text-sm text-red-600">
                <i class="pi pi-exclamation-triangle me-1"></i>{{ createError }}
              </div>
            </div>
            <div class="flex justify-end gap-3 border-t border-secondary-100 p-5">
              <button
                class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary-600 hover:bg-secondary-50"
                @click="showCreateDialog = false"
              >
                {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
              </button>
              <button
                class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-primary-600 disabled:opacity-50"
                :disabled="isCreating || !createForm.projectNameAr.trim() || !createForm.descriptionAr.trim() || !createForm.referenceNumber.trim() || !createForm.department.trim() || !createForm.fiscalYear.trim() || !createForm.estimatedBudget || !createForm.startDate || !createForm.endDate || !createForm.submissionDeadline"
                @click="handleCreateBooklet"
              >
                <i v-if="isCreating" class="pi pi-spin pi-spinner me-2 text-xs"></i>
                {{ isCreating
                  ? (locale === 'ar' ? 'جاري الإنشاء...' : 'Creating...')
                  : (locale === 'ar' ? 'إنشاء الكراسة والانتقال للمحرر' : 'Create & Open Editor') }}
              </button>
            </div>
          </div>
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
