/**
 * Tendex AI - Performance Testing Configuration
 * ==============================================
 * Shared configuration for all k6 performance test scripts.
 * Centralizes base URLs, thresholds, and common parameters.
 *
 * TASK-703: Performance Testing & Optimization
 */

// ---------------------------------------------------------------------------
// Environment Configuration
// ---------------------------------------------------------------------------
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
export const API_PREFIX = '/api/v1';

// ---------------------------------------------------------------------------
// Authentication Credentials (loaded from environment variables)
// ---------------------------------------------------------------------------
export const AUTH_CONFIG = {
  email: __ENV.TEST_USER_EMAIL || 'admin@tendex.ai',
  password: __ENV.TEST_USER_PASSWORD || 'TestPassword123!',
  tenantId: __ENV.TEST_TENANT_ID || '00000000-0000-0000-0000-000000000001',
};

// ---------------------------------------------------------------------------
// Performance Thresholds
// ---------------------------------------------------------------------------
// These thresholds define acceptable performance limits.
// Tests will fail if any threshold is breached.
export const THRESHOLDS = {
  // HTTP request duration thresholds
  http_req_duration: [
    'p(50)<300',   // 50th percentile under 300ms
    'p(90)<800',   // 90th percentile under 800ms
    'p(95)<1500',  // 95th percentile under 1500ms
    'p(99)<3000',  // 99th percentile under 3000ms
  ],
  // HTTP request failure rate
  http_req_failed: ['rate<0.05'], // Less than 5% failure rate
  // Iteration duration
  iteration_duration: ['p(95)<5000'], // 95th percentile under 5s
};

// ---------------------------------------------------------------------------
// Load Test Stages (Ramp-up Pattern)
// ---------------------------------------------------------------------------
export const LOAD_STAGES = {
  // Smoke test: minimal load to verify functionality
  smoke: [
    { duration: '30s', target: 1 },
  ],
  // Load test: normal expected traffic
  load: [
    { duration: '1m', target: 10 },   // Ramp up to 10 VUs
    { duration: '3m', target: 10 },   // Hold at 10 VUs
    { duration: '1m', target: 20 },   // Ramp up to 20 VUs
    { duration: '3m', target: 20 },   // Hold at 20 VUs
    { duration: '1m', target: 0 },    // Ramp down
  ],
  // Stress test: beyond normal capacity
  stress: [
    { duration: '1m', target: 20 },
    { duration: '2m', target: 50 },
    { duration: '2m', target: 100 },
    { duration: '2m', target: 100 },
    { duration: '2m', target: 0 },
  ],
  // Spike test: sudden burst of traffic
  spike: [
    { duration: '30s', target: 5 },
    { duration: '10s', target: 100 },
    { duration: '1m', target: 100 },
    { duration: '30s', target: 5 },
    { duration: '30s', target: 0 },
  ],
  // Soak test: extended duration for memory leak detection
  soak: [
    { duration: '2m', target: 20 },
    { duration: '30m', target: 20 },
    { duration: '2m', target: 0 },
  ],
};

// ---------------------------------------------------------------------------
// Common HTTP Request Parameters
// ---------------------------------------------------------------------------
export function getAuthHeaders(accessToken) {
  return {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'Authorization': `Bearer ${accessToken}`,
      'X-Tenant-Id': AUTH_CONFIG.tenantId,
    },
  };
}

export function getPublicHeaders() {
  return {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  };
}
