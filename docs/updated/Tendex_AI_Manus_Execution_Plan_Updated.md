# خطة التنفيذ لوكلاء Manus (Manus Execution Plan)
## منصة Tendex AI — Tendex AI Platform

| البيان | التفاصيل |
|---|---|
| اسم المنتج | منصة Tendex AI |
| رقم الإصدار | 1.1 |
| تاريخ الإصدار | 21 أبريل 2026 |
| حالة الوثيقة | الإصدار النهائي المحدث (Final Release Updated) |
| المؤلف | فريق إدارة المشاريع |
| الجمهور المستهدف | وكلاء Manus (AI Agents)، مدراء المشاريع |
| التصنيف | سري — للاستخدام الداخلي فقط |

## سجل التغييرات
| الإصدار | التاريخ | المؤلف | الوصف |
|---|---|---|---|
| 1.0 | 27 مارس 2026 | Tendex AI | الإصدار الأول — خطة التنفيذ لوكلاء Manus |
| 1.1 | 21 أبريل 2026 | Tendex AI | تحديث — إضافة مهام توفير الجهات (Tenant Provisioning) وإدارة شهادات SSL |

## 3. خطة المهام (Sprint Backlog)
### 3.1 مهام البنية التحتية (Infrastructure Tasks)
- **مهمة INF-001:** أتمتة عملية إصدار وتوسيع شهادات SSL (Let's Encrypt) باستخدام `certbot` كجزء من سير عمل توفير الجهات الجديدة (Tenant Provisioning).
- **مهمة INF-002:** إعداد خادم Hostinger VPS وتكوين Nginx لدعم النطاقات الفرعية (Subdomains) المتعددة.

### 3.2 مهام الواجهة الخلفية (Backend Tasks)
- **مهمة BE-001:** تحديث `TenantDatabaseProvisioner` لضمان تضمين جميع الأعمدة الإلزامية (`NOT NULL`) في استعلامات SQL المباشرة (Raw SQL) عند إنشاء الحساب الإداري الافتراضي (SeedDefaultAdminUserAsync).
- **مهمة BE-002:** تحديث `TenantProvider` للسماح بالوصول للجهات في جميع الحالات التشغيلية المسموح بها (مثل `EnvironmentSetup`, `Training`, `FinalAcceptance`, `Active`, `RenewalWindow`).
EOF
