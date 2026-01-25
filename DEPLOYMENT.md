# MedEdge Gateway - Deployment Guide

**Version:** 1.1.0
**Last Updated:** 2026-01-25
**Status:** Production-Ready (Single-Page Dashboard)

## Table of Contents

1. [Quick Start](#quick-start)
2. [Prerequisites](#prerequisites)
3. [Deployment Options](#deployment-options)
4. [VPS Deployment (Recommended for Production)](#vps-deployment)
5. [Docker Compose Deployment](#docker-compose-deployment)
6. [Monitoring & Logging](#monitoring--logging)
7. [Troubleshooting](#troubleshooting)
8. [Performance Tuning](#performance-tuning)
9. [Security Hardening](#security-hardening)

## Quick Start

### Fastest VPS Deployment

```bash
# Clone repository
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge

# Set dashboard credentials (REQUIRED)
export DASHBOARD_USERNAME=admin
export DASHBOARD_PASSWORD=YourSecurePassword123!

# Build and deploy
docker-compose up -d --build

# Access immediately at
# http://YOUR_SERVER_IP:5000
```

**Result:** Single-Page Interactive Dashboard auto-redirects to System Workflow view.

## Prerequisites

### System Requirements

**Minimum:**
- 2GB RAM
- 1 CPU core
- 10GB disk space
- Docker & Docker Compose (latest)
- Linux (Ubuntu 20.04+, Debian 11+, or similar)

**Recommended:**
- 4GB+ RAM
- 2+ CPU cores
- 20GB disk space
- Docker with 2GB memory allocation
- Public IP address with ports 8080, 5000, 5001 open

### Software Requirements

| Component | Version | Purpose |
|-----------|---------|---------|
| Docker | 20.10+ | Container runtime |
| Docker Compose | 2.20+ | Multi-container orchestration |
| Git | 2.30+ | Version control |
| Nginx | Alpine | Dashboard web server (containerized) |

## Deployment Options

### Option 1: VPS Server (Recommended for Production)

**Ideal for:** Production deployments, public access, demo environments

**Advantages:**
- ✅ Public IP access for dashboard
- ✅ Auto-redirect to System Dashboard
- ✅ All services in one deployment
- ✅ Automatic restart on failure
- ✅ Credential protection via environment variables

**Steps:** See [VPS Deployment](#vps-deployment)

### Option 2: Docker Compose (Local Development)

**Ideal for:** Development, testing, private networks

**Advantages:**
- ✅ Single command deployment
- ✅ All services in one network
- ✅ Easy to debug and modify

**Steps:** See [Docker Compose Deployment](#docker-compose-deployment)

## VPS Deployment

### Server Setup

**1. Prepare Your VPS**

Choose a VPS provider:
- **DigitalOcean** (Basic: $6/month, 1GB RAM, 1 vCPU, 25GB SSD)
- **Linode** (Nanode: $5/month, 1GB RAM, 1 vCPU, 25GB SSD)
- **AWS Lightsail** (Basic: $3.50/month, 512MB RAM, 1 vCPU, 20GB SSD)
- **Vultr** (Regular: $5/month, 1GB RAM, 1 vCPU, 25GB SSD)

**2. Connect to Your VPS**

```bash
# SSH into your VPS
ssh root@YOUR_SERVER_IP

# Update system
apt update && apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" \
  -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# Verify installation
docker --version
docker-compose --version
```

**3. Clone Repository**

```bash
# Install Git
apt install git -y

# Clone repository
cd /opt
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge
```

**4. Configure Credentials**

```bash
# Create environment file
cat > .env << EOF
DASHBOARD_USERNAME=admin
DASHBOARD_PASSWORD=YourSecurePassword123!
EOF

# Secure the file
chmod 600 .env
```

> **⚠️ Important**: Use strong passwords. The default credentials `guest`/`changeme` are NOT secure.

**5. Deploy Application**

```bash
# Build and start all services
docker-compose up -d --build

# Wait 60-90 seconds for all services to initialize
```

**6. Verify Deployment**

```bash
# Check all services are running
docker-compose ps

# Check dashboard is accessible
curl http://localhost:5000

# Check FHIR API health
curl http://localhost:5001/health
```

**7. Access Your Dashboard**

Open your browser and navigate to:
```
http://YOUR_SERVER_IP:5000
```

You should see the **Single-Page Interactive Dashboard** with:
- System Workflow diagram as the centerpiece
- Real-time statistics cards
- Animated status indicators
- Auto-refresh every 3 seconds

### Opening Firewall Ports

If you have a firewall (UFW, firewalld, iptables), open these ports:

```bash
# UFW (Ubuntu)
sudo ufw allow 5000/tcp
sudo ufw allow 5001/tcp
sudo ufw allow 8080/tcp
sudo ufw allow 1883/tcp
sudo ufw reload
```

### Configure Reverse Proxy (Optional)

For domain-based access with SSL:

```bash
# Install Nginx
apt install nginx certbot python3-certbot-nginx -y

# Obtain SSL certificate
certbot --nginx -d yourdomain.com

# Configure Nginx reverse proxy
cat > /etc/nginx/sites-available/mededge << 'EOF'
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl;
    server_name yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

# Enable site
ln -s /etc/nginx/sites-available/medge /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

Access: `https://yourdomain.com`

### Configure Auto-Start on Boot

```bash
# Enable Docker service
systemctl enable docker

# Create systemd service for MedEdge
cat > /etc/systemd/system/mededge.service << 'EOF'
[Unit]
Description=MedEdge Medical Device Platform
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/MedEdge
ExecStart=/usr/local/bin/docker-compose up -d
ExecStop=/usr/local/bin/docker-compose down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

# Enable service
systemctl enable medge
systemctl start mede
```

## Docker Compose Deployment

### File Structure

```bash
MedEdge/
├── docker-compose.yml        # Main orchestration file
├── .env                     # Environment variables (credentials)
├── src/
│   ├── Edge/
│   │   ├── MedEdge.DeviceSimulator/Dockerfile
│   │   └── MedEdge.EdgeGateway/Dockerfile
│   ├── Cloud/
│   │   ├── MedEdge.FhirApi/Dockerfile
│   │   ├── MedEdge.TransformService/Dockerfile
│   │   └── MedEdge.AiEngine/Dockerfile
│   └── Web/
│       └── MedEdge.Dashboard/Dockerfile
└── mosquitto/
    └── config/mosquitto.conf
```

### Quick Deploy

```bash
# Clone repository
git clone https://github.com/bejranonda/MedEdge-Gateway.git
cd MedEdge

# Set credentials
echo "DASHBOARD_USERNAME=admin" > .env
echo "DASHBOARD_PASSWORD=YourSecurePassword123!" >> .env

# Build all images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Verify Deployment

```bash
# Check running containers
docker-compose ps

# Expected output:
# NAME                    IMAGE                              STATUS
# medEdge-dashboard       mededge-dashboard:latest             Up 2 minutes
# medEdge-fhir-api        mededge-fhir-api:latest              Up 2 minutes
# medEdge-mqtt            eclipse-mosquitto:2.0               Up 2 minutes
# medEdge-simulator       mededge-simulator:latest              Up 2 minutes
# medEdge-gateway         mededge-gateway:latest                Up 2 minutes
# medEdge-transform       mededge-transform:latest               Up 2 minutes
# medEdge-ai-engine        mededge-ai-engine:latest               Up 2 minutes

# Test endpoints
curl http://localhost:5000                  # Dashboard (auto-redirects)
curl http://localhost:5001/health          # FHIR API health
curl http://localhost:8080/health          # IoT Hub health

# View service logs
docker-compose logs dashboard
docker-compose logs fhir-api
docker-compose logs mosquitto
```

### Configuration via Environment Variables

The `.env` file in the project root:

```bash
# Dashboard Credentials (REQUIRED)
DASHBOARD_USERNAME=admin
DASHBOARD_PASSWORD=YourSecurePassword123!

# Optional: MQTT Configuration
# MQTT_BROKER=mosquitto
# MQTT_PORT=1883
# MQTT_TLS=false

# Optional: FHIR API Configuration
# FHIR_API_PORT=5001
# DB_CONNECTION=Data Source=/app/data/medEdge.db

# Optional: Logging
# LOG_LEVEL=Information

# Environment
# ENVIRONMENT=Production
```

### Port Mapping

| Service | Internal | External | Protocol | Access URL |
|---------|----------|----------|----------|-------------|
| Dashboard | 8080 | 5000 | HTTP | `http://IP:5000` |
| FHIR API | 8080 | 5001 | HTTP | `http://IP:5001/swagger` |
| IoT Hub Simulator | 8080 | 8080 | HTTP | `http://IP:8080/swagger` |
| MQTT | 1883 | 1883 | TCP | `localhost:1883` |
| MQTT WebSocket | 9001 | 9001 | WebSocket | `localhost:9001` |

## Monitoring & Logging

### Viewing Logs

**All services:**
```bash
docker-compose logs
```

**Specific service:**
```bash
docker-compose logs -f dashboard
docker-compose logs -f fhir-api
docker-compose logs -f transform-service
```

**Last 100 lines:**
```bash
docker-compose logs --tail=100 dashboard
```

### Health Checks

**Dashboard:**
```bash
curl http://localhost:5000
# Should auto-redirect to /system-dashboard
```

**FHIR API:**
```bash
curl http://localhost:5001/health
# Returns: {"status":"healthy"}
```

**IoT Hub Simulator:**
```bash
curl http://localhost:8080/health
# Returns: {"status":"healthy"}
```

### Service Status Dashboard

Access `http://localhost:5000` and click on workflow nodes to see:
- **Medical Devices**: Device count, online status, risk levels
- **Edge Gateway**: Operational status, message throughput
- **MQTT Broker**: Connection status
- **IoT Hub**: Device registry info
- **FHIR API**: Observation count, version info
- **AI Engine**: Monitoring status
- **Dashboard**: Session info, update count

## Troubleshooting

### Common Issues & Solutions

**Issue 1: Dashboard Shows Old Design/Menu**

```bash
# Stop and remove old containers
docker-compose down

# Rebuild from scratch (no cache)
docker-compose build --no-cache dashboard

# Start services
docker-compose up -d

# Hard refresh browser
# Chrome/Edge: Ctrl + Shift + R
# Firefox: Ctrl + Shift + R
```

**Issue 2: Ports Already in Use**

```bash
# Find process using port 5000
lsof -i :5000  # macOS/Linux
netstat -ano | findstr :5000  # Windows

# Kill process
kill -9 <PID>
```

**Issue 3: Dashboard Won't Load**

```bash
# Check dashboard service
docker-compose logs dashboard

# Verify port is accessible
curl http://localhost:5000

# Restart dashboard
docker-compose restart dashboard

# Check if other services are running
docker-compose ps
```

**Issue 4: No Data Showing**

```bash
# Check all services
docker-compose ps

# Check FHIR API
curl http://localhost:5001/health

# Check logs
docker-compose logs -f fhir-api
docker-compose logs -f transform-service
```

**Issue 5: Build Errors**

```bash
# Clean up and rebuild
docker-compose down -v
docker system prune -f
docker-compose up -d --build
```

### Getting Help

**Check Logs:**
```bash
docker-compose logs > logs.txt
```

**Restart All Services:**
```bash
docker-compose restart
```

**Full Reset:**
```bash
docker-compose down -v
docker-compose up -d --build
```

## Performance Tuning

### Resource Limits

In `docker-compose.yml`, add resource limits:

```yaml
services:
  dashboard:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

### Database Optimization

```csharp
// Connection pooling (default: 25)
options.UseNpgsql(connectionString, sqlOptions =>
{
    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    sqlOptions.MaxPoolSize = 50;
    sqlOptions.MinPoolSize = 10;
});
```

### Caching Strategy

Dashboard caches static assets for 1 year in nginx configuration.

## Security Hardening

### Dashboard Credentials

Always set strong credentials via `.env` file:

```bash
DASHBOARD_USERNAME=admin
DASHBOARD_PASSWORD=UseStrongPassword123!#$%
```

### TLS/SSL (Recommended for Production)

1. **Use reverse proxy with SSL** (see above)
2. **Enable MQTT TLS** in mosquitto configuration
3. **Configure API authentication**

### Firewall Configuration

```bash
# Only expose necessary ports
sudo ufw default deny incoming
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 5000/tcp
sudo ufw allow 5001/tcp
sudo ufw enable
```

### Update System

```bash
# Regular security updates
apt update && apt upgrade -y

# Security patches
apt unattended-upgrades -d
```

## Backup & Recovery

### Database Backup

```bash
# SQLite backup
docker-compose exec fhir-api \
  sqlite3 /app/data/medEdge.db ".backup /app/backup/medEdge-backup.db"

# Copy to host
docker cp medEdge-fhir-api:/app/backup/medEdge-backup.db ./backups/
```

### Volume Backup

```bash
# Backup named volume
docker run --rm -v fhir-data:/data -v $(pwd)/backups:/backup \
  busybox tar czf /backup/fhir-data-$(date +%Y%m%d).tar.gz -C /data .
```

## Production Checklist

Before deploying to production:

- [ ] Set strong dashboard credentials (in `.env` file)
- [ ] Configure firewall rules (only expose necessary ports)
- [ ] Enable SSL/TLS (use reverse proxy)
- [ ] Set up database backups (daily)
- [ ] Configure monitoring and alerts
- [ ] Test disaster recovery procedures
- [ ] Document deployment architecture
- [ ] Configure auto-start on system boot
- [ ] Update system regularly
- [ ] Review logs for security issues

## Support & Documentation

**Additional Resources:**
- [Architecture Documentation](docs/ARCHITECTURE.md)
- [FHIR Mapping Guide](docs/FHIR-MAPPING.md)
- [Implementation Summary](IMPLEMENTATION.md)
- [Troubleshooting](#troubleshooting)

---

**Version:** 1.1.0
**Status:** Production-Ready
**Last Updated:** 2026-01-25
