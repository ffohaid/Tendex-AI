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
  <li class="mb-0.5">
    <!-- Parent / Leaf item button -->
    <button
      type="button"
      class="group relative flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-all duration-200"
      :class="[
        isActive
          ? 'bg-white/10 text-white'
          : item.key === 'ai-assistant'
            ? 'text-ai-200 hover:bg-ai/10 hover:text-white'
            : 'text-secondary-400 hover:bg-white/5 hover:text-white',
      ]"
      :title="isCollapsed ? t(item.labelKey) : undefined"
      @click="handleClick"
    >
      <!-- Active indicator bar -->
      <span
        v-if="isActive"
        class="absolute inset-y-1 rounded-full bg-primary"
        :style="{ insetInlineStart: '-0.5rem', width: '3px' }"
      ></span>

      <!-- Icon -->
      <span
        class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg transition-colors"
        :class="[
          isActive
            ? 'bg-primary/20 text-primary'
            : item.key === 'ai-assistant'
              ? 'bg-ai/10 text-ai-light'
              : 'text-secondary-400 group-hover:text-white',
        ]"
      >
        <i :class="[item.icon, 'text-base']" />
      </span>

      <!-- Label (hidden when sidebar is collapsed) -->
      <span
        v-if="!isCollapsed"
        class="flex-1 text-start leading-snug"
      >
        {{ t(item.labelKey) }}
      </span>

      <!-- AI sparkle badge for AI assistant -->
      <span
        v-if="item.key === 'ai-assistant' && !isCollapsed"
        class="rounded-md bg-ai/20 px-1.5 py-0.5 text-[10px] font-bold text-ai-200"
      >
        AI
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
          isActive ? 'text-white' : 'text-secondary-500',
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
        class="mt-1 overflow-hidden ps-6"
      >
        <li
          v-for="child in item.children"
          :key="child.key"
          class="mb-0.5"
        >
          <button
            type="button"
            class="group relative flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-all duration-150"
            :class="[
              route.name === child.route
                ? 'bg-white/10 font-medium text-white'
                : 'text-secondary-400 hover:bg-white/5 hover:text-secondary-200',
            ]"
            @click="navigateToChild(child)"
          >
            <!-- Active dot for child -->
            <span
              v-if="route.name === child.route"
              class="absolute h-1.5 w-1.5 rounded-full bg-primary"
              :style="{ insetInlineStart: '-0.75rem' }"
            ></span>
            <i
              :class="[
                child.icon,
                'text-sm',
                route.name === child.route
                  ? 'text-primary'
                  : 'text-secondary-500 group-hover:text-secondary-300',
              ]"
            ></i>
            <span class="leading-snug">{{ t(child.labelKey) }}</span>
          </button>
        </li>
      </ul>
    </Transition>
  </li>
</template>
