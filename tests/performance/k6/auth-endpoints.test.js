/**
 * Tendex AI - Authentication Endpoints Performance Test
 * ======================================================
 * Tests the performance of authentication and session management:
 * - POST /api/v1/auth/login
 * - POST /api/v1/auth/refresh-token
 * - POST /api/v1/auth/logout
 * - GET  /api/v1/auth/me
 *
 * TASK-703: Performance Testing & Optimization
 */

import http from 'k6/http';
import { group, check, sleep } from 'k6';
import {
  BASE_URL,
  API_PREFIX,
  THRESHOLDS,
  LOAD_STAGES,
  AUTH_CONFIG,
  getAuthHeaders,
  getPublicHeaders,
} from './config.js';
import {
  authDuration,
  errorRate,
  thinkTime,
} from './helpers.js';
import { Trend } from 'k6/metrics';

const refreshDuration = new Trend('refresh_token_duration', true);
const meDuration = new Trend('me_endpoint_duration', true);

// ---------------------------------------------------------------------------
// Test Configuration
// ---------------------------------------------------------------------------
const testType = __ENV.TEST_TYPE || 'load';

export const options = {
  stages: LOAD_STAGES[testType] || LOAD_STAGES.load,
  thresholds: {
    ...THRESHOLDS,
    auth_duration: ['p(95)<1500'],           // Login under 1.5s at p95
    refresh_token_duration: ['p(95)<500'],   // Token refresh under 500ms at p95
    me_endpoint_duration: ['p(95)<300'],     // /me endpoint under 300ms at p95
    custom_error_rate: ['rate<0.05'],
  },
  tags: {
    testSuite: 'auth-endpoints',
    testType: testType,
  },
};

// ---------------------------------------------------------------------------
// Main Test Scenario
// ---------------------------------------------------------------------------
export default function () {
  let accessToken = null;
  let refreshToken = null;

  // ---- Group 1: Login ----
  group('Auth - Login', () => {
    const url = `${BASE_URL}${API_PREFIX}/auth/login`;
    const payload = JSON.stringify({
      email: AUTH_CONFIG.email,
      password: AUTH_CONFIG.password,
      tenantId: AUTH_CONFIG.tenantId,
    });

    const response = http.post(url, payload, getPublicHeaders());
    authDuration.add(response.timings.duration);

    const success = check(response, {
      'login: status is 200': (r) => r.status === 200,
      'login: has accessToken': (r) => {
        try {
          return JSON.parse(r.body).accessToken !== undefined;
        } catch {
          return false;
        }
      },
      'login: has refreshToken': (r) => {
        try {
          return JSON.parse(r.body).refreshToken !== undefined;
        } catch {
          return false;
        }
      },
      'login: response time < 2s': (r) => r.timings.duration < 2000,
    });

    errorRate.add(!success);

    if (success && response.status === 200) {
      try {
        const body = JSON.parse(response.body);
        accessToken = body.accessToken;
        refreshToken = body.refreshToken;
      } catch (e) {
        // Parse error
      }
    }
  });

  thinkTime(1, 2);

  // ---- Group 2: Get Current User (/me) ----
  if (accessToken) {
    group('Auth - Get Current User', () => {
      const url = `${BASE_URL}${API_PREFIX}/auth/me`;
      const params = getAuthHeaders(accessToken);

      const response = http.get(url, params);
      meDuration.add(response.timings.duration);

      const success = check(response, {
        'me: status is 200': (r) => r.status === 200,
        'me: has user data': (r) => {
          try {
            const body = JSON.parse(r.body);
            return body.id !== undefined || body.email !== undefined;
          } catch {
            return false;
          }
        },
        'me: response time < 500ms': (r) => r.timings.duration < 500,
      });

      errorRate.add(!success);
    });

    thinkTime(1, 2);
  }

  // ---- Group 3: Refresh Token ----
  if (refreshToken) {
    group('Auth - Refresh Token', () => {
      const url = `${BASE_URL}${API_PREFIX}/auth/refresh-token`;
      const payload = JSON.stringify({
        refreshToken: refreshToken,
        tenantId: AUTH_CONFIG.tenantId,
      });

      const response = http.post(url, payload, getPublicHeaders());
      refreshDuration.add(response.timings.duration);

      const success = check(response, {
        'refresh: status is 200': (r) => r.status === 200,
        'refresh: has new accessToken': (r) => {
          try {
            return JSON.parse(r.body).accessToken !== undefined;
          } catch {
            return false;
          }
        },
        'refresh: response time < 1s': (r) => r.timings.duration < 1000,
      });

      errorRate.add(!success);

      if (success && response.status === 200) {
        try {
          const body = JSON.parse(response.body);
          accessToken = body.accessToken;
          refreshToken = body.refreshToken;
        } catch (e) {
          // Parse error
        }
      }
    });

    thinkTime(1, 2);
  }

  // ---- Group 4: Logout ----
  if (accessToken) {
    group('Auth - Logout', () => {
      const url = `${BASE_URL}${API_PREFIX}/auth/logout`;
      const params = getAuthHeaders(accessToken);

      const response = http.post(url, null, params);

      const success = check(response, {
        'logout: status is 200 or 204': (r) => r.status === 200 || r.status === 204,
        'logout: response time < 500ms': (r) => r.timings.duration < 500,
      });

      errorRate.add(!success);
    });
  }

  thinkTime(2, 4);
}

// ---------------------------------------------------------------------------
// Teardown
// ---------------------------------------------------------------------------
export function teardown() {
  console.log('Authentication performance test completed.');
}
