# Project Roadmap

This roadmap captures the current state of ktsu.Schema and the work left to complete the
project's stated goals: a schema definition library with a rich type system, a visual editor,
code generation, and data source management.

Version and test-count figures are deliberately absent — they were the first things to rot last
time. For what the library actually does, the tests in
[`Schema.Test`](../Schema.Test) are the live answer; for what is left, the
[open issues](https://github.com/ktsu-dev/Schema/issues) are.

## Current state

### What works today

- **Core model** — Classes, enums, members, and a polymorphic type system with full CRUD,
  parent/child association, and `Reassociate()` after load. Vector and colour types are their own
  branch of the type hierarchy rather than class references, so `IsObject` means "references a
  user-defined class".
- **Serialization** — `SchemaSerializer` round-trips schemas to `.schema.json`, with a
  `formatVersion` field, migration of older files, and a distinguishable error for a file written
  by a newer version. The format is specified in [`schema-format.md`](schema-format.md).
- **Validation** — `Schema.Validate()` returns structured diagnostics (severity, element path,
  message, and a reference to the offending element). It covers dangling enum and class
  references, array key rules, duplicate names, empty names, untyped members, container
  vocabulary, keyed maps without a key, and data sources pointing at files that are not there.
- **Reflection import** — `Schema.AddClass(Type)` handles primitives, `long`/`double`/`decimal`,
  `DateTime`, `TimeSpan`, nullable value types, arrays, `IEnumerable`, `IDictionary`, enums,
  nested classes, `System.Numerics` vectors, and the library's colour types.
- **Code generation** — `ISchemaCodeGenerator` plus a first-party C# generator emitting POCOs,
  enums, container mappings and XML doc comments. Generation is refused for a schema with
  validation errors. Runnable from the editor and from `SchemaTool`.
- **Data sources** — Relative paths resolve against the schema file's own directory, and
  `SchemaDataValidator` checks a bound data file against its class.
- **Editor** — Tree navigation, rename with reference cascade, descriptions, member reordering,
  type and container editing, code generator configuration and a Generate action, a diagnostics
  panel with click-to-navigate, undo/redo across every edit, Save As, dirty tracking, and a
  recent-files list.
- **CLI** — `SchemaTool` validates a schema or runs its code generators, exiting non-zero on
  errors so it can gate a build.
- **Editor tests** — `SchemaEditor.Test` drives the editor headlessly through
  `ktsu.ImGui.App.Testing`, which rasterizes in software and injects input straight into ImGui, so
  the editor's real draw code runs on a continuous integration runner with no window or display.
- **CI/CD** — GitHub Actions with build, multi-framework test, SonarCloud analysis, CodeQL, NuGet
  publishing and winget manifest updates; Dependabot with auto-merge.

### What is left

The full list is in the [issue tracker](https://github.com/ktsu-dev/Schema/issues). The
substantial items:

| Issue | Work | Why it is not done |
| --- | --- | --- |
| [#110](https://github.com/ktsu-dev/Schema/issues/110) | Implement or delete the unused `Schema.Contracts` API | Needs a decision from the project owner; deleting is a breaking change |
| [#126](https://github.com/ktsu-dev/Schema/issues/126) | Generated data editors | Builds on the generator architecture |
| [#127](https://github.com/ktsu-dev/Schema/issues/127) | Generated data migrations | Needs a schema diff, which does not exist yet |

## Phase status

The phases below were the original plan. They are kept for continuity, marked with where each one
actually stands.

### Phase 1 — Foundation hardening — **done**

Validation, reflection import correctness, and the documentation refresh have all landed. The
build blocker that made the repository unbuildable on a current SDK
([#123](https://github.com/ktsu-dev/Schema/issues/123)) is fixed, and the test suite now runs
against every published target framework rather than just the newest.

### Phase 2 — Editor completeness — **done**

Renaming, descriptions, the code generator panel, member reordering, validation surfacing, undo
coverage, Save As, dirty tracking and recent files have landed. Cross-platform "Open Externally"
is fixed.

The window title and the close prompt ([#116](https://github.com/ktsu-dev/Schema/issues/116)) were
blocked on `ktsu.ImGui.App` exposing neither a settable title nor a cancellable close hook; both
landed upstream, and the editor now keeps its title current and refuses a close that would discard
unsaved work. The document name and dirty marker are still drawn in the menu bar as well, because a
maximised title bar is easy to overlook and a tiling window manager may not draw one at all.

### Phase 3 — Code generation — **done, less the deferred targets**

The generator architecture, the C# generator, the editor action and the CLI have landed. The
generate → compile → reimport round trip is an automated test, which is what keeps the generator's
type mapping and the reflection importer's from drifting apart.

Still deferred by the recorded decision below: additional languages, and JSON Schema interop.

### Phase 4 — Data sources — **path resolution and validation done**

Relative paths now resolve against the schema file, and a bound data file can be validated against
its class. Generated data editors and generated migrations are
[#126](https://github.com/ktsu-dev/Schema/issues/126) and
[#127](https://github.com/ktsu-dev/Schema/issues/127).

### Phase 5 — Release & distribution — **format stability done**

The `.schema.json` format is documented and versioned with a stated compatibility policy.

Outstanding: editor packaging via winget, and cutting the v2.0 milestone.

### Phase 6 — Test coverage — **the editor is now testable**

Not one of the original phases; added when the editor grew large enough to need one.

`SchemaEditor.Test` ([#128](https://github.com/ktsu-dev/Schema/issues/128)) drives the editor
headlessly and covers the recent-files list, the commit-once text field, the unsaved-changes guard
and the save-then-continue sequence, and validation debouncing and click-to-navigate. The
SonarCloud coverage exclusion has narrowed from the whole application to the panel and tree files,
which are still pure draw code.

## What to do next

| Order | Work item | Effort | Rationale |
| ----- | --- | --- | --- |
| 1 | Decide [#110](https://github.com/ktsu-dev/Schema/issues/110): implement or delete `Schema.Contracts` | S | A decision, not a build. It is public API on a published package that nothing implements, and `docs/examples/dependency-injection.md` documents it as though it works |
| 2 | [#126](https://github.com/ktsu-dev/Schema/issues/126): generated data editors | L | The first thing the data source binding was for |
| 3 | Extend `SchemaEditor.Test` to the panel and tree files | M | The harness exists; those files are what it does not reach yet, and they are the ones still excluded from coverage |
| 4 | [#127](https://github.com/ktsu-dev/Schema/issues/127): generated migrations | L | Needs a schema diff first; the largest remaining design problem |
| 5 | Editor packaging and the v2.0 milestone | M | Ship it |

## Decisions

Resolved with the project owner (2026-06):

1. **Codegen targets** — C# first. C++ and other languages deferred until there is demand.
2. **Data source intent** — `DataSource` tracks which data files reference which schema classes so
   that editors and migrations can be code-generated for them.
3. **Editor platforms** — Windows-first, shipped via winget; code stays defensively
   cross-platform but Linux/macOS are not tested or packaged.
4. **JSON Schema interop** — Remains open; treated as a demand-driven follow-on rather than a goal.

Made while implementing, and open to revision:

5. **Renames cascade.** Renaming a class or enum repoints every reference to it, rather than being
   blocked while references exist or allowed to dangle. It is the only option that neither loses
   work nor knowingly breaks the schema.
6. **A data file's root** is either one object of the bound class or an array of them, and every
   member of a class is required, since the schema has no notion of an optional member.
7. **Colour types live in the library** (`ktsu.Schema.Runtime`), because the base class library has
   no counterpart and a generator inventing its own would break the reimport round trip.
