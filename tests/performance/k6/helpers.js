/**
 * Tendex AI - Performance Testing Helpers
 * ========================================
 * Shared utility functions for k6 performance tests.
 *
 * TASK-703: Performance Testing & Optimization
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';
import { BASE_URL, API_PREFIX, AUTH_CONFIG, getPublicHeaders } from './config.js';

// ---------------------------------------------------------------------------
// Custom Metrics
// ---------------------------------------------------------------------------
export const errorRate = new Rate('custom_error_rate');
export const authDuration = new Trend('auth_duration', true);
export const rfpListDuration = new Trend('rfp_list_duration', true);
export const rfpDetailDuration = new Trend('rfp_detail_duration', true);
export const ragSearchDuration = new Trend('rag_search_duration', true);
export const dashboardDuration = new Trend('dashboard_duration', true);
export const healthCheckDuration = new Trend('health_check_duration', true);
export const tenantListDuration = new Trend('tenant_list_duration', true);
export const userListDuration = new Trend('user_list_duration', true);
export const committeeListDuration = new Trend('committee_list_duration', true);
export const evaluationDuration = new Trend('evaluation_duration', true);
export const apiCallsTotal = new Counter('api_calls_total');

// ---------------------------------------------------------------------------
// Authentication Helper
// ---------------------------------------------------------------------------
/**
 * Authenticates a test user and returns the access token.
 * Caches the token for the VU lifetime to avoid repeated logins.
 */
export function authenticate() {
  const loginUrl = `${BASE_URL}${API_PREFIX}/auth/login`;
  const payload = JSON.stringify({
    email: AUTH_CONFIG.email,
    password: AUTH_CONFIG.password,
    tenantId: AUTH_CONFIG.tenantId,
  });

  const startTime = Date.now();
  const response = http.post(loginUrl, payload, getPublicHeaders());
  const duration = Date.now() - startTime;
  authDuration.add(duration);
  apiCallsTotal.add(1);

  const success = check(response, {
    'auth: status is 200': (r) => r.status === 200,
    'auth: response has accessToken': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.accessToken !== undefined;
      } catch {
        return false;
      }
    },
  });

  errorRate.add(!success);

  if (success) {
    const body = JSON.parse(response.body);
    return body.accessToken;
  }

  return null;
}

// ---------------------------------------------------------------------------
// Response Validation Helpers
// ---------------------------------------------------------------------------
/**
 * Validates a standard API response with common checks.
 */
export function validateResponse(response, name, expectedStatus = 200) {
  apiCallsTotal.add(1);

  const checks = {};
  checks[`${name}: status is ${expectedStatus}`] = (r) => r.status === expectedStatus;
  checks[`${name}: response time < 3s`] = (r) => r.timings.duration < 3000;
  checks[`${name}: no server error`] = (r) => r.status < 500;

  const success = check(response, checks);
  errorRate.add(!success);
  return success;
}

/**
 * Validates a paginated list response.
 */
export function validatePagedResponse(response, name) {
  apiCallsTotal.add(1);

  const checks = {};
  checks[`${name}: status is 200`] = (r) => r.status === 200;
  checks[`${name}: response time < 3s`] = (r) => r.timings.duration < 3000;
  checks[`${name}: has items array`] = (r) => {
    try {
      const body = JSON.parse(r.body);
      return Array.isArray(body.items) || body.items !== undefined;
    } catch {
      return false;
    }
  };
  checks[`${name}: has totalCount`] = (r) => {
    try {
      const body = JSON.parse(r.body);
      return body.totalCount !== undefined;
    } catch {
      return false;
    }
  };

  const success = check(response, checks);
  errorRate.add(!success);
  return success;
}

// ---------------------------------------------------------------------------
// Random Data Generators
// ---------------------------------------------------------------------------
/**
 * Generates a random integer between min and max (inclusive).
 */
export function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

/**
 * Generates a random page number for paginated requests.
 */
export function randomPage(maxPages = 5) {
  return randomInt(1, maxPages);
}

/**
 * Generates a random search term from common Arabic procurement terms.
 */
export function randomSearchTerm() {
  const terms = [
    'مشروع',
    'تقنية',
    'بنية تحتية',
    'أمن معلومات',
    'شبكات',
    'برمجيات',
    'استشارات',
    'صيانة',
    'تطوير',
    'تحول رقمي',
  ];
  return terms[randomInt(0, terms.length - 1)];
}

/**
 * Generates a random UUID for testing.
 */
export function randomUUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

// ---------------------------------------------------------------------------
// Think Time Helper
// ---------------------------------------------------------------------------
/**
 * Simulates user think time between actions.
 */
export function thinkTime(minSeconds = 1, maxSeconds = 3) {
  sleep(randomInt(minSeconds * 1000, maxSeconds * 1000) / 1000);
}
