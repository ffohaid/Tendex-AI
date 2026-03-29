#!/usr/bin/env bash
# =============================================================================
# Tendex AI - Automated Backup Script
# =============================================================================
# Creates backups of databases, file storage, and configuration.
# Designed to be run via cron for scheduled backups.
#
# Usage:
#   ./backup.sh [full|db|files|config]
#
# Cron example (daily at 2:00 AM):
#   0 2 * * * /opt/tendex-ai/infrastructure/scripts/backup.sh full >> /opt/tendex-ai/logs/backup.log 2>&1
# =============================================================================

set -euo pipefail

# -------------------------------------------------------------------------
# Configuration
# -------------------------------------------------------------------------
DEPLOY_PATH="/opt/tendex-ai"
INFRA_PATH="${DEPLOY_PATH}/infrastructure"
ENV_FILE="${INFRA_PATH}/.env.prod"
BACKUP_DIR="${DEPLOY_PATH}/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=7

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"; }

# -------------------------------------------------------------------------
# Source environment
# -------------------------------------------------------------------------
if [ -f "${ENV_FILE}" ]; then
    set -a
    source "${ENV_FILE}"
    set +a
else
    log "ERROR: ${ENV_FILE} not found!"
    exit 1
fi

# -------------------------------------------------------------------------
# Backup: SQL Server Databases
# -------------------------------------------------------------------------
backup_db() {
    log "Backing up SQL Server databases..."
    mkdir -p "${BACKUP_DIR}/db"

    # Backup master_platform database
    docker exec tendex-sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
        -Q "BACKUP DATABASE [master_platform] TO DISK = N'/var/opt/mssql/backup/master_platform_${TIMESTAMP}.bak' WITH COMPRESSION, INIT, STATS = 10" \
        2>&1

    # Copy from container
    docker cp "tendex-sqlserver:/var/opt/mssql/backup/master_platform_${TIMESTAMP}.bak" \
        "${BACKUP_DIR}/db/" 2>/dev/null || true

    # Backup all tenant databases
    local tenant_dbs=$(docker exec tendex-sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
        -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name LIKE 'tenant_%'" \
        -h -1 -W 2>/dev/null || echo "")

    for db in ${tenant_dbs}; do
        db=$(echo "${db}" | tr -d '[:space:]')
        if [ -n "${db}" ] && [ "${db}" != "---" ]; then
            log "  Backing up tenant database: ${db}"
            docker exec tendex-sqlserver /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
                -Q "BACKUP DATABASE [${db}] TO DISK = N'/var/opt/mssql/backup/${db}_${TIMESTAMP}.bak' WITH COMPRESSION, INIT" \
                2>&1 || true

            docker cp "tendex-sqlserver:/var/opt/mssql/backup/${db}_${TIMESTAMP}.bak" \
                "${BACKUP_DIR}/db/" 2>/dev/null || true
        fi
    done

    log "Database backup complete."
}

# -------------------------------------------------------------------------
# Backup: MinIO File Storage
# -------------------------------------------------------------------------
backup_files() {
    log "Backing up MinIO file storage..."
    mkdir -p "${BACKUP_DIR}/files"

    # Create a compressed archive of MinIO data volume
    docker run --rm \
        -v tendex-prod-minio-data:/data:ro \
        -v "${BACKUP_DIR}/files:/backup" \
        alpine:latest \
        tar czf "/backup/minio_${TIMESTAMP}.tar.gz" -C /data . \
        2>&1

    log "File storage backup complete."
}

# -------------------------------------------------------------------------
# Backup: Configuration Files
# -------------------------------------------------------------------------
backup_config() {
    log "Backing up configuration files..."
    mkdir -p "${BACKUP_DIR}/config"

    tar czf "${BACKUP_DIR}/config/config_${TIMESTAMP}.tar.gz" \
        -C "${DEPLOY_PATH}" \
        --exclude='backups' \
        --exclude='logs' \
        --exclude='.env.prod' \
        infrastructure/ \
        2>&1

    log "Configuration backup complete."
}

# -------------------------------------------------------------------------
# Cleanup old backups
# -------------------------------------------------------------------------
cleanup() {
    log "Cleaning up backups older than ${RETENTION_DAYS} days..."

    find "${BACKUP_DIR}/db" -name "*.bak" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    find "${BACKUP_DIR}/files" -name "*.tar.gz" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    find "${BACKUP_DIR}/config" -name "*.tar.gz" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true

    # Cleanup backup files inside SQL Server container
    docker exec tendex-sqlserver find /var/opt/mssql/backup -name "*.bak" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true

    log "Cleanup complete."
}

# -------------------------------------------------------------------------
# Main
# -------------------------------------------------------------------------
COMMAND="${1:-full}"

log "=============================================="
log "  Tendex AI - Backup (${COMMAND})"
log "  Timestamp: ${TIMESTAMP}"
log "=============================================="

case "${COMMAND}" in
    full)
        backup_db
        backup_files
        backup_config
        cleanup
        ;;
    db)
        backup_db
        ;;
    files)
        backup_files
        ;;
    config)
        backup_config
        ;;
    cleanup)
        cleanup
        ;;
    *)
        echo "Usage: $0 {full|db|files|config|cleanup}"
        exit 1
        ;;
esac

log "=============================================="
log "  Backup Complete!"
log "  Location: ${BACKUP_DIR}"
log "=============================================="

# Show backup sizes
du -sh "${BACKUP_DIR}"/* 2>/dev/null || true
