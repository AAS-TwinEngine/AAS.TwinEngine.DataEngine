FROM mcr.microsoft.com/dotnet/sdk:10.0.403@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
# Install CycloneDX SBOM Generator Package
RUN dotnet tool install --global CycloneDX --version 5.5.0
ENV PATH="$PATH:/root/.dotnet/tools"
# Build application
ARG BUILD_CONFIGURATION=Release
WORKDIR /App
COPY ["AAS.TwinEngine.ExportService/", "AAS.TwinEngine.ExportService/"]
RUN dotnet restore "AAS.TwinEngine.ExportService/AAS.TwinEngine.ExportService.csproj" --locked-mode
RUN dotnet publish "AAS.TwinEngine.ExportService/AAS.TwinEngine.ExportService.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o out \
    --no-restore
# Generate Application SBOM at sbom/bom.xml (omitting dev/test dependencies as they do not appear in final build)
RUN dotnet-CycloneDX "AAS.TwinEngine.ExportService/AAS.TwinEngine.ExportService.csproj" \
    -o "sbom/" \
    --exclude-dev \
    --exclude-test-projects \
    --set-nuget-purl \
    --spec-version 1.6 \
    --disable-package-restore

# Create an "app-sbom-artifact" image which can be use to extract the pure APP SBOM after completing the docker build (we want to keep the SBOM for the application separate from the pure linux image SBOM as this provides more details and simplifies vuln management)
FROM scratch AS app-sbom-artifact
COPY --from=build /App/sbom/bom.xml /sbom_application.cyclonedx.xml

# Create final image containing application
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-alpine3.24@sha256:486297ae006e95ee2ee3112a2c03e3d26e41fbbc64b7a29e28e7a18cc36b12bb
RUN apk add --no-cache tzdata
USER app
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.ExportService.dll"]
