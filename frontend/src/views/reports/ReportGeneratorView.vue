<script setup lang="ts">
/**
 * ReportGeneratorView - Export-focused Report Generator (TASK-1001)
 *
 * Complements the existing ReportsView (analytics dashboard) with:
 * - Specific report type selection
 * - PDF/Excel export with dynamic branding
 * - Report history
 */
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPost } from '@/services/http'

const { t } = useI18n()

interface ReportType {
  key: string
  label: string
  icon: string
  description: string
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
  { key: 'competitionSummary', label: 'Competition Summary', icon: 'pi-briefcase', description: 'Overview of all competitions with status and timelines', color: 'bg-primary-50 text-primary' },
  { key: 'compliance', label: 'Compliance Report', icon: 'pi-shield', description: 'Regulatory compliance status and audit trail', color: 'bg-warning-50 text-warning' },
  { key: 'financialAnalysis', label: 'Financial Analysis', icon: 'pi-dollar', description: 'Financial analysis of competitions and offers', color: 'bg-info-50 text-info' },
  { key: 'offerComparison', label: 'Offer Comparison', icon: 'pi-table', description: 'Side-by-side comparison of submitted offers', color: 'bg-ai-50 text-ai' },
  { key: 'timeline', label: 'Timeline Report', icon: 'pi-clock', description: 'Timeline analysis with SLA compliance tracking', color: 'bg-danger-50 text-danger' },
]

const isGenerating = ref(false)
const selectedReport = ref<string | null>(null)
const recentReports = ref<GeneratedReport[]>([])
const dateFrom = ref('')
const dateTo = ref('')
const exportFormat = ref<'pdf' | 'excel'>('pdf')

async function loadRecentReports(): Promise<void> {
  try {
    const data = await httpGet<{ items: GeneratedReport[] }>('/v1/reports/generated')
    recentReports.value = data.items
  } catch (err) {
    console.error('Failed to load recent reports:', err)
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
  } catch (err) {
    console.error('Failed to generate report:', err)
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
      <h1 class="page-title">Export Reports</h1>
      <p class="page-description">Generate and download formatted reports</p>
    </div>

    <!-- Filters -->
    <div class="card !p-4">
      <div class="flex flex-col gap-3 sm:flex-row sm:items-end">
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">From</label>
          <input v-model="dateFrom" type="date" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">To</label>
          <input v-model="dateTo" type="date" class="input text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-secondary-600">Format</label>
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
        <div class="flex items-start gap-3 mb-3">
          <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl" :class="rt.color">
            <i class="pi text-lg" :class="rt.icon"></i>
          </div>
          <div>
            <h3 class="text-sm font-bold text-secondary-800">{{ rt.label }}</h3>
            <p class="mt-1 text-xs text-secondary-500">{{ rt.description }}</p>
          </div>
        </div>
        <button
          class="btn-primary btn-sm w-full"
          :disabled="isGenerating && selectedReport === rt.key"
          @click="generateReport(rt.key)"
        >
          <i class="pi" :class="isGenerating && selectedReport === rt.key ? 'pi-spin pi-spinner' : 'pi-download'"></i>
          {{ isGenerating && selectedReport === rt.key ? 'Generating...' : 'Generate' }}
        </button>
      </div>
    </div>

    <!-- Recent Reports -->
    <div v-if="recentReports.length > 0" class="card">
      <h3 class="mb-4 text-sm font-bold text-secondary">Recent Exports</h3>
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
