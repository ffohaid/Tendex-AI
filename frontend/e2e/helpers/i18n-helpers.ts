/**
 * Internationalization (i18n) test helpers.
 *
 * Provides utilities for testing bilingual (Arabic/English) support
 * and RTL/LTR direction switching in Tendex AI.
 */
import { type Page, expect } from '@playwright/test'

/* ------------------------------------------------------------------ */
/*  Direction Assertions                                               */
/* ------------------------------------------------------------------ */

/**
 * Verify that RTL-specific Tailwind logical properties are used.
 * Checks that the sidebar and main content respect the current direction.
 */
export async function assertRtlLayout(page: Page): Promise<void> {
  const html = page.locator('html')
  await expect(html).toHaveAttribute('dir', 'rtl')

  /* Sidebar should be on the right side in RTL */
  const sidebar = page.locator('[data-testid="app-sidebar"]').or(
    page.locator('aside').first(),
  )
  if (await sidebar.isVisible()) {
    const sidebarBox = await sidebar.boundingBox()
    const viewportSize = page.viewportSize()
    if (sidebarBox && viewportSize) {
      /* In RTL, sidebar right edge should be at viewport right */
      expect(sidebarBox.x + sidebarBox.width).toBeGreaterThan(
        viewportSize.width * 0.7,
      )
    }
  }
}

/**
 * Verify that LTR layout is correctly applied.
 */
export async function assertLtrLayout(page: Page): Promise<void> {
  const html = page.locator('html')
  await expect(html).toHaveAttribute('dir', 'ltr')

  /* Sidebar should be on the left side in LTR */
  const sidebar = page.locator('[data-testid="app-sidebar"]').or(
    page.locator('aside').first(),
  )
  if (await sidebar.isVisible()) {
    const sidebarBox = await sidebar.boundingBox()
    if (sidebarBox) {
      expect(sidebarBox.x).toBeLessThan(100)
    }
  }
}

/**
 * Verify that text alignment follows the current direction.
 */
export async function assertTextAlignment(
  page: Page,
  direction: 'rtl' | 'ltr',
): Promise<void> {
  const mainContent = page.locator('main').or(page.locator('[role="main"]'))
  if (await mainContent.isVisible()) {
    const computedStyle = await mainContent.evaluate((el) => {
      return window.getComputedStyle(el).direction
    })
    expect(computedStyle).toBe(direction)
  }
}

/* ------------------------------------------------------------------ */
/*  Language Content Assertions                                        */
/* ------------------------------------------------------------------ */

/**
 * Verify that the page displays content in Arabic.
 */
export async function assertArabicContent(page: Page): Promise<void> {
  const bodyText = await page.locator('body').innerText()
  /* Check for common Arabic characters */
  const hasArabic = /[\u0600-\u06FF]/.test(bodyText)
  expect(hasArabic, 'Expected Arabic text content on page').toBeTruthy()
}

/**
 * Verify that the page displays content in English.
 */
export async function assertEnglishContent(page: Page): Promise<void> {
  const bodyText = await page.locator('body').innerText()
  /* Check for common English words that should appear */
  const hasEnglish = /[a-zA-Z]{3,}/.test(bodyText)
  expect(hasEnglish, 'Expected English text content on page').toBeTruthy()
}

/**
 * Verify that a specific translation key is rendered correctly.
 */
export async function assertTranslation(
  page: Page,
  selector: string,
  expectedText: string,
): Promise<void> {
  const element = page.locator(selector)
  await expect(element).toContainText(expectedText)
}
