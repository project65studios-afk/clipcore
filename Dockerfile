# Base Image: Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build Image: Use the full .NET SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipCore.Web/ClipCore.Web.csproj", "ClipCore.Web/"]
COPY ["ClipCore.Infrastructure/ClipCore.Infrastructure.csproj", "ClipCore.Infrastructure/"]
COPY ["ClipCore.Core/ClipCore.Core.csproj", "ClipCore.Core/"]
RUN dotnet restore "./ClipCore.Web/ClipCore.Web.csproj"
COPY . .
WORKDIR "/src/ClipCore.Web"
RUN dotnet build "./ClipCore.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish Image: Publish the app to a folder
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClipCore.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final Image: Copy the published app to the base image and run it
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClipCore.Web.dll"]
