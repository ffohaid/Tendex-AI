<script setup lang="ts">
/**
 * Step 5: Mandatory Attachments Selection.
 *
 * Issue 25 Fix: This step is now focused ONLY on selecting which
 * mandatory attachment types competitors must submit with their offers.
 * File upload functionality has been separated into a dedicated
 * section that only appears for supplementary documents.
 *
 * The required attachment types checklist defines what competitors
 * must provide, NOT what the entity uploads.
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { attachmentsSchema } from '@/validations/rfp'
import { useRfpStore } from '@/stores/rfp'
import type { RfpAttachment } from '@/types/rfp'
import AiAttachmentRecommender from './AiAttachmentRecommender.vue'

const { t } = useI18n()
const rfpStore = useRfpStore()

const schema = toTypedSchema(attachmentsSchema)

const { validate, setFieldValue } = useForm({
  validationSchema: schema,
  initialValues: { ...rfpStore.formData.attachments },
  validateOnMount: false,
})

const isDragOver = ref(false)
const uploadError = ref('')
const showSupplementaryUpload = ref(false)

/** Required attachments list per PRD */
const requiredAttachments = computed(() => [
  { key: 'technical_proposal_form', label: t('rfp.requiredAttachments.technicalProposalForm') },
  { key: 'financial_proposal_form', label: t('rfp.requiredAttachments.financialProposalForm') },
  { key: 'bank_guarantee_form', label: t('rfp.requiredAttachments.bankGuaranteeForm') },
  { key: 'compliance_declaration', label: t('rfp.requiredAttachments.complianceDeclaration') },
  { key: 'conflict_of_interest', label: t('rfp.requiredAttachments.conflictOfInterest') },
  { key: 'company_profile_form', label: t('rfp.requiredAttachments.companyProfileForm') },
  { key: 'experience_form', label: t('rfp.requiredAttachments.experienceForm') },
  { key: 'team_qualifications', label: t('rfp.requiredAttachments.teamQualifications') },
  { key: 'sla_template', label: t('rfp.requiredAttachments.slaTemplate') },
  { key: 'nda_template', label: t('rfp.requiredAttachments.ndaTemplate') },
])

/** Check if a required attachment type is selected */
function isRequiredAttachmentSelected(key: string): boolean {
  return rfpStore.formData.attachments.requiredAttachmentTypes?.includes(key) ?? false
}

/** Toggle a required attachment type selection */
function toggleRequiredType(key: string) {
  rfpStore.toggleRequiredAttachmentType(key)
}

/** Allowed file types */
const allowedTypes = [
  'application/pdf',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'image/png',
  'image/jpeg',
]
const maxFileSize = 25 * 1024 * 1024 // 25 MB

/** Format file size */
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

/** Get file icon */
function getFileIcon(fileType: string): string {
  if (fileType.includes('pdf')) return 'pi pi-file-pdf'
  if (fileType.includes('word') || fileType.includes('document')) return 'pi pi-file-word'
  if (fileType.includes('excel') || fileType.includes('sheet')) return 'pi pi-file-excel'
  if (fileType.includes('image')) return 'pi pi-image'
  return 'pi pi-file'
}

/** Handle file selection */
function handleFiles(files: FileList | null) {
  if (!files) return
  uploadError.value = ''

  Array.from(files).forEach((file) => {
    // Validate file type
    if (!allowedTypes.includes(file.type)) {
      uploadError.value = t('rfp.errors.invalidFileType')
      return
    }

    // Validate file size
    if (file.size > maxFileSize) {
      uploadError.value = t('rfp.errors.fileTooLarge')
      return
    }

    // Create attachment record
    const attachment: RfpAttachment = {
      id: `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`,
      name: file.name,
      fileUrl: URL.createObjectURL(file),
      fileSize: file.size,
      fileType: file.type,
      isRequired: false,
      uploadedAt: new Date().toISOString(),
      uploadedBy: 'current-user',
    }

    rfpStore.addAttachment(attachment)
  })
}

/** Handle drag events */
function onDragOver(event: DragEvent) {
  event.preventDefault()
  isDragOver.value = true
}

function onDragLeave() {
  isDragOver.value = false
}

function onDrop(event: DragEvent) {
  event.preventDefault()
  isDragOver.value = false
  handleFiles(event.dataTransfer?.files || null)
}

/** Handle file input change */
function onFileInputChange(event: Event) {
  const input = event.target as HTMLInputElement
  handleFiles(input.files)
  input.value = '' // Reset for re-upload
}

/** Remove attachment */
function removeAttachment(id: string) {
  rfpStore.removeAttachment(id)
}

/** Handle AI attachment recommendations */
function handleAiAttachments(keys: string[]) {
  // Merge AI recommendations idempotently so duplicate keys never toggle a
  // previously selected attachment type back off.
  const uniqueKeys = Array.from(new Set(keys.filter(Boolean)))
  const existing = rfpStore.formData.attachments.requiredAttachmentTypes || []
  const merged = Array.from(new Set([...existing, ...uniqueKeys]))

  rfpStore.updateAttachments({ requiredAttachmentTypes: merged })
}

/** Count selected required attachments */
const selectedCount = computed(() => {
  return rfpStore.formData.attachments.requiredAttachmentTypes?.length ?? 0
})

defineExpose({
  validate: async () => {
    /**
     * CRITICAL FIX: Sync Pinia store attachments into VeeValidate before
     * running validation. Attachments are managed directly in the Pinia
     * store, so VeeValidate's internal state becomes stale.
     */
    setFieldValue('files', rfpStore.formData.attachments.files)

    const result = await validate()
    return result.valid
  },
})
</script>

<template>
  <div class="space-y-6">
    <div class="mb-6">
      <h2 class="text-xl font-bold text-secondary">
        {{ t('rfp.steps.attachments') }}
      </h2>
      <p class="mt-1 text-sm text-tertiary">
        {{ t('rfp.steps.attachmentsDesc') }}
      </p>
    </div>

    <!-- AI Attachment Recommender -->
    <AiAttachmentRecommender @select-attachments="handleAiAttachments" />

    <!-- Required attachments checklist (interactive checkboxes) -->
    <div class="rounded-lg border border-surface-dim bg-surface-muted p-4">
      <div class="mb-3 flex items-center justify-between">
        <h3 class="text-sm font-bold text-secondary">
          {{ t('rfp.labels.requiredAttachments') }}
        </h3>
        <span class="text-xs text-tertiary">
          {{ selectedCount }} / {{ requiredAttachments.length }}
        </span>
      </div>
      <p class="mb-4 text-xs text-tertiary">
        {{ $t('rfp.messages.selectRequiredAttachments', 'حدد المستندات الإلزامية التي يجب على المتنافسين تقديمها مع عروضهم') }}
      </p>
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label
          v-for="req in requiredAttachments"
          :key="req.key"
          class="flex cursor-pointer items-center gap-3 rounded-lg border border-surface-dim bg-white p-3 transition-all hover:border-primary/40 hover:bg-primary/5"
          :class="{ 'border-primary bg-primary/5': isRequiredAttachmentSelected(req.key) }"
        >
          <input
            type="checkbox"
            :checked="isRequiredAttachmentSelected(req.key)"
            class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            @change="toggleRequiredType(req.key)"
          />
          <span
            class="text-sm"
            :class="isRequiredAttachmentSelected(req.key) ? 'font-medium text-secondary' : 'text-tertiary'"
          >
            {{ req.label }}
          </span>
        </label>
      </div>
    </div>

    <!-- Supplementary Documents Section (collapsible) -->
    <div class="rounded-lg border border-surface-dim bg-white">
      <button
        type="button"
        class="flex w-full items-center justify-between p-4 text-start transition-colors hover:bg-surface-muted/50"
        @click="showSupplementaryUpload = !showSupplementaryUpload"
      >
        <div class="flex items-center gap-2">
          <i class="pi pi-paperclip text-sm text-primary"></i>
          <span class="text-sm font-bold text-secondary">
            {{ $t('rfp.labels.supplementaryDocuments', 'مستندات تكميلية للكراسة') }}
          </span>
          <span v-if="rfpStore.formData.attachments.files.length > 0" class="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
            {{ rfpStore.formData.attachments.files.length }}
          </span>
        </div>
        <i
          class="pi text-xs text-tertiary transition-transform"
          :class="showSupplementaryUpload ? 'pi-chevron-up' : 'pi-chevron-down'"
        ></i>
      </button>

      <div v-if="showSupplementaryUpload" class="border-t border-surface-dim p-4 space-y-4">
        <p class="text-xs text-tertiary">
          {{ $t('rfp.messages.supplementaryDocsHint', 'يمكنك إرفاق مستندات تكميلية خاصة بالكراسة مثل المخططات أو المواصفات الفنية التفصيلية') }}
        </p>

        <!-- Drop zone -->
        <div
          class="relative rounded-lg border-2 border-dashed p-8 text-center transition-colors"
          :class="{
            'border-primary bg-primary/5': isDragOver,
            'border-surface-dim hover:border-primary/40': !isDragOver,
          }"
          @dragover="onDragOver"
          @dragleave="onDragLeave"
          @drop="onDrop"
        >
          <input
            id="fileUpload"
            type="file"
            multiple
            :accept="allowedTypes.join(',')"
            class="absolute inset-0 cursor-pointer opacity-0"
            @change="onFileInputChange"
          />
          <div class="pointer-events-none">
            <i class="pi pi-cloud-upload mb-3 text-4xl text-tertiary"></i>
            <p class="text-sm font-medium text-secondary">
              {{ t('rfp.messages.dropFilesHere') }}
            </p>
            <p class="mt-1 text-xs text-tertiary">
              {{ t('rfp.messages.allowedFileTypes') }}
            </p>
            <p class="text-xs text-tertiary">
              {{ t('rfp.messages.maxFileSize') }}
            </p>
          </div>
        </div>

        <!-- Upload error -->
        <p v-if="uploadError" class="text-sm text-danger">
          <i class="pi pi-exclamation-circle me-1"></i>
          {{ uploadError }}
        </p>

        <!-- Uploaded files list -->
        <div v-if="rfpStore.formData.attachments.files.length > 0" class="space-y-2">
          <h3 class="text-sm font-bold text-secondary">
            {{ t('rfp.labels.uploadedFiles') }}
            ({{ rfpStore.formData.attachments.files.length }})
          </h3>

          <div
            v-for="file in rfpStore.formData.attachments.files"
            :key="file.id"
            class="flex items-center gap-3 rounded-lg border border-surface-dim bg-white p-3 transition-colors hover:bg-surface-muted/50"
          >
            <i :class="[getFileIcon(file.fileType), 'text-xl text-primary']"></i>
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium text-secondary">{{ file.name }}</p>
              <p class="text-xs text-tertiary">{{ formatFileSize(file.fileSize) }}</p>
            </div>
            <button
              type="button"
              class="flex h-8 w-8 items-center justify-center rounded-lg text-danger transition-colors hover:bg-danger/10"
              :title="t('common.delete')"
              @click="removeAttachment(file.id)"
            >
              <i class="pi pi-trash text-sm"></i>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
