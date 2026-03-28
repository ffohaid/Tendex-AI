/**
 * Tendex AI - RAG Search Performance Test
 * ========================================
 * Tests the performance of RAG (Retrieval-Augmented Generation) endpoints:
 * - POST /api/v1/rag/retrieve-context (Qdrant vector search)
 * - GET  /api/v1/rag/status (Vector store health)
 * - POST /api/v1/rag/index-document (Document indexing)
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
} from './config.js';
import {
  authenticate,
  validateResponse,
  ragSearchDuration,
  healthCheckDuration,
  errorRate,
  randomSearchTerm,
  randomUUID,
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
    rag_search_duration: ['p(95)<2000'],    // RAG search under 2s at p95
    health_check_duration: ['p(95)<500'],   // Health check under 500ms at p95
    custom_error_rate: ['rate<0.05'],
  },
  tags: {
    testSuite: 'rag-search',
    testType: testType,
  },
};

// ---------------------------------------------------------------------------
// RAG Query Templates (Arabic procurement domain)
// ---------------------------------------------------------------------------
const RAG_QUERIES = [
  'ما هي الشروط والمواصفات المطلوبة لمشروع البنية التحتية الرقمية؟',
  'ما هي معايير التقييم الفني للعروض المقدمة؟',
  'ما هي المتطلبات الأمنية لمشاريع تقنية المعلومات الحكومية؟',
  'كيف يتم احتساب الدرجات في التقييم المالي؟',
  'ما هي مراحل دورة حياة المنافسة الحكومية؟',
  'ما هي شروط الضمان البنكي المطلوبة؟',
  'ما هي متطلبات نقل المعرفة والتدريب؟',
  'ما هي مواصفات الخوادم المطلوبة للمشروع؟',
  'كيف يتم التعامل مع التعديلات على العقد بعد الترسية؟',
  'ما هي آلية تقديم الاعتراضات على نتائج المنافسة؟',
  'ما هي الأنظمة واللوائح المنظمة للمنافسات الحكومية؟',
  'ما هي متطلبات التكامل مع الأنظمة الحكومية القائمة؟',
  'ما هي معايير الجودة المطلوبة في مشاريع البرمجيات؟',
  'كيف يتم تقييم الخبرات السابقة للمتنافسين؟',
  'ما هي آلية فض المظاريف الإلكترونية؟',
];

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
export function setup() {
  const token = authenticate();
  return { accessToken: token };
}

// ---------------------------------------------------------------------------
// Main Test Scenario
// ---------------------------------------------------------------------------
export default function (data) {
  const params = getAuthHeaders(data.accessToken);

  // ---- Group 1: Vector Store Health Check ----
  group('RAG - Vector Store Status', () => {
    const url = `${BASE_URL}${API_PREFIX}/rag/status`;

    const response = http.get(url, params);
    healthCheckDuration.add(response.timings.duration);
    validateResponse(response, 'rag-status');
  });

  thinkTime(1, 2);

  // ---- Group 2: Context Retrieval (Primary Search) ----
  group('RAG - Context Retrieval', () => {
    const query = RAG_QUERIES[Math.floor(Math.random() * RAG_QUERIES.length)];
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

    const success = check(response, {
      'rag-search: status is 200 or 400': (r) => r.status === 200 || r.status === 400,
      'rag-search: response time < 3s': (r) => r.timings.duration < 3000,
      'rag-search: no server error': (r) => r.status < 500,
    });
    errorRate.add(!success);
  });

  thinkTime(2, 4);

  // ---- Group 3: Context Retrieval with Document Filter ----
  group('RAG - Filtered Context Retrieval', () => {
    const query = RAG_QUERIES[Math.floor(Math.random() * RAG_QUERIES.length)];
    const url = `${BASE_URL}${API_PREFIX}/rag/retrieve-context`;

    const payload = JSON.stringify({
      query: query,
      tenantId: AUTH_CONFIG.tenantId,
      collectionName: `tenant_${AUTH_CONFIG.tenantId}`,
      topK: 3,
      scoreThreshold: 0.7,
      categoryFilter: 'specifications',
    });

    const response = http.post(url, payload, params);
    ragSearchDuration.add(response.timings.duration);

    const success = check(response, {
      'rag-filtered: status is 200 or 400': (r) => r.status === 200 || r.status === 400,
      'rag-filtered: response time < 3s': (r) => r.timings.duration < 3000,
    });
    errorRate.add(!success);
  });

  thinkTime(2, 4);

  // ---- Group 4: Concurrent Search Simulation ----
  group('RAG - Burst Search', () => {
    const queries = [];
    for (let i = 0; i < 3; i++) {
      const query = RAG_QUERIES[Math.floor(Math.random() * RAG_QUERIES.length)];
      queries.push({
        method: 'POST',
        url: `${BASE_URL}${API_PREFIX}/rag/retrieve-context`,
        body: JSON.stringify({
          query: query,
          tenantId: AUTH_CONFIG.tenantId,
          collectionName: `tenant_${AUTH_CONFIG.tenantId}`,
          topK: 5,
          scoreThreshold: 0.5,
        }),
        params: params,
      });
    }

    const responses = http.batch(queries);
    responses.forEach((response, index) => {
      ragSearchDuration.add(response.timings.duration);
      const success = check(response, {
        [`rag-burst-${index}: no server error`]: (r) => r.status < 500,
      });
      errorRate.add(!success);
    });
  });

  thinkTime(3, 5);
}

// ---------------------------------------------------------------------------
// Teardown
// ---------------------------------------------------------------------------
export function teardown(data) {
  console.log('RAG Search performance test completed.');
}
