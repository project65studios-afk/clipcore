# ── API ────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipCore.API/ClipCore.API.csproj",                       "ClipCore.API/"]
COPY ["ClipCore.Core/ClipCore.Core.csproj",                     "ClipCore.Core/"]
COPY ["ClipCore.Infrastructure/ClipCore.Infrastructure.csproj", "ClipCore.Infrastructure/"]
RUN dotnet restore "ClipCore.API/ClipCore.API.csproj"
COPY . .
WORKDIR "/src/ClipCore.API"
RUN dotnet build "ClipCore.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ClipCore.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClipCore.API.dll"]
