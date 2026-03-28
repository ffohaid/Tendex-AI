/**
 * Tendex AI - Comprehensive Performance Test
 * ============================================
 * Combined scenario that simulates realistic user behavior across
 * all critical API endpoints. This test represents a typical user
 * session: login -> browse RFPs -> search -> view details -> evaluate.
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
  authenticate,
  validateResponse,
  validatePagedResponse,
  rfpListDuration,
  rfpDetailDuration,
  ragSearchDuration,
  dashboardDuration,
  healthCheckDuration,
  tenantListDuration,
  userListDuration,
  committeeListDuration,
  evaluationDuration,
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
    rfp_list_duration: ['p(95)<1000'],
    rfp_detail_duration: ['p(95)<1500'],
    rag_search_duration: ['p(95)<2000'],
    dashboard_duration: ['p(95)<1000'],
    health_check_duration: ['p(95)<500'],
    tenant_list_duration: ['p(95)<800'],
    user_list_duration: ['p(95)<800'],
    committee_list_duration: ['p(95)<800'],
    evaluation_duration: ['p(95)<1500'],
    custom_error_rate: ['rate<0.05'],
  },
  tags: {
    testSuite: 'comprehensive',
    testType: testType,
  },
};

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
export function setup() {
  const token = authenticate();
  return { accessToken: token };
}

// ---------------------------------------------------------------------------
// Main Test Scenario: Realistic User Journey
// ---------------------------------------------------------------------------
export default function (data) {
  const params = getAuthHeaders(data.accessToken);
  let competitionId = null;

  // ---- Step 1: Health Check ----
  group('01 - Health Check', () => {
    const url = `${BASE_URL}/api/v1/health/storage`;
    const response = http.get(url);
    healthCheckDuration.add(response.timings.duration);
    validateResponse(response, 'health-check');
  });

  thinkTime(1, 2);

  // ---- Step 2: Dashboard Data ----
  group('02 - Dashboard Load', () => {
    // Simulate loading dashboard with multiple parallel requests
    const requests = [
      { method: 'GET', url: `${BASE_URL}${API_PREFIX}/competitions?pageNumber=1&pageSize=5`, params: params },
      { method: 'GET', url: `${BASE_URL}${API_PREFIX}/auth/me`, params: params },
    ];

    const responses = http.batch(requests);
    responses.forEach((response) => {
      dashboardDuration.add(response.timings.duration);
      check(response, {
        'dashboard: no server error': (r) => r.status < 500,
      });
    });
  });

  thinkTime(2, 4);

  // ---- Step 3: Browse RFP List ----
  group('03 - Browse RFP List', () => {
    const url = `${BASE_URL}${API_PREFIX}/competitions?pageNumber=1&pageSize=10`;
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
        // Parse error
      }
    }
  });

  thinkTime(2, 3);

  // ---- Step 4: Search RFPs ----
  group('04 - Search RFPs', () => {
    const searchTerm = randomSearchTerm();
    const url = `${BASE_URL}${API_PREFIX}/competitions?pageNumber=1&pageSize=10&searchTerm=${encodeURIComponent(searchTerm)}`;
    const response = http.get(url, params);
    rfpListDuration.add(response.timings.duration);
    validatePagedResponse(response, 'rfp-search');
  });

  thinkTime(1, 2);

  // ---- Step 5: View RFP Detail ----
  if (competitionId) {
    group('05 - View RFP Detail', () => {
      const url = `${BASE_URL}${API_PREFIX}/competitions/${competitionId}`;
      const response = http.get(url, params);
      rfpDetailDuration.add(response.timings.duration);
      validateResponse(response, 'rfp-detail');
    });

    thinkTime(3, 5);
  }

  // ---- Step 6: RAG Context Search ----
  group('06 - RAG Context Search', () => {
    const queries = [
      'ما هي الشروط والمواصفات المطلوبة لمشروع البنية التحتية الرقمية؟',
      'ما هي معايير التقييم الفني للعروض المقدمة؟',
      'ما هي المتطلبات الأمنية لمشاريع تقنية المعلومات الحكومية؟',
    ];
    const query = queries[Math.floor(Math.random() * queries.length)];

    const url = `${BASE_URL}${API_PREFIX}/rag/retrieve-context`;
    const payload = JSON.stringify({
      query: query,
      tenantId: AUTH_CONFIG.tenantId,
      collectionName: `tenant_${AUTH_CONFIG.tenantId}`,
      topK: 5,
      scoreThreshold: 0.5,
    });

    const response = http.post(url, payload, params);
    ragSearchDuration.add(response.timings.duration);

    check(response, {
      'rag: no server error': (r) => r.status < 500,
      'rag: response time < 3s': (r) => r.timings.duration < 3000,
    });
  });

  thinkTime(2, 4);

  // ---- Step 7: User Management ----
  group('07 - User List', () => {
    const url = `${BASE_URL}${API_PREFIX}/users?pageNumber=1&pageSize=10`;
    const response = http.get(url, params);
    userListDuration.add(response.timings.duration);
    validateResponse(response, 'user-list');
  });

  thinkTime(1, 2);

  // ---- Step 8: Committee List ----
  group('08 - Committee List', () => {
    const url = `${BASE_URL}${API_PREFIX}/committees?pageNumber=1&pageSize=10`;
    const response = http.get(url, params);
    committeeListDuration.add(response.timings.duration);
    validateResponse(response, 'committee-list');
  });

  thinkTime(1, 2);

  // ---- Step 9: Tenant Information ----
  group('09 - Tenant Info', () => {
    const url = `${BASE_URL}${API_PREFIX}/tenants/${AUTH_CONFIG.tenantId}`;
    const response = http.get(url, params);
    tenantListDuration.add(response.timings.duration);
    validateResponse(response, 'tenant-info');
  });

  thinkTime(1, 2);

  // ---- Step 10: Evaluation Data ----
  if (competitionId) {
    group('10 - Evaluation Data', () => {
      const url = `${BASE_URL}${API_PREFIX}/evaluations/technical/${competitionId}`;
      const response = http.get(url, params);
      evaluationDuration.add(response.timings.duration);
      check(response, {
        'evaluation: no server error': (r) => r.status < 500,
      });
    });
  }

  // ---- Step 11: Audit Trail ----
  group('11 - Audit Trail', () => {
    const url = `${BASE_URL}${API_PREFIX}/audit-logs?pageNumber=1&pageSize=10`;
    const response = http.get(url, params);
    validateResponse(response, 'audit-trail');
  });

  thinkTime(3, 5);
}

// ---------------------------------------------------------------------------
// Teardown
// ---------------------------------------------------------------------------
export function teardown(data) {
  console.log('Comprehensive performance test completed.');
}
