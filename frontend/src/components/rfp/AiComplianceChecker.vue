<script setup lang="ts">
/**
 * Compliance checker for the full RFP.
 *
 * This component intentionally uses deterministic business rules instead of
 * free-form AI output so that pass/fail states remain stable and actionable.
 */
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRfpStore } from '@/stores/rfp'

const { locale } = useI18n()
const rfpStore = useRfpStore()

type CheckStatus = 'pass' | 'warning' | 'fail'

interface ComplianceCheckItem {
  category: string
  status: CheckStatus
  message: string
  recommendation?: string
}

const isChecking = ref(false)
const error = ref('')
const showResults = ref(false)
const checkItems = ref<ComplianceCheckItem[]>([])

const passCount = computed(() => checkItems.value.filter(i => i.status === 'pass').length)
const warningCount = computed(() => checkItems.value.filter(i => i.status === 'warning').length)
const failCount = computed(() => checkItems.value.filter(i => i.status === 'fail').length)
const overallScore = computed(() => {
  if (checkItems.value.length === 0) return 0
  const total = checkItems.value.length
  return Math.round(((passCount.value + warningCount.value * 0.5) / total) * 100)
})

function createItem(
  category: string,
  status: CheckStatus,
  message: string,
  recommendation?: string,
): ComplianceCheckItem {
  return { category, status, message, recommendation }
}

function hasMeaningfulContent(value?: string | null): boolean {
  return !!value && value.trim().length > 0
}

function buildComplianceResults(): ComplianceCheckItem[] {
  const basicInfo = rfpStore.formData.basicInfo
  const settings = rfpStore.formData.settings
  const sections = rfpStore.formData.content.sections
  const boqItems = rfpStore.formData.boq.items
  const attachments = rfpStore.formData.attachments

  const results: ComplianceCheckItem[] = []

  const hasProjectName = hasMeaningfulContent(basicInfo.projectName)
  const hasCompetitionType = hasMeaningfulContent(basicInfo.competitionType)
  const hasDescription = hasMeaningfulContent(basicInfo.projectDescription)
  const hasDepartment = hasMeaningfulContent(basicInfo.department)
  const hasFiscalYear = hasMeaningfulContent(basicInfo.fiscalYear)
  const hasBookletNumber = hasMeaningfulContent(basicInfo.bookletNumber)
  const hasSubmissionDeadline = hasMeaningfulContent(basicInfo.submissionDeadline)

  const basicInfoMissing = [
    !hasProjectName ? 'اسم المشروع' : null,
    !hasCompetitionType ? 'نوع المنافسة' : null,
    !hasDescription ? 'وصف المشروع' : null,
    !hasDepartment ? 'الإدارة المسؤولة' : null,
    !hasFiscalYear ? 'السنة المالية' : null,
  ].filter(Boolean)

  if (basicInfoMissing.length === 0) {
    results.push(createItem(
      'البيانات الأساسية',
      'pass',
      'جميع الحقول الأساسية المطلوبة موجودة ومكتملة.',
    ))
  } else {
    results.push(createItem(
      'البيانات الأساسية',
      'fail',
      `البيانات الأساسية غير مكتملة. الحقول الناقصة: ${basicInfoMissing.join('، ')}.`,
      'أكمل الحقول الأساسية قبل المتابعة، خصوصاً الرقم المرجعي والتواريخ ووصف المشروع.',
    ))
  }

  const technicalWeight = Number(settings.technicalWeight || 0)
  const financialWeight = Number(settings.financialWeight || 0)
  const weightsTotal = technicalWeight + financialWeight
  const evaluationMethod = settings.evaluationMethod || ''
  const criteriaCount = settings.evaluationCriteria.length
  const criteriaWeightTotal = settings.evaluationCriteria.reduce((sum, criterion) => sum + Number(criterion.weight || 0), 0)

  const evaluationSettingsIssues: string[] = []
  if (!evaluationMethod) {
    evaluationSettingsIssues.push('طريقة التقييم غير محددة')
  }
  if (weightsTotal !== 100) {
    evaluationSettingsIssues.push(`مجموع الأوزان الفني والمالي يساوي ${weightsTotal}% بدلاً من 100%`)
  }
  if (evaluationMethod === 'weighted_criteria' && criteriaCount === 0) {
    evaluationSettingsIssues.push('طريقة المعايير الموزونة محددة بدون أي معايير تقييم')
  }
  if (evaluationMethod === 'weighted_criteria' && criteriaWeightTotal !== 100) {
    evaluationSettingsIssues.push(`مجموع أوزان معايير التقييم يساوي ${criteriaWeightTotal}% بدلاً من 100%`)
  }

  if (evaluationSettingsIssues.length === 0) {
    results.push(createItem(
      'إعدادات التقييم',
      'pass',
      'إعدادات التقييم متسقة، ومجاميع الأوزان صحيحة، ولا يوجد تعارض بين الطريقة والمعايير.',
    ))
  } else {
    results.push(createItem(
      'إعدادات التقييم',
      'fail',
      evaluationSettingsIssues.join('، '),
      'اضبط طريقة التقييم ثم تأكد من أن مجموع الوزن الفني والمالي يساوي 100%، وإذا كانت الطريقة هي المعايير الموزونة فليكن مجموع أوزان المعايير 100% أيضاً.',
    ))
  }

  if (criteriaCount === 0) {
    results.push(createItem(
      'معايير التقييم',
      'fail',
      'لا توجد أي معايير تقييم فنية معرفة حالياً.',
      'أضف معايير تقييم فنية واضحة وحدد لكل معيار وزنه ووصفه قبل الاعتماد.',
    ))
  } else if (criteriaWeightTotal !== 100) {
    results.push(createItem(
      'معايير التقييم',
      'fail',
      `تم تعريف ${criteriaCount} معيار/معايير ولكن مجموع الأوزان يساوي ${criteriaWeightTotal}% بدلاً من 100%.`,
      'راجع أوزان جميع المعايير بحيث يكون مجموعها 100% دون زيادة أو نقص.',
    ))
  } else {
    const criteriaMissingDescriptions = settings.evaluationCriteria.filter(c => !hasMeaningfulContent(c.description)).length
    results.push(createItem(
      'معايير التقييم',
      criteriaMissingDescriptions > 0 ? 'warning' : 'pass',
      criteriaMissingDescriptions > 0
        ? `المعايير معرفة ومجموع أوزانها صحيح، لكن يوجد ${criteriaMissingDescriptions} معيار/معايير بدون وصف توضيحي.`
        : `تم تعريف ${criteriaCount} معيار/معايير ومجموع الأوزان يساوي 100% بشكل صحيح.`,
      criteriaMissingDescriptions > 0
        ? 'أضف وصفاً مختصراً لكل معيار لتوضيح آلية التقييم للمنافسين ولجان الفحص.'
        : undefined,
    ))
  }

  const completedSections = sections.filter(section => hasMeaningfulContent(section.contentHtml) || hasMeaningfulContent(section.content)).length
  if (sections.length === 0) {
    results.push(createItem(
      'محتوى الكراسة',
      'fail',
      'لا توجد أي أقسام مضافة إلى محتوى الكراسة.',
      'أضف أقسام الكراسة الإلزامية أولاً ثم راجع محتوى كل قسم قبل الاعتماد.',
    ))
  } else if (completedSections === 0) {
    results.push(createItem(
      'محتوى الكراسة',
      'fail',
      'تم إنشاء أقسام للكراسة ولكن بدون أي محتوى فعلي حتى الآن.',
      'املأ محتوى الأقسام أو حمّل النصوص الفعلية قبل تقديم الكراسة للاعتماد.',
    ))
  } else if (completedSections < sections.length) {
    results.push(createItem(
      'محتوى الكراسة',
      'warning',
      `هناك ${completedSections} أقسام مكتملة من أصل ${sections.length} قسم/أقسام.`,
      'راجع الأقسام غير المكتملة واستكمل النصوص الأساسية قبل التقديم النهائي.',
    ))
  } else {
    results.push(createItem(
      'محتوى الكراسة',
      'pass',
      `جميع أقسام الكراسة تحتوي على محتوى فعلي (${sections.length} قسم/أقسام).`,
    ))
  }

  if (boqItems.length === 0) {
    results.push(createItem(
      'جدول الكميات',
      'warning',
      'لا يوجد أي بند في جدول الكميات حالياً.',
      'إذا كانت المنافسة تتطلب جدول كميات، فأضف البنود والكميات المطلوبة قبل الاعتماد.',
    ))
  } else {
    const missingBoqDescriptions = boqItems.filter(item => !hasMeaningfulContent(item.description)).length
    const invalidQuantities = boqItems.filter(item => Number(item.quantity || 0) <= 0).length
    const boqIssues: string[] = []

    if (missingBoqDescriptions > 0) boqIssues.push(`${missingBoqDescriptions} بند/بنود بدون وصف`)
    if (invalidQuantities > 0) boqIssues.push(`${invalidQuantities} بند/بنود بكميات غير صالحة`)

    results.push(createItem(
      'جدول الكميات',
      boqIssues.length > 0 ? 'warning' : 'pass',
      boqIssues.length > 0
        ? `تمت إضافة ${boqItems.length} بند/بنود، لكن توجد ملاحظات: ${boqIssues.join('، ')}.`
        : `تمت إضافة ${boqItems.length} بند/بنود إلى جدول الكميات مع بيانات كمية سليمة.`,
      boqIssues.length > 0
        ? 'استكمل أو صحح أوصاف البنود والكميات قبل التصدير أو الاعتماد.'
        : undefined,
    ))
  }

  const requiredAttachmentsCount = attachments.requiredAttachmentTypes?.length || 0
  results.push(createItem(
    'المرفقات الإلزامية',
    requiredAttachmentsCount > 0 ? 'pass' : 'warning',
    requiredAttachmentsCount > 0
      ? `تم تحديد ${requiredAttachmentsCount} نوع/أنواع من المرفقات الإلزامية.`
      : 'لم يتم تحديد أي مرفقات إلزامية حتى الآن.',
    requiredAttachmentsCount > 0
      ? undefined
      : 'حدد المرفقات المطلوبة من المتنافسين إذا كانت المنافسة تستلزم وثائق داعمة أو نماذج إلزامية.',
  ))

  const estimatedValue = Number(basicInfo.estimatedValue || 0)
  if (settings.requireBankGuarantee) {
    results.push(createItem(
      'الضمان البنكي',
      'pass',
      'تم تفعيل متطلب الضمان البنكي في إعدادات المنافسة.',
    ))
  } else {
    results.push(createItem(
      'الضمان البنكي',
      estimatedValue > 0 ? 'warning' : 'pass',
      estimatedValue > 0
        ? 'الضمان البنكي غير مفعل رغم وجود قيمة تقديرية للمنافسة.'
        : 'الضمان البنكي غير مفعل، ولا يمكن الجزم بضرورة تفعيله دون قيمة تقديرية واضحة.',
      estimatedValue > 0
        ? 'راجع ما إذا كانت هذه المنافسة تتطلب ضماناً بنكياً وفق السياسات الداخلية واللوائح المنظمة.'
        : undefined,
    ))
  }

  const inquiryPeriodDays = Number(basicInfo.inquiryPeriodDays || 0)
  results.push(createItem(
    'فترة الاستفسارات',
    inquiryPeriodDays >= 5 ? 'pass' : inquiryPeriodDays > 0 ? 'warning' : 'fail',
    inquiryPeriodDays >= 5
      ? `فترة الاستفسارات مضبوطة على ${inquiryPeriodDays} يوم/أيام.`
      : inquiryPeriodDays > 0
        ? `فترة الاستفسارات الحالية قصيرة (${inquiryPeriodDays} يوم/أيام).`
        : 'فترة الاستفسارات غير محددة.',
    inquiryPeriodDays >= 5
      ? undefined
      : 'اضبط فترة استفسارات كافية وواضحة قبل نشر المنافسة أو اعتمادها.',
  ))

  const parsedBookletIssueDate = basicInfo.bookletIssueDate ? new Date(basicInfo.bookletIssueDate) : null
  const parsedInquiriesStartDate = basicInfo.inquiriesStartDate ? new Date(basicInfo.inquiriesStartDate) : null
  const parsedOffersStartDate = basicInfo.offersStartDate ? new Date(basicInfo.offersStartDate) : null
  const parsedSubmissionDeadline = hasSubmissionDeadline ? new Date(basicInfo.submissionDeadline) : null
  const parsedExpectedAwardDate = basicInfo.expectedAwardDate ? new Date(basicInfo.expectedAwardDate) : null
  const parsedWorkStartDate = basicInfo.workStartDate ? new Date(basicInfo.workStartDate) : null
  const dateIssues: string[] = []

  if (parsedBookletIssueDate && parsedInquiriesStartDate && parsedInquiriesStartDate < parsedBookletIssueDate) {
    dateIssues.push('تاريخ إرسال الاستفسارات يسبق تاريخ طرح الكراسة')
  }
  if (parsedInquiriesStartDate && parsedOffersStartDate && parsedOffersStartDate < parsedInquiriesStartDate) {
    dateIssues.push('تاريخ تقديم العروض يسبق تاريخ إرسال الاستفسارات')
  }
  if (parsedOffersStartDate && parsedSubmissionDeadline && parsedSubmissionDeadline <= parsedOffersStartDate) {
    dateIssues.push('آخر موعد لتقديم العروض يجب أن يكون بعد تاريخ تقديم العروض')
  }
  if (parsedSubmissionDeadline && parsedExpectedAwardDate && parsedExpectedAwardDate <= parsedSubmissionDeadline) {
    dateIssues.push('التاريخ المتوقع للترسية يجب أن يكون بعد آخر موعد لتقديم العروض')
  }
  if (parsedExpectedAwardDate && parsedWorkStartDate && parsedWorkStartDate <= parsedExpectedAwardDate) {
    dateIssues.push('تاريخ بدء الأعمال يجب أن يكون بعد التاريخ المتوقع للترسية')
  }


  if (dateIssues.length > 0) {
    results.push(createItem(
      'التواريخ',
      'fail',
      dateIssues.join('، '),
      'راجع تسلسل التواريخ بحيث يحترم تسلسل طرح الكراسة ثم الاستفسارات ثم تقديم العروض ثم آخر موعد للتقديم ثم الترسية ثم بدء الأعمال.',
    ))
  } else {
    results.push(createItem(
      'التواريخ',
      hasSubmissionDeadline || hasBookletNumber ? 'pass' : 'warning',
      hasSubmissionDeadline || hasBookletNumber
        ? 'الحقول الزمنية المُدخلة متسقة مع التسلسل الزمني المطلوب.'
        : 'لم يتم إدخال أي تواريخ بعد، وهذا مسموح في المسودة الحالية.',
      hasSubmissionDeadline || hasBookletNumber
        ? undefined
        : 'أدخل التواريخ الأساسية تدريجيًا عند اكتمال الجدول الزمني للمنافسة.',
    ))
  }

  const failItems = results.filter(item => item.status === 'fail').length
  const warningItems = results.filter(item => item.status === 'warning').length
  if (failItems === 0 && warningItems === 0) {
    results.push(createItem(
      'الامتثال العام',
      'pass',
      'لا توجد ملاحظات جوهرية حالياً، والكراسة جاهزة مبدئياً للمرحلة التالية.',
    ))
  } else if (failItems === 0) {
    results.push(createItem(
      'الامتثال العام',
      'warning',
      `لا توجد حالات فشل، لكن توجد ${warningItems} ملاحظات تحتاج مراجعة قبل الاعتماد النهائي.`,
      'راجع التحذيرات المفتوحة وحسّن جودة الكراسة قبل التقديم أو النشر النهائي.',
    ))
  } else {
    results.push(createItem(
      'الامتثال العام',
      'fail',
      `يوجد ${failItems} حالة فشل و${warningItems} حالة تحذير تمنع اعتبار الكراسة ممتثلة بشكل كامل.`,
      'عالج حالات الفشل أولاً، ثم أعد تشغيل الفحص للتأكد من استقرار جميع النقاط الحاكمة.',
    ))
  }

  return results
}

async function runComplianceCheck() {
  isChecking.value = true
  error.value = ''

  try {
    checkItems.value = buildComplianceResults()
    showResults.value = true
  } catch (err: unknown) {
    error.value = err instanceof Error
      ? err.message
      : (locale.value === 'ar' ? 'حدث خطأ أثناء فحص الامتثال' : 'Error during compliance check')
  } finally {
    isChecking.value = false
  }
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
            : (locale === 'ar' ? 'فحص الامتثال' : 'Compliance Check')
          }}
        </span>
        <span class="block text-[11px] font-normal text-ai-400">
          {{ locale === 'ar'
            ? 'التحقق الحتمي من اكتمال واتساق الكراسة قبل الاعتماد'
            : 'Deterministic validation of booklet completeness and consistency'
          }}
        </span>
      </div>
    </button>

    <div v-if="error" class="mt-3 rounded-lg border border-danger/20 bg-danger/5 p-3 text-xs text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ error }}
    </div>

    <Transition name="fade">
      <div
        v-if="showResults && checkItems.length > 0"
        class="mt-4 overflow-hidden rounded-xl border border-ai-200 bg-white shadow-sm"
      >
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

            <div class="text-center">
              <span class="text-2xl font-bold" :class="scoreColor">{{ overallScore }}%</span>
              <p class="text-[10px] text-secondary-400">
                {{ locale === 'ar' ? 'درجة الامتثال' : 'Compliance Score' }}
              </p>
            </div>
          </div>

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
