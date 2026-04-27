<script setup lang="ts">
/**
 * Tenant Branding Customization View — Super Admin Portal.
 *
 * Comprehensive branding editor for government entities including:
 * - Logo upload with drag-and-drop support
 * - Primary and secondary color pickers with hex input
 * - Live preview panel showing how the branding will look
 * - Instant preview updates before saving
 * - Reset to defaults functionality
 *
 * Data is fetched dynamically from the API — NO mock data.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useTenantStore } from '@/stores/tenant'
import * as brandingService from '@/services/brandingService'
import type { UpdateTenantBrandingRequest } from '@/types/tenant'
import { DEFAULT_BRANDING } from '@/types/branding'

const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const tenantStore = useTenantStore()

const { currentTenant, isLoading: tenantLoading } = storeToRefs(tenantStore)

/** Tenant ID from route */
const tenantId = computed(() => route.params.id as string)

/** Branding form state */
const logoUrl = ref('')
const logoStorageValue = ref('')
const primaryColor = ref(DEFAULT_BRANDING.primaryColor)
const secondaryColor = ref(DEFAULT_BRANDING.secondaryColor)

/** File upload state */
const isUploading = ref(false)
const uploadError = ref<string | null>(null)
const isDragOver = ref(false)
const fileInputRef = ref<HTMLInputElement | null>(null)

/** Save state */
const isSaving = ref(false)
const saveSuccess = ref(false)
const saveError = ref<string | null>(null)

/** Get tenant display name */
function getTenantName(): string {
  if (!currentTenant.value) return ''
  return locale.value === 'ar'
    ? currentTenant.value.nameAr
    : currentTenant.value.nameEn
}

/** Populate form from current tenant */
function populateForm(): void {
  if (!currentTenant.value) return
  logoUrl.value = currentTenant.value.logoUrl || ''
  logoStorageValue.value = currentTenant.value.logoUrl || ''
  primaryColor.value = currentTenant.value.primaryColor || DEFAULT_BRANDING.primaryColor
  secondaryColor.value = currentTenant.value.secondaryColor || DEFAULT_BRANDING.secondaryColor
}

/** Check if form has changes */
const hasChanges = computed(() => {
  if (!currentTenant.value) return false
  return (
    logoStorageValue.value !== (currentTenant.value.logoUrl || '') ||
    primaryColor.value !== (currentTenant.value.primaryColor || DEFAULT_BRANDING.primaryColor) ||
    secondaryColor.value !== (currentTenant.value.secondaryColor || DEFAULT_BRANDING.secondaryColor)
  )
})

/** Handle file selection */
async function handleFileSelect(event: Event): Promise<void> {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (file) {
    await uploadLogo(file)
  }
  // Reset input
  if (target) target.value = ''
}

/** Handle drag and drop */
function handleDragOver(event: DragEvent): void {
  event.preventDefault()
  isDragOver.value = true
}

function handleDragLeave(): void {
  isDragOver.value = false
}

async function handleDrop(event: DragEvent): Promise<void> {
  event.preventDefault()
  isDragOver.value = false
  const file = event.dataTransfer?.files?.[0]
  if (file) {
    await uploadLogo(file)
  }
}

/** Upload logo file */
async function uploadLogo(file: File): Promise<void> {
  // Validate file type
  const allowedTypes = ['image/png', 'image/jpeg', 'image/svg+xml', 'image/webp']
  if (!allowedTypes.includes(file.type)) {
    uploadError.value = t('branding.messages.logoUploadError')
    return
  }

  // Validate file size (max 2MB)
  if (file.size > 2 * 1024 * 1024) {
    uploadError.value = t('branding.messages.logoUploadError')
    return
  }

  isUploading.value = true
  uploadError.value = null

  try {
    const result = await brandingService.uploadTenantLogo(tenantId.value, file)
    logoStorageValue.value = result.fileId
    logoUrl.value = await brandingService.getFileDownloadUrl(result.fileId)
  } catch (err) {
    uploadError.value = t('branding.messages.logoUploadError')
  } finally {
    isUploading.value = false
  }
}

/** Remove logo */
function removeLogo(): void {
  logoUrl.value = ''
  logoStorageValue.value = ''
}

/** Trigger file input click */
function triggerFileInput(): void {
  fileInputRef.value?.click()
}

/** Save branding */
async function saveBranding(): Promise<void> {
  isSaving.value = true
  saveError.value = null
  saveSuccess.value = false

  try {
    const request: UpdateTenantBrandingRequest = {
      logoUrl: logoStorageValue.value || undefined,
      primaryColor: primaryColor.value,
      secondaryColor: secondaryColor.value,
    }

    await tenantStore.updateBranding(tenantId.value, request)
    saveSuccess.value = true

    // Clear success after 4 seconds
    setTimeout(() => {
      saveSuccess.value = false
    }, 4000)
  } catch (err) {
    saveError.value = t('branding.messages.saveError')
  } finally {
    isSaving.value = false
  }
}

/** Reset to defaults */
function resetToDefaults(): void {
  primaryColor.value = DEFAULT_BRANDING.primaryColor
  secondaryColor.value = DEFAULT_BRANDING.secondaryColor
  logoUrl.value = ''
  logoStorageValue.value = ''
}

/** Go back to tenant detail */
function goBack(): void {
  router.push({ name: 'TenantDetail', params: { id: tenantId.value } })
}

/** Lighten a hex color */
function lightenColor(hex: string, percent: number): string {
  const cleanHex = hex.replace('#', '')
  const num = parseInt(cleanHex, 16)
  let r = (num >> 16) & 0xff
  let g = (num >> 8) & 0xff
  let b = num & 0xff
  r = Math.min(255, Math.max(0, Math.round(r + (r * percent) / 100)))
  g = Math.min(255, Math.max(0, Math.round(g + (g * percent) / 100)))
  b = Math.min(255, Math.max(0, Math.round(b + (b * percent) / 100)))
  return `#${((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1)}`
}

/** Darken a hex color */
function darkenColor(hex: string, percent: number): string {
  return lightenColor(hex, -percent)
}

/** Load data on mount */
onMounted(async () => {
  if (!currentTenant.value || currentTenant.value.id !== tenantId.value) {
    await tenantStore.loadTenantDetail(tenantId.value)
  }
  populateForm()
})

/** Watch for tenant changes */
watch(currentTenant, () => {
  populateForm()
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <!-- Back button -->
      <button
        type="button"
        class="mb-4 flex items-center gap-1 text-sm text-tertiary hover:text-primary"
        @click="goBack"
      >
        <i class="pi pi-arrow-right rotate-180 text-xs rtl:rotate-0"></i>
        {{ t('branding.backToDetail') }}
      </button>

      <!-- Loading -->
      <div v-if="tenantLoading && !currentTenant" class="flex items-center justify-center py-20">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Content -->
      <template v-else-if="currentTenant">
        <!-- Header -->
        <div class="mb-6">
          <h1 class="text-2xl font-bold text-secondary">
            {{ t('branding.title') }}
          </h1>
          <p class="mt-1 text-sm text-tertiary">
            {{ t('branding.subtitle', { tenant: getTenantName() }) }}
          </p>
        </div>

        <!-- Success message -->
        <div
          v-if="saveSuccess"
          class="mb-6 rounded-lg border border-emerald-200 bg-emerald-50 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-check-circle text-emerald-600"></i>
            <p class="text-sm text-emerald-700">{{ t('branding.messages.saveSuccess') }}</p>
          </div>
        </div>

        <!-- Error message -->
        <div
          v-if="saveError"
          class="mb-6 rounded-lg border border-danger/20 bg-danger/5 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-exclamation-triangle text-danger"></i>
            <p class="text-sm text-danger">{{ saveError }}</p>
          </div>
        </div>

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-5">
          <!-- Editor Panel (3/5) -->
          <div class="lg:col-span-3 space-y-6">
            <!-- Logo Upload -->
            <div class="rounded-lg border border-surface-dim bg-white p-6">
              <h2 class="mb-4 text-lg font-semibold text-secondary">
                {{ t('branding.sections.logo') }}
              </h2>

              <!-- Current logo display -->
              <div v-if="logoUrl" class="mb-4 flex items-center gap-4">
                <div class="flex h-20 w-20 items-center justify-center overflow-hidden rounded-lg border border-surface-dim bg-white p-2">
                  <img :src="logoUrl" :alt="t('branding.labels.currentLogo')" class="h-full w-full object-contain" />
                </div>
                <div>
                  <p class="text-sm font-medium text-secondary">{{ t('branding.labels.currentLogo') }}</p>
                  <button
                    type="button"
                    class="mt-1 text-xs text-danger hover:text-danger/80"
                    @click="removeLogo"
                  >
                    <i class="pi pi-trash text-xs"></i>
                    {{ t('branding.actions.removeLogo') }}
                  </button>
                </div>
              </div>

              <!-- Upload area -->
              <div
                class="relative rounded-lg border-2 border-dashed p-8 text-center transition-colors"
                :class="isDragOver ? 'border-primary bg-primary/5' : 'border-surface-dim hover:border-primary/50'"
                @dragover="handleDragOver"
                @dragleave="handleDragLeave"
                @drop="handleDrop"
              >
                <input
                  ref="fileInputRef"
                  type="file"
                  accept="image/png,image/jpeg,image/svg+xml,image/webp"
                  class="hidden"
                  @change="handleFileSelect"
                />

                <div v-if="isUploading" class="flex flex-col items-center gap-2">
                  <i class="pi pi-spin pi-spinner text-2xl text-primary"></i>
                  <p class="text-sm text-tertiary">{{ t('branding.labels.uploading') }}</p>
                </div>

                <div v-else class="flex flex-col items-center gap-2">
                  <div class="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                    <i class="pi pi-cloud-upload text-xl text-primary"></i>
                  </div>
                  <p class="text-sm font-medium text-secondary">
                    {{ t('branding.labels.dragDrop') }}
                  </p>
                  <p class="text-xs text-tertiary">
                    {{ t('branding.labels.orClickToUpload') }}
                  </p>
                  <button
                    type="button"
                    class="mt-2 rounded-lg bg-primary/10 px-4 py-2 text-sm font-medium text-primary hover:bg-primary/20"
                    @click="triggerFileInput"
                  >
                    {{ t('branding.actions.selectFile') }}
                  </button>
                  <p class="mt-1 text-[10px] text-tertiary">
                    {{ t('branding.labels.fileRequirements') }}
                  </p>
                </div>

                <!-- Upload error -->
                <p v-if="uploadError" class="mt-2 text-xs text-danger">{{ uploadError }}</p>
              </div>
            </div>

            <!-- Color Pickers -->
            <div class="rounded-lg border border-surface-dim bg-white p-6">
              <h2 class="mb-4 text-lg font-semibold text-secondary">
                {{ t('branding.sections.colors') }}
              </h2>

              <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
                <!-- Primary Color -->
                <div>
                  <label class="mb-2 block text-sm font-medium text-secondary">
                    {{ t('branding.fields.primaryColor') }}
                  </label>
                  <p class="mb-3 text-xs text-tertiary">
                    {{ t('branding.descriptions.primaryColor') }}
                  </p>
                  <div class="flex items-center gap-3">
                    <input
                      v-model="primaryColor"
                      type="color"
                      class="h-12 w-12 cursor-pointer rounded-lg border border-surface-dim"
                    />
                    <input
                      v-model="primaryColor"
                      type="text"
                      dir="ltr"
                      class="flex-1 rounded-lg border border-surface-dim px-3 py-2 font-mono text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                      pattern="^#[0-9A-Fa-f]{6}$"
                    />
                  </div>
                  <!-- Color variants preview -->
                  <div class="mt-3 flex gap-1">
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: lightenColor(primaryColor, 30) }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: lightenColor(primaryColor, 15) }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: primaryColor }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: darkenColor(primaryColor, 15) }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: darkenColor(primaryColor, 30) }"
                    ></div>
                  </div>
                </div>

                <!-- Secondary Color -->
                <div>
                  <label class="mb-2 block text-sm font-medium text-secondary">
                    {{ t('branding.fields.secondaryColor') }}
                  </label>
                  <p class="mb-3 text-xs text-tertiary">
                    {{ t('branding.descriptions.secondaryColor') }}
                  </p>
                  <div class="flex items-center gap-3">
                    <input
                      v-model="secondaryColor"
                      type="color"
                      class="h-12 w-12 cursor-pointer rounded-lg border border-surface-dim"
                    />
                    <input
                      v-model="secondaryColor"
                      type="text"
                      dir="ltr"
                      class="flex-1 rounded-lg border border-surface-dim px-3 py-2 font-mono text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                      pattern="^#[0-9A-Fa-f]{6}$"
                    />
                  </div>
                  <!-- Color variants preview -->
                  <div class="mt-3 flex gap-1">
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: lightenColor(secondaryColor, 30) }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: lightenColor(secondaryColor, 15) }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: secondaryColor }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: darkenColor(secondaryColor, 15) }"
                    ></div>
                    <div
                      class="h-6 flex-1 rounded"
                      :style="{ backgroundColor: darkenColor(secondaryColor, 30) }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Action buttons -->
            <div class="flex items-center justify-between">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-muted"
                @click="resetToDefaults"
              >
                <i class="pi pi-refresh text-xs"></i>
                {{ t('branding.actions.resetDefaults') }}
              </button>
              <div class="flex items-center gap-3">
                <button
                  type="button"
                  class="rounded-lg border border-surface-dim px-4 py-2 text-sm text-secondary hover:bg-surface-muted"
                  :disabled="!hasChanges"
                  @click="populateForm"
                >
                  {{ t('common.cancel') }}
                </button>
                <button
                  type="button"
                  class="flex items-center gap-2 rounded-lg bg-primary px-6 py-2 text-sm font-bold text-white hover:bg-primary-dark disabled:opacity-50"
                  :disabled="isSaving || !hasChanges"
                  @click="saveBranding"
                >
                  <i v-if="isSaving" class="pi pi-spin pi-spinner text-xs"></i>
                  {{ t('common.save') }}
                </button>
              </div>
            </div>
          </div>

          <!-- Live Preview Panel (2/5) -->
          <div class="lg:col-span-2">
            <div class="sticky top-24 space-y-4">
              <div class="rounded-lg border border-surface-dim bg-white p-4">
                <h3 class="mb-4 text-sm font-semibold text-secondary">
                  {{ t('branding.sections.preview') }}
                </h3>

                <!-- Mini app preview -->
                <div class="overflow-hidden rounded-lg border border-surface-dim">
                  <!-- Header preview -->
                  <div
                    class="flex items-center gap-3 px-4 py-3"
                    :style="{ backgroundColor: secondaryColor }"
                  >
                    <div
                      v-if="logoUrl"
                      class="flex h-8 w-8 items-center justify-center overflow-hidden rounded bg-white/20"
                    >
                      <img :src="logoUrl" alt="Logo" class="h-full w-full object-contain" />
                    </div>
                    <div
                      v-else
                      class="flex h-8 w-8 items-center justify-center rounded bg-white/20"
                    >
                      <i class="pi pi-building text-sm text-white/70"></i>
                    </div>
                    <div>
                      <p class="text-xs font-bold text-white">{{ getTenantName() || 'Tendex AI' }}</p>
                      <p class="text-[10px] text-white/60">{{ t('branding.preview.platform') }}</p>
                    </div>
                  </div>

                  <!-- Sidebar preview -->
                  <div class="flex">
                    <div class="w-12 border-e border-surface-dim bg-slate-50 py-3">
                      <div class="mb-2 flex justify-center">
                        <div class="h-2 w-6 rounded bg-slate-200"></div>
                      </div>
                      <div class="mb-2 flex justify-center">
                        <div class="h-2 w-6 rounded bg-slate-200"></div>
                      </div>
                      <div class="flex justify-center">
                        <div
                          class="h-2 w-6 rounded"
                          :style="{ backgroundColor: primaryColor }"
                        ></div>
                      </div>
                    </div>

                    <!-- Content preview -->
                    <div class="flex-1 p-3">
                      <div class="mb-3 h-2 w-20 rounded bg-slate-200"></div>
                      <div class="mb-2 grid grid-cols-3 gap-2">
                        <div class="rounded-md border border-surface-dim p-2">
                          <div class="mb-1 h-1.5 w-8 rounded bg-slate-200"></div>
                          <div
                            class="h-3 w-6 rounded text-[8px] font-bold"
                            :style="{ color: primaryColor }"
                          >
                            24
                          </div>
                        </div>
                        <div class="rounded-md border border-surface-dim p-2">
                          <div class="mb-1 h-1.5 w-8 rounded bg-slate-200"></div>
                          <div
                            class="h-3 w-6 rounded text-[8px] font-bold"
                            :style="{ color: primaryColor }"
                          >
                            12
                          </div>
                        </div>
                        <div class="rounded-md border border-surface-dim p-2">
                          <div class="mb-1 h-1.5 w-8 rounded bg-slate-200"></div>
                          <div
                            class="h-3 w-6 rounded text-[8px] font-bold"
                            :style="{ color: primaryColor }"
                          >
                            8
                          </div>
                        </div>
                      </div>
                      <!-- Button preview -->
                      <div class="flex gap-2">
                        <div
                          class="rounded px-3 py-1 text-[8px] font-bold text-white"
                          :style="{ backgroundColor: primaryColor }"
                        >
                          {{ t('branding.preview.button') }}
                        </div>
                        <div
                          class="rounded border px-3 py-1 text-[8px] font-bold"
                          :style="{ borderColor: primaryColor, color: primaryColor }"
                        >
                          {{ t('branding.preview.secondary') }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Color palette summary -->
              <div class="rounded-lg border border-surface-dim bg-white p-4">
                <h3 class="mb-3 text-sm font-semibold text-secondary">
                  {{ t('branding.sections.palette') }}
                </h3>
                <div class="space-y-2">
                  <div class="flex items-center gap-3">
                    <div class="h-8 w-8 rounded" :style="{ backgroundColor: primaryColor }"></div>
                    <div>
                      <p class="text-xs font-medium text-secondary">{{ t('branding.fields.primaryColor') }}</p>
                      <p class="font-mono text-[10px] text-tertiary" dir="ltr">{{ primaryColor }}</p>
                    </div>
                  </div>
                  <div class="flex items-center gap-3">
                    <div class="h-8 w-8 rounded" :style="{ backgroundColor: secondaryColor }"></div>
                    <div>
                      <p class="text-xs font-medium text-secondary">{{ t('branding.fields.secondaryColor') }}</p>
                      <p class="font-mono text-[10px] text-tertiary" dir="ltr">{{ secondaryColor }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
