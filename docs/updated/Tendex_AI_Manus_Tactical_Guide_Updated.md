# الدليل التكتيكي لوكلاء Manus (Manus Tactical Guide)
## منصة Tendex AI — Tendex AI Platform

| البيان | التفاصيل |
|---|---|
| اسم المنتج | منصة Tendex AI |
| رقم الإصدار | 1.1 |
| تاريخ الإصدار | 21 أبريل 2026 |
| حالة الوثيقة | الإصدار النهائي المحدث (Final Release Updated) |
| المؤلف | فريق هندسة البرمجيات |
| الجمهور المستهدف | وكلاء Manus (AI Agents) |
| التصنيف | سري — للاستخدام الداخلي فقط |

## سجل التغييرات
| الإصدار | التاريخ | المؤلف | الوصف |
|---|---|---|---|
| 1.0 | 27 مارس 2026 | Tendex AI | الإصدار الأول — الدليل التكتيكي لوكلاء Manus |
| 1.1 | 21 أبريل 2026 | Tendex AI | تحديث — إضافة تعليمات التحقق من مخطط قاعدة البيانات (Schema Validation) وإدارة حالة الجهات (Tenant Status Management) |

## 3. قواعد جودة الكود وقواعد البيانات (Code & DB Quality Rules)
### 3.1 التحقق من المخطط (Schema Validation)
- **قيود `NOT NULL`:** عند كتابة استعلامات SQL مباشرة (Raw SQL) أو استخدام `ExecuteSqlRawAsync` (كما في عمليات Provisioning)، يجب التحقق بدقة من مطابقة الاستعلام لمخطط قاعدة البيانات، والتأكد من تضمين جميع الأعمدة الإلزامية (`NOT NULL`) لتجنب أخطاء الإدراج (Insert Errors).
  - *مثال:* في جدول `Users`، يجب تضمين `MfaEnabled`.
  - *مثال:* في جدول `UserRoles`، يجب تضمين `Id`, `AssignedAt`, `AssignedBy`, `CreatedAt`.

### 3.2 إدارة حالة الجهات (Tenant Status Management)
- **مزودات الخدمة (Providers):** عند كتابة مزودات الخدمة التي تعتمد على حالة الجهة (مثل `TenantProvider`)، يجب مراعاة جميع الحالات التشغيلية المسموح بها (مثل `EnvironmentSetup`, `Training`, `FinalAcceptance`, `Active`, `RenewalWindow`) وعدم قصرها على حالة `Active` فقط، ما لم يكن ذلك مطلوباً صراحةً في سياق العمل.
EOF
