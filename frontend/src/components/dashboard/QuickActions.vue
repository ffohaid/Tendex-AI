<script setup lang="ts">
/**
 * QuickActions Component
 *
 * Provides quick access buttons for common actions:
 * - Create new competition
 * - Submit offer
 * - View reports
 * - AI Assistant
 */
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

const { t } = useI18n()
const router = useRouter()

interface QuickAction {
  key: string
  icon: string
  labelKey: string
  route: string
  color: string
  bgColor: string
}

const actions: QuickAction[] = [
  {
    key: 'createCompetition',
    icon: 'pi-plus-circle',
    labelKey: 'dashboard.quickActions.createCompetition',
    route: '/rfp/create',
    color: 'text-primary',
    bgColor: 'bg-primary/10 hover:bg-primary/20',
  },
  {
    key: 'viewReports',
    icon: 'pi-chart-bar',
    labelKey: 'dashboard.quickActions.viewReports',
    route: '/reports',
    color: 'text-info',
    bgColor: 'bg-info/10 hover:bg-info/20',
  },
  {
    key: 'aiAssistant',
    icon: 'pi-sparkles',
    labelKey: 'dashboard.quickActions.aiAssistant',
    route: '/ai-assistant',
    color: 'text-tertiary',
    bgColor: 'bg-tertiary/10 hover:bg-tertiary/20',
  },
  {
    key: 'manageCommittees',
    icon: 'pi-users',
    labelKey: 'dashboard.quickActions.manageCommittees',
    route: '/committees/permanent',
    color: 'text-warning',
    bgColor: 'bg-warning/10 hover:bg-warning/20',
  },
]

function navigateTo(route: string): void {
  router.push(route)
}
</script>

<template>
  <div class="card">
    <h3 class="mb-4 text-lg font-semibold text-secondary">
      {{ t('dashboard.quickActions.title') }}
    </h3>
    <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
      <button
        v-for="action in actions"
        :key="action.key"
        class="flex flex-col items-center gap-2 rounded-xl p-4 transition-all duration-200"
        :class="action.bgColor"
        @click="navigateTo(action.route)"
      >
        <div
          class="flex h-10 w-10 items-center justify-center rounded-lg"
          :class="action.bgColor"
        >
          <i class="pi text-xl" :class="[action.icon, action.color]"></i>
        </div>
        <span class="text-center text-xs font-medium text-secondary">
          {{ t(action.labelKey) }}
        </span>
      </button>
    </div>
  </div>
</template>
