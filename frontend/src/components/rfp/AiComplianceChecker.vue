<script setup lang="ts">
/**
 * AiComplianceChecker Component
 *
 * Performs an AI-powered compliance check on the entire RFP
 * before submission. Checks against Saudi government procurement
 * regulations and best practices.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRfpStore } from '@/stores/rfp'
import http from '@/services/http'

const { locale } = useI18n()
const rfpStore = useRfpStore()

const isChecking = ref(false)
const error = ref('')
const showResults = ref(false)
const checkItems = ref<Array<{
  category: string
  status: 'pass' | 'warning' | 'fail'
  message: string
  recommendation?: string
}>>([])

const passCount = computed(() => checkItems.value.filter(i => i.status === 'pass').length)
const warningCount = computed(() => checkItems.value.filter(i => i.status === 'warning').length)
const failCount = computed(() => checkItems.value.filter(i => i.status === 'fail').length)
const overallScore = computed(() => {
  if (checkItems.value.length === 0) return 0
  const total = checkItems.value.length
  return Math.round(((passCount.value + warningCount.value * 0.5) / total) * 100)
})

async function runComplianceCheck() {
  isChecking.value = true
  error.value = ''
  checkItems.value = []

  try {
    // Build comprehensive context from all RFP data
    const basicInfo = rfpStore.formData.basicInfo
    const settings = rfpStore.formData.settings
    const sections = rfpStore.formData.content.sections
    const boqItems = rfpStore.formData.boq.items
    const attachments = rfpStore.formData.attachments

    const rfpSummary = [
      `اسم المشروع: ${basicInfo.projectName || 'غير محدد'}`,
      `نوع المنافسة: ${basicInfo.competitionType || 'غير محدد'}`,
      `القيمة التقديرية: ${basicInfo.estimatedValue || 'غير محددة'}`,
      `تاريخ البداية: ${basicInfo.startDate || 'غير محدد'}`,
      `تاريخ النهاية: ${basicInfo.endDate || 'غير محدد'}`,
      `طريقة التقييم: ${settings.evaluationMethod || 'غير محددة'}`,
      `الوزن الفني: ${settings.technicalWeight}%`,
      `الوزن المالي: ${settings.financialWeight}%`,
      `الحد الأدنى للدرجة الفنية: ${settings.minimumTechnicalScore}`,
      `عدد معايير التقييم: ${settings.evaluationCriteria.length}`,
      `مجموع أوزان المعايير: ${settings.evaluationCriteria.reduce((s, c) => s + c.weight, 0)}%`,
      `عدد أقسام المحتوى: ${sections.length}`,
      `عناوين الأقسام: ${sections.map(s => s.title).join('، ')}`,
      `عدد بنود جدول الكميات: ${boqItems.length}`,
      `المرفقات الإلزامية المختارة: ${attachments.requiredAttachmentTypes?.length || 0}`,
      `ضمان بنكي مطلوب: ${settings.requireBankGuarantee ? 'نعم' : 'لا'}`,
      `فترة الاستفسارات: ${settings.inquiryPeriodDays} يوم`,
    ].join('\n')

    const response = await http.post<{ isSuccess: boolean; generatedText: string; errorMessage?: string }>(
      '/v1/ai/text/assist',
      {
        action: 'custom',
        currentText: rfpSummary,
        fieldName: 'فحص الامتثال',
        fieldPurpose: 'التحقق من امتثال كراسة الشروط والمواصفات لنظام المنافسات والمشتريات الحكومية السعودي',
        projectName: basicInfo.projectName || '',
        projectDescription: basicInfo.projectDescription || '',
        competitionType: basicInfo.competitionType || '',
        additionalContext: '',
        customPrompt: `أنت مدقق امتثال متخصص في نظام المنافسات والمشتريات الحكومية السعودي.

قم بفحص بيانات كراسة الشروط والمواصفات التالية وأعطِ تقييماً شاملاً.

لكل نقطة فحص، أعطِ النتيجة بالصيغة التالية (كل نقطة في سطر منفصل):
الفئة|الحالة|الرسالة|التوصية

حيث الحالة: pass أو warning أو fail

النقاط المطلوب فحصها:
1. اكتمال البيانات الأساسية (الاسم، النوع، القيمة، التواريخ)
2. صحة إعدادات التقييم (مجموع الأوزان = 100%)
3. كفاية معايير التقييم (عدد ونوع المعايير)
4. اكتمال محتوى الكراسة (الأقسام الإلزامية)
5. جدول الكميات (وجود بنود وأسعار)
6. المرفقات الإلزامية
7. الضمان البنكي (حسب قيمة المنافسة)
8. فترة الاستفسارات (كافية حسب النظام)
9. التواريخ (منطقية ومتسلسلة)
10. الامتثال العام لنظام المنافسات والمشتريات الحكومية

أعد النتائج بالصيغة المطلوبة فقط بدون أي نص إضافي.`,
        language: 'ar',
      },
      { timeout: 120_000 },
    )

    if (response.data.isSuccess && response.data.generatedText) {
      checkItems.value = parseComplianceResults(response.data.generatedText)
      showResults.value = true
    } else {
      error.value = response.data.errorMessage
        || (locale.value === 'ar' ? 'فشل في إجراء فحص الامتثال' : 'Compliance check failed')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage
      || (locale.value === 'ar' ? 'حدث خطأ أثناء فحص الامتثال' : 'Error during compliance check')
  } finally {
    isChecking.value = false
  }
}

function parseComplianceResults(text: string): Array<{
  category: string
  status: 'pass' | 'warning' | 'fail'
  message: string
  recommendation?: string
}> {
  const lines = text.split('\n').filter(line => line.trim() && line.includes('|'))
  const results: Array<{
    category: string
    status: 'pass' | 'warning' | 'fail'
    message: string
    recommendation?: string
  }> = []

  for (const line of lines) {
    const parts = line.split('|').map(p => p.trim())
    if (parts.length >= 3) {
      const category = parts[0]
      const statusRaw = parts[1].toLowerCase()
      const status: 'pass' | 'warning' | 'fail' =
        statusRaw === 'pass' ? 'pass' :
        statusRaw === 'warning' ? 'warning' : 'fail'
      const message = parts[2]
      const recommendation = parts[3] || undefined

      results.push({ category, status, message, recommendation })
    }
  }

  return results
}

const statusIcon = (status: string) => {
  switch (status) {
    case 'pass': return 'pi pi-check-circle text-success'
    case 'warning': return 'pi pi-exclamation-triangle text-warning'
    case 'fail': return 'pi pi-times-circle text-danger'
    default: return 'pi pi-circle text-tertiary'
  }
}

const statusBg = (status: string) => {
  switch (status) {
    case 'pass': return 'bg-success/5 border-success/20'
    case 'warning': return 'bg-warning/5 border-warning/20'
    case 'fail': return 'bg-danger/5 border-danger/20'
    default: return 'bg-secondary-50 border-secondary-200'
  }
}

const scoreColor = computed(() => {
  if (overallScore.value >= 80) return 'text-success'
  if (overallScore.value >= 60) return 'text-warning'
  return 'text-danger'
})
</script>

<template>
  <div class="ai-compliance-checker">
    <!-- Check Button -->
    <button
      type="button"
      class="group flex w-full items-center gap-3 rounded-xl border-2 border-dashed border-ai-300 bg-gradient-to-r from-ai-50 to-white px-5 py-4 text-sm font-semibold text-ai-600 transition-all duration-300 hover:border-ai-400 hover:shadow-lg hover:shadow-ai-100/50"
      :disabled="isChecking"
      @click="runComplianceCheck"
    >
      <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-ai-100 transition-transform duration-300 group-hover:scale-110">
        <i
          class="pi text-xl text-ai-600"
          :class="isChecking ? 'pi-spin pi-spinner' : 'pi-shield'"
        ></i>
      </div>
      <div class="text-start">
        <span class="block text-base">
          {{ isChecking
            ? (locale === 'ar' ? 'جارٍ فحص الامتثال...' : 'Running compliance check...')
            : (locale === 'ar' ? 'فحص الامتثال بالذكاء الاصطناعي' : 'AI Compliance Check')
          }}
        </span>
        <span class="block text-[11px] font-normal text-ai-400">
          {{ locale === 'ar'
            ? 'التحقق من امتثال الكراسة لنظام المنافسات والمشتريات الحكومية'
            : 'Verify RFP compliance with government procurement regulations'
          }}
        </span>
      </div>
    </button>

    <!-- Error -->
    <div v-if="error" class="mt-3 rounded-lg border border-danger/20 bg-danger/5 p-3 text-xs text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ error }}
    </div>

    <!-- Results Panel -->
    <Transition name="fade">
      <div
        v-if="showResults && checkItems.length > 0"
        class="mt-4 overflow-hidden rounded-xl border border-ai-200 bg-white shadow-sm"
      >
        <!-- Results Header -->
        <div class="border-b border-secondary-100 bg-gradient-to-r from-ai-50 to-white p-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100">
                <i class="pi pi-shield text-lg text-ai-600"></i>
              </div>
              <div>
                <h3 class="text-sm font-bold text-secondary-800">
                  {{ locale === 'ar' ? 'نتائج فحص الامتثال' : 'Compliance Check Results' }}
                </h3>
                <p class="text-xs text-secondary-500">
                  {{ checkItems.length }} {{ locale === 'ar' ? 'نقطة فحص' : 'check points' }}
                </p>
              </div>
            </div>

            <!-- Score Badge -->
            <div class="text-center">
              <span class="text-2xl font-bold" :class="scoreColor">{{ overallScore }}%</span>
              <p class="text-[10px] text-secondary-400">
                {{ locale === 'ar' ? 'درجة الامتثال' : 'Compliance Score' }}
              </p>
            </div>
          </div>

          <!-- Summary Stats -->
          <div class="mt-3 flex gap-4">
            <span class="flex items-center gap-1 text-xs text-success">
              <i class="pi pi-check-circle text-[10px]"></i>
              {{ passCount }} {{ locale === 'ar' ? 'ناجح' : 'passed' }}
            </span>
            <span class="flex items-center gap-1 text-xs text-warning">
              <i class="pi pi-exclamation-triangle text-[10px]"></i>
              {{ warningCount }} {{ locale === 'ar' ? 'تحذير' : 'warnings' }}
            </span>
            <span class="flex items-center gap-1 text-xs text-danger">
              <i class="pi pi-times-circle text-[10px]"></i>
              {{ failCount }} {{ locale === 'ar' ? 'فشل' : 'failed' }}
            </span>
          </div>
        </div>

        <!-- Check Items -->
        <div class="max-h-[400px] overflow-auto p-4">
          <div class="space-y-2">
            <div
              v-for="(item, idx) in checkItems"
              :key="idx"
              class="rounded-lg border p-3"
              :class="statusBg(item.status)"
            >
              <div class="flex items-start gap-2">
                <i :class="statusIcon(item.status)" class="mt-0.5 text-sm"></i>
                <div class="flex-1">
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-bold text-secondary-700">{{ item.category }}</span>
                  </div>
                  <p class="mt-0.5 text-xs text-secondary-600">{{ item.message }}</p>
                  <p v-if="item.recommendation" class="mt-1 text-[11px] text-ai-600">
                    <i class="pi pi-lightbulb me-1 text-[10px]"></i>
                    {{ item.recommendation }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Close Button -->
        <div class="border-t border-secondary-100 bg-secondary-50 px-4 py-3 text-end">
          <button
            type="button"
            class="rounded-lg border border-secondary-200 bg-white px-4 py-1.5 text-xs text-secondary-600 hover:bg-secondary-50"
            @click="showResults = false"
          >
            {{ locale === 'ar' ? 'إغلاق' : 'Close' }}
          </button>
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
