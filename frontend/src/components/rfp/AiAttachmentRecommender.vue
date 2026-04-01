<script setup lang="ts">
/**
 * AiAttachmentRecommender Component
 *
 * Uses AI to recommend which mandatory attachments should be selected
 * based on the project type, competition type, and content.
 * Also suggests any additional custom attachments that might be needed.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRfpStore } from '@/stores/rfp'
import http from '@/services/http'

const emit = defineEmits<{
  (e: 'select-attachments', keys: string[]): void
}>()

const { locale } = useI18n()
const rfpStore = useRfpStore()

const isAnalyzing = ref(false)
const error = ref('')
const showResults = ref(false)
const recommendations = ref<Array<{
  key: string
  label: string
  reason: string
  required: boolean
}>>([])
const additionalSuggestions = ref<string[]>([])

/** Map of attachment keys to their Arabic labels */
const attachmentLabels: Record<string, string> = {
  technical_proposal_form: 'نموذج العرض الفني',
  financial_proposal_form: 'نموذج العرض المالي',
  bank_guarantee_form: 'نموذج الضمان البنكي',
  compliance_declaration: 'إقرار الالتزام بالشروط',
  conflict_of_interest: 'إقرار عدم تعارض المصالح',
  company_profile_form: 'نموذج ملف الشركة',
  experience_form: 'نموذج الخبرات السابقة',
  team_qualifications: 'مؤهلات فريق العمل',
  sla_template: 'نموذج اتفاقية مستوى الخدمة',
  nda_template: 'نموذج اتفاقية السرية',
}

async function analyzeAndRecommend() {
  isAnalyzing.value = true
  error.value = ''
  recommendations.value = []
  additionalSuggestions.value = []

  try {
    const basicInfo = rfpStore.formData.basicInfo
    const settings = rfpStore.formData.settings

    const attachmentsList = Object.entries(attachmentLabels)
      .map(([key, label]) => `${key}: ${label}`)
      .join('\n')

    const response = await http.post<{ isSuccess: boolean; generatedText: string; errorMessage?: string }>(
      '/v1/ai/text/assist',
      {
        action: 'custom',
        currentText: '',
        fieldName: 'المرفقات الإلزامية',
        fieldPurpose: 'تحديد المرفقات الإلزامية المطلوبة من المتنافسين',
        projectName: basicInfo.projectName || '',
        projectDescription: basicInfo.projectDescription || '',
        competitionType: basicInfo.competitionType || '',
        additionalContext: [
          `القيمة التقديرية: ${basicInfo.estimatedValue || 'غير محددة'}`,
          `ضمان بنكي مطلوب: ${settings.requireBankGuarantee ? 'نعم' : 'لا'}`,
          `طريقة التقييم: ${settings.evaluationMethod || 'غير محددة'}`,
        ].join('\n'),
        customPrompt: `بناءً على بيانات المشروع، حدد المرفقات الإلزامية المناسبة من القائمة التالية:

${attachmentsList}

لكل مرفق مناسب، أعد سطراً بالصيغة التالية:
المفتاح|الاسم|السبب|إلزامي_أم_اختياري

حيث إلزامي_أم_اختياري: required أو optional

ثم أضف سطراً فارغاً وبعده أي اقتراحات إضافية لمرفقات غير موجودة في القائمة (كل اقتراح في سطر يبدأ بـ "+"):

مثال:
technical_proposal_form|نموذج العرض الفني|ضروري لتقييم الحل التقني المقترح|required
+شهادة الجودة ISO 27001 (مطلوبة للمشاريع التقنية الحساسة)

أعد النتائج بالصيغة المطلوبة فقط.`,
        language: 'ar',
      },
      { timeout: 120_000 },
    )

    if (response.data.isSuccess && response.data.generatedText) {
      parseRecommendations(response.data.generatedText)
      showResults.value = true
    } else {
      error.value = response.data.errorMessage
        || (locale.value === 'ar' ? 'فشل في تحليل المرفقات المطلوبة' : 'Failed to analyze required attachments')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage
      || (locale.value === 'ar' ? 'حدث خطأ أثناء التحليل' : 'Error during analysis')
  } finally {
    isAnalyzing.value = false
  }
}

function parseRecommendations(text: string) {
  const lines = text.split('\n').filter(l => l.trim())

  for (const line of lines) {
    const trimmed = line.trim()

    // Additional suggestions start with "+"
    if (trimmed.startsWith('+')) {
      additionalSuggestions.value.push(trimmed.substring(1).trim())
      continue
    }

    // Parse pipe-delimited recommendations
    if (trimmed.includes('|')) {
      const parts = trimmed.split('|').map(p => p.trim())
      if (parts.length >= 3) {
        const key = parts[0]
        const label = parts[1] || attachmentLabels[key] || key
        const reason = parts[2]
        const isRequired = (parts[3] || '').toLowerCase() === 'required'

        if (attachmentLabels[key]) {
          recommendations.value.push({ key, label, reason, required: isRequired })
        }
      }
    }
  }
}

function applyRecommendations() {
  const keys = recommendations.value.map(r => r.key)
  emit('select-attachments', keys)
  showResults.value = false
}

const requiredCount = computed(() => recommendations.value.filter(r => r.required).length)
const optionalCount = computed(() => recommendations.value.filter(r => !r.required).length)
</script>

<template>
  <div class="ai-attachment-recommender mb-4">
    <!-- Recommend Button -->
    <button
      type="button"
      class="group flex items-center gap-2.5 rounded-xl border-2 border-dashed border-ai-300 bg-gradient-to-r from-ai-50 to-white px-5 py-3 text-sm font-semibold text-ai-600 transition-all duration-300 hover:border-ai-400 hover:shadow-lg hover:shadow-ai-100/50"
      :disabled="isAnalyzing"
      @click="analyzeAndRecommend"
    >
      <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100 transition-transform duration-300 group-hover:scale-110">
        <i
          class="pi text-lg text-ai-600"
          :class="isAnalyzing ? 'pi-spin pi-spinner' : 'pi-sparkles'"
        ></i>
      </div>
      <div class="text-start">
        <span class="block">
          {{ isAnalyzing
            ? (locale === 'ar' ? 'جارٍ تحليل المرفقات المطلوبة...' : 'Analyzing required attachments...')
            : (locale === 'ar' ? 'اقتراح المرفقات بالذكاء الاصطناعي' : 'AI Attachment Recommendations')
          }}
        </span>
        <span class="block text-[11px] font-normal text-ai-400">
          {{ locale === 'ar'
            ? 'تحديد المرفقات الإلزامية المناسبة بناءً على نوع المشروع'
            : 'Identify suitable mandatory attachments based on project type'
          }}
        </span>
      </div>
    </button>

    <!-- Error -->
    <div v-if="error" class="mt-3 rounded-lg border border-danger/20 bg-danger/5 p-3 text-xs text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ error }}
    </div>

    <!-- Results Modal -->
    <Transition name="fade">
      <div
        v-if="showResults && recommendations.length > 0"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm"
        @click.self="showResults = false"
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
                  {{ locale === 'ar' ? 'المرفقات المقترحة' : 'Recommended Attachments' }}
                </h3>
                <p class="text-xs text-secondary-500">
                  {{ requiredCount }} {{ locale === 'ar' ? 'إلزامي' : 'required' }}
                  ·
                  {{ optionalCount }} {{ locale === 'ar' ? 'اختياري' : 'optional' }}
                </p>
              </div>
              <button
                type="button"
                class="ms-auto flex h-8 w-8 items-center justify-center rounded-lg hover:bg-secondary-100"
                @click="showResults = false"
              >
                <i class="pi pi-times text-sm text-secondary-400"></i>
              </button>
            </div>
          </div>

          <!-- Recommendations List -->
          <div class="max-h-[50vh] overflow-auto p-5">
            <div class="space-y-2">
              <div
                v-for="(rec, idx) in recommendations"
                :key="idx"
                class="rounded-lg border p-3"
                :class="rec.required ? 'border-primary/20 bg-primary/5' : 'border-secondary-100 bg-white'"
              >
                <div class="flex items-start gap-2">
                  <i
                    class="mt-0.5 text-sm"
                    :class="rec.required ? 'pi pi-check-circle text-primary' : 'pi pi-info-circle text-secondary-400'"
                  ></i>
                  <div class="flex-1">
                    <div class="flex items-center gap-2">
                      <span class="text-sm font-semibold text-secondary-800">{{ rec.label }}</span>
                      <span
                        v-if="rec.required"
                        class="rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-bold text-primary"
                      >
                        {{ locale === 'ar' ? 'إلزامي' : 'Required' }}
                      </span>
                      <span
                        v-else
                        class="rounded-full bg-secondary-100 px-2 py-0.5 text-[10px] font-bold text-secondary-500"
                      >
                        {{ locale === 'ar' ? 'اختياري' : 'Optional' }}
                      </span>
                    </div>
                    <p class="mt-0.5 text-xs text-secondary-500">{{ rec.reason }}</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Additional Suggestions -->
            <div v-if="additionalSuggestions.length > 0" class="mt-4">
              <h4 class="mb-2 text-xs font-bold text-ai-600">
                <i class="pi pi-lightbulb me-1"></i>
                {{ locale === 'ar' ? 'اقتراحات إضافية' : 'Additional Suggestions' }}
              </h4>
              <ul class="space-y-1">
                <li
                  v-for="(suggestion, idx) in additionalSuggestions"
                  :key="idx"
                  class="flex items-start gap-2 text-xs text-secondary-600"
                >
                  <i class="pi pi-plus-circle mt-0.5 text-[10px] text-ai-400"></i>
                  {{ suggestion }}
                </li>
              </ul>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 bg-secondary-50 px-5 py-4">
            <button
              type="button"
              class="rounded-lg border border-secondary-200 bg-white px-4 py-2 text-sm text-secondary-600 hover:bg-secondary-50"
              @click="showResults = false"
            >
              {{ locale === 'ar' ? 'إلغاء' : 'Cancel' }}
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg bg-gradient-to-r from-ai-500 to-ai-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:from-ai-600 hover:to-ai-700"
              @click="applyRecommendations"
            >
              <i class="pi pi-check text-xs"></i>
              <span>{{ locale === 'ar' ? 'تطبيق التوصيات' : 'Apply Recommendations' }}</span>
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
