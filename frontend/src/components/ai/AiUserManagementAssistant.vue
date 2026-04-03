<script setup lang="ts">
/**
 * AiUserManagementAssistant — Floating AI Assistant for User/Role Management.
 *
 * Features:
 * - Floating button that opens a chat panel
 * - Quick action buttons for common AI tasks
 * - Role suggestion based on job title
 * - Permission analysis
 * - General Q&A about user management
 * - Full Arabic support
 */
import { ref, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpPost } from '@/services/http'

const { t } = useI18n()

const isOpen = ref(false)
const isLoading = ref(false)
const activeTab = ref<'chat' | 'suggest' | 'analyze'>('chat')
const chatMessages = ref<{ role: 'user' | 'assistant'; content: string }[]>([])
const chatInput = ref('')
const chatContainer = ref<HTMLElement | null>(null)

/* Suggest Role Form */
const suggestForm = ref({
  jobTitle: '',
  department: '',
  responsibilities: '',
  currentRoles: '',
})
const suggestResult = ref('')

/* Analyze Form */
const analyzeForm = ref({
  roleName: '',
  currentPermissions: '',
  userCount: 0,
  analysisType: 'comprehensive',
})
const analyzeResult = ref('')

function togglePanel() {
  isOpen.value = !isOpen.value
}

async function scrollToBottom() {
  await nextTick()
  if (chatContainer.value) {
    chatContainer.value.scrollTop = chatContainer.value.scrollHeight
  }
}

async function sendChatMessage() {
  if (!chatInput.value.trim() || isLoading.value) return
  const question = chatInput.value.trim()
  chatMessages.value.push({ role: 'user', content: question })
  chatInput.value = ''
  isLoading.value = true
  await scrollToBottom()

  try {
    const response = await httpPost<{ isSuccess: boolean; answer: string; errorMessage?: string }>(
      '/v1/ai/user-management/assist',
      { question }
    )
    if (response.isSuccess) {
      chatMessages.value.push({ role: 'assistant', content: response.answer })
    } else {
      chatMessages.value.push({ role: 'assistant', content: response.errorMessage || t('ai.assistant.errorGeneric') })
    }
  } catch {
    chatMessages.value.push({ role: 'assistant', content: t('ai.assistant.errorGeneric') })
  } finally {
    isLoading.value = false
    await scrollToBottom()
  }
}

async function handleSuggestRole() {
  if (isLoading.value) return
  isLoading.value = true
  suggestResult.value = ''

  try {
    const response = await httpPost<{ isSuccess: boolean; suggestion: string; errorMessage?: string }>(
      '/v1/ai/user-management/suggest-role',
      suggestForm.value
    )
    if (response.isSuccess) {
      try {
        const parsed = JSON.parse(response.suggestion)
        suggestResult.value = formatSuggestion(parsed)
      } catch {
        suggestResult.value = response.suggestion
      }
    } else {
      suggestResult.value = response.errorMessage || t('ai.assistant.errorGeneric')
    }
  } catch {
    suggestResult.value = t('ai.assistant.errorGeneric')
  } finally {
    isLoading.value = false
  }
}

async function handleAnalyzePermissions() {
  if (isLoading.value) return
  isLoading.value = true
  analyzeResult.value = ''

  try {
    const response = await httpPost<{ isSuccess: boolean; analysis: string; errorMessage?: string }>(
      '/v1/ai/user-management/analyze-permissions',
      analyzeForm.value
    )
    if (response.isSuccess) {
      try {
        const parsed = JSON.parse(response.analysis)
        analyzeResult.value = formatAnalysis(parsed)
      } catch {
        analyzeResult.value = response.analysis
      }
    } else {
      analyzeResult.value = response.errorMessage || t('ai.assistant.errorGeneric')
    }
  } catch {
    analyzeResult.value = t('ai.assistant.errorGeneric')
  } finally {
    isLoading.value = false
  }
}

function formatSuggestion(data: Record<string, unknown>): string {
  const lines: string[] = []
  if (data.suggestedRoleNameAr) lines.push(`الدور المقترح: ${data.suggestedRoleNameAr}`)
  if (data.suggestedRoleNameEn) lines.push(`(${data.suggestedRoleNameEn})`)
  if (data.reason) lines.push(`\nالسبب: ${data.reason}`)
  if (Array.isArray(data.suggestedPermissions) && data.suggestedPermissions.length > 0) {
    lines.push(`\nالصلاحيات المقترحة:`)
    data.suggestedPermissions.forEach((p, i) => lines.push(`  ${i + 1}. ${p}`))
  }
  if (Array.isArray(data.suggestedPhases) && data.suggestedPhases.length > 0) {
    lines.push(`\nالمراحل المقترحة:`)
    data.suggestedPhases.forEach((p, i) => lines.push(`  ${i + 1}. ${p}`))
  }
  if (data.riskNotes) lines.push(`\nملاحظات أمنية: ${data.riskNotes}`)
  return lines.join('\n')
}

function formatAnalysis(data: Record<string, unknown>): string {
  const lines: string[] = []
  if (data.summary) lines.push(`الملخص: ${data.summary}`)
  if (data.riskLevel) lines.push(`مستوى المخاطر: ${data.riskLevel}`)
  if (Array.isArray(data.recommendations) && data.recommendations.length > 0) {
    lines.push(`\nالتوصيات:`)
    data.recommendations.forEach((r, i) => lines.push(`  ${i + 1}. ${r}`))
  }
  if (Array.isArray(data.excessivePermissions) && data.excessivePermissions.length > 0) {
    lines.push(`\nصلاحيات زائدة:`)
    data.excessivePermissions.forEach((p, i) => lines.push(`  ${i + 1}. ${p}`))
  }
  if (Array.isArray(data.missingPermissions) && data.missingPermissions.length > 0) {
    lines.push(`\nصلاحيات مفقودة:`)
    data.missingPermissions.forEach((p, i) => lines.push(`  ${i + 1}. ${p}`))
  }
  if (data.securityNotes) lines.push(`\nملاحظات أمنية: ${data.securityNotes}`)
  return lines.join('\n')
}

function useQuickQuestion(question: string) {
  activeTab.value = 'chat'
  chatInput.value = question
  sendChatMessage()
}
</script>

<template>
  <!-- Floating Button -->
  <button
    class="fixed bottom-6 end-6 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-primary text-white shadow-lg transition-all hover:bg-primary-dark hover:shadow-xl"
    :title="t('ai.assistant.title')"
    @click="togglePanel"
  >
    <i :class="['pi text-xl', isOpen ? 'pi-times' : 'pi-sparkles']"></i>
  </button>

  <!-- Panel -->
  <Teleport to="body">
    <div
      v-if="isOpen"
      class="fixed bottom-24 end-6 z-50 flex w-96 flex-col overflow-hidden rounded-xl border border-surface-dim bg-white shadow-2xl"
      style="max-height: 70vh"
    >
      <!-- Header -->
      <div class="flex items-center gap-3 border-b border-surface-dim bg-primary px-4 py-3">
        <i class="pi pi-sparkles text-white"></i>
        <div class="flex-1">
          <h3 class="text-sm font-semibold text-white">{{ t('ai.assistant.title') }}</h3>
          <p class="text-xs text-white/70">{{ t('ai.assistant.subtitle') }}</p>
        </div>
        <button class="rounded-lg p-1 text-white/70 hover:text-white" @click="isOpen = false">
          <i class="pi pi-times text-sm"></i>
        </button>
      </div>

      <!-- Tabs -->
      <div class="flex border-b border-surface-dim bg-surface-ground">
        <button
          :class="['flex-1 px-3 py-2 text-xs font-medium transition-colors', activeTab === 'chat' ? 'border-b-2 border-primary text-primary bg-white' : 'text-tertiary hover:text-secondary']"
          @click="activeTab = 'chat'"
        >
          <i class="pi pi-comments me-1 text-xs"></i>{{ t('ai.assistant.tabs.chat') }}
        </button>
        <button
          :class="['flex-1 px-3 py-2 text-xs font-medium transition-colors', activeTab === 'suggest' ? 'border-b-2 border-primary text-primary bg-white' : 'text-tertiary hover:text-secondary']"
          @click="activeTab = 'suggest'"
        >
          <i class="pi pi-lightbulb me-1 text-xs"></i>{{ t('ai.assistant.tabs.suggest') }}
        </button>
        <button
          :class="['flex-1 px-3 py-2 text-xs font-medium transition-colors', activeTab === 'analyze' ? 'border-b-2 border-primary text-primary bg-white' : 'text-tertiary hover:text-secondary']"
          @click="activeTab = 'analyze'"
        >
          <i class="pi pi-chart-bar me-1 text-xs"></i>{{ t('ai.assistant.tabs.analyze') }}
        </button>
      </div>

      <!-- Chat Tab -->
      <template v-if="activeTab === 'chat'">
        <div ref="chatContainer" class="flex-1 overflow-y-auto p-4 space-y-3" style="min-height: 200px; max-height: 350px">
          <!-- Welcome -->
          <div v-if="chatMessages.length === 0" class="space-y-3">
            <p class="text-sm text-tertiary text-center">{{ t('ai.assistant.welcomeMessage') }}</p>
            <div class="space-y-2">
              <button class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-start text-xs text-secondary transition-colors hover:border-primary/30" @click="useQuickQuestion('ما هي أفضل الممارسات لتعيين الأدوار والصلاحيات في نظام المنافسات الحكومية؟')">
                <i class="pi pi-question-circle me-1 text-primary text-xs"></i>{{ t('ai.assistant.quickQuestions.bestPractices') }}
              </button>
              <button class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-start text-xs text-secondary transition-colors hover:border-primary/30" @click="useQuickQuestion('كيف أقوم بإعداد هيكل صلاحيات آمن للجان المنافسات؟')">
                <i class="pi pi-question-circle me-1 text-primary text-xs"></i>{{ t('ai.assistant.quickQuestions.secureStructure') }}
              </button>
              <button class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-start text-xs text-secondary transition-colors hover:border-primary/30" @click="useQuickQuestion('ما هي الأدوار المطلوبة لإدارة دورة حياة المنافسة كاملة؟')">
                <i class="pi pi-question-circle me-1 text-primary text-xs"></i>{{ t('ai.assistant.quickQuestions.lifecycleRoles') }}
              </button>
            </div>
          </div>
          <!-- Messages -->
          <div v-for="(msg, idx) in chatMessages" :key="idx" :class="['flex', msg.role === 'user' ? 'justify-end' : 'justify-start']">
            <div :class="['max-w-[80%] rounded-lg px-3 py-2 text-sm', msg.role === 'user' ? 'bg-primary text-white' : 'bg-surface-ground text-secondary']">
              <p class="whitespace-pre-wrap">{{ msg.content }}</p>
            </div>
          </div>
          <!-- Loading -->
          <div v-if="isLoading" class="flex justify-start">
            <div class="rounded-lg bg-surface-ground px-3 py-2">
              <i class="pi pi-spin pi-spinner text-sm text-primary"></i>
            </div>
          </div>
        </div>
        <!-- Input -->
        <div class="border-t border-surface-dim p-3">
          <div class="flex items-center gap-2">
            <input
              v-model="chatInput"
              type="text"
              :placeholder="t('ai.assistant.chatPlaceholder')"
              class="flex-1 rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary placeholder:text-tertiary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              @keyup.enter="sendChatMessage"
            />
            <button
              :disabled="!chatInput.trim() || isLoading"
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-white transition-colors hover:bg-primary-dark disabled:opacity-50"
              @click="sendChatMessage"
            >
              <i class="pi pi-send text-sm"></i>
            </button>
          </div>
        </div>
      </template>

      <!-- Suggest Role Tab -->
      <template v-if="activeTab === 'suggest'">
        <div class="flex-1 overflow-y-auto p-4 space-y-3" style="max-height: 400px">
          <p class="text-xs text-tertiary">{{ t('ai.assistant.suggestDescription') }}</p>
          <div>
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('ai.assistant.fields.jobTitle') }}</label>
            <input v-model="suggestForm.jobTitle" type="text" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('ai.assistant.fields.department') }}</label>
            <input v-model="suggestForm.department" type="text" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('ai.assistant.fields.responsibilities') }}</label>
            <textarea v-model="suggestForm.responsibilities" rows="2" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
          </div>
          <button
            :disabled="isLoading || !suggestForm.jobTitle"
            class="w-full flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white transition-all hover:bg-primary-dark disabled:opacity-50"
            @click="handleSuggestRole"
          >
            <i v-if="isLoading" class="pi pi-spin pi-spinner text-xs"></i>
            <i v-else class="pi pi-sparkles text-xs"></i>
            {{ t('ai.assistant.suggestButton') }}
          </button>
          <div v-if="suggestResult" class="rounded-lg border border-surface-dim bg-surface-ground p-3">
            <p class="whitespace-pre-wrap text-sm text-secondary">{{ suggestResult }}</p>
          </div>
        </div>
      </template>

      <!-- Analyze Tab -->
      <template v-if="activeTab === 'analyze'">
        <div class="flex-1 overflow-y-auto p-4 space-y-3" style="max-height: 400px">
          <p class="text-xs text-tertiary">{{ t('ai.assistant.analyzeDescription') }}</p>
          <div>
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('ai.assistant.fields.roleName') }}</label>
            <input v-model="analyzeForm.roleName" type="text" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <div>
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('ai.assistant.fields.currentPermissions') }}</label>
            <textarea v-model="analyzeForm.currentPermissions" rows="2" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
          </div>
          <div>
            <label class="mb-1 block text-xs font-medium text-secondary">{{ t('ai.assistant.fields.userCount') }}</label>
            <input v-model.number="analyzeForm.userCount" type="number" min="0" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-3 py-2 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
          </div>
          <button
            :disabled="isLoading || !analyzeForm.roleName"
            class="w-full flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white transition-all hover:bg-primary-dark disabled:opacity-50"
            @click="handleAnalyzePermissions"
          >
            <i v-if="isLoading" class="pi pi-spin pi-spinner text-xs"></i>
            <i v-else class="pi pi-chart-bar text-xs"></i>
            {{ t('ai.assistant.analyzeButton') }}
          </button>
          <div v-if="analyzeResult" class="rounded-lg border border-surface-dim bg-surface-ground p-3">
            <p class="whitespace-pre-wrap text-sm text-secondary">{{ analyzeResult }}</p>
          </div>
        </div>
      </template>
    </div>
  </Teleport>
</template>
