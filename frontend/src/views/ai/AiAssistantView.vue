<script setup lang="ts">
/**
 * AiAssistantView — AI Chat Assistant Page.
 *
 * TASK-904: Full-page AI assistant with chat interface.
 * Connects to AiGatewayEndpoints (/api/v1/ai/completions) for generating responses.
 *
 * Features:
 * - Chat-style interface with message bubbles
 * - Quick action suggestions for common tasks
 * - Conversation history maintained in session
 * - AI status check for tenant
 * - Loading/streaming indicators
 * - Reference citations display
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 * - All AI outputs in Arabic per project standards
 *
 * All data is fetched dynamically from the API (no mock data).
 */
import { ref, computed, onMounted, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from '@/composables/useFormatters'
import { sendChatMessage, checkAiStatus } from '@/services/aiAssistantService'
import type { ChatMessage, QuickAction, AiChatRequest } from '@/types/aiAssistant'

const { t, locale } = useI18n()
const { formatDateTime } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const messages = ref<ChatMessage[]>([])
const inputText = ref('')
const isLoading = ref(false)
const isAiAvailable = ref(false)
const isCheckingStatus = ref(true)
const error = ref('')
const chatContainer = ref<HTMLElement | null>(null)
const isRtl = computed(() => locale.value === 'ar')

/* Tenant ID from localStorage */
const tenantId = computed(() => localStorage.getItem('tenant_id') || '')

/* ------------------------------------------------------------------ */
/*  Quick Actions                                                      */
/* ------------------------------------------------------------------ */
const quickActions = computed<QuickAction[]>(() => [
  {
    key: 'draft-rfp',
    labelAr: 'مساعدة في صياغة كراسة شروط',
    labelEn: 'Help draft a specification booklet',
    icon: 'pi-file-edit',
    prompt: 'ساعدني في صياغة كراسة شروط ومواصفات لمشروع رقمي جديد',
  },
  {
    key: 'evaluate-offer',
    labelAr: 'تحليل عرض فني',
    labelEn: 'Analyze a technical offer',
    icon: 'pi-chart-bar',
    prompt: 'أريد مساعدة في تحليل وتقييم عرض فني مقدم',
  },
  {
    key: 'compliance-check',
    labelAr: 'فحص الامتثال للأنظمة',
    labelEn: 'Compliance check',
    icon: 'pi-shield',
    prompt: 'ساعدني في التحقق من امتثال كراسة الشروط لنظام المنافسات والمشتريات الحكومية',
  },
  {
    key: 'boq-help',
    labelAr: 'إعداد جدول كميات',
    labelEn: 'Prepare Bill of Quantities',
    icon: 'pi-list',
    prompt: 'أحتاج مساعدة في إعداد جدول كميات لمشروع',
  },
  {
    key: 'general-question',
    labelAr: 'سؤال عام عن المنافسات',
    labelEn: 'General procurement question',
    icon: 'pi-question-circle',
    prompt: 'لدي سؤال عن إجراءات المنافسات والمشتريات الحكومية',
  },
  {
    key: 'report-analysis',
    labelAr: 'تحليل تقرير',
    labelEn: 'Analyze a report',
    icon: 'pi-chart-line',
    prompt: 'ساعدني في تحليل تقرير أداء المنافسات',
  },
])

/* ------------------------------------------------------------------ */
/*  System Prompt                                                      */
/* ------------------------------------------------------------------ */
const SYSTEM_PROMPT = `أنت مساعد ذكي متخصص في نظام المنافسات والمشتريات الحكومية في المملكة العربية السعودية.
مهامك تشمل:
- المساعدة في صياغة كراسات الشروط والمواصفات
- تحليل وتقييم العروض الفنية والمالية
- التحقق من الامتثال للأنظمة واللوائح
- إعداد جداول الكميات
- الإجابة على الاستفسارات المتعلقة بالمنافسات
يجب أن تكون جميع إجاباتك باللغة العربية.
استخدم الأرقام الإنجليزية (1, 2, 3) حصرياً.
كن دقيقاً ومهنياً في إجاباتك.`

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function generateId(): string {
  return `msg-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`
}

function scrollToBottom(): void {
  nextTick(() => {
    if (chatContainer.value) {
      chatContainer.value.scrollTop = chatContainer.value.scrollHeight
    }
  })
}

/* ------------------------------------------------------------------ */
/*  AI Status Check                                                    */
/* ------------------------------------------------------------------ */
async function checkStatus(): Promise<void> {
  isCheckingStatus.value = true
  try {
    if (tenantId.value) {
      const status = await checkAiStatus(tenantId.value)
      isAiAvailable.value = status.isAiAvailable
    } else {
      /* If no tenant ID, assume available for now */
      isAiAvailable.value = true
    }
  } catch {
    /* If status check fails, still allow usage */
    isAiAvailable.value = true
  } finally {
    isCheckingStatus.value = false
  }
}

/* ------------------------------------------------------------------ */
/*  Send Message                                                       */
/* ------------------------------------------------------------------ */
async function handleSendMessage(text?: string): Promise<void> {
  const messageText = text || inputText.value.trim()
  if (!messageText || isLoading.value) return

  /* Add user message */
  const userMessage: ChatMessage = {
    id: generateId(),
    role: 'user',
    content: messageText,
    timestamp: new Date().toISOString(),
  }
  messages.value.push(userMessage)
  inputText.value = ''
  scrollToBottom()

  /* Add placeholder assistant message */
  const assistantMessage: ChatMessage = {
    id: generateId(),
    role: 'assistant',
    content: '',
    timestamp: new Date().toISOString(),
    isStreaming: true,
  }
  messages.value.push(assistantMessage)
  scrollToBottom()

  isLoading.value = true
  error.value = ''

  try {
    /* Build conversation history for context */
    const conversationHistory = messages.value
      .filter((m) => m.role !== 'system' && m.id !== assistantMessage.id)
      .slice(-10)
      .map((m) => ({
        role: m.role,
        content: m.content,
      }))

    const request: AiChatRequest = {
      tenantId: tenantId.value,
      systemPrompt: SYSTEM_PROMPT,
      userPrompt: messageText,
      conversationHistory,
    }

    const response = await sendChatMessage(request)

    if (response.isSuccess) {
      assistantMessage.content = response.content
      assistantMessage.isStreaming = false
      assistantMessage.timestamp = new Date().toISOString()
    } else {
      assistantMessage.content = t('aiAssistant.errorResponse')
      assistantMessage.isStreaming = false
      error.value = response.errorMessage || t('aiAssistant.unknownError')
    }
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err)
    assistantMessage.content = t('aiAssistant.errorResponse')
    assistantMessage.isStreaming = false
    error.value = message
    console.error('[AiAssistantView] Failed to send message:', err)
  } finally {
    isLoading.value = false
    scrollToBottom()
  }
}

function handleQuickAction(action: QuickAction): void {
  handleSendMessage(action.prompt)
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault()
    handleSendMessage()
  }
}

function clearConversation(): void {
  messages.value = []
  error.value = ''
}

onMounted(() => {
  checkStatus()
})
</script>

<template>
  <div class="flex h-[calc(100vh-8rem)] flex-col">
    <!-- Page Header -->
    <div class="flex items-center justify-between border-b border-surface-dim pb-4">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          <i class="pi pi-sparkles me-2 text-info" />
          {{ t('aiAssistant.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('aiAssistant.subtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <!-- AI Status Indicator -->
        <div class="flex items-center gap-2 rounded-full border border-surface-dim px-3 py-1.5">
          <div
            class="h-2 w-2 rounded-full"
            :class="isCheckingStatus ? 'animate-pulse bg-warning' : isAiAvailable ? 'bg-success' : 'bg-danger'"
          />
          <span class="text-xs font-medium text-tertiary">
            {{ isCheckingStatus
              ? t('aiAssistant.checkingStatus')
              : isAiAvailable
                ? t('aiAssistant.available')
                : t('aiAssistant.unavailable')
            }}
          </span>
        </div>
        <button
          v-if="messages.length > 0"
          class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-3 py-1.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-muted"
          @click="clearConversation"
        >
          <i class="pi pi-trash text-xs" />
          {{ t('aiAssistant.clearChat') }}
        </button>
      </div>
    </div>

    <!-- Chat Area -->
    <div
      ref="chatContainer"
      class="flex-1 overflow-y-auto px-4 py-6"
    >
      <!-- Welcome State (no messages) -->
      <div v-if="messages.length === 0" class="flex h-full flex-col items-center justify-center">
        <div class="mb-6 flex h-20 w-20 items-center justify-center rounded-2xl bg-info/10">
          <i class="pi pi-sparkles text-4xl text-info" />
        </div>
        <h2 class="mb-2 text-xl font-bold text-secondary">
          {{ t('aiAssistant.welcome.title') }}
        </h2>
        <p class="mb-8 max-w-md text-center text-sm text-tertiary">
          {{ t('aiAssistant.welcome.subtitle') }}
        </p>

        <!-- Quick Actions Grid -->
        <div class="grid w-full max-w-2xl grid-cols-2 gap-3 md:grid-cols-3">
          <button
            v-for="action in quickActions"
            :key="action.key"
            class="flex flex-col items-center gap-2 rounded-xl border border-surface-dim bg-white p-4 text-center transition-all hover:border-info/30 hover:shadow-sm"
            @click="handleQuickAction(action)"
          >
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-info/10">
              <i class="pi text-info" :class="action.icon" />
            </div>
            <span class="text-xs font-medium text-secondary">
              {{ isRtl ? action.labelAr : action.labelEn }}
            </span>
          </button>
        </div>
      </div>

      <!-- Messages -->
      <div v-else class="mx-auto max-w-3xl space-y-4">
        <div
          v-for="message in messages"
          :key="message.id"
          class="flex gap-3"
          :class="message.role === 'user' ? 'flex-row-reverse' : 'flex-row'"
        >
          <!-- Avatar -->
          <div
            class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
            :class="message.role === 'user' ? 'bg-primary/10' : 'bg-info/10'"
          >
            <i
              class="pi text-sm"
              :class="message.role === 'user' ? 'pi-user text-primary' : 'pi-sparkles text-info'"
            />
          </div>

          <!-- Message Bubble -->
          <div
            class="max-w-[75%] rounded-2xl px-4 py-3"
            :class="message.role === 'user'
              ? 'bg-primary text-white'
              : 'border border-surface-dim bg-white text-secondary'"
          >
            <!-- Streaming indicator -->
            <div v-if="message.isStreaming" class="flex items-center gap-2">
              <div class="flex gap-1">
                <div class="h-2 w-2 animate-bounce rounded-full bg-info" style="animation-delay: 0ms" />
                <div class="h-2 w-2 animate-bounce rounded-full bg-info" style="animation-delay: 150ms" />
                <div class="h-2 w-2 animate-bounce rounded-full bg-info" style="animation-delay: 300ms" />
              </div>
              <span class="text-xs text-tertiary">{{ t('aiAssistant.thinking') }}</span>
            </div>

            <!-- Message content -->
            <div v-else>
              <p class="whitespace-pre-wrap text-sm leading-relaxed">{{ message.content }}</p>
              <!-- References -->
              <div v-if="message.references && message.references.length > 0" class="mt-2 border-t border-surface-dim/30 pt-2">
                <p class="mb-1 text-xs font-medium text-tertiary">{{ t('aiAssistant.references') }}:</p>
                <div class="flex flex-wrap gap-1">
                  <span
                    v-for="(ref, idx) in message.references"
                    :key="idx"
                    class="rounded-md bg-surface-muted px-2 py-0.5 text-xs text-secondary/70"
                  >
                    <i class="pi pi-file me-1 text-xs" />
                    {{ ref }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Timestamp -->
            <p
              class="mt-1 text-[10px]"
              :class="message.role === 'user' ? 'text-white/60' : 'text-tertiary'"
            >
              {{ formatDateTime(message.timestamp) }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="error"
      class="mx-4 mb-2 flex items-center gap-2 rounded-lg border border-danger/20 bg-danger/5 px-4 py-2"
    >
      <i class="pi pi-exclamation-triangle text-sm text-danger" />
      <p class="flex-1 text-xs text-danger">{{ error }}</p>
      <button
        class="text-xs font-medium text-danger hover:underline"
        @click="error = ''"
      >
        {{ t('aiAssistant.dismiss') }}
      </button>
    </div>

    <!-- Input Area -->
    <div class="border-t border-surface-dim bg-white px-4 py-4">
      <div class="mx-auto flex max-w-3xl items-end gap-3">
        <div class="relative flex-1">
          <textarea
            v-model="inputText"
            rows="1"
            :placeholder="t('aiAssistant.inputPlaceholder')"
            :disabled="isLoading"
            class="w-full resize-none rounded-xl border border-surface-dim bg-surface-muted px-4 py-3 pe-12 text-sm text-secondary placeholder:text-tertiary focus:border-info focus:bg-white focus:outline-none focus:ring-1 focus:ring-info disabled:opacity-50"
            style="max-height: 120px"
            @keydown="handleKeydown"
            @input="($event.target as HTMLTextAreaElement).style.height = 'auto'; ($event.target as HTMLTextAreaElement).style.height = ($event.target as HTMLTextAreaElement).scrollHeight + 'px'"
          />
        </div>
        <button
          class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-info text-white transition-colors hover:bg-info/90 disabled:opacity-50"
          :disabled="!inputText.trim() || isLoading"
          @click="handleSendMessage()"
        >
          <i class="pi" :class="isLoading ? 'pi-spinner pi-spin' : 'pi-send'" />
        </button>
      </div>
      <p class="mx-auto mt-2 max-w-3xl text-center text-[10px] text-tertiary">
        {{ t('aiAssistant.disclaimer') }}
      </p>
    </div>
  </div>
</template>
