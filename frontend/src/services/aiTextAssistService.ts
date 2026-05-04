import http from '@/services/http'

export interface AiTextAssistContext {
  fieldName: string
  fieldPurpose?: string
  projectName?: string
  projectDescription?: string
  competitionType?: string
  additionalContext?: string
  strictFieldScope?: boolean
}

export interface AiTextAssistRequest {
  action: string
  currentText: string
  context: AiTextAssistContext
  customPrompt?: string
  language?: 'ar' | 'en'
  maxCharacters?: number
}

export interface AiTextAssistResponse {
  isSuccess: boolean
  generatedText: string
  errorMessage?: string
}

function buildAdditionalContext(context: AiTextAssistContext): string {
  const segments: string[] = []

  if (context.fieldPurpose?.trim()) {
    segments.push(`الغرض من الحقل: ${context.fieldPurpose.trim()}`)
  }

  if (context.additionalContext?.trim()) {
    segments.push(context.additionalContext.trim())
  }

  if (context.strictFieldScope) {
    segments.push('تعليمات إلزامية: يجب أن يقتصر التحليل والتوليد والتحسين على النص الحالي للحقل المستهدف فقط، دون الاعتماد على سياق خارجي أو افتراضات عن بقية الكراسة أو المشروع ما لم تكن مذكورة صراحة داخل هذا الحقل.')
  }

  return segments.join('\n')
}

function buildPayload(request: AiTextAssistRequest) {
  const { context } = request
  const strictFieldScope = context.strictFieldScope === true

  return {
    action: request.action,
    currentText: request.currentText || '',
    fieldName: context.fieldName,
    fieldPurpose: context.fieldPurpose || '',
    projectName: strictFieldScope ? '' : (context.projectName || ''),
    projectDescription: strictFieldScope ? '' : (context.projectDescription || ''),
    competitionType: strictFieldScope ? '' : (context.competitionType || ''),
    additionalContext: buildAdditionalContext(context),
    customPrompt: request.customPrompt || '',
    language: request.language || 'ar',
    maxCharacters: request.maxCharacters || 0,
  }
}

export async function requestAiTextAssist(request: AiTextAssistRequest): Promise<AiTextAssistResponse> {
  const response = await http.post<AiTextAssistResponse>(
    '/v1/ai/text/assist',
    buildPayload(request),
    { timeout: 120_000 },
  )

  return response.data
}
