<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import OfficialBookletDocument from '@/components/rfp/OfficialBookletDocument.vue'
import { httpGet } from '@/services/http'
import { fetchRfpById } from '@/services/rfpService'
import type { RfpFormData } from '@/types/rfp'
import { formatCurrency } from '@/utils/numbers'

interface TemplateBookletBlock {
  id: string
  sortOrder: number
  originalContent: string
  contentHtml: string
  colorType: 'fixed' | 'editable' | 'example' | 'guidance'
  isHeading: boolean
}

interface TemplateBookletSection {
  id: string
  competitionSectionId: string | null
  titleAr: string
  sortOrder: number
  isMainSection: boolean
  blocks: TemplateBookletBlock[]
}

interface TemplateBookletData {
  competitionId: string
  templateId: string
  templateNameAr: string
  projectNameAr: string
  projectNameEn: string
  description: string | null
  sections: TemplateBookletSection[]
}

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const rfpData = ref<RfpFormData | null>(null)
const templateBooklet = ref<TemplateBookletData | null>(null)
const isLoading = ref(true)
const error = ref('')

const rfpId = computed(() => route.params.id as string)
const isTemplateBasedExport = computed(() => Boolean(rfpData.value?.content.templateId && templateBooklet.value))

const officialBookletSections = computed(() => {
  if (!templateBooklet.value) return []

  return templateBooklet.value.sections.map(section => ({
    id: section.id,
    titleAr: section.titleAr,
    sortOrder: section.sortOrder,
    isMainSection: section.isMainSection,
    blocks: section.blocks.map(block => ({
      id: block.id,
      sortOrder: block.sortOrder,
      html: block.contentHtml?.trim() || block.originalContent || '',
      colorType: block.colorType,
      isHeading: block.isHeading,
    })),
  }))
})

const officialBookletMeta = computed(() => ({
  projectNameAr: templateBooklet.value?.projectNameAr || rfpData.value?.basicInfo.projectName || '',
  projectNameEn: templateBooklet.value?.projectNameEn || '',
  templateNameAr: templateBooklet.value?.templateNameAr || t('rfp.export.bookletTitle', 'كراسة الشروط والمواصفات'),
  referenceNumber: rfpData.value?.basicInfo.referenceNumber || '',
  issueDate: formatDate(rfpData.value?.basicInfo.submissionDeadline || rfpData.value?.basicInfo.startDate || ''),
  administrationName: rfpData.value?.basicInfo.department || '',
  organizationName: 'المملكة العربية السعودية',
  versionLabel: 'الأولى',
}))

async function loadTemplateBooklet(): Promise<void> {
  templateBooklet.value = await httpGet<TemplateBookletData>(`/v1/booklet-templates/competition/${rfpId.value}/blocks`)
}

async function loadRfp() {
  isLoading.value = true
  error.value = ''
  templateBooklet.value = null

  const response = await fetchRfpById(rfpId.value)

  if (response.success && response.data) {
    rfpData.value = response.data

    if (response.data.content.templateId) {
      try {
        await loadTemplateBooklet()
      } catch (templateError) {
        console.warn('Failed to load template booklet export data, falling back to generic export view.', templateError)
      }
    }
  } else {
    error.value = response.message || t('rfp.errors.loadFailed')
  }

  isLoading.value = false
}

function printBooklet() {
  window.print()
}

function goBack() {
  router.push({ name: 'rfp-list' })
}

function formatDate(dateStr: string): string {
  if (!dateStr) return '-'

  return new Date(dateStr).toLocaleDateString('en-SA', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

function getCompetitionTypeLabel(type: string): string {
  const labels: Record<string, string> = {
    public_tender: t('rfp.competitionTypes.publicTender'),
    limited_tender: t('rfp.competitionTypes.limitedTender'),
    direct_purchase: t('rfp.competitionTypes.directPurchase'),
    framework_agreement: t('rfp.competitionTypes.frameworkAgreement'),
    reverse_auction: t('rfp.competitionTypes.reverseAuction'),
  }

  return labels[type] || type
}

onMounted(() => {
  loadRfp()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="sticky top-0 z-10 border-b border-surface-dim bg-white px-4 py-3 print:hidden">
      <div class="mx-auto flex max-w-5xl items-center justify-between">
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
          @click="goBack"
        >
          <i class="pi pi-arrow-right rotate-180 rtl:rotate-0"></i>
          {{ t('common.back') }}
        </button>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
          :disabled="isLoading || !!error"
          @click="printBooklet"
        >
          <i class="pi pi-download text-sm"></i>
          {{ $t('rfp.actions.exportPdf', 'تحميل كـ PDF') }}
        </button>
      </div>
    </div>

    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <div class="text-center">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        <p class="mt-3 text-sm text-tertiary">{{ t('common.loading') }}</p>
      </div>
    </div>

    <div v-else-if="error" class="mx-auto max-w-5xl px-4 py-20 text-center">
      <i class="pi pi-exclamation-triangle text-4xl text-danger"></i>
      <p class="mt-3 text-sm text-danger">{{ error }}</p>
    </div>

    <div v-else-if="isTemplateBasedExport" class="px-4 py-8 print:px-0 print:py-0">
      <OfficialBookletDocument
        :meta="officialBookletMeta"
        :sections="officialBookletSections"
        :include-guidance="false"
        :include-example-blocks="true"
      />
    </div>

    <div v-else-if="rfpData" class="mx-auto max-w-5xl px-4 py-8 print:px-0 print:py-0">
      <div class="rounded-lg border border-surface-dim bg-white p-8 shadow-sm print:border-0 print:shadow-none">
        <div class="mb-10 text-center">
          <h1 class="text-3xl font-bold text-secondary">
            {{ $t('rfp.export.bookletTitle', 'كراسة الشروط والمواصفات') }}
          </h1>
          <div class="mx-auto mt-4 h-1 w-24 rounded bg-primary"></div>
          <h2 class="mt-6 text-xl font-bold text-primary">
            {{ rfpData.basicInfo.projectName }}
          </h2>
          <p v-if="rfpData.basicInfo.referenceNumber" class="mt-2 text-sm text-tertiary">
            {{ $t('rfp.fields.referenceNumber', 'الرقم المرجعي') }}: {{ rfpData.basicInfo.referenceNumber }}
          </p>
        </div>

        <div class="mb-8">
          <h3 class="mb-4 border-b-2 border-primary pb-2 text-lg font-bold text-secondary">
            {{ t('rfp.steps.basicInfo') }}
          </h3>
          <table class="w-full border-collapse text-sm">
            <tbody>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary" style="width: 35%">
                  {{ t('rfp.fields.projectName') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ rfpData.basicInfo.projectName }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.competitionType') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ getCompetitionTypeLabel(rfpData.basicInfo.competitionType) }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.estimatedValue') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ formatCurrency(Number(rfpData.basicInfo.estimatedValue)) }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.department') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ rfpData.basicInfo.department || '-' }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.fiscalYear') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ rfpData.basicInfo.fiscalYear || '-' }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.startDate') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ formatDate(rfpData.basicInfo.startDate) }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.endDate') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ formatDate(rfpData.basicInfo.endDate) }}</td>
              </tr>
              <tr class="border-b border-surface-dim">
                <td class="bg-surface-muted px-4 py-3 font-medium text-secondary">
                  {{ t('rfp.fields.submissionDeadline') }}
                </td>
                <td class="px-4 py-3 text-secondary">{{ formatDate(rfpData.basicInfo.submissionDeadline) }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mb-8">
          <h3 class="mb-4 border-b-2 border-primary pb-2 text-lg font-bold text-secondary">
            {{ t('rfp.fields.projectDescription') }}
          </h3>
          <div class="whitespace-pre-wrap text-sm leading-relaxed text-secondary">
            {{ rfpData.basicInfo.projectDescription }}
          </div>
        </div>

        <div v-if="rfpData.content.sections.length > 0" class="mb-8">
          <div
            v-for="(section, index) in rfpData.content.sections"
            :key="section.id"
            class="mb-6"
          >
            <h3 class="mb-3 border-b-2 border-primary pb-2 text-lg font-bold text-secondary">
              {{ index + 1 }}. {{ section.title }}
            </h3>
            <div
              v-if="section.contentHtml"
              class="prose prose-sm max-w-none text-secondary"
              v-html="section.contentHtml"
            ></div>
            <div
              v-else-if="section.content"
              class="whitespace-pre-wrap text-sm leading-relaxed text-secondary"
            >
              {{ section.content }}
            </div>
            <p v-else class="text-sm italic text-tertiary">
              {{ $t('rfp.export.noContent', 'لم يتم إضافة محتوى لهذا القسم بعد') }}
            </p>
          </div>
        </div>

        <div v-if="rfpData.boq.items.length > 0" class="mb-8">
          <h3 class="mb-4 border-b-2 border-primary pb-2 text-lg font-bold text-secondary">
            {{ t('rfp.steps.boq') }}
          </h3>
          <table class="w-full border-collapse border border-surface-dim text-sm">
            <thead>
              <tr class="bg-surface-muted">
                <th class="border border-surface-dim px-3 py-2 text-start font-medium text-secondary">#</th>
                <th class="border border-surface-dim px-3 py-2 text-start font-medium text-secondary">
                  {{ $t('rfp.boq.description', 'الوصف') }}
                </th>
                <th class="border border-surface-dim px-3 py-2 text-center font-medium text-secondary">
                  {{ $t('rfp.boq.unit', 'الوحدة') }}
                </th>
                <th class="border border-surface-dim px-3 py-2 text-center font-medium text-secondary">
                  {{ $t('rfp.boq.quantity', 'الكمية') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(item, index) in rfpData.boq.items" :key="item.id">
                <td class="border border-surface-dim px-3 py-2 text-center">{{ index + 1 }}</td>
                <td class="border border-surface-dim px-3 py-2">{{ item.description }}</td>
                <td class="border border-surface-dim px-3 py-2 text-center">{{ item.unit }}</td>
                <td class="border border-surface-dim px-3 py-2 text-center">{{ item.quantity }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="rfpData.settings.evaluationCriteria.length > 0" class="mb-8">
          <h3 class="mb-4 border-b-2 border-primary pb-2 text-lg font-bold text-secondary">
            {{ t('rfp.fields.evaluationCriteria') }}
          </h3>
          <table class="w-full border-collapse border border-surface-dim text-sm">
            <thead>
              <tr class="bg-surface-muted">
                <th class="border border-surface-dim px-3 py-2 text-start font-medium text-secondary">#</th>
                <th class="border border-surface-dim px-3 py-2 text-start font-medium text-secondary">
                  {{ $t('rfp.criteria.name', 'المعيار') }}
                </th>
                <th class="border border-surface-dim px-3 py-2 text-center font-medium text-secondary">
                  {{ $t('rfp.criteria.weight', 'الوزن') }}
                </th>
                <th class="border border-surface-dim px-3 py-2 text-start font-medium text-secondary">
                  {{ $t('rfp.criteria.description', 'الوصف') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(criterion, index) in rfpData.settings.evaluationCriteria" :key="criterion.id">
                <td class="border border-surface-dim px-3 py-2 text-center">{{ index + 1 }}</td>
                <td class="border border-surface-dim px-3 py-2">{{ criterion.name }}</td>
                <td class="border border-surface-dim px-3 py-2 text-center">{{ criterion.weight }}%</td>
                <td class="border border-surface-dim px-3 py-2">{{ criterion.description || '-' }}</td>
              </tr>
            </tbody>
          </table>
          <div class="mt-3 text-sm text-tertiary">
            <span>{{ $t('rfp.export.technicalWeight', 'الوزن الفني') }}: {{ rfpData.settings.technicalWeight }}%</span>
            <span class="mx-3">|</span>
            <span>{{ $t('rfp.export.financialWeight', 'الوزن المالي') }}: {{ rfpData.settings.financialWeight }}%</span>
            <span class="mx-3">|</span>
            <span>{{ $t('rfp.export.minTechnicalScore', 'الحد الأدنى للنجاح الفني') }}: {{ rfpData.settings.minimumTechnicalScore }}%</span>
          </div>
        </div>

        <div v-if="rfpData.attachments.requiredAttachmentTypes.length > 0" class="mb-8">
          <h3 class="mb-4 border-b-2 border-primary pb-2 text-lg font-bold text-secondary">
            {{ $t('rfp.labels.requiredAttachments', 'المرفقات الإلزامية') }}
          </h3>
          <ul class="list-inside list-decimal space-y-2 text-sm text-secondary">
            <li v-for="attachType in rfpData.attachments.requiredAttachmentTypes" :key="attachType">
              {{ $t(`rfp.requiredAttachments.${attachType}`, attachType) }}
            </li>
          </ul>
        </div>

        <div class="mt-12 border-t border-surface-dim pt-4 text-center text-xs text-tertiary">
          <p>{{ $t('rfp.export.generatedBy', 'تم إنشاء هذا المستند بواسطة منصة Tendex AI') }}</p>
          <p class="mt-1">{{ $t('rfp.export.generatedAt', 'تاريخ الإنشاء') }}: {{ new Date().toLocaleDateString('en-SA') }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
