FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:ea8bde36c11b6e7eec2656d0e59101d4462f6bd630730f2c8201ed0572b295d5 AS build 
# Install CycloneDX SBOM Generator Package
RUN dotnet tool install --global CycloneDX --version 5.5.0
ENV PATH="$PATH:/root/.dotnet/tools"
# Build application
ARG BUILD_CONFIGURATION=Release
WORKDIR /App
COPY ["AAS.TwinEngine.DataEngine/", "AAS.TwinEngine.DataEngine/"]
RUN dotnet restore "AAS.TwinEngine.DataEngine/AAS.TwinEngine.DataEngine.csproj" --locked-mode
RUN dotnet publish "AAS.TwinEngine.DataEngine/AAS.TwinEngine.DataEngine.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o out \
    --no-restore
# Generate Application SBOM at sbom/bom.xml (omitting dev/test dependencies as they do not appear in final build)
RUN dotnet-CycloneDX "AAS.TwinEngine.DataEngine/AAS.TwinEngine.DataEngine.csproj" -o "sbom/" --exclude-dev --exclude-test-projects --set-nuget-purl --spec-version 1.6  --disable-package-restore

# Create an "app-sbom-artifact" image which can be use to extract the pure APP SBOM after completing the docker build (we want to keep the SBOM for the application separate from the pure linux image SBOM as this provides more details and simplifies vuln management)
FROM scratch AS app-sbom-artifact
COPY --from=build /App/sbom/bom.xml /sbom_application.cyclonedx.xml

# Create final image containing application
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:57bd717ac18ff6c8a39cc0ee4a76c1f15adc46df50434c73eff0c3f1df4c88f0
USER app
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.DataEngine.dll"]
