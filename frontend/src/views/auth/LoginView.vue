<script setup lang="ts">
/**
 * LoginView — Main authentication screen.
 *
 * Features:
 * - Email + Password login with real-time validation (VeeValidate + Zod)
 * - Automatic tenant resolution by hostname (no manual tenant ID entry)
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
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

/* ------------------------------------------------------------------ */
/*  Tenant Auto-Resolution                                             */
/* ------------------------------------------------------------------ */

const tenantResolved = ref(false)
const tenantError = ref(false)

/**
 * Automatically resolves the tenant from the current hostname.
 * Called on mount so the user never needs to manually set tenant_id.
 */
async function autoResolveTenant(): Promise<void> {
  // If tenant_id already exists in localStorage (from a previous login), use it
  const existingTenantId = localStorage.getItem('tenant_id')
  if (existingTenantId && existingTenantId !== '00000000-0000-0000-0000-000000000000') {
    authStore.setTenantId(existingTenantId)
    tenantResolved.value = true
    return
  }

  try {
    const hostname = window.location.hostname
    const resolved = await resolveTenantByHostname(hostname)
    authStore.setTenantId(resolved.id)
    tenantResolved.value = true
  } catch {
    // Fallback: if resolve fails, still allow login attempt
    tenantError.value = true
    tenantResolved.value = true
  }
}

onMounted(() => {
  autoResolveTenant()
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
        <div
          class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary"
        >
          <i class="pi pi-shield text-3xl text-white"></i>
        </div>
        <h1 class="text-2xl font-bold text-white">
          {{ t('app.name') }}
        </h1>
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
