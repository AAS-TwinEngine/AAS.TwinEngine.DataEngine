FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /App
COPY ["AAS.TwinEngine.Plugin.TestPlugin/", "AAS.TwinEngine.Plugin.TestPlugin/"]
RUN dotnet restore "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" --locked-mode
RUN dotnet publish "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o out \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:1e37a8236c558ae31bd6bc8144e38e6036b73cf1b0616fe56d79e60babb9d93b
WORKDIR /App

ENV DATA_DIR=/App/Data
RUN mkdir -p $DATA_DIR
USER app
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.Plugin.TestPlugin.dll"]
