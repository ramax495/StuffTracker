# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file for dependency caching
COPY src/StuffTracker.Api/StuffTracker.Api.csproj ./src/StuffTracker.Api/

# Restore dependencies for the specific project
RUN dotnet restore src/StuffTracker.Api/StuffTracker.Api.csproj

# Copy source code
COPY src/ ./src/

# Build and publish
RUN dotnet publish src/StuffTracker.Api/StuffTracker.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "StuffTracker.Api.dll"]
