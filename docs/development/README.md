# Development Guide

This section contains information for developers who want to contribute to the Schema library or extend it for their own use.

## Guides

### [Architecture](architecture.md)

High-level architecture overview and design decisions.

### [Roadmap](../ROADMAP.md)

Current state of the project and planned work.

## Development Environment

| Component | Requirement                                          |
| --------- | ---------------------------------------------------- |
| .NET SDK  | 10.0 (see `global.json`; the library multi-targets net7.0–net10.0) |
| IDE       | Visual Studio 2022 or VS Code                        |
| Git       | Latest version                                       |

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestName"

# Launch the visual editor
dotnet run --project SchemaEditor
```

## Project Structure

| Directory       | Purpose                        |
| --------------- | ------------------------------ |
| `Schema/`       | Core schema definition library |
| `Schema.Test/`  | MSTest unit tests              |
| `SchemaEditor/` | ImGui-based visual editor      |
| `docs/`         | Markdown documentation         |
| `scripts/`      | Build automation (PSBuild)     |

Within the core library:

-   `Schema/Models/` - `Schema`, `SchemaClass`, `SchemaEnum`, `SchemaMember`, `DataSource`, `SchemaCodeGenerator`, and `SchemaSerializer`
-   `Schema/Models/Types/` - The polymorphic type system (`BaseType` and derived types)
-   `Schema/Models/Names/` - Semantic string name types
-   `Schema/Contracts/` - Interfaces describing the schema object model

## Testing

Tests live in `Schema.Test` and use MSTest with the Microsoft.Testing.Platform runner. Run them with `dotnet test`. All new features should include tests; the existing suites (`SchemaTests`, `SchemaClassTests`, `SchemaEnumTests`, `TypeSystemTests`, `SchemaSerializerTests`, `AddClassFromTypeTests`) show the conventions in use.

## Code Style

Code style is enforced at build time by the analyzers configured through `ktsu.Sdk` and the repository `.editorconfig` (tabs for indentation in C#, file-scoped namespaces, XML documentation on public APIs). Run a local build before submitting changes — style violations fail the build.

```bash
# Verify formatting and analyzers
dotnet build

# Auto-fix formatting
dotnet format
```

## Contributing Workflow

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create** a feature branch
4. **Make** your changes, including tests
5. **Build and test** (`dotnet build && dotnet test`)
6. **Submit** a pull request

Contributions are welcome in all areas: the core library, the editor, documentation, tests, and examples. Contributors are listed in [AUTHORS.md](../../AUTHORS.md).

## Releases

Releases are automated through the GitHub Actions workflow (`.github/workflows/dotnet.yml`) using the PSBuild pipeline in `scripts/`. Versioning is derived from commit history (`[major]`/`[minor]` markers in commit messages; patch otherwise), the changelog is generated automatically, and packages publish to NuGet. Breaking changes follow semantic versioning and are documented in [CHANGELOG.md](../../CHANGELOG.md).

## See Also

-   **[Getting Started](../getting-started.md)** - Using the library
-   **[API Reference](../api/README.md)** - Detailed API documentation
-   **[Features](../features/README.md)** - Feature guides and capabilities
-   **[Examples](../examples/README.md)** - Usage examples and tutorials
