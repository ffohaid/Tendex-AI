<script setup lang="ts">
/**
 * Tenant Selector Component — Super Admin Portal.
 *
 * Provides a dropdown for the Super Admin to switch between tenant contexts.
 * This resolves the issue of tenantId being stored as an empty GUID.
 * Data is fetched dynamically from the API — NO mock data.
 *
 * Usage: Place in the top header/navbar for Super Admin users.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useTenantStore } from '@/stores/tenant'
import { TenantStatus } from '@/types/tenant'
import type { TenantSelectorOption } from '@/types/tenant'

const { t, locale } = useI18n()
const tenantStore = useTenantStore()

const { selectorOptions, selectedTenantId, selectedTenant } =
  storeToRefs(tenantStore)

/** Dropdown state */
const isOpen = ref(false)
const searchText = ref('')

/** Filtered options */
const filteredOptions = computed(() => {
  if (!searchText.value) return selectorOptions.value
  const query = searchText.value.toLowerCase()
  return selectorOptions.value.filter(
    (t) =>
      t.nameAr.toLowerCase().includes(query) ||
      t.nameEn.toLowerCase().includes(query) ||
      t.identifier.toLowerCase().includes(query),
  )
})

/** Get display name */
function getDisplayName(option: TenantSelectorOption): string {
  return locale.value === 'ar' ? option.nameAr : option.nameEn
}

/** Get status color */
function getStatusDot(status: TenantStatus): string {
  switch (status) {
    case TenantStatus.Active:
      return 'bg-emerald-500'
    case TenantStatus.Suspended:
      return 'bg-orange-500'
    case TenantStatus.PendingProvisioning:
      return 'bg-amber-500'
    case TenantStatus.Cancelled:
      return 'bg-red-500'
    default:
      return 'bg-slate-400'
  }
}

/** Select a tenant */
function selectTenant(option: TenantSelectorOption) {
  tenantStore.selectTenant(option.id)
  isOpen.value = false
  searchText.value = ''
}

/** Clear selection (view all tenants mode) */
function clearSelection() {
  tenantStore.clearTenantSelection()
  isOpen.value = false
  searchText.value = ''
}

/** Close dropdown on outside click */
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (!target.closest('.tenant-selector')) {
    isOpen.value = false
  }
}

onMounted(() => {
  tenantStore.loadSelectorOptions()
  document.addEventListener('click', handleClickOutside)
})

/** Cleanup */
import { onUnmounted } from 'vue'
onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div class="tenant-selector relative">
    <!-- Trigger Button -->
    <button
      type="button"
      class="flex items-center gap-2 rounded-lg border border-surface-dim bg-white px-3 py-2 text-sm transition-colors hover:bg-surface-muted"
      @click.stop="isOpen = !isOpen"
    >
      <i class="pi pi-building text-sm text-primary"></i>

      <template v-if="selectedTenant">
        <span class="h-2 w-2 rounded-full" :class="getStatusDot(selectedTenant.status)"></span>
        <span class="max-w-[180px] truncate text-sm font-medium text-secondary">
          {{ getDisplayName(selectedTenant) }}
        </span>
      </template>
      <template v-else>
        <span class="text-sm text-tertiary">
          {{ t('tenantSelector.placeholder') }}
        </span>
      </template>

      <i
        class="pi text-xs text-tertiary transition-transform"
        :class="isOpen ? 'pi-chevron-up' : 'pi-chevron-down'"
      ></i>
    </button>

    <!-- Dropdown -->
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="scale-95 opacity-0"
      enter-to-class="scale-100 opacity-100"
      leave-active-class="transition duration-100 ease-in"
      leave-from-class="scale-100 opacity-100"
      leave-to-class="scale-95 opacity-0"
    >
      <div
        v-if="isOpen"
        class="absolute start-0 top-full z-50 mt-1 w-80 rounded-lg border border-surface-dim bg-white shadow-xl"
      >
        <!-- Search -->
        <div class="border-b border-surface-dim p-3">
          <div class="relative">
            <i class="pi pi-search pointer-events-none absolute inset-y-0 start-0 flex items-center ps-3 text-xs text-tertiary"></i>
            <input
              v-model="searchText"
              type="text"
              :placeholder="t('tenantSelector.searchPlaceholder')"
              class="w-full rounded-md border border-surface-dim py-1.5 pe-3 ps-8 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary/20"
              @click.stop
            />
          </div>
        </div>

        <!-- All tenants option -->
        <div class="border-b border-surface-dim">
          <button
            type="button"
            class="flex w-full items-center gap-2 px-4 py-2.5 text-sm transition-colors hover:bg-surface-muted"
            :class="{ 'bg-primary/5 font-medium text-primary': !selectedTenantId }"
            @click="clearSelection"
          >
            <i class="pi pi-th-large text-xs"></i>
            {{ t('tenantSelector.allTenants') }}
          </button>
        </div>

        <!-- Options list -->
        <div class="max-h-[280px] overflow-y-auto py-1">
          <button
            v-for="option in filteredOptions"
            :key="option.id"
            type="button"
            class="flex w-full items-center gap-3 px-4 py-2.5 text-start transition-colors hover:bg-surface-muted"
            :class="{ 'bg-primary/5': selectedTenantId === option.id }"
            @click="selectTenant(option)"
          >
            <span class="h-2 w-2 shrink-0 rounded-full" :class="getStatusDot(option.status)"></span>
            <div class="min-w-0 flex-1">
              <p
                class="truncate text-sm"
                :class="selectedTenantId === option.id ? 'font-medium text-primary' : 'text-secondary'"
              >
                {{ getDisplayName(option) }}
              </p>
              <p class="text-[10px] font-mono text-tertiary" dir="ltr">
                {{ option.identifier }}
              </p>
            </div>
            <i
              v-if="selectedTenantId === option.id"
              class="pi pi-check text-xs text-primary"
            ></i>
          </button>

          <!-- No results -->
          <div
            v-if="filteredOptions.length === 0"
            class="px-4 py-6 text-center text-sm text-tertiary"
          >
            {{ t('tenantSelector.noResults') }}
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>
