<script setup lang="ts">
/**
 * TechnicalEvaluationDetail - Main technical evaluation workspace.
 * Implements blind evaluation with AI-assisted dual scoring.
 * Vendors are shown with anonymous codes only.
 */
import { onMounted, onUnmounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEvaluationStore } from '@/stores/evaluation'
import EvaluationHeader from '@/components/evaluation/EvaluationHeader.vue'
import CriteriaPanel from '@/components/evaluation/CriteriaPanel.vue'
import VendorScoreCard from '@/components/evaluation/VendorScoreCard.vue'
import type { EvaluationCriterion } from '@/types/evaluation'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEvaluationStore()

const competitionId = computed(() => route.params.id as string)
const selectedCriterion = ref<EvaluationCriterion | null>(null)
const activeTab = ref<'scoring' | 'matrix' | 'minutes'>('scoring')
const savingStatus = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')

/* Local score map: vendorId -> { score, notes } */
const localScores = ref<Record<string, { score: number; notes: string }>>({})

onMounted(async () => {
  await store.selectCompetition(competitionId.value)
  await store.loadTechnicalData(competitionId.value)
  if (store.criteria.length > 0) {
    selectedCriterion.value = store.criteria[0]
  }
  store.startAutoSave(competitionId.value, 'technical')
  initializeLocalScores()
})

onUnmounted(() => {
  store.stopAutoSave()
})

function initializeLocalScores() {
  store.vendors.forEach(vendor => {
    if (selectedCriterion.value) {
      const existing = store.technicalScores.find(
        s => s.vendorId === vendor.id && s.criterionId === selectedCriterion.value!.id
      )
      localScores.value[vendor.id] = {
        score: existing?.score ?? 0,
        notes: existing?.notes ?? '',
      }
    }
  })
}

function onCriterionSelect(criterion: EvaluationCriterion) {
  selectedCriterion.value = criterion
  initializeLocalScores()
}

function updateScore(vendorId: string, score: number) {
  if (!localScores.value[vendorId]) {
    localScores.value[vendorId] = { score: 0, notes: '' }
  }
  localScores.value[vendorId].score = score
}

function updateNotes(vendorId: string, notes: string) {
  if (!localScores.value[vendorId]) {
    localScores.value[vendorId] = { score: 0, notes: '' }
  }
  localScores.value[vendorId].notes = notes
}

function getAiEvaluation(vendorId: string) {
  if (!selectedCriterion.value) return undefined
  return store.aiEvaluations.find(
    a => a.vendorId === vendorId && a.criterionId === selectedCriterion.value!.id
  )
}

function getVariancePercent(vendorId: string): number | undefined {
  if (!selectedCriterion.value) return undefined
  const alert = store.varianceAlerts.find(
    a => a.vendorId === vendorId && a.criterionId === selectedCriterion.value!.id
  )
  return alert?.variancePercent
}

async function saveScores() {
  if (!selectedCriterion.value) return
  savingStatus.value = 'saving'
  try {
    const scores = Object.entries(localScores.value).map(([vendorId, data]) => ({
      criterionId: selectedCriterion.value!.id,
      vendorId,
      score: data.score,
      maxScore: 100,
      notes: data.notes,
    }))
    for (const score of scores) {
      await store.submitScore(competitionId.value, 'technical', score)
    }
    savingStatus.value = 'saved'
    setTimeout(() => { savingStatus.value = 'idle' }, 2000)
  } catch {
    savingStatus.value = 'error'
  }
}

async function requestAiHelp(vendorId: string) {
  await store.requestAiAnalysis(competitionId.value, vendorId)
}

function goToComparison() {
  router.push({
    name: 'TechnicalComparison',
    params: { id: competitionId.value },
  })
}

const completedCriteria = computed(() => {
  if (!store.criteria.length || !store.vendors.length) return 0
  const criteriaWithScores = new Set(
    store.technicalScores.map(s => s.criterionId)
  )
  return criteriaWithScores.size
})
</script>

<template>
  <div class="space-y-6">
    <!-- Back button -->
    <button
      class="flex items-center gap-2 text-sm text-secondary/60 transition-colors hover:text-primary"
      @click="router.push({ name: 'EvaluationTechnical' })"
    >
      <i class="pi pi-arrow-right rtl:pi-arrow-left" />
      {{ t('common.back') }}
    </button>

    <!-- Loading -->
    <div v-if="store.loading && !store.selectedCompetition" class="flex items-center justify-center py-12">
      <i class="pi pi-spinner pi-spin text-2xl text-primary" />
    </div>

    <template v-else-if="store.selectedCompetition">
      <!-- Header -->
      <EvaluationHeader
        :competition="store.selectedCompetition"
        :committee="store.committee"
        evaluation-type="technical"
      />

      <!-- Tab navigation -->
      <div class="flex gap-1 rounded-xl border border-surface-dim bg-surface-muted p-1">
        <button
          v-for="tab in (['scoring', 'matrix', 'minutes'] as const)"
          :key="tab"
          class="flex-1 rounded-lg px-4 py-2 text-sm font-medium transition-all"
          :class="activeTab === tab
            ? 'bg-white text-primary shadow-sm'
            : 'text-secondary/60 hover:text-secondary'"
          @click="activeTab = tab"
        >
          <i
            class="pi me-1.5"
            :class="{
              'pi-pencil': tab === 'scoring',
              'pi-th-large': tab === 'matrix',
              'pi-file-edit': tab === 'minutes',
            }"
          />
          {{ t(`evaluation.tabs.${tab}`) }}
        </button>
      </div>

      <!-- Scoring tab -->
      <div v-if="activeTab === 'scoring'" class="grid grid-cols-1 gap-6 xl:grid-cols-12">
        <!-- Criteria sidebar -->
        <div class="xl:col-span-4">
          <CriteriaPanel
            :criteria="store.criteria"
            :total-weight="store.totalCriteriaWeight"
            @select-criterion="onCriterionSelect"
          />

          <!-- Progress summary -->
          <div class="card mt-4">
            <h3 class="mb-2 text-sm font-semibold text-secondary">
              {{ t('evaluation.scoringProgress') }}
            </h3>
            <div class="flex items-center gap-3">
              <div class="flex-1">
                <div class="h-2 w-full overflow-hidden rounded-full bg-surface-muted">
                  <div
                    class="h-full rounded-full bg-primary transition-all"
                    :style="{ width: `${(completedCriteria / Math.max(store.criteria.length, 1)) * 100}%` }"
                  />
                </div>
              </div>
              <span class="text-sm font-medium text-secondary">
                {{ completedCriteria }} / {{ store.criteria.length }}
              </span>
            </div>
          </div>
        </div>

        <!-- Scoring area -->
        <div class="xl:col-span-8">
          <div v-if="selectedCriterion" class="space-y-4">
            <!-- Selected criterion header -->
            <div class="card border-primary/20 bg-primary/5">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-lg font-bold text-secondary">{{ selectedCriterion.name }}</h2>
                  <p v-if="selectedCriterion.description" class="mt-1 text-sm text-secondary/60">
                    {{ selectedCriterion.description }}
                  </p>
                </div>
                <div class="flex items-center gap-3">
                  <span class="badge badge-primary">{{ selectedCriterion.weight }}%</span>
                  <span v-if="selectedCriterion.minimumScore" class="text-xs text-secondary/50">
                    {{ t('evaluation.criteria.minimumScore') }}: {{ selectedCriterion.minimumScore }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Vendor score cards (blind evaluation) -->
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <VendorScoreCard
                v-for="vendor in store.vendors"
                :key="vendor.id"
                :vendor="vendor"
                :criterion="selectedCriterion"
                :score="localScores[vendor.id]?.score ?? 0"
                :notes="localScores[vendor.id]?.notes ?? ''"
                :max-score="100"
                :ai-evaluation="getAiEvaluation(vendor.id)"
                :show-variance-alert="true"
                :variance-percent="getVariancePercent(vendor.id)"
                @update:score="updateScore(vendor.id, $event)"
                @update:notes="updateNotes(vendor.id, $event)"
              />
            </div>

            <!-- Action buttons -->
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <button
                  v-for="vendor in store.vendors"
                  :key="`ai-${vendor.id}`"
                  class="flex items-center gap-1.5 rounded-lg border border-info/30 bg-info/5 px-3 py-2 text-xs font-medium text-info transition-colors hover:bg-info/10"
                  @click="requestAiHelp(vendor.id)"
                >
                  <i class="pi pi-sparkles" />
                  {{ t('evaluation.ai.analyze') }} {{ vendor.code }}
                </button>
              </div>

              <div class="flex items-center gap-3">
                <!-- Save status -->
                <span v-if="savingStatus === 'saving'" class="text-xs text-secondary/50">
                  <i class="pi pi-spinner pi-spin me-1" />
                  {{ t('evaluation.saving') }}
                </span>
                <span v-else-if="savingStatus === 'saved'" class="text-xs text-success">
                  <i class="pi pi-check me-1" />
                  {{ t('evaluation.saved') }}
                </span>

                <button
                  class="rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-dark"
                  @click="saveScores"
                >
                  <i class="pi pi-save me-1.5" />
                  {{ t('evaluation.saveScores') }}
                </button>
              </div>
            </div>
          </div>

          <!-- No criterion selected -->
          <div v-else class="card text-center">
            <i class="pi pi-arrow-right rtl:pi-arrow-left text-3xl text-secondary/20" />
            <p class="mt-3 text-sm text-secondary/60">{{ t('evaluation.selectCriterion') }}</p>
          </div>
        </div>
      </div>

      <!-- Matrix tab -->
      <div v-else-if="activeTab === 'matrix'" class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-bold text-secondary">
            <i class="pi pi-th-large me-2 text-primary" />
            {{ t('evaluation.heatmap.title') }}
          </h2>
          <button
            class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
            @click="goToComparison"
          >
            <i class="pi pi-external-link" />
            {{ t('evaluation.heatmap.fullView') }}
          </button>
        </div>

        <!-- Inline heatmap preview -->
        <div class="card overflow-x-auto">
          <table class="w-full border-collapse text-sm">
            <thead>
              <tr>
                <th class="border-b border-surface-dim px-4 py-3 text-start text-xs font-semibold text-secondary/60">
                  {{ t('evaluation.heatmap.criterion') }}
                </th>
                <th
                  v-for="vendor in store.vendors"
                  :key="vendor.id"
                  class="border-b border-surface-dim px-4 py-3 text-center text-xs font-semibold text-secondary/60"
                >
                  {{ vendor.code }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, rowIdx) in store.buildHeatmapCells()" :key="rowIdx">
                <td class="border-b border-surface-dim px-4 py-3 text-sm font-medium text-secondary">
                  {{ row[0]?.criterionName }}
                </td>
                <td
                  v-for="cell in row"
                  :key="cell.vendorId"
                  class="border-b border-surface-dim px-4 py-3 text-center"
                  :class="{
                    'bg-success/10 text-success': cell.color === 'excellent',
                    'bg-warning/10 text-warning': cell.color === 'average',
                    'bg-danger/10 text-danger': cell.color === 'weak',
                  }"
                >
                  <span class="text-sm font-bold">{{ cell.percentage.toFixed(0) }}%</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Minutes tab -->
      <div v-else-if="activeTab === 'minutes'" class="card text-center py-12">
        <i class="pi pi-file-edit text-4xl text-secondary/20" />
        <p class="mt-3 text-sm text-secondary/60">{{ t('evaluation.minutes.comingSoon') }}</p>
      </div>
    </template>
  </div>
</template>
