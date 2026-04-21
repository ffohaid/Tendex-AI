# معايير جودة قواعد البيانات (Database Quality Standards)
## منصة Tendex AI — Tendex AI Platform

| البيان | التفاصيل |
|---|---|
| اسم المنتج | منصة Tendex AI |
| رقم الإصدار | 2.1 |
| تاريخ الإصدار | 21 أبريل 2026 |
| حالة الوثيقة | الإصدار النهائي المحدث (Final Release Updated) |
| المؤلف | فريق هندسة البيانات |
| الجمهور المستهدف | مهندسو قواعد البيانات، مطورو الواجهة الخلفية |
| التصنيف | سري — للاستخدام الداخلي فقط |

## سجل التغييرات
| الإصدار | التاريخ | المؤلف | الوصف |
|---|---|---|---|
| 2.0 | 27 مارس 2026 | Tendex AI | تحديث شامل لمعايير SQL Server و Qdrant لدعم نموذج SaaS Multi-Tenant |
| 2.1 | 21 أبريل 2026 | Tendex AI | تحديث — إضافة قواعد صارمة لقيود `NOT NULL` وتحديث حالات دورة حياة الجهة (Tenant Lifecycle Statuses) |

## 2. معايير SQL Server (قاعدة البيانات العلائقية)
### 2.1 تصميم المخطط (Schema Design)
- **قيود `NOT NULL` (Strict Constraints):** يجب الالتزام الصارم بقيود `NOT NULL` في جميع الجداول، خاصة الجداول الأساسية مثل `Users` و `UserRoles`. عند كتابة نصوص التأسيس (Provisioning Scripts) أو الإدراج المباشر (Raw SQL Inserts)، يجب التأكد من تضمين جميع الأعمدة الإلزامية لتجنب فشل العمليات.
  - *مثال:* في جدول `Users`، يجب تضمين `MfaEnabled`.
  - *مثال:* في جدول `UserRoles`، يجب تضمين `Id`, `AssignedAt`, `AssignedBy`, `CreatedAt`.
- **منع الحذف المتسلسل (No Cascade Deletes):** يُمنع تماماً استخدام الحذف المتسلسل (`ON DELETE CASCADE`)؛ يجب استخدام `ON DELETE NO ACTION` حصرياً لمنع فقدان البيانات العرضي.

## 4. القواعد الإلزامية العامة (General Mandatory Rules)
### 4.1 إدارة حالة الجهات (Tenant Status Management)
يجب أن يعكس حقل `Status` في جدول `Tenants` دورة الحياة التشغيلية للجهة بدقة. الحالات المعتمدة هي:
- `0`: PendingProvisioning
- `1`: EnvironmentSetup
- `2`: Training
- `3`: FinalAcceptance
- `4`: Active
- `5`: RenewalWindow
- `6`: Suspended
- `7`: Cancelled
- `8`: Archived

يجب على جميع الاستعلامات ومزودات الخدمة (مثل `TenantProvider`) التي تتحقق من صلاحية الجهة أن تسمح بالوصول للحالات التشغيلية (1 إلى 5) وتمنع الوصول للحالات المعلقة أو الملغاة (6 إلى 8).
EOF
