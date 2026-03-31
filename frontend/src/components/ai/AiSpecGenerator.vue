<script setup lang="ts">
/**
 * AiSpecGenerator - AI-Powered Specification Generation (TASK-1001)
 *
 * Uses Gemini AI + RAG to generate competition specifications:
 * - Project description input
 * - AI generates structured sections based on:
 *   - Knowledge base documents (RAG)
 *   - Government procurement regulations
 *   - Previous similar competitions
 * - Section-by-section generation with user review
 * - Arabic-only output (STRICT: no English in AI output)
 * - Streaming response display
 *
 * All AI config fetched from database (AiConfiguration table).
 */
import { ref, reactive, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpPost } from '@/services/http'

useI18n() // i18n available for future use

const emit = defineEmits<{
  (e: 'generated', sections: GeneratedSection[]): void
  (e: 'close'): void
}>()

const props = defineProps<{
  competitionType?: string
  projectName?: string
}>()

interface GeneratedSection {
  titleAr: string
  titleEn: string
  content: string
  order: number
  isApproved: boolean
}

interface GenerationRequest {
  projectDescription: string
  competitionType: string
  estimatedBudget: string
  specialRequirements: string
  useKnowledgeBase: boolean
  usePreviousCompetitions: boolean
}

/* ── State ── */
const isGenerating = ref(false)
const generationProgress = ref(0)
const generatedSections = ref<GeneratedSection[]>([])
const streamingText = ref('')
const showPreview = ref(false)

const request = reactive<GenerationRequest>({
  projectDescription: '',
  competitionType: props.competitionType || 'it',
  estimatedBudget: '',
  specialRequirements: '',
  useKnowledgeBase: true,
  usePreviousCompetitions: true,
})

const competitionTypes = [
  { value: 'it', label: 'تقنية المعلومات' },
  { value: 'construction', label: 'مشاريع إنشائية' },
  { value: 'consulting', label: 'استشارات' },
  { value: 'maintenance', label: 'صيانة وتشغيل' },
  { value: 'supplies', label: 'توريدات ومعدات' },
  { value: 'services', label: 'خدمات عامة' },
]

const canGenerate = computed(() =>
  request.projectDescription.length >= 50 &&
  request.competitionType !== ''
)

/* ── Methods ── */
async function generateSpecs(): Promise<void> {
  if (!canGenerate.value) return

  isGenerating.value = true
  generationProgress.value = 0
  streamingText.value = ''
  generatedSections.value = []

  try {
    const response = await httpPost<{
      sections: GeneratedSection[]
      metadata: { model: string; tokensUsed: number; ragChunksUsed: number }
    }>('/v1/ai/generate-specs', {
      projectDescription: request.projectDescription,
      competitionType: request.competitionType,
      estimatedBudget: request.estimatedBudget,
      specialRequirements: request.specialRequirements,
      useKnowledgeBase: request.useKnowledgeBase,
      usePreviousCompetitions: request.usePreviousCompetitions,
    })

    generatedSections.value = response.sections.map(s => ({
      ...s,
      isApproved: false,
    }))
    showPreview.value = true
  } catch (err) {
    console.error('AI generation failed:', err)
  } finally {
    isGenerating.value = false
    generationProgress.value = 100
  }
}

function approveSection(index: number): void {
  generatedSections.value[index].isApproved = true
}

function approveAll(): void {
  generatedSections.value.forEach(s => (s.isApproved = true))
}

function applyApproved(): void {
  const approved = generatedSections.value.filter(s => s.isApproved)
  emit('generated', approved)
}

function regenerateSection(index: number): void {
  /* TODO: Call AI to regenerate specific section */
  console.log('Regenerating section:', index)
}
</script>

<template>
  <div class="space-y-6">
    <!-- Generation Form -->
    <div v-if="!showPreview" class="space-y-5">
      <!-- Header -->
      <div class="flex items-center gap-3">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-ai to-ai-dark">
          <i class="pi pi-sparkles text-xl text-white"></i>
        </div>
        <div>
          <h2 class="text-lg font-bold text-secondary">مساعد توليد المواصفات بالذكاء الاصطناعي</h2>
          <p class="text-xs text-tertiary">يستخدم قاعدة المعرفة والأنظمة الحكومية لتوليد مواصفات متوافقة</p>
        </div>
      </div>

      <!-- Project Description -->
      <div>
        <label class="mb-1.5 block text-sm font-semibold text-secondary-700">وصف المشروع</label>
        <textarea
          v-model="request.projectDescription"
          rows="4"
          class="input text-sm"
          placeholder="اكتب وصفاً تفصيلياً للمشروع المراد إعداد كراسة الشروط والمواصفات له..."
        ></textarea>
        <p class="mt-1 text-[10px] text-secondary-400">
          {{ request.projectDescription.length }}/50 حرف كحد أدنى
        </p>
      </div>

      <!-- Competition Type -->
      <div>
        <label class="mb-1.5 block text-sm font-semibold text-secondary-700">نوع المنافسة</label>
        <select v-model="request.competitionType" class="input text-sm">
          <option v-for="ct in competitionTypes" :key="ct.value" :value="ct.value">
            {{ ct.label }}
          </option>
        </select>
      </div>

      <!-- Budget -->
      <div>
        <label class="mb-1.5 block text-sm font-semibold text-secondary-700">الميزانية التقديرية</label>
        <input
          v-model="request.estimatedBudget"
          type="text"
          class="input text-sm"
          placeholder="مثال: 5,000,000 ﷼"
        />
      </div>

      <!-- Special Requirements -->
      <div>
        <label class="mb-1.5 block text-sm font-semibold text-secondary-700">متطلبات خاصة</label>
        <textarea
          v-model="request.specialRequirements"
          rows="2"
          class="input text-sm"
          placeholder="أي متطلبات خاصة أو ملاحظات إضافية..."
        ></textarea>
      </div>

      <!-- RAG Options -->
      <div class="rounded-xl border border-ai/20 bg-ai-50/30 p-4">
        <h4 class="mb-3 text-xs font-bold text-secondary-700">مصادر المعرفة</h4>
        <div class="space-y-2">
          <label class="flex items-center gap-2">
            <input
              v-model="request.useKnowledgeBase"
              type="checkbox"
              class="h-4 w-4 rounded border-secondary-300 text-ai focus:ring-ai"
            />
            <span class="text-xs text-secondary-700">استخدام قاعدة المعرفة (الأنظمة واللوائح)</span>
          </label>
          <label class="flex items-center gap-2">
            <input
              v-model="request.usePreviousCompetitions"
              type="checkbox"
              class="h-4 w-4 rounded border-secondary-300 text-ai focus:ring-ai"
            />
            <span class="text-xs text-secondary-700">الاستفادة من المنافسات السابقة المشابهة</span>
          </label>
        </div>
      </div>

      <!-- Generate Button -->
      <div class="flex justify-end gap-3">
        <button class="btn-ghost" @click="emit('close')">إلغاء</button>
        <button
          class="btn-ai"
          :disabled="!canGenerate || isGenerating"
          @click="generateSpecs"
        >
          <i class="pi" :class="isGenerating ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
          {{ isGenerating ? 'جاري التوليد...' : 'توليد المواصفات' }}
        </button>
      </div>

      <!-- Progress -->
      <div v-if="isGenerating" class="space-y-2">
        <div class="progress-bar">
          <div class="progress-bar-fill bg-ai animate-pulse" style="width: 60%"></div>
        </div>
        <p class="text-center text-xs text-ai">يتم تحليل المتطلبات وتوليد المواصفات...</p>
      </div>
    </div>

    <!-- Preview Generated Sections -->
    <div v-else class="space-y-5">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <button class="btn-ghost btn-sm" @click="showPreview = false">
            <i class="pi pi-arrow-right"></i>
          </button>
          <h2 class="text-lg font-bold text-secondary">مراجعة المواصفات المولّدة</h2>
        </div>
        <div class="flex gap-2">
          <button class="btn-ghost btn-sm" @click="approveAll">
            <i class="pi pi-check-circle"></i>
            اعتماد الكل
          </button>
          <button class="btn-ai btn-sm" @click="applyApproved">
            <i class="pi pi-check"></i>
            تطبيق المعتمد
          </button>
        </div>
      </div>

      <div class="space-y-3">
        <div
          v-for="(section, idx) in generatedSections"
          :key="idx"
          class="rounded-2xl border p-4 transition-all"
          :class="section.isApproved
            ? 'border-success/30 bg-success-50/30'
            : 'border-secondary-100 bg-white'"
        >
          <div class="flex items-start justify-between mb-2">
            <div class="flex items-center gap-2">
              <span class="flex h-6 w-6 items-center justify-center rounded-full bg-primary-50 text-[10px] font-bold text-primary">
                {{ section.order }}
              </span>
              <h4 class="text-sm font-bold text-secondary">{{ section.titleAr }}</h4>
            </div>
            <div class="flex gap-1">
              <button
                class="btn-icon text-xs"
                :class="section.isApproved ? 'text-success' : 'text-secondary-400 hover:text-success'"
                @click="approveSection(idx)"
              >
                <i class="pi pi-check-circle"></i>
              </button>
              <button
                class="btn-icon text-xs text-secondary-400 hover:text-ai"
                @click="regenerateSection(idx)"
              >
                <i class="pi pi-refresh"></i>
              </button>
            </div>
          </div>
          <div class="prose prose-sm text-secondary-600 text-xs leading-relaxed" v-html="section.content"></div>
        </div>
      </div>
    </div>
  </div>
</template>
