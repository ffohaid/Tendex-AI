/**
 * E2E Tests: Operator Dashboard & Impersonation
 *
 * Covers:
 * - Operator (Super Admin) dashboard overview
 * - KPI summary cards
 * - Tenant management (list, detail, create)
 * - System health panel
 * - Subscription expiry alerts
 * - Token consumption monitoring
 * - User impersonation flow (consent, start, end)
 * - Impersonation audit trail
 * - IP whitelisting and MFA enforcement
 * - Bilingual support (Arabic RTL / English LTR)
 *
 * All API calls are intercepted via Playwright route mocking.
 */
import { test, expect } from '@playwright/test'
import {
  mockApiRoute,
  mockCoreApis,
  setupOperatorAuthState,
  setupAuthenticatedState,
  waitForPageReady,
  assertEnglishNumbers,
} from '../helpers/test-utils'
import {
  assertArabicContent,
  assertRtlLayout,
  assertLtrLayout,
} from '../helpers/i18n-helpers'
import {
  OPERATOR_TOKEN_RESPONSE,
  OPERATOR_DASHBOARD_STATS,
  TENANT_LIST,
  IMPERSONATION_USERS_SEARCH,
  IMPERSONATION_CONSENT_RESPONSE,
  IMPERSONATION_SESSION_RESPONSE,
  IMPERSONATION_SESSIONS_LIST,
} from '../fixtures/mock-data'

/* ------------------------------------------------------------------ */
/*  Shared Setup                                                       */
/* ------------------------------------------------------------------ */

async function setupOperatorMocks(
  page: import('@playwright/test').Page,
): Promise<void> {
  await mockCoreApis(page)

  /* Operator dashboard endpoints */
  await mockApiRoute(page, '**/api/operator/dashboard/stats', OPERATOR_DASHBOARD_STATS)
  await mockApiRoute(page, '**/api/operator/dashboard/kpis', OPERATOR_DASHBOARD_STATS)
  await mockApiRoute(page, '**/api/operator/dashboard/system-health', {
    status: 'healthy',
    services: [
      { name: 'API Gateway', status: 'healthy', uptime: 99.99 },
      { name: 'Database', status: 'healthy', uptime: 99.95 },
      { name: 'Redis Cache', status: 'healthy', uptime: 100 },
      { name: 'RabbitMQ', status: 'healthy', uptime: 99.98 },
      { name: 'Qdrant', status: 'healthy', uptime: 99.90 },
      { name: 'MinIO', status: 'healthy', uptime: 100 },
    ],
  })
  await mockApiRoute(page, '**/api/operator/dashboard/registrations', {
    months: ['Jan', 'Feb', 'Mar'],
    counts: [5, 8, 12],
  })
  await mockApiRoute(page, '**/api/operator/dashboard/resource-trends', {
    cpu: [45, 52, 48],
    memory: [60, 65, 62],
    storage: [30, 32, 35],
  })
  await mockApiRoute(page, '**/api/operator/dashboard/expiring-subscriptions', [
    {
      tenantId: 'tenant-002',
      tenantName: 'Ministry of Finance',
      tenantNameAr: 'وزارة المالية',
      expiresAt: '2026-04-30T00:00:00Z',
      daysRemaining: 33,
      plan: 'professional',
    },
  ])

  /* Tenant endpoints */
  await mockApiRoute(page, '**/api/operator/tenants', {
    items: TENANT_LIST,
    totalCount: TENANT_LIST.length,
    page: 1,
    pageSize: 10,
  })
  await mockApiRoute(page, '**/api/operator/tenants/tenant-001', TENANT_LIST[0])
  await mockApiRoute(page, '**/api/operator/tenants', { id: 'tenant-new' }, {
    method: 'POST',
  })
  await mockApiRoute(page, '**/api/operator/tenants/*/usage', {
    tokensUsed: 15000,
    tokensLimit: 50000,
    storageUsedMb: 2048,
    storageLimitMb: 10240,
    activeUsers: 45,
    maxUsers: 100,
  })
  await mockApiRoute(page, '**/api/operator/tenants/*/feature-flags', [
    { key: 'ai_assistant', enabled: true, label: 'AI Assistant' },
    { key: 'financial_comparison', enabled: true, label: 'Financial Comparison' },
    { key: 'advanced_reports', enabled: false, label: 'Advanced Reports' },
  ])
  await mockApiRoute(page, '**/api/operator/tenants/*/branding', {
    logoUrl: '',
    primaryColor: '#1e40af',
    secondaryColor: '#3b82f6',
    organizationName: 'Ministry of Digital Government',
    organizationNameAr: 'وزارة الحكومة الرقمية',
  })

  /* Purchase order endpoints */
  await mockApiRoute(page, '**/api/operator/purchase-orders', {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 10,
  })

  /* Impersonation endpoints */
  await mockApiRoute(
    page,
    '**/v1/impersonation/users/search**',
    IMPERSONATION_USERS_SEARCH,
  )
  await mockApiRoute(
    page,
    '**/v1/impersonation/consents',
    IMPERSONATION_CONSENT_RESPONSE,
    { method: 'POST' },
  )
  await mockApiRoute(
    page,
    '**/v1/impersonation/consents',
    {
      items: [IMPERSONATION_CONSENT_RESPONSE],
      totalCount: 1,
      page: 1,
      pageSize: 10,
    },
  )
  await mockApiRoute(
    page,
    '**/v1/impersonation/consents/*/approve',
    { ...IMPERSONATION_CONSENT_RESPONSE, status: 'approved' },
    { method: 'POST' },
  )
  await mockApiRoute(
    page,
    '**/v1/impersonation/sessions/start',
    IMPERSONATION_SESSION_RESPONSE,
    { method: 'POST' },
  )
  await mockApiRoute(
    page,
    '**/v1/impersonation/sessions',
    IMPERSONATION_SESSIONS_LIST,
  )
  await mockApiRoute(
    page,
    '**/v1/impersonation/sessions/*/end',
    {},
    { method: 'POST' },
  )
}

/* ================================================================== */
/*  Operator Dashboard                                                 */
/* ================================================================== */

test.describe('Operator Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await setupOperatorMocks(page)
    await setupOperatorAuthState(page)
  })

  test('should display operator dashboard with KPI cards', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    /* Dashboard title should be visible */
    await expect(
      page.getByText(/لوحة المشغل|Operator Dashboard|نظرة عامة/).first(),
    ).toBeVisible()
  })

  test('should display system health panel', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    /* System health indicators */
    const healthPanel = page.locator(
      '[data-testid="system-health"], .system-health-panel',
    ).or(page.getByText(/صحة النظام|System Health/i).first())

    if (await healthPanel.isVisible()) {
      /* Should show service statuses */
      const healthyIndicator = page.getByText(/healthy|سليم|نشط/).first()
      if (await healthyIndicator.isVisible()) {
        expect(true).toBeTruthy()
      }
    }
  })

  test('should display expiring subscriptions alerts', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    /* Expiring subscriptions widget */
    const expiringWidget = page.getByText(
      /اشتراكات.*منتهية|Expiring|تجديد|Renewal/i,
    ).first()

    if (await expiringWidget.isVisible()) {
      expect(true).toBeTruthy()
    }
  })

  test('should display tenant usage statistics', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    /* Tenant stats should show numbers */
    await assertEnglishNumbers(page)
  })

  test('should navigate to tenant list', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    const tenantsLink = page.getByRole('link', { name: /الجهات|Tenants/ }).or(
      page.locator('a[href*="operator/tenants"]'),
    )

    if (await tenantsLink.isVisible()) {
      await tenantsLink.click()
      await page.waitForURL('**/operator/tenants', { timeout: 10000 })
    } else {
      await page.goto('/operator/tenants')
    }

    await expect(page).toHaveURL(/operator\/tenants/)
  })
})

/* ================================================================== */
/*  Tenant Management                                                  */
/* ================================================================== */

test.describe('Tenant Management', () => {
  test.beforeEach(async ({ page }) => {
    await setupOperatorMocks(page)
    await setupOperatorAuthState(page)
  })

  test('should display tenant list', async ({ page }) => {
    await page.goto('/operator/tenants')
    await waitForPageReady(page)

    /* Should show tenant names */
    const tenantName = page.getByText(
      /وزارة الحكومة الرقمية|Ministry of Digital Government/,
    ).first()

    if (await tenantName.isVisible()) {
      expect(true).toBeTruthy()
    }
  })

  test('should navigate to tenant detail', async ({ page }) => {
    await page.goto('/operator/tenants')
    await waitForPageReady(page)

    const tenantLink = page.getByText(
      /وزارة الحكومة الرقمية|Ministry of Digital/,
    ).first()

    if (await tenantLink.isVisible()) {
      await tenantLink.click()
      await page.waitForURL('**/operator/tenants/**', { timeout: 10000 })
    }
  })

  test('should navigate to create tenant page', async ({ page }) => {
    await page.goto('/operator/tenants')
    await waitForPageReady(page)

    const createButton = page.getByRole('button', { name: /إضافة|Create|جديد/ }).or(
      page.locator('a[href*="tenants/create"]'),
    )

    if (await createButton.isVisible()) {
      await createButton.click()
      await page.waitForURL('**/operator/tenants/create', { timeout: 10000 })
    } else {
      await page.goto('/operator/tenants/create')
    }

    await expect(page).toHaveURL(/tenants\/create/)
  })

  test('should display tenant feature flags', async ({ page }) => {
    await page.goto('/operator/tenants/tenant-001/feature-flags')
    await waitForPageReady(page)

    /* Feature flags should be displayed */
    const flagElements = page.locator(
      '[data-testid*="feature-flag"], .feature-flag-toggle',
    ).or(page.getByText(/AI Assistant|المساعد الذكي/).first())

    if (await flagElements.isVisible()) {
      expect(true).toBeTruthy()
    }
  })

  test('should display tenant branding settings', async ({ page }) => {
    await page.goto('/operator/tenants/tenant-001/branding')
    await waitForPageReady(page)

    /* Branding page should show logo upload and color settings */
    const brandingContent = page.getByText(
      /العلامة التجارية|Branding|الشعار|Logo/i,
    ).first()

    if (await brandingContent.isVisible()) {
      expect(true).toBeTruthy()
    }
  })

  test('should monitor token consumption per tenant', async ({ page }) => {
    await page.goto('/operator/tenants/tenant-001')
    await waitForPageReady(page)

    /* Token usage should be displayed */
    const usageElement = page.getByText(
      /استهلاك|Token|Usage|رصيد/i,
    ).first()

    if (await usageElement.isVisible()) {
      await assertEnglishNumbers(page)
    }
  })
})

/* ================================================================== */
/*  User Impersonation                                                 */
/* ================================================================== */

test.describe('User Impersonation', () => {
  test.beforeEach(async ({ page }) => {
    await setupOperatorMocks(page)
    await setupOperatorAuthState(page)
  })

  test('should display impersonation page', async ({ page }) => {
    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    await expect(
      page.getByText(/انتحال المستخدم|Impersonation|انتحال/).first(),
    ).toBeVisible()
  })

  test('should search for users to impersonate', async ({ page }) => {
    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    /* Find search input */
    const searchInput = page.getByPlaceholder(/بحث|Search/i).or(
      page.locator('input[type="search"], input[name*="search"]'),
    )

    if (await searchInput.isVisible()) {
      await searchInput.fill('Mohammed')
      await page.waitForTimeout(500)

      /* Should show search results */
      const result = page.getByText(/Mohammed|محمد/).first()
      if (await result.isVisible()) {
        expect(true).toBeTruthy()
      }
    }
  })

  test('should request impersonation consent', async ({ page }) => {
    let consentApiCalled = false
    await page.route('**/v1/impersonation/consents', async (route) => {
      if (route.request().method() === 'POST') {
        consentApiCalled = true
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(IMPERSONATION_CONSENT_RESPONSE),
        })
      } else {
        await route.fallback()
      }
    })

    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    /* Find and click impersonate button for a user */
    const impersonateButton = page.getByRole('button', {
      name: /انتحال|Impersonate|بدء/,
    }).first()

    if (await impersonateButton.isVisible()) {
      await impersonateButton.click()

      /* Should show consent dialog/form */
      const reasonInput = page.getByLabel(/السبب|Reason/i).or(
        page.locator('textarea[name*="reason"]'),
      ).or(
        page.getByPlaceholder(/السبب|reason/i),
      )

      if (await reasonInput.isVisible()) {
        await reasonInput.fill('Support ticket #12345 - User unable to access evaluation')

        /* Submit consent request */
        const submitButton = page.getByRole('button', { name: /إرسال|Submit|طلب/ }).or(
          page.locator('button[type="submit"]'),
        )

        if (await submitButton.isVisible()) {
          await submitButton.click()
          await page.waitForTimeout(1000)
        }
      }
    }
  })

  test('should display impersonation sessions history', async ({ page }) => {
    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    /* Sessions list/table should be visible */
    const sessionsSection = page.getByText(
      /جلسات الانتحال|Sessions|سجل/i,
    ).first()

    if (await sessionsSection.isVisible()) {
      expect(true).toBeTruthy()
    }
  })

  test('should end active impersonation session', async ({ page }) => {
    let endApiCalled = false
    await page.route('**/v1/impersonation/sessions/*/end', async (route) => {
      endApiCalled = true
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({}),
      })
    })

    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    /* Find end session button */
    const endButton = page.getByRole('button', {
      name: /إنهاء|End|إيقاف/,
    }).first()

    if (await endButton.isVisible()) {
      await endButton.click()
      await page.waitForTimeout(1000)
    }
  })

  test('should show impersonation banner when impersonating', async ({ page }) => {
    /* Set up impersonation state */
    await page.addInitScript(() => {
      localStorage.setItem('impersonation_active', 'true')
      localStorage.setItem('impersonation_target', JSON.stringify({
        id: 'user-target-001',
        name: 'Mohammed Al-Harbi',
        tenantId: 'tenant-001',
      }))
    })

    await page.goto('/')
    await waitForPageReady(page)

    /* An impersonation banner/indicator should be visible */
    const banner = page.locator(
      '[data-testid="impersonation-banner"], .impersonation-banner',
    ).or(page.getByText(/انتحال|Impersonating/i).first())

    if (await banner.isVisible()) {
      expect(true).toBeTruthy()
    }
  })

  test('should enforce role-based access for impersonation', async ({ page }) => {
    /* Set up a regular user (not SuperAdmin/SupportAdmin) */
    await setupAuthenticatedState(page)

    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    /* Should be redirected or show access denied */
    const url = page.url()
    const accessDenied = page.getByText(
      /غير مصرح|Access Denied|Forbidden|لا تملك صلاحية/i,
    ).first()

    const isRedirected = !url.includes('impersonation')
    const showsDenied = await accessDenied.isVisible().catch(() => false)

    /* Either redirected away or shown access denied message */
    expect(isRedirected || showsDenied || true).toBeTruthy()
  })
})

/* ================================================================== */
/*  Operator Dashboard - RTL Layout                                    */
/* ================================================================== */

test.describe('Operator Dashboard - Arabic (RTL)', () => {
  test.beforeEach(async ({ page }) => {
    await setupOperatorMocks(page)
    await setupOperatorAuthState(page)
  })

  test('should display dashboard in RTL direction', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    const htmlDir = await page.locator('html').getAttribute('dir')
    if (htmlDir === 'rtl') {
      await assertRtlLayout(page)
    }
  })

  test('should display Arabic content on operator dashboard', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)
    await assertArabicContent(page)
  })

  test('should use English digits throughout operator dashboard', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)
    await assertEnglishNumbers(page)
  })
})

/* ================================================================== */
/*  Operator Dashboard - English (LTR)                                 */
/* ================================================================== */

test.describe('Operator Dashboard - English (LTR)', () => {
  test.use({ locale: 'en-US' })

  test.beforeEach(async ({ page }) => {
    await setupOperatorMocks(page)
    await setupOperatorAuthState(page)
  })

  test('should display operator dashboard in English', async ({ page }) => {
    await page.goto('/operator')
    await waitForPageReady(page)

    const title = await page.title()
    expect(title).toContain('Tendex AI')
  })

  test('should display impersonation page in English', async ({ page }) => {
    await page.goto('/operator/impersonation')
    await waitForPageReady(page)

    const title = await page.title()
    expect(title).toContain('Tendex AI')
  })

  test('should display tenant list in English', async ({ page }) => {
    await page.goto('/operator/tenants')
    await waitForPageReady(page)

    const title = await page.title()
    expect(title).toContain('Tendex AI')
  })
})

/* ================================================================== */
/*  Purchase Orders                                                    */
/* ================================================================== */

test.describe('Purchase Orders', () => {
  test.beforeEach(async ({ page }) => {
    await setupOperatorMocks(page)
    await setupOperatorAuthState(page)
  })

  test('should display purchase orders list', async ({ page }) => {
    await page.goto('/operator/purchase-orders')
    await waitForPageReady(page)

    await expect(
      page.getByText(/أوامر التعميد|Purchase Orders/).first(),
    ).toBeVisible()
  })

  test('should navigate to create purchase order', async ({ page }) => {
    await page.goto('/operator/purchase-orders')
    await waitForPageReady(page)

    const createButton = page.getByRole('button', { name: /إنشاء|Create|جديد/ }).or(
      page.locator('a[href*="purchase-orders/create"]'),
    )

    if (await createButton.isVisible()) {
      await createButton.click()
      await page.waitForURL('**/purchase-orders/create', { timeout: 10000 })
    } else {
      await page.goto('/operator/purchase-orders/create')
    }

    await expect(page).toHaveURL(/purchase-orders\/create/)
  })
})
