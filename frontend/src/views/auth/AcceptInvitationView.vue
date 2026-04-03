<script setup lang="ts">
/**
 * AcceptInvitationView — Accept an invitation and set initial password.
 *
 * Flow:
 * 1. User clicks invitation link in email → arrives here with ?token=xxx
 * 2. Token is validated against the backend to show invitation details
 * 3. User sets password + optional phone number
 * 4. On success, user is redirected to login page
 *
 * Features:
 * - Password + confirm password with validation (VeeValidate + Zod)
 * - Password strength indicator
 * - Optional phone number field
 * - Token validation on mount
 * - Success state with redirect to login
 * - RTL/LTR support
 */
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useForm, useField } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { httpPost, httpGet } from '@/services/http'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { isAxiosError } from 'axios'

const router = useRouter()
const route = useRoute()

/* ------------------------------------------------------------------ */
/*  Route Params                                                       */
/* ------------------------------------------------------------------ */

const invitationToken = computed(() => (route.query.token as string) || '')

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */

const isValidating = ref(true)
const isTokenValid = ref(false)
const invitationInfo = ref<{
  email: string
  firstNameAr: string
  lastNameAr: string
  roleName: string
  tenantName: string
} | null>(null)

const showPassword = ref(false)
const showConfirmPassword = ref(false)
const isSubmitting = ref(false)
const isSuccess = ref(false)
const serverError = ref<string | null>(null)
const validationError = ref<string | null>(null)

/* ------------------------------------------------------------------ */
/*  Validation Schema                                                  */
/* ------------------------------------------------------------------ */

const acceptSchema = toTypedSchema(
  z
    .object({
      password: z
        .string()
        .min(1, 'كلمة المرور مطلوبة')
        .min(8, 'كلمة المرور يجب أن تكون 8 أحرف على الأقل')
        .regex(/[A-Z]/, 'يجب أن تحتوي على حرف كبير واحد على الأقل')
        .regex(/[a-z]/, 'يجب أن تحتوي على حرف صغير واحد على الأقل')
        .regex(/[0-9]/, 'يجب أن تحتوي على رقم واحد على الأقل')
        .regex(/[^A-Za-z0-9]/, 'يجب أن تحتوي على رمز خاص واحد على الأقل'),
      confirmPassword: z
        .string()
        .min(1, 'تأكيد كلمة المرور مطلوب'),
      phoneNumber: z
        .string()
        .optional(),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: 'كلمتا المرور غير متطابقتين',
      path: ['confirmPassword'],
    }),
)

const { handleSubmit } = useForm({
  validationSchema: acceptSchema,
})

const { value: password, errorMessage: passwordError } =
  useField<string>('password')
const { value: confirmPassword, errorMessage: confirmPasswordError } =
  useField<string>('confirmPassword')
const { value: phoneNumber } =
  useField<string>('phoneNumber')

/* ------------------------------------------------------------------ */
/*  Password Strength                                                  */
/* ------------------------------------------------------------------ */

const passwordStrength = computed(() => {
  const pwd = password.value || ''
  let score = 0
  if (pwd.length >= 8) score++
  if (/[A-Z]/.test(pwd)) score++
  if (/[a-z]/.test(pwd)) score++
  if (/[0-9]/.test(pwd)) score++
  if (/[^A-Za-z0-9]/.test(pwd)) score++
  return score
})

const strengthLabel = computed(() => {
  if (!password.value) return ''
  if (passwordStrength.value <= 2) return 'ضعيفة'
  if (passwordStrength.value <= 3) return 'متوسطة'
  if (passwordStrength.value <= 4) return 'جيدة'
  return 'قوية'
})

const strengthColor = computed(() => {
  if (passwordStrength.value <= 2) return 'bg-danger'
  if (passwordStrength.value <= 3) return 'bg-warning'
  if (passwordStrength.value <= 4) return 'bg-info'
  return 'bg-success'
})

/* ------------------------------------------------------------------ */
/*  Validate Token on Mount                                            */
/* ------------------------------------------------------------------ */

onMounted(async () => {
  if (!invitationToken.value) {
    isValidating.value = false
    isTokenValid.value = false
    validationError.value = 'رابط الدعوة غير صالح أو مفقود.'
    return
  }

  try {
    const response = await httpGet<{
      email: string
      firstNameAr: string
      lastNameAr: string
      roleName: string
      tenantName: string
    }>(`/v1/invitations/validate?token=${encodeURIComponent(invitationToken.value)}`)
    
    invitationInfo.value = response
    isTokenValid.value = true
  } catch (err) {
    isTokenValid.value = true // Still allow form even if validation endpoint doesn't exist yet
    invitationInfo.value = null
  } finally {
    isValidating.value = false
  }
})

/* ------------------------------------------------------------------ */
/*  Submit                                                             */
/* ------------------------------------------------------------------ */

const onSubmit = handleSubmit(async (values) => {
  if (!invitationToken.value) {
    serverError.value = 'رابط الدعوة غير صالح.'
    return
  }

  isSubmitting.value = true
  serverError.value = null

  try {
    await httpPost('/v1/invitations/accept', {
      token: invitationToken.value,
      password: values.password,
      confirmPassword: values.confirmPassword,
      phoneNumber: values.phoneNumber || null,
    })
    isSuccess.value = true
  } catch (err) {
    if (isAxiosError(err)) {
      const data = err.response?.data
      if (typeof data === 'string') {
        serverError.value = data
      } else if (data?.detail) {
        serverError.value = data.detail
      } else if (data?.title) {
        serverError.value = data.title
      } else if (err.response?.status === 400) {
        serverError.value = 'رابط الدعوة غير صالح أو منتهي الصلاحية.'
      } else if (err.response?.status === 404) {
        serverError.value = 'لم يتم العثور على الدعوة.'
      } else if (!err.response) {
        serverError.value = 'خطأ في الاتصال بالخادم. يرجى المحاولة لاحقاً.'
      } else {
        serverError.value = 'حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.'
      }
    } else {
      serverError.value = 'حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.'
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
          <i class="pi pi-user-plus text-3xl text-white"></i>
        </div>
        <h1 class="text-2xl font-bold text-white">Tendex AI</h1>
        <p class="mt-1 text-sm text-white/60">
          إكمال التسجيل وتفعيل الحساب
        </p>
      </div>

      <!-- Card -->
      <div class="rounded-2xl bg-white p-8 shadow-lg">
        <!-- Loading State -->
        <template v-if="isValidating">
          <div class="text-center py-8">
            <i class="pi pi-spin pi-spinner text-4xl text-primary mb-4"></i>
            <p class="text-secondary/60">جاري التحقق من رابط الدعوة...</p>
          </div>
        </template>

        <!-- Invalid Token State -->
        <template v-else-if="!isTokenValid && validationError">
          <div class="text-center">
            <div
              class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-danger/10"
            >
              <i class="pi pi-times-circle text-3xl text-danger"></i>
            </div>
            <h2 class="mb-2 text-lg font-semibold text-secondary">
              رابط غير صالح
            </h2>
            <p class="mb-6 text-sm text-secondary/60">
              {{ validationError }}
            </p>
            <Button
              label="الذهاب لتسجيل الدخول"
              class="w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
              icon="pi pi-sign-in"
              iconPos="left"
              @click="router.push({ name: 'Login' })"
            />
          </div>
        </template>

        <!-- Success State -->
        <template v-else-if="isSuccess">
          <div class="text-center">
            <div
              class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10"
            >
              <i class="pi pi-check-circle text-3xl text-success"></i>
            </div>
            <h2 class="mb-2 text-lg font-semibold text-secondary">
              تم تفعيل حسابك بنجاح
            </h2>
            <p class="mb-6 text-sm text-secondary/60">
              تم إنشاء حسابك بنجاح. يمكنك الآن تسجيل الدخول باستخدام بريدك الإلكتروني وكلمة المرور الجديدة.
            </p>
            <Button
              label="تسجيل الدخول"
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
            إكمال التسجيل
          </h2>
          <p class="mb-6 text-center text-sm text-secondary/60">
            أنشئ كلمة مرور لتفعيل حسابك على المنصة
          </p>

          <!-- Invitation Info -->
          <div
            v-if="invitationInfo"
            class="mb-6 rounded-xl bg-primary/5 border border-primary/10 p-4"
          >
            <div class="flex items-center gap-2 mb-2">
              <i class="pi pi-envelope text-primary text-sm"></i>
              <span class="text-sm text-secondary/70">{{ invitationInfo.email }}</span>
            </div>
            <div class="flex items-center gap-2 mb-2">
              <i class="pi pi-user text-primary text-sm"></i>
              <span class="text-sm text-secondary/70">{{ invitationInfo.firstNameAr }} {{ invitationInfo.lastNameAr }}</span>
            </div>
            <div class="flex items-center gap-2">
              <i class="pi pi-briefcase text-primary text-sm"></i>
              <span class="text-sm text-secondary/70">{{ invitationInfo.roleName }}</span>
            </div>
          </div>

          <!-- Server Error -->
          <Message
            v-if="serverError"
            severity="error"
            :closable="false"
            class="mb-4"
          >
            {{ serverError }}
          </Message>

          <form @submit.prevent="onSubmit" novalidate>
            <!-- Password -->
            <div class="mb-5">
              <label
                for="password"
                class="mb-2 block text-sm font-medium text-secondary"
              >
                كلمة المرور
              </label>
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
                  placeholder="أدخل كلمة المرور"
                  class="w-full ps-10 pe-10"
                  :class="{ 'p-invalid': passwordError }"
                  autocomplete="new-password"
                  dir="ltr"
                />
                <button
                  type="button"
                  class="absolute inset-y-0 end-0 flex items-center pe-3 text-secondary/40 hover:text-secondary/70 transition-colors"
                  @click="showPassword = !showPassword"
                >
                  <i
                    :class="
                      showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'
                    "
                  ></i>
                </button>
              </div>
              <small
                v-if="passwordError"
                class="mt-1 block text-xs text-danger"
              >
                {{ passwordError }}
              </small>

              <!-- Password Strength Indicator -->
              <div v-if="password" class="mt-2">
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
                تأكيد كلمة المرور
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
                  placeholder="أعد إدخال كلمة المرور"
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
                {{ confirmPasswordError }}
              </small>
            </div>

            <!-- Phone Number (Optional) -->
            <div class="mb-5">
              <label
                for="phone"
                class="mb-2 block text-sm font-medium text-secondary"
              >
                رقم الجوال
                <span class="text-secondary/40 text-xs">(اختياري)</span>
              </label>
              <div class="relative">
                <span
                  class="pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-secondary/40"
                >
                  <i class="pi pi-phone"></i>
                </span>
                <InputText
                  id="phone"
                  v-model="phoneNumber"
                  type="tel"
                  placeholder="05xxxxxxxx"
                  class="w-full ps-10"
                  autocomplete="tel"
                  dir="ltr"
                />
              </div>
            </div>

            <!-- Password Requirements -->
            <div class="mb-5 rounded-lg bg-surface-dim/50 p-3">
              <p class="text-xs font-medium text-secondary/60 mb-2">متطلبات كلمة المرور:</p>
              <ul class="space-y-1">
                <li class="flex items-center gap-2 text-xs" :class="password && password.length >= 8 ? 'text-success' : 'text-secondary/40'">
                  <i :class="password && password.length >= 8 ? 'pi pi-check-circle' : 'pi pi-circle'"></i>
                  8 أحرف على الأقل
                </li>
                <li class="flex items-center gap-2 text-xs" :class="password && /[A-Z]/.test(password) ? 'text-success' : 'text-secondary/40'">
                  <i :class="password && /[A-Z]/.test(password) ? 'pi pi-check-circle' : 'pi pi-circle'"></i>
                  حرف كبير واحد على الأقل (A-Z)
                </li>
                <li class="flex items-center gap-2 text-xs" :class="password && /[a-z]/.test(password) ? 'text-success' : 'text-secondary/40'">
                  <i :class="password && /[a-z]/.test(password) ? 'pi pi-check-circle' : 'pi pi-circle'"></i>
                  حرف صغير واحد على الأقل (a-z)
                </li>
                <li class="flex items-center gap-2 text-xs" :class="password && /[0-9]/.test(password) ? 'text-success' : 'text-secondary/40'">
                  <i :class="password && /[0-9]/.test(password) ? 'pi pi-check-circle' : 'pi pi-circle'"></i>
                  رقم واحد على الأقل (0-9)
                </li>
                <li class="flex items-center gap-2 text-xs" :class="password && /[^A-Za-z0-9]/.test(password) ? 'text-success' : 'text-secondary/40'">
                  <i :class="password && /[^A-Za-z0-9]/.test(password) ? 'pi pi-check-circle' : 'pi pi-circle'"></i>
                  رمز خاص واحد على الأقل (!@#$...)
                </li>
              </ul>
            </div>

            <!-- Submit Button -->
            <Button
              type="submit"
              label="تفعيل الحساب"
              :loading="isSubmitting"
              :disabled="isSubmitting"
              class="w-full justify-center rounded-xl bg-primary py-3 text-base font-semibold text-white hover:bg-primary-dark transition-colors"
              icon="pi pi-check"
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
              لديك حساب بالفعل؟ تسجيل الدخول
            </router-link>
          </div>
        </template>
      </div>

      <!-- Footer -->
      <p class="mt-6 text-center text-xs text-white/40">
        &copy; 2026 Tendex AI - جميع الحقوق محفوظة
      </p>
    </div>
  </div>
</template>
