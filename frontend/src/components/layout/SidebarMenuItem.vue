<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { computed } from 'vue'
import type { NavigationItem } from '@/types/navigation'

const props = defineProps<{
  /** The navigation item to render */
  item: NavigationItem
  /** Whether the parent menu is expanded (for items with children) */
  isExpanded: boolean
  /** Whether the sidebar is collapsed */
  isCollapsed: boolean
}>()

const emit = defineEmits<{
  /** Emitted when a parent item is clicked to toggle expansion */
  toggle: [key: string]
}>()

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const hasChildren = computed(() => !!props.item.children?.length)

/**
 * Checks if the current route matches this item or any of its children.
 */
const isActive = computed(() => {
  if (props.item.route) {
    return route.name === props.item.route
  }
  if (props.item.children) {
    return props.item.children.some((child) => route.name === child.route)
  }
  return false
})

/**
 * Handles click on a menu item.
 * If it has children, toggles expansion.
 * If it has a route, navigates to it.
 */
function handleClick(): void {
  if (hasChildren.value) {
    emit('toggle', props.item.key)
  } else if (props.item.route) {
    router.push({ name: props.item.route })
  }
}

/**
 * Navigates to a child route.
 */
function navigateToChild(child: NavigationItem): void {
  if (child.route) {
    router.push({ name: child.route })
  }
}
</script>

<template>
  <li class="mb-1">
    <!-- Parent / Leaf item button -->
    <button
      type="button"
      class="group flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-all duration-200"
      :class="[
        isActive
          ? 'bg-primary/10 text-primary'
          : 'text-surface hover:bg-white/10 hover:text-white',
      ]"
      :title="isCollapsed ? t(item.labelKey) : undefined"
      @click="handleClick"
    >
      <!-- Icon -->
      <i
        :class="[
          item.icon,
          'text-base transition-colors',
          isActive ? 'text-primary' : 'text-surface-dim group-hover:text-white',
        ]"
      ></i>

      <!-- Label (hidden when sidebar is collapsed) -->
      <span
        v-if="!isCollapsed"
        class="flex-1 truncate text-start"
      >
        {{ t(item.labelKey) }}
      </span>

      <!-- Badge -->
      <span
        v-if="item.badge && !isCollapsed"
        class="inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-bold text-white"
      >
        {{ item.badge }}
      </span>

      <!-- Expand/Collapse chevron for parent items -->
      <i
        v-if="hasChildren && !isCollapsed"
        class="pi text-xs transition-transform duration-200"
        :class="[
          isExpanded ? 'pi-chevron-down' : 'pi-chevron-left',
          isActive ? 'text-primary' : 'text-surface-dim',
        ]"
        :style="{ transform: isExpanded ? 'none' : (undefined) }"
      ></i>
    </button>

    <!-- Children sub-menu (animated) -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="max-h-0 opacity-0"
      enter-to-class="max-h-96 opacity-100"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="max-h-96 opacity-100"
      leave-to-class="max-h-0 opacity-0"
    >
      <ul
        v-if="hasChildren && isExpanded && !isCollapsed"
        class="mt-1 overflow-hidden ps-4"
      >
        <li
          v-for="child in item.children"
          :key="child.key"
          class="mb-0.5"
        >
          <button
            type="button"
            class="group flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-all duration-150"
            :class="[
              route.name === child.route
                ? 'bg-primary/10 font-medium text-primary'
                : 'text-surface-dim hover:bg-white/5 hover:text-white',
            ]"
            @click="navigateToChild(child)"
          >
            <i
              :class="[
                child.icon,
                'text-sm',
                route.name === child.route
                  ? 'text-primary'
                  : 'text-surface-dim group-hover:text-white',
              ]"
            ></i>
            <span class="truncate">{{ t(child.labelKey) }}</span>
          </button>
        </li>
      </ul>
    </Transition>
  </li>
</template>
