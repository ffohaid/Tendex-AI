#!/usr/bin/env bash
# =============================================================================
# Tendex AI - Server Initial Setup Script
# =============================================================================
# Run this script ONCE on a fresh Hostinger VPS (Ubuntu) to prepare the
# server for Tendex AI deployment.
#
# Usage (as root):
#   bash server-setup.sh
#
# What this script does:
#   1. Updates system packages
#   2. Installs Docker & Docker Compose
#   3. Configures firewall (UFW)
#   4. Sets up swap space
#   5. Configures system limits
#   6. Creates deployment directory structure
#   7. Hardens SSH configuration
# =============================================================================

set -euo pipefail

echo "=============================================="
echo "  Tendex AI - Server Initial Setup"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"
echo "=============================================="

# -------------------------------------------------------------------------
# Verify running as root
# -------------------------------------------------------------------------
if [ "$(id -u)" -ne 0 ]; then
    echo "ERROR: This script must be run as root."
    exit 1
fi

# -------------------------------------------------------------------------
# Step 1: Update system packages
# -------------------------------------------------------------------------
echo ""
echo "[1/7] Updating system packages..."
apt-get update -y
apt-get upgrade -y
apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    software-properties-common \
    ufw \
    fail2ban \
    htop \
    iotop \
    net-tools \
    unzip \
    jq \
    logrotate

# -------------------------------------------------------------------------
# Step 2: Install Docker & Docker Compose
# -------------------------------------------------------------------------
echo ""
echo "[2/7] Installing Docker..."

if ! command -v docker &> /dev/null; then
    # Add Docker official GPG key
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc

    # Add Docker repository
    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
      $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
      tee /etc/apt/sources.list.d/docker.list > /dev/null

    apt-get update -y
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Enable and start Docker
    systemctl enable docker
    systemctl start docker

    echo "  -> Docker installed: $(docker --version)"
else
    echo "  -> Docker already installed: $(docker --version)"
fi

# -------------------------------------------------------------------------
# Step 3: Configure Firewall (UFW)
# -------------------------------------------------------------------------
echo ""
echo "[3/7] Configuring firewall..."

# Reset UFW
ufw --force reset

# Default policies
ufw default deny incoming
ufw default allow outgoing

# Allow SSH
ufw allow 22/tcp comment 'SSH'

# Allow HTTP and HTTPS
ufw allow 80/tcp comment 'HTTP'
ufw allow 443/tcp comment 'HTTPS'

# Enable UFW
ufw --force enable

echo "  -> Firewall configured. Active rules:"
ufw status verbose

# -------------------------------------------------------------------------
# Step 4: Setup Swap Space (if not already configured)
# -------------------------------------------------------------------------
echo ""
echo "[4/7] Setting up swap space..."

if [ ! -f /swapfile ]; then
    # Create 4GB swap
    fallocate -l 4G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile

    # Make permanent
    echo '/swapfile none swap sw 0 0' >> /etc/fstab

    # Optimize swap settings
    sysctl vm.swappiness=10
    sysctl vm.vfs_cache_pressure=50
    echo 'vm.swappiness=10' >> /etc/sysctl.conf
    echo 'vm.vfs_cache_pressure=50' >> /etc/sysctl.conf

    echo "  -> 4GB swap space created."
else
    echo "  -> Swap already configured."
fi

# -------------------------------------------------------------------------
# Step 5: Configure System Limits
# -------------------------------------------------------------------------
echo ""
echo "[5/7] Configuring system limits..."

# Increase file descriptor limits
cat > /etc/security/limits.d/tendex.conf << 'EOF'
* soft nofile 65535
* hard nofile 65535
root soft nofile 65535
root hard nofile 65535
EOF

# Kernel parameters for Docker
cat > /etc/sysctl.d/99-tendex.conf << 'EOF'
# Network performance
net.core.somaxconn = 65535
net.ipv4.tcp_max_syn_backlog = 65535
net.ipv4.ip_local_port_range = 1024 65535
net.ipv4.tcp_tw_reuse = 1
net.ipv4.tcp_fin_timeout = 15

# Memory
vm.overcommit_memory = 1
vm.max_map_count = 262144

# File system
fs.file-max = 2097152
fs.inotify.max_user_watches = 524288
EOF

sysctl --system > /dev/null 2>&1

echo "  -> System limits configured."

# -------------------------------------------------------------------------
# Step 6: Create Deployment Directory Structure
# -------------------------------------------------------------------------
echo ""
echo "[6/7] Creating deployment directory structure..."

DEPLOY_PATH="/opt/tendex-ai"

mkdir -p "${DEPLOY_PATH}/infrastructure/nginx/conf.d"
mkdir -p "${DEPLOY_PATH}/infrastructure/nginx/ssl"
mkdir -p "${DEPLOY_PATH}/infrastructure/scripts"
mkdir -p "${DEPLOY_PATH}/infrastructure/rabbitmq"
mkdir -p "${DEPLOY_PATH}/backups/db"
mkdir -p "${DEPLOY_PATH}/backups/files"
mkdir -p "${DEPLOY_PATH}/logs"

echo "  -> Directory structure created at ${DEPLOY_PATH}"

# -------------------------------------------------------------------------
# Step 7: Configure fail2ban
# -------------------------------------------------------------------------
echo ""
echo "[7/7] Configuring fail2ban..."

cat > /etc/fail2ban/jail.local << 'EOF'
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5
backend = systemd

[sshd]
enabled = true
port = ssh
filter = sshd
logpath = /var/log/auth.log
maxretry = 3
bantime = 7200
EOF

systemctl enable fail2ban
systemctl restart fail2ban

echo "  -> fail2ban configured."

# -------------------------------------------------------------------------
# Setup Docker log rotation
# -------------------------------------------------------------------------
cat > /etc/docker/daemon.json << 'EOF'
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "5"
  },
  "storage-driver": "overlay2",
  "live-restore": true
}
EOF

systemctl restart docker

echo ""
echo "=============================================="
echo "  Server Setup Complete!"
echo "=============================================="
echo ""
echo "  Next steps:"
echo "  1. Copy .env.prod.example to ${DEPLOY_PATH}/infrastructure/.env.prod"
echo "  2. Edit .env.prod with real production values"
echo "  3. Run the deployment script: deploy.sh"
echo "  4. Run SSL setup: init-ssl.sh <domain> <email>"
echo ""
echo "  IMPORTANT: Reboot the server to apply all changes:"
echo "    reboot"
echo ""
