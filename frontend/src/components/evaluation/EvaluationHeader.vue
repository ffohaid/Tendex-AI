<script setup lang="ts">
/**
 * EvaluationHeader - Top section of evaluation pages.
 * Shows competition info, committee details, and progress.
 */
import { useI18n } from 'vue-i18n'
import DualDateDisplay from '@/components/common/DualDateDisplay.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import type { CompetitionEvaluation, Committee } from '@/types/evaluation'

defineProps<{
  competition: CompetitionEvaluation
  committee: Committee | null
  evaluationType: 'technical' | 'financial'
}>()

const { t } = useI18n()
</script>

<template>
  <div class="card mb-6">
    <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
      <!-- Competition Info -->
      <div class="flex-1">
        <div class="mb-2 flex items-center gap-3">
          <h1 class="text-xl font-bold text-secondary">
            {{ evaluationType === 'technical'
              ? t('evaluation.technical.title')
              : t('evaluation.financial.title')
            }}
          </h1>
          <StatusBadge
            :status="evaluationType === 'technical'
              ? competition.technicalStatus
              : competition.financialStatus"
          />
        </div>

        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div class="flex items-center gap-2">
            <i class="pi pi-folder text-sm text-primary" />
            <div>
              <span class="text-xs text-secondary/60">{{ t('evaluation.projectName') }}</span>
              <p class="text-sm font-medium text-secondary">{{ competition.projectName }}</p>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <i class="pi pi-hashtag text-sm text-primary" />
            <div>
              <span class="text-xs text-secondary/60">{{ t('evaluation.competitionNumber') }}</span>
              <p class="text-sm font-medium text-secondary">{{ competition.competitionNumber }}</p>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <i class="pi pi-calendar text-sm text-primary" />
            <div>
              <span class="text-xs text-secondary/60">{{ t('evaluation.deadline') }}</span>
              <DualDateDisplay :date="competition.deadlineGregorian" />
            </div>
          </div>

          <div class="flex items-center gap-2">
            <i class="pi pi-users text-sm text-primary" />
            <div>
              <span class="text-xs text-secondary/60">{{ t('evaluation.vendorCount') }}</span>
              <p class="text-sm font-medium text-secondary">{{ competition.vendorCount }}</p>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <i class="pi pi-wallet text-sm text-primary" />
            <div>
              <span class="text-xs text-secondary/60">{{ t('evaluation.estimatedBudget') }}</span>
              <p class="text-sm font-medium text-secondary">
                {{ new Intl.NumberFormat('en-SA', { minimumFractionDigits: 2 }).format(competition.estimatedBudget) }}
                &#xFDFC;
              </p>
            </div>
          </div>

          <div v-if="committee" class="flex items-center gap-2">
            <i class="pi pi-sitemap text-sm text-primary" />
            <div>
              <span class="text-xs text-secondary/60">{{ t('evaluation.committee') }}</span>
              <p class="text-sm font-medium text-secondary">{{ committee.name }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Progress -->
      <div class="flex flex-col items-end gap-2">
        <div class="text-end">
          <span class="text-xs text-secondary/60">{{ t('evaluation.progress') }}</span>
          <p class="text-2xl font-bold text-primary">{{ competition.progress }}%</p>
        </div>
        <div class="h-2 w-32 overflow-hidden rounded-full bg-surface-muted">
          <div
            class="h-full rounded-full bg-primary transition-all duration-500"
            :style="{ width: `${competition.progress}%` }"
          />
        </div>
      </div>
    </div>

    <!-- Committee Members -->
    <div v-if="committee" class="mt-4 border-t border-surface-dim pt-4">
      <h3 class="mb-2 text-sm font-semibold text-secondary">
        {{ t('evaluation.committeeMembers') }}
      </h3>
      <div class="flex flex-wrap gap-2">
        <div
          v-for="member in committee.members"
          :key="member.id"
          class="flex items-center gap-2 rounded-full border border-surface-dim bg-surface-muted px-3 py-1"
        >
          <div
            class="flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold text-white"
            :class="member.role === 'chair' ? 'bg-primary' : 'bg-tertiary'"
          >
            {{ member.name.charAt(0) }}
          </div>
          <span class="text-xs font-medium text-secondary">{{ member.name }}</span>
          <span class="text-xs text-secondary/50">
            ({{ t(`evaluation.roles.${member.role}`) }})
          </span>
          <i
            v-if="member.hasSubmittedScores"
            class="pi pi-check-circle text-xs text-success"
            :title="t('evaluation.scoresSubmitted')"
          />
        </div>
      </div>
    </div>
  </div>
</template>
