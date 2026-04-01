<script setup lang="ts">
/**
 * AiCriteriaSuggester Component
 *
 * Uses the AI Text Assist endpoint to suggest evaluation criteria
 * based on the project name, description, and competition type.
 * Parses the AI response into structured criteria objects.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRfpStore } from '@/stores/rfp'
import http from '@/services/http'

const emit = defineEmits<{
  (e: 'criteria-suggested', criteria: Array<{ name: string; weight: number; description: string }>): void
}>()

const { locale } = useI18n()
const rfpStore = useRfpStore()

const isGenerating = ref(false)
const error = ref('')
const suggestedCriteria = ref<Array<{ name: string; weight: number; description: string }>>([])
const showPreview = ref(false)

const projectName = computed(() => rfpStore.formData.basicInfo.projectName || '')
const projectDescription = computed(() => rfpStore.formData.basicInfo.projectDescription || '')
const competitionType = computed(() => rfpStore.formData.basicInfo.competitionType || '')

async function handleGenerate() {
  isGenerating.value = true
  error.value = ''
  suggestedCriteria.value = []

  try {
    const response = await http.post<{ isSuccess: boolean; generatedText: string; errorMessage?: string }>(
      '/v1/ai/text/assist',
      {
        action: 'custom',
        currentText: '',
        fieldName: 'معايير التقييم',
        fieldPurpose: 'تحديد معايير تقييم العروض الفنية للمنافسة',
        projectName: projectName.value,
        projectDescription: projectDescription.value,
        competitionType: competitionType.value,
        additionalContext: `طريقة التقييم: ${rfpStore.formData.settings.evaluationMethod || 'weighted_criteria'}`,
        customPrompt: `قم بإنشاء قائمة معايير تقييم فنية مناسبة لهذا المشروع.

القواعد:
1. أنشئ من 4 إلى 6 معايير تقييم
2. يجب أن يكون مجموع الأوزان 100%
3. لكل معيار: اسم المعيار، الوزن (نسبة مئوية)، وصف مختصر
4. استخدم الفاصل "|" بين الحقول
5. ضع كل معيار في سطر منفصل
6. الصيغة: اسم المعيار|الوزن|الوصف

مثال:
الخبرة والمؤهلات|25|خبرة الشركة في تنفيذ مشاريع مماثلة ومؤهلات فريق العمل
المنهجية والخطة التنفيذية|20|وضوح المنهجية المقترحة وجدوى الخطة الزمنية

أنشئ المعايير المناسبة لهذا المشروع فقط بالصيغة المطلوبة بدون أي نص إضافي.`,
        language: 'ar',
      },
      { timeout: 120_000 },
    )

    if (response.data.isSuccess && response.data.generatedText) {
      const parsed = parseCriteria(response.data.generatedText)
      if (parsed.length > 0) {
        suggestedCriteria.value = parsed
        showPreview.value = true
      } else {
        error.value = locale.value === 'ar'
          ? 'لم يتمكن الذكاء الاصطناعي من توليد معايير مناسبة. حاول مرة أخرى.'
          : 'AI could not generate suitable criteria. Please try again.'
      }
    } else {
      error.value = response.data.errorMessage
        || (locale.value === 'ar' ? 'فشل في توليد المعايير' : 'Failed to generate criteria')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage
      || (locale.value === 'ar' ? 'حدث خطأ أثناء الاتصال بالذكاء الاصطناعي' : 'Error connecting to AI service')
  } finally {
    isGenerating.value = false
  }
}

function parseCriteria(text: string): Array<{ name: string; weight: number; description: string }> {
  const lines = text.split('\n').filter(line => line.trim() && line.includes('|'))
  const criteria: Array<{ name: string; weight: number; description: string }> = []

  for (const line of lines) {
    const parts = line.split('|').map(p => p.trim())
    if (parts.length >= 3) {
      const name = parts[0]
      const weight = parseInt(parts[1].replace(/[^0-9]/g, ''), 10)
      const description = parts[2]
      if (name && !isNaN(weight) && weight > 0) {
        criteria.push({ name, weight, description: description || '' })
      }
    }
  }

  // Normalize weights to sum to 100 if they don't
  const totalWeight = criteria.reduce((sum, c) => sum + c.weight, 0)
  if (totalWeight !== 100 && totalWeight > 0) {
    const factor = 100 / totalWeight
    let remaining = 100
    criteria.forEach((c, i) => {
      if (i === criteria.length - 1) {
        c.weight = remaining
      } else {
        c.weight = Math.round(c.weight * factor)
        remaining -= c.weight
      }
    })
  }

  return criteria
}

function applyCriteria() {
  emit('criteria-suggested', suggestedCriteria.value)
  showPreview.value = false
}

const totalWeight = computed(() => suggestedCriteria.value.reduce((sum, c) => sum + c.weight, 0))
</script>

<template>
  <div class="ai-criteria-suggester">
    <!-- Generate Button -->
    <button
      type="button"
      class="group flex items-center gap-2 rounded-lg border border-ai-300 bg-gradient-to-r from-ai-50 to-white px-4 py-2 text-sm font-medium text-ai-600 transition-all duration-200 hover:border-ai-400 hover:shadow-md hover:shadow-ai-100/50"
      :disabled="isGenerating"
      @click="handleGenerate"
    >
      <i
        class="pi text-sm transition-transform duration-200 group-hover:scale-110"
        :class="isGenerating ? 'pi-spin pi-spinner' : 'pi-sparkles'"
      ></i>
      <span>{{ isGenerating
        ? (locale === 'ar' ? 'جارٍ اقتراح المعايير...' : 'Suggesting criteria...')
        : (locale === 'ar' ? 'اقتراح معايير بالذكاء الاصطناعي' : 'Suggest Criteria with AI')
      }}</span>
    </button>

    <!-- Error -->
    <div v-if="error" class="mt-3 rounded-lg border border-danger/20 bg-danger/5 p-3 text-xs text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ error }}
    </div>

    <!-- Preview Modal -->
    <Transition name="fade">
      <div
        v-if="showPreview"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm"
        @click.self="showPreview = false"
      >
        <div class="mx-4 max-h-[80vh] w-full max-w-2xl overflow-hidden rounded-2xl bg-white shadow-2xl">
          <!-- Modal Header -->
          <div class="border-b border-secondary-100 bg-gradient-to-r from-ai-50 to-white p-5">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100">
                <i class="pi pi-sparkles text-lg text-ai-600"></i>
              </div>
              <div>
                <h3 class="text-base font-bold text-secondary-800">
                  {{ locale === 'ar' ? 'معايير التقييم المقترحة' : 'Suggested Evaluation Criteria' }}
                </h3>
                <p class="text-xs text-secondary-500">
                  {{ locale === 'ar' ? 'راجع المعايير ثم اضغط تطبيق لإضافتها' : 'Review criteria then click Apply to add them' }}
                </p>
              </div>
              <button
                type="button"
                class="ms-auto flex h-8 w-8 items-center justify-center rounded-lg hover:bg-secondary-100"
                @click="showPreview = false"
              >
                <i class="pi pi-times text-sm text-secondary-400"></i>
              </button>
            </div>
          </div>

          <!-- Criteria List -->
          <div class="max-h-[50vh] overflow-auto p-5">
            <div class="space-y-3">
              <div
                v-for="(criterion, idx) in suggestedCriteria"
                :key="idx"
                class="rounded-lg border border-secondary-100 bg-white p-4 transition-shadow hover:shadow-sm"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1">
                    <div class="flex items-center gap-2">
                      <span class="flex h-6 w-6 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                        {{ idx + 1 }}
                      </span>
                      <h4 class="text-sm font-semibold text-secondary-800">{{ criterion.name }}</h4>
                    </div>
                    <p class="mt-1 ps-8 text-xs leading-relaxed text-secondary-500">{{ criterion.description }}</p>
                  </div>
                  <span class="rounded-full bg-primary/10 px-3 py-1 text-sm font-bold text-primary">
                    {{ criterion.weight }}%
                  </span>
                </div>
              </div>
            </div>

            <!-- Total Weight -->
            <div
              class="mt-4 flex items-center justify-end gap-2 text-sm"
              :class="totalWeight === 100 ? 'text-success' : 'text-warning'"
            >
              <i :class="totalWeight === 100 ? 'pi pi-check-circle' : 'pi pi-exclamation-circle'"></i>
              <span>
                {{ locale === 'ar' ? 'إجمالي الأوزان' : 'Total Weight' }}: {{ totalWeight }}%
              </span>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 bg-secondary-50 px-5 py-4">
            <button
              type="button"
              class="rounded-lg border border-secondary-200 bg-white px-4 py-2 text-sm text-secondary-600 hover:bg-secondary-50"
              @click="showPreview = false"
            >
              {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg bg-gradient-to-r from-ai-500 to-ai-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:from-ai-600 hover:to-ai-700"
              @click="applyCriteria"
            >
              <i class="pi pi-check text-xs"></i>
              <span>{{ locale === 'ar' ? 'تطبيق المعايير' : 'Apply Criteria' }}</span>
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
