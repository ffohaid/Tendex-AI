<script setup lang="ts">
/**
 * WizardStepper Component.
 *
 * Displays a horizontal stepper for the 6-step RFP creation wizard.
 * Supports RTL/LTR, accessibility, and visual progress indication.
 */
import { useI18n } from 'vue-i18n'
import type { WizardStep } from '@/types/rfp'

defineProps<{
  steps: WizardStep[]
  currentStep: number
}>()

const emit = defineEmits<{
  (e: 'step-click', step: number): void
}>()

const { t } = useI18n()

function handleStepClick(step: WizardStep) {
  if (step.isAccessible) {
    emit('step-click', step.id)
  }
}
</script>

<template>
  <nav aria-label="خطوات إنشاء الكراسة" class="mb-8">
    <ol class="flex items-center gap-0">
      <li
        v-for="(step, index) in steps"
        :key="step.id"
        class="flex flex-1 items-center"
        :class="{ 'flex-none': index === steps.length - 1 }"
      >
        <!-- Step circle + label -->
        <button
          type="button"
          class="group flex flex-col items-center gap-2"
          :class="{
            'cursor-pointer': step.isAccessible,
            'cursor-not-allowed opacity-50': !step.isAccessible,
          }"
          :disabled="!step.isAccessible"
          :aria-current="step.isActive ? 'step' : undefined"
          :aria-label="t(step.titleKey)"
          @click="handleStepClick(step)"
        >
          <!-- Circle -->
          <div
            class="flex h-10 w-10 items-center justify-center rounded-full border-2 text-sm font-bold transition-all duration-300"
            :class="{
              'border-primary bg-primary text-white shadow-md': step.isActive,
              'border-primary bg-primary/10 text-primary': step.isCompleted && !step.isActive,
              'border-surface-dim bg-white text-tertiary': !step.isActive && !step.isCompleted,
              'group-hover:border-primary/60 group-hover:text-primary': step.isAccessible && !step.isActive,
            }"
          >
            <i v-if="step.isCompleted && !step.isActive" class="pi pi-check text-sm"></i>
            <span v-else>{{ step.id }}</span>
          </div>

          <!-- Label -->
          <span
            class="text-center text-xs font-medium leading-tight whitespace-nowrap"
            :class="{
              'text-primary font-bold': step.isActive,
              'text-secondary': step.isCompleted,
              'text-tertiary': !step.isActive && !step.isCompleted,
            }"
          >
            {{ t(step.titleKey) }}
          </span>
        </button>

        <!-- Connector line -->
        <div
          v-if="index < steps.length - 1"
          class="mx-2 h-0.5 flex-1 transition-colors duration-300"
          :class="{
            'bg-primary': step.isCompleted,
            'bg-surface-dim': !step.isCompleted,
          }"
        ></div>
      </li>
    </ol>
  </nav>
</template>
