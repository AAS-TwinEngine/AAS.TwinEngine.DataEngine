FROM mcr.microsoft.com/dotnet/sdk@sha256:dc8430e6024d454edadad1e160e1973be3cabbb7125998ef190d9e5c6adf7dbb AS build 
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
FROM mcr.microsoft.com/dotnet/aspnet@sha256:8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711
USER app
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.DataEngine.dll"]
