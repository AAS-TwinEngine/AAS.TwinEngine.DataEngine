# TwinEngine Demonstrator Setup

## Overview

This folder provides a complete, containerized setup to demonstrate how **TwinEngine.DataEngine** can be integrated and run locally. It creates a fully functional environment for managing Asset Administration Shells (AAS), submodels, and related digital asset components using Docker Compose.

The setup includes a complete tech stack with services for AAS registry, repository, submodel management, data persistence, UI access, and a plugin system—all orchestrated through Docker containers on a shared network.

## Included Submodel Templates

This example includes 5 standardized submodel templates from the **Digital Product Passport for Industry 4.0**:

- **Nameplate**
- **MaintenanceInstructions**
- **TechnicalData**
- **CarbonFootprint**
- **HandoverDocumentation**

## Quick Start

### Prerequisites

Before running the demonstrator, ensure you have installed:

- **Docker** (v20.10+) — [Install Docker](https://docs.docker.com/get-docker/)
- **Docker Compose** (v1.29+) — Usually included with Docker Desktop
- **Available Ports** — The following ports must be available on your machine:
  - `8080` — Main API Gateway (nginx)
  - `8081` — PGAdmin

### Running the Setup

1. **Clone or extract this repository:**
   ```bash
   git clone https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine.git
   ```
2. **Navigate to the minimal example folder:**
   ```bash
   cd AAS.TwinEngine.DataEngine/examples/minimalExample
   ```
3. **Start all services:**
   ```bash
   docker compose up -d
   ```
4. **Access the Web UI:**
   Open your browser and navigate to:
   ```
   http://localhost:8080/aas-ui/
   ```
5. **Stop all services:**
   ```bash
   docker compose down
   ```

## Architecture & Services

The docker-compose setup includes the following services, all running on a shared `twinengine-network`:

### Core Services

| Service | Port | Image | Purpose |
|---------|------|-------|----------|
| **nginx** | 8080 | `nginx:trixie-perl` | API Gateway & Web UI proxy |
| **twinengine-dataengine** | - | `ghcr.io/aas-twinengine/dataengine:develop` | Main TwinEngine DataEngine service |
| **template-repository-registry** | 8082 | `eclipsebasyx/aasenvironment-go:SNAPSHOT` | AAS Environment & Submodel repository (BaSyx Go) |
| **dpp-plugin** | - | `ghcr.io/aas-twinengine/plugindpp:develop` | Digital Product Passport Plugin |
| **aas-web-ui** | — | `eclipsebasyx/aas-gui:SNAPSHOT` | Web User Interface (served via nginx) |

### Infrastructure Services

| Service | Port | Image | Purpose |
|---------|------|-------|----------|
| **basyx_configuration** | - | `eclipsebasyx/basyxconfigurationservice-go:SNAPSHOT` | Initializes BaSyx Go database schema |
| **postgres** | - | `postgres:16-alpine` | Relational database for plugin and BaSyx Go data |
| **pgadmin** | 8081 | `dpage/pgadmin4:snapshot` | Web UI for managing PostgreSQL database |

Notes:

- `postgres` hosts both databases: `twinengine` (plugin data) and `basyxTestDB` (BaSyx Go data).
- DB initialization scripts are in [../shared/postgres](../shared/postgres).
- The setup uses the BaSyx Go stack — no Java or MongoDB services.

## Creating/Changing Your AAS-Data

### Using PGAdmin

PGAdmin provides a web-based interface to manage the PostgreSQL database without writing SQL queries.

**Access PGAdmin:**
1. Navigate to `http://localhost:8081`
2. Login with:
   - **Email:** admin@example.com
   - **Password:** admin

**Connect to PostgreSQL Server:**
1. In PGAdmin, click **"Add New Server"**
2. Fill in the connection details:
   - **Name:** twinengine
   - **Host name:** postgres
   - **Port:** 5432
   - **Username:** postgres
   - **Password:** admin
   - **Database:** twinengine
3. Click **"Save"**

**Browse and Modify Data:**
- In the left sidebar, navigate to: **Servers → twinengine → Databases → twinengine → Schemas → public → Tables**
- Right-click any table and select **"View/Edit Data"** to manage records
- Create new records or modify existing ones directly through the UI

**How changes affect the Plugin:**
- Updates to application data (e.g., shell records, submodels, submodel element values) are reflected in what the Plugin serves.
- Submodel and shell templates are managed by BaSyx Go services and are not modified via PostgreSQL.

---

## Additional Notes

### PostgreSQL Database (Plugin)

If desired, you can edit credentials in `docker-compose.yml`:
```yaml
POSTGRES_PASSWORD: admin
```

Update the plugin connection string to match. Edit `../shared/postgres/` scripts for custom schema/data.

**Using an External Database:**
To use your own database instead:
1. Change `RelationalDatabaseConfiguration__ConnectionString` in the plugin service environment variables
2. Remove the postgres container from `docker-compose.yml`

**Database Initialization:**
The initial database scripts are in `../shared/postgres/`. Modify these files as needed for your requirements.

### Port Changes

Modify port mappings in `docker-compose.yml`. Update corresponding environment variables in affected services.

### Security and Production Notice

Change all default passwords before any use beyond local development. Default credentials (postgres: admin) are for **development** only.

In production, hosting and managing the PostgreSQL database is the customer's responsibility, not the DataEngine's. Use a managed or self-hosted, production-grade PostgreSQL instance and configure the plugin connection string accordingly.

*Do not use this Docker Compose configuration in production.*

---

## Troubleshooting

**UI not loading:** `docker compose logs nginx` — Verify ports 8080 and 8081 are available.

**Port conflicts:** `netstat -ano | findstr :8080` (Windows) to find conflicts. Change ports in `docker-compose.yml`.

**Startup issues:** Run `docker compose pull`

**Database errors:** Check `docker compose ps` for health status. Verify connection strings match credentials.

**PGAdmin not accessible:** Verify the postgres service is healthy with `docker compose ps`. Check port mappings are correctly configured.

**Schema/init changed — recreate volumes:**
```bash
docker compose down -v
docker compose up -d
```

## Additional Resources

- [TwinEngine Documentation](https://github.com/AAS-TwinEngine/AAS.TwinEngine.DataEngine/wiki)
