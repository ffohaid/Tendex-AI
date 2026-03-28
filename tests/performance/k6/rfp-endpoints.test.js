/**
 * Tendex AI - RFP Endpoints Performance Test
 * ============================================
 * Tests the performance of RFP (Competition) related API endpoints:
 * - GET /api/v1/competitions (paginated list)
 * - GET /api/v1/competitions/:id (detail with related data)
 * - GET /api/v1/competitions/:id/transition-feasibility
 *
 * TASK-703: Performance Testing & Optimization
 */

import http from 'k6/http';
import { group, sleep } from 'k6';
import {
  BASE_URL,
  API_PREFIX,
  THRESHOLDS,
  LOAD_STAGES,
  getAuthHeaders,
} from './config.js';
import {
  authenticate,
  validateResponse,
  validatePagedResponse,
  rfpListDuration,
  rfpDetailDuration,
  errorRate,
  randomPage,
  randomSearchTerm,
  thinkTime,
} from './helpers.js';

// ---------------------------------------------------------------------------
// Test Configuration
// ---------------------------------------------------------------------------
const testType = __ENV.TEST_TYPE || 'load';

export const options = {
  stages: LOAD_STAGES[testType] || LOAD_STAGES.load,
  thresholds: {
    ...THRESHOLDS,
    rfp_list_duration: ['p(95)<1000'],    // RFP list under 1s at p95
    rfp_detail_duration: ['p(95)<1500'],  // RFP detail under 1.5s at p95
    custom_error_rate: ['rate<0.05'],     // Less than 5% errors
  },
  tags: {
    testSuite: 'rfp-endpoints',
    testType: testType,
  },
};

// ---------------------------------------------------------------------------
// Setup: Authenticate once per VU
// ---------------------------------------------------------------------------
export function setup() {
  const token = authenticate();
  if (!token) {
    console.error('Authentication failed during setup. Tests may fail.');
  }
  return { accessToken: token };
}

// ---------------------------------------------------------------------------
// Main Test Scenario
// ---------------------------------------------------------------------------
export default function (data) {
  const params = getAuthHeaders(data.accessToken);
  let competitionId = null;

  // ---- Group 1: List Competitions (Paginated) ----
  group('RFP List - Paginated', () => {
    const page = randomPage();
    const url = `${BASE_URL}${API_PREFIX}/competitions?pageNumber=${page}&pageSize=10`;

    const response = http.get(url, params);
    rfpListDuration.add(response.timings.duration);

    const success = validatePagedResponse(response, 'rfp-list');

    if (success && response.status === 200) {
      try {
        const body = JSON.parse(response.body);
        if (body.items && body.items.length > 0) {
          competitionId = body.items[0].id;
        }
      } catch (e) {
        // Parse error, continue
      }
    }
  });

  thinkTime(1, 2);

  // ---- Group 2: List Competitions with Search Filter ----
  group('RFP List - Search Filter', () => {
    const searchTerm = randomSearchTerm();
    const url = `${BASE_URL}${API_PREFIX}/competitions?pageNumber=1&pageSize=10&searchTerm=${encodeURIComponent(searchTerm)}`;

    const response = http.get(url, params);
    rfpListDuration.add(response.timings.duration);
    validatePagedResponse(response, 'rfp-list-search');
  });

  thinkTime(1, 2);

  // ---- Group 3: List Competitions with Status Filter ----
  group('RFP List - Status Filter', () => {
    const statuses = [0, 1, 2, 3, 4]; // Draft, PendingApproval, Published, etc.
    const status = statuses[Math.floor(Math.random() * statuses.length)];
    const url = `${BASE_URL}${API_PREFIX}/competitions?pageNumber=1&pageSize=10&statusFilter=${status}`;

    const response = http.get(url, params);
    rfpListDuration.add(response.timings.duration);
    validatePagedResponse(response, 'rfp-list-status');
  });

  thinkTime(1, 2);

  // ---- Group 4: Get Competition Detail ----
  if (competitionId) {
    group('RFP Detail - Full Load', () => {
      const url = `${BASE_URL}${API_PREFIX}/competitions/${competitionId}`;

      const response = http.get(url, params);
      rfpDetailDuration.add(response.timings.duration);
      validateResponse(response, 'rfp-detail');
    });

    thinkTime(1, 2);

    // ---- Group 5: Get Transition Feasibility ----
    group('RFP Transition Feasibility', () => {
      const url = `${BASE_URL}${API_PREFIX}/competitions/${competitionId}/transition-feasibility`;

      const response = http.get(url, params);
      validateResponse(response, 'rfp-transition');
    });
  }

  thinkTime(2, 4);
}

// ---------------------------------------------------------------------------
// Teardown
// ---------------------------------------------------------------------------
export function teardown(data) {
  console.log('RFP Endpoints performance test completed.');
}
