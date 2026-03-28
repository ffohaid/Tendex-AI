# دليل تنفيذ مهام Sprint 7 (Testing & Deployment)

هذا الملف يحتوي على البرومبتات الجاهزة للنسخ واللصق لكل مهمة في Sprint 7.
يجب فتح محادثة جديدة مع Manus لكل مهمة ونسخ البرومبت الخاص بها.

## ملاحظات هامة قبل البدء في Sprint 7:
- يجب التأكد من تشغيل جميع الـ Migrations المعلقة (بما في ذلك ImpersonationSessions و ImpersonationConsents من TASK-603).
- يجب تنفيذ نقاط النهاية المفقودة (forgot-password و reset-password) في الباك إند.
- إعداد Docker Compose للإنتاج مع إدارة الأسرار بشكل صحيح.
- إعداد مسار النشر المستمر (CD pipeline).

---

## TASK-701: Integration Testing Suite

**البرومبت:**
```text
أنت وكيل تطوير تعمل على مشروع Tendex AI.
مهمتك الحالية هي TASK-701: Integration Testing Suite.

الرجاء تنفيذ المهام التالية:
1. إعداد بيئة اختبارات التكامل (Integration Tests) للباك إند باستخدام xUnit و Testcontainers (SQL Server, Redis, RabbitMQ).
2. كتابة اختبارات تكامل لتدفقات العمل الأساسية (RFP Creation, Committee Evaluation).
3. كتابة اختبارات تكامل لنظام المصادقة وإدارة الجلسات.
4. التأكد من تغطية جميع الحالات الحرجة (Edge Cases).
5. إضافة أوامر تشغيل الاختبارات إلى CI pipeline.
6. تنفيذ نقاط النهاية المفقودة (forgot-password و reset-password) في الباك إند (تم تجهيز الفرونت إند مسبقاً في TASK-502).
7. إنشاء وتشغيل EF Core Migrations للجداول الجديدة (ImpersonationSessions و ImpersonationConsents من TASK-603).

يجب الالتزام بـ Clean Architecture ومعايير جودة الكود.
بعد الانتهاء، قم بتحديث ملف PROGRESS.md ورفع التغييرات إلى GitHub.
```

---

## TASK-702: End-to-End Testing (Playwright)

**البرومبت:**
```text
أنت وكيل تطوير تعمل على مشروع Tendex AI.
مهمتك الحالية هي TASK-702: End-to-End Testing (Playwright).

الرجاء تنفيذ المهام التالية:
1. إعداد Playwright في مجلد الفرونت إند لاختبارات E2E.
2. كتابة اختبارات E2E للسيناريوهات التالية:
   - تسجيل الدخول وإدارة الجلسة.
   - إنشاء طلب عرض (RFP) جديد.
   - تقييم العروض الفنية والمالية.
   - لوحة تحكم المشغل (Operator Dashboard) وميزة انتحال الشخصية (Impersonation).
3. التأكد من دعم الاختبارات للغتين (العربية والإنجليزية) واتجاهات الشاشة (RTL/LTR).
4. دمج تشغيل اختبارات Playwright في CI pipeline الخاص بالفرونت إند.

يجب الالتزام بمعايير جودة الكود.
بعد الانتهاء، قم بتحديث ملف PROGRESS.md ورفع التغييرات إلى GitHub.
```

---

## TASK-703: Performance Testing & Optimization

**البرومبت:**
```text
أنت وكيل تطوير تعمل على مشروع Tendex AI.
مهمتك الحالية هي TASK-703: Performance Testing & Optimization.

الرجاء تنفيذ المهام التالية:
1. إعداد اختبارات أداء باستخدام k6 أو أداة مشابهة لاختبار نقاط النهاية الحرجة (مثل استرجاع قائمة RFPs، والبحث في Qdrant).
2. تحليل وتحسين استعلامات Entity Framework Core (مثل إضافة Indexes، وتجنب N+1 query problems).
3. تحسين أداء الفرونت إند (Lazy loading, Code splitting, Image optimization).
4. مراجعة وتحسين استخدام Redis للتخزين المؤقت (Caching) لتقليل الحمل على قاعدة البيانات.
5. توثيق نتائج اختبارات الأداء قبل وبعد التحسينات في ملف `docs/Performance_Report.md`.

يجب الالتزام بمعايير جودة الكود.
بعد الانتهاء، قم بتحديث ملف PROGRESS.md ورفع التغييرات إلى GitHub.
```

---

## TASK-704: Production Deployment Setup

**البرومبت:**
```text
أنت وكيل تطوير تعمل على مشروع Tendex AI.
مهمتك الحالية هي TASK-704: Production Deployment Setup.

الرجاء تنفيذ المهام التالية:
1. إعداد ملف `docker-compose.prod.yml` مخصص لبيئة الإنتاج (Hostinger VPS) مع إعدادات الأمان المناسبة (لا تستخدم كلمات مرور افتراضية، استخدم Docker Secrets أو ملفات .env آمنة).
2. إعداد Nginx أو Traefik كـ Reverse Proxy مع دعم SSL/TLS.
3. إعداد مسار النشر المستمر (CD pipeline) في GitHub Actions للنشر التلقائي على خادم Hostinger عند الدفع إلى فرع `main`.
4. كتابة سكربتات النشر (Deployment Scripts) لتحديث الحاويات وقواعد البيانات بأمان (Zero-downtime deployment إن أمكن).
5. توثيق خطوات النشر اليدوي والآلي في ملف `docs/Deployment_Guide.md`.

يجب الالتزام بتعليمات المشروع الرئيسية (Project Instructions) وخاصة قسم بيئة العمل والنشر.
بعد الانتهاء، قم بتحديث ملف PROGRESS.md ورفع التغييرات إلى GitHub.
```
