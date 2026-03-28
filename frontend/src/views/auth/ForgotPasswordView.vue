<script setup lang="ts">
/**
 * ForgotPasswordView — Request password reset link.
 *
 * Features:
 * - Email input with validation (VeeValidate + Zod)
 * - Success state with instructions
 * - Error handling
 * - RTL/LTR support
 */
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { forgotPassword } from '@/services/authService'
import { useAuthStore } from '@/stores/auth'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { isAxiosError } from 'axios'
import type { ApiProblemDetails } from '@/types/auth'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

/* ------------------------------------------------------------------ */
/*  Validation Schema                                                  */
/* ------------------------------------------------------------------ */

const forgotSchema = toTypedSchema(
  z.object({
    email: z
      .string()
      .min(1, 'auth.validation.emailRequired')
      .email('auth.validation.emailInvalid'),
  }),
)

const { handleSubmit } = useForm({
  validationSchema: forgotSchema,
})

const { value: email, errorMessage: emailError } = useField<string>('email')

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */

const isSubmitting = ref(false)
const isSuccess = ref(false)
const serverError = ref<string | null>(null)

/* ------------------------------------------------------------------ */
/*  Submit                                                             */
/* ------------------------------------------------------------------ */

const onSubmit = handleSubmit(async (values) => {
  isSubmitting.value = true
  serverError.value = null

  try {
    const tenantId = authStore.tenantId || '00000000-0000-0000-0000-000000000000'
    await forgotPassword({
      email: values.email,
      tenantId,
    })
    isSuccess.value = true
  } catch (err) {
    if (isAxiosError(err)) {
      const problem = err.response?.data as ApiProblemDetails | undefined
      if (problem?.detail) {
        serverError.value = problem.detail
      } else if (err.response?.status === 404) {
        /* For security, show success even if email not found */
        isSuccess.value = true
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
          <i class="pi pi-envelope text-3xl text-white"></i>
        </div>
        <h1 class="text-2xl font-bold text-white">
          {{ t('auth.forgotPassword') }}
        </h1>
        <p class="mt-1 text-sm text-white/60">
          {{ t('auth.forgot.subtitle') }}
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
              {{ t('auth.forgot.successTitle') }}
            </h2>
            <p class="mb-6 text-sm text-secondary/60">
              {{ t('auth.forgot.successMessage') }}
            </p>
            <Button
              :label="t('auth.forgot.backToLogin')"
              class="w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
              icon="pi pi-arrow-left"
              iconPos="left"
              @click="router.push({ name: 'Login' })"
            />
          </div>
        </template>

        <!-- Form State -->
        <template v-else>
          <h2 class="mb-2 text-center text-xl font-semibold text-secondary">
            {{ t('auth.forgot.title') }}
          </h2>
          <p class="mb-6 text-center text-sm text-secondary/60">
            {{ t('auth.forgot.instruction') }}
          </p>

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
            <!-- Email Field -->
            <div class="mb-5">
              <label
                for="forgot-email"
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
                  id="forgot-email"
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

            <!-- Submit Button -->
            <Button
              type="submit"
              :label="t('auth.forgot.sendButton')"
              :loading="isSubmitting"
              :disabled="isSubmitting"
              class="w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
              icon="pi pi-send"
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
              {{ t('auth.forgot.backToLogin') }}
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
