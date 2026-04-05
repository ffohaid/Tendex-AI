<script setup lang="ts">
/**
 * RfpMethodSelectionView - Competition Creation Method Selection
 *
 * Provides 3 creation methods:
 * 1. From Template - Start with a pre-built template
 * 2. Upload & Extract - AI-powered document extraction (full flow)
 * 3. From Scratch - Manual creation
 *
 * The "Upload & Extract" flow is fully integrated:
 * - Step 1: File upload with drag-and-drop and progress tracking
 * - Step 2: AI extraction with real-time status updates
 * - Step 3: Review extracted content (sections, BOQ, metadata)
 * - Step 4: Create competition and navigate to editor
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useRfpStore } from '@/stores/rfp'
import {
  extractBookletFromDocument,
  validateExtractionFile,
  formatFileSize,
  getSectionTypeLabel,
  getSectionTypeColor,
  getCompetitionTypeLabel,
  type BookletExtractionResult,
} from '@/services/bookletExtractionService'
import {
  createRfpFromExtraction,
  saveAllSections,
  saveAllBoqItems,
  addRfpSection,
} from '@/services/rfpService'

const { t } = useI18n()
const router = useRouter()
const rfpStore = useRfpStore()

// ═══════════════════════════════════════════════════════════════
//  State
// ═══════════════════════════════════════════════════════════════

/** Which creation method card is selected */
const selectedMethod = ref<string | null>(null)

/** Upload & Extract flow state */
type ExtractPhase = 'idle' | 'uploading' | 'extracting' | 'review' | 'creating' | 'error'
const extractPhase = ref<ExtractPhase>('idle')
const uploadedFile = ref<File | null>(null)
const uploadProgress = ref(0)
const extractionResult = ref<BookletExtractionResult | null>(null)
const errorMessage = ref('')
const dragOver = ref(false)

/** Extraction status messages for the progress display */
const extractionSteps = ref([
  { key: 'upload', label: 'رفع الملف إلى الخادم', status: 'pending' as 'pending' | 'active' | 'done' | 'error' },
  { key: 'parse', label: 'استخراج النص من المستند', status: 'pending' as 'pending' | 'active' | 'done' | 'error' },
  { key: 'analyze', label: 'تحليل المحتوى بالذكاء الاصطناعي', status: 'pending' as 'pending' | 'active' | 'done' | 'error' },
  { key: 'structure', label: 'بناء هيكل كراسة الشروط', status: 'pending' as 'pending' | 'active' | 'done' | 'error' },
])

/** Review tab state */
const activeReviewTab = ref<'overview' | 'sections' | 'boq'>('overview')

/** Creating state */
const isCreating = ref(false)

// ═══════════════════════════════════════════════════════════════
//  Computed
// ═══════════════════════════════════════════════════════════════

const competitionTypeMap: Record<string, number> = {
  PublicTender: 0,
  LimitedTender: 1,
  DirectPurchase: 2,
  FrameworkAgreement: 3,
  ReverseAuction: 4,
}

const confidenceColor = computed(() => {
  if (!extractionResult.value) return 'text-tertiary'
  const score = extractionResult.value.confidenceScore
  if (score >= 80) return 'text-success'
  if (score >= 60) return 'text-warning'
  return 'text-danger'
})

const confidenceLabel = computed(() => {
  if (!extractionResult.value) return ''
  const score = extractionResult.value.confidenceScore
  if (score >= 80) return 'ممتاز'
  if (score >= 60) return 'جيد'
  if (score >= 40) return 'متوسط'
  return 'منخفض'
})

// ═══════════════════════════════════════════════════════════════
//  Creation Method Cards
// ═══════════════════════════════════════════════════════════════

interface CreationMethod {
  key: string
  icon: string
  title: string
  description: string
  features: string[]
  badge?: string
  badgeColor?: string
  gradient: string
  iconBg: string
  action: () => void
}

const methods: CreationMethod[] = [
  {
    key: 'scratch',
    icon: 'pi-file-edit',
    title: 'من البداية',
    description: 'إنشاء كراسة شروط فارغة وملء جميع التفاصيل يدوياً',
    features: [
      'تحكم كامل في المحتوى',
      'اقتراحات الذكاء الاصطناعي متاحة',
      'الأفضل للمشاريع الفريدة',
    ],
    gradient: 'from-slate-50 to-gray-50 border-secondary-200 hover:border-secondary-400',
    iconBg: 'bg-slate-100 text-slate-600',
    action: () => router.push({ name: 'rfp-create' }),
  },
  {
    key: 'upload',
    icon: 'pi-cloud-upload',
    title: 'رفع واستخراج',
    description: 'قم برفع مستند وسيقوم الذكاء الاصطناعي باستخراج محتوى كراسة الشروط',
    features: [
      'PDF و Word مدعومان',
      'استخراج بقوة الذكاء الاصطناعي',
      'استيراد سريع لكراسات الشروط الموجودة',
    ],
    badge: 'AI',
    badgeColor: 'bg-interactive text-white',
    gradient: 'from-blue-50 to-cyan-50 border-blue-200 hover:border-blue-400',
    iconBg: 'bg-blue-100 text-blue-600',
    action: () => { selectedMethod.value = 'upload' },
  },
  {
    key: 'template',
    icon: 'pi-th-large',
    title: 'من قالب',
    description: 'ابدأ بقالب جاهز وخصصه حسب احتياجاتك',
    features: [
      'محرر مرن مع معاينة مباشرة',
      'استخراج البيانات بالذكاء الاصطناعي',
      'يتضمن هوية منظمتك',
      'أسرع طريقة لإنشاء كراسة شروط',
    ],
    badge: 'جديد',
    badgeColor: 'bg-accent text-white',
    gradient: 'from-purple-50 to-indigo-50 border-purple-200 hover:border-purple-400',
    iconBg: 'bg-purple-100 text-purple-600',
    action: () => router.push({ name: 'TemplateLibrary' }),
  },
]

// ═══════════════════════════════════════════════════════════════
//  Upload & Extract Handlers
// ═══════════════════════════════════════════════════════════════

function handleMethodClick(method: CreationMethod): void {
  selectedMethod.value = method.key
  method.action()
}

function handleDragOver(e: DragEvent): void {
  e.preventDefault()
  dragOver.value = true
}

function handleDragLeave(): void {
  dragOver.value = false
}

function handleDrop(e: DragEvent): void {
  e.preventDefault()
  dragOver.value = false
  const files = e.dataTransfer?.files
  if (files && files.length > 0) {
    handleFileSelect(files[0])
  }
}

function handleFileInput(e: Event): void {
  const input = e.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    handleFileSelect(input.files[0])
  }
}

async function handleFileSelect(file: File): Promise<void> {
  // Validate file
  const validation = validateExtractionFile(file)
  if (!validation.valid) {
    errorMessage.value = validation.error || 'خطأ في الملف'
    extractPhase.value = 'error'
    return
  }

  uploadedFile.value = file
  errorMessage.value = ''
  extractPhase.value = 'uploading'
  uploadProgress.value = 0

  // Reset extraction steps
  extractionSteps.value.forEach(s => { s.status = 'pending' })
  extractionSteps.value[0].status = 'active'

  try {
    // Start extraction via API
    const result = await extractBookletFromDocument(file, (progressEvent) => {
      if (progressEvent.total) {
        uploadProgress.value = Math.round((progressEvent.loaded / progressEvent.total) * 100)
      }
      // When upload completes, move to extracting phase
      if (uploadProgress.value >= 100) {
        extractionSteps.value[0].status = 'done'
        extractionSteps.value[1].status = 'active'
        extractPhase.value = 'extracting'

        // Simulate progress through extraction steps
        setTimeout(() => {
          extractionSteps.value[1].status = 'done'
          extractionSteps.value[2].status = 'active'
        }, 1000)
        setTimeout(() => {
          extractionSteps.value[2].status = 'done'
          extractionSteps.value[3].status = 'active'
        }, 2000)
      }
    })

    // Mark all steps as done
    extractionSteps.value.forEach(s => { s.status = 'done' })

    if (result.isSuccess && result.extraction) {
      extractionResult.value = result.extraction
      extractPhase.value = 'review'
    } else {
      errorMessage.value = result.errorMessage || 'فشل في استخراج المحتوى من المستند'
      extractPhase.value = 'error'
      extractionSteps.value[extractionSteps.value.findIndex(s => s.status === 'active')].status = 'error'
    }
  } catch (error: unknown) {
    const axiosError = error as { response?: { data?: { error?: string }; status?: number }; message?: string }
    errorMessage.value =
      axiosError?.response?.data?.error ||
      axiosError?.message ||
      'حدث خطأ غير متوقع أثناء الاستخراج'
    extractPhase.value = 'error'

    // Mark the active step as error
    const activeStep = extractionSteps.value.find(s => s.status === 'active')
    if (activeStep) activeStep.status = 'error'
  }
}

function resetUpload(): void {
  extractPhase.value = 'idle'
  uploadedFile.value = null
  uploadProgress.value = 0
  extractionResult.value = null
  errorMessage.value = ''
  extractionSteps.value.forEach(s => { s.status = 'pending' })
}

async function createFromExtraction(): Promise<void> {
  if (!extractionResult.value) return

  isCreating.value = true
  extractPhase.value = 'creating'

  try {
    const ext = extractionResult.value

    // 1. Create the competition
    const createResult = await createRfpFromExtraction({
      projectNameAr: ext.projectNameAr,
      projectNameEn: ext.projectNameEn || ext.projectNameAr,
      description: ext.projectDescription || null,
      competitionType: competitionTypeMap[ext.detectedCompetitionType || ''] ?? 0,
      estimatedBudget: ext.estimatedBudget || null,
      projectDurationDays: ext.projectDurationDays || null,
    })

    if (!createResult.success || !createResult.data?.id) {
      errorMessage.value = createResult.message || 'فشل في إنشاء المنافسة'
      extractPhase.value = 'error'
      isCreating.value = false
      return
    }

    const competitionId = createResult.data.id

    // 2. Save extracted sections
    if (ext.sections.length > 0) {
      const sectionsMapped = ext.sections.map((s, idx) => ({
        id: `temp-${idx}`,
        title: s.titleAr,
        content: '',
        contentHtml: s.contentHtml,
        order: s.sortOrder,
        isRequired: s.isMandatory,
        colorCode: 'green' as const,
        assignedTo: null,
        isCompleted: true,
      }))

      // clearExisting = false because the competition was just created (no existing sections)
      const sectionsResult = await saveAllSections(competitionId, sectionsMapped, false)
      if (!sectionsResult.success) {
        console.warn('Failed to save sections via batch, trying individually...')
        // Fallback: add sections one by one
        for (const section of sectionsMapped) {
          await addRfpSection(competitionId, section)
        }
      }
    }

    // 3. Save extracted BOQ items
    if (ext.boqItems.length > 0) {
      const boqMapped = ext.boqItems.map((b, idx) => ({
        id: `temp-${idx}`,
        category: b.category || '',
        description: b.descriptionAr,
        unit: 'unit' as const,
        quantity: b.quantity,
        estimatedPrice: b.estimatedUnitPrice || 0,
        totalPrice: b.quantity * (b.estimatedUnitPrice || 0),
        notes: '',
        order: b.sortOrder,
      }))

      // clearExisting = false because the competition was just created (no existing BOQ items)
      await saveAllBoqItems(competitionId, boqMapped, false)
    }

    // 4. Pre-fill the store and navigate to editor
    rfpStore.prefillFromExtraction(ext)
    rfpStore.formData.id = competitionId

    router.push({ name: 'rfp-edit', params: { id: competitionId } })
  } catch (error: unknown) {
    errorMessage.value = (error as Error).message || 'حدث خطأ أثناء إنشاء الكراسة'
    extractPhase.value = 'error'
  } finally {
    isCreating.value = false
  }
}
</script>

<template>
  <div class="min-h-[80vh] flex flex-col items-center px-4 py-8">
    <!-- Header -->
    <div class="mb-8 flex w-full max-w-5xl items-center justify-between">
      <router-link
        :to="{ name: 'rfp-list' }"
        class="flex items-center gap-2 text-sm text-tertiary transition-colors hover:text-secondary"
      >
        <i class="pi pi-arrow-right text-xs ltr:rotate-180"></i>
        {{ t('rfp.methodSelection.backToList') }}
      </router-link>
      <div class="flex items-center gap-2">
        <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-primary-dark text-white shadow-sm">
          <i class="pi pi-bolt text-sm"></i>
        </div>
        <span class="text-sm font-bold text-secondary">Tendex AI</span>
      </div>
    </div>

    <!-- Title -->
    <div class="mb-10 text-center">
      <h1 class="text-2xl font-bold text-secondary sm:text-3xl">
        {{ t('rfp.methodSelection.title') }}
      </h1>
      <p class="mt-2 text-sm text-tertiary">
        {{ t('rfp.methodSelection.subtitle') }}
      </p>
    </div>

    <!-- Method Cards (hidden during extraction flow) -->
    <Transition
      enter-active-class="transition-all duration-300"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-all duration-200"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="extractPhase === 'idle' || extractPhase === 'error'"
        class="grid w-full max-w-5xl grid-cols-1 gap-5 md:grid-cols-3"
      >
        <div
          v-for="method in methods"
          :key="method.key"
          class="group relative cursor-pointer overflow-hidden rounded-2xl border-2 bg-gradient-to-br p-6 transition-all duration-300 hover:-translate-y-1 hover:shadow-lg"
          :class="[
            method.gradient,
            selectedMethod === method.key ? 'ring-2 ring-primary ring-offset-2' : '',
          ]"
          @click="handleMethodClick(method)"
        >
          <!-- Badge -->
          <span
            v-if="method.badge"
            class="absolute end-3 top-3 rounded-full px-2.5 py-1 text-[10px] font-bold"
            :class="method.badgeColor"
          >
            {{ method.badge }}
          </span>

          <!-- Icon -->
          <div class="mb-5 flex justify-center">
            <div
              class="flex h-16 w-16 items-center justify-center rounded-2xl transition-transform duration-300 group-hover:scale-110"
              :class="method.iconBg"
            >
              <i class="pi text-2xl" :class="method.icon"></i>
            </div>
          </div>

          <!-- Title & Description -->
          <h3 class="mb-2 text-center text-base font-bold text-secondary">
            {{ method.title }}
          </h3>
          <p class="mb-5 text-center text-xs leading-relaxed text-tertiary">
            {{ method.description }}
          </p>

          <!-- Features -->
          <ul class="space-y-2">
            <li
              v-for="(feature, idx) in method.features"
              :key="idx"
              class="flex items-center gap-2 text-xs text-secondary-600"
            >
              <i class="pi pi-check-circle text-xs text-success"></i>
              <span>{{ feature }}</span>
            </li>
          </ul>

          <!-- CTA for template method -->
          <button
            v-if="method.key === 'template'"
            class="mt-5 flex w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-purple-500 to-indigo-500 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:shadow-md"
            @click.stop="handleMethodClick(method)"
          >
            ابدأ بمحرر القوالب
            <i class="pi pi-arrow-left text-xs ltr:rotate-180"></i>
          </button>
        </div>
      </div>
    </Transition>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Upload & Extract: File Drop Zone                          -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="selectedMethod === 'upload' && (extractPhase === 'idle' || extractPhase === 'error')"
        class="mt-8 w-full max-w-5xl"
      >
        <!-- Error Message -->
        <div
          v-if="extractPhase === 'error' && errorMessage"
          class="mb-4 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4"
        >
          <i class="pi pi-exclamation-triangle mt-0.5 text-red-500"></i>
          <div class="flex-1">
            <p class="text-sm font-semibold text-red-700">فشل في الاستخراج</p>
            <p class="mt-1 text-xs text-red-600">{{ errorMessage }}</p>
          </div>
          <button
            class="text-xs text-red-500 hover:text-red-700"
            @click="resetUpload"
          >
            <i class="pi pi-times"></i>
          </button>
        </div>

        <!-- Drop Zone -->
        <div
          class="relative rounded-2xl border-2 border-dashed p-12 text-center transition-all duration-300"
          :class="dragOver
            ? 'border-primary bg-primary/5 shadow-lg shadow-primary/10'
            : 'border-secondary-300 bg-white hover:border-primary/50 hover:bg-primary/[0.02]'"
          @dragover="handleDragOver"
          @dragleave="handleDragLeave"
          @drop="handleDrop"
        >
          <div class="flex flex-col items-center">
            <div class="mb-4 flex h-20 w-20 items-center justify-center rounded-2xl bg-blue-50">
              <i class="pi pi-cloud-upload text-4xl text-blue-500"></i>
            </div>
            <p class="text-base font-bold text-secondary">
              اسحب الملف وأفلته هنا
            </p>
            <p class="mt-2 text-sm text-tertiary">
              أو اختر ملفاً من جهازك
            </p>
            <p class="mt-1 text-xs text-tertiary">
              PDF, Word (حتى 50 ميجابايت)
            </p>
            <label class="mt-5 inline-flex cursor-pointer items-center gap-2 rounded-xl bg-gradient-to-r from-blue-500 to-cyan-500 px-6 py-3 text-sm font-semibold text-white shadow-sm transition-all hover:shadow-md hover:from-blue-600 hover:to-cyan-600">
              <i class="pi pi-upload text-xs"></i>
              اختر ملفاً
              <input type="file" class="hidden" accept=".pdf,.doc,.docx" @change="handleFileInput" />
            </label>
          </div>

          <!-- Supported formats info -->
          <div class="mt-8 flex items-center justify-center gap-6">
            <div class="flex items-center gap-2 text-xs text-tertiary">
              <i class="pi pi-file-pdf text-red-400"></i>
              <span>PDF</span>
            </div>
            <div class="flex items-center gap-2 text-xs text-tertiary">
              <i class="pi pi-file-word text-blue-400"></i>
              <span>Word (.docx)</span>
            </div>
            <div class="flex items-center gap-2 text-xs text-tertiary">
              <i class="pi pi-file text-gray-400"></i>
              <span>Word (.doc)</span>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Upload & Extract: Progress / Extraction Phase              -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Transition
      enter-active-class="transition-all duration-500 ease-out"
      enter-from-class="opacity-0 scale-95"
      enter-to-class="opacity-100 scale-100"
    >
      <div
        v-if="extractPhase === 'uploading' || extractPhase === 'extracting'"
        class="mt-8 w-full max-w-3xl"
      >
        <div class="rounded-2xl border border-blue-100 bg-white p-8 shadow-lg shadow-blue-50">
          <!-- File info -->
          <div class="mb-6 flex items-center gap-4 rounded-xl bg-blue-50/50 p-4">
            <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-100">
              <i class="pi text-xl text-blue-600"
                :class="uploadedFile?.type === 'application/pdf' ? 'pi-file-pdf' : 'pi-file-word'"
              ></i>
            </div>
            <div class="flex-1 text-start">
              <p class="text-sm font-semibold text-secondary">{{ uploadedFile?.name }}</p>
              <p class="text-xs text-tertiary">{{ formatFileSize(uploadedFile?.size || 0) }}</p>
            </div>
          </div>

          <!-- Upload progress bar -->
          <div v-if="extractPhase === 'uploading'" class="mb-6">
            <div class="mb-2 flex items-center justify-between">
              <span class="text-xs font-medium text-secondary">جاري رفع الملف...</span>
              <span class="text-xs font-bold text-primary">{{ uploadProgress }}%</span>
            </div>
            <div class="h-2 overflow-hidden rounded-full bg-blue-100">
              <div
                class="h-full rounded-full bg-gradient-to-l from-blue-500 to-cyan-500 transition-all duration-300"
                :style="{ width: `${uploadProgress}%` }"
              ></div>
            </div>
          </div>

          <!-- Extraction steps -->
          <div class="space-y-4">
            <div
              v-for="step in extractionSteps"
              :key="step.key"
              class="flex items-center gap-3"
            >
              <!-- Status icon -->
              <div class="flex h-8 w-8 items-center justify-center rounded-full"
                :class="{
                  'bg-gray-100': step.status === 'pending',
                  'bg-blue-100': step.status === 'active',
                  'bg-green-100': step.status === 'done',
                  'bg-red-100': step.status === 'error',
                }"
              >
                <i v-if="step.status === 'pending'" class="pi pi-circle text-xs text-gray-400"></i>
                <i v-else-if="step.status === 'active'" class="pi pi-spin pi-spinner text-xs text-blue-600"></i>
                <i v-else-if="step.status === 'done'" class="pi pi-check text-xs text-green-600"></i>
                <i v-else class="pi pi-times text-xs text-red-600"></i>
              </div>

              <!-- Label -->
              <span class="text-sm"
                :class="{
                  'text-tertiary': step.status === 'pending',
                  'font-semibold text-blue-700': step.status === 'active',
                  'text-green-700': step.status === 'done',
                  'text-red-700': step.status === 'error',
                }"
              >
                {{ step.label }}
              </span>
            </div>
          </div>

          <!-- AI processing animation -->
          <div v-if="extractPhase === 'extracting'" class="mt-6 text-center">
            <div class="inline-flex items-center gap-2 rounded-xl bg-gradient-to-l from-blue-50 to-purple-50 px-4 py-2">
              <i class="pi pi-spin pi-cog text-sm text-primary"></i>
              <span class="text-xs font-medium text-primary">الذكاء الاصطناعي يحلل المستند...</span>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Upload & Extract: Review Phase                             -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Transition
      enter-active-class="transition-all duration-500 ease-out"
      enter-from-class="opacity-0 translate-y-8"
      enter-to-class="opacity-100 translate-y-0"
    >
      <div
        v-if="extractPhase === 'review' && extractionResult"
        class="mt-4 w-full max-w-5xl"
      >
        <!-- Success header -->
        <div class="mb-6 flex items-center justify-between rounded-2xl border border-green-100 bg-gradient-to-l from-green-50 to-emerald-50 p-5">
          <div class="flex items-center gap-4">
            <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-green-100">
              <i class="pi pi-check-circle text-2xl text-green-600"></i>
            </div>
            <div>
              <h3 class="text-base font-bold text-green-800">تم الاستخراج بنجاح</h3>
              <p class="text-xs text-green-600">
                {{ extractionResult.sections.length }} قسم
                <span v-if="extractionResult.boqItems.length > 0">
                  و {{ extractionResult.boqItems.length }} بند في جدول الكميات
                </span>
                - بواسطة {{ extractionResult.providerName }} ({{ extractionResult.modelName }})
                - {{ (extractionResult.latencyMs / 1000).toFixed(1) }} ثانية
              </p>
            </div>
          </div>
          <div class="text-center">
            <div class="text-2xl font-bold" :class="confidenceColor">
              {{ Math.round(extractionResult.confidenceScore) }}%
            </div>
            <div class="text-[10px] font-medium" :class="confidenceColor">{{ confidenceLabel }}</div>
          </div>
        </div>

        <!-- Warnings -->
        <div
          v-if="extractionResult.warnings.length > 0"
          class="mb-4 rounded-xl border border-amber-200 bg-amber-50 p-4"
        >
          <div class="flex items-center gap-2 mb-2">
            <i class="pi pi-exclamation-triangle text-amber-600"></i>
            <span class="text-sm font-semibold text-amber-700">ملاحظات على الاستخراج</span>
          </div>
          <ul class="space-y-1">
            <li
              v-for="(warning, idx) in extractionResult.warnings"
              :key="idx"
              class="text-xs text-amber-600 flex items-start gap-2"
            >
              <span class="mt-1 block h-1 w-1 rounded-full bg-amber-400 flex-shrink-0"></span>
              {{ warning }}
            </li>
          </ul>
        </div>

        <!-- Review Tabs -->
        <div class="mb-4 flex gap-1 rounded-xl bg-gray-100 p-1">
          <button
            class="flex-1 rounded-lg px-4 py-2.5 text-sm font-medium transition-all"
            :class="activeReviewTab === 'overview'
              ? 'bg-white text-primary shadow-sm'
              : 'text-tertiary hover:text-secondary'"
            @click="activeReviewTab = 'overview'"
          >
            <i class="pi pi-info-circle me-1.5"></i>
            نظرة عامة
          </button>
          <button
            class="flex-1 rounded-lg px-4 py-2.5 text-sm font-medium transition-all"
            :class="activeReviewTab === 'sections'
              ? 'bg-white text-primary shadow-sm'
              : 'text-tertiary hover:text-secondary'"
            @click="activeReviewTab = 'sections'"
          >
            <i class="pi pi-list me-1.5"></i>
            الأقسام ({{ extractionResult.sections.length }})
          </button>
          <button
            v-if="extractionResult.boqItems.length > 0"
            class="flex-1 rounded-lg px-4 py-2.5 text-sm font-medium transition-all"
            :class="activeReviewTab === 'boq'
              ? 'bg-white text-primary shadow-sm'
              : 'text-tertiary hover:text-secondary'"
            @click="activeReviewTab = 'boq'"
          >
            <i class="pi pi-table me-1.5"></i>
            جدول الكميات ({{ extractionResult.boqItems.length }})
          </button>
        </div>

        <!-- Tab: Overview -->
        <div v-if="activeReviewTab === 'overview'" class="rounded-2xl border bg-white p-6">
          <h4 class="mb-4 text-sm font-bold text-secondary">البيانات الأساسية المستخرجة</h4>
          <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div class="rounded-xl bg-gray-50 p-4">
              <label class="text-[10px] font-medium uppercase tracking-wider text-tertiary">اسم المشروع</label>
              <p class="mt-1 text-sm font-semibold text-secondary">{{ extractionResult.projectNameAr || 'غير محدد' }}</p>
            </div>
            <div class="rounded-xl bg-gray-50 p-4">
              <label class="text-[10px] font-medium uppercase tracking-wider text-tertiary">نوع المنافسة</label>
              <p class="mt-1 text-sm font-semibold text-secondary">{{ getCompetitionTypeLabel(extractionResult.detectedCompetitionType) }}</p>
            </div>
            <div class="rounded-xl bg-gray-50 p-4">
              <label class="text-[10px] font-medium uppercase tracking-wider text-tertiary">الميزانية التقديرية</label>
              <p class="mt-1 text-sm font-semibold text-secondary">
                {{ extractionResult.estimatedBudget
                  ? new Intl.NumberFormat('ar-SA', { style: 'currency', currency: 'SAR' }).format(extractionResult.estimatedBudget)
                  : 'غير محدد' }}
              </p>
            </div>
            <div class="rounded-xl bg-gray-50 p-4">
              <label class="text-[10px] font-medium uppercase tracking-wider text-tertiary">مدة المشروع</label>
              <p class="mt-1 text-sm font-semibold text-secondary">
                {{ extractionResult.projectDurationDays
                  ? `${extractionResult.projectDurationDays} يوم`
                  : 'غير محدد' }}
              </p>
            </div>
          </div>

          <div v-if="extractionResult.projectDescription" class="mt-4 rounded-xl bg-gray-50 p-4">
            <label class="text-[10px] font-medium uppercase tracking-wider text-tertiary">وصف المشروع</label>
            <p class="mt-1 text-sm leading-relaxed text-secondary">{{ extractionResult.projectDescription }}</p>
          </div>

          <div class="mt-4 rounded-xl bg-blue-50 p-4">
            <label class="text-[10px] font-medium uppercase tracking-wider text-blue-600">ملخص الاستخراج</label>
            <p class="mt-1 text-sm leading-relaxed text-blue-800">{{ extractionResult.extractionSummaryAr }}</p>
          </div>
        </div>

        <!-- Tab: Sections -->
        <div v-if="activeReviewTab === 'sections'" class="space-y-3">
          <div
            v-for="(section, idx) in extractionResult.sections"
            :key="idx"
            class="rounded-2xl border bg-white p-5 transition-all hover:shadow-sm"
          >
            <div class="flex items-start justify-between">
              <div class="flex items-center gap-3">
                <div
                  class="flex h-8 w-8 items-center justify-center rounded-lg text-xs font-bold text-white"
                  :style="{ backgroundColor: getSectionTypeColor(section.sectionType) }"
                >
                  {{ idx + 1 }}
                </div>
                <div>
                  <h5 class="text-sm font-bold text-secondary">{{ section.titleAr }}</h5>
                  <div class="mt-0.5 flex items-center gap-2">
                    <span class="rounded-md px-1.5 py-0.5 text-[10px] font-medium"
                      :style="{
                        backgroundColor: getSectionTypeColor(section.sectionType) + '15',
                        color: getSectionTypeColor(section.sectionType)
                      }"
                    >
                      {{ getSectionTypeLabel(section.sectionType) }}
                    </span>
                    <span v-if="section.isMandatory" class="rounded-md bg-red-50 px-1.5 py-0.5 text-[10px] font-medium text-red-600">
                      إلزامي
                    </span>
                  </div>
                </div>
              </div>
              <div class="text-xs text-tertiary">
                {{ Math.round(section.confidenceScore) }}% ثقة
              </div>
            </div>
            <!-- Content preview -->
            <div
              v-if="section.contentHtml"
              class="mt-3 max-h-32 overflow-hidden rounded-lg bg-gray-50 p-3 text-xs leading-relaxed text-secondary"
              v-html="section.contentHtml"
            ></div>
          </div>
        </div>

        <!-- Tab: BOQ -->
        <div v-if="activeReviewTab === 'boq'" class="rounded-2xl border bg-white overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-4 py-3 text-start text-xs font-semibold text-tertiary">#</th>
                <th class="px-4 py-3 text-start text-xs font-semibold text-tertiary">الوصف</th>
                <th class="px-4 py-3 text-center text-xs font-semibold text-tertiary">الوحدة</th>
                <th class="px-4 py-3 text-center text-xs font-semibold text-tertiary">الكمية</th>
                <th class="px-4 py-3 text-center text-xs font-semibold text-tertiary">سعر الوحدة</th>
                <th class="px-4 py-3 text-center text-xs font-semibold text-tertiary">الإجمالي</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(item, idx) in extractionResult.boqItems"
                :key="idx"
                class="border-t border-gray-100"
              >
                <td class="px-4 py-3 text-xs text-tertiary">{{ item.itemNumber }}</td>
                <td class="px-4 py-3 text-xs font-medium text-secondary">{{ item.descriptionAr }}</td>
                <td class="px-4 py-3 text-center text-xs text-tertiary">{{ item.unit }}</td>
                <td class="px-4 py-3 text-center text-xs text-secondary">{{ item.quantity.toLocaleString('en') }}</td>
                <td class="px-4 py-3 text-center text-xs text-secondary">
                  {{ item.estimatedUnitPrice ? item.estimatedUnitPrice.toLocaleString('en') : '-' }}
                </td>
                <td class="px-4 py-3 text-center text-xs font-semibold text-secondary">
                  {{ item.estimatedUnitPrice
                    ? (item.quantity * item.estimatedUnitPrice).toLocaleString('en')
                    : '-' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Action Buttons -->
        <div class="mt-6 flex items-center justify-between">
          <button
            class="flex items-center gap-2 rounded-xl border border-secondary-200 bg-white px-5 py-2.5 text-sm font-medium text-secondary transition-all hover:bg-gray-50"
            @click="resetUpload"
          >
            <i class="pi pi-refresh text-xs"></i>
            رفع ملف آخر
          </button>

          <button
            class="flex items-center gap-2 rounded-xl bg-gradient-to-r from-primary to-primary-dark px-8 py-3 text-sm font-bold text-white shadow-md transition-all hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="isCreating"
            @click="createFromExtraction"
          >
            <i v-if="isCreating" class="pi pi-spin pi-spinner text-xs"></i>
            <i v-else class="pi pi-check text-xs"></i>
            {{ isCreating ? 'جاري الإنشاء...' : 'إنشاء كراسة الشروط' }}
          </button>
        </div>
      </div>
    </Transition>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!--  Upload & Extract: Creating Phase                           -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Transition
      enter-active-class="transition-all duration-300"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
    >
      <div
        v-if="extractPhase === 'creating'"
        class="mt-8 w-full max-w-3xl"
      >
        <div class="rounded-2xl border bg-white p-12 text-center shadow-lg">
          <i class="pi pi-spin pi-spinner mb-4 text-4xl text-primary"></i>
          <h3 class="text-lg font-bold text-secondary">جاري إنشاء كراسة الشروط</h3>
          <p class="mt-2 text-sm text-tertiary">
            يتم الآن إنشاء المنافسة وحفظ الأقسام وجدول الكميات المستخرجة...
          </p>
        </div>
      </div>
    </Transition>

    <!-- Back to list link -->
    <div class="mt-8">
      <router-link
        :to="{ name: 'rfp-list' }"
        class="flex items-center gap-1 text-sm text-tertiary transition-colors hover:text-interactive"
      >
        <i class="pi pi-arrow-right text-xs ltr:rotate-180"></i>
        {{ t('rfp.methodSelection.backToList') }}
      </router-link>
    </div>
  </div>
</template>
