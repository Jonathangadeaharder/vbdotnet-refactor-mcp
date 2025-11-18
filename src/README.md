# Source Code

This directory contains all production code for the Mass Code Platform.

## Structure

- **MCP.Contracts**: Plugin interface definitions (zero dependencies)
- **MCP.Core**: Shared models and services
- **MCP.ApiGateway**: REST API entry point
- **MCP.RefactoringWorker**: Roslyn transformation executor
- **MCP.ValidationWorker**: Git + Compilation + CI/CD validator
- **MCP.Plugins.***: Refactoring plugin implementations

## Architectural Rules

Each component has clear dependency boundaries:
- Contracts: No dependencies
- Core: Depends only on Contracts
- Workers/Gateway: Depend on Core + Contracts
- Plugins: Depend only on Contracts

These rules are enforced by structurelint.
