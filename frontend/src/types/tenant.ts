/**
 * TypeScript type definitions for Tenant Management, Subscriptions,
 * and Purchase Order (PO) lifecycle in the Super Admin portal.
 *
 * These types mirror the backend DTOs from TendexAI.Application.Features.Tenants.Dtos
 * and extend them with PO/subscription management models per PRD TASK-601.
 */

/* ------------------------------------------------------------------ */
/*  Tenant Status Enum                                                 */
/* ------------------------------------------------------------------ */

export enum TenantStatus {
  PendingProvisioning = 0,
  Active = 1,
  Suspended = 2,
  Cancelled = 3,
  Archived = 4,
}

/* ------------------------------------------------------------------ */
/*  Tenant DTOs (matching backend)                                     */
/* ------------------------------------------------------------------ */

/** Full tenant detail DTO — mirrors TenantDto from backend. */
export interface TenantDto {
  id: string
  nameAr: string
  nameEn: string
  identifier: string
  subdomain: string
  databaseName: string
  isProvisioned: boolean
  provisionedAt: string | null
  status: TenantStatus
  statusName: string
  subscriptionExpiresAt: string | null
  logoUrl: string | null
  primaryColor: string | null
  secondaryColor: string | null
  contactPersonName: string | null
  contactPersonEmail: string | null
  contactPersonPhone: string | null
  notes: string | null
  createdAt: string
  createdBy: string | null
  lastModifiedAt: string | null
}

/** Lightweight tenant list item DTO — mirrors TenantListItemDto. */
export interface TenantListItemDto {
  id: string
  nameAr: string
  nameEn: string
  identifier: string
  subdomain: string
  status: TenantStatus
  statusName: string
  isProvisioned: boolean
  subscriptionExpiresAt: string | null
  createdAt: string
}

/** Paginated response wrapper — mirrors PagedResultDto<T>. */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

/** Tenant status option for dropdowns. */
export interface TenantStatusOption {
  value: number
  name: string
}

/* ------------------------------------------------------------------ */
/*  Tenant Request DTOs                                                */
/* ------------------------------------------------------------------ */

/** Request to create a new tenant — mirrors CreateTenantRequest. */
export interface CreateTenantRequest {
  nameAr: string
  nameEn: string
  identifier: string
  subdomain: string
  contactPersonName?: string
  contactPersonEmail?: string
  contactPersonPhone?: string
  notes?: string
  logoUrl?: string
  primaryColor?: string
  secondaryColor?: string
}

/** Request to update tenant info — mirrors UpdateTenantRequest. */
export interface UpdateTenantRequest {
  nameAr: string
  nameEn: string
  contactPersonName?: string
  contactPersonEmail?: string
  contactPersonPhone?: string
  notes?: string
}

/** Request to change tenant status — mirrors ChangeTenantStatusRequest. */
export interface ChangeTenantStatusRequest {
  newStatus: TenantStatus
}

/** Request to update tenant branding — mirrors UpdateTenantBrandingRequest. */
export interface UpdateTenantBrandingRequest {
  logoUrl?: string
  primaryColor?: string
  secondaryColor?: string
}

/* ------------------------------------------------------------------ */
/*  Purchase Order (PO) Types                                          */
/* ------------------------------------------------------------------ */

/** PO lifecycle stages per PRD section 3.2.1. */
export enum PoStatus {
  Received = 0,
  EnvironmentSetup = 1,
  Training = 2,
  FinalAcceptance = 3,
  ActiveOperation = 4,
  RenewalWindow = 5,
  Renewed = 6,
  Cancelled = 7,
}

/** Subscription plan tiers. */
export enum SubscriptionPlan {
  Basic = 'basic',
  Professional = 'professional',
  Enterprise = 'enterprise',
}

/** Full PO detail DTO. */
export interface PurchaseOrderDto {
  id: string
  tenantId: string
  tenantNameAr: string
  tenantNameEn: string
  poNumber: string
  totalAmount: number
  subscriptionPlan: SubscriptionPlan
  poDateGregorian: string
  poDateHijri: string
  durationMonths: number
  subscriptionStartDate: string | null
  subscriptionEndDate: string | null
  poDocumentUrl: string | null
  contactPersonName: string | null
  contactPersonEmail: string | null
  contactPersonPhone: string | null
  notes: string | null
  status: PoStatus
  statusName: string
  createdAt: string
  createdBy: string | null
  lastModifiedAt: string | null
}

/** Lightweight PO list item. */
export interface PurchaseOrderListItemDto {
  id: string
  tenantId: string
  tenantNameAr: string
  tenantNameEn: string
  poNumber: string
  totalAmount: number
  subscriptionPlan: SubscriptionPlan
  status: PoStatus
  statusName: string
  subscriptionStartDate: string | null
  subscriptionEndDate: string | null
  daysUntilExpiry: number | null
  createdAt: string
}

/** Request to create a new PO. */
export interface CreatePurchaseOrderRequest {
  tenantId: string
  poNumber: string
  totalAmount: number
  subscriptionPlan: SubscriptionPlan
  poDateGregorian: string
  durationMonths: number
  contactPersonName?: string
  contactPersonEmail?: string
  contactPersonPhone?: string
  notes?: string
}

/** Request to update PO status. */
export interface ChangePurchaseOrderStatusRequest {
  newStatus: PoStatus
  subscriptionStartDate?: string
  notes?: string
}

/* ------------------------------------------------------------------ */
/*  Renewal Alert Types                                                */
/* ------------------------------------------------------------------ */

/** Renewal alert severity levels per PRD section 3.2.3. */
export enum RenewalAlertSeverity {
  Info = 'info',
  Warning = 'warning',
  Critical = 'critical',
  Expired = 'expired',
}

/** Renewal alert item for the early warning system. */
export interface RenewalAlertDto {
  tenantId: string
  tenantNameAr: string
  tenantNameEn: string
  poId: string
  poNumber: string
  subscriptionEndDate: string
  daysUntilExpiry: number
  severity: RenewalAlertSeverity
  contactPersonName: string | null
  contactPersonEmail: string | null
}

/* ------------------------------------------------------------------ */
/*  Tenant Selector for Super Admin                                    */
/* ------------------------------------------------------------------ */

/** Lightweight tenant option for the Super Admin tenant selector. */
export interface TenantSelectorOption {
  id: string
  nameAr: string
  nameEn: string
  identifier: string
  status: TenantStatus
  statusName: string
}

/* ------------------------------------------------------------------ */
/*  Query Parameters                                                   */
/* ------------------------------------------------------------------ */

/** Query parameters for tenant list endpoint. */
export interface TenantListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: TenantStatus
}

/** Query parameters for PO list endpoint. */
export interface PurchaseOrderListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: PoStatus
  tenantId?: string
}
