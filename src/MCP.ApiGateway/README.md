# MCP.ApiGateway

## Overview

REST API service that receives refactoring job requests and queues them for asynchronous processing via Hangfire.

## Responsibilities

- Accept HTTP POST requests for refactoring jobs
- Validate request parameters
- Submit jobs to Hangfire queue
- Provide job status polling endpoints
- Host Hangfire dashboard UI

## API Endpoints

### POST /api/v1/refactoringjobs
Submit a new refactoring job.

**Request Body:**
```json
{
  "solutionPath": "/path/to/solution.sln",
  "refactoringToolName": "RenameSymbol",
  "parameters": { ... },
  "validationPolicy": { ... }
}
```

**Response:** `202 Accepted` with job ID

### GET /api/v1/refactoringjobs/{id}
Poll job status.

**Response:**
```json
{
  "jobId": "abc-123",
  "status": "Running|Succeeded|Failed",
  "message": "...",
  "executionLog": [...]
}
```

## Configuration

See `appsettings.json` for:
- Hangfire SQL Server connection string
- Logging configuration
- CORS policies

## Deployment

Lightweight, I/O-bound service. Can run multiple instances behind a load balancer.

**Default Port:** 5000

## Monitoring

- **Hangfire Dashboard:** http://localhost:5000/hangfire
- **Health Check:** http://localhost:5000/health
