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
| .NET SDK  | 10.0.400 or newer (see `global.json`; the library multi-targets net8.0–net10.0) |
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
| `Schema/`             | Core schema definition library      |
| `Schema.Test/`        | MSTest unit tests for the library   |
| `SchemaEditor/`       | ImGui-based visual editor           |
| `SchemaEditor.Test/`  | Headless UI tests for the editor    |
| `SchemaTool/`         | Command line validator and generator |
| `docs/`               | Markdown documentation              |
| `scripts/`            | Build automation (PSBuild)          |

Within the core library:

-   `Schema/Models/` - `Schema`, `SchemaClass`, `SchemaEnum`, `SchemaMember`, `DataSource`, `SchemaCodeGenerator`, and `SchemaSerializer`
-   `Schema/Models/Types/` - The polymorphic type system (`BaseType` and derived types)
-   `Schema/Models/Names/` - Semantic string name types
-   `Schema/Contracts/` - Interfaces describing the schema object model

## Testing

Both suites use MSTest with the Microsoft.Testing.Platform runner, and `dotnet test` runs them
together. All new features should include tests.

### The library — `Schema.Test`

Plain unit tests over the core library, run against every framework it publishes. The existing
suites (`SchemaTests`, `SchemaClassTests`, `SchemaEnumTests`, `TypeSystemTests`,
`SchemaSerializerTests`, `AddClassFromTypeTests`) show the conventions in use.

### The editor — `SchemaEditor.Test`

The editor's code is immediate-mode draw calls, so none of it executes without a live ImGui
context. `ktsu.ImGui.App.Testing` supplies one with no window, no display and no GPU: it rasterizes
in software and injects input straight into ImGui's event queue, so these tests run unchanged on a
headless continuous integration runner and disturb nothing on a desktop.

Two fixtures wrap it:

-   **`EditorHarness`** starts a real editor from `EditorHost.CreateConfig`, the same configuration
    the application itself starts with, so a callback renamed or dropped there breaks the tests
    rather than leaving them passing against a host that no longer exists. It redirects
    `AppDataStorage` to an in-memory file system first, so a test never reads or writes the
    settings of whoever is running it.
-   **`WidgetHarness`** draws one widget and nothing else, for widget-level behaviour such as
    `EditField`.

Widgets are addressed by name. The editor marks its own items through `ktsu.ImGui.Probes` — a
dependency-free package whose whole purpose is that an application, a widget library and a dialog
library can each mark items without depending on one another. Marking costs nothing when no probe
is installed, which is every run that is not a test. The marks are deliberately few and central:
one in `ButtonTree` covers every row of every tree, and a probe scope per member row keeps two
rows' controls apart the same way `PushID` does for ImGui itself. A test then clicks
`App.Click("BtnUser")` or `App.Click("memberAge/Delete")` and never states a coordinate.

Frames are advanced by the test, never by wall-clock time — `Step(n)` for a fixed number and
`StepUntil(condition, budget)` for a wait — so a loaded runner is slower rather than flakier.
`App.Capture().SavePng(path)` writes the rendered frame to disk, which is how a visual change is
checked without a display: render before, render after, and look at the two images.

Where a regression is only visible on screen,
`App.Capture()` gives the rendered pixels: `TwoRowsSharingALabelDoNotShareABuffer` compares one
row's pixels before and during an edit of its sibling, which is the only place the shared-buffer
bug it guards against is observable at all.

Note that a modal sizes itself on the frame it appears and is centred on the next, so a button's
rectangle is not final until a few frames after it is first recorded. Step past that before
clicking.

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
