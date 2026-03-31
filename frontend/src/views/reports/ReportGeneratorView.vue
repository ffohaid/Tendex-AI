<script setup lang="ts">
/**
 * ReportGeneratorView - Export-focused Report Generator
 *
 * Complements the existing ReportsView (analytics dashboard) with:
 * - Specific report type selection
 * - PDF/Excel export with dynamic branding
 * - Report history
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost } from '@/services/http'

const { t, locale } = useI18n()

interface ReportType {
  key: string
  labelAr: string
  labelEn: string
  icon: string
  descriptionAr: string
  descriptionEn: string
  color: string
}

interface GeneratedReport {
  id: string
  type: string
  title: string
  generatedAt: string
  format: 'pdf' | 'excel'
  downloadUrl: string
  size: number
}

const reportTypes: ReportType[] = [
  { key: 'competitionSummary', labelAr: 'ملخص المنافسات', labelEn: 'Competition Summary', icon: 'pi-briefcase', descriptionAr: 'نظرة عامة على جميع المنافسات مع الحالات والجداول الزمنية', descriptionEn: 'Overview of all competitions with status and timelines', color: 'bg-primary-50 text-primary' },
  { key: 'compliance', labelAr: 'تقرير الامتثال', labelEn: 'Compliance Report', icon: 'pi-shield', descriptionAr: 'حالة الامتثال التنظيمي وسجل التدقيق', descriptionEn: 'Regulatory compliance status and audit trail', color: 'bg-warning-50 text-warning' },
  { key: 'financialAnalysis', labelAr: 'التحليل المالي', labelEn: 'Financial Analysis', icon: 'pi-dollar', descriptionAr: 'تحليل مالي للمنافسات والعروض', descriptionEn: 'Financial analysis of competitions and offers', color: 'bg-info-50 text-info' },
  { key: 'offerComparison', labelAr: 'مقارنة العروض', labelEn: 'Offer Comparison', icon: 'pi-table', descriptionAr: 'مقارنة جنباً إلى جنب للعروض المقدمة', descriptionEn: 'Side-by-side comparison of submitted offers', color: 'bg-ai-50 text-ai' },
  { key: 'timeline', labelAr: 'تقرير الجدول الزمني', labelEn: 'Timeline Report', icon: 'pi-clock', descriptionAr: 'تحليل الجدول الزمني مع تتبع الالتزام بمدد الإنجاز', descriptionEn: 'Timeline analysis with SLA compliance tracking', color: 'bg-danger-50 text-danger' },
]

const isGenerating = ref(false)
const selectedReport = ref<string | null>(null)
const recentReports = ref<GeneratedReport[]>([])
const dateFrom = ref('')
const dateTo = ref('')
const exportFormat = ref<'pdf' | 'excel'>('pdf')

const isAr = computed(() => locale.value === 'ar')

async function loadRecentReports(): Promise<void> {
  try {
    const data = await httpGet<{ items: GeneratedReport[] }>('/v1/reports/generated')
    recentReports.value = data.items
  } catch {
    console.warn('Reports API not available')
  }
}

async function generateReport(type: string): Promise<void> {
  isGenerating.value = true
  selectedReport.value = type
  try {
    const result = await httpPost<GeneratedReport>('/v1/reports/generate', {
      type,
      dateFrom: dateFrom.value || undefined,
      dateTo: dateTo.value || undefined,
      format: exportFormat.value,
    })
    recentReports.value.unshift(result)
    window.open(result.downloadUrl, '_blank')
  } catch {
    console.error('Failed to generate report')
  } finally {
    isGenerating.value = false
    selectedReport.value = null
  }
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

onMounted(() => {
  loadRecentReports()
})
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="page-title">{{ t('reportGenerator.title') }}</h1>
      <p class="page-description">{{ t('reportGenerator.description') }}</p>
    </div>

    <!-- Filters -->
    <div class="card !p-4">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-end">
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">
            {{ isAr ? 'من' : 'From' }}
          </label>
          <input v-model="dateFrom" type="date" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">
            {{ isAr ? 'إلى' : 'To' }}
          </label>
          <input v-model="dateTo" type="date" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">
            {{ isAr ? 'التنسيق' : 'Format' }}
          </label>
          <select v-model="exportFormat" class="input text-sm">
            <option value="pdf">PDF</option>
            <option value="excel">Excel</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Report Types -->
    <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      <div v-for="rt in reportTypes" :key="rt.key" class="card-interactive">
        <div class="mb-3 flex items-start gap-3">
          <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl" :class="rt.color">
            <i class="pi text-lg" :class="rt.icon"></i>
          </div>
          <div>
            <h3 class="text-sm font-bold text-secondary-800">
              {{ isAr ? rt.labelAr : rt.labelEn }}
            </h3>
            <p class="mt-1 text-xs text-secondary-500">
              {{ isAr ? rt.descriptionAr : rt.descriptionEn }}
            </p>
          </div>
        </div>
        <button
          class="btn-primary btn-sm w-full"
          :disabled="isGenerating && selectedReport === rt.key"
          @click="generateReport(rt.key)"
        >
          <i class="pi" :class="isGenerating && selectedReport === rt.key ? 'pi-spin pi-spinner' : 'pi-download'"></i>
          {{ isGenerating && selectedReport === rt.key
            ? (isAr ? 'جارٍ الإنشاء...' : 'Generating...')
            : (isAr ? 'إنشاء التقرير' : 'Generate') }}
        </button>
      </div>
    </div>

    <!-- Recent Reports -->
    <div v-if="recentReports.length > 0" class="card">
      <h3 class="mb-4 text-sm font-bold text-secondary">
        {{ isAr ? 'التقارير الأخيرة' : 'Recent Exports' }}
      </h3>
      <div class="space-y-2">
        <div v-for="report in recentReports" :key="report.id" class="flex items-center gap-3 rounded-xl border border-secondary-100 p-3">
          <div class="flex h-9 w-9 items-center justify-center rounded-lg" :class="report.format === 'pdf' ? 'bg-danger-50' : 'bg-success-50'">
            <i class="pi text-sm" :class="report.format === 'pdf' ? 'pi-file-pdf text-danger' : 'pi-file-excel text-success'"></i>
          </div>
          <div class="min-w-0 flex-1">
            <p class="text-xs font-semibold text-secondary-800">{{ report.title }}</p>
            <p class="text-[10px] text-secondary-400">{{ formatFileSize(report.size) }}</p>
          </div>
          <a :href="report.downloadUrl" target="_blank" class="btn-ghost btn-sm"><i class="pi pi-download"></i></a>
        </div>
      </div>
    </div>
  </div>
</template>
