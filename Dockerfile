# ============================================================================
# STAGE 1: Base Image (Runtime)
# ============================================================================
# This is the final runtime image - contains only the .NET runtime
# (not the SDK, which is much larger)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Expose ports for HTTP and HTTPS
EXPOSE 80
EXPOSE 443

# ============================================================================
# STAGE 2: Build Image (SDK)
# ============================================================================
# This stage contains the .NET SDK needed to compile the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Set working directory in the build container
WORKDIR /src

# Copy the project file first (for better Docker layer caching)
# Docker caches layers, so if the .csproj hasn't changed, it won't re-download NuGet packages
COPY ["backend/WaybillManagementSystem.csproj", "backend/"]

# Restore NuGet packages
# This downloads all dependencies defined in the .csproj file
RUN dotnet restore "backend/WaybillManagementSystem.csproj"

# Copy all source code
COPY backend/ backend/

# Set working directory to the project folder
WORKDIR "/src/backend"

# Build the application in Release configuration
# -c Release: Optimized build for production
# -o /app/build: Output directory for the build
RUN dotnet build "WaybillManagementSystem.csproj" -c Release -o /app/build

# ============================================================================
# STAGE 3: Publish (Optimized Build)
# ============================================================================
FROM build AS publish

# Publish the application (creates optimized, self-contained output)
# This includes only the necessary files to run the application
RUN dotnet publish "WaybillManagementSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================================================
# STAGE 4: Final Runtime Image
# ============================================================================
FROM base AS final
WORKDIR /app

# Copy only the published files from the publish stage
# This keeps the final image small (only runtime + our app)
COPY --from=publish /app/publish .

# Set the entry point - this is what runs when the container starts
ENTRYPOINT ["dotnet", "WaybillManagementSystem.dll"]
