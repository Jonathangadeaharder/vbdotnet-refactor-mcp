# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MCP.sln", "./"]
COPY ["src/MCP.Core/MCP.Core.csproj", "src/MCP.Core/"]
COPY ["src/MCP.Contracts/MCP.Contracts.csproj", "src/MCP.Contracts/"]
COPY ["src/MCP.RefactoringWorker/MCP.RefactoringWorker.csproj", "src/MCP.RefactoringWorker/"]
COPY ["src/MCP.Plugins.RenameSymbol/MCP.Plugins.RenameSymbol.csproj", "src/MCP.Plugins.RenameSymbol/"]

# Restore dependencies
RUN dotnet restore "src/MCP.RefactoringWorker/MCP.RefactoringWorker.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/MCP.RefactoringWorker"
RUN dotnet build "MCP.RefactoringWorker.csproj" -c Release -o /app/build

# Build the RenameSymbol plugin
WORKDIR "/src/src/MCP.Plugins.RenameSymbol"
RUN dotnet build "MCP.Plugins.RenameSymbol.csproj" -c Release -o /app/plugins

# Publish stage
FROM build AS publish
WORKDIR "/src/src/MCP.RefactoringWorker"
RUN dotnet publish "MCP.RefactoringWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
# Note: Using SDK image instead of runtime because we need MSBuild for compilation validation
WORKDIR /app

# Create plugins directory
RUN mkdir -p /app/plugins

COPY --from=publish /app/publish .
COPY --from=build /app/plugins /app/plugins

ENTRYPOINT ["dotnet", "MCP.RefactoringWorker.dll"]
