<script setup lang="ts">
/**
 * UserProfileView — User profile management page.
 *
 * Features:
 * - View and edit personal information (name, email, phone)
 * - Upload/remove profile avatar
 * - Change password
 * - View account details (roles, tenant, status)
 */
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import * as profileService from '@/services/profileService'
import type { ProfileDto, UpdateProfileRequest, ChangePasswordRequest } from '@/services/profileService'

const { t } = useI18n()
const authStore = useAuthStore()

/* ---- State ---- */
const isLoading = ref(true)
const isEditing = ref(false)
const isSaving = ref(false)
const isChangingPassword = ref(false)
const isUploadingAvatar = ref(false)
const showPasswordSection = ref(false)

const successMessage = ref('')
const errorMessage = ref('')

const profile = ref<ProfileDto | null>(null)

const editForm = reactive<UpdateProfileRequest>({
  firstName: '',
  lastName: '',
  phoneNumber: null,
  email: '',
})

const passwordForm = reactive<ChangePasswordRequest>({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

const passwordErrors = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})

/* ---- Computed ---- */
const userInitials = computed(() => {
  if (!profile.value) return '?'
  const first = profile.value.firstName?.charAt(0) || ''
  const last = profile.value.lastName?.charAt(0) || ''
  return `${first}${last}`.toUpperCase() || '?'
})

const displayName = computed(() => {
  if (!profile.value) return ''
  return `${profile.value.firstName} ${profile.value.lastName}`.trim()
})

/* ---- Methods ---- */
async function loadProfile(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''
  try {
    profile.value = await profileService.getProfile()
  } catch {
    errorMessage.value = t('profile.profileError')
  } finally {
    isLoading.value = false
  }
}

function startEditing(): void {
  if (!profile.value) return
  editForm.firstName = profile.value.firstName
  editForm.lastName = profile.value.lastName
  editForm.phoneNumber = profile.value.phoneNumber
  editForm.email = profile.value.email
  isEditing.value = true
  clearMessages()
}

function cancelEditing(): void {
  isEditing.value = false
  clearMessages()
}

async function saveProfile(): Promise<void> {
  if (!profile.value) return
  isSaving.value = true
  clearMessages()
  try {
    await profileService.updateProfile(editForm)
    // Reload profile to get updated data
    await loadProfile()
    // Update auth store user info
    if (authStore.user && profile.value) {
      authStore.user.firstName = profile.value.firstName
      authStore.user.lastName = profile.value.lastName
      authStore.user.email = profile.value.email
      localStorage.setItem('user', JSON.stringify(authStore.user))
    }
    isEditing.value = false
    successMessage.value = t('profile.profileUpdated')
    autoHideSuccess()
  } catch {
    errorMessage.value = t('profile.profileError')
  } finally {
    isSaving.value = false
  }
}

function togglePasswordSection(): void {
  showPasswordSection.value = !showPasswordSection.value
  resetPasswordForm()
}

function resetPasswordForm(): void {
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmNewPassword = ''
  passwordErrors.currentPassword = ''
  passwordErrors.newPassword = ''
  passwordErrors.confirmNewPassword = ''
}

function validatePasswordForm(): boolean {
  let valid = true
  passwordErrors.currentPassword = ''
  passwordErrors.newPassword = ''
  passwordErrors.confirmNewPassword = ''

  if (!passwordForm.currentPassword) {
    passwordErrors.currentPassword = t('auth.validation.passwordRequired')
    valid = false
  }
  if (!passwordForm.newPassword) {
    passwordErrors.newPassword = t('auth.validation.passwordRequired')
    valid = false
  } else if (passwordForm.newPassword.length < 8) {
    passwordErrors.newPassword = t('auth.validation.passwordMinLength')
    valid = false
  }
  if (!passwordForm.confirmNewPassword) {
    passwordErrors.confirmNewPassword = t('auth.validation.confirmPasswordRequired')
    valid = false
  } else if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
    passwordErrors.confirmNewPassword = t('auth.validation.passwordsMismatch')
    valid = false
  }
  return valid
}

async function changePassword(): Promise<void> {
  if (!validatePasswordForm()) return
  isChangingPassword.value = true
  clearMessages()
  try {
    await profileService.changePassword(passwordForm)
    successMessage.value = t('profile.passwordChanged')
    showPasswordSection.value = false
    resetPasswordForm()
    autoHideSuccess()
  } catch (err: any) {
    if (err?.response?.status === 400) {
      errorMessage.value = t('profile.currentPasswordWrong')
    } else {
      errorMessage.value = t('profile.passwordError')
    }
  } finally {
    isChangingPassword.value = false
  }
}

async function handleAvatarUpload(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  // Validate file type and size
  const allowedTypes = ['image/jpeg', 'image/png', 'image/webp']
  if (!allowedTypes.includes(file.type)) {
    errorMessage.value = t('profile.avatarError')
    return
  }
  if (file.size > 5 * 1024 * 1024) {
    errorMessage.value = t('profile.avatarError')
    return
  }

  isUploadingAvatar.value = true
  clearMessages()
  try {
    const result = await profileService.uploadAvatar(file)
    if (profile.value) {
      profile.value.avatarUrl = result.avatarUrl
    }
    successMessage.value = t('profile.avatarUpdated')
    autoHideSuccess()
  } catch {
    errorMessage.value = t('profile.avatarError')
  } finally {
    isUploadingAvatar.value = false
    // Reset file input
    input.value = ''
  }
}

async function removeAvatar(): Promise<void> {
  isUploadingAvatar.value = true
  clearMessages()
  try {
    await profileService.deleteAvatar()
    if (profile.value) {
      profile.value.avatarUrl = null
    }
    successMessage.value = t('profile.avatarRemoved')
    autoHideSuccess()
  } catch {
    errorMessage.value = t('profile.avatarError')
  } finally {
    isUploadingAvatar.value = false
  }
}

function clearMessages(): void {
  successMessage.value = ''
  errorMessage.value = ''
}

function autoHideSuccess(): void {
  setTimeout(() => {
    successMessage.value = ''
  }, 4000)
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  try {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(dateStr))
  } catch {
    return dateStr
  }
}

onMounted(() => {
  loadProfile()
})
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-6 sm:px-6 lg:px-8">
    <!-- Page Header -->
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-secondary">{{ t('profile.title') }}</h1>
      <p class="mt-1 text-sm text-tertiary">{{ t('profile.subtitle') }}</p>
    </div>

    <!-- Success/Error Banners -->
    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="-translate-y-2 opacity-0"
    >
      <div v-if="successMessage" class="mb-6 flex items-center gap-3 rounded-xl border border-success/20 bg-success/5 px-4 py-3">
        <i class="pi pi-check-circle text-lg text-success"></i>
        <span class="text-sm font-medium text-success">{{ successMessage }}</span>
      </div>
    </Transition>

    <Transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="-translate-y-2 opacity-0"
    >
      <div v-if="errorMessage" class="mb-6 flex items-center gap-3 rounded-xl border border-danger/20 bg-danger/5 px-4 py-3">
        <i class="pi pi-exclamation-circle text-lg text-danger"></i>
        <span class="text-sm font-medium text-danger">{{ errorMessage }}</span>
      </div>
    </Transition>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <i class="pi pi-spinner pi-spin text-3xl text-primary"></i>
    </div>

    <template v-else-if="profile">
      <!-- Avatar & Name Card -->
      <div class="mb-6 overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm">
        <div class="bg-gradient-to-r from-primary/5 to-primary/10 px-6 py-8">
          <div class="flex flex-col items-center gap-4 sm:flex-row">
            <!-- Avatar -->
            <div class="relative">
              <div
                v-if="profile.avatarUrl"
                class="h-24 w-24 overflow-hidden rounded-full ring-4 ring-white shadow-md"
              >
                <img
                  :src="profile.avatarUrl"
                  :alt="displayName"
                  class="h-full w-full object-cover"
                />
              </div>
              <div
                v-else
                class="flex h-24 w-24 items-center justify-center rounded-full bg-gradient-to-br from-primary to-primary-dark text-3xl font-bold text-white ring-4 ring-white shadow-md"
              >
                {{ userInitials }}
              </div>

              <!-- Upload overlay -->
              <label
                class="absolute inset-0 flex cursor-pointer items-center justify-center rounded-full bg-black/0 transition-colors hover:bg-black/30 group"
                :class="{ 'pointer-events-none': isUploadingAvatar }"
              >
                <i class="pi pi-camera text-xl text-white opacity-0 transition-opacity group-hover:opacity-100"></i>
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  class="hidden"
                  @change="handleAvatarUpload"
                />
              </label>

              <!-- Loading spinner on avatar -->
              <div
                v-if="isUploadingAvatar"
                class="absolute inset-0 flex items-center justify-center rounded-full bg-black/40"
              >
                <i class="pi pi-spinner pi-spin text-xl text-white"></i>
              </div>
            </div>

            <!-- Name & Role -->
            <div class="text-center sm:text-start">
              <h2 class="text-xl font-bold text-secondary">{{ displayName }}</h2>
              <p class="text-sm text-tertiary">{{ profile.email }}</p>
              <div class="mt-2 flex flex-wrap justify-center gap-2 sm:justify-start">
                <span
                  v-for="role in profile.roles"
                  :key="role"
                  class="inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary"
                >
                  {{ role }}
                </span>
              </div>
            </div>

            <!-- Avatar actions -->
            <div class="ms-auto hidden flex-col gap-2 sm:flex">
              <label
                class="cursor-pointer rounded-lg border border-secondary-200 bg-white px-3 py-1.5 text-xs font-medium text-secondary transition-colors hover:bg-surface-subtle"
                :class="{ 'pointer-events-none opacity-50': isUploadingAvatar }"
              >
                <i class="pi pi-upload me-1 text-xs"></i>
                {{ t('profile.changeAvatar') }}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  class="hidden"
                  @change="handleAvatarUpload"
                />
              </label>
              <button
                v-if="profile.avatarUrl"
                type="button"
                class="rounded-lg border border-danger/20 px-3 py-1.5 text-xs font-medium text-danger transition-colors hover:bg-danger/5"
                :disabled="isUploadingAvatar"
                @click="removeAvatar"
              >
                <i class="pi pi-trash me-1 text-xs"></i>
                {{ t('profile.removeAvatar') }}
              </button>
            </div>
          </div>
          <p class="mt-3 text-center text-xs text-tertiary sm:text-start">
            {{ t('profile.avatarHint') }}
          </p>
        </div>
      </div>

      <!-- Personal Information Card -->
      <div class="mb-6 overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm">
        <div class="flex items-center justify-between border-b border-secondary-100 px-6 py-4">
          <div>
            <h3 class="text-base font-semibold text-secondary">{{ t('profile.personalInfo') }}</h3>
            <p class="text-xs text-tertiary">{{ t('profile.personalInfoDesc') }}</p>
          </div>
          <button
            v-if="!isEditing"
            type="button"
            class="flex items-center gap-2 rounded-xl bg-primary px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
            @click="startEditing"
          >
            <i class="pi pi-pencil text-xs"></i>
            {{ t('profile.editProfile') }}
          </button>
        </div>

        <div class="px-6 py-5">
          <!-- View Mode -->
          <div v-if="!isEditing" class="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.firstName') }}</label>
              <p class="text-sm font-medium text-secondary">{{ profile.firstName || '-' }}</p>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.lastName') }}</label>
              <p class="text-sm font-medium text-secondary">{{ profile.lastName || '-' }}</p>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.email') }}</label>
              <p class="text-sm font-medium text-secondary">{{ profile.email }}</p>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.phone') }}</label>
              <p class="text-sm font-medium text-secondary">{{ profile.phoneNumber || '-' }}</p>
            </div>
          </div>

          <!-- Edit Mode -->
          <div v-else class="space-y-5">
            <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.firstName') }}</label>
                <input
                  v-model="editForm.firstName"
                  type="text"
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.lastName') }}</label>
                <input
                  v-model="editForm.lastName"
                  type="text"
                  class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.email') }}</label>
              <input
                v-model="editForm.email"
                type="email"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
              <p class="mt-1 text-xs text-warning">
                <i class="pi pi-info-circle me-1 text-xs"></i>
                {{ t('profile.emailNote') }}
              </p>
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.phone') }}</label>
              <input
                v-model="editForm.phoneNumber"
                type="tel"
                dir="ltr"
                class="w-full rounded-xl border border-secondary-200 px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <!-- Save/Cancel buttons -->
            <div class="flex justify-end gap-3 border-t border-secondary-100 pt-4">
              <button
                type="button"
                class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
                :disabled="isSaving"
                @click="cancelEditing"
              >
                {{ t('profile.cancelEdit') }}
              </button>
              <button
                type="button"
                class="flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
                :disabled="isSaving"
                @click="saveProfile"
              >
                <i v-if="isSaving" class="pi pi-spinner pi-spin text-sm"></i>
                {{ t('profile.saveChanges') }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Security Card (Change Password) -->
      <div class="mb-6 overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm">
        <div class="flex items-center justify-between border-b border-secondary-100 px-6 py-4">
          <div>
            <h3 class="text-base font-semibold text-secondary">
              <i class="pi pi-shield me-2 text-primary"></i>
              {{ t('profile.securitySection') }}
            </h3>
            <p class="text-xs text-tertiary">{{ t('profile.securityDesc') }}</p>
          </div>
          <button
            type="button"
            class="flex items-center gap-2 rounded-xl border border-secondary-200 px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
            @click="togglePasswordSection"
          >
            <i class="pi pi-key text-xs"></i>
            {{ t('profile.changePassword') }}
          </button>
        </div>

        <!-- Password Change Form -->
        <Transition
          enter-active-class="transition duration-300 ease-out"
          enter-from-class="max-h-0 opacity-0"
          enter-to-class="max-h-96 opacity-100"
          leave-active-class="transition duration-200 ease-in"
          leave-from-class="max-h-96 opacity-100"
          leave-to-class="max-h-0 opacity-0"
        >
          <div v-if="showPasswordSection" class="overflow-hidden px-6 py-5">
            <div class="mx-auto max-w-md space-y-4">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.currentPassword') }}</label>
                <input
                  v-model="passwordForm.currentPassword"
                  type="password"
                  class="w-full rounded-xl border px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:ring-2 focus:ring-primary/20"
                  :class="passwordErrors.currentPassword ? 'border-danger focus:border-danger' : 'border-secondary-200 focus:border-primary'"
                />
                <p v-if="passwordErrors.currentPassword" class="mt-1 text-xs text-danger">{{ passwordErrors.currentPassword }}</p>
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.newPassword') }}</label>
                <input
                  v-model="passwordForm.newPassword"
                  type="password"
                  class="w-full rounded-xl border px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:ring-2 focus:ring-primary/20"
                  :class="passwordErrors.newPassword ? 'border-danger focus:border-danger' : 'border-secondary-200 focus:border-primary'"
                />
                <p v-if="passwordErrors.newPassword" class="mt-1 text-xs text-danger">{{ passwordErrors.newPassword }}</p>
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">{{ t('profile.confirmNewPassword') }}</label>
                <input
                  v-model="passwordForm.confirmNewPassword"
                  type="password"
                  class="w-full rounded-xl border px-4 py-2.5 text-sm text-secondary outline-none transition-all focus:ring-2 focus:ring-primary/20"
                  :class="passwordErrors.confirmNewPassword ? 'border-danger focus:border-danger' : 'border-secondary-200 focus:border-primary'"
                />
                <p v-if="passwordErrors.confirmNewPassword" class="mt-1 text-xs text-danger">{{ passwordErrors.confirmNewPassword }}</p>
              </div>
              <div class="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  class="rounded-xl border border-secondary-200 px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-subtle"
                  :disabled="isChangingPassword"
                  @click="togglePasswordSection"
                >
                  {{ t('common.cancel') }}
                </button>
                <button
                  type="button"
                  class="flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
                  :disabled="isChangingPassword"
                  @click="changePassword"
                >
                  <i v-if="isChangingPassword" class="pi pi-spinner pi-spin text-sm"></i>
                  {{ t('profile.changePassword') }}
                </button>
              </div>
            </div>
          </div>
        </Transition>
      </div>

      <!-- Account Info Card -->
      <div class="overflow-hidden rounded-2xl border border-secondary-200/60 bg-white shadow-sm">
        <div class="border-b border-secondary-100 px-6 py-4">
          <h3 class="text-base font-semibold text-secondary">
            <i class="pi pi-info-circle me-2 text-primary"></i>
            {{ t('profile.accountInfo') }}
          </h3>
          <p class="text-xs text-tertiary">{{ t('profile.accountInfoDesc') }}</p>
        </div>
        <div class="px-6 py-5">
          <div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.roles') }}</label>
              <div class="flex flex-wrap gap-1.5">
                <span
                  v-for="role in profile.roles"
                  :key="role"
                  class="inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary"
                >
                  {{ role }}
                </span>
              </div>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.tenant') }}</label>
              <p class="text-sm font-medium text-secondary">{{ profile.tenantName || '-' }}</p>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.status') }}</label>
              <span
                class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium"
                :class="profile.isActive ? 'bg-success/10 text-success' : 'bg-danger/10 text-danger'"
              >
                <span class="h-1.5 w-1.5 rounded-full" :class="profile.isActive ? 'bg-success' : 'bg-danger'"></span>
                {{ profile.isActive ? t('profile.active') : t('profile.inactive') }}
              </span>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.lastLogin') }}</label>
              <p class="text-sm font-medium text-secondary" dir="ltr">{{ formatDate(profile.lastLoginAt) }}</p>
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-tertiary">{{ t('profile.memberSince') }}</label>
              <p class="text-sm font-medium text-secondary" dir="ltr">{{ formatDate(profile.createdAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
