FROM mcr.microsoft.com/dotnet/sdk@sha256:8a90a473da5205a16979de99d2fc20975e922c68304f5c79d564e666dc3982fc AS build 
ARG BUILD_CONFIGURATION=Release
WORKDIR /App
COPY ["AAS.TwinEngine.Plugin.TestPlugin/", "AAS.TwinEngine.Plugin.TestPlugin/"]
RUN dotnet restore "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" --locked-mode
RUN dotnet publish "AAS.TwinEngine.Plugin.TestPlugin/AAS.TwinEngine.Plugin.TestPlugin.csproj" -c "$BUILD_CONFIGURATION" -o out

FROM mcr.microsoft.com/dotnet/aspnet@sha256:60eb031b554df75a4b9f358290a2fa15d8961a3bc79b47bb34a00e31f7b78c69
WORKDIR /App

ENV DATA_DIR=/App/Data
RUN mkdir -p $DATA_DIR
USER app
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.Plugin.TestPlugin.dll"]
