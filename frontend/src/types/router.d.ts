import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** Page title shown in the browser tab */
    title?: string
    /** Whether authentication is required */
    requiresAuth?: boolean
    /** Whether the route is for guests only (unauthenticated users) */
    guest?: boolean
  }
}
