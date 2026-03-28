import { ref, computed } from 'vue'
import type { NavigationItem } from '@/types/navigation'

/**
 * Composable for managing sidebar navigation state.
 *
 * Implements accordion behavior: when a parent menu item is expanded,
 * all other expanded parent items are automatically collapsed.
 */
export function useSidebarNavigation(items: NavigationItem[]) {
  /** Key of the currently expanded parent menu item (null = all collapsed) */
  const expandedKey = ref<string | null>(null)

  /** Flat list of all navigation items for search/filter purposes */
  const flatItems = computed<NavigationItem[]>(() => {
    const result: NavigationItem[] = []
    function flatten(list: NavigationItem[]): void {
      for (const item of list) {
        result.push(item)
        if (item.children?.length) {
          flatten(item.children)
        }
      }
    }
    flatten(items)
    return result
  })

  /**
   * Toggles the expanded state of a parent menu item.
   * If the item is already expanded, it collapses it.
   * If another item is expanded, it collapses that one first (accordion behavior).
   */
  function toggleExpand(key: string): void {
    expandedKey.value = expandedKey.value === key ? null : key
  }

  /**
   * Checks whether a given menu item is currently expanded.
   */
  function isExpanded(key: string): boolean {
    return expandedKey.value === key
  }

  /**
   * Collapses all expanded menu items.
   */
  function collapseAll(): void {
    expandedKey.value = null
  }

  return {
    expandedKey,
    flatItems,
    toggleExpand,
    isExpanded,
    collapseAll,
  }
}
