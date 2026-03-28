#!/usr/bin/env bash
# =============================================================================
# Tendex AI - Performance Test Runner
# =============================================================================
# Runs k6 performance tests against the specified environment.
#
# Usage:
#   ./run-tests.sh [test_type] [test_suite]
#
# Arguments:
#   test_type  : smoke | load | stress | spike | soak (default: smoke)
#   test_suite : all | rfp | rag | auth | comprehensive (default: all)
#
# Environment Variables:
#   BASE_URL           : API base URL (default: http://localhost:5000)
#   TEST_USER_EMAIL    : Test user email
#   TEST_USER_PASSWORD : Test user password
#   TEST_TENANT_ID     : Test tenant ID
#
# TASK-703: Performance Testing & Optimization
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K6_DIR="${SCRIPT_DIR}/k6"
RESULTS_DIR="${SCRIPT_DIR}/results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

TEST_TYPE="${1:-smoke}"
TEST_SUITE="${2:-all}"

# Ensure results directory exists
mkdir -p "${RESULTS_DIR}"

# ---------------------------------------------------------------------------
# Color Output
# ---------------------------------------------------------------------------
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info()  { echo -e "${BLUE}[INFO]${NC}  $1"; }
log_ok()    { echo -e "${GREEN}[OK]${NC}    $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ---------------------------------------------------------------------------
# Pre-flight Checks
# ---------------------------------------------------------------------------
log_info "Tendex AI Performance Test Runner"
log_info "================================="
log_info "Test Type  : ${TEST_TYPE}"
log_info "Test Suite : ${TEST_SUITE}"
log_info "Base URL   : ${BASE_URL:-http://localhost:5000}"
log_info "Results Dir: ${RESULTS_DIR}"
echo ""

# Check if k6 is installed
if ! command -v k6 &> /dev/null; then
  log_warn "k6 is not installed. Installing..."
  sudo gpg -k 2>/dev/null
  sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
    --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D68 2>/dev/null
  echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
    | sudo tee /etc/apt/sources.list.d/k6.list > /dev/null
  sudo apt-get update -qq && sudo apt-get install -y -qq k6
  log_ok "k6 installed successfully."
fi

# ---------------------------------------------------------------------------
# Run Test Function
# ---------------------------------------------------------------------------
run_test() {
  local suite_name=$1
  local test_file=$2
  local output_file="${RESULTS_DIR}/${suite_name}_${TEST_TYPE}_${TIMESTAMP}"

  log_info "Running ${suite_name} test (${TEST_TYPE})..."

  k6 run \
    --env TEST_TYPE="${TEST_TYPE}" \
    --env BASE_URL="${BASE_URL:-http://localhost:5000}" \
    --env TEST_USER_EMAIL="${TEST_USER_EMAIL:-admin@tendex.ai}" \
    --env TEST_USER_PASSWORD="${TEST_USER_PASSWORD:-TestPassword123!}" \
    --env TEST_TENANT_ID="${TEST_TENANT_ID:-00000000-0000-0000-0000-000000000001}" \
    --out json="${output_file}.json" \
    --summary-export="${output_file}_summary.json" \
    "${test_file}" 2>&1 | tee "${output_file}.log"

  local exit_code=${PIPESTATUS[0]}

  if [ ${exit_code} -eq 0 ]; then
    log_ok "${suite_name} test passed."
  else
    log_error "${suite_name} test failed (exit code: ${exit_code})."
  fi

  return ${exit_code}
}

# ---------------------------------------------------------------------------
# Execute Tests
# ---------------------------------------------------------------------------
OVERALL_EXIT=0

case "${TEST_SUITE}" in
  rfp)
    run_test "rfp" "${K6_DIR}/rfp-endpoints.test.js" || OVERALL_EXIT=1
    ;;
  rag)
    run_test "rag" "${K6_DIR}/rag-search.test.js" || OVERALL_EXIT=1
    ;;
  auth)
    run_test "auth" "${K6_DIR}/auth-endpoints.test.js" || OVERALL_EXIT=1
    ;;
  comprehensive)
    run_test "comprehensive" "${K6_DIR}/comprehensive.test.js" || OVERALL_EXIT=1
    ;;
  all)
    run_test "auth" "${K6_DIR}/auth-endpoints.test.js" || OVERALL_EXIT=1
    echo ""
    run_test "rfp" "${K6_DIR}/rfp-endpoints.test.js" || OVERALL_EXIT=1
    echo ""
    run_test "rag" "${K6_DIR}/rag-search.test.js" || OVERALL_EXIT=1
    echo ""
    run_test "comprehensive" "${K6_DIR}/comprehensive.test.js" || OVERALL_EXIT=1
    ;;
  *)
    log_error "Unknown test suite: ${TEST_SUITE}"
    log_info "Available suites: all, rfp, rag, auth, comprehensive"
    exit 1
    ;;
esac

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo ""
log_info "================================="
if [ ${OVERALL_EXIT} -eq 0 ]; then
  log_ok "All performance tests passed!"
else
  log_error "Some performance tests failed. Check results in: ${RESULTS_DIR}"
fi
log_info "Results saved to: ${RESULTS_DIR}"

exit ${OVERALL_EXIT}
