<script setup lang="ts">
/**
 * RfpMethodSelectionView - Competition Creation Method Selection
 *
 * Matches the RFP.AI reference design with 3 creation methods:
 * 1. From Template - Start with a pre-built template
 * 2. Upload & Extract - AI-powered document extraction
 * 3. From Scratch - Manual creation
 *
 * This page is the entry point for creating a new competition.
 */
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

const { t } = useI18n()
const router = useRouter()

const selectedMethod = ref<string | null>(null)
const isUploading = ref(false)
const uploadedFile = ref<File | null>(null)
const dragOver = ref(false)

interface CreationMethod {
  key: string
  icon: string
  titleKey: string
  descriptionKey: string
  features: string[]
  badge?: string
  badgeColor?: string
  gradient: string
  action: () => void
}

const methods: CreationMethod[] = [
  {
    key: 'template',
    icon: 'pi-th-large',
    titleKey: 'rfp.methodSelection.fromTemplate',
    descriptionKey: 'rfp.methodSelection.fromTemplateDesc',
    features: [
      'rfp.methodSelection.templateFeature1',
      'rfp.methodSelection.templateFeature2',
      'rfp.methodSelection.templateFeature3',
      'rfp.methodSelection.templateFeature4',
    ],
    badge: 'rfp.methodSelection.newBadge',
    badgeColor: 'bg-accent text-white',
    gradient: 'from-purple-50 to-indigo-50 border-purple-200 hover:border-purple-400',
    action: () => router.push({ name: 'RfpTemplates' }),
  },
  {
    key: 'upload',
    icon: 'pi-cloud-upload',
    titleKey: 'rfp.methodSelection.uploadExtract',
    descriptionKey: 'rfp.methodSelection.uploadExtractDesc',
    features: [
      'rfp.methodSelection.uploadFeature1',
      'rfp.methodSelection.uploadFeature2',
      'rfp.methodSelection.uploadFeature3',
    ],
    badge: 'AI',
    badgeColor: 'bg-interactive text-white',
    gradient: 'from-blue-50 to-cyan-50 border-blue-200 hover:border-blue-400',
    action: () => { selectedMethod.value = 'upload' },
  },
  {
    key: 'scratch',
    icon: 'pi-file-edit',
    titleKey: 'rfp.methodSelection.fromScratch',
    descriptionKey: 'rfp.methodSelection.fromScratchDesc',
    features: [
      'rfp.methodSelection.scratchFeature1',
      'rfp.methodSelection.scratchFeature2',
      'rfp.methodSelection.scratchFeature3',
    ],
    gradient: 'from-gray-50 to-slate-50 border-secondary-200 hover:border-secondary-400',
    action: () => router.push({ name: 'rfp-create' }),
  },
]

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
  const allowedTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  ]
  if (!allowedTypes.includes(file.type)) {
    return
  }
  uploadedFile.value = file
  isUploading.value = true

  // Simulate AI extraction (will connect to real API)
  setTimeout(() => {
    isUploading.value = false
    router.push({ name: 'rfp-create', query: { source: 'upload', file: file.name } })
  }, 2000)
}
</script>

<template>
  <div class="min-h-[80vh] flex flex-col items-center justify-center px-4">
    <!-- Close button -->
    <div class="mb-8 flex w-full max-w-4xl items-center justify-between">
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

    <!-- Method Cards -->
    <div class="grid w-full max-w-4xl grid-cols-1 gap-5 md:grid-cols-3">
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
          {{ method.badge === 'AI' ? 'AI' : t(method.badge) }}
        </span>

        <!-- Icon -->
        <div class="mb-5 flex justify-center">
          <div
            class="flex h-16 w-16 items-center justify-center rounded-2xl transition-transform duration-300 group-hover:scale-110"
            :class="method.key === 'template' ? 'bg-purple-100 text-purple-600' : method.key === 'upload' ? 'bg-blue-100 text-blue-600' : 'bg-slate-100 text-slate-600'"
          >
            <i class="pi text-2xl" :class="method.icon"></i>
          </div>
        </div>

        <!-- Title & Description -->
        <h3 class="mb-2 text-center text-base font-bold text-secondary">
          {{ t(method.titleKey) }}
        </h3>
        <p class="mb-5 text-center text-xs leading-relaxed text-tertiary">
          {{ t(method.descriptionKey) }}
        </p>

        <!-- Features -->
        <ul class="space-y-2">
          <li
            v-for="(feature, idx) in method.features"
            :key="idx"
            class="flex items-center gap-2 text-xs text-secondary-600"
          >
            <i class="pi pi-check-circle text-xs text-success"></i>
            <span>{{ t(feature) }}</span>
          </li>
        </ul>

        <!-- CTA for template method -->
        <button
          v-if="method.key === 'template'"
          class="mt-5 flex w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-purple-500 to-indigo-500 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:shadow-md"
          @click.stop="handleMethodClick(method)"
        >
          {{ t('rfp.methodSelection.startWithTemplates') }}
          <i class="pi pi-arrow-left text-xs ltr:rotate-180"></i>
        </button>
      </div>
    </div>

    <!-- Upload Area (shown when upload method is selected) -->
    <Transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="selectedMethod === 'upload'"
        class="mt-8 w-full max-w-4xl"
      >
        <div
          class="relative rounded-2xl border-2 border-dashed p-10 text-center transition-colors"
          :class="dragOver ? 'border-primary bg-primary/5' : 'border-secondary-300 bg-white'"
          @dragover="handleDragOver"
          @dragleave="handleDragLeave"
          @drop="handleDrop"
        >
          <div v-if="isUploading" class="flex flex-col items-center">
            <i class="pi pi-spin pi-spinner mb-3 text-4xl text-primary"></i>
            <p class="text-sm font-semibold text-secondary">{{ t('rfp.methodSelection.extracting') }}</p>
            <p class="mt-1 text-xs text-tertiary">{{ t('rfp.methodSelection.extractingDesc') }}</p>
          </div>
          <div v-else>
            <i class="pi pi-cloud-upload mb-3 text-4xl text-secondary-300"></i>
            <p class="text-sm font-semibold text-secondary">{{ t('rfp.methodSelection.dropHere') }}</p>
            <p class="mt-1 text-xs text-tertiary">PDF, Word {{ t('rfp.methodSelection.maxSize') }}</p>
            <label class="mt-4 inline-flex cursor-pointer items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-primary-dark">
              <i class="pi pi-upload text-xs"></i>
              {{ t('rfp.methodSelection.browseFiles') }}
              <input type="file" class="hidden" accept=".pdf,.doc,.docx" @change="handleFileInput" />
            </label>
          </div>
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
