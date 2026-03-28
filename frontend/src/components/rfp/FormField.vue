<script setup lang="ts">
/**
 * FormField Component.
 *
 * Wraps a form input with label, error message, and help text.
 * Provides consistent styling and accessibility attributes.
 */
defineProps<{
  label: string
  fieldId: string
  error?: string
  helpText?: string
  required?: boolean
}>()
</script>

<template>
  <div class="mb-4">
    <label
      :for="fieldId"
      class="mb-1.5 block text-sm font-medium text-secondary"
    >
      {{ label }}
      <span v-if="required" class="text-danger">*</span>
    </label>

    <slot />

    <Transition name="fade">
      <p
        v-if="error"
        :id="`${fieldId}-error`"
        class="mt-1.5 flex items-center gap-1 text-xs text-danger"
        role="alert"
      >
        <i class="pi pi-exclamation-circle text-xs" aria-hidden="true"></i>
        {{ error }}
      </p>
    </Transition>

    <p
      v-if="helpText && !error"
      :id="`${fieldId}-help`"
      class="mt-1 text-xs text-tertiary"
    >
      {{ helpText }}
    </p>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
