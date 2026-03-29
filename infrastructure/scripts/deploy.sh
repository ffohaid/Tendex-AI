#!/usr/bin/env bash
# =============================================================================
# Tendex AI - Production Deployment Script
# =============================================================================
# Deploys or updates the Tendex AI platform on the production server.
# Supports full deployment, service-specific updates, and rollback.
#
# Usage:
#   ./deploy.sh [command] [options]
#
# Commands:
#   up          - Start all services (default)
#   update      - Update backend and frontend (zero-downtime)
#   restart     - Restart all services
#   stop        - Stop all services
#   status      - Show service status
#   logs        - Show service logs
#   rollback    - Rollback to previous version
#   backup-db   - Backup SQL Server databases
#   health      - Run health checks
#
# Examples:
#   ./deploy.sh up
#   ./deploy.sh update
#   ./deploy.sh logs backend
#   ./deploy.sh backup-db
# =============================================================================

set -euo pipefail

# -------------------------------------------------------------------------
# Configuration
# -------------------------------------------------------------------------
DEPLOY_PATH="/opt/tendex-ai"
INFRA_PATH="${DEPLOY_PATH}/infrastructure"
COMPOSE_FILE="${INFRA_PATH}/docker-compose.prod.yml"
ENV_FILE="${INFRA_PATH}/.env.prod"
BACKUP_DIR="${DEPLOY_PATH}/backups"
LOG_FILE="${DEPLOY_PATH}/logs/deploy-$(date +%Y%m%d).log"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# -------------------------------------------------------------------------
# Helper Functions
# -------------------------------------------------------------------------
log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo -e "${msg}" | tee -a "${LOG_FILE}"
}

info()    { log "${BLUE}[INFO]${NC} $1"; }
success() { log "${GREEN}[OK]${NC} $1"; }
warn()    { log "${YELLOW}[WARN]${NC} $1"; }
error()   { log "${RED}[ERROR]${NC} $1"; }

check_prerequisites() {
    if [ ! -f "${COMPOSE_FILE}" ]; then
        error "docker-compose.prod.yml not found at ${COMPOSE_FILE}"
        exit 1
    fi

    if [ ! -f "${ENV_FILE}" ]; then
        error ".env.prod not found at ${ENV_FILE}"
        error "Please create it from .env.prod.example"
        exit 1
    fi

    if ! command -v docker &> /dev/null; then
        error "Docker is not installed. Run server-setup.sh first."
        exit 1
    fi
}

compose() {
    docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" "$@"
}

# -------------------------------------------------------------------------
# Command: up - Start all services
# -------------------------------------------------------------------------
cmd_up() {
    info "Starting all Tendex AI services..."
    check_prerequisites

    compose up -d --remove-orphans

    info "Waiting for services to become healthy..."
    sleep 20

    cmd_health
    success "All services started."
}

# -------------------------------------------------------------------------
# Command: update - Zero-downtime update of backend and frontend
# -------------------------------------------------------------------------
cmd_update() {
    info "Starting zero-downtime update..."
    check_prerequisites

    # Step 1: Backup current state
    info "Creating pre-update backup..."
    cmd_backup_db || warn "Database backup skipped (non-critical)."

    # Step 2: Pull latest images or rebuild
    info "Building updated images..."
    compose build --no-cache backend frontend

    # Step 3: Rolling update - backend first
    info "Updating backend service..."
    compose up -d --no-deps --force-recreate backend

    # Wait for backend health
    info "Waiting for backend health check..."
    local retries=0
    while [ $retries -lt 30 ]; do
        if compose exec -T backend curl -sf http://localhost:8080/health > /dev/null 2>&1; then
            success "Backend is healthy."
            break
        fi
        retries=$((retries + 1))
        sleep 2
    done

    if [ $retries -ge 30 ]; then
        error "Backend health check failed after 60 seconds!"
        warn "Consider rolling back: ./deploy.sh rollback"
        return 1
    fi

    # Step 4: Update frontend
    info "Updating frontend service..."
    compose up -d --no-deps --force-recreate frontend

    sleep 5

    # Step 5: Reload Nginx to pick up any config changes
    info "Reloading Nginx..."
    compose exec -T nginx nginx -s reload 2>/dev/null || \
        compose restart nginx

    # Step 6: Final health check
    sleep 10
    cmd_health

    # Step 7: Cleanup
    info "Cleaning up old images..."
    docker image prune -f --filter "until=24h" 2>/dev/null || true

    success "Zero-downtime update complete! (${TIMESTAMP})"
}

# -------------------------------------------------------------------------
# Command: restart - Restart all services
# -------------------------------------------------------------------------
cmd_restart() {
    info "Restarting all services..."
    check_prerequisites
    compose restart
    sleep 15
    cmd_health
    success "All services restarted."
}

# -------------------------------------------------------------------------
# Command: stop - Stop all services
# -------------------------------------------------------------------------
cmd_stop() {
    info "Stopping all services..."
    check_prerequisites
    compose down
    success "All services stopped."
}

# -------------------------------------------------------------------------
# Command: status - Show service status
# -------------------------------------------------------------------------
cmd_status() {
    check_prerequisites
    echo ""
    echo "=============================================="
    echo "  Tendex AI - Service Status"
    echo "  $(date '+%Y-%m-%d %H:%M:%S')"
    echo "=============================================="
    echo ""
    compose ps
    echo ""

    # Show resource usage
    echo "--- Resource Usage ---"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" \
        $(compose ps -q 2>/dev/null) 2>/dev/null || echo "  (unable to fetch stats)"
    echo ""
}

# -------------------------------------------------------------------------
# Command: logs - Show service logs
# -------------------------------------------------------------------------
cmd_logs() {
    local service="${1:-}"
    check_prerequisites

    if [ -n "${service}" ]; then
        compose logs -f --tail=100 "${service}"
    else
        compose logs -f --tail=50
    fi
}

# -------------------------------------------------------------------------
# Command: rollback - Rollback to previous version
# -------------------------------------------------------------------------
cmd_rollback() {
    warn "Rolling back to previous version..."
    check_prerequisites

    # Check if previous images exist
    local prev_backend=$(docker images --format '{{.Repository}}:{{.Tag}}' | grep 'tendex-backend:' | sed -n '2p')
    local prev_frontend=$(docker images --format '{{.Repository}}:{{.Tag}}' | grep 'tendex-frontend:' | sed -n '2p')

    if [ -z "${prev_backend}" ] || [ -z "${prev_frontend}" ]; then
        error "No previous images found for rollback."
        error "Available images:"
        docker images | grep tendex
        exit 1
    fi

    info "Rolling back to:"
    info "  Backend:  ${prev_backend}"
    info "  Frontend: ${prev_frontend}"

    # Tag previous as latest
    docker tag "${prev_backend}" tendex-backend:latest
    docker tag "${prev_frontend}" tendex-frontend:latest

    # Recreate services
    compose up -d --no-deps --force-recreate backend frontend

    sleep 15
    cmd_health

    success "Rollback complete."
}

# -------------------------------------------------------------------------
# Command: backup-db - Backup SQL Server databases
# -------------------------------------------------------------------------
cmd_backup_db() {
    info "Backing up SQL Server databases..."
    check_prerequisites

    local backup_file="${BACKUP_DIR}/db/tendex_backup_${TIMESTAMP}.bak"
    mkdir -p "${BACKUP_DIR}/db"

    # Source the env file to get the password
    source "${ENV_FILE}"

    compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
        -Q "BACKUP DATABASE [master_platform] TO DISK = N'/var/opt/mssql/backup/tendex_backup_${TIMESTAMP}.bak' WITH COMPRESSION, INIT, STATS = 10" \
        2>&1 | tee -a "${LOG_FILE}"

    # Copy backup from container
    docker cp tendex-sqlserver:/var/opt/mssql/backup/tendex_backup_${TIMESTAMP}.bak "${backup_file}" 2>/dev/null || true

    # Cleanup old backups (keep last 7)
    ls -t "${BACKUP_DIR}/db/"*.bak 2>/dev/null | tail -n +8 | xargs rm -f 2>/dev/null || true

    success "Database backup created: ${backup_file}"
}

# -------------------------------------------------------------------------
# Command: health - Run health checks
# -------------------------------------------------------------------------
cmd_health() {
    echo ""
    echo "--- Health Check Results ---"

    local all_healthy=true
    local services=("backend:8080/health" "frontend:80/health" "redis:6379" "sqlserver:1433" "rabbitmq:5672" "minio:9000" "qdrant:6333" "elasticsearch:9200")

    for svc_check in "${services[@]}"; do
        local svc_name="${svc_check%%:*}"
        local container="tendex-${svc_name}"

        if docker ps --format '{{.Names}}' | grep -q "^${container}$"; then
            local health=$(docker inspect --format='{{.State.Health.Status}}' "${container}" 2>/dev/null || echo "no-healthcheck")
            local status=$(docker inspect --format='{{.State.Status}}' "${container}" 2>/dev/null || echo "unknown")

            if [ "${health}" = "healthy" ]; then
                echo -e "  ${GREEN}[HEALTHY]${NC}  ${svc_name}"
            elif [ "${status}" = "running" ]; then
                echo -e "  ${YELLOW}[RUNNING]${NC}  ${svc_name} (health: ${health})"
            else
                echo -e "  ${RED}[DOWN]${NC}     ${svc_name} (status: ${status})"
                all_healthy=false
            fi
        else
            echo -e "  ${RED}[MISSING]${NC}  ${svc_name}"
            all_healthy=false
        fi
    done

    echo ""

    if [ "${all_healthy}" = true ]; then
        success "All services are healthy."
    else
        warn "Some services are not healthy. Check logs for details."
    fi
}

# -------------------------------------------------------------------------
# Main
# -------------------------------------------------------------------------
mkdir -p "${DEPLOY_PATH}/logs"

COMMAND="${1:-up}"
shift 2>/dev/null || true

case "${COMMAND}" in
    up)         cmd_up ;;
    update)     cmd_update ;;
    restart)    cmd_restart ;;
    stop)       cmd_stop ;;
    status)     cmd_status ;;
    logs)       cmd_logs "$@" ;;
    rollback)   cmd_rollback ;;
    backup-db)  cmd_backup_db ;;
    health)     cmd_health ;;
    *)
        echo "Usage: $0 {up|update|restart|stop|status|logs|rollback|backup-db|health}"
        echo ""
        echo "Commands:"
        echo "  up          Start all services"
        echo "  update      Zero-downtime update of backend and frontend"
        echo "  restart     Restart all services"
        echo "  stop        Stop all services"
        echo "  status      Show service status and resource usage"
        echo "  logs [svc]  Show logs (optionally for a specific service)"
        echo "  rollback    Rollback to previous image version"
        echo "  backup-db   Backup SQL Server databases"
        echo "  health      Run health checks on all services"
        exit 1
        ;;
esac
