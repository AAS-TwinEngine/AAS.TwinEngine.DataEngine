# TwinEngine Demonstrator Setup

## Overview

This project provides a basic setup to demonstrate how **TwinEngine** can be integrated and run locally. It provides a complete environment for managing Asset Administration Shells (AAS) and related components.

This example includes three submodels for each existing product (`000-001`, `000-002`, and `000-003`):

- ContactInformation
- CustomSubmodel
- HandoverDocumentation

---

## Default configuration

- [aas/](aas/) contains the existing submodel templates and shell template.
- `plugin/`— contain JSON files mounted into the plugin containers:
  Changes to these JSON files on the host not visible to the running containers, you must restart the container.

- Two services are built from local sources in the repo:

- twinengine-dataengine (built from local DataEngine source)
- twinengine-plugin (plugin build)

### Running the Checks

1. Reload/import the modified XML into the template repository used by this setup. A repository with persisted templates may not overwrite them merely because its container restarts; confirm that the repository serves the updated template before testing.
2. Restart `twinengine-plugin` after updating its mounted JSON data. Docker uses [plugin/mock-submodel-data.json](plugin/mock-submodel-data.json); local runs use [../Data/mock-submodel-data.json](../Data/mock-submodel-data.json). These copies contain the same scenario data.
3. In the existing [API collection](apiCollection/README.md), retrieve each existing product's ContactInformation, CustomSubmodel, and HandoverDocumentation submodels. Inspect the fields and counts from the tables above.
4. Repeat with `GET /submodels` to compare batch retrieval with individual submodel retrieval. Confirm the added static entries retain their relative template order.
5. Retrieve a fully populated element, for example `FiveTemplateLanguages` from CustomSubmodel or `CorporateContact` from ContactInformation, through `GET /submodels/{submodelIdentifier}/submodel-elements/{idShortPath}`. No plugin data request is needed for those elements.
6. Inspect the schema requests sent to TestPlugin: resolved IDs such as `0173-1#02-AAW001#001_en` and `https://mm-software.com/template-values/zero` must be absent. German company IDs and unresolved property/entity IDs must be present in their structural context. A mixed submodel still needs a plugin request for its unresolved fields.

Existing Playwright requests can exercise these scenarios after the setup is reloaded. Full-response snapshots must be reviewed against the new fields, language entries, and static list items before they are refreshed; this change does not update those snapshots or add test code.

---

## Rebuild images (when you change code)

Rebuild all images then restart:

```bash
# rebuild images
docker-compose build --no-cache

# restart the stack
docker-compose up -d
```

---

### Troubleshooting

- If http://localhost:8080/aas-ui/ doesn't load:

  Check docker-compose logs nginx for errors

  Make sure port **8080, 8085, 8086** is not used by another service.

- If a container fails to start because of bind port, stop whatever uses that port or change the mapping in `docker-compose.yml`.

- If you edit plugin JSON files of twinengine-testplugin, make sure you restart that cotainerApp.
  ```bash
  docker-compose restart <container-app-name>
  ```

  - Verify the host path for the mounted volume is correct relative to the example folder.

---
