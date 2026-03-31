<script setup lang="ts">
/**
 * Step 4: Bill of Quantities (BOQ).
 *
 * Interactive table for managing BOQ items with:
 * - Add/remove/reorder items
 * - Inline editing
 * - Auto-calculation of totals
 * - VAT toggle
 * - Drag & Drop reordering
 */
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { boqSchema } from '@/validations/rfp'
import { useRfpStore } from '@/stores/rfp'
import { formatCurrency } from '@/utils/numbers'
import draggable from 'vuedraggable'
import AiBoqGenerator from './AiBoqGenerator.vue'
import type { BoqItem as AiBoqItem } from '@/services/aiSpecificationService'

const { t } = useI18n()
const rfpStore = useRfpStore()

const schema = toTypedSchema(boqSchema)

const { errors, validate, setFieldValue } = useForm({
  validationSchema: schema,
  initialValues: { ...rfpStore.formData.boq },
  validateOnMount: false,
})

/** Unit options */
const unitOptions = computed(() => [
  { value: 'unit', label: t('rfp.units.unit') },
  { value: 'meter', label: t('rfp.units.meter') },
  { value: 'sqm', label: t('rfp.units.sqm') },
  { value: 'cbm', label: t('rfp.units.cbm') },
  { value: 'kg', label: t('rfp.units.kg') },
  { value: 'ton', label: t('rfp.units.ton') },
  { value: 'liter', label: t('rfp.units.liter') },
  { value: 'hour', label: t('rfp.units.hour') },
  { value: 'day', label: t('rfp.units.day') },
  { value: 'month', label: t('rfp.units.month') },
  { value: 'lump_sum', label: t('rfp.units.lumpSum') },
  { value: 'trip', label: t('rfp.units.trip') },
  { value: 'set', label: t('rfp.units.set') },
])

/** Subtotal before VAT */
const subtotal = computed(() =>
  rfpStore.formData.boq.items.reduce(
    (sum, item) => sum + item.quantity * item.estimatedPrice,
    0,
  ),
)

/** VAT amount */
const vatAmount = computed(() => {
  if (!rfpStore.formData.boq.includesVat) return 0
  return subtotal.value * (rfpStore.formData.boq.vatPercentage / 100)
})

/** Grand total */
const grandTotal = computed(() => subtotal.value + vatAmount.value)

/** Add new BOQ item */
function addItem() {
  rfpStore.addBoqItem()
}

/** Handle drag end */
function onDragEnd() {
  rfpStore.reorderBoqItems(rfpStore.formData.boq.items)
}

/** Handle AI-generated BOQ items */
function handleAiBoq(items: AiBoqItem[]) {
  // Clear existing items and add AI-generated ones
  rfpStore.formData.boq.items = []
  items.forEach((item) => {
    rfpStore.addBoqItem({
      category: item.category,
      description: item.descriptionAr,
      unit: item.unit as any,
      quantity: item.estimatedQuantity,
      estimatedPrice: item.estimatedUnitPrice,
    })
  })
}

/** Toggle VAT */
function toggleVat(event: Event) {
  rfpStore.updateBoq({ includesVat: (event.target as HTMLInputElement).checked })
}

/** Update VAT percentage */
function updateVatPercentage(event: Event) {
  rfpStore.updateBoq({ vatPercentage: Number((event.target as HTMLInputElement).value) })
}

defineExpose({
  validate: async () => {
    /**
     * CRITICAL FIX: Sync Pinia store BOQ items into VeeValidate before
     * running validation. BOQ items are managed directly in the Pinia
     * store, so VeeValidate's internal state becomes stale.
     */
    const storeBoq = rfpStore.formData.boq
    setFieldValue('items', storeBoq.items)
    setFieldValue('includesVat', storeBoq.includesVat)
    setFieldValue('vatPercentage', storeBoq.vatPercentage)
    setFieldValue('totalEstimatedValue', storeBoq.totalEstimatedValue)

    const result = await validate()
    return result.valid
  },
})
</script>

<template>
  <div class="space-y-6">
    <div class="mb-6">
      <h2 class="text-xl font-bold text-secondary">
        {{ t('rfp.steps.boq') }}
      </h2>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('rfp.steps.boqDesc') }}
      </p>
    </div>

    <!-- AI BOQ Generator -->
    <div v-if="rfpStore.formData.boq.items.length === 0" class="mb-4">
      <AiBoqGenerator @boq-generated="handleAiBoq" />
    </div>

    <!-- Action bar -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div class="flex items-center gap-3">
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
          @click="addItem"
        >
          <i class="pi pi-plus text-xs"></i>
          {{ t('rfp.actions.addBoqItem') }}
        </button>

        <!-- AI Regenerate BOQ (shown when items exist) -->
        <button
          v-if="rfpStore.formData.boq.items.length > 0"
          type="button"
          class="flex items-center gap-2 rounded-lg border border-ai-300 bg-ai-50 px-4 py-2 text-sm font-medium text-ai-600 transition-colors hover:bg-ai-100"
          @click="rfpStore.formData.boq.items = []"
        >
          <i class="pi pi-sparkles text-xs"></i>
          {{ t('ai.regenerateBoq') }}
        </button>
      </div>

      <div class="flex items-center gap-4">
        <!-- VAT toggle -->
        <div class="flex items-center gap-2">
          <input
            id="includesVat"
            :checked="rfpStore.formData.boq.includesVat"
            type="checkbox"
            class="h-4 w-4 rounded border-surface-dim text-primary accent-primary"
            @change="toggleVat"
          />
          <label for="includesVat" class="text-sm text-secondary">
            {{ t('rfp.fields.includesVat') }}
          </label>
        </div>

        <!-- VAT percentage -->
        <div v-if="rfpStore.formData.boq.includesVat" class="flex items-center gap-2">
          <input
            :value="rfpStore.formData.boq.vatPercentage"
            type="number"
            min="0"
            max="100"
            step="0.5"
            class="w-20 rounded-lg border border-surface-dim px-3 py-1.5 text-sm focus:border-primary focus:outline-none"
            @input="updateVatPercentage"
          />
          <span class="text-sm text-tertiary">%</span>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-if="rfpStore.formData.boq.items.length === 0"
      class="rounded-lg border-2 border-dashed border-surface-dim p-12 text-center"
    >
      <i class="pi pi-table mb-3 text-4xl text-tertiary"></i>
      <p class="text-sm text-tertiary">{{ t('rfp.messages.noBoqItems') }}</p>
    </div>

    <!-- BOQ Table -->
    <div v-else class="overflow-x-auto rounded-lg border border-surface-dim">
      <table class="w-full min-w-[800px] text-sm">
        <thead class="bg-surface-muted">
          <tr>
            <th class="w-10 px-3 py-3 text-center text-xs font-medium text-tertiary">#</th>
            <th class="px-3 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.boqFields.category') }}</th>
            <th class="px-3 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.boqFields.description') }}</th>
            <th class="w-32 px-3 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.boqFields.unit') }}</th>
            <th class="w-24 px-3 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.boqFields.quantity') }}</th>
            <th class="w-32 px-3 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.boqFields.estimatedPrice') }}</th>
            <th class="w-32 px-3 py-3 text-start text-xs font-medium text-tertiary">{{ t('rfp.boqFields.totalPrice') }}</th>
            <th class="w-16 px-3 py-3 text-center text-xs font-medium text-tertiary">{{ t('rfp.boqFields.actions') }}</th>
          </tr>
        </thead>
        <draggable
          v-model="rfpStore.formData.boq.items"
          tag="tbody"
          item-key="id"
          handle=".boq-drag-handle"
          ghost-class="opacity-30"
          animation="200"
          @end="onDragEnd"
        >
          <template #item="{ element: item }">
            <tr class="border-t border-surface-dim transition-colors hover:bg-surface-muted/50">
              <td class="px-3 py-2 text-center">
                <span class="boq-drag-handle cursor-grab text-tertiary hover:text-secondary active:cursor-grabbing">
                  <i class="pi pi-bars text-xs"></i>
                </span>
              </td>
              <td class="px-3 py-2">
                <input
                  :value="item.category"
                  type="text"
                  class="w-full rounded border border-transparent bg-transparent px-2 py-1 text-sm focus:border-primary focus:bg-white focus:outline-none"
                  :placeholder="t('rfp.placeholders.boqCategory')"
                  @input="rfpStore.updateBoqItem(item.id, { category: ($event.target as HTMLInputElement).value })"
                />
              </td>
              <td class="px-3 py-2">
                <input
                  :value="item.description"
                  type="text"
                  class="w-full rounded border border-transparent bg-transparent px-2 py-1 text-sm focus:border-primary focus:bg-white focus:outline-none"
                  :placeholder="t('rfp.placeholders.boqDescription')"
                  @input="rfpStore.updateBoqItem(item.id, { description: ($event.target as HTMLInputElement).value })"
                />
              </td>
              <td class="px-3 py-2">
                <select
                  :value="item.unit"
                  class="w-full rounded border border-transparent bg-transparent px-2 py-1 text-sm focus:border-primary focus:bg-white focus:outline-none"
                  @change="rfpStore.updateBoqItem(item.id, { unit: ($event.target as HTMLSelectElement).value as any })"
                >
                  <option
                    v-for="unit in unitOptions"
                    :key="unit.value"
                    :value="unit.value"
                  >
                    {{ unit.label }}
                  </option>
                </select>
              </td>
              <td class="px-3 py-2">
                <input
                  :value="item.quantity"
                  type="number"
                  min="0"
                  step="0.01"
                  class="w-full rounded border border-transparent bg-transparent px-2 py-1 text-sm text-end focus:border-primary focus:bg-white focus:outline-none"
                  @input="rfpStore.updateBoqItem(item.id, { quantity: Number(($event.target as HTMLInputElement).value) })"
                />
              </td>
              <td class="px-3 py-2">
                <input
                  :value="item.estimatedPrice"
                  type="number"
                  min="0"
                  step="0.01"
                  class="w-full rounded border border-transparent bg-transparent px-2 py-1 text-sm text-end focus:border-primary focus:bg-white focus:outline-none"
                  @input="rfpStore.updateBoqItem(item.id, { estimatedPrice: Number(($event.target as HTMLInputElement).value) })"
                />
              </td>
              <td class="px-3 py-2 text-end font-medium text-secondary">
                {{ formatCurrency(item.quantity * item.estimatedPrice) }}
              </td>
              <td class="px-3 py-2 text-center">
                <button
                  type="button"
                  class="flex h-7 w-7 items-center justify-center rounded text-danger transition-colors hover:bg-danger/10"
                  :title="t('common.delete')"
                  @click="rfpStore.removeBoqItem(item.id)"
                >
                  <i class="pi pi-trash text-xs"></i>
                </button>
              </td>
            </tr>
          </template>
        </draggable>

        <!-- Totals footer -->
        <tfoot class="border-t-2 border-primary/20 bg-surface-muted">
          <tr>
            <td colspan="6" class="px-3 py-3 text-end text-sm font-medium text-secondary">
              {{ t('rfp.boqFields.subtotal') }}
            </td>
            <td class="px-3 py-3 text-end text-sm font-bold text-secondary">
              {{ formatCurrency(subtotal) }}
            </td>
            <td></td>
          </tr>
          <tr v-if="rfpStore.formData.boq.includesVat">
            <td colspan="6" class="px-3 py-2 text-end text-sm text-tertiary">
              {{ t('rfp.boqFields.vat') }} ({{ rfpStore.formData.boq.vatPercentage }}%)
            </td>
            <td class="px-3 py-2 text-end text-sm text-tertiary">
              {{ formatCurrency(vatAmount) }}
            </td>
            <td></td>
          </tr>
          <tr>
            <td colspan="6" class="px-3 py-3 text-end text-sm font-bold text-primary">
              {{ t('rfp.boqFields.grandTotal') }}
            </td>
            <td class="px-3 py-3 text-end text-sm font-bold text-primary">
              {{ formatCurrency(grandTotal) }}
            </td>
            <td></td>
          </tr>
        </tfoot>
      </table>
    </div>

    <!-- Validation error -->
    <p v-if="errors.items" class="mt-2 text-sm text-danger">
      <i class="pi pi-exclamation-circle me-1"></i>
      {{ errors.items }}
    </p>
  </div>
</template>
