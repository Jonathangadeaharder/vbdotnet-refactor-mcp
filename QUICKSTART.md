# Mass Code Platform - Quick Start Guide

This guide will help you get the MCP platform up and running in under 10 minutes.

## Prerequisites

- Docker and Docker Compose installed
- .NET 8.0 SDK (for local development)
- A VB.NET solution to refactor

## Quick Start with Docker

### 1. Start the Platform

```bash
docker-compose up -d
```

This starts:
- SQL Server (database for job queue)
- API Gateway (REST API on port 5000)
- Refactoring Worker (Roslyn transformation engine)
- Validation Worker (compilation and CI/CD integration)

### 2. Verify Services are Running

```bash
docker-compose ps
```

All services should show as "Up" with healthy status.

### 3. Access the Hangfire Dashboard

Open your browser to: http://localhost:5000/hangfire

This shows the job queue, active workers, and job history.

### 4. Access the API Documentation

Open: http://localhost:5000/swagger

This provides interactive API documentation with the ability to test endpoints.

## Submit Your First Refactoring Job

### Example: Rename a Symbol

Create a file `job-request.json`:

```json
{
  "solutionPath": "/workspaces/MyApp/MyApp.sln",
  "refactoringToolName": "RenameSymbol",
  "parameters": {
    "targetFile": "MyProject/Calculator.vb",
    "textSpanStart": 50,
    "textSpanLength": 3,
    "newName": "Calculate"
  },
  "validationPolicy": {
    "onSuccess": "CreatePullRequest",
    "onFailure": "DeleteBranch",
    "steps": ["Compile"]
  }
}
```

### Submit the Job

```bash
curl -X POST http://localhost:5000/api/v1/refactoringjobs \
  -H "Content-Type: application/json" \
  -d @job-request.json
```

Response:
```json
{
  "jobId": "1",
  "status": "Accepted",
  "statusUrl": "http://localhost:5000/api/v1/refactoringjobs/1"
}
```

### Poll for Results

```bash
curl http://localhost:5000/api/v1/refactoringjobs/1
```

## Local Development Setup

### 1. Clone and Build

```bash
git clone <repository-url>
cd vbdotnet-refactor-mcp
dotnet build MCP.sln
```

### 2. Set Up SQL Server

Using Docker:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name mcp-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Or use a local SQL Server instance and update connection strings in `appsettings.json`.

### 3. Run Services Locally

**Terminal 1 - API Gateway:**
```bash
cd src/MCP.ApiGateway
dotnet run
```

**Terminal 2 - Refactoring Worker:**
```bash
cd src/MCP.RefactoringWorker
mkdir -p plugins
cp ../MCP.Plugins.RenameSymbol/bin/Debug/net8.0/*.dll plugins/
dotnet run
```

The API will be available at: http://localhost:5000

## Creating Your First Custom Tool

### 1. Create Plugin Project

```bash
cd src
dotnet new classlib -n MCP.Plugins.MyTool
cd MCP.Plugins.MyTool
dotnet add reference ../MCP.Contracts/MCP.Contracts.csproj
dotnet add package Microsoft.CodeAnalysis.VisualBasic.Workspaces
```

### 2. Implement the Interface

```csharp
using MCP.Contracts;

public class MyToolProvider : IRefactoringProvider
{
    public string Name => "MyTool";
    public string Description => "Does something useful";

    public ValidationResult ValidateParameters(JsonElement parameters)
    {
        // Validate required parameters
        return ValidationResult.Success();
    }

    public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
    {
        // Your Roslyn transformation logic here
        return RefactoringResult.Success(context.OriginalSolution);
    }
}
```

### 3. Deploy the Plugin

```bash
dotnet build
cp bin/Debug/net8.0/*.dll ../MCP.RefactoringWorker/plugins/
```

Restart the RefactoringWorker service. Your new tool is now available!

## Testing the Platform

### Run Unit Tests

```bash
dotnet test tests/MCP.Tests/MCP.Tests.csproj
```

### Test with Sample VB.NET Project

1. Place your VB.NET solution in the `workspaces/` directory
2. Update the `solutionPath` in your job request
3. Submit the job
4. Monitor progress in the Hangfire dashboard

## Troubleshooting

### "Cannot connect to SQL Server"

Verify SQL Server is running:
```bash
docker logs mcp-sqlserver
```

Check connection string in `appsettings.json`.

### "Plugin not found"

Check plugin is in the plugins directory:
```bash
ls -la src/MCP.RefactoringWorker/plugins/
```

Check RefactoringWorker logs:
```bash
docker logs mcp-refactoring-worker
```

### "Solution contains no projects"

Ensure the VB.NET solution file path is correct and accessible from the worker container.

## Next Steps

- Read the full [README.md](README.md) for detailed architecture information
- Review the [Architectural Blueprint](docs/architecture-blueprint.md) for technical details
- Explore the example plugin: `src/MCP.Plugins.RenameSymbol/`
- Integrate with your CI/CD pipeline for automated testing

## Support

For issues, questions, or contributions, please refer to the main README or open an issue in the repository.
