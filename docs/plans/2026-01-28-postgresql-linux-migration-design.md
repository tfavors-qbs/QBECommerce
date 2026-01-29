# PostgreSQL Linux Migration Design

## Overview

Migrate QBECommerce from SQL Server on Windows to PostgreSQL on Linux, with the application running in Docker containers and Nginx Proxy Manager handling reverse proxy and SSL.

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      Linux Server                            │
│                                                              │
│  ┌───────────────────────────────────────┐    ┌───────────┐ │
│  │            Docker                      │    │ PostgreSQL│ │
│  │  ┌───────────────┐                    │    │  (native) │ │
│  │  │ Nginx Proxy   │   ┌─────────────┐  │    │   :5432   │ │
│  │  │ Manager       │──▶│   Blazor    │  │    └───────────┘ │
│  │  │ :80/:443/:81  │   │   :8080     │  │          ▲       │
│  │  └───────────────┘   └─────────────┘  │          │       │
│  │                            │          │          │       │
│  │                      ┌─────▼───────┐  │          │       │
│  │                      │    API      │──┼──────────┘       │
│  │                      │   :8080     │  │                  │
│  │                      └─────────────┘  │                  │
│  └───────────────────────────────────────┘                  │
└──────────────────────────────────────────────────────────────┘
```

**Components:**
- **PostgreSQL** - Runs natively on Linux host (port 5432)
- **API container** - .NET 9 backend API (internal port 8080)
- **Blazor container** - .NET 9 Blazor Server frontend (internal port 8080)
- **Nginx Proxy Manager** - Reverse proxy with SSL (ports 80, 443, 81 for admin)

**Deployment method:** Artifact-based using GitHub Container Registry (ghcr.io)

---

## Production Deployment Guide

### Prerequisites on Dev Machine (Windows)

1. Docker Desktop with WSL 2
2. GitHub account with Personal Access Token (PAT) with `write:packages` and `read:packages` scopes

### Step 1: Build and Push Images to GitHub Container Registry

**One-time login to ghcr.io:**
```powershell
echo YOUR_GITHUB_TOKEN | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

**Build and push images:**
```powershell
cd C:\Projects\QBECommerce_git\QBECommerce

# Build images
docker build -f Dockerfile.api -t ghcr.io/YOUR_USERNAME/qbcommerce-api:latest .
docker build -f Dockerfile.blazor -t ghcr.io/YOUR_USERNAME/qbcommerce-blazor:latest .

# Push to registry
docker push ghcr.io/YOUR_USERNAME/qbcommerce-api:latest
docker push ghcr.io/YOUR_USERNAME/qbcommerce-blazor:latest
```

### Step 2: Prepare Production Server

**Install Docker:**
```bash
sudo apt update
sudo apt install -y docker.io docker-compose
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker $USER
# Log out and back in for group change to take effect
```

**Install PostgreSQL:**
```bash
sudo apt install -y postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

**Find your PostgreSQL version (important for config paths):**
```bash
ls /etc/postgresql/
# Note the version number (e.g., 16, 17)
```

### Step 3: Configure PostgreSQL

**Create database and user:**
```bash
sudo -u postgres psql
```

```sql
-- IMPORTANT: Use lowercase database name or quote it
-- Unquoted names become lowercase in PostgreSQL
CREATE USER qbcommerce WITH PASSWORD 'your-secure-password';
CREATE DATABASE qbcommercedb OWNER qbcommerce;
GRANT ALL PRIVILEGES ON DATABASE qbcommercedb TO qbcommerce;
\q
```

**Configure PostgreSQL to listen on all interfaces:**

Edit `/etc/postgresql/XX/main/postgresql.conf` (replace XX with your version):
```bash
sudo nano /etc/postgresql/17/main/postgresql.conf
```

Find and change:
```
listen_addresses = '*'
```

**Configure pg_hba.conf for Docker access:**

Edit `/etc/postgresql/XX/main/pg_hba.conf`:
```bash
sudo nano /etc/postgresql/17/main/pg_hba.conf
```

Add this line at the end (covers all Docker networks):
```
host    qbcommercedb    qbcommerce    172.16.0.0/12    scram-sha-256
```

**Restart PostgreSQL:**
```bash
sudo systemctl restart postgresql
```

**Verify PostgreSQL is listening:**
```bash
sudo ss -tlnp | grep 5432
# Should show 0.0.0.0:5432
```

### Step 4: Deploy Application

**Create application directory:**
```bash
mkdir -p ~/qbcommerce
cd ~/qbcommerce
```

**Create docker-compose.prod.yml:**
```yaml
services:
  api:
    image: ghcr.io/YOUR_USERNAME/qbcommerce-api:latest
    container_name: qbcommerce-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnectionString=Host=host.docker.internal;Database=qbcommercedb;Username=qbcommerce;Password=${DB_PASSWORD}
      - AdminUser__Email=${ADMIN_EMAIL}
      - AdminUser__Password=${ADMIN_PASSWORD}
    extra_hosts:
      - "host.docker.internal:host-gateway"
    restart: unless-stopped
    networks:
      - qbcommerce-network

  blazor:
    image: ghcr.io/YOUR_USERNAME/qbcommerce-blazor:latest
    container_name: qbcommerce-blazor
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ApiSettings__BaseAddress=http://api:8080
    depends_on:
      - api
    restart: unless-stopped
    networks:
      - qbcommerce-network

  nginx-proxy-manager:
    image: jc21/nginx-proxy-manager:latest
    container_name: nginx-proxy-manager
    ports:
      - "80:80"
      - "443:443"
      - "81:81"
    volumes:
      - ./npm-data:/data
      - ./npm-letsencrypt:/etc/letsencrypt
    restart: unless-stopped
    networks:
      - qbcommerce-network

networks:
  qbcommerce-network:
    driver: bridge
```

**Create .env file:**
```bash
cat > .env << 'EOF'
DB_PASSWORD=your-postgres-password
ADMIN_EMAIL=admin@yourcompany.com
ADMIN_PASSWORD=YourSecureAdminPassword123!
EOF
```

**Login to ghcr.io on production server:**
```bash
echo YOUR_GITHUB_TOKEN | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

**Start the application:**
```bash
docker-compose -f docker-compose.prod.yml up -d
```

**Check logs:**
```bash
docker logs qbcommerce-api 2>&1 | tail -30
```

### Step 5: Configure Nginx Proxy Manager

1. Access admin UI at `http://your-server-ip:81`
2. Default login: `admin@example.com` / `changeme`
3. Change default password
4. Add Proxy Host for **Blazor frontend**:
   - Domain: `yourdomain.com`
   - Scheme: `http`
   - Forward Hostname: `qbcommerce-blazor`
   - Forward Port: `8080`
   - **Enable Websockets** (required for Blazor Server)
   - SSL tab: Upload custom certificate or request Let's Encrypt

---

## Troubleshooting

### "no pg_hba.conf entry for host" Error

Docker networks can use various subnets (172.17.x.x, 172.18.x.x, etc.). Use the broad range `172.16.0.0/12` which covers all possible Docker networks.

```bash
# Check which IP the container is using
docker logs qbcommerce-api 2>&1 | grep "host"

# Update pg_hba.conf with broad range
sudo nano /etc/postgresql/17/main/pg_hba.conf
# Add: host    qbcommercedb    qbcommerce    172.16.0.0/12    scram-sha-256

sudo systemctl restart postgresql
docker restart qbcommerce-api
```

### "Connection refused" Error

PostgreSQL isn't listening on the Docker interface.

```bash
# Check if PostgreSQL is listening
sudo ss -tlnp | grep 5432

# If only showing 127.0.0.1, fix postgresql.conf
sudo nano /etc/postgresql/17/main/postgresql.conf
# Set: listen_addresses = '*'

sudo systemctl restart postgresql
```

### "database does not exist" Error

PostgreSQL lowercases database names unless quoted. Use lowercase `qbcommercedb` everywhere.

### "password authentication failed" Error

Verify the password in .env matches what was set in PostgreSQL:
```bash
# Test connection directly
psql -h localhost -U qbcommerce -d qbcommercedb -c "SELECT 1;"

# Reset password if needed
sudo -u postgres psql
ALTER USER qbcommerce WITH PASSWORD 'new-password';
\q
```

### Port 80 Already in Use

Something else is using port 80 (Apache, nginx, etc.):
```bash
sudo ss -tlnp | grep :80

# Stop conflicting service
sudo systemctl stop apache2
sudo systemctl disable apache2
```

---

## Development Environment

### docker-compose.yml (Local Development)

```yaml
# Run with: docker-compose up -d
# Access: Blazor UI at http://localhost:5000, API at http://localhost:5278

services:
  db:
    image: postgres:16
    container_name: qbcommerce-postgres-dev
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: devpassword
      POSTGRES_DB: QBCommerceDB
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - qbcommerce-dev

  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    container_name: qbcommerce-api-dev
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnectionString=Host=db;Database=QBCommerceDB;Username=postgres;Password=devpassword
    ports:
      - "5278:8080"
    depends_on:
      db:
        condition: service_healthy
    networks:
      - qbcommerce-dev

  blazor:
    build:
      context: .
      dockerfile: Dockerfile.blazor
    container_name: qbcommerce-blazor-dev
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ApiSettings__BaseAddress=http://api:8080
    ports:
      - "5000:8080"
    depends_on:
      - api
    networks:
      - qbcommerce-dev

volumes:
  pgdata:

networks:
  qbcommerce-dev:
    driver: bridge
```

---

## Files Required for Production Deployment

Only these files are needed on the production server:
- `docker-compose.prod.yml`
- `.env` (with secrets - never commit to git)

The application images are pulled from GitHub Container Registry.

---

## Updating Production

To deploy updates:

**On dev machine:**
```powershell
# Build and push new images
docker build -f Dockerfile.api -t ghcr.io/YOUR_USERNAME/qbcommerce-api:latest .
docker build -f Dockerfile.blazor -t ghcr.io/YOUR_USERNAME/qbcommerce-blazor:latest .
docker push ghcr.io/YOUR_USERNAME/qbcommerce-api:latest
docker push ghcr.io/YOUR_USERNAME/qbcommerce-blazor:latest
```

**On production server:**
```bash
cd ~/qbcommerce
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

---

## Implementation Status

### Completed
- [x] PostgreSQL migration (NuGet packages, DbContext, connection strings)
- [x] Dockerfiles for API and Blazor
- [x] docker-compose for development
- [x] docker-compose for production with ghcr.io images
- [x] Admin user seeding on startup
- [x] GitHub Container Registry deployment workflow
- [x] Test deployment to staging server
- [x] Documentation with troubleshooting guide

### Files Changed
| File | Status |
|------|--------|
| `QBExternalWebLibrary/QBExternalWebLibrary.csproj` | Modified (Npgsql) |
| `ShopQualityboltWeb/ShopQualityboltWeb.csproj` | Modified (Npgsql) |
| `ShopQualityboltWeb/ShopQualityboltWeb/Program.cs` | Modified (UseNpgsql, admin seeding) |
| `ShopQualityboltWeb/ShopQualityboltWeb/appsettings.*.json` | Modified (connection strings) |
| `Dockerfile.api` | Created |
| `Dockerfile.blazor` | Created |
| `docker-compose.yml` | Created (development) |
| `docker-compose.prod.yml` | Created (production) |
| `.dockerignore` | Created |
| `.env.example` | Created |
| `deploy/` | Created (deployment files) |
