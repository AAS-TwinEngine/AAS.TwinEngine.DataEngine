FROM mcr.microsoft.com/dotnet/sdk@sha256:dc8430e6024d454edadad1e160e1973be3cabbb7125998ef190d9e5c6adf7dbb AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /App
COPY ["AAS.TwinEngine.Plugin.TestPlugin/", "AAS.TwinEngine.Plugin.TestPlugin/"]
RUN dotnet restore "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" --locked-mode
RUN dotnet publish "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" -c "$BUILD_CONFIGURATION" -o out

FROM mcr.microsoft.com/dotnet/aspnet@sha256:9b5222b0ff8e9eb991a7c1a64b25f0f771d21ccc05dfa1c834f5668ffd9cd73f
WORKDIR /App

ENV DATA_DIR=/App/Data
RUN mkdir -p $DATA_DIR
USER app
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.Plugin.TestPlugin.dll"]
