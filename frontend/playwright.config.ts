import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright E2E test configuration for Tendex AI Frontend.
 *
 * Features:
 * - Dual-language support (Arabic RTL / English LTR)
 * - API mocking via route interception (no real backend required)
 * - Chromium-only for CI speed; extend to Firefox/WebKit as needed
 *
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI
    ? [['html', { open: 'never' }], ['junit', { outputFile: 'e2e-results/junit.xml' }]]
    : [['html', { open: 'on-failure' }]],
  timeout: 30_000,
  expect: {
    timeout: 10_000,
  },
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    locale: 'ar-SA',
    timezoneId: 'Asia/Riyadh',
  },

  projects: [
    /* ── Arabic (RTL) ─────────────────────────────────── */
    {
      name: 'chromium-ar',
      use: {
        ...devices['Desktop Chrome'],
        locale: 'ar-SA',
        timezoneId: 'Asia/Riyadh',
      },
    },
    /* ── English (LTR) ────────────────────────────────── */
    {
      name: 'chromium-en',
      use: {
        ...devices['Desktop Chrome'],
        locale: 'en-US',
        timezoneId: 'Asia/Riyadh',
      },
    },
  ],

  /* Run local dev server before tests */
  webServer: {
    command: 'pnpm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
