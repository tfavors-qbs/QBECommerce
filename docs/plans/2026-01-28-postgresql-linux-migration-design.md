# PostgreSQL Linux Migration Design

## Overview

Migrate QBECommerce from SQL Server on Windows to PostgreSQL on Linux, with the application running in Docker and Nginx Proxy Manager handling reverse proxy and SSL.

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      Linux Server                            │
│                                                              │
│  ┌───────────────────────────────────────┐    ┌───────────┐ │
│  │            Docker                      │    │ PostgreSQL│ │
│  │  ┌───────────────┐   ┌─────────────┐  │    │  (native) │ │
│  │  │ Nginx Proxy   │──▶│   App       │  │───▶│   :5432   │ │
│  │  │ Manager       │   │   :8080     │  │    └───────────┘ │
│  │  │ (custom SSL)  │   └─────────────┘  │                  │
│  │  └───────────────┘                    │                  │
│  └───────────────────────────────────────┘                  │
└──────────────────────────────────────────────────────────────┘
```

**Components:**
- **PostgreSQL** - Runs natively on Linux host (port 5432)
- **App container** - .NET 9 Blazor app (port 8080)
- **Nginx Proxy Manager** - Reverse proxy with custom SSL certificate (ports 80, 443, 81 for admin)

**Development environment:**
- PostgreSQL runs in Docker on Windows
- App runs with `dotnet run` or optionally in Docker

## Code Changes

### 1. NuGet Package Swap

**Remove from QBExternalWebLibrary.csproj:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.7" />
```

**Add:**
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
```

### 2. DbContext Configuration (Program.cs)

**Change:**
```csharp
options.UseSqlServer(connectionString)
```

**To:**
```csharp
options.UseNpgsql(connectionString)
```

### 3. Connection Strings

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=QBCommerceDB;Username=postgres;Password=devpassword"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=host.docker.internal;Database=QBCommerceDB;Username=qbcommerce;Password=SECURE_PASSWORD_HERE"
  }
}
```

### 4. Migrations

Delete existing migrations folder and create fresh initial migration:

```bash
rm -rf QBExternalWebLibrary/QBExternalWebLibrary/Migrations/
dotnet ef migrations add InitialCreate --project QBExternalWebLibrary/QBExternalWebLibrary
```

### 5. Dockerfile

Create `ShopQualityboltWeb/ShopQualityboltWeb/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj", "ShopQualityboltWeb/"]
COPY ["QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj", "QBExternalWebLibrary/"]
RUN dotnet restore "ShopQualityboltWeb/ShopQualityboltWeb.csproj"
COPY . .
WORKDIR "/src/ShopQualityboltWeb/ShopQualityboltWeb"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopQualityboltWeb.dll"]
```

### 6. docker-compose.yml (Development)

Create in solution root:

```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: devpassword
      POSTGRES_DB: QBCommerceDB
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

### 7. docker-compose.yml (Production)

Create on Linux server:

```yaml
services:
  app:
    image: your-registry/qbcommerce:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=host.docker.internal;Database=QBCommerceDB;Username=qbcommerce;Password=SECURE_PASSWORD_HERE
    extra_hosts:
      - "host.docker.internal:host-gateway"
    restart: unless-stopped

  nginx-proxy-manager:
    image: jc21/nginx-proxy-manager:latest
    ports:
      - "80:80"
      - "443:443"
      - "81:81"
    volumes:
      - ./npm-data:/data
      - ./npm-letsencrypt:/etc/letsencrypt
    restart: unless-stopped
```

## Production Server Setup

### Install PostgreSQL

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### Create Database and User

```bash
sudo -u postgres psql

CREATE USER qbcommerce WITH PASSWORD 'yoursecurepassword';
CREATE DATABASE QBCommerceDB OWNER qbcommerce;
GRANT ALL PRIVILEGES ON DATABASE QBCommerceDB TO qbcommerce;
\q
```

### Configure PostgreSQL for Docker Access

Edit `/etc/postgresql/16/main/pg_hba.conf`:
```
host    QBCommerceDB    qbcommerce    172.17.0.0/16    scram-sha-256
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

### Nginx Proxy Manager Setup

1. Access admin UI at `http://your-server-ip:81`
2. Default login: `admin@example.com` / `changeme`
3. Change default password
4. Add Proxy Host:
   - Domain: `yourdomain.com`
   - Forward hostname: `app`
   - Forward port: `8080`
   - SSL tab: Upload custom certificate (.crt and .key files)

## What Stays the Same

- All models/entities
- All services and repositories
- All Blazor pages and components
- ASP.NET Core Identity (works with PostgreSQL)
- Error logging infrastructure

## Implementation Steps

1. Set up local PostgreSQL with `docker-compose up -d db`
2. Swap NuGet packages
3. Update Program.cs to use `UseNpgsql()`
4. Update connection strings
5. Delete migrations folder
6. Create fresh InitialCreate migration
7. Run and test locally
8. Create Dockerfile and build image
9. Set up Linux server (PostgreSQL, Docker)
10. Deploy and configure Nginx Proxy Manager
