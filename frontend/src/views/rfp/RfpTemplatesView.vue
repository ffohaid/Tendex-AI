<script setup lang="ts">
/**
 * RfpTemplatesView - Competition Templates Management
 *
 * Features:
 * - Browse and search competition templates
 * - Create new template from existing competition
 * - Copy competition from template
 * - Template categories (IT, Construction, Consulting, etc.)
 * - Template preview
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { httpGet, httpPost } from '@/services/http'

const { t, locale } = useI18n()
const router = useRouter()

interface RfpTemplate {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr: string
  descriptionEn: string
  category: string
  sectionCount: number
  boqItemCount: number
  usageCount: number
  createdAt: string
  createdBy: string
  isOfficial: boolean
  tags: string[]
}

const isLoading = ref(false)
const isCopying = ref<string | null>(null)
const templates = ref<RfpTemplate[]>([])
const searchQuery = ref('')
const selectedCategory = ref('all')

const categories = computed(() => [
  { key: 'all', labelAr: 'الكل', labelEn: 'All' },
  { key: 'it', labelAr: 'تقنية المعلومات', labelEn: 'Information Technology' },
  { key: 'construction', labelAr: 'الإنشاءات', labelEn: 'Construction' },
  { key: 'consulting', labelAr: 'الاستشارات', labelEn: 'Consulting' },
  { key: 'maintenance', labelAr: 'الصيانة والتشغيل', labelEn: 'Maintenance' },
  { key: 'supplies', labelAr: 'التوريدات والمعدات', labelEn: 'Supplies & Equipment' },
  { key: 'services', labelAr: 'الخدمات العامة', labelEn: 'General Services' },
])

const filteredTemplates = computed(() => {
  let result = templates.value
  if (selectedCategory.value !== 'all') {
    result = result.filter(t => t.category === selectedCategory.value)
  }
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(t =>
      t.nameAr.toLowerCase().includes(q) ||
      t.nameEn.toLowerCase().includes(q) ||
      t.tags.some(tag => tag.toLowerCase().includes(q))
    )
  }
  return result
})

async function loadTemplates(): Promise<void> {
  isLoading.value = true
  try {
    const data = await httpGet<{ items: RfpTemplate[] }>('/v1/rfp/templates')
    templates.value = data.items
  } catch {
    console.warn('Templates API not available yet')
  } finally {
    isLoading.value = false
  }
}

async function copyFromTemplate(templateId: string): Promise<void> {
  isCopying.value = templateId
  try {
    const result = await httpPost<{ rfpId: string }>(`/v1/rfp/templates/${templateId}/copy`)
    router.push({ name: 'rfp-edit', params: { id: result.rfpId } })
  } catch {
    console.error('Failed to copy template')
  } finally {
    isCopying.value = null
  }
}

function createNewRfp(): void {
  router.push({ name: 'rfp-method-selection' })
}

onMounted(() => {
  loadTemplates()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">{{ t('rfp.templates') }}</h1>
        <p class="page-description">
          {{ locale === 'ar' ? 'تصفح واستخدم قوالب كراسات الشروط الجاهزة' : 'Browse and use pre-built competition templates' }}
        </p>
      </div>
      <button class="btn-primary" @click="createNewRfp">
        <i class="pi pi-plus"></i>
        {{ t('rfp.actions.createNew') }}
      </button>
    </div>

    <!-- Search + Categories -->
    <div class="card !p-4">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-center">
        <div class="relative flex-1">
          <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-secondary-400"></i>
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="locale === 'ar' ? 'البحث في القوالب...' : 'Search templates...'"
            class="input ps-10 text-sm"
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
      <div v-for="i in 6" :key="i" class="skeleton-card"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredTemplates.length === 0" class="card !py-16 text-center">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-secondary-50">
        <i class="pi pi-copy text-2xl text-secondary-400"></i>
      </div>
      <h3 class="text-lg font-bold text-secondary-700">
        {{ locale === 'ar' ? 'لا توجد قوالب حالياً' : 'No templates found' }}
      </h3>
      <p class="mt-2 text-sm text-secondary-500">
        {{ locale === 'ar' ? 'سيتم إضافة قوالب جاهزة قريباً. يمكنك إنشاء كراسة شروط جديدة من البداية.' : 'Templates will be added soon. You can create a new RFP from scratch.' }}
      </p>
      <button class="btn-primary mx-auto mt-6" @click="createNewRfp">
        <i class="pi pi-plus"></i>
        {{ t('rfp.actions.createNew') }}
      </button>
    </div>

    <!-- Templates Grid -->
    <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="tmpl in filteredTemplates"
        :key="tmpl.id"
        class="card-interactive"
      >
        <div class="mb-3 flex items-start justify-between">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-50">
            <i class="pi pi-file text-primary"></i>
          </div>
          <div class="flex items-center gap-1">
            <span v-if="tmpl.isOfficial" class="badge badge-success text-[10px]">
              <i class="pi pi-verified text-[8px]"></i>
              {{ locale === 'ar' ? 'رسمي' : 'Official' }}
            </span>
            <span class="badge badge-info text-[10px]">{{ tmpl.category }}</span>
          </div>
        </div>

        <h3 class="text-sm font-bold text-secondary-800">
          {{ locale === 'ar' ? tmpl.nameAr : tmpl.nameEn }}
        </h3>
        <p class="mt-1 line-clamp-2 text-xs text-secondary-500">
          {{ locale === 'ar' ? tmpl.descriptionAr : tmpl.descriptionEn }}
        </p>

        <div class="mt-3 flex items-center gap-3 text-[10px] text-secondary-400">
          <span>
            <i class="pi pi-list me-1"></i>
            {{ tmpl.sectionCount }} {{ locale === 'ar' ? 'قسم' : 'sections' }}
          </span>
          <span>
            <i class="pi pi-table me-1"></i>
            {{ tmpl.boqItemCount }} {{ locale === 'ar' ? 'بند' : 'BOQ items' }}
          </span>
          <span>
            <i class="pi pi-copy me-1"></i>
            {{ locale === 'ar' ? `استُخدم ${tmpl.usageCount} مرة` : `Used ${tmpl.usageCount} times` }}
          </span>
        </div>

        <!-- Tags -->
        <div v-if="tmpl.tags.length > 0" class="mt-2 flex flex-wrap gap-1">
          <span
            v-for="tag in tmpl.tags.slice(0, 4)"
            :key="tag"
            class="rounded-full bg-secondary-50 px-2 py-0.5 text-[10px] text-secondary-500"
          >
            {{ tag }}
          </span>
        </div>

        <!-- Copy Button -->
        <button
          class="btn-primary btn-sm mt-4 w-full"
          :disabled="isCopying === tmpl.id"
          @click="copyFromTemplate(tmpl.id)"
        >
          <i class="pi" :class="isCopying === tmpl.id ? 'pi-spin pi-spinner' : 'pi-copy'"></i>
          {{ locale === 'ar' ? 'استخدام هذا القالب' : 'Use This Template' }}
        </button>
      </div>
    </div>
  </div>
</template>
