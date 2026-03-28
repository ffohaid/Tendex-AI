<script setup lang="ts">
/**
 * ResetPasswordView — Set a new password using a reset token.
 *
 * Features:
 * - New password + confirm password with validation (VeeValidate + Zod)
 * - Password strength indicator
 * - Success state with redirect to login
 * - RTL/LTR support
 */
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { resetPassword } from '@/services/authService'
import { useAuthStore } from '@/stores/auth'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { isAxiosError } from 'axios'
import type { ApiProblemDetails } from '@/types/auth'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()

/* ------------------------------------------------------------------ */
/*  Route Params                                                       */
/* ------------------------------------------------------------------ */

const resetToken = computed(() => (route.query.token as string) || '')
const resetEmail = computed(() => (route.query.email as string) || '')

/* ------------------------------------------------------------------ */
/*  Validation Schema                                                  */
/* ------------------------------------------------------------------ */

const resetSchema = toTypedSchema(
  z
    .object({
      newPassword: z
        .string()
        .min(1, 'auth.validation.passwordRequired')
        .min(8, 'auth.validation.passwordMinLength')
        .regex(/[A-Z]/, 'auth.validation.passwordUppercase')
        .regex(/[a-z]/, 'auth.validation.passwordLowercase')
        .regex(/[0-9]/, 'auth.validation.passwordDigit')
        .regex(/[^A-Za-z0-9]/, 'auth.validation.passwordSpecial'),
      confirmPassword: z
        .string()
        .min(1, 'auth.validation.confirmPasswordRequired'),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
      message: 'auth.validation.passwordsMismatch',
      path: ['confirmPassword'],
    }),
)

const { handleSubmit } = useForm({
  validationSchema: resetSchema,
})

const { value: newPassword, errorMessage: newPasswordError } =
  useField<string>('newPassword')
const { value: confirmPassword, errorMessage: confirmPasswordError } =
  useField<string>('confirmPassword')

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */

const showNewPassword = ref(false)
const showConfirmPassword = ref(false)
const isSubmitting = ref(false)
const isSuccess = ref(false)
const serverError = ref<string | null>(null)

/* ------------------------------------------------------------------ */
/*  Password Strength                                                  */
/* ------------------------------------------------------------------ */

const passwordStrength = computed(() => {
  const pwd = newPassword.value || ''
  let score = 0
  if (pwd.length >= 8) score++
  if (/[A-Z]/.test(pwd)) score++
  if (/[a-z]/.test(pwd)) score++
  if (/[0-9]/.test(pwd)) score++
  if (/[^A-Za-z0-9]/.test(pwd)) score++
  return score
})

const strengthLabel = computed(() => {
  if (!newPassword.value) return ''
  if (passwordStrength.value <= 2) return t('auth.reset.strengthWeak')
  if (passwordStrength.value <= 3) return t('auth.reset.strengthMedium')
  if (passwordStrength.value <= 4) return t('auth.reset.strengthGood')
  return t('auth.reset.strengthStrong')
})

const strengthColor = computed(() => {
  if (passwordStrength.value <= 2) return 'bg-danger'
  if (passwordStrength.value <= 3) return 'bg-warning'
  if (passwordStrength.value <= 4) return 'bg-info'
  return 'bg-success'
})

/* ------------------------------------------------------------------ */
/*  Submit                                                             */
/* ------------------------------------------------------------------ */

const onSubmit = handleSubmit(async (values) => {
  if (!resetToken.value || !resetEmail.value) {
    serverError.value = 'auth.errors.invalidResetLink'
    return
  }

  isSubmitting.value = true
  serverError.value = null

  try {
    const tenantId = authStore.tenantId || '00000000-0000-0000-0000-000000000000'
    await resetPassword({
      token: resetToken.value,
      email: resetEmail.value,
      newPassword: values.newPassword,
      confirmPassword: values.confirmPassword,
      tenantId,
    })
    isSuccess.value = true
  } catch (err) {
    if (isAxiosError(err)) {
      const problem = err.response?.data as ApiProblemDetails | undefined
      if (problem?.detail) {
        serverError.value = problem.detail
      } else if (err.response?.status === 400) {
        serverError.value = 'auth.errors.invalidResetLink'
      } else if (!err.response) {
        serverError.value = 'auth.errors.networkError'
      } else {
        serverError.value = 'auth.errors.unexpectedError'
      }
    } else {
      serverError.value = 'auth.errors.unexpectedError'
    }
  } finally {
    isSubmitting.value = false
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
          <i class="pi pi-lock text-3xl text-white"></i>
        </div>
        <h1 class="text-2xl font-bold text-white">
          {{ t('auth.reset.pageTitle') }}
        </h1>
        <p class="mt-1 text-sm text-white/60">
          {{ t('auth.reset.subtitle') }}
        </p>
      </div>

      <!-- Card -->
      <div class="rounded-2xl bg-white p-8 shadow-lg">
        <!-- Success State -->
        <template v-if="isSuccess">
          <div class="text-center">
            <div
              class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10"
            >
              <i class="pi pi-check-circle text-3xl text-success"></i>
            </div>
            <h2 class="mb-2 text-lg font-semibold text-secondary">
              {{ t('auth.reset.successTitle') }}
            </h2>
            <p class="mb-6 text-sm text-secondary/60">
              {{ t('auth.reset.successMessage') }}
            </p>
            <Button
              :label="t('auth.reset.goToLogin')"
              class="w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
              icon="pi pi-sign-in"
              iconPos="left"
              @click="router.push({ name: 'Login' })"
            />
          </div>
        </template>

        <!-- Form State -->
        <template v-else>
          <h2 class="mb-2 text-center text-xl font-semibold text-secondary">
            {{ t('auth.reset.title') }}
          </h2>
          <p class="mb-6 text-center text-sm text-secondary/60">
            {{ t('auth.reset.instruction') }}
          </p>

          <!-- Invalid Link Warning -->
          <Message
            v-if="!resetToken || !resetEmail"
            severity="warn"
            :closable="false"
            class="mb-4"
          >
            {{ t('auth.errors.invalidResetLink') }}
          </Message>

          <!-- Server Error -->
          <Message
            v-if="serverError"
            severity="error"
            :closable="false"
            class="mb-4"
          >
            {{ t(serverError) }}
          </Message>

          <form @submit.prevent="onSubmit" novalidate>
            <!-- New Password -->
            <div class="mb-5">
              <label
                for="new-password"
                class="mb-2 block text-sm font-medium text-secondary"
              >
                {{ t('auth.reset.newPassword') }}
              </label>
              <div class="relative">
                <span
                  class="pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-secondary/40"
                >
                  <i class="pi pi-lock"></i>
                </span>
                <InputText
                  id="new-password"
                  v-model="newPassword"
                  :type="showNewPassword ? 'text' : 'password'"
                  :placeholder="t('auth.reset.newPasswordPlaceholder')"
                  class="w-full ps-10 pe-10"
                  :class="{ 'p-invalid': newPasswordError }"
                  autocomplete="new-password"
                  dir="ltr"
                />
                <button
                  type="button"
                  class="absolute inset-y-0 end-0 flex items-center pe-3 text-secondary/40 hover:text-secondary/70 transition-colors"
                  @click="showNewPassword = !showNewPassword"
                >
                  <i
                    :class="
                      showNewPassword ? 'pi pi-eye-slash' : 'pi pi-eye'
                    "
                  ></i>
                </button>
              </div>
              <small
                v-if="newPasswordError"
                class="mt-1 block text-xs text-danger"
              >
                {{ t(newPasswordError) }}
              </small>

              <!-- Password Strength Indicator -->
              <div v-if="newPassword" class="mt-2">
                <div class="flex gap-1">
                  <div
                    v-for="i in 5"
                    :key="i"
                    class="h-1 flex-1 rounded-full transition-colors"
                    :class="
                      i <= passwordStrength ? strengthColor : 'bg-surface-dim'
                    "
                  ></div>
                </div>
                <p class="mt-1 text-xs text-secondary/50">
                  {{ strengthLabel }}
                </p>
              </div>
            </div>

            <!-- Confirm Password -->
            <div class="mb-5">
              <label
                for="confirm-password"
                class="mb-2 block text-sm font-medium text-secondary"
              >
                {{ t('auth.reset.confirmPassword') }}
              </label>
              <div class="relative">
                <span
                  class="pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-secondary/40"
                >
                  <i class="pi pi-lock"></i>
                </span>
                <InputText
                  id="confirm-password"
                  v-model="confirmPassword"
                  :type="showConfirmPassword ? 'text' : 'password'"
                  :placeholder="t('auth.reset.confirmPasswordPlaceholder')"
                  class="w-full ps-10 pe-10"
                  :class="{ 'p-invalid': confirmPasswordError }"
                  autocomplete="new-password"
                  dir="ltr"
                />
                <button
                  type="button"
                  class="absolute inset-y-0 end-0 flex items-center pe-3 text-secondary/40 hover:text-secondary/70 transition-colors"
                  @click="showConfirmPassword = !showConfirmPassword"
                >
                  <i
                    :class="
                      showConfirmPassword ? 'pi pi-eye-slash' : 'pi pi-eye'
                    "
                  ></i>
                </button>
              </div>
              <small
                v-if="confirmPasswordError"
                class="mt-1 block text-xs text-danger"
              >
                {{ t(confirmPasswordError) }}
              </small>
            </div>

            <!-- Submit Button -->
            <Button
              type="submit"
              :label="t('auth.reset.resetButton')"
              :loading="isSubmitting"
              :disabled="isSubmitting || (!resetToken || !resetEmail)"
              class="w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
              icon="pi pi-refresh"
              iconPos="left"
            />
          </form>

          <!-- Back to Login -->
          <div class="mt-6 border-t border-surface-dim pt-4 text-center">
            <router-link
              :to="{ name: 'Login' }"
              class="inline-flex items-center gap-1 text-sm text-secondary/60 hover:text-secondary transition-colors"
            >
              <i class="pi pi-arrow-left text-xs rtl:rotate-180"></i>
              {{ t('auth.reset.backToLogin') }}
            </router-link>
          </div>
        </template>
      </div>

      <!-- Footer -->
      <p class="mt-6 text-center text-xs text-white/40">
        {{ t('footer.copyright') }}
      </p>
    </div>
  </div>
</template>
