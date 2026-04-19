<script setup lang="ts">
/**
 * AiTextHelper - Reusable AI Writing Assistant
 *
 * A lightweight, reusable AI writing assistant that can be attached to any textarea
 * in the platform. It provides:
 * - AI text generation based on context
 * - Text improvement/refinement
 * - Quick action buttons (expand, summarize, formalize)
 *
 * Usage:
 *   <AiTextHelper
 *     :context="{ projectName: '...', fieldName: 'وصف المشروع' }"
 *     :current-text="myText"
 *     @text-generated="myText = $event"
 *   />
 */
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '@/services/http'

const props = defineProps<{
  /** Context information to help AI generate relevant content */
  context: {
    projectName?: string
    projectDescription?: string
    fieldName: string
    fieldPurpose?: string
    competitionType?: string
    additionalContext?: string
  }
  /** Current text content in the field */
  currentText: string
  /** Whether to show in compact/inline mode */
  compact?: boolean
  /**
   * Issue 20 Fix: Maximum characters allowed for the generated text.
   * When set, this constraint is sent to the backend to limit AI output.
   */
  maxCharacters?: number
}>()

const emit = defineEmits<{
  (e: 'text-generated', text: string): void
}>()

const { locale } = useI18n()

const isGenerating = ref(false)
const isExpanded = ref(false)
const customPrompt = ref('')
const error = ref('')
const selectedAction = ref<string | null>(null)

const hasContent = computed(() => props.currentText && props.currentText.trim().length > 0)

const quickActions = computed(() => [
  {
    key: 'generate',
    icon: 'pi-sparkles',
    label: locale.value === 'ar' ? 'توليد نص' : 'Generate Text',
    description: locale.value === 'ar' ? 'إنشاء محتوى جديد بالذكاء الاصطناعي' : 'Create new content with AI',
    show: !hasContent.value,
  },
  {
    key: 'improve',
    icon: 'pi-pencil',
    label: locale.value === 'ar' ? 'تحسين النص' : 'Improve Text',
    description: locale.value === 'ar' ? 'تحسين جودة النص الحالي' : 'Improve current text quality',
    show: hasContent.value,
  },
  {
    key: 'expand',
    icon: 'pi-arrows-alt',
    label: locale.value === 'ar' ? 'توسيع النص' : 'Expand Text',
    description: locale.value === 'ar' ? 'إضافة تفاصيل أكثر' : 'Add more details',
    show: hasContent.value,
  },
  {
    key: 'summarize',
    icon: 'pi-align-center',
    label: locale.value === 'ar' ? 'تلخيص' : 'Summarize',
    description: locale.value === 'ar' ? 'اختصار النص مع الحفاظ على المعنى' : 'Shorten while keeping meaning',
    show: hasContent.value,
  },
  {
    key: 'formalize',
    icon: 'pi-briefcase',
    label: locale.value === 'ar' ? 'صياغة رسمية' : 'Formalize',
    description: locale.value === 'ar' ? 'تحويل إلى لغة رسمية حكومية' : 'Convert to formal government language',
    show: hasContent.value,
  },
])

const visibleActions = computed(() => quickActions.value.filter(a => a.show))

async function handleAction(actionKey: string) {
  selectedAction.value = actionKey
  isGenerating.value = true
  error.value = ''

  try {
    const response = await http.post<{ isSuccess: boolean; generatedText: string; errorMessage?: string }>(
      '/v1/ai/text/assist',
      {
        action: actionKey,
        currentText: props.currentText || '',
        fieldName: props.context.fieldName,
        fieldPurpose: props.context.fieldPurpose || '',
        projectName: props.context.projectName || '',
        projectDescription: props.context.projectDescription || '',
        competitionType: props.context.competitionType || '',
        additionalContext: props.context.additionalContext || '',
        customPrompt: customPrompt.value || '',
        language: 'ar',
        maxCharacters: props.maxCharacters || 0,
      },
      { timeout: 120_000 },
    )

    if (response.data.isSuccess && response.data.generatedText) {
      emit('text-generated', response.data.generatedText)
      customPrompt.value = ''
      isExpanded.value = false
    } else {
      error.value = response.data.errorMessage || (locale.value === 'ar' ? 'فشل في توليد النص' : 'Failed to generate text')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage
      || (locale.value === 'ar' ? 'حدث خطأ أثناء الاتصال بالذكاء الاصطناعي' : 'Error connecting to AI service')
  } finally {
    isGenerating.value = false
    selectedAction.value = null
  }
}

async function handleCustomPrompt() {
  if (!customPrompt.value.trim()) return
  selectedAction.value = 'custom'
  isGenerating.value = true
  error.value = ''

  try {
    const response = await http.post<{ isSuccess: boolean; generatedText: string; errorMessage?: string }>(
      '/v1/ai/text/assist',
      {
        action: 'custom',
        currentText: props.currentText || '',
        fieldName: props.context.fieldName,
        fieldPurpose: props.context.fieldPurpose || '',
        projectName: props.context.projectName || '',
        projectDescription: props.context.projectDescription || '',
        competitionType: props.context.competitionType || '',
        additionalContext: props.context.additionalContext || '',
        customPrompt: customPrompt.value,
        language: 'ar',
        maxCharacters: props.maxCharacters || 0,
      },
      { timeout: 120_000 },
    )

    if (response.data.isSuccess && response.data.generatedText) {
      emit('text-generated', response.data.generatedText)
      customPrompt.value = ''
    } else {
      error.value = response.data.errorMessage || (locale.value === 'ar' ? 'فشل في توليد النص' : 'Failed to generate text')
    }
  } catch (err: any) {
    error.value = err?.response?.data?.errorMessage
      || (locale.value === 'ar' ? 'حدث خطأ أثناء الاتصال بالذكاء الاصطناعي' : 'Error connecting to AI service')
  } finally {
    isGenerating.value = false
    selectedAction.value = null
  }
}
</script>

<template>
  <div class="ai-text-helper">
    <!-- Toggle Button -->
    <button
      type="button"
      class="group inline-flex items-center gap-1.5 rounded-lg border border-ai-200 bg-gradient-to-r from-ai-50 to-white px-2.5 py-1 text-[11px] font-semibold text-ai-600 transition-all duration-200 hover:border-ai-300 hover:shadow-sm"
      @click="isExpanded = !isExpanded"
    >
      <i class="pi pi-sparkles text-[10px] transition-transform duration-200 group-hover:scale-110"></i>
      <span>{{ locale === 'ar' ? 'مساعد الذكاء الاصطناعي' : 'AI Assistant' }}</span>
      <i
        class="pi text-[8px] transition-transform duration-200"
        :class="isExpanded ? 'pi-chevron-up' : 'pi-chevron-down'"
      ></i>
    </button>

    <!-- Expanded Panel -->
    <Transition name="ai-slide">
      <div
        v-if="isExpanded"
        class="mt-2 rounded-xl border border-ai-200 bg-gradient-to-br from-ai-50/50 via-white to-ai-50/30 p-3"
      >
        <!-- Quick Actions -->
        <div class="mb-2 flex flex-wrap gap-1.5">
          <button
            v-for="action in visibleActions"
            :key="action.key"
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-ai-200 bg-white px-2.5 py-1.5 text-[11px] font-medium text-ai-600 transition-all hover:border-ai-300 hover:bg-ai-50 disabled:opacity-50"
            :disabled="isGenerating"
            :title="action.description"
            @click="handleAction(action.key)"
          >
            <i
              class="pi text-[10px]"
              :class="isGenerating && selectedAction === action.key ? 'pi-spin pi-spinner' : action.icon"
            ></i>
            {{ action.label }}
          </button>
        </div>

        <!-- Custom Prompt Input -->
        <div class="flex gap-2">
          <input
            v-model="customPrompt"
            type="text"
            class="flex-1 rounded-lg border border-ai-200 bg-white px-3 py-1.5 text-xs text-secondary-700 placeholder:text-secondary-400 focus:border-ai-400 focus:outline-none focus:ring-1 focus:ring-ai-200"
            :placeholder="locale === 'ar' ? 'اكتب تعليمات مخصصة للذكاء الاصطناعي...' : 'Write custom AI instructions...'"
            :disabled="isGenerating"
            @keyup.enter="handleCustomPrompt"
          />
          <button
            type="button"
            class="rounded-lg bg-ai-500 px-3 py-1.5 text-[11px] font-semibold text-white transition-colors hover:bg-ai-600 disabled:opacity-50"
            :disabled="isGenerating || !customPrompt.trim()"
            @click="handleCustomPrompt"
          >
            <i class="pi text-[10px]" :class="isGenerating && selectedAction === 'custom' ? 'pi-spin pi-spinner' : 'pi-send'"></i>
          </button>
        </div>

        <!-- Error -->
        <div v-if="error" class="mt-2 rounded-lg border border-danger/20 bg-danger/5 px-3 py-1.5 text-[11px] text-danger">
          <i class="pi pi-exclamation-circle me-1 text-[10px]"></i>
          {{ error }}
        </div>

        <!-- Loading indicator -->
        <div v-if="isGenerating" class="mt-2 flex items-center gap-2 text-[11px] text-ai-500">
          <i class="pi pi-spin pi-spinner text-[10px]"></i>
          {{ locale === 'ar' ? 'جارٍ التوليد بالذكاء الاصطناعي...' : 'Generating with AI...' }}
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.ai-slide-enter-active,
.ai-slide-leave-active {
  transition: all 0.25s ease;
}

.ai-slide-enter-from,
.ai-slide-leave-to {
  max-height: 0;
  opacity: 0;
  transform: translateY(-4px);
  overflow: hidden;
}

.ai-slide-enter-to,
.ai-slide-leave-from {
  max-height: 300px;
  opacity: 1;
  transform: translateY(0);
}
</style>
