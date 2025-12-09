# DataEngine

**DataEngine** is a .NET-based service that dynamically generates complete **Asset Administration Shell (AAS)** submodels by combining standardized templates with real-time data.
It integrates with **Eclipse BaSyx** and follows **IDTA specifications** to ensure interoperability.
When a submodel is requested, DataEngine retrieves its template, queries the **Plugin** for semantic ID values, and populates the structure automatically.
It supports nested and hierarchical data models, providing ready-to-use submodels for visualization or API consumption.
In short, DataEngine acts as the **core orchestration layer** that transforms static AAS templates into live digital representations.

