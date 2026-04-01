/**
 * Booklet Extraction Service
 *
 * Provides API calls for the "Upload & Extract" (رفع واستخراج) feature.
 * Handles document upload and AI-powered content extraction.
 *
 * Endpoint: POST /api/v1/ai/booklet/extract-from-document
 */
import http from './http'

// ═══════════════════════════════════════════════════════════════
//  Response Types
// ═══════════════════════════════════════════════════════════════

export interface ExtractedSection {
  titleAr: string
  titleEn: string
  sectionType: string
  contentHtml: string
  isMandatory: boolean
  sortOrder: number
  confidenceScore: number
}

export interface ExtractedBoqItem {
  itemNumber: string
  descriptionAr: string
  unit: string
  quantity: number
  estimatedUnitPrice?: number
  category?: string
  sortOrder: number
}

export interface BookletExtractionResult {
  projectNameAr: string
  projectNameEn?: string
  projectDescription?: string
  detectedCompetitionType?: string
  estimatedBudget?: number
  projectDurationDays?: number
  sections: ExtractedSection[]
  boqItems: ExtractedBoqItem[]
  extractionSummaryAr: string
  confidenceScore: number
  warnings: string[]
  providerName: string
  modelName: string
  latencyMs: number
  uploadedFileId?: string
}

export interface ExtractBookletFromDocumentResult {
  isSuccess: boolean
  errorMessage?: string
  extraction?: BookletExtractionResult
  uploadedFileId?: string
}

// ═══════════════════════════════════════════════════════════════
//  API Functions
// ═══════════════════════════════════════════════════════════════

const BASE_URL = '/v1/ai/booklet'

/** Extended timeout for AI extraction (3 minutes) */
const EXTRACTION_TIMEOUT = 180_000

/**
 * Upload a document and extract structured booklet content using AI.
 *
 * @param file - The PDF or Word file to extract content from
 * @param onUploadProgress - Optional callback for upload progress tracking
 * @returns The extraction result with structured sections and metadata
 */
export async function extractBookletFromDocument(
  file: File,
  onUploadProgress?: (progressEvent: { loaded: number; total?: number; progress?: number }) => void,
): Promise<ExtractBookletFromDocumentResult> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await http.post<ExtractBookletFromDocumentResult>(
    `${BASE_URL}/extract-from-document`,
    formData,
    {
      timeout: EXTRACTION_TIMEOUT,
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress,
    },
  )

  return response.data
}

// ═══════════════════════════════════════════════════════════════
//  Utility Functions
// ═══════════════════════════════════════════════════════════════

/**
 * Maps the section type string from the API to a human-readable Arabic label.
 */
export function getSectionTypeLabel(sectionType: string): string {
  const labels: Record<string, string> = {
    GeneralInformation: 'معلومات عامة',
    TechnicalSpecifications: 'المواصفات الفنية',
    TermsAndConditions: 'الشروط والأحكام',
    EvaluationCriteria: 'معايير التقييم',
    BillOfQuantities: 'جدول الكميات',
    Attachments: 'المرفقات',
    Custom: 'قسم مخصص',
  }
  return labels[sectionType] || sectionType
}

/**
 * Maps the section type to a color code for visual distinction.
 */
export function getSectionTypeColor(sectionType: string): string {
  const colors: Record<string, string> = {
    GeneralInformation: '#3B82F6',      // blue
    TechnicalSpecifications: '#8B5CF6', // purple
    TermsAndConditions: '#EF4444',      // red
    EvaluationCriteria: '#F59E0B',      // amber
    BillOfQuantities: '#10B981',        // green
    Attachments: '#6B7280',             // gray
    Custom: '#6366F1',                  // indigo
  }
  return colors[sectionType] || '#6B7280'
}

/**
 * Maps the competition type from the API to a human-readable Arabic label.
 */
export function getCompetitionTypeLabel(competitionType?: string): string {
  if (!competitionType) return 'غير محدد'
  const labels: Record<string, string> = {
    PublicTender: 'منافسة عامة',
    LimitedTender: 'منافسة محدودة',
    DirectPurchase: 'شراء مباشر',
    FrameworkAgreement: 'اتفاقية إطارية',
    TwoStageTender: 'منافسة على مرحلتين',
    ReverseAuction: 'مزاد عكسي',
  }
  return labels[competitionType] || competitionType
}

/**
 * Validates if a file is acceptable for extraction.
 */
export function validateExtractionFile(file: File): { valid: boolean; error?: string } {
  const maxSize = 50 * 1024 * 1024 // 50 MB
  const allowedTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  ]
  const allowedExtensions = ['.pdf', '.doc', '.docx']

  const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase()

  if (!allowedTypes.includes(file.type) && !allowedExtensions.includes(extension)) {
    return {
      valid: false,
      error: 'نوع الملف غير مدعوم. يرجى رفع ملف PDF أو Word فقط.',
    }
  }

  if (file.size > maxSize) {
    return {
      valid: false,
      error: 'حجم الملف يتجاوز الحد الأقصى المسموح (50 ميجابايت).',
    }
  }

  if (file.size === 0) {
    return {
      valid: false,
      error: 'الملف المرفوع فارغ.',
    }
  }

  return { valid: true }
}

/**
 * Formats file size in human-readable format.
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 بايت'
  const units = ['بايت', 'كيلوبايت', 'ميجابايت', 'جيجابايت']
  const k = 1024
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const size = parseFloat((bytes / Math.pow(k, i)).toFixed(1))
  return `${size} ${units[i]}`
}
