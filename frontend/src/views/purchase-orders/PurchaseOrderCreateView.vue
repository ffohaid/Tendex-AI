<script setup lang="ts">
/**
 * Purchase Order Create View — Super Admin Portal.
 *
 * Form for creating a new purchase order linked to a tenant.
 * Data is submitted dynamically to the API — NO mock data.
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { usePurchaseOrderStore } from '@/stores/purchaseOrder'
import { useTenantStore } from '@/stores/tenant'
import { SubscriptionPlan } from '@/types/tenant'
import type { CreatePurchaseOrderRequest } from '@/types/tenant'

const { t, locale } = useI18n()
const router = useRouter()
const poStore = usePurchaseOrderStore()
const tenantStore = useTenantStore()

const { selectorOptions } = storeToRefs(tenantStore)

/** Form data */
const form = ref<CreatePurchaseOrderRequest>({
  tenantId: '',
  poNumber: '',
  totalAmount: 0,
  subscriptionPlan: SubscriptionPlan.Basic,
  poDateGregorian: '',
  durationMonths: 12,
  contactPersonName: '',
  contactPersonEmail: '',
  contactPersonPhone: '',
  notes: '',
})

/** Validation errors */
const validationErrors = ref<Record<string, string>>({})

/** Plan options */
const planOptions = [
  { value: SubscriptionPlan.Basic, labelKey: 'purchaseOrders.plans.basic' },
  { value: SubscriptionPlan.Professional, labelKey: 'purchaseOrders.plans.professional' },
  { value: SubscriptionPlan.Enterprise, labelKey: 'purchaseOrders.plans.enterprise' },
]

/** Duration options */
const durationOptions = [
  { value: 6, labelKey: 'purchaseOrders.durations.sixMonths' },
  { value: 12, labelKey: 'purchaseOrders.durations.oneYear' },
  { value: 24, labelKey: 'purchaseOrders.durations.twoYears' },
  { value: 36, labelKey: 'purchaseOrders.durations.threeYears' },
]

/** Validate form */
function validate(): boolean {
  const errors: Record<string, string> = {}

  if (!form.value.tenantId) {
    errors.tenantId = t('purchaseOrders.validation.tenantRequired')
  }
  if (!form.value.poNumber.trim()) {
    errors.poNumber = t('purchaseOrders.validation.poNumberRequired')
  }
  if (!form.value.totalAmount || form.value.totalAmount <= 0) {
    errors.totalAmount = t('purchaseOrders.validation.amountRequired')
  }
  if (!form.value.poDateGregorian) {
    errors.poDateGregorian = t('purchaseOrders.validation.dateRequired')
  }
  if (
    form.value.contactPersonEmail &&
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.value.contactPersonEmail)
  ) {
    errors.contactPersonEmail = t('purchaseOrders.validation.emailInvalid')
  }

  validationErrors.value = errors
  return Object.keys(errors).length === 0
}

/** Submit form */
async function handleSubmit() {
  if (!validate()) return

  const result = await poStore.createPurchaseOrder(form.value)
  if (result) {
    router.push({ name: 'PurchaseOrderDetail', params: { id: result.id } })
  }
}

/** Go back */
function goBack() {
  router.push({ name: 'PurchaseOrderList' })
}

const isSubmitting = computed(() => poStore.isSubmitting)
const apiError = computed(() => poStore.error)

onMounted(() => {
  tenantStore.loadSelectorOptions()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-4xl px-4 py-8 sm:px-6">
      <!-- Header -->
      <div class="mb-8">
        <button
          type="button"
          class="mb-4 flex items-center gap-1 text-sm text-tertiary hover:text-primary"
          @click="goBack"
        >
          <i class="pi pi-arrow-right rotate-180 text-xs rtl:rotate-0"></i>
          {{ t('purchaseOrders.actions.backToList') }}
        </button>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('purchaseOrders.titles.create') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('purchaseOrders.titles.createSubtitle') }}
        </p>
      </div>

      <!-- API Error -->
      <div
        v-if="apiError"
        class="mb-6 rounded-lg border border-danger/20 bg-danger/5 p-4"
      >
        <div class="flex items-center gap-2">
          <i class="pi pi-exclamation-triangle text-danger"></i>
          <p class="text-sm text-danger">{{ apiError }}</p>
        </div>
      </div>

      <!-- Form -->
      <form @submit.prevent="handleSubmit" class="space-y-8">
        <!-- Tenant & PO Info Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-file me-2 text-primary"></i>
            {{ t('purchaseOrders.sections.poInfo') }}
          </h2>

          <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
            <!-- Tenant Selection -->
            <div class="md:col-span-2">
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.tenant') }}
                <span class="text-danger">*</span>
              </label>
              <select
                v-model="form.tenantId"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.tenantId }"
              >
                <option value="">{{ t('purchaseOrders.placeholders.selectTenant') }}</option>
                <option
                  v-for="tenant in selectorOptions"
                  :key="tenant.id"
                  :value="tenant.id"
                >
                  {{ locale === 'ar' ? tenant.nameAr : tenant.nameEn }} ({{ tenant.identifier }})
                </option>
              </select>
              <p v-if="validationErrors.tenantId" class="mt-1 text-xs text-danger">
                {{ validationErrors.tenantId }}
              </p>
            </div>

            <!-- PO Number -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.poNumber') }}
                <span class="text-danger">*</span>
              </label>
              <input
                v-model="form.poNumber"
                type="text"
                dir="ltr"
                :placeholder="t('purchaseOrders.placeholders.poNumber')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.poNumber }"
              />
              <p v-if="validationErrors.poNumber" class="mt-1 text-xs text-danger">
                {{ validationErrors.poNumber }}
              </p>
            </div>

            <!-- PO Date -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.poDate') }}
                <span class="text-danger">*</span>
              </label>
              <input
                v-model="form.poDateGregorian"
                type="date"
                dir="ltr"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.poDateGregorian }"
              />
              <p v-if="validationErrors.poDateGregorian" class="mt-1 text-xs text-danger">
                {{ validationErrors.poDateGregorian }}
              </p>
            </div>

            <!-- Total Amount -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.totalAmount') }}
                <span class="text-danger">*</span>
              </label>
              <div class="relative">
                <input
                  v-model.number="form.totalAmount"
                  type="number"
                  dir="ltr"
                  min="0"
                  step="0.01"
                  :placeholder="t('purchaseOrders.placeholders.totalAmount')"
                  class="w-full rounded-lg border border-surface-dim px-4 py-2.5 pe-16 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                  :class="{ 'border-danger': validationErrors.totalAmount }"
                />
                <span class="absolute inset-y-0 end-0 flex items-center pe-3 text-sm text-tertiary">
                  &#xFDFC;
                </span>
              </div>
              <p v-if="validationErrors.totalAmount" class="mt-1 text-xs text-danger">
                {{ validationErrors.totalAmount }}
              </p>
            </div>

            <!-- Subscription Plan -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.subscriptionPlan') }}
              </label>
              <select
                v-model="form.subscriptionPlan"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option
                  v-for="option in planOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ t(option.labelKey) }}
                </option>
              </select>
            </div>

            <!-- Duration -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.duration') }}
              </label>
              <select
                v-model="form.durationMonths"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option
                  v-for="option in durationOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ t(option.labelKey) }}
                </option>
              </select>
            </div>
          </div>
        </div>

        <!-- Contact Information Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-user me-2 text-primary"></i>
            {{ t('purchaseOrders.sections.contactInfo') }}
          </h2>

          <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.contactName') }}
              </label>
              <input
                v-model="form.contactPersonName"
                type="text"
                :placeholder="t('purchaseOrders.placeholders.contactName')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.contactEmail') }}
              </label>
              <input
                v-model="form.contactPersonEmail"
                type="email"
                dir="ltr"
                :placeholder="t('purchaseOrders.placeholders.contactEmail')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.contactPersonEmail }"
              />
              <p v-if="validationErrors.contactPersonEmail" class="mt-1 text-xs text-danger">
                {{ validationErrors.contactPersonEmail }}
              </p>
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('purchaseOrders.fields.contactPhone') }}
              </label>
              <input
                v-model="form.contactPersonPhone"
                type="tel"
                dir="ltr"
                :placeholder="t('purchaseOrders.placeholders.contactPhone')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>
        </div>

        <!-- Notes Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-file-edit me-2 text-primary"></i>
            {{ t('purchaseOrders.sections.notes') }}
          </h2>
          <textarea
            v-model="form.notes"
            rows="4"
            :placeholder="t('purchaseOrders.placeholders.notes')"
            class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
          ></textarea>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-3">
          <button
            type="button"
            class="rounded-lg border border-surface-dim px-6 py-2.5 text-sm font-medium text-secondary hover:bg-surface-muted"
            @click="goBack"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="submit"
            class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-bold text-white shadow-md transition-all hover:bg-primary-dark hover:shadow-lg disabled:opacity-50"
            :disabled="isSubmitting"
          >
            <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-sm"></i>
            <i v-else class="pi pi-check text-sm"></i>
            {{ t('purchaseOrders.actions.create') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
