<script setup lang="ts">
/**
 * QuickActions Component - Modern Design
 *
 * Provides quick access buttons for common actions:
 * - Create new competition
 * - View reports
 * - AI Assistant (highlighted with AI branding)
 * - Manage committees
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
  isAi?: boolean
  iconBg: string
  iconColor: string
}

const actions: QuickAction[] = [
  {
    key: 'createCompetition',
    icon: 'pi-plus-circle',
    labelKey: 'dashboard.quickActions.createCompetition',
    route: '/rfp/create',
    iconBg: 'bg-primary-50',
    iconColor: 'text-primary',
  },
  {
    key: 'viewReports',
    icon: 'pi-chart-bar',
    labelKey: 'dashboard.quickActions.viewReports',
    route: '/reports',
    iconBg: 'bg-info-50',
    iconColor: 'text-info',
  },
  {
    key: 'aiAssistant',
    icon: 'pi-sparkles',
    labelKey: 'dashboard.quickActions.aiAssistant',
    route: '/ai-assistant',
    isAi: true,
    iconBg: 'bg-ai-50',
    iconColor: 'text-ai-600',
  },
  {
    key: 'manageCommittees',
    icon: 'pi-users',
    labelKey: 'dashboard.quickActions.manageCommittees',
    route: '/committees/permanent',
    iconBg: 'bg-warning-50',
    iconColor: 'text-warning',
  },
]

function navigateTo(route: string): void {
  router.push(route)
}
</script>

<template>
  <div class="rounded-xl border border-secondary-100 bg-white p-5 shadow-sm">
    <h3 class="mb-4 text-lg font-semibold text-secondary-800">
      {{ t('dashboard.quickActions.title') }}
    </h3>
    <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
      <button
        v-for="action in actions"
        :key="action.key"
        class="group flex flex-col items-center gap-3 rounded-xl border p-5 transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
        :class="[
          action.isAi
            ? 'border-ai-200 bg-gradient-to-br from-ai-50 to-white hover:border-ai-300 hover:shadow-ai-100/50'
            : 'border-secondary-100 bg-white hover:border-primary-200',
        ]"
        @click="navigateTo(action.route)"
      >
        <div
          class="flex h-12 w-12 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110"
          :class="[
            action.isAi
              ? 'bg-gradient-to-br from-ai-100 to-ai-50'
              : action.iconBg,
          ]"
        >
          <i
            class="pi text-xl"
            :class="[action.icon, action.iconColor]"
          ></i>
        </div>
        <span
          class="text-center text-xs font-semibold"
          :class="[
            action.isAi ? 'text-ai-700' : 'text-secondary-700',
          ]"
        >
          {{ t(action.labelKey) }}
        </span>
        <!-- AI badge -->
        <span
          v-if="action.isAi"
          class="rounded-full bg-ai-100 px-2 py-0.5 text-[10px] font-bold text-ai-600"
        >
          Powered by AI
        </span>
      </button>
    </div>
  </div>
</template>
