# TwinEngine Demonstrator Setup

## 📋 Overview

This folder provides a complete, containerized setup to demonstrate how **TwinEngine.DataEngine** can be integrated and run locally. It creates a fully functional environment for managing Asset Administration Shells (AAS), submodels, and related digital asset components using Docker Compose.

The setup includes a complete tech stack with services for AAS registry, repository, submodel management, data persistence, UI access, and a plugin system—all orchestrated through Docker containers on a shared network.

## ✨ Included Submodel Templates

This example includes 5 standardized Submodel templates from the **Digital Product Passport for Industry 4.0**:

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
   git clone <repository-url>
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

### Utility Services

| Service | Purpose |
|---------|---------|
| **shell-template-creator** | One-time initialization service that creates the default shell template |

## ⚙️ Configuration & Customization

#### PostgreSQL (Used by Plugin)

The PostgreSQL database is used by the **plugin** service to store data. Configure it through:

**1. Docker Compose Environment Variables:**
```yaml
postgres:
  environment:
    POSTGRES_DB: twinengine
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: admin
```

**2. Plugin Connection String:**
```yaml
plugin:
  environment:
    RelationalDatabaseConfiguration__ConnectionString=Host=postgres;Port=5432;Database=twinengine;Username=postgres;Password=admin
```

**Customization:**

- **Change Database Credentials:**
  ```yaml
  POSTGRES_PASSWORD: your_secure_password
  # Update plugin connection string to match
  RelationalDatabaseConfiguration__ConnectionString=Host=postgres;Port=5432;Database=twinengine;Username=postgres;Password=your_secure_password
  ```

- **Modify Database Initialization Data:**
  Edit `example/postgres/init.sql` to:
  - Create initial tables and schemas
  - Insert seed data
  - Configure user privileges
  - Define data structures for plugin to use

- **Change Database Name:**
  ```yaml
  POSTGRES_DB: your_database_name
  # Update plugin connection string accordingly
  ```

**Important:** Any changes to credentials in docker-compose.yml must be reflected in the plugin's `RelationalDatabaseConfiguration__ConnectionString` environment variable.

## 🐛 Troubleshooting

### Issue: Web UI doesn't load at http://localhost:8080/aas-ui/

**Solution:**
1. Check nginx logs for errors:
   ```bash
   docker-compose logs nginx
   ```
2. Verify nginx is running:
   ```bash
   docker-compose ps nginx
   ```
3. Check that dependencies are healthy:
   ```bash
   docker-compose logs template-repository
   docker-compose logs aas-template-registry
   docker-compose logs sm-template-regisry
   ```

### Issue: Port already in use

**Solution:**
1. Identify which service is using the port:
   ```bash
   netstat -ano | findstr :8080  # Windows
   lsof -i :8080                 # macOS/Linux
   ```
2. Either:
   - Stop the conflicting service
   - Change the port mapping in `docker-compose.yml`
   - Use a different host port (e.g., `8090:80` instead of `8080:80`)

### Issue: Containers fail to start

**Solution:**
1. Check logs for specific service:
   ```bash
   docker-compose logs twinengine-dataengine
   ```
2. Verify all images can be pulled:
   ```bash
   docker-compose pull
   ```

### Issue: Database connection errors

**Solution:**
1. Verify database service is healthy:
   ```bash
   docker-compose logs postgres
   docker-compose logs mongo
   ```
2. Check connection string matches configured credentials
3. For PostgreSQL: Wait 10+ seconds after starting (migration scripts may be running)
4. Verify init.sql executed successfully:
   ```bash
   docker-compose exec postgres psql -U postgres -d twinengine -c "\dt"
   ```

### Issue: Plugin can't connect to database

**Solution:**
1. Ensure postgres is healthy:
   ```bash
   docker-compose ps postgres
   ```
2. Check connection string in plugin environment variables
3. Verify database `twinengine` exists:
   ```bash
   docker-compose exec postgres psql -U postgres -l
   ```
4. Verify credentials match (postgres password must be 'admin')


## 🔐 Security Considerations

For production deployments:

- **Change default passwords:**
  - PostgreSQL: Update `POSTGRES_PASSWORD`
  - MongoDB: Update `MONGO_INITDB_ROOT_PASSWORD`
  - Update corresponding connection strings

- **Use environment files:**
  ```bash
  # Create .env file (do not commit)
  DB_PASSWORD=your_secure_password
  ```

- **Enable HTTPS:** Configure nginx to use SSL certificates

- **Restrict network access:** Use firewall rules and VPC security groups

- **Use secrets management:** For production, consider Docker Secrets or external secret managers

## 📚 Additional Resources

- [TwinEngine Documentation](https://github.com/aas-twinengine) # Todo Add Public wiki link 
- [DPP-Plugin Documentation]()
- [Eclipse BaSyx Documentation](https://wiki.eclipse.org/BaSyx)
- [Asset Administration Shell Specification](https://industrialdigitaltwin.org/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)


## 🤝 Support & Contribution

For issues, questions, or contributions:

1. Check existing GitHub issues
2. Review logs: `docker-compose logs`
3. Create a detailed issue with reproduction steps
4. Include Docker version and docker-compose version output
