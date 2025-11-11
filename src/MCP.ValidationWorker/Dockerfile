# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MCP.sln", "./"]
COPY ["src/MCP.Core/MCP.Core.csproj", "src/MCP.Core/"]
COPY ["src/MCP.ValidationWorker/MCP.ValidationWorker.csproj", "src/MCP.ValidationWorker/"]

# Restore dependencies
RUN dotnet restore "src/MCP.ValidationWorker/MCP.ValidationWorker.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/MCP.ValidationWorker"
RUN dotnet build "MCP.ValidationWorker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MCP.ValidationWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
# Note: Using SDK image instead of runtime because we need MSBuild
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MCP.ValidationWorker.dll"]
