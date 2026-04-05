<script setup lang="ts">
/**
 * LoginView — Main authentication screen.
 *
 * Features:
 * - Email + Password login with real-time validation (VeeValidate + Zod)
 * - Automatic tenant resolution by hostname/subdomain (no manual tenant ID entry)
 * - Dynamic tenant branding: displays tenant name, logo, and colors
 * - Differentiates between operator login (netaq.pro) and tenant login (mof.netaq.pro)
 * - MFA redirect when required
 * - Clear error messages
 * - RTL/LTR support via logical Tailwind properties
 * - PrimeVue 4 components
 */
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useAuthStore } from '@/stores/auth'
import { resolveTenantByHostname } from '@/services/tenantService'
import type { TenantResolveDto } from '@/types/tenant'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'

const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()

/* ------------------------------------------------------------------ */
/*  Tenant Auto-Resolution                                             */
/* ------------------------------------------------------------------ */

const tenantResolved = ref(false)
const tenantError = ref(false)
const tenantInfo = ref<TenantResolveDto | null>(null)
const isOperatorLogin = ref(false)

/**
 * Determines if the current hostname is the base/operator domain.
 * Base domain = netaq.pro or www.netaq.pro (operator login).
 * Subdomain = mof.netaq.pro, edu.netaq.pro, etc. (tenant login).
 */
function detectSubdomain(hostname: string): { isOperator: boolean; subdomain: string | null } {
  const parts = hostname.split('.')

  // localhost or IP-based access (development)
  if (hostname === 'localhost' || /^\d+\.\d+\.\d+\.\d+$/.test(hostname)) {
    return { isOperator: true, subdomain: null }
  }

  // Base domain (netaq.pro) or www (www.netaq.pro) → operator login
  if (parts.length <= 2 || (parts.length === 3 && parts[0] === 'www')) {
    return { isOperator: true, subdomain: null }
  }

  // Subdomain detected (e.g., mof.netaq.pro)
  const subdomain = parts[0]
  return { isOperator: false, subdomain }
}

/**
 * Automatically resolves the tenant from the current hostname.
 * Always re-resolves based on current hostname to ensure correct tenant
 * when switching between subdomains.
 */
async function autoResolveTenant(): Promise<void> {
  const hostname = window.location.hostname
  const { isOperator } = detectSubdomain(hostname)
  isOperatorLogin.value = isOperator

  // Always clear previous tenant data when visiting login page
  // to ensure fresh resolution based on current hostname
  localStorage.removeItem('tenant_id')
  localStorage.removeItem('tenant_branding')
  sessionStorage.removeItem('tenant_branding')

  try {
    const resolved = await resolveTenantByHostname(hostname)
    tenantInfo.value = resolved
    authStore.setTenantId(resolved.id)

    // Store branding info for use in other components
    sessionStorage.setItem('tenant_branding', JSON.stringify(resolved))

    tenantResolved.value = true
  } catch {
    tenantError.value = true
    tenantResolved.value = true
  }
}

onMounted(() => {
  autoResolveTenant()
})

/* ------------------------------------------------------------------ */
/*  Computed Properties for Tenant Display                             */
/* ------------------------------------------------------------------ */

/** Tenant display name based on current locale */
const tenantDisplayName = computed(() => {
  if (!tenantInfo.value) return ''
  return locale.value === 'ar' ? tenantInfo.value.nameAr : tenantInfo.value.nameEn
})

/** Whether the tenant has a custom logo */
const hasLogo = computed(() => {
  return tenantInfo.value?.logoUrl && tenantInfo.value.logoUrl.trim() !== ''
})

/** Primary color from tenant branding */
const primaryColor = computed(() => {
  return tenantInfo.value?.primaryColor || '#1E40AF'
})

/* ------------------------------------------------------------------ */
/*  Validation Schema                                                  */
/* ------------------------------------------------------------------ */

const loginSchema = toTypedSchema(
  z.object({
    email: z
      .string()
      .min(1, 'auth.validation.emailRequired')
      .email('auth.validation.emailInvalid'),
    password: z
      .string()
      .min(1, 'auth.validation.passwordRequired')
      .min(8, 'auth.validation.passwordMinLength'),
  }),
)

const { handleSubmit } = useForm({
  validationSchema: loginSchema,
})

const { value: email, errorMessage: emailError } = useField<string>('email')
const { value: password, errorMessage: passwordError } = useField<string>('password')

/* ------------------------------------------------------------------ */
/*  Local State                                                        */
/* ------------------------------------------------------------------ */

const showPassword = ref(false)

const isSubmitting = computed(() => authStore.isLoading)
const serverError = computed(() => authStore.error)

/* ------------------------------------------------------------------ */
/*  Form Submission                                                    */
/* ------------------------------------------------------------------ */

const onSubmit = handleSubmit(async (values) => {
  const tenantId = authStore.tenantId || '00000000-0000-0000-0000-000000000000'

  const success = await authStore.loginAction({
    email: values.email,
    password: values.password,
    tenantId,
  })

  if (success) {
    if (authStore.mfaRequired) {
      router.push({ name: 'MfaVerify' })
    } else {
      // Redirect based on context:
      // - Operator login → Operator Dashboard
      // - Tenant login → Tenant Dashboard
      const redirectTo = (router.currentRoute.value.query.redirect as string) || '/'
      router.push(redirectTo)
    }
  }
})
</script>

<template>
  <div class="flex min-h-screen w-full items-center justify-center bg-secondary p-4">
    <div class="w-full max-w-md">
      <!-- Logo & Title -->
      <div class="mb-8 text-center">
        <!-- Tenant Logo or Default Icon -->
        <div
          v-if="hasLogo"
          class="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-2xl bg-white p-2 shadow-md"
        >
          <img
            :src="tenantInfo!.logoUrl!"
            :alt="tenantDisplayName"
            class="h-full w-full object-contain"
          />
        </div>
        <div
          v-else
          class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
          :style="{ backgroundColor: primaryColor }"
        >
          <i
            :class="isOperatorLogin ? 'pi pi-cog' : 'pi pi-building'"
            class="text-3xl text-white"
          ></i>
        </div>

        <!-- Platform Name -->
        <h1 class="text-2xl font-bold text-white">
          {{ t('app.name') }}
        </h1>

        <!-- Tenant Name (for government entities) -->
        <p
          v-if="tenantInfo && !isOperatorLogin"
          class="mt-2 text-lg font-semibold text-white/90"
        >
          {{ tenantDisplayName }}
        </p>

        <!-- Operator Badge -->
        <p
          v-if="isOperatorLogin && tenantResolved"
          class="mt-2 text-sm font-medium text-white/70"
        >
          {{ t('auth.operatorPanel', 'لوحة تحكم مشغل المنصة') }}
        </p>

        <p class="mt-1 text-sm text-white/60">
          {{ t('auth.loginSubtitle') }}
        </p>
      </div>

      <!-- Login Card -->
      <div class="rounded-2xl bg-white p-8 shadow-lg">
        <h2 class="mb-6 text-center text-xl font-semibold text-secondary">
          {{ t('auth.login') }}
        </h2>

        <!-- Server Error -->
        <Message
          v-if="serverError"
          severity="error"
          :closable="false"
          class="mb-4"
        >
          {{ t(serverError) }}
        </Message>

        <!-- Tenant Resolution Error -->
        <Message
          v-if="tenantError"
          severity="warn"
          :closable="false"
          class="mb-4"
        >
          {{ t('auth.tenantResolutionError', 'تعذر تحديد الجهة تلقائياً. يرجى المحاولة مرة أخرى.') }}
        </Message>

        <!-- Loading Tenant Resolution -->
        <div
          v-if="!tenantResolved && !tenantError"
          class="mb-4 flex items-center justify-center gap-2 text-sm text-secondary/60"
        >
          <i class="pi pi-spin pi-spinner"></i>
          {{ t('auth.resolvingTenant', 'جاري تحديد الجهة...') }}
        </div>

        <form @submit.prevent="onSubmit" novalidate>
          <!-- Email Field -->
          <div class="mb-5">
            <label
              for="email"
              class="mb-2 block text-sm font-medium text-secondary"
            >
              {{ t('auth.email') }}
            </label>
            <div class="relative">
              <span
                class="pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-secondary/40"
              >
                <i class="pi pi-envelope"></i>
              </span>
              <InputText
                id="email"
                v-model="email"
                type="email"
                :placeholder="t('auth.emailPlaceholder')"
                class="w-full ps-10"
                :class="{ 'p-invalid': emailError }"
                autocomplete="email"
                dir="ltr"
              />
            </div>
            <small v-if="emailError" class="mt-1 block text-xs text-danger">
              {{ t(emailError) }}
            </small>
          </div>

          <!-- Password Field -->
          <div class="mb-5">
            <div class="mb-2 flex items-center justify-between">
              <label
                for="password"
                class="text-sm font-medium text-secondary"
              >
                {{ t('auth.password') }}
              </label>
              <router-link
                :to="{ name: 'ForgotPassword' }"
                class="text-xs font-medium text-primary hover:text-primary-dark transition-colors"
              >
                {{ t('auth.forgotPassword') }}
              </router-link>
            </div>
            <div class="relative">
              <span
                class="pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-secondary/40"
              >
                <i class="pi pi-lock"></i>
              </span>
              <InputText
                id="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                :placeholder="t('auth.passwordPlaceholder')"
                class="w-full ps-10 pe-10"
                :class="{ 'p-invalid': passwordError }"
                autocomplete="current-password"
                dir="ltr"
              />
              <button
                type="button"
                class="absolute inset-y-0 end-0 flex items-center pe-3 text-secondary/40 hover:text-secondary/70 transition-colors"
                @click="showPassword = !showPassword"
                :aria-label="showPassword ? t('auth.hidePassword') : t('auth.showPassword')"
              >
                <i :class="showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'"></i>
              </button>
            </div>
            <small v-if="passwordError" class="mt-1 block text-xs text-danger">
              {{ t(passwordError) }}
            </small>
          </div>

          <!-- Submit Button -->
          <Button
            type="submit"
            :label="t('auth.loginButton')"
            :loading="isSubmitting"
            :disabled="isSubmitting || !tenantResolved"
            class="mt-2 w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
            icon="pi pi-sign-in"
            iconPos="left"
          />
        </form>
      </div>

      <!-- Footer -->
      <p class="mt-6 text-center text-xs text-white/40">
        {{ t('footer.copyright') }}
      </p>
    </div>
  </div>
</template>
