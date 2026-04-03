import { ref, computed } from 'vue'
import type { NavigationItem } from '@/types/navigation'
import { useAuthStore } from '@/stores/auth'

/**
 * Composable for managing sidebar navigation state.
 *
 * Implements:
 * - Accordion behavior: when a parent menu item is expanded,
 *   all other expanded parent items are automatically collapsed.
 * - Permission-based filtering: items with `permission` or `requiredRoles`
 *   are hidden if the user lacks the required access.
 * - Owner and Admin users bypass all permission checks.
 */
export function useSidebarNavigation(items: NavigationItem[]) {
  const authStore = useAuthStore()

  /** Key of the currently expanded parent menu item (null = all collapsed) */
  const expandedKey = ref<string | null>(null)

  /**
   * Check if the current user is privileged (Owner/Admin) and bypasses checks.
   */
  const isPrivileged = computed(() => {
    const roles = authStore.userRoles
    return roles.some((r: string) =>
      ['Owner', 'Tenant Owner', 'System Administrator', 'Super Admin', 'SuperAdmin', 'Operator', 'مدير النظام'].includes(r)
    )
  })

  /**
   * Check if a single navigation item is accessible to the current user.
   */
  function isItemAccessible(item: NavigationItem): boolean {
    // Privileged users can see everything
    if (isPrivileged.value) return true

    // Check requiredRoles
    if (item.requiredRoles && item.requiredRoles.length > 0) {
      const hasRole = item.requiredRoles.some((r: string) => authStore.hasRole(r))
      if (!hasRole) return false
    }

    // Check permission
    if (item.permission) {
      if (!authStore.hasPermission(item.permission)) return false
    }

    return true
  }

  /**
   * Recursively filter navigation items based on user permissions.
   * Parent items with children are only shown if at least one child is accessible.
   */
  function filterItems(navItems: NavigationItem[]): NavigationItem[] {
    return navItems
      .filter(item => isItemAccessible(item))
      .map(item => {
        if (item.children && item.children.length > 0) {
          const filteredChildren = filterItems(item.children)
          // Only show parent if it has at least one accessible child
          if (filteredChildren.length === 0) return null
          return { ...item, children: filteredChildren }
        }
        return item
      })
      .filter((item): item is NavigationItem => item !== null)
  }

  /** Filtered navigation items based on user permissions */
  const filteredNavigation = computed<NavigationItem[]>(() => {
    return filterItems(items)
  })

  /** Flat list of all filtered navigation items for search/filter purposes */
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
    flatten(filteredNavigation.value)
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
    filteredNavigation,
    toggleExpand,
    isExpanded,
    collapseAll,
  }
}
