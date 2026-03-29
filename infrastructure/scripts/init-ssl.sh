#!/usr/bin/env bash
# =============================================================================
# Tendex AI - SSL Certificate Initialization Script
# =============================================================================
# This script initializes SSL certificates for the production environment.
# It creates self-signed certificates first (for initial startup), then
# obtains real Let's Encrypt certificates via Certbot.
#
# Usage:
#   ./init-ssl.sh <domain> <email>
#
# Example:
#   ./init-ssl.sh tendex.example.com admin@example.com
# =============================================================================

set -euo pipefail

DOMAIN="${1:?Usage: $0 <domain> <email>}"
EMAIL="${2:?Usage: $0 <domain> <email>}"
CERT_DIR="/etc/letsencrypt/live/tendex"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$(dirname "$SCRIPT_DIR")"

echo "=============================================="
echo "  Tendex AI - SSL Certificate Setup"
echo "=============================================="
echo "  Domain: ${DOMAIN}"
echo "  Email:  ${EMAIL}"
echo "=============================================="

# -------------------------------------------------------------------------
# Step 1: Create self-signed certificate for initial Nginx startup
# -------------------------------------------------------------------------
echo ""
echo "[1/4] Creating self-signed certificate for initial startup..."

mkdir -p "${CERT_DIR}"

if [ ! -f "${CERT_DIR}/fullchain.pem" ]; then
    openssl req -x509 -nodes -newkey rsa:4096 \
        -days 1 \
        -keyout "${CERT_DIR}/privkey.pem" \
        -out "${CERT_DIR}/fullchain.pem" \
        -subj "/CN=${DOMAIN}" \
        2>/dev/null
    echo "  -> Self-signed certificate created."
else
    echo "  -> Certificate already exists, skipping."
fi

# -------------------------------------------------------------------------
# Step 2: Start Nginx with self-signed cert
# -------------------------------------------------------------------------
echo ""
echo "[2/4] Starting Nginx with self-signed certificate..."

cd "${INFRA_DIR}"
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d nginx
sleep 5

# -------------------------------------------------------------------------
# Step 3: Obtain Let's Encrypt certificate
# -------------------------------------------------------------------------
echo ""
echo "[3/4] Requesting Let's Encrypt certificate..."

docker compose -f docker-compose.prod.yml --env-file .env.prod run --rm certbot \
    certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email "${EMAIL}" \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d "${DOMAIN}"

# -------------------------------------------------------------------------
# Step 4: Copy certificates to the correct path and reload Nginx
# -------------------------------------------------------------------------
echo ""
echo "[4/4] Updating certificate paths and reloading Nginx..."

# The certbot stores certs in /etc/letsencrypt/live/<domain>/
# We need to create a symlink or copy to our expected path
CERTBOT_CERT_DIR="/etc/letsencrypt/live/${DOMAIN}"

if [ -d "${CERTBOT_CERT_DIR}" ] && [ "${CERTBOT_CERT_DIR}" != "${CERT_DIR}" ]; then
    # Remove self-signed certs
    rm -f "${CERT_DIR}/fullchain.pem" "${CERT_DIR}/privkey.pem"
    # Create symlinks
    ln -sf "${CERTBOT_CERT_DIR}/fullchain.pem" "${CERT_DIR}/fullchain.pem"
    ln -sf "${CERTBOT_CERT_DIR}/privkey.pem" "${CERT_DIR}/privkey.pem"
fi

# Reload Nginx to pick up new certificates
docker compose -f docker-compose.prod.yml --env-file .env.prod exec nginx nginx -s reload

echo ""
echo "=============================================="
echo "  SSL Setup Complete!"
echo "  Certificate: Let's Encrypt (${DOMAIN})"
echo "  Auto-renewal: Handled by certbot container"
echo "=============================================="
