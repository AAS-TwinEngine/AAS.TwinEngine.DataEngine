#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AAS.TwinEngine.DataEngine/AAS.TwinEngine.DataEngine.csproj", "AAS.TwinEngine.DataEngine/"]
RUN dotnet restore "AAS.TwinEngine.DataEngine/AAS.TwinEngine.DataEngine.csproj"
COPY AAS.TwinEngine.DataEngine/ AAS.TwinEngine.DataEngine/
WORKDIR "/src/AAS.TwinEngine.DataEngine"

RUN dotnet build "AAS.TwinEngine.DataEngine.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AAS.TwinEngine.DataEngine.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

RUN useradd -m appuser \
 && mkdir /app \
 && chown appuser:appuser /app

USER appuser
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AAS.TwinEngine.DataEngine.dll"]
