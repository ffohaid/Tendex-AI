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
export const basicInfoSchema = z.object({
  projectName: z
    .string({ required_error: 'اسم المشروع مطلوب' })
    .min(5, 'يجب أن يكون اسم المشروع 5 أحرف على الأقل')
    .max(200, 'يجب ألا يتجاوز اسم المشروع 200 حرف'),

  projectDescription: z
    .string({ required_error: 'وصف المشروع مطلوب' })
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

  startDate: z
    .string({ required_error: 'تاريخ البداية مطلوب' })
    .min(1, 'يرجى تحديد تاريخ البداية'),

  endDate: z
    .string({ required_error: 'تاريخ الانتهاء مطلوب' })
    .min(1, 'يرجى تحديد تاريخ الانتهاء'),

  submissionDeadline: z
    .string({ required_error: 'آخر موعد لتقديم العروض مطلوب' })
    .min(1, 'يرجى تحديد آخر موعد لتقديم العروض'),

  referenceNumber: z
    .string({ required_error: 'الرقم المرجعي مطلوب' })
    .min(1, 'الرقم المرجعي مطلوب')
    .max(50, 'يجب ألا يتجاوز الرقم المرجعي 50 حرفاً'),

  department: z
    .string({ required_error: 'الإدارة المسؤولة مطلوبة' })
    .min(1, 'يرجى تحديد الإدارة المسؤولة'),

  fiscalYear: z
    .string({ required_error: 'السنة المالية مطلوبة' })
    .min(1, 'يرجى تحديد السنة المالية'),
}).refine(
  (data) => {
    if (data.startDate && data.endDate) {
      return new Date(data.endDate) > new Date(data.startDate)
    }
    return true
  },
  {
    message: 'يجب أن يكون تاريخ الانتهاء بعد تاريخ البداية',
    path: ['endDate'],
  },
)

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

  inquiryPeriodDays: z
    .number({ required_error: 'مدة فترة الاستفسارات مطلوبة' })
    .min(1, 'يجب أن تكون فترة الاستفسارات يوماً واحداً على الأقل')
    .max(90, 'يجب ألا تتجاوز فترة الاستفسارات 90 يوماً'),

  evaluationCriteria: z
    .array(evaluationCriterionSchema)
    .default([]),
}).refine(
  (data) => data.technicalWeight + data.financialWeight === 100,
  {
    message: 'يجب أن يكون مجموع أوزان التقييم الفني والمالي 100%',
    path: ['financialWeight'],
  },
)

/* ------------------------------------------------------------------ */
/*  Step 3: Sections / Content                                        */
/* ------------------------------------------------------------------ */
export const sectionSchema = z.object({
  id: z.string(),
  title: z
    .string({ required_error: 'عنوان القسم مطلوب' })
    .min(3, 'يجب أن يكون عنوان القسم 3 أحرف على الأقل'),
  content: z.string().default(''),
  order: z.number(),
  isRequired: z.boolean().default(false),
  colorCode: z.enum(['black', 'green', 'red', 'blue']).default('green'),
  assignedTo: z.string().nullable().default(null),
  isCompleted: z.boolean().default(false),
})

export const contentSchema = z.object({
  sections: z
    .array(sectionSchema)
    .min(1, 'يجب إضافة قسم واحد على الأقل في الكراسة'),
  creationMethod: z.enum(['wizard', 'template', 'clone', 'ai']).default('wizard'),
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
    .number({ required_error: 'السعر التقديري مطلوب', invalid_type_error: 'يرجى إدخال سعر صحيح' })
    .min(0, 'يجب ألا يقل السعر عن صفر'),
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
})

/* ------------------------------------------------------------------ */
/*  Export schema types                                               */
/* ------------------------------------------------------------------ */
export type BasicInfoFormValues = z.infer<typeof basicInfoSchema>
export type SettingsFormValues = z.infer<typeof settingsSchema>
export type ContentFormValues = z.infer<typeof contentSchema>
export type BoqFormValues = z.infer<typeof boqSchema>
export type AttachmentsFormValues = z.infer<typeof attachmentsSchema>
