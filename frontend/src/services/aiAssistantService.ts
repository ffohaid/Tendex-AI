/**
 * AI Assistant API service for Tendex AI Platform.
 * Connects to AiGatewayEndpoints for chat completions.
 * NO mock data — all data comes from the API.
 */
import { httpPost, httpGet } from './http'
import type { AiChatRequest, AiChatResponse } from '@/types/aiAssistant'

const AI_BASE_URL = '/v1/ai'

/**
 * Sends a chat completion request to the AI Gateway.
 */
export async function sendChatMessage(request: AiChatRequest): Promise<AiChatResponse> {
  return httpPost<AiChatResponse>(`${AI_BASE_URL}/completions`, request)
}

/**
 * Checks if AI services are available for the current tenant.
 */
export async function checkAiStatus(tenantId: string): Promise<{
  tenantId: string
  isAiAvailable: boolean
  timestamp: string
}> {
  return httpGet(`${AI_BASE_URL}/status/${tenantId}`)
}
