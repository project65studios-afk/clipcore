# Base Image: Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build Image: Use the full .NET SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Project65.Web/Project65.Web.csproj", "Project65.Web/"]
COPY ["Project65.Infrastructure/Project65.Infrastructure.csproj", "Project65.Infrastructure/"]
COPY ["Project65.Core/Project65.Core.csproj", "Project65.Core/"]
RUN dotnet restore "./Project65.Web/Project65.Web.csproj"
COPY . .
WORKDIR "/src/Project65.Web"
RUN dotnet build "./Project65.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish Image: Publish the app to a folder
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Project65.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final Image: Copy the published app to the base image and run it
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Project65.Web.dll"]
