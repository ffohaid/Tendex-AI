/**
 * Shared E2E test utilities for Tendex AI.
 *
 * Provides reusable helpers for:
 * - API route mocking
 * - Authentication state setup
 * - Language / direction switching
 * - Common assertions
 */
import { type Page, type Route, expect } from '@playwright/test'
import {
  AUTH_TOKEN_RESPONSE,
  OPERATOR_TOKEN_RESPONSE,
  DASHBOARD_STATS,
  DASHBOARD_COMPETITIONS,
  DASHBOARD_TASKS,
} from '../fixtures/mock-data'

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

type SupportedLocale = 'ar' | 'en'

/* ------------------------------------------------------------------ */
/*  API Mocking Helpers                                                */
/* ------------------------------------------------------------------ */

/**
 * Intercept an API route and respond with mock JSON data.
 */
export async function mockApiRoute(
  page: Page,
  urlPattern: string | RegExp,
  responseBody: unknown,
  options: { status?: number; method?: string } = {},
): Promise<void> {
  const { status = 200, method } = options
  await page.route(urlPattern, async (route: Route) => {
    if (method && route.request().method() !== method.toUpperCase()) {
      return route.fallback()
    }
    await route.fulfill({
      status,
      contentType: 'application/json',
      body: JSON.stringify(responseBody),
    })
  })
}

/**
 * Mock all core API routes needed for basic navigation.
 * Includes auth, dashboard stats, competitions, and tasks.
 */
export async function mockCoreApis(page: Page): Promise<void> {
  /* Auth endpoints */
  await mockApiRoute(page, '**/v1/auth/login', AUTH_TOKEN_RESPONSE, { method: 'POST' })
  await mockApiRoute(page, '**/v1/auth/refresh-token', AUTH_TOKEN_RESPONSE, { method: 'POST' })
  await mockApiRoute(page, '**/v1/auth/logout', {}, { method: 'POST' })

  /* Dashboard endpoints */
  await mockApiRoute(page, '**/api/dashboard/stats', DASHBOARD_STATS)
  await mockApiRoute(page, '**/api/dashboard/competitions', DASHBOARD_COMPETITIONS)
  await mockApiRoute(page, '**/api/dashboard/tasks', DASHBOARD_TASKS)
  await mockApiRoute(page, '**/api/dashboard/notifications', [])
  await mockApiRoute(page, '**/api/dashboard/committees', [])
  await mockApiRoute(page, '**/api/dashboard/activity', [])
  await mockApiRoute(page, '**/api/dashboard/charts/**', {})
}

/* ------------------------------------------------------------------ */
/*  Authentication Helpers                                             */
/* ------------------------------------------------------------------ */

/**
 * Set up authenticated state by injecting tokens into localStorage.
 * This bypasses the login flow for tests that don't test auth itself.
 */
export async function setupAuthenticatedState(
  page: Page,
  tokenResponse = AUTH_TOKEN_RESPONSE,
): Promise<void> {
  await page.addInitScript((token) => {
    localStorage.setItem('access_token', token.accessToken)
    localStorage.setItem('refresh_token', token.refreshToken)
    localStorage.setItem('session_id', token.sessionId)
    localStorage.setItem('user', JSON.stringify(token.user))
  }, tokenResponse)
}

/**
 * Set up operator (SuperAdmin) authenticated state.
 */
export async function setupOperatorAuthState(page: Page): Promise<void> {
  await setupAuthenticatedState(page, OPERATOR_TOKEN_RESPONSE)
}

/**
 * Clear all authentication state.
 */
export async function clearAuthState(page: Page): Promise<void> {
  await page.evaluate(() => {
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('session_id')
    localStorage.removeItem('user')
  })
}

/* ------------------------------------------------------------------ */
/*  Language & Direction Helpers                                        */
/* ------------------------------------------------------------------ */

/**
 * Switch the application language by clicking the language toggle.
 */
export async function switchLanguage(
  page: Page,
  targetLocale: SupportedLocale,
): Promise<void> {
  /* The header has a language switch button */
  const langButton = page.getByTestId('language-switch').or(
    page.locator('[data-testid="language-switch"]'),
  ).or(
    page.getByRole('button', { name: /English|العربية/ }),
  )

  if (await langButton.isVisible()) {
    await langButton.click()
    /* Wait for direction change */
    const expectedDir = targetLocale === 'ar' ? 'rtl' : 'ltr'
    await expect(page.locator('html')).toHaveAttribute('dir', expectedDir, {
      timeout: 5000,
    })
  }
}

/**
 * Assert the current document direction.
 */
export async function assertDirection(
  page: Page,
  expectedDir: 'rtl' | 'ltr',
): Promise<void> {
  await expect(page.locator('html')).toHaveAttribute('dir', expectedDir)
}

/**
 * Assert that the page uses the expected language.
 */
export async function assertLanguage(
  page: Page,
  expectedLang: SupportedLocale,
): Promise<void> {
  await expect(page.locator('html')).toHaveAttribute('lang', expectedLang)
}

/* ------------------------------------------------------------------ */
/*  Common Assertions                                                  */
/* ------------------------------------------------------------------ */

/**
 * Assert that all visible numbers use English digits (0-9).
 * Per project rules, Arabic/Hindi digits (٠-٩) are forbidden.
 */
export async function assertEnglishNumbers(page: Page): Promise<void> {
  const bodyText = await page.locator('body').innerText()
  const arabicDigits = /[٠-٩]/g
  const matches = bodyText.match(arabicDigits)
  expect(matches, 'Arabic/Hindi digits found on page').toBeNull()
}

/**
 * Assert that the SAR currency symbol is used for financial values.
 */
export async function assertSarCurrency(
  page: Page,
  selector: string,
): Promise<void> {
  const element = page.locator(selector)
  if (await element.isVisible()) {
    const text = await element.innerText()
    expect(
      text.includes('﷼') || text.includes('SAR') || text.includes('ر.س'),
      `Expected SAR currency symbol in: ${text}`,
    ).toBeTruthy()
  }
}

/**
 * Wait for page to be fully loaded (no pending network requests).
 */
export async function waitForPageReady(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle')
}

/**
 * Take a named screenshot for visual comparison.
 */
export async function takeSnapshot(
  page: Page,
  name: string,
): Promise<void> {
  await page.screenshot({
    path: `e2e-results/screenshots/${name}.png`,
    fullPage: true,
  })
}
