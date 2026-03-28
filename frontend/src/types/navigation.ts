/**
 * Represents a single navigation menu item in the sidebar.
 */
export interface NavigationItem {
  /** Unique key for the menu item */
  key: string
  /** i18n translation key for the label */
  labelKey: string
  /** PrimeIcons icon class name */
  icon: string
  /** Vue Router route name or path */
  route?: string
  /** Child navigation items (sub-menu) */
  children?: NavigationItem[]
  /** Required permission key (for future RBAC integration) */
  permission?: string
  /** Badge count (for notifications, tasks, etc.) */
  badge?: number
}
