<script setup lang="ts">
/**
 * DashboardView - Professional Dashboard (Redesigned to match RFP.AI reference)
 */
import { onMounted, onUnmounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { useFormatters } from '@/composables/useFormatters'
import { storeToRefs } from 'pinia'

const { t, locale } = useI18n()
const router = useRouter()
const { formatNumber, formatDateTime } = useFormatters()
const dashboardStore = useDashboardStore()
const authStore = useAuthStore()

const {
  isLoading, stats,
  activeCompetitions,
  recentActivities,
} = storeToRefs(dashboardStore)

const AUTO_REFRESH_INTERVAL = 5 * 60 * 1000 // 5 minutes
let refreshTimer: ReturnType<typeof setInterval> | null = null

onMounted(async () => {
  await dashboardStore.loadDashboard()
  refreshTimer = setInterval(() => dashboardStore.refreshDashboard(), AUTO_REFRESH_INTERVAL)
})

onUnmounted(() => {
  if (refreshTimer) { clearInterval(refreshTimer); refreshTimer = null }
})

function getGreeting(): string {
  const h = new Date().getHours()
  if (h < 12) return t('dashboard.greetingMorning')
  if (h < 17) return t('dashboard.greetingAfternoon')
  return t('dashboard.greetingEvening')
}

interface KpiCard { key: string; icon: string; iconBg: string; iconColor: string; value: number; labelKey: string; trend: string; trendUp: boolean }

const kpiCards = computed<KpiCard[]>(() => [
  { key: 'activeRfps', icon: 'pi-file-edit', iconBg: 'bg-interactive-50', iconColor: 'text-interactive', value: stats.value.activeCompetitions, labelKey: 'dashboard.stats.activeCompetitions', trend: '', trendUp: false },
  { key: 'totalOffers', icon: 'pi-send', iconBg: 'bg-accent-50', iconColor: 'text-accent-dark', value: stats.value.totalOffers, labelKey: 'dashboard.stats.totalOffers', trend: '', trendUp: false },
  { key: 'pendingReviews', icon: 'pi-clock', iconBg: 'bg-warning-50', iconColor: 'text-warning', value: stats.value.pendingEvaluations, labelKey: 'dashboard.stats.pendingEvaluations', trend: '', trendUp: false },
  { key: 'completed', icon: 'pi-check-circle', iconBg: 'bg-success-50', iconColor: 'text-success', value: stats.value.completedCompetitions, labelKey: 'dashboard.stats.completedCompetitions', trend: '', trendUp: false },
])

interface QuickAction { key: string; icon: string; labelKey: string; subtitleKey: string; route: string; isAi?: boolean; bgClass: string; iconColor: string; requiredPermission?: string }

const allQuickActions: QuickAction[] = [
  { key: 'createRfp', icon: 'pi-file-edit', labelKey: 'dashboard.quickActions.createCompetition', subtitleKey: 'dashboard.quickActions.createCompetitionSub', route: '/rfp/new', bgClass: 'bg-interactive-50', iconColor: 'text-interactive', requiredPermission: 'rfp.create' },
  { key: 'uploadProposal', icon: 'pi-upload', labelKey: 'dashboard.quickActions.uploadProposal', subtitleKey: 'dashboard.quickActions.uploadProposalSub', route: '/rfp', bgClass: 'bg-surface-subtle', iconColor: 'text-secondary-600', requiredPermission: 'offers.create' },
  { key: 'aiAssistant', icon: 'pi-sparkles', labelKey: 'dashboard.quickActions.aiAssistant', subtitleKey: 'dashboard.quickActions.aiAssistantSub', route: '/ai-assistant', isAi: true, bgClass: 'bg-ai-50', iconColor: 'text-ai-600' },
  { key: 'scoring', icon: 'pi-chart-bar', labelKey: 'dashboard.quickActions.scoring', subtitleKey: 'dashboard.quickActions.scoringSub', route: '/evaluation/technical', bgClass: 'bg-surface-subtle', iconColor: 'text-secondary-600', requiredPermission: 'evaluation.view' },
]

const quickActions = computed(() =>
  allQuickActions.filter(a => !a.requiredPermission || authStore.hasPermission(a.requiredPermission))
)

const canCreateRfp = computed(() => authStore.hasPermission('rfp.create'))

const gettingStartedSteps = [
  { number: 1, labelKey: 'dashboard.gettingStarted.step1', descKey: 'dashboard.gettingStarted.step1Desc' },
  { number: 2, labelKey: 'dashboard.gettingStarted.step2', descKey: 'dashboard.gettingStarted.step2Desc' },
  { number: 3, labelKey: 'dashboard.gettingStarted.step3', descKey: 'dashboard.gettingStarted.step3Desc' },
]

const hasCompetitions = computed(() => activeCompetitions.value.length > 0)

function navigateTo(route: string): void { router.push(route) }

function getActivityIcon(type: string): string {
  const m: Record<string, string> = { rfp_created: 'pi-file-plus', proposal_uploaded: 'pi-folder-open', status_changed: 'pi-refresh', evaluation_started: 'pi-chart-line', score_submitted: 'pi-star', comment_added: 'pi-comment' }
  return m[type] ?? 'pi-circle'
}

function getActivityColor(type: string): string {
  const m: Record<string, string> = { rfp_created: 'bg-interactive-50 text-interactive', proposal_uploaded: 'bg-accent-50 text-accent-dark', status_changed: 'bg-warning-50 text-warning', evaluation_started: 'bg-ai-50 text-ai-600', score_submitted: 'bg-success-50 text-success', comment_added: 'bg-info-50 text-info' }
  return m[type] ?? 'bg-surface-subtle text-tertiary'
}
</script>

<template>
  <div class="space-y-6">
    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- 1. WELCOME BANNER                                         -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <div
      class="animate-fade-in relative overflow-hidden rounded-2xl p-6 text-white shadow-elevated sm:p-8"
      style="background: linear-gradient(135deg, #4A7CC9 0%, #5B8FD9 40%, #7BA8E8 100%);"
    >
      <div class="pointer-events-none absolute inset-0 overflow-hidden">
        <div class="absolute -end-12 -top-12 h-40 w-40 rounded-full bg-white/10"></div>
        <div class="absolute -bottom-8 start-1/3 h-32 w-32 rounded-full bg-white/5"></div>
      </div>
      <div class="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p class="text-sm font-medium text-white/80">{{ getGreeting() }}</p>
          <h1 class="mt-1 text-2xl font-bold sm:text-3xl">{{ t('dashboard.welcomeBack') }}</h1>
          <div class="mt-2 flex items-center gap-2 text-white/60">
            <i class="pi pi-building text-sm"></i>
            <span class="text-sm">{{ (authStore.user as any)?.organizationName || t('app.name') }}</span>
          </div>
        </div>
        <button
          v-if="canCreateRfp"
          class="flex items-center gap-2 self-start rounded-xl border border-white/30 bg-white px-5 py-2.5 text-sm font-semibold text-interactive shadow-sm transition-all hover:shadow-md"
          @click="navigateTo('/rfp/new')"
        >
          <i class="pi pi-plus text-xs"></i>
          {{ t('dashboard.createNewRfp') }}
        </button>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- 2. KPI STAT CARDS                                         -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <div
        v-for="(card, index) in kpiCards"
        :key="card.key"
        class="animate-fade-in-up"
        :style="{ animationDelay: `${index * 80}ms` }"
      >
        <!-- Skeleton -->
        <div v-if="isLoading" class="h-28 animate-pulse rounded-2xl bg-secondary-200"></div>
        <!-- Card -->
        <div v-else class="group relative overflow-hidden rounded-2xl border border-secondary-100 bg-white p-5 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md">
          <div class="flex items-start justify-between">
            <div
              class="flex h-11 w-11 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110"
              :class="card.iconBg"
            >
              <i class="pi text-lg" :class="[card.icon, card.iconColor]"></i>
            </div>
            <span
              v-if="card.trend"
              class="flex items-center gap-0.5 text-xs font-medium"
              :class="card.trendUp ? 'text-success' : 'text-danger'"
            >
              <i class="pi text-[10px]" :class="card.trendUp ? 'pi-arrow-up-right' : 'pi-arrow-down-right'"></i>
              {{ card.trend }}
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-bold tracking-tight text-secondary">{{ formatNumber(card.value) }}</p>
            <p class="mt-1 text-xs font-medium text-tertiary">{{ t(card.labelKey) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- 3. RECENT RFPS + QUICK ACTIONS                            -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <!-- Recent RFPs (2/3) -->
      <div class="lg:col-span-2">
        <div class="rounded-2xl border border-secondary-100 bg-white shadow-sm">
          <div class="flex items-center justify-between border-b border-secondary-100 px-6 py-4">
            <div class="flex items-center gap-2">
              <i class="pi pi-layers text-secondary-400"></i>
              <h3 class="text-base font-semibold text-secondary">{{ t('dashboard.recentRfps') }}</h3>
            </div>
            <router-link
              to="/rfp"
              class="flex items-center gap-1 text-sm font-medium text-interactive transition-colors hover:text-interactive-dark"
            >
              {{ t('dashboard.viewAll') }}
              <i class="pi pi-arrow-left text-xs ltr:rotate-180"></i>
            </router-link>
          </div>
          <div class="p-6">
            <!-- Loading -->
            <div v-if="isLoading" class="space-y-3">
              <div v-for="i in 3" :key="i" class="flex animate-pulse items-center gap-3 rounded-lg p-3">
                <div class="h-10 w-10 rounded-lg bg-secondary-200"></div>
                <div class="flex-1"><div class="mb-2 h-3 w-3/4 rounded bg-secondary-200"></div><div class="h-2 w-1/2 rounded bg-secondary-100"></div></div>
              </div>
            </div>
            <!-- Empty state -->
            <div v-else-if="!hasCompetitions" class="flex flex-col items-center py-10 text-center">
              <div class="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-surface-subtle">
                <i class="pi pi-file-plus text-3xl text-secondary-300"></i>
              </div>
              <h4 class="text-base font-semibold text-secondary">{{ t('dashboard.noRfpsYet') }}</h4>
              <p class="mt-1.5 max-w-xs text-sm text-tertiary">{{ t('dashboard.noRfpsDescription') }}</p>
              <button
                v-if="canCreateRfp"
                class="btn-primary mt-5"
                @click="navigateTo('/rfp/new')"
              >
                <i class="pi pi-plus text-xs"></i>
                {{ t('dashboard.createFirstRfp') }}
              </button>
            </div>
            <!-- Competition list -->
            <div v-else class="space-y-2">
              <div
                v-for="comp in activeCompetitions.slice(0, 5)"
                :key="comp.id"
                class="group flex cursor-pointer items-center gap-4 rounded-xl p-3 transition-all hover:bg-surface-subtle"
                @click="navigateTo(`/rfp/${comp.id}/edit`)"
              >
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-interactive-50">
                  <i class="pi pi-file-edit text-interactive"></i>
                </div>
                <div class="min-w-0 flex-1">
                  <h4 class="truncate text-sm font-medium text-secondary group-hover:text-interactive">
                    {{ locale === 'ar' ? comp.titleAr : comp.titleEn }}
                  </h4>
                  <p class="mt-0.5 text-xs text-tertiary">{{ comp.referenceNumber }}</p>
                </div>
                <span
                  class="shrink-0 rounded-full px-2.5 py-1 text-[10px] font-semibold"
                  :class="{
                    'bg-interactive-50 text-interactive': comp.status === 'published' || comp.status === 'receiving_offers',
                    'bg-warning-50 text-warning': comp.status === 'pending_approval' || comp.status === 'technical_evaluation' || comp.status === 'financial_evaluation',
                    'bg-success-50 text-success': comp.status === 'completed' || comp.status === 'approved',
                    'bg-secondary-100 text-secondary-500': comp.status === 'draft',
                    'bg-info-50 text-info': comp.status === 'awarding',
                  }"
                >
                  {{ t(`dashboard.competitionStatus.${comp.status}`) }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Actions (1/3) -->
      <div>
        <div class="rounded-2xl border border-secondary-100 bg-white shadow-sm">
          <div class="flex items-center gap-2 border-b border-secondary-100 px-6 py-4">
            <i class="pi pi-bolt text-warning"></i>
            <h3 class="text-base font-semibold text-secondary">{{ t('dashboard.quickActions.title') }}</h3>
          </div>
          <div class="space-y-1.5 p-4">
            <button
              v-for="action in quickActions"
              :key="action.key"
              class="flex w-full items-center gap-3 rounded-xl p-3 text-start transition-all duration-200 hover:bg-surface-subtle"
              @click="navigateTo(action.route)"
            >
              <div
                class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl"
                :class="action.bgClass"
              >
                <i class="pi" :class="[action.icon, action.iconColor]"></i>
              </div>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-semibold" :class="action.isAi ? 'text-ai-600' : 'text-secondary'">
                  {{ t(action.labelKey) }}
                </p>
                <p class="mt-0.5 text-xs text-tertiary">{{ t(action.subtitleKey) }}</p>
              </div>
              <i class="pi pi-chevron-left text-xs text-secondary-300 ltr:rotate-180"></i>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- 4. RECENT ACTIVITY + GETTING STARTED                      -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <!-- Recent Activity (2/3) -->
      <div class="lg:col-span-2">
        <div class="rounded-2xl border border-secondary-100 bg-white shadow-sm">
          <div class="flex items-center gap-2 border-b border-secondary-100 px-6 py-4">
            <i class="pi pi-history text-accent-dark"></i>
            <h3 class="text-base font-semibold text-secondary">{{ t('dashboard.recentActivity') }}</h3>
          </div>
          <div class="p-6">
            <!-- Loading -->
            <div v-if="isLoading" class="space-y-4">
              <div v-for="i in 4" :key="i" class="flex animate-pulse items-center gap-3">
                <div class="h-8 w-8 rounded-full bg-secondary-200"></div>
                <div class="flex-1"><div class="mb-1 h-3 w-2/3 rounded bg-secondary-200"></div><div class="h-2 w-1/3 rounded bg-secondary-100"></div></div>
              </div>
            </div>
            <!-- Empty -->
            <div v-else-if="recentActivities.length === 0" class="py-8 text-center">
              <i class="pi pi-history mb-2 text-3xl text-secondary-300"></i>
              <p class="text-sm text-tertiary">{{ t('dashboard.noActivity') }}</p>
            </div>
            <!-- Activity list -->
            <div v-else class="space-y-1">
              <div
                v-for="activity in recentActivities.slice(0, 6)"
                :key="activity.id"
                class="flex items-start gap-3 rounded-lg p-2.5 transition-colors hover:bg-surface-subtle"
              >
                <div
                  class="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full"
                  :class="getActivityColor(activity.entityType)"
                >
                  <i class="pi text-xs" :class="getActivityIcon(activity.entityType)"></i>
                </div>
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium text-secondary">
                    {{ locale === 'ar' ? activity.actionAr : activity.actionEn }}
                  </p>
                  <p class="mt-0.5 truncate text-xs text-tertiary">{{ locale === 'ar' ? activity.userNameAr : activity.userNameEn }}</p>
                </div>
                <span class="shrink-0 text-[11px] text-tertiary">{{ formatDateTime(activity.timestamp) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Getting Started (1/3) -->
      <div>
        <div class="rounded-2xl border border-secondary-100 bg-white shadow-sm">
          <div class="flex items-center gap-2 border-b border-secondary-100 px-6 py-4">
            <i class="pi pi-flag text-interactive"></i>
            <h3 class="text-base font-semibold text-secondary">{{ t('dashboard.gettingStarted.title') }}</h3>
          </div>
          <div class="p-5">
            <div class="flex gap-3">
              <div
                v-for="step in gettingStartedSteps"
                :key="step.number"
                class="flex flex-1 flex-col items-center rounded-xl border border-secondary-100 p-4 text-center transition-all hover:border-interactive-100 hover:bg-interactive-50/30"
              >
                <span class="mb-3 flex h-8 w-8 items-center justify-center rounded-lg bg-interactive text-sm font-bold text-white">
                  {{ step.number }}
                </span>
                <p class="text-xs font-semibold text-secondary">{{ t(step.labelKey) }}</p>
                <p class="mt-1 text-[10px] leading-relaxed text-tertiary">{{ t(step.descKey) }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════ -->
    <!-- 5. AI INSIGHTS BANNER                                     -->
    <!-- ══════════════════════════════════════════════════════════ -->
    <div
      class="animate-fade-in relative overflow-hidden rounded-2xl p-6"
      style="background: linear-gradient(135deg, #7C3AED 0%, #8B5CF6 50%, #A78BFA 100%);"
    >
      <div class="pointer-events-none absolute inset-0 overflow-hidden">
        <div class="absolute -end-8 -top-8 h-32 w-32 rounded-full bg-white/10"></div>
        <div class="absolute -bottom-4 start-1/4 h-24 w-24 rounded-full bg-white/5"></div>
      </div>
      <div class="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex items-center gap-4">
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-white/20">
            <i class="pi pi-sparkles text-xl text-white"></i>
          </div>
          <div>
            <h3 class="text-lg font-bold text-white">{{ t('dashboard.aiInsights') }}</h3>
            <p class="mt-0.5 text-sm text-white/80">{{ t('dashboard.aiInsightsDescription') }}</p>
          </div>
        </div>
        <button
          class="flex items-center gap-2 self-start rounded-xl bg-white/20 px-5 py-2.5 text-sm font-semibold text-white backdrop-blur-sm transition-all hover:bg-white/30"
          @click="navigateTo('/ai-assistant')"
        >
          <i class="pi pi-comments text-sm"></i>
          {{ t('dashboard.startAiChat') }}
        </button>
      </div>
    </div>
  </div>
</template>
