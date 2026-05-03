/**
 * Zod validation schemas for RFP creation wizard.
 *
 * All error messages are in Arabic as required by TASK-504.
 * Each wizard step has its own schema for real-time validation.
 */
import { z } from 'zod'

/* ------------------------------------------------------------------ */
/*  Step 1: Basic Information                                         */
/* ------------------------------------------------------------------ */
const isoDateRegex = /^\d{4}-\d{2}-\d{2}$/

function parseIsoDate(value: string): Date | null {
  if (!value || !isoDateRegex.test(value)) return null
  const parsed = new Date(`${value}T00:00:00`)
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

function isWithinFiscalYear(value: string, fiscalYear: string): boolean {
  return value.startsWith(`${fiscalYear}-`)
}

export const basicInfoSchema = z.object({
  projectName: z
    .string({ required_error: 'اسم المشروع مطلوب' })
    .trim()
    .min(5, 'يجب أن يكون اسم المشروع 5 أحرف على الأقل')
    .max(200, 'يجب ألا يتجاوز اسم المشروع 200 حرف'),

  projectDescription: z
    .string({ required_error: 'وصف المشروع مطلوب' })
    .trim()
    .min(20, 'يجب أن يكون وصف المشروع 20 حرفاً على الأقل')
    .max(2000, 'يجب ألا يتجاوز وصف المشروع 2000 حرف'),

  competitionType: z
    .string({ required_error: 'نوع المنافسة مطلوب' })
    .min(1, 'يرجى اختيار نوع المنافسة'),

  estimatedValue: z
    .number({ required_error: 'القيمة التقديرية مطلوبة', invalid_type_error: 'يرجى إدخال قيمة رقمية صحيحة' })
    .positive('يجب أن تكون القيمة التقديرية أكبر من صفر')
    .nullable()
    .refine((val) => val !== null, { message: 'القيمة التقديرية مطلوبة' }),

  currency: z.string().default('SAR'),

  bookletNumber: z
    .string()
    .trim()
    .max(50, 'يجب ألا يتجاوز رقم الكراسة 50 حرفاً')
    .regex(/^[A-Za-z0-9\-/]*$/, 'يسمح فقط بالأحرف الإنجليزية والأرقام والشرطة')
    .default(''),

  department: z
    .string({ required_error: 'الإدارة المسؤولة مطلوبة' })
    .trim()
    .min(1, 'يرجى تحديد الإدارة المسؤولة')
    .max(200, 'يجب ألا يتجاوز اسم الإدارة 200 حرف'),

  fiscalYear: z
    .string({ required_error: 'السنة المالية مطلوبة' })
    .regex(/^\d{4}$/, 'يرجى اختيار سنة مالية صحيحة'),

  bookletIssueDate: z.string().default(''),
  inquiriesStartDate: z.string().default(''),
  inquiryPeriodDays: z
    .number({ invalid_type_error: 'يرجى إدخال مدة صحيحة للاستفسارات' })
    .int('يجب إدخال عدد صحيح من الأيام')
    .min(1, 'يجب أن تكون مدة الاستفسارات يوماً واحداً على الأقل')
    .max(365, 'يجب ألا تتجاوز مدة الاستفسارات 365 يوماً')
    .nullable(),
  offersStartDate: z.string().default(''),
  submissionDeadline: z.string().default(''),
  expectedAwardDate: z.string().default(''),
  workStartDate: z.string().default(''),
}).superRefine((data, ctx) => {
  const orderedDates = [
    { key: 'bookletIssueDate', label: 'تاريخ طرح الكراسة', strictAfterPrevious: false },
    { key: 'inquiriesStartDate', label: 'تاريخ إرسال الاستفسارات', strictAfterPrevious: false },
    { key: 'offersStartDate', label: 'تاريخ تقديم العروض', strictAfterPrevious: false },
    { key: 'submissionDeadline', label: 'آخر موعد لتقديم العروض', strictAfterPrevious: true },
    { key: 'expectedAwardDate', label: 'التاريخ المتوقع للترسية', strictAfterPrevious: true },
    { key: 'workStartDate', label: 'تاريخ بدء الأعمال', strictAfterPrevious: true },
  ] as const

  const parsedDates = new Map<string, Date>()

  for (const dateField of orderedDates) {
    const value = data[dateField.key]
    if (!value) continue

    const parsed = parseIsoDate(value)
    if (!parsed) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: `يرجى إدخال ${dateField.label} بصيغة تاريخ صحيحة`,
        path: [dateField.key],
      })
      continue
    }

    if (!isWithinFiscalYear(value, data.fiscalYear)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: `${dateField.label} يجب أن يكون ضمن السنة المالية المحددة`,
        path: [dateField.key],
      })
    }

    parsedDates.set(dateField.key, parsed)
  }

  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const bookletIssueDate = parsedDates.get('bookletIssueDate')
  if (bookletIssueDate && bookletIssueDate < today) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'تاريخ طرح الكراسة يجب أن يكون اليوم أو بعده',
      path: ['bookletIssueDate'],
    })
  }

  for (let index = 1; index < orderedDates.length; index += 1) {
    const current = orderedDates[index]
    const previous = orderedDates[index - 1]
    const currentDate = parsedDates.get(current.key)
    const previousDate = parsedDates.get(previous.key)

    if (!currentDate || !previousDate) continue

    const isValid = current.strictAfterPrevious
      ? currentDate > previousDate
      : currentDate >= previousDate

    if (!isValid) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: `${current.label} يجب أن يكون ${current.strictAfterPrevious ? 'بعد' : 'في نفس يوم أو بعد'} ${previous.label}`,
        path: [current.key],
      })
    }
  }
})

/* ------------------------------------------------------------------ */
/*  Step 2: Competition Settings                                      */
/* ------------------------------------------------------------------ */
export const evaluationCriterionSchema = z.object({
  id: z.string(),
  name: z
    .string({ required_error: 'اسم المعيار مطلوب' })
    .min(2, 'يجب أن يكون اسم المعيار حرفين على الأقل'),
  weight: z
    .number({ required_error: 'وزن المعيار مطلوب' })
    .min(1, 'يجب أن يكون الوزن 1% على الأقل')
    .max(100, 'يجب ألا يتجاوز الوزن 100%'),
  description: z.string().default(''),
  order: z.number().default(0),
})

export const settingsSchema = z.object({
  evaluationMethod: z
    .string({ required_error: 'طريقة التقييم مطلوبة' })
    .min(1, 'يرجى اختيار طريقة التقييم'),

  technicalWeight: z
    .number({ required_error: 'وزن التقييم الفني مطلوب' })
    .min(0, 'يجب ألا يقل الوزن عن 0%')
    .max(100, 'يجب ألا يتجاوز الوزن 100%'),

  financialWeight: z
    .number({ required_error: 'وزن التقييم المالي مطلوب' })
    .min(0, 'يجب ألا يقل الوزن عن 0%')
    .max(100, 'يجب ألا يتجاوز الوزن 100%'),

  minimumTechnicalScore: z
    .number({ required_error: 'الحد الأدنى للدرجة الفنية مطلوب' })
    .min(0, 'يجب ألا يقل الحد الأدنى عن 0')
    .max(100, 'يجب ألا يتجاوز الحد الأدنى 100'),

  allowPartialOffers: z.boolean().default(false),

  requireBankGuarantee: z.boolean().default(true),

  guaranteePercentage: z
    .number()
    .min(0, 'يجب ألا تقل نسبة الضمان عن 0%')
    .max(100, 'يجب ألا تتجاوز نسبة الضمان 100%')
    .default(5),

  evaluationCriteria: z
    .array(evaluationCriterionSchema)
    .min(1, 'يرجى تعريف معيار تقييم واحد على الأقل قبل المتابعة')
    .default([]),
}).refine(
  (data) => data.technicalWeight + data.financialWeight === 100,
  {
    message: 'يجب أن يكون مجموع أوزان التقييم الفني والمالي 100%',
    path: ['financialWeight'],
  },
).refine(
  (data) => data.evaluationCriteria.reduce((sum, criterion) => sum + criterion.weight, 0) <= 100,
  {
    message: 'لا يمكن أن يتجاوز مجموع أوزان معايير التقييم 100%',
    path: ['evaluationCriteria'],
  },
)

/* ------------------------------------------------------------------ */
/*  Step 3: Sections / Content                                        */
/* ------------------------------------------------------------------ */
export const sectionSchema = z.object({
  id: z.string(),
  title: z
    .string({ required_error: 'عنوان القسم مطلوب' })
    .trim()
    .min(1, 'عنوان القسم مطلوب')
    .min(3, 'يجب أن يكون عنوان القسم 3 أحرف على الأقل'),
  content: z
    .string()
    .default('')
    .superRefine((value, ctx) => {
      const plainText = value.replace(/<[^>]*>/g, ' ').replace(/&nbsp;/gi, ' ').replace(/\s+/g, ' ').trim()
      if (!plainText) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'محتوى القسم مطلوب',
        })
      }
    }),
  order: z.number(),
  isRequired: z.boolean().default(false),
  colorCode: z.enum(['', 'black', 'green', 'red', 'blue']).default(''),
  assignedTo: z.string().nullable().default(null),
  isCompleted: z.boolean().default(false),
})

export const contentSchema = z.object({
  sections: z
    .array(sectionSchema)
    .min(1, 'يجب إضافة قسم واحد على الأقل في الكراسة'),
  creationMethod: z.enum(['wizard', 'template', 'clone', 'ai', 'upload_extract']).default('wizard'),
  templateId: z.string().nullable().default(null),
  cloneFromId: z.string().nullable().default(null),
})

/* ------------------------------------------------------------------ */
/*  Step 4: BOQ (Bill of Quantities)                                  */
/* ------------------------------------------------------------------ */
export const boqItemSchema = z.object({
  id: z.string(),
  category: z
    .string({ required_error: 'تصنيف البند مطلوب' })
    .min(1, 'يرجى تحديد تصنيف البند'),
  description: z
    .string({ required_error: 'وصف البند مطلوب' })
    .min(3, 'يجب أن يكون وصف البند 3 أحرف على الأقل'),
  unit: z
    .string({ required_error: 'وحدة القياس مطلوبة' })
    .min(1, 'يرجى اختيار وحدة القياس'),
  quantity: z
    .number({ required_error: 'الكمية مطلوبة', invalid_type_error: 'يرجى إدخال كمية صحيحة' })
    .positive('يجب أن تكون الكمية أكبر من صفر'),
  estimatedPrice: z
    .number({ invalid_type_error: 'يرجى إدخال سعر صحيح' })
    .nullable()
    .optional(),
  totalPrice: z.number().default(0),
  notes: z.string().default(''),
  order: z.number().default(0),
})

export const boqSchema = z.object({
  items: z
    .array(boqItemSchema)
    .min(1, 'يجب إضافة بند واحد على الأقل في جدول الكميات'),
  totalEstimatedValue: z.number().default(0),
  includesVat: z.boolean().default(true),
  vatPercentage: z
    .number()
    .min(0, 'يجب ألا تقل نسبة الضريبة عن 0%')
    .max(100, 'يجب ألا تتجاوز نسبة الضريبة 100%')
    .default(15),
})

/* ------------------------------------------------------------------ */
/*  Step 5: Attachments                                               */
/* ------------------------------------------------------------------ */
export const attachmentSchema = z.object({
  id: z.string(),
  name: z.string().min(1, 'اسم المرفق مطلوب'),
  fileUrl: z.string(),
  fileSize: z.number(),
  fileType: z.string(),
  isRequired: z.boolean().default(false),
  uploadedAt: z.string(),
  uploadedBy: z.string(),
})

export const attachmentsSchema = z.object({
  files: z.array(attachmentSchema),
  requiredAttachmentTypes: z
    .array(z.string())
    .min(1, 'يرجى اختيار مرفق أساسي واحد على الأقل قبل المتابعة'),
})

/* ------------------------------------------------------------------ */
/*  Export schema types                                               */
/* ------------------------------------------------------------------ */
export type BasicInfoFormValues = z.infer<typeof basicInfoSchema>
export type SettingsFormValues = z.infer<typeof settingsSchema>
export type ContentFormValues = z.infer<typeof contentSchema>
export type BoqFormValues = z.infer<typeof boqSchema>
export type AttachmentsFormValues = z.infer<typeof attachmentsSchema>
