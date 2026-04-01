<script setup lang="ts">
/**
 * AiBoqGenerator Component
 *
 * Generates Bill of Quantities items using AI based on
 * the project description and sections content.
 *
 * Maps backend GeneratedBoqItem → frontend BoqItem for the parent.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { useRfpStore } from '@/stores/rfp'
import {
  generateBoq,
  type GenerateBoqResult,
  type GeneratedBoqItem,
  type BoqItem,
} from '@/services/aiSpecificationService'

const emit = defineEmits<{
  (e: 'boq-generated', items: BoqItem[]): void
}>()

const { t } = useI18n()
const authStore = useAuthStore()
const rfpStore = useRfpStore()

const isGenerating = ref(false)
const result = ref<GenerateBoqResult | null>(null)
const error = ref('')
const showPreview = ref(false)

/** Convenience accessor: backend nests items inside `boq` */
const boqItems = computed<GeneratedBoqItem[]>(() => {
  return result.value?.boq?.items ?? []
})

const boqSummary = computed<string>(() => {
  return result.value?.boq?.summaryAr ?? ''
})

const boqWarnings = computed<string[]>(() => {
  return result.value?.boq?.warnings ?? []
})

const totalCost = computed<number>(() => {
  return result.value?.boq?.totalEstimatedCost ?? 0
})

async function handleGenerate() {
  isGenerating.value = true
  error.value = ''
  result.value = null

  try {
    // Collect sections content for context
    const sectionsContent = rfpStore.formData.content.sections
      .map(s => `${s.title}: ${s.content || ''}`)
      .join('\n')

    const response = await generateBoq({
      tenantId: authStore.tenantId || '',
      competitionId: rfpStore.formData.id || crypto.randomUUID(),
      projectNameAr: rfpStore.formData.basicInfo.projectName || '',
      projectDescriptionAr: rfpStore.formData.basicInfo.projectDescription || '',
      projectType: (rfpStore.formData.basicInfo.competitionType as string) || 'general',
      estimatedBudget: rfpStore.formData.basicInfo.estimatedValue ?? undefined,
      specificationsContentHtml: sectionsContent || undefined,
    })

    if (response.isSuccess && response.boq?.items?.length) {
      result.value = response
      showPreview.value = true
    } else {
      error.value = response.errorMessage || t('ai.errors.boqGenerationFailed')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage || t('ai.errors.boqGenerationFailed')
  } finally {
    isGenerating.value = false
  }
}

/**
 * Normalize AI-generated unit strings (Arabic/English/mixed) to frontend UnitOfMeasure keys.
 * Falls back to 'unit' if no match is found.
 */
const unitNormalizationMap: Record<string, string> = {
  // Arabic unit names
  'وحدة': 'unit',
  'عدد': 'unit',
  'قطعة': 'unit',
  'متر': 'meter',
  'م.ط': 'meter',
  'متر مربع': 'sqm',
  'م²': 'sqm',
  'م.م': 'sqm',
  'متر مكعب': 'cbm',
  'م³': 'cbm',
  'كيلوغرام': 'kg',
  'كجم': 'kg',
  'طن': 'ton',
  'لتر': 'liter',
  'ساعة': 'hour',
  'يوم': 'day',
  'شهر': 'month',
  'سنة': 'year',
  'مقطوعية': 'lump_sum',
  'مبلغ مقطوع': 'lump_sum',
  'مقطوع': 'lump_sum',
  'رحلة': 'trip',
  'طقم': 'set',
  'مجموعة': 'set',
  'شخص': 'person',
  'فرد': 'person',
  'رخصة': 'license',
  'ترخيص': 'license',
  // English unit names
  'unit': 'unit',
  'each': 'unit',
  'piece': 'unit',
  'pcs': 'unit',
  'meter': 'meter',
  'sqm': 'sqm',
  'square meter': 'sqm',
  'cbm': 'cbm',
  'cubic meter': 'cbm',
  'kg': 'kg',
  'kilogram': 'kg',
  'ton': 'ton',
  'liter': 'liter',
  'hour': 'hour',
  'day': 'day',
  'month': 'month',
  'year': 'year',
  'lump sum': 'lump_sum',
  'lumpsum': 'lump_sum',
  'lump_sum': 'lump_sum',
  'ls': 'lump_sum',
  'trip': 'trip',
  'set': 'set',
  'person': 'person',
  'license': 'license',
}

function normalizeUnit(rawUnit: string): string {
  if (!rawUnit) return 'unit'
  const normalized = rawUnit.trim().toLowerCase()
  return unitNormalizationMap[normalized] || unitNormalizationMap[rawUnit.trim()] || 'unit'
}

/**
 * Map backend GeneratedBoqItem → frontend BoqItem and emit.
 */
function applyBoq() {
  if (!boqItems.value.length) return

  const mapped: BoqItem[] = boqItems.value.map((item) => ({
    itemNumber: item.itemNumber,
    descriptionAr: item.descriptionAr,
    unit: normalizeUnit(item.unit),
    estimatedQuantity: item.quantity,
    estimatedUnitPrice: item.estimatedUnitPrice ?? 0,
    estimatedTotalPrice: (item.quantity ?? 0) * (item.estimatedUnitPrice ?? 0),
    category: item.category ?? '',
    notes: item.justificationAr,
  }))

  emit('boq-generated', mapped)
  showPreview.value = false
}

function formatNumber(num: number | undefined | null) {
  return new Intl.NumberFormat('en-SA', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(num ?? 0)
}
</script>

<template>
  <div class="ai-boq-generator">
    <!-- Generate BOQ Button -->
    <button
      type="button"
      class="group flex items-center gap-2.5 rounded-xl border-2 border-dashed border-ai-300 bg-gradient-to-r from-ai-50 to-white px-5 py-3 text-sm font-semibold text-ai-600 transition-all duration-300 hover:border-ai-400 hover:shadow-lg hover:shadow-ai-100/50"
      :disabled="isGenerating"
      @click="handleGenerate"
    >
      <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100 transition-transform duration-300 group-hover:scale-110">
        <i
          class="pi text-lg text-ai-600"
          :class="isGenerating ? 'pi-spin pi-spinner' : 'pi-sparkles'"
        ></i>
      </div>
      <div class="text-start">
        <span class="block">{{ isGenerating ? t('ai.generatingBoq') : t('ai.generateBoq') }}</span>
        <span class="block text-[11px] font-normal text-ai-400">{{ t('ai.generateBoqDesc') }}</span>
      </div>
    </button>

    <!-- Error -->
    <div v-if="error" class="mt-3 rounded-lg border border-danger/20 bg-danger/5 p-3 text-xs text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ error }}
    </div>

    <!-- Preview Modal -->
    <Transition name="fade">
      <div
        v-if="showPreview && result"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm"
        @click.self="showPreview = false"
      >
        <div class="mx-4 max-h-[85vh] w-full max-w-4xl overflow-hidden rounded-2xl bg-white shadow-2xl">
          <!-- Modal Header -->
          <div class="border-b border-secondary-100 bg-gradient-to-r from-ai-50 to-white p-5">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-ai-100">
                <i class="pi pi-sparkles text-lg text-ai-600"></i>
              </div>
              <div>
                <h3 class="text-base font-bold text-secondary-800">{{ t('ai.suggestedBoq') }}</h3>
                <p class="text-xs text-secondary-500">
                  {{ t('ai.reviewAndApplyBoq') }} - {{ boqItems.length }} {{ t('rfp.boqFields.items') }}
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

          <!-- BOQ Items Table -->
          <div class="max-h-[55vh] overflow-auto p-5">
            <!-- Summary -->
            <div v-if="boqSummary" class="mb-4 rounded-lg bg-ai-50 p-3">
              <p class="text-xs leading-relaxed text-ai-700">
                <i class="pi pi-info-circle me-1"></i>
                {{ boqSummary }}
              </p>
            </div>

            <!-- Warnings -->
            <div v-if="boqWarnings.length > 0" class="mb-4 rounded-lg bg-warning/5 border border-warning/20 p-3">
              <p class="mb-1 text-xs font-semibold text-warning">
                <i class="pi pi-exclamation-triangle me-1"></i>
                {{ t('ai.assumptions') }}
              </p>
              <ul class="list-inside list-disc space-y-0.5 text-xs text-warning/80">
                <li v-for="(warning, idx) in boqWarnings" :key="idx">{{ warning }}</li>
              </ul>
            </div>

            <div class="overflow-x-auto rounded-lg border border-secondary-100">
              <table class="w-full text-sm">
                <thead class="bg-secondary-50">
                  <tr>
                    <th class="w-10 px-3 py-2.5 text-center text-xs font-medium text-secondary-500">#</th>
                    <th class="px-3 py-2.5 text-start text-xs font-medium text-secondary-500">{{ t('rfp.boqFields.category') }}</th>
                    <th class="px-3 py-2.5 text-start text-xs font-medium text-secondary-500">{{ t('rfp.boqFields.description') }}</th>
                    <th class="w-24 px-3 py-2.5 text-center text-xs font-medium text-secondary-500">{{ t('rfp.boqFields.unit') }}</th>
                    <th class="w-20 px-3 py-2.5 text-center text-xs font-medium text-secondary-500">{{ t('rfp.boqFields.quantity') }}</th>
                    <th class="w-28 px-3 py-2.5 text-end text-xs font-medium text-secondary-500">{{ t('rfp.boqFields.estimatedPrice') }}</th>
                    <th class="w-28 px-3 py-2.5 text-end text-xs font-medium text-secondary-500">{{ t('rfp.boqFields.totalPrice') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(item, idx) in boqItems"
                    :key="idx"
                    class="border-t border-secondary-100 transition-colors hover:bg-secondary-50/50"
                  >
                    <td class="px-3 py-2 text-center text-xs text-secondary-400">{{ item.itemNumber || idx + 1 }}</td>
                    <td class="px-3 py-2 text-xs font-medium text-secondary-700">{{ item.category || '-' }}</td>
                    <td class="px-3 py-2 text-xs text-secondary-600">{{ item.descriptionAr }}</td>
                    <td class="px-3 py-2 text-center text-xs text-secondary-500">{{ item.unit }}</td>
                    <td class="px-3 py-2 text-center text-xs text-secondary-600">{{ item.quantity }}</td>
                    <td class="px-3 py-2 text-end text-xs text-secondary-600">{{ item.estimatedUnitPrice ? formatNumber(item.estimatedUnitPrice) : '-' }}</td>
                    <td class="px-3 py-2 text-end text-xs font-medium text-secondary-700">
                      {{ item.estimatedUnitPrice ? formatNumber(item.quantity * item.estimatedUnitPrice) : '-' }}
                    </td>
                  </tr>
                </tbody>
                <tfoot v-if="totalCost" class="border-t-2 border-primary/20 bg-primary/5">
                  <tr>
                    <td colspan="6" class="px-3 py-2.5 text-end text-xs font-bold text-primary">{{ t('rfp.boqFields.grandTotal') }}</td>
                    <td class="px-3 py-2.5 text-end text-xs font-bold text-primary">
                      {{ formatNumber(totalCost) }}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="flex items-center justify-end gap-3 border-t border-secondary-100 bg-secondary-50 px-5 py-4">
            <button
              type="button"
              class="rounded-lg border border-secondary-200 bg-white px-4 py-2 text-sm text-secondary-600 hover:bg-secondary-50"
              @click="showPreview = false"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg bg-gradient-to-r from-ai-500 to-ai-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:from-ai-600 hover:to-ai-700"
              :disabled="!boqItems.length"
              @click="applyBoq"
            >
              <i class="pi pi-check text-xs"></i>
              <span>{{ t('ai.applyBoq') }}</span>
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
