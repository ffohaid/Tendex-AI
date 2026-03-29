# دليل تنفيذ مهام Sprint 9 (سبرنت الإصلاحات والاستكمال)

هذا الدليل يحتوي على البرومبتات الجاهزة لتنفيذ مهام Sprint 9، والذي يهدف إلى استكمال الصفحات المفقودة في الواجهة الأمامية (التي كانت مجرد Stubs) وبناء الـ Endpoints الناقصة في الباك إند لتعمل لوحة التحكم (Dashboard) بشكل حقيقي.

---

## TASK-901: استكمال مسارات لوحة التحكم (Dashboard Endpoints)

**الهدف:** بناء الـ Endpoints المفقودة في الباك إند والتي يطلبها الفرونت إند لعرض إحصائيات لوحة التحكم والمهام والإشعارات.

**البرومبت (انسخ النص التالي في محادثة جديدة):**

```text
أنت وكيل تطوير (Developer Agent) تعمل على مشروع Tendex AI.
مهمتك هي TASK-901: استكمال مسارات لوحة التحكم (Dashboard Endpoints).

**المشكلة الحالية:**
الفرونت إند (في `frontend/src/services/dashboard.ts`) يطلب مسارات غير موجودة في الباك إند، مما يؤدي إلى عدم ظهور أي بيانات في لوحة التحكم للمستخدمين العاديين.

**المطلوب تنفيذه في الباك إند (.NET 10):**
1. إنشاء `DashboardEndpoints.cs` (أو إضافتها لملف مناسب) لتوفير المسارات التالية:
   - `GET /api/v1/dashboard/stats` (إرجاع إحصائيات: totalRfps, activeCommittees, pendingApprovals, etc.)
   - `GET /api/v1/dashboard/activities` (إرجاع نشاطات حديثة)
   - `GET /api/v1/dashboard/metrics` (إرجاع بيانات الرسوم البيانية)
2. إنشاء `TaskEndpoints.cs` لتوفير:
   - `GET /api/v1/tasks/pending` (إرجاع المهام المعلقة للمستخدم الحالي)
3. إنشاء `NotificationEndpoints.cs` لتوفير:
   - `GET /api/v1/notifications` (إرجاع إشعارات المستخدم)
   - `POST /api/v1/notifications/{id}/read` (تحديد إشعار كمقروء)
4. يجب أن تعتمد هذه المسارات على `TenantId` و `UserId` من الـ Token الحالي.
5. لا تستخدم بيانات وهمية (Mock Data) قدر الإمكان، حاول جلب البيانات من الجداول الحقيقية (مثل Competitions, Committees). إذا كان الجدول فارغاً، أرجع قائمة فارغة.

**خطوات العمل:**
1. اقرأ `frontend/src/services/dashboard.ts` و `frontend/src/types/dashboard.ts` لفهم هيكل البيانات المطلوب.
2. قم بإنشاء الـ Commands/Queries والـ Endpoints في الباك إند.
3. تأكد من تسجيل الـ Endpoints في `DependencyInjection` أو عبر `Carter` / `EndpointRouteBuilder`.
4. قم ببناء الباك إند (`dotnet build`) للتأكد من عدم وجود أخطاء.
5. حدث `PROGRESS.md` وارفع التغييرات إلى GitHub.
```

---

## TASK-902: بناء صفحات اللجان والاعتمادات (Committees & Approvals)

**الهدف:** استبدال الـ Stubs في الفرونت إند بصفحات حقيقية لإدارة اللجان والاعتمادات.

**البرومبت (انسخ النص التالي في محادثة جديدة):**

```text
أنت وكيل تطوير (Developer Agent) تعمل على مشروع Tendex AI.
مهمتك هي TASK-902: بناء صفحات اللجان والاعتمادات في الواجهة الأمامية.

**المشكلة الحالية:**
في `frontend/src/router/index.ts`، المسارات الخاصة باللجان والاعتمادات تشير إلى `DashboardView.vue` كـ Stub. الباك إند يحتوي بالفعل على `CommitteeEndpoints.cs` لكن لا توجد واجهة لها.

**المطلوب تنفيذه في الفرونت إند (Vue 3 + Tailwind + PrimeVue):**
1. إنشاء صفحة `frontend/src/views/committees/CommitteesPermanentView.vue` (لإدارة اللجان الدائمة).
2. إنشاء صفحة `frontend/src/views/committees/CommitteesTemporaryView.vue` (لإدارة اللجان المؤقتة).
3. إنشاء صفحة `frontend/src/views/approvals/ApprovalsView.vue` (لعرض المهام والاعتمادات المعلقة).
4. تحديث `frontend/src/router/index.ts` لربط هذه المسارات بالمكونات الجديدة بدلاً من `DashboardView.vue`.
5. إنشاء Services في `frontend/src/services/` للتواصل مع `CommitteeEndpoints` في الباك إند.
6. الالتزام بقواعد التصميم (RTL/LTR، Tailwind logical properties، English numerals).

**خطوات العمل:**
1. راجع `CommitteeEndpoints.cs` في الباك إند لمعرفة الـ APIs المتاحة.
2. قم بإنشاء الصفحات المطلوبة.
3. قم بتحديث الراوتر.
4. تأكد من نجاح البناء (`pnpm run build`).
5. حدث `PROGRESS.md` وارفع التغييرات إلى GitHub.
```

---

## TASK-903: بناء صفحات الإعدادات وإدارة المستخدمين (Settings & Users)

**الهدف:** استبدال الـ Stubs بصفحات حقيقية لإدارة إعدادات المنظمة، المستخدمين، والأدوار.

**البرومبت (انسخ النص التالي في محادثة جديدة):**

```text
أنت وكيل تطوير (Developer Agent) تعمل على مشروع Tendex AI.
مهمتك هي TASK-903: بناء صفحات الإعدادات وإدارة المستخدمين في الواجهة الأمامية.

**المشكلة الحالية:**
في `frontend/src/router/index.ts`، مسارات الإعدادات (Organization, Users, Roles) تشير إلى `DashboardView.vue`. الباك إند يحتوي على `UserManagementEndpoints.cs` و `TenantEndpoints.cs` لكن الواجهات مفقودة.

**المطلوب تنفيذه في الفرونت إند:**
1. إنشاء `frontend/src/views/settings/OrganizationSettingsView.vue` (لعرض/تعديل بيانات الـ Tenant الحالي مثل الشعار والاسم).
2. إنشاء `frontend/src/views/settings/UsersManagementView.vue` (لعرض قائمة المستخدمين، إضافتهم، وتعديل أدوارهم).
3. إنشاء `frontend/src/views/settings/RolesManagementView.vue` (لعرض الأدوار المتاحة في النظام).
4. تحديث `frontend/src/router/index.ts` لربط المسارات بالمكونات الجديدة.
5. إنشاء/تحديث Services للتواصل مع `UserManagementEndpoints` و `TenantEndpoints`.

**خطوات العمل:**
1. راجع الـ Endpoints الخاصة بالمستخدمين والـ Tenants في الباك إند.
2. قم بإنشاء الصفحات المطلوبة باستخدام PrimeVue (DataTables, Dialogs, Forms).
3. قم بتحديث الراوتر.
4. تأكد من نجاح البناء (`pnpm run build`).
5. حدث `PROGRESS.md` وارفع التغييرات إلى GitHub.
```

---

## TASK-904: بناء صفحات التقارير، الاستفسارات، ومساعد الذكاء الاصطناعي

**الهدف:** استكمال بقية الصفحات المفقودة في النظام.

**البرومبت (انسخ النص التالي في محادثة جديدة):**

```text
أنت وكيل تطوير (Developer Agent) تعمل على مشروع Tendex AI.
مهمتك هي TASK-904: بناء صفحات التقارير، الاستفسارات، ومساعد الذكاء الاصطناعي.

**المشكلة الحالية:**
مسارات Reports, Inquiries, و AiAssistant تشير إلى `DashboardView.vue`.

**المطلوب تنفيذه:**
1. إنشاء `frontend/src/views/reports/ReportsView.vue` (واجهة لعرض التقارير، يمكن أن تكون مبدئية تحتوي على رسوم بيانية من PrimeVue/Chart.js).
2. إنشاء `frontend/src/views/inquiries/InquiriesView.vue` (واجهة لإدارة الاستفسارات على كراسات الشروط).
3. إنشاء `frontend/src/views/ai/AiAssistantView.vue` (واجهة دردشة مع مساعد الذكاء الاصطناعي، تتصل بـ `AiGatewayEndpoints.cs` أو `AiSpecificationDraftingEndpoints.cs`).
4. تحديث `frontend/src/router/index.ts` لربط المسارات.
5. إذا كانت الـ Endpoints غير موجودة في الباك إند للتقارير والاستفسارات، قم بإنشاء Endpoints مبسطة ترجع قوائم فارغة مؤقتاً حتى لا يتعطل الفرونت إند.

**خطوات العمل:**
1. قم بإنشاء الصفحات المطلوبة.
2. قم بتحديث الراوتر.
3. تأكد من نجاح البناء (`pnpm run build`).
4. حدث `PROGRESS.md` وارفع التغييرات إلى GitHub.
```

---

## TASK-905: تحسينات البنية التحتية (OpenIddict & Tenant Resolution)

**الهدف:** حل مشكلة مفتاح التشفير المؤقت لـ OpenIddict، وتحسين آلية تحديد الـ Tenant.

**البرومبت (انسخ النص التالي في محادثة جديدة):**

```text
أنت وكيل تطوير (Developer Agent) تعمل على مشروع Tendex AI.
مهمتك هي TASK-905: تحسينات البنية التحتية والأمان.

**المطلوب تنفيذه في الباك إند:**
1. **OpenIddict Persistent Key:**
   - حالياً يتم استخدام `AddEphemeralSigningKey()` و `AddEphemeralEncryptionKey()` في إعدادات OpenIddict، مما يؤدي إلى إبطال جميع الـ Tokens عند إعادة تشغيل الخادم.
   - قم بتعديل الإعدادات لتوليد وحفظ مفاتيح RSA في قاعدة البيانات أو في ملفات مشفرة (باستخدام `AddDevelopmentEncryptionCertificate` و `AddDevelopmentSigningCertificate` للبيئة التطويرية، أو استخدام شهادات حقيقية/مفاتيح محفوظة في الإنتاج).
2. **Tenant Resolution Fix:**
   - تأكد من أن الـ Endpoint `/api/v1/tenants/resolve` يعمل بشكل مثالي مع الـ CORS.
   - تأكد من أن الفرونت إند يخزن الـ `tenantId` بشكل صحيح ولا يفقده عند تحديث الصفحة.

**خطوات العمل:**
1. قم بتعديل إعدادات OpenIddict في `DependencyInjection.cs` أو ملف الإعدادات الخاص به.
2. اختبر تسجيل الدخول وتوليد الـ Token.
3. قم ببناء الباك إند.
4. حدث `PROGRESS.md` وارفع التغييرات إلى GitHub.
```

---

## TASK-906: إعادة النشر على السيرفر (Deployment Update)

**الهدف:** تحديث المنصة الحية على السيرفر (netaq.pro) بجميع التغييرات والصفحات الجديدة التي تم بناؤها خلال Sprint 9، وتشغيل الـ Migrations الجديدة.

**البرومبت (انسخ ما يلي في محادثة جديدة):**

```text
أنت وكيل مسؤول عن عمليات النشر (DevOps/Deployment). مهمتك هي تنفيذ TASK-906 لإعادة نشر منصة Tendex AI على خادم الإنتاج لتشمل جميع تحديثات Sprint 9.

**معلومات الخادم:**
- IP: 187.124.41.141
- User: root
- الدومين: netaq.pro
- (كلمة المرور موجودة في تعليمات المشروع Project Instructions)

**الخطوات المطلوبة:**
1. الاتصال بالخادم عبر SSH.
2. الانتقال إلى مجلد المشروع `/opt/tendex-ai` وسحب آخر التحديثات من GitHub (`git pull origin main`).
3. استخدام سكربت النشر الموجود لإجراء تحديث بدون توقف: `./infrastructure/scripts/deploy.sh update`
4. **خطوة حرجة (Migrations):** بعد اكتمال التحديث، يجب الدخول إلى حاوية الباك إند وتشغيل الـ Migrations لتطبيق جدول `Notifications` الجديد على قاعدة البيانات:
   `docker exec -it tendex-backend dotnet ef database update --project src/TendexAI.Infrastructure --startup-project src/TendexAI.API`
5. التحقق من صحة عمل المنصة:
   - فحص `https://netaq.pro/api/v1/health`
   - فحص واجهة المستخدم `https://netaq.pro/`
6. تحديث ملف `PROGRESS.md` في المستودع ليعكس اكتمال TASK-906.
7. تقديم تقرير نهائي يوضح حالة الحاويات ونجاح عملية النشر.

**ملاحظة:** لا تقم بتغيير أي إعدادات في `.env.prod` أو `docker-compose.prod.yml` ما لم يكن ذلك ضرورياً جداً. استخدم السكربتات المتاحة.
```
