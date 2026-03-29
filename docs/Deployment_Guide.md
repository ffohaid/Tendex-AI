# دليل النشر لمنصة Tendex AI (Deployment Guide)

يوفر هذا الدليل إرشادات شاملة لنشر منصة Tendex AI على خوادم Hostinger VPS باستخدام Docker و Docker Compose، بالإضافة إلى إعدادات التكامل والتسليم المستمر (CI/CD) عبر GitHub Actions.

## 1. متطلبات النظام (System Requirements)

- **نظام التشغيل:** Ubuntu 22.04 LTS أو أحدث.
- **الموارد:** 4GB RAM كحد أدنى (يوصى بـ 8GB)، 4 vCPUs، مساحة تخزين 100GB SSD.
- **الوصول:** صلاحيات Root عبر SSH.
- **البرمجيات المطلوبة:** Docker, Docker Compose, Git, UFW (Firewall).

## 2. الإعداد الأولي للخادم (Initial Server Setup)

يجب تنفيذ هذه الخطوات **مرة واحدة فقط** عند إعداد خادم جديد.

1. اتصل بالخادم عبر SSH:
   ```bash
   ssh root@187.124.41.141
   ```

2. قم بنسخ سكربت الإعداد الأولي من المستودع وتشغيله:
   ```bash
   # افترض أنك قمت بنسخ المستودع أو السكربت إلى الخادم
   chmod +x /opt/tendex-ai/infrastructure/scripts/server-setup.sh
   /opt/tendex-ai/infrastructure/scripts/server-setup.sh
   ```
   *يقوم هذا السكربت بتحديث النظام، تثبيت Docker، إعداد جدار الحماية (UFW)، إنشاء مساحة Swap، وتجهيز هيكل المجلدات.*

3. أعد تشغيل الخادم لتطبيق جميع التغييرات:
   ```bash
   reboot
   ```

## 3. إعداد بيئة الإنتاج (Production Environment Setup)

1. انتقل إلى مجلد البنية التحتية:
   ```bash
   cd /opt/tendex-ai/infrastructure
   ```

2. قم بإنشاء ملف المتغيرات البيئية للإنتاج:
   ```bash
   cp .env.prod.example .env.prod
   ```

3. قم بتحرير ملف `.env.prod` وإدخال كلمات مرور قوية وحقيقية:
   ```bash
   nano .env.prod
   ```
   *(تنبيه أمني: لا تقم أبداً برفع ملف `.env.prod` إلى GitHub).*

## 4. إعداد شهادات SSL (SSL Certificate Setup)

نستخدم Nginx كـ Reverse Proxy مع شهادات Let's Encrypt المجانية.

1. قم بتشغيل سكربت إعداد SSL:
   ```bash
   cd /opt/tendex-ai/infrastructure/scripts
   ./init-ssl.sh tendex.yourdomain.com admin@yourdomain.com
   ```
   *يقوم هذا السكربت بإنشاء شهادة ذاتية التوقيع مؤقتاً، تشغيل Nginx، ثم طلب شهادة حقيقية من Let's Encrypt وتحديث Nginx.*

## 5. النشر اليدوي (Manual Deployment)

لإدارة الخدمات يدوياً، استخدم سكربت النشر المخصص `deploy.sh`:

```bash
cd /opt/tendex-ai/infrastructure/scripts

# بدء تشغيل جميع الخدمات
./deploy.sh up

# تحديث الواجهة الأمامية والخلفية بدون توقف (Zero-downtime)
./deploy.sh update

# عرض حالة الخدمات
./deploy.sh status

# عرض السجلات (Logs)
./deploy.sh logs backend

# إيقاف جميع الخدمات
./deploy.sh stop
```

## 6. النشر التلقائي (CI/CD Pipeline)

تم إعداد مسار نشر مستمر باستخدام GitHub Actions. عند دفع أي تغييرات (Push) إلى فرع `main`، سيتم تلقائياً:
1. تشغيل اختبارات الوحدة (Unit Tests).
2. بناء صور Docker للواجهة الأمامية والخلفية.
3. نقل الصور والملفات إلى خادم Hostinger.
4. تنفيذ النشر بدون توقف (Rolling Update).

### إعداد أسرار GitHub (GitHub Secrets)
لكي يعمل مسار النشر التلقائي، يجب إضافة الأسرار التالية في إعدادات مستودع GitHub (Settings > Secrets and variables > Actions):

- `VPS_HOST`: عنوان IP للخادم (مثال: 187.124.41.141).
- `VPS_USER`: اسم المستخدم (root).
- `VPS_SSH_KEY`: المفتاح الخاص (Private Key) للاتصال بالخادم.
- `VPS_PORT`: منفذ SSH (الافتراضي 22).

## 7. النسخ الاحتياطي والمراقبة (Backup & Monitoring)

### النسخ الاحتياطي التلقائي
تم إعداد سكربت `backup.sh` لأخذ نسخ احتياطية لقواعد البيانات والملفات. يوصى بإضافته إلى Cron Job:
```bash
crontab -e
# إضافة السطر التالي للنسخ الاحتياطي يومياً الساعة 2 صباحاً
0 2 * * * /opt/tendex-ai/infrastructure/scripts/backup.sh full >> /opt/tendex-ai/logs/backup.log 2>&1
```

### المراقبة التلقائية
تم إعداد سكربت `monitor.sh` لمراقبة صحة الحاويات وإعادة تشغيلها إذا لزم الأمر. يوصى بإضافته إلى Cron Job:
```bash
crontab -e
# إضافة السطر التالي للمراقبة كل 5 دقائق
*/5 * * * * /opt/tendex-ai/infrastructure/scripts/monitor.sh >> /opt/tendex-ai/logs/monitor.log 2>&1
```

## 8. التراجع عن التحديثات (Rollback)

في حال حدوث مشكلة بعد التحديث، يمكن التراجع إلى الإصدار السابق من صور Docker باستخدام:
```bash
cd /opt/tendex-ai/infrastructure/scripts
./deploy.sh rollback
```
