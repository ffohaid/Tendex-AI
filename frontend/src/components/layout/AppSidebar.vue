<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useAppStore } from '@/stores/app'
import { computed } from 'vue'
import { sidebarNavigation } from '@/config/navigation'
import { useSidebarNavigation } from '@/composables/useSidebarNavigation'
import SidebarMenuItem from './SidebarMenuItem.vue'

const { t } = useI18n()
const appStore = useAppStore()

const isCollapsed = computed(() => appStore.sidebarCollapsed)

const { toggleExpand, isExpanded } = useSidebarNavigation(sidebarNavigation)
</script>

<template>
  <aside
    class="fixed top-16 bottom-0 z-40 flex flex-col border-e border-secondary-dark bg-secondary transition-all duration-300"
    :class="[isCollapsed ? 'w-16' : 'w-64']"
    :style="{ insetInlineStart: '0' }"
  >
    <!-- Navigation menu -->
    <nav class="flex-1 overflow-y-auto px-2 py-4" :aria-label="t('nav.dashboard')">
      <ul class="space-y-0.5">
        <SidebarMenuItem
          v-for="item in sidebarNavigation"
          :key="item.key"
          :item="item"
          :is-expanded="isExpanded(item.key)"
          :is-collapsed="isCollapsed"
          @toggle="toggleExpand"
        />
      </ul>
    </nav>

    <!-- Sidebar footer: Collapse toggle -->
    <div class="border-t border-secondary-dark p-2">
      <button
        type="button"
        class="flex w-full items-center justify-center gap-2 rounded-lg px-3 py-2.5 text-sm text-surface-dim transition-colors hover:bg-white/10 hover:text-white"
        :aria-label="isCollapsed ? t('sidebar.expand') : t('sidebar.collapse')"
        @click="appStore.toggleSidebar()"
      >
        <i
          class="pi text-sm transition-transform duration-200"
          :class="[
            isCollapsed
              ? 'pi-angle-double-right rtl:pi-angle-double-left'
              : 'pi-angle-double-left rtl:pi-angle-double-right',
          ]"
        ></i>
        <span v-if="!isCollapsed" class="text-xs">
          {{ t('sidebar.collapse') }}
        </span>
      </button>
    </div>
  </aside>
</template>
