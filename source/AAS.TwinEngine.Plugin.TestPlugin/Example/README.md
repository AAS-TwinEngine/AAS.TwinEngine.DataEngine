# TwinEngine Demonstrator Setup

## Overview
This project provides a basic setup to demonstrate how **TwinEngine** can be integrated and run locally. It provides a complete environment for managing Asset Administration Shells (AAS) and related components.

This example includes three submodels:

- Nameplate
- ContactInformation
- Reliability
---

## Default configuration

- `example/aas/` — contains default submodel templates (Nameplate, ContactInformation, Reliability).
- `plugin/`— contain JSON files mounted into the plugin containers:
    Changes to these JSON files on the host not visible to the running containers, you must restart the container.

- Two services are built from local sources in the repo:

- twinengine-dataengine (built from local DataEngine source)
- twinengine-plugin (plugin build)

---
## Rebuild images (when you change code)

Rebuild all images then restart:

``` bash
# rebuild images
docker-compose build --no-cache

# restart the stack
docker-compose up -d
```
---

### Troubleshooting 

- If http://localhost:8080/aas-ui/ doesn't load:

    Check docker-compose logs nginx for errors

    Make sure port **8080, 8081, 8082, 8083, 8085, 8086** is not used by another service.

- If a container fails to start because of bind port, stop whatever uses that port or change the mapping in `docker-compose.yml`.

- If you edit plugin JSON files of twinengine-testplugin, make sure you restart that cotainerApp. 
    ```bash
    docker-compose restart <container-app-name>
    ```
    - Verify the host path for the mounted volume is correct relative to the example folder.

---