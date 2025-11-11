# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MCP.sln", "./"]
COPY ["src/MCP.Core/MCP.Core.csproj", "src/MCP.Core/"]
COPY ["src/MCP.Contracts/MCP.Contracts.csproj", "src/MCP.Contracts/"]
COPY ["src/MCP.ApiGateway/MCP.ApiGateway.csproj", "src/MCP.ApiGateway/"]

# Restore dependencies
RUN dotnet restore "src/MCP.ApiGateway/MCP.ApiGateway.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/MCP.ApiGateway"
RUN dotnet build "MCP.ApiGateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MCP.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MCP.ApiGateway.dll"]
