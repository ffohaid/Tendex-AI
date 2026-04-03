import { ref, computed } from 'vue'
import type { NavigationItem } from '@/types/navigation'
import { useAuthStore } from '@/stores/auth'

/**
 * Role name aliases to support both old and new naming conventions.
 *
 * The database may contain either the old names (e.g., "Tenant Owner")
 * or the new names (e.g., "Tenant Primary Admin"). This mapping ensures
 * the sidebar works correctly regardless of which naming convention is
 * stored in the database.
 */
const OPERATOR_ADMIN_ALIASES = [
  'OperatorPrimaryAdmin',
  'Operator Super Admin',
  'OperatorSuperAdmin',
  'SuperAdmin',
  'Super Admin',
]

const TENANT_ADMIN_ALIASES = [
  'TenantPrimaryAdmin',
  'Tenant Primary Admin',
  'Tenant Owner',
]

/**
 * Composable for managing sidebar navigation state.
 *
 * Governance Role Hierarchy:
 * ─────────────────────────────────────────────────────────────────
 * 1. OperatorSuperAdmin    → Sees ONLY the operator panel
 * 2. TenantPrimaryAdmin    → Sees everything within their tenant
 *                            (bypasses all permission checks)
 * 3. All other roles       → Controlled by permission matrix
 *
 * Implements:
 * - Accordion behavior: when a parent menu item is expanded,
 *   all other expanded parent items are automatically collapsed.
 * - Permission-based filtering: items with `permission` or `requiredRoles`
 *   are hidden if the user lacks the required access.
 */
export function useSidebarNavigation(items: NavigationItem[]) {
  const authStore = useAuthStore()

  /** Key of the currently expanded parent menu item (null = all collapsed) */
  const expandedKey = ref<string | null>(null)

  /**
   * Check if the current user is the Operator Primary Admin.
   * This user sees ONLY the operator panel.
   */
  const isOperatorAdmin = computed(() => {
    return authStore.userRoles.some(r => OPERATOR_ADMIN_ALIASES.includes(r))
  })

  /**
   * Check if the current user is the Tenant Primary Admin.
   * This user bypasses all permission checks within their tenant.
   */
  const isTenantAdmin = computed(() => {
    return authStore.userRoles.some(r => TENANT_ADMIN_ALIASES.includes(r))
  })

  /**
   * Check if a required role matches any of the operator admin aliases.
   */
  function isOperatorRole(role: string): boolean {
    return OPERATOR_ADMIN_ALIASES.includes(role)
  }


  /**
   * Check if a single navigation item is accessible to the current user.
   */
  function isItemAccessible(item: NavigationItem): boolean {
    // ── Operator Primary Admin: ONLY sees operator panel ──
    if (isOperatorAdmin.value) {
      return item.key === 'operator' ||
        (item.requiredRoles?.some(r => isOperatorRole(r)) ?? false)
    }

    // ── Tenant Primary Admin: sees everything EXCEPT operator panel ──
    if (isTenantAdmin.value) {
      // Block operator panel for tenant admins
      if (item.requiredRoles?.some(r => isOperatorRole(r))) return false
      return true
    }

    // ── Regular users: check requiredRoles and permissions ──

    // If item requires specific roles, check them
    if (item.requiredRoles && item.requiredRoles.length > 0) {
      const hasRole = item.requiredRoles.some((r: string) => authStore.hasRole(r))
      if (!hasRole) return false
    }

    // If item requires a permission, check it
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
    isOperatorAdmin,
    isTenantAdmin,
    toggleExpand,
    isExpanded,
    collapseAll,
  }
}
