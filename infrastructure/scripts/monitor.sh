#!/usr/bin/env bash
# =============================================================================
# Tendex AI - Service Monitor Script
# =============================================================================
# Monitors all Tendex AI services and restarts unhealthy containers.
# Designed to be run via cron every 5 minutes.
#
# Cron example:
#   */5 * * * * /opt/tendex-ai/infrastructure/scripts/monitor.sh >> /opt/tendex-ai/logs/monitor.log 2>&1
# =============================================================================

set -euo pipefail

DEPLOY_PATH="/opt/tendex-ai"
INFRA_PATH="${DEPLOY_PATH}/infrastructure"
COMPOSE_FILE="${INFRA_PATH}/docker-compose.prod.yml"
ENV_FILE="${INFRA_PATH}/.env.prod"
LOG_FILE="${DEPLOY_PATH}/logs/monitor-$(date +%Y%m%d).log"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "${LOG_FILE}"; }

mkdir -p "${DEPLOY_PATH}/logs"

# -------------------------------------------------------------------------
# Check each critical service
# -------------------------------------------------------------------------
SERVICES=(
    "tendex-nginx"
    "tendex-backend"
    "tendex-frontend"
    "tendex-sqlserver"
    "tendex-redis"
    "tendex-rabbitmq"
    "tendex-minio"
    "tendex-qdrant"
)

RESTART_NEEDED=false

for container in "${SERVICES[@]}"; do
    if ! docker ps --format '{{.Names}}' | grep -q "^${container}$"; then
        log "ALERT: ${container} is NOT running! Attempting restart..."
        RESTART_NEEDED=true
    else
        health=$(docker inspect --format='{{.State.Health.Status}}' "${container}" 2>/dev/null || echo "none")
        if [ "${health}" = "unhealthy" ]; then
            log "ALERT: ${container} is UNHEALTHY! Attempting restart..."
            docker restart "${container}" 2>/dev/null || true
            RESTART_NEEDED=true
        fi
    fi
done

# -------------------------------------------------------------------------
# Restart all services if any critical service is down
# -------------------------------------------------------------------------
if [ "${RESTART_NEEDED}" = true ]; then
    log "Restarting affected services..."
    cd "${INFRA_PATH}"
    docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d --remove-orphans 2>&1 | \
        while read -r line; do log "  ${line}"; done
    log "Restart attempt complete."
fi

# -------------------------------------------------------------------------
# Check disk space
# -------------------------------------------------------------------------
DISK_USAGE=$(df / | awk 'NR==2 {print $5}' | tr -d '%')
if [ "${DISK_USAGE}" -gt 85 ]; then
    log "WARNING: Disk usage is at ${DISK_USAGE}%!"
    # Auto-cleanup Docker resources
    docker system prune -f --volumes --filter "until=168h" 2>/dev/null || true
    log "Docker cleanup executed."
fi

# -------------------------------------------------------------------------
# Check memory usage
# -------------------------------------------------------------------------
MEM_USAGE=$(free | awk '/Mem:/ {printf("%.0f", $3/$2 * 100)}')
if [ "${MEM_USAGE}" -gt 90 ]; then
    log "WARNING: Memory usage is at ${MEM_USAGE}%!"
fi

# -------------------------------------------------------------------------
# Rotate old log files (keep 30 days)
# -------------------------------------------------------------------------
find "${DEPLOY_PATH}/logs" -name "*.log" -mtime +30 -delete 2>/dev/null || true
