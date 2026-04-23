<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'

const { locale } = useI18n()
const route = useRoute()

const isArabic = computed(() => locale.value === 'ar')
const variant = computed(() => String(route.meta.legalVariant ?? 'privacy'))

const content = computed(() => {
  if (variant.value === 'terms') {
    return isArabic.value
      ? {
          title: 'شروط الاستخدام',
          subtitle: 'توضح هذه الصفحة الشروط العامة المنظمة لاستخدام منصة Tendex AI داخل الجهة.',
          sections: [
            {
              heading: 'نطاق الاستخدام',
              body:
                'تُستخدم المنصة لإدارة إجراءات المنافسات والاعتمادات والمهام ذات الصلة وفق الصلاحيات الممنوحة لكل مستخدم داخل الجهة. ويجب استخدام المنصة للأغراض الرسمية فقط وضمن الأنظمة والسياسات المعتمدة.',
            },
            {
              heading: 'مسؤولية المستخدم',
              body:
                'يلتزم كل مستخدم بصحة البيانات التي يُدخلها، والمحافظة على سرية وسائل الوصول، وعدم مشاركة الحساب مع أي طرف آخر. كما يتحمل المستخدم مسؤولية أي إجراء يتم من خلال حسابه ما لم يتم الإبلاغ عن إساءة الاستخدام وفق القنوات المعتمدة.',
            },
            {
              heading: 'سلامة البيانات والإجراءات',
              body:
                'تعتمد المنصة على سير عمل مترابط بين الوحدات المختلفة، لذلك قد تؤثر أي مدخلات غير مكتملة أو غير صحيحة على التقارير والاعتمادات ومراكز المهام. يجب مراجعة البيانات قبل الحفظ أو الإرسال لضمان الاتساق التشغيلي.',
            },
            {
              heading: 'الدعم والتحديثات',
              body:
                'قد يتم تحديث المنصة وتحسين وظائفها بشكل دوري لمعالجة المشكلات التقنية أو تلبية المتطلبات التشغيلية. ويتم تقديم الدعم الفني من خلال القنوات المخصصة داخل المنصة بحسب الصلاحيات المتاحة.',
            },
          ],
        }
      : {
          title: 'Terms of Service',
          subtitle: 'This page outlines the general terms governing the use of Tendex AI within the entity.',
          sections: [
            {
              heading: 'Scope of Use',
              body:
                'The platform is intended for managing procurement workflows, approvals, and related operational tasks according to the permissions granted to each user. It must be used only for official purposes and in accordance with approved policies.',
            },
            {
              heading: 'User Responsibility',
              body:
                'Each user is responsible for the accuracy of submitted data, protecting access credentials, and not sharing the account with any other party. Actions performed through the account remain the responsibility of the user unless misuse is formally reported.',
            },
            {
              heading: 'Data and Process Integrity',
              body:
                'The platform depends on connected workflows across multiple modules. Incomplete or inaccurate input may affect reports, approvals, and task visibility. Users should review data carefully before saving or submitting.',
            },
            {
              heading: 'Support and Updates',
              body:
                'The platform may be updated periodically to resolve technical issues or improve operational capabilities. Technical support is provided through the designated in-platform channels, subject to available permissions.',
            },
          ],
        }
  }

  return isArabic.value
    ? {
        title: 'سياسة الخصوصية',
        subtitle: 'توضح هذه الصفحة المبادئ العامة لمعالجة البيانات داخل منصة Tendex AI.',
        sections: [
          {
            heading: 'البيانات التي تتم معالجتها',
            body:
              'تقوم المنصة بمعالجة البيانات التشغيلية اللازمة لإدارة المنافسات والاعتمادات والمراسلات والملفات ذات الصلة، بما في ذلك بيانات المستخدمين، والمستندات المرفوعة، وسجلات التدقيق المطلوبة لدعم الامتثال والحوكمة.',
          },
          {
            heading: 'أغراض المعالجة',
            body:
              'تُستخدم البيانات لدعم تنفيذ إجراءات العمل، وتمكين الصلاحيات، وتحسين تتبع المهام، وحماية سلامة القرارات والاعتمادات داخل الجهة. ولا ينبغي استخدام البيانات خارج نطاق الغرض المؤسسي المعتمد.',
          },
          {
            heading: 'الحماية وسرية المعلومات',
            body:
              'تُطبَّق ضوابط حماية وصلاحيات وصول على مستوى المستخدم والجهة للمساعدة في تقليل الوصول غير المصرح به. كما تخضع الأنشطة المهمة لسجلات تدقيق بما يضمن إمكانية التتبع والمراجعة.',
          },
          {
            heading: 'قنوات الدعم',
            body:
              'في حال وجود استفسارات أو ملاحظات تتعلق باستخدام المنصة أو البيانات الظاهرة فيها، يجب استخدام قنوات الدعم الفني المعتمدة داخل المنصة لرفع الطلب ومتابعته وفق إجراءات الجهة.',
          },
        ],
      }
    : {
        title: 'Privacy Policy',
        subtitle: 'This page describes the general principles for handling data within Tendex AI.',
        sections: [
          {
            heading: 'Data Processed by the Platform',
            body:
              'The platform processes operational data required for managing procurements, approvals, correspondence, and related files, including user data, uploaded documents, and audit records needed for governance and compliance.',
          },
          {
            heading: 'Processing Purpose',
            body:
              'Data is used to support workflow execution, enable permissions, improve task tracking, and preserve the integrity of decisions and approvals within the entity. It should not be used outside the approved institutional purpose.',
          },
          {
            heading: 'Protection and Confidentiality',
            body:
              'User- and tenant-level access controls are applied to reduce unauthorized access. Important activities are also covered by audit records to support traceability and review.',
          },
          {
            heading: 'Support Channels',
            body:
              'For questions or concerns related to platform usage or visible data, users should rely on the designated in-platform support channels to submit and track requests according to entity procedures.',
          },
        ],
      }
})
</script>

<template>
  <div class="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-8 sm:px-6 lg:px-8">
    <div class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-sm sm:p-8">
      <div class="max-w-3xl">
        <p class="text-sm font-semibold text-primary">
          {{ variant === 'terms' ? (isArabic ? 'الشروط والأحكام' : 'Terms & Conditions') : (isArabic ? 'الخصوصية وحماية البيانات' : 'Privacy & Data Protection') }}
        </p>
        <h1 class="mt-2 text-2xl font-bold text-secondary-900 sm:text-3xl">
          {{ content.title }}
        </h1>
        <p class="mt-3 text-sm leading-7 text-secondary-600 sm:text-base">
          {{ content.subtitle }}
        </p>
      </div>
    </div>

    <div class="grid gap-4">
      <section
        v-for="section in content.sections"
        :key="section.heading"
        class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-sm"
      >
        <h2 class="text-lg font-semibold text-secondary-900">
          {{ section.heading }}
        </h2>
        <p class="mt-3 text-sm leading-7 text-secondary-600 sm:text-base">
          {{ section.body }}
        </p>
      </section>
    </div>
  </div>
</template>
