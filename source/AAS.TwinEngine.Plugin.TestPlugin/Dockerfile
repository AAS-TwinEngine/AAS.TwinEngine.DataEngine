FROM mcr.microsoft.com/dotnet/sdk:10.0.403@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
RUN dotnet --info && test "$(dotnet --version)" = "10.0.403"
RUN dotnet tool install --global CycloneDX --version 5.5.0
ENV PATH="$PATH:/root/.dotnet/tools"
ARG BUILD_CONFIGURATION=Release
WORKDIR /App
COPY ["AAS.TwinEngine.Plugin.TestPlugin/", "AAS.TwinEngine.Plugin.TestPlugin/"]
RUN dotnet restore "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" --locked-mode
RUN dotnet publish "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o out \
    --no-restore
RUN dotnet-CycloneDX "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" \
    -o "sbom/" \
    --exclude-dev \
    --exclude-test-projects \
    --set-nuget-purl \
    --spec-version 1.6 \
    --disable-package-restore

FROM scratch AS app-sbom-artifact
COPY --from=build /App/sbom/bom.xml /sbom_application.cyclonedx.xml

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-alpine3.24@sha256:254fe017495996d3abcc487ffacdcce2b73affb380d3af87af12c8dff41fe0b0
RUN dotnet --list-runtimes && dotnet --list-runtimes | grep -Fq 'Microsoft.NETCore.App 10.0.14 '
WORKDIR /App

ENV DATA_DIR=/App/Data
RUN mkdir -p $DATA_DIR
USER app
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.Plugin.TestPlugin.dll"]
