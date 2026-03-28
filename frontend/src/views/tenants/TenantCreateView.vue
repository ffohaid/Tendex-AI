<script setup lang="ts">
/**
 * Tenant Create View — Super Admin Portal.
 *
 * Form for creating a new government entity (tenant) with validation.
 * Data is submitted dynamically to the API — NO mock data.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTenantStore } from '@/stores/tenant'
import type { CreateTenantRequest } from '@/types/tenant'

const { t } = useI18n()
const router = useRouter()
const tenantStore = useTenantStore()

/** Form data */
const form = ref<CreateTenantRequest>({
  nameAr: '',
  nameEn: '',
  identifier: '',
  subdomain: '',
  contactPersonName: '',
  contactPersonEmail: '',
  contactPersonPhone: '',
  notes: '',
  logoUrl: '',
  primaryColor: '#1e40af',
  secondaryColor: '#3b82f6',
})

/** Validation errors */
const validationErrors = ref<Record<string, string>>({})

/** Validate form */
function validate(): boolean {
  const errors: Record<string, string> = {}

  if (!form.value.nameAr.trim()) {
    errors.nameAr = t('tenants.validation.nameArRequired')
  }
  if (!form.value.nameEn.trim()) {
    errors.nameEn = t('tenants.validation.nameEnRequired')
  }
  if (!form.value.identifier.trim()) {
    errors.identifier = t('tenants.validation.identifierRequired')
  } else if (!/^[a-z0-9-]+$/.test(form.value.identifier)) {
    errors.identifier = t('tenants.validation.identifierFormat')
  }
  if (!form.value.subdomain.trim()) {
    errors.subdomain = t('tenants.validation.subdomainRequired')
  } else if (!/^[a-z0-9-]+$/.test(form.value.subdomain)) {
    errors.subdomain = t('tenants.validation.subdomainFormat')
  }
  if (
    form.value.contactPersonEmail &&
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.value.contactPersonEmail)
  ) {
    errors.contactPersonEmail = t('tenants.validation.emailInvalid')
  }

  validationErrors.value = errors
  return Object.keys(errors).length === 0
}

/** Auto-generate subdomain from identifier */
function onIdentifierChange() {
  if (!form.value.subdomain) {
    form.value.subdomain = form.value.identifier
  }
}

/** Submit form */
async function handleSubmit() {
  if (!validate()) return

  const result = await tenantStore.createTenant(form.value)
  if (result) {
    router.push({ name: 'TenantDetail', params: { id: result.id } })
  }
}

/** Go back */
function goBack() {
  router.push({ name: 'TenantList' })
}

const isSubmitting = computed(() => tenantStore.isSubmitting)
const apiError = computed(() => tenantStore.error)
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
          {{ t('tenants.actions.backToList') }}
        </button>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('tenants.titles.create') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('tenants.titles.createSubtitle') }}
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
        <!-- Basic Information Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-building me-2 text-primary"></i>
            {{ t('tenants.sections.basicInfo') }}
          </h2>

          <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
            <!-- Name Arabic -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.nameAr') }}
                <span class="text-danger">*</span>
              </label>
              <input
                v-model="form.nameAr"
                type="text"
                :placeholder="t('tenants.placeholders.nameAr')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.nameAr }"
              />
              <p
                v-if="validationErrors.nameAr"
                class="mt-1 text-xs text-danger"
              >
                {{ validationErrors.nameAr }}
              </p>
            </div>

            <!-- Name English -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.nameEn') }}
                <span class="text-danger">*</span>
              </label>
              <input
                v-model="form.nameEn"
                type="text"
                dir="ltr"
                :placeholder="t('tenants.placeholders.nameEn')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.nameEn }"
              />
              <p
                v-if="validationErrors.nameEn"
                class="mt-1 text-xs text-danger"
              >
                {{ validationErrors.nameEn }}
              </p>
            </div>

            <!-- Identifier -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.identifier') }}
                <span class="text-danger">*</span>
              </label>
              <input
                v-model="form.identifier"
                type="text"
                dir="ltr"
                :placeholder="t('tenants.placeholders.identifier')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{ 'border-danger': validationErrors.identifier }"
                @blur="onIdentifierChange"
              />
              <p
                v-if="validationErrors.identifier"
                class="mt-1 text-xs text-danger"
              >
                {{ validationErrors.identifier }}
              </p>
              <p v-else class="mt-1 text-xs text-tertiary">
                {{ t('tenants.hints.identifier') }}
              </p>
            </div>

            <!-- Subdomain -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.subdomain') }}
                <span class="text-danger">*</span>
              </label>
              <div class="flex items-center gap-2">
                <input
                  v-model="form.subdomain"
                  type="text"
                  dir="ltr"
                  :placeholder="t('tenants.placeholders.subdomain')"
                  class="flex-1 rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                  :class="{ 'border-danger': validationErrors.subdomain }"
                />
                <span
                  class="whitespace-nowrap text-sm text-tertiary"
                  dir="ltr"
                >
                  .tendex.ai
                </span>
              </div>
              <p
                v-if="validationErrors.subdomain"
                class="mt-1 text-xs text-danger"
              >
                {{ validationErrors.subdomain }}
              </p>
            </div>
          </div>
        </div>

        <!-- Contact Information Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-user me-2 text-primary"></i>
            {{ t('tenants.sections.contactInfo') }}
          </h2>

          <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
            <!-- Contact Name -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.contactName') }}
              </label>
              <input
                v-model="form.contactPersonName"
                type="text"
                :placeholder="t('tenants.placeholders.contactName')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <!-- Contact Email -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.contactEmail') }}
              </label>
              <input
                v-model="form.contactPersonEmail"
                type="email"
                dir="ltr"
                :placeholder="t('tenants.placeholders.contactEmail')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                :class="{
                  'border-danger': validationErrors.contactPersonEmail,
                }"
              />
              <p
                v-if="validationErrors.contactPersonEmail"
                class="mt-1 text-xs text-danger"
              >
                {{ validationErrors.contactPersonEmail }}
              </p>
            </div>

            <!-- Contact Phone -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.contactPhone') }}
              </label>
              <input
                v-model="form.contactPersonPhone"
                type="tel"
                dir="ltr"
                :placeholder="t('tenants.placeholders.contactPhone')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>
          </div>
        </div>

        <!-- Branding Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-palette me-2 text-primary"></i>
            {{ t('tenants.sections.branding') }}
          </h2>

          <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
            <!-- Logo URL -->
            <div class="md:col-span-3">
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.logoUrl') }}
              </label>
              <input
                v-model="form.logoUrl"
                type="url"
                dir="ltr"
                :placeholder="t('tenants.placeholders.logoUrl')"
                class="w-full rounded-lg border border-surface-dim px-4 py-2.5 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <!-- Primary Color -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.primaryColor') }}
              </label>
              <div class="flex items-center gap-3">
                <input
                  v-model="form.primaryColor"
                  type="color"
                  class="h-10 w-10 cursor-pointer rounded border border-surface-dim"
                />
                <input
                  v-model="form.primaryColor"
                  type="text"
                  dir="ltr"
                  class="flex-1 rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                />
              </div>
            </div>

            <!-- Secondary Color -->
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('tenants.fields.secondaryColor') }}
              </label>
              <div class="flex items-center gap-3">
                <input
                  v-model="form.secondaryColor"
                  type="color"
                  class="h-10 w-10 cursor-pointer rounded border border-surface-dim"
                />
                <input
                  v-model="form.secondaryColor"
                  type="text"
                  dir="ltr"
                  class="flex-1 rounded-lg border border-surface-dim px-4 py-2.5 text-sm font-mono focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Notes Section -->
        <div class="rounded-lg border border-surface-dim bg-white p-6">
          <h2 class="mb-6 text-lg font-semibold text-secondary">
            <i class="pi pi-file-edit me-2 text-primary"></i>
            {{ t('tenants.sections.notes') }}
          </h2>
          <textarea
            v-model="form.notes"
            rows="4"
            :placeholder="t('tenants.placeholders.notes')"
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
            <i
              v-if="isSubmitting"
              class="pi pi-spin pi-spinner text-sm"
            ></i>
            <i v-else class="pi pi-check text-sm"></i>
            {{ t('tenants.actions.create') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
