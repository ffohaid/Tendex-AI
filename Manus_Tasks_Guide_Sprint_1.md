# دليل إطلاق المهام في واجهة Manus (Sprint 1)

هذا الدليل مصمم خصيصاً لك لتعرف بالضبط كيف تفتح كل مهمة جديدة في واجهة Manus. كل ما عليك فعله هو **نسخ ولصق** النصوص أدناه في كل مرة تبدأ فيها مهمة جديدة.

## خطوات عامة قبل البدء:
1. افتح واجهة Manus.
2. تأكد أنك داخل مشروع **Tendex AI - Project**.
3. انقر على **New Task** (مهمة جديدة).
4. انسخ اسم المهمة، المرفقات المطلوبة، والنص (البرومبت) من الأسفل والصقها في الواجهة.

---

### المهمة الأولى: TASK-101 (تهيئة المستودع)

**اسم المهمة (Task Name):**
`TASK-101: تهيئة مستودع المشروع وهيكلة المجلدات`

**المرفقات المطلوبة (Attachments):**
- لا يوجد مرفقات إضافية (الوكيل سيعتمد على تعليمات المشروع الرئيسية المرفقة مسبقاً في المشروع).

**النص (Prompt) - انسخ هذا بالكامل:**
```text
أنت وكيل تطوير (DevOps/Backend Agent) تعمل على منصة Tendex AI.
مهمتك هي تنفيذ TASK-101 من Sprint 1.

المطلوب:
1. استنساخ مستودع المشروع (https://github.com/ffohaid/Tendex-AI).
2. إنشاء الهيكلة الأساسية للمشروع (Monorepo) لتشمل مجلدات: frontend, backend, infrastructure, docs.
3. إنشاء ملف .gitignore شامل في الجذر يتضمن استثناءات لـ Node.js و .NET.
4. إنشاء ملف README.md مبدئي.
5. تحديث ملف PROGRESS.md لتسجيل إنجاز المهمة.
6. رفع التغييرات إلى GitHub باستخدام رسالة Commit واضحة.

الرجاء قراءة ملف Sprint_1_Execution_Guide.md من المستودع قبل البدء لتجنب الأخطاء الشائعة.
```

---

### المهمة الثانية: TASK-102 (إعداد Docker)

**اسم المهمة (Task Name):**
`TASK-102: إعداد بيئة Docker و Docker Compose`

**المرفقات المطلوبة (Attachments):**
- `Tendex_AI_Architecture_v1.pdf`
- `Tendex_AI_Database_Quality_Standards.pdf`

**النص (Prompt) - انسخ هذا بالكامل:**
```text
أنت وكيل تطوير (DevOps Agent) تعمل على منصة Tendex AI.
مهمتك هي تنفيذ TASK-102 من Sprint 1.

المطلوب:
1. استنساخ مستودع المشروع (https://github.com/ffohaid/Tendex-AI).
2. إنشاء ملف docker-compose.yml داخل مجلد infrastructure لتهيئة الخدمات التالية: SQL Server 2022, Redis 7, RabbitMQ, MinIO, Qdrant.
3. إعداد الـ Volumes لكل خدمة لضمان حفظ البيانات.
4. إنشاء ملف .env.example لمتغيرات البيئة (لا تضع كلمات مرور حقيقية في الكود).
5. تحديث ملف PROGRESS.md لتسجيل إنجاز المهمة.
6. رفع التغييرات إلى GitHub.

الرجاء قراءة ملف Sprint_1_Execution_Guide.md من المستودع قبل البدء لتجنب الأخطاء الشائعة.
```

---

### المهمة الثالثة: TASK-103 (تهيئة الواجهة الخلفية .NET)

**اسم المهمة (Task Name):**
`TASK-103: تهيئة مشروع الواجهة الخلفية .NET 10`

**المرفقات المطلوبة (Attachments):**
- `Tendex_AI_Code_Quality_Standards.pdf`
- `Tendex_AI_Architecture_v1.pdf`

**النص (Prompt) - انسخ هذا بالكامل:**
```text
أنت وكيل تطوير (Backend .NET Agent) تعمل على منصة Tendex AI.
مهمتك هي تنفيذ TASK-103 من Sprint 1.

المطلوب:
1. استنساخ مستودع المشروع (https://github.com/ffohaid/Tendex-AI).
2. داخل مجلد backend، قم بإنشاء Solution جديد لـ .NET 10.
3. قم بتطبيق بنية Clean Architecture بإنشاء 4 مشاريع: Domain, Application, Infrastructure, API (استخدم Minimal APIs).
4. إعداد المراجع (References) بين المشاريع بشكل صحيح (يجب أن تكون طبقة Domain خالية من أي اعتماديات خارجية).
5. تحديث ملف PROGRESS.md لتسجيل إنجاز المهمة.
6. رفع التغييرات إلى GitHub.

الرجاء قراءة ملف Sprint_1_Execution_Guide.md من المستودع قبل البدء لتجنب الأخطاء الشائعة.
```

---

### المهمة الرابعة: TASK-104 (تهيئة الواجهة الأمامية Vue.js)

**اسم المهمة (Task Name):**
`TASK-104: تهيئة مشروع الواجهة الأمامية Vue.js 3`

**المرفقات المطلوبة (Attachments):**
- `Tendex_AI_Code_Quality_Standards.pdf`
- `Tendex_AI_Brand_Identity.pdf`

**النص (Prompt) - انسخ هذا بالكامل:**
```text
أنت وكيل تطوير (Frontend Vue.js Agent) تعمل على منصة Tendex AI.
مهمتك هي تنفيذ TASK-104 من Sprint 1.

المطلوب:
1. استنساخ مستودع المشروع (https://github.com/ffohaid/Tendex-AI).
2. داخل مجلد frontend، قم بتهيئة مشروع Vue 3 باستخدام Vite و TypeScript.
3. قم بتثبيت وإعداد Tailwind CSS v4 و PrimeVue 4 و Pinia.
4. قم بإعداد Tailwind لدعم الخصائص المنطقية (Logical Properties مثل ms-4) لدعم اتجاه RTL واللغة العربية.
5. تحديث ملف PROGRESS.md لتسجيل إنجاز المهمة.
6. رفع التغييرات إلى GitHub.

الرجاء قراءة ملف Sprint_1_Execution_Guide.md من المستودع قبل البدء لتجنب الأخطاء الشائعة.
```

---

### المهمة الخامسة: TASK-105 (إعداد قاعدة البيانات)
*(ملاحظة: لا تفتح هذه المهمة إلا بعد اكتمال TASK-102 و TASK-103)*

**اسم المهمة (Task Name):**
`TASK-105: إعداد قاعدة البيانات المركزية master_platform`

**المرفقات المطلوبة (Attachments):**
- `Tendex_AI_Database_Quality_Standards.pdf`

**النص (Prompt) - انسخ هذا بالكامل:**
```text
أنت وكيل تطوير (Backend/Database Agent) تعمل على منصة Tendex AI.
مهمتك هي تنفيذ TASK-105 من Sprint 1.

المطلوب:
1. استنساخ مستودع المشروع (https://github.com/ffohaid/Tendex-AI).
2. في مشروع الواجهة الخلفية (طبقة Infrastructure)، قم بإعداد Entity Framework Core مع SQL Server.
3. قم بإنشاء DbContext لقاعدة البيانات المركزية وتطبيق نموذج Database-per-Tenant.
4. تأكد من منع الحذف المتسلسل (No Cascade Deletes) باستخدام DeleteBehavior.NoAction.
5. تحديث ملف PROGRESS.md لتسجيل إنجاز المهمة.
6. رفع التغييرات إلى GitHub.

الرجاء قراءة ملف Sprint_1_Execution_Guide.md من المستودع قبل البدء لتجنب الأخطاء الشائعة.
```

---

### المهمة السادسة: TASK-106 (إعداد مسارات CI)
*(ملاحظة: لا تفتح هذه المهمة إلا بعد اكتمال TASK-103 و TASK-104)*

**اسم المهمة (Task Name):**
`TASK-106: إعداد مسارات التكامل المستمر CI Pipelines`

**المرفقات المطلوبة (Attachments):**
- `Tendex_AI_Execution_Plan.pdf`
- `Tendex_AI_Code_Quality_Standards.pdf`

**النص (Prompt) - انسخ هذا بالكامل:**
```text
أنت وكيل تطوير (DevOps Agent) تعمل على منصة Tendex AI.
مهمتك هي تنفيذ TASK-106 من Sprint 1.

المطلوب:
1. استنساخ مستودع المشروع (https://github.com/ffohaid/Tendex-AI).
2. إنشاء مجلد .github/workflows/ في جذر المشروع.
3. إنشاء ملف ci-backend.yml لأتمتة بناء واختبار مشروع .NET عند كل Pull Request.
4. إنشاء ملف ci-frontend.yml لأتمتة بناء واختبار مشروع Vue.js.
5. (ملاحظة: لا تقم بإعداد النشر المباشر CD في هذه المرحلة، فقط CI).
6. تحديث ملف PROGRESS.md لتسجيل إنجاز المهمة.
7. رفع التغييرات إلى GitHub.

الرجاء قراءة ملف Sprint_1_Execution_Guide.md من المستودع قبل البدء لتجنب الأخطاء الشائعة.
```
