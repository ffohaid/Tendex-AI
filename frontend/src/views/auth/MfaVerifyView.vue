<script setup lang="ts">
/**
 * MfaVerifyView — OTP / MFA verification screen.
 *
 * Features:
 * - 6-digit OTP input with auto-focus and auto-advance
 * - Countdown timer for resend
 * - Clear error messages
 * - Redirect to dashboard on success
 * - RTL/LTR support via logical Tailwind properties
 */
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import Button from 'primevue/button'
import Message from 'primevue/message'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

const OTP_LENGTH = 6
const RESEND_COOLDOWN = 60 /* seconds */

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */

const otpDigits = ref<string[]>(Array(OTP_LENGTH).fill(''))
const inputRefs = ref<(HTMLInputElement | null)[]>([])
const resendTimer = ref(RESEND_COOLDOWN)
const canResend = ref(false)
let timerInterval: ReturnType<typeof setInterval> | null = null

const isSubmitting = computed(() => authStore.isLoading)
const serverError = computed(() => authStore.error)
const otpCode = computed(() => otpDigits.value.join(''))
const isOtpComplete = computed(() => otpCode.value.length === OTP_LENGTH)

/* ------------------------------------------------------------------ */
/*  Guard: redirect if no MFA session                                  */
/* ------------------------------------------------------------------ */

onMounted(() => {
  if (!authStore.mfaSessionId) {
    router.replace({ name: 'Login' })
    return
  }
  startResendTimer()
  nextTick(() => {
    inputRefs.value[0]?.focus()
  })
})

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval)
})

/* ------------------------------------------------------------------ */
/*  Timer                                                              */
/* ------------------------------------------------------------------ */

function startResendTimer(): void {
  resendTimer.value = RESEND_COOLDOWN
  canResend.value = false

  if (timerInterval) clearInterval(timerInterval)

  timerInterval = setInterval(() => {
    resendTimer.value--
    if (resendTimer.value <= 0) {
      canResend.value = true
      if (timerInterval) clearInterval(timerInterval)
    }
  }, 1000)
}

/* ------------------------------------------------------------------ */
/*  OTP Input Handlers                                                 */
/* ------------------------------------------------------------------ */

function setInputRef(el: unknown, index: number): void {
  inputRefs.value[index] = el as HTMLInputElement | null
}

function handleInput(index: number, event: Event): void {
  const target = event.target as HTMLInputElement
  const value = target.value

  /* Allow only digits */
  if (!/^\d*$/.test(value)) {
    target.value = otpDigits.value[index]
    return
  }

  /* Handle paste of full code */
  if (value.length > 1) {
    const digits = value.slice(0, OTP_LENGTH).split('')
    digits.forEach((digit, i) => {
      if (i < OTP_LENGTH) {
        otpDigits.value[i] = digit
      }
    })
    const focusIndex = Math.min(digits.length, OTP_LENGTH - 1)
    inputRefs.value[focusIndex]?.focus()
    if (digits.length >= OTP_LENGTH) {
      submitOtp()
    }
    return
  }

  otpDigits.value[index] = value

  /* Auto-advance to next input */
  if (value && index < OTP_LENGTH - 1) {
    inputRefs.value[index + 1]?.focus()
  }

  /* Auto-submit when all digits entered */
  if (isOtpComplete.value) {
    submitOtp()
  }
}

function handleKeydown(index: number, event: KeyboardEvent): void {
  if (event.key === 'Backspace') {
    if (!otpDigits.value[index] && index > 0) {
      otpDigits.value[index - 1] = ''
      inputRefs.value[index - 1]?.focus()
      event.preventDefault()
    } else {
      otpDigits.value[index] = ''
    }
  } else if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
    /* Navigate between inputs using arrow keys */
    const isRtl = document.documentElement.dir === 'rtl'
    const goBack =
      (event.key === 'ArrowLeft' && !isRtl) ||
      (event.key === 'ArrowRight' && isRtl)

    if (goBack && index > 0) {
      inputRefs.value[index - 1]?.focus()
    } else if (!goBack && index < OTP_LENGTH - 1) {
      inputRefs.value[index + 1]?.focus()
    }
  }
}

function handlePaste(event: ClipboardEvent): void {
  event.preventDefault()
  const pasted = event.clipboardData?.getData('text')?.replace(/\D/g, '') || ''
  const digits = pasted.slice(0, OTP_LENGTH).split('')

  digits.forEach((digit, i) => {
    otpDigits.value[i] = digit
  })

  const focusIndex = Math.min(digits.length, OTP_LENGTH - 1)
  inputRefs.value[focusIndex]?.focus()

  if (digits.length >= OTP_LENGTH) {
    submitOtp()
  }
}

/* ------------------------------------------------------------------ */
/*  Submit & Resend                                                    */
/* ------------------------------------------------------------------ */

async function submitOtp(): Promise<void> {
  if (!isOtpComplete.value || isSubmitting.value) return

  const success = await authStore.verifyMfaAction(otpCode.value)

  if (success) {
    router.push({ name: 'Dashboard' })
  } else {
    /* Clear OTP on failure and refocus first input */
    otpDigits.value = Array(OTP_LENGTH).fill('')
    nextTick(() => {
      inputRefs.value[0]?.focus()
    })
  }
}

function handleResend(): void {
  if (!canResend.value) return
  /* TODO: Call resend OTP API when available */
  startResendTimer()
  otpDigits.value = Array(OTP_LENGTH).fill('')
  nextTick(() => {
    inputRefs.value[0]?.focus()
  })
}

function goBackToLogin(): void {
  authStore.clearAuthState()
  router.push({ name: 'Login' })
}
</script>

<template>
  <div class="flex min-h-screen w-full items-center justify-center bg-secondary p-4">
    <div class="w-full max-w-md">
      <!-- Logo & Title -->
      <div class="mb-8 text-center">
        <div
          class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary"
        >
          <i class="pi pi-key text-3xl text-white"></i>
        </div>
        <h1 class="text-2xl font-bold text-white">
          {{ t('auth.mfa.title') }}
        </h1>
        <p class="mt-1 text-sm text-white/60">
          {{ t('auth.mfa.subtitle') }}
        </p>
      </div>

      <!-- MFA Card -->
      <div class="rounded-2xl bg-white p-8 shadow-lg">
        <div class="mb-6 text-center">
          <div
            class="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10"
          >
            <i class="pi pi-mobile text-xl text-primary"></i>
          </div>
          <p class="text-sm text-secondary/70">
            {{ t('auth.mfa.instruction') }}
          </p>
        </div>

        <!-- Server Error -->
        <Message
          v-if="serverError"
          severity="error"
          :closable="false"
          class="mb-4"
        >
          {{ t(serverError) }}
        </Message>

        <!-- OTP Inputs -->
        <div class="mb-6 flex justify-center gap-2" dir="ltr">
          <input
            v-for="(_, index) in OTP_LENGTH"
            :key="index"
            :ref="(el) => setInputRef(el, index)"
            type="text"
            inputmode="numeric"
            maxlength="6"
            :value="otpDigits[index]"
            @input="handleInput(index, $event)"
            @keydown="handleKeydown(index, $event)"
            @paste="handlePaste"
            class="h-14 w-12 rounded-xl border-2 border-surface-dim bg-surface-muted text-center text-xl font-bold text-secondary transition-all focus:border-primary focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary/20"
            :class="{ 'border-danger': serverError }"
            :aria-label="`${t('auth.mfa.digitLabel')} ${index + 1}`"
          />
        </div>

        <!-- Submit Button -->
        <Button
          :label="t('auth.mfa.verifyButton')"
          :loading="isSubmitting"
          :disabled="!isOtpComplete || isSubmitting"
          class="mb-4 w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
          icon="pi pi-check-circle"
          iconPos="left"
          @click="submitOtp"
        />

        <!-- Resend Timer -->
        <div class="text-center">
          <p v-if="!canResend" class="text-sm text-secondary/50">
            {{ t('auth.mfa.resendIn') }}
            <span class="font-semibold text-secondary" dir="ltr">
              {{ resendTimer }}
            </span>
            {{ t('auth.mfa.seconds') }}
          </p>
          <button
            v-else
            type="button"
            class="text-sm font-medium text-primary hover:text-primary-dark transition-colors"
            @click="handleResend"
          >
            {{ t('auth.mfa.resendCode') }}
          </button>
        </div>

        <!-- Back to Login -->
        <div class="mt-6 border-t border-surface-dim pt-4 text-center">
          <button
            type="button"
            class="inline-flex items-center gap-1 text-sm text-secondary/60 hover:text-secondary transition-colors"
            @click="goBackToLogin"
          >
            <i class="pi pi-arrow-left text-xs rtl:rotate-180"></i>
            {{ t('auth.mfa.backToLogin') }}
          </button>
        </div>
      </div>

      <!-- Footer -->
      <p class="mt-6 text-center text-xs text-white/40">
        {{ t('footer.copyright') }}
      </p>
    </div>
  </div>
</template>
