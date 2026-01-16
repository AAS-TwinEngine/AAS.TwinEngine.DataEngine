# TwinEngine Demonstrator Setup

## 📋 Overview

This folder provides a complete, containerized setup to demonstrate how **TwinEngine.DataEngine** can be integrated and run locally. It creates a fully functional environment for managing Asset Administration Shells (AAS), submodels, and related digital asset components using Docker Compose.

The setup includes a complete tech stack with services for AAS registry, repository, submodel management, data persistence, UI access, and a plugin system—all orchestrated through Docker containers on a shared network.

## ✨ Included Submodel Templates

This example includes 5 standardized submodel templates from the **Digital Product Passport for Industry 4.0**:

- **Nameplate** 
- **ContactInformation** 
- **TechnicalData** 
- **CarbonFootprint** 
- **HandoverDocumentation** 

## 🚀 Quick Start

### Prerequisites

Before running the demonstrator, ensure you have installed:

- **Docker** (v20.10+) — [Install Docker](https://docs.docker.com/get-docker/)
- **Docker Compose** (v1.29+) — Usually included with Docker Desktop
- **Available Ports** — The following ports must be available on your machine:
  - `8080` — Main API Gateway (nginx)
  - `8081` — AAS Environment Repository
  - `8082` — AAS Registry
  - `8083` — Submodel Registry
  - `8085` — TwinEngine DataEngine
  - `8086` — Plugin DPP
  - `5432` — PostgreSQL Database
  - `27017` — MongoDB Database

### Running the Setup

1. **Clone or extract this repository:**
   ```bash
   git clone https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine.git
   cd AAS.TwinEngine.DataEngine
   ```

2. **Start all services:**
   ```bash
   docker-compose up -d
   ```

3. **Access the Web UI:**
   Open your browser and navigate to:
   ```
   http://localhost:8080/aas-ui/
   ```

4. **Stop all services:**
   ```bash
   docker-compose down
   ```

## 🏗️ Architecture & Services

The docker-compose setup includes the following services, all running on a shared `twinengine-network`:

### Core Services

| Service | Port | Image | Purpose |
|---------|------|-------|---------|
| **nginx** | 8080 | `nginx:latest` | API Gateway & Web UI proxy |
| **twinengine-dataengine** | 8085 | `ghcr.io/aas-twinengine/dataengine:latest` | Main TwinEngine DataEngine service |
| **template-repository** | 8081 | `eclipsebasyx/aas-environment:2.0.0-SNAPSHOT` | AAS Environment & Submodel repository |
| **aas-template-registry** | 8082 | `eclipsebasyx/aas-registry-log-mongodb:2.0.0-SNAPSHOT` | AAS Shell Descriptor Registry |
| **sm-template-registry** | 8083 | `eclipsebasyx/submodel-registry-log-mongodb:2.0.0-SNAPSHOT` | Submodel Descriptor Registry |
| **plugin** | 8086 | `ghcr.io/aas-twinengine/plugindpp:latest` | Digital Product Passport Plugin |
| **aas-web-ui** | — | `eclipsebasyx/aas-gui:SNAPSHOT` | Web User Interface (served via nginx) |

### Infrastructure Services

| Service | Port | Image | Purpose |
|---------|------|-------|---------|
| **postgres** | 5432 | `postgres:16-alpine` | Relational database for plugin data |
| **mongo** | 27017 | `mongo:6.0` | NoSQL database for registry metadata |

## Configuration

### PostgreSQL Database (Plugin)

If desired, you can edit credentials in `docker-compose.yml`:
```yaml
POSTGRES_PASSWORD: admin 
```

Update plugin connection string to match. Edit `example/postgres/init.sql` for custom schema/data.

**Using an External Database:**  
To use your own database instead:
1. Change `RelationalDatabaseConfiguration__ConnectionString` in the plugin service environment variables
2. Remove the postgres container from `docker-compose.yml`

**Database Initialization:**  
The initial database script is located in `postgres/init.sql`. Modify this file as needed for your requirements.

### Port Changes

Modify port mappings in `docker-compose.yml`. Update corresponding environment variables in affected services.

## Troubleshooting

**UI not loading:** `docker-compose logs nginx` - Verify ports 8080-8086 are available.

**Port conflicts:** `netstat -ano | findstr :8080` (Windows) to find conflicts. Change ports in `docker-compose.yml`.

**Startup issues:** Run `docker-compose pull` followed by `docker-compose up -d --force-recreate`

**Database errors:** Check `docker-compose ps` for health status. Verify connection strings match credentials.

## Security Note

⚠️ **Change default passwords before production.** Default credentials (postgres: admin) are for development only.

## 📚 Additional Resources

- [TwinEngine Documentation](https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine)
- [DPP-Plugin Documentation](https://github.com/AAS-TwinEngine/AAS.TwinEngine.Plugin.DPP)

