# Development Guide

This section contains information for developers who want to contribute to the Schema library or extend it for their own use.

## Getting Started

### [Setup and Building](setup.md)

How to set up your development environment and build the project from source.

### [Project Structure](project-structure.md)

Overview of the codebase organization and key components.

### [Contributing](contributing.md)

Guidelines for contributing code, documentation, and examples.

## Development Topics

### [Testing](testing.md)

How to run tests, write new tests, and ensure code quality.

### [Code Style](code-style.md)

Coding standards, formatting, and best practices used in the project.

### [Architecture](architecture.md)

High-level architecture overview and design decisions.

### [Performance](performance.md)

Performance considerations and optimization techniques.

## Extension and Customization

### [Custom Types](custom-types.md)

How to create and integrate custom types into the schema system.

### [Plugins](plugins.md)

Creating plugins and extensions for the Schema Editor.

### [Code Generation Templates](code-generation.md)

Developing custom code generation templates and targets.

### [Serialization Customization](serialization.md)

Customizing JSON serialization behavior for special requirements.

## Maintenance

### [Release Process](release-process.md)

How releases are created and versioned.

### [Security](security.md)

Security considerations and vulnerability reporting.

### [Dependencies](dependencies.md)

Managing dependencies and keeping them up to date.

### [Documentation](documentation.md)

How to update and maintain project documentation.

## Quick Reference

### Development Environment

| Component  | Requirement                   |
| ---------- | ----------------------------- |
| .NET SDK   | 8.0 or 9.0                    |
| IDE        | Visual Studio 2022 or VS Code |
| Git        | Latest version                |
| PowerShell | 7.0+ (Windows)                |

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Build SchemaEditor
dotnet build SchemaEditor/SchemaEditor.csproj

# Package for release
dotnet pack --configuration Release
```

### Project Structure Overview

```
Schema/
├── Schema/                  # Core library
├── Schema.Test/            # Unit tests
├── SchemaEditor/          # Visual editor application
├── docs/                  # Documentation
├── scripts/               # Build and utility scripts
└── examples/              # Example projects
```

### Key Directories

| Directory       | Purpose                        |
| --------------- | ------------------------------ |
| `Schema/`       | Core schema definition library |
| `Schema.Test/`  | Unit and integration tests     |
| `SchemaEditor/` | ImGui-based visual editor      |
| `docs/`         | Markdown documentation         |
| `scripts/`      | Build automation and utilities |

## Getting Help

### For Contributors

-   **Issues**: Report bugs or request features on GitHub
-   **Discussions**: Join conversations about development topics
-   **Code Review**: Get feedback on pull requests
-   **Mentoring**: Connect with maintainers for guidance

### For Extension Developers

-   **API Documentation**: [API Reference](../api/) for extending the library
-   **Architecture Guide**: [Architecture](architecture.md) for understanding internals
-   **Custom Types**: [Custom Types](custom-types.md) for type system extensions
-   **Examples**: [Examples](../examples/) for implementation patterns

## Development Workflow

### Standard Workflow

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create** a feature branch
4. **Make** your changes
5. **Test** your changes thoroughly
6. **Submit** a pull request

### Detailed Steps

```bash
# Clone your fork
git clone https://github.com/yourusername/Schema.git
cd Schema

# Create feature branch
git checkout -b feature/my-new-feature

# Make changes and commit
git add .
git commit -m "Add new feature"

# Push to your fork
git push origin feature/my-new-feature

# Create pull request via GitHub web interface
```

### Code Quality Checks

Before submitting changes:

```bash
# Run all tests
dotnet test

# Check code formatting
dotnet format --verify-no-changes

# Run static analysis
dotnet build --verbosity normal

# Build documentation
# (Follow docs/development/documentation.md)
```

## Development Best Practices

### Code Quality

-   **Write Tests**: All new features should include tests
-   **Follow Standards**: Use consistent coding style and patterns
-   **Document Changes**: Update documentation for public APIs
-   **Performance**: Consider performance impact of changes

### Git Practices

-   **Small Commits**: Make focused, atomic commits
-   **Clear Messages**: Write descriptive commit messages
-   **Feature Branches**: Use branches for new features
-   **Clean History**: Rebase and squash when appropriate

### Testing Practices

-   **Unit Tests**: Test individual components in isolation
-   **Integration Tests**: Test component interactions
-   **End-to-End Tests**: Test complete workflows
-   **Performance Tests**: Benchmark critical paths

## Release Information

### Current Version: 1.3.2

-   **Stability**: Production ready
-   **API Status**: Stable with semantic versioning
-   **Breaking Changes**: Documented in CHANGELOG.md
-   **Support**: Active development and maintenance

### Version History

| Version | Release Date | Key Features                                |
| ------- | ------------ | ------------------------------------------- |
| 1.3.2   | Current      | Enhanced type system, editor improvements   |
| 1.3.1   | Previous     | Bug fixes and performance improvements      |
| 1.3.0   | Previous     | Major feature release with new capabilities |

### Compatibility

-   **.NET**: 8.0 and 9.0 supported
-   **Platforms**: Windows, macOS, Linux
-   **Dependencies**: ktsu.dev ecosystem libraries
-   **Breaking Changes**: Follow semantic versioning

## Community

### Contributing Areas

We welcome contributions in these areas:

-   **Core Library**: Schema definition and type system improvements
-   **Editor**: Visual editor features and usability enhancements
-   **Documentation**: Guides, examples, and API documentation
-   **Testing**: Test coverage and quality improvements
-   **Performance**: Optimization and benchmarking
-   **Examples**: Real-world usage examples and tutorials

### Getting Recognition

-   **Contributors**: Listed in AUTHORS.md
-   **Major Features**: Highlighted in release notes
-   **Documentation**: Credited in relevant sections
-   **Community**: Featured in project discussions

## See Also

-   **[Getting Started](../getting-started.md)** - Using the library
-   **[API Reference](../api/)** - Detailed API documentation
-   **[Features](../features/)** - Feature guides and capabilities
-   **[Examples](../examples/)** - Usage examples and tutorials
