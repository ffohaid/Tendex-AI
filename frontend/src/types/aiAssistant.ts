/**
 * AI Assistant type definitions for Tendex AI Platform.
 * Covers chat messages, conversation state, and AI responses.
 */

/* ──────────────────────────────────────────────
 * Chat Message
 * ──────────────────────────────────────────── */
export type ChatRole = 'user' | 'assistant' | 'system'

export interface ChatMessage {
  id: string
  role: ChatRole
  content: string
  timestamp: string
  isStreaming?: boolean
  references?: string[]
  confidence?: number
}

/* ──────────────────────────────────────────────
 * Conversation
 * ──────────────────────────────────────────── */
export interface Conversation {
  id: string
  titleAr: string
  titleEn: string
  createdAt: string
  updatedAt: string
  messagesCount: number
}

/* ──────────────────────────────────────────────
 * AI Completion Request (matches AiGatewayEndpoints)
 * ──────────────────────────────────────────── */
export interface AiChatRequest {
  tenantId: string
  systemPrompt?: string
  userPrompt: string
  conversationHistory?: Array<{
    role: string
    content: string
  }>
  maxTokensOverride?: number
  temperatureOverride?: number
  ragContext?: string
}

/* ──────────────────────────────────────────────
 * AI Completion Response
 * ──────────────────────────────────────────── */
export interface AiChatResponse {
  isSuccess: boolean
  content: string
  provider: string
  modelName: string
  tokensUsed: number
  latencyMs: number
  errorMessage: string | null
}

/* ──────────────────────────────────────────────
 * Quick Action Suggestions
 * ──────────────────────────────────────────── */
export interface QuickAction {
  key: string
  labelAr: string
  labelEn: string
  icon: string
  prompt: string
}
