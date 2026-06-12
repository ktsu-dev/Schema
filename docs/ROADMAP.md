# Project Roadmap

This roadmap captures the current state of ktsu.Schema (v1.4.0) and the work required to
complete the project's stated goals: a schema definition library with a rich type system,
a visual editor, code generation, and data source management.

## Current State Assessment

### What works today

- **Core model (Schema)** — Classes, enums, members, and a polymorphic type system
  (18 types) with full CRUD, parent/child association, and `Reassociate()` after load.
- **Serialization** — `SchemaSerializer` round-trips schemas to `.schema.json` via
  `System.Text.Json` with `TypeName` discriminators.
- **Reflection import** — `Schema.AddClass(Type)` builds schema classes from .NET types
  for primitives, enums, and nested classes.
- **Test suite** — 121 passing MSTest tests covering the model, type system,
  serialization, and reflection import.
- **Editor (SchemaEditor)** — Tree navigation for classes/enums/data sources/code
  generators, member type editing, array container/key configuration, data source
  property panel, file new/open/save, persistent layout settings.
- **CI/CD** — GitHub Actions pipeline (PSBuild) with build, test, NuGet publishing, and
  winget manifest updates; Dependabot with auto-merge.

### Gaps

Two of the four schema element kinds are metadata shells with no behavior behind them:

- **Code generation** — `SchemaCodeGenerator` stores only a name and an output path.
  No code is ever generated, despite code generation being a headline feature in the
  README and package tags.
- **Data sources** — `DataSource` stores a file path and a class reference, but nothing
  loads, validates, or edits the referenced data.

Cross-cutting gaps:

- **No schema validation** — Nothing checks referential integrity: an `Enum` type can
  reference a deleted enum, an `Object` type a deleted class, an `Array` key a removed
  member. Renames and deletes silently break references.
- **Stale documentation** — `docs/README.md` reports v1.3.2 and documents removed APIs
  (`Schema.TryLoad`, `schema.Save()`, `SchemaProvider`, `ktsu.StrongPaths`/`StrongStrings`),
  and links to pages that don't exist (`development/contributing.md`,
  `development/testing.md`). Four docs files reference obsolete APIs.

## Phase 1 — Foundation hardening

Goal: the library is trustworthy as a dependency; what's documented matches what exists.

1. **Schema validation API**
   - `Schema.Validate()` returning structured diagnostics (error/warning, element path,
     message).
   - Rules: enum/class/member references resolve; array keys point at existing primitive
     members of the element class; duplicate names; data source class references resolve.
   - Unit tests for every rule.
2. **Reflection import correctness** (`Schema.AddClass(Type)`)
   - `long` currently maps to `Int` and `double`/`decimal` to `Float`, despite `Long`
     and `Double` existing — fix lossy mappings.
   - Handle `DateTime`, `TimeSpan`, nullable value types, arrays, `List<T>`,
     `Dictionary<TKey,TValue>`, and `System.Numerics` vector types; today these fall
     through to `None` or are skipped.
   - Tests for each mapping.
3. **Documentation refresh**
   - Rewrite `docs/README.md`, `docs/api/schema-core.md`, `docs/examples/*` against the
     v1.4 API (root `README.md` is already accurate and can be the reference).
   - Add or unlink `development/contributing.md` and `development/testing.md`.
   - Add a link-check to CI or a docs test to prevent re-rot.

## Phase 2 — Editor completeness

Goal: every model capability is editable in the editor, and the editor behaves like a
proper desktop app. (PR #6 — undo/redo, keyboard shortcuts, auto-save debounce — overlaps
heavily with this phase and should be reviewed/merged first.)

1. **Editing parity with the model**
   - Rename classes, enums, members, enum values, data sources, and code generators
     (model `Rename()` exists; the editor exposes no rename anywhere — member name fields
     are read-only).
   - Edit `Description`/`MemberDescription` (model supports them; editor never shows them).
   - Code generator property panel (only data sources have one today).
   - Member reordering.
2. **File-handling robustness**
   - "Save As" menu item (currently only reachable when the path is empty).
   - Dirty-state tracking: title-bar indicator and unsaved-changes prompt on
     New/Open/exit.
   - Window title updates when a schema is opened (currently computed once at startup).
   - Recent-files menu.
   - Cross-platform "Open Externally" (hardcoded `explorer.exe` fails on Linux/macOS).
3. **Validation surfacing**
   - Run Phase 1 validation live; show diagnostics inline (broken type references
     highlighted in the tree and member grid).

## Phase 3 — Code generation

Goal: `SchemaCodeGenerator` does what its name says. This is the largest missing feature
and the main reason the project exists ("provides a foundation for code generation").

1. **Generator architecture**
   - `ISchemaCodeGenerator` abstraction in the core library; generators consume a
     validated `Schema` and emit files to `OutputPath`.
   - Extend the `SchemaCodeGenerator` model with the settings a real generator needs
     (language/template id, namespace, naming conventions).
2. **First-party C# generator**
   - POCO classes, enums, and container mappings (`vector` → `List<T>`,
     `map` → `Dictionary<TKey,TValue>` keyed by the array `Key` member).
   - Round-trip guarantee: generated types re-imported via `AddClass(Type)` produce an
     equivalent schema (this becomes the integration test).
3. **Invocation paths**
   - CLI entry point (`dotnet schema generate <file>`) or MSBuild task for build-time
     generation.
   - "Generate" action in the editor.
4. **Candidate follow-ons** (sequence by demand)
   - Additional languages (C++ was the historic motivation for `ContainerName`-style
     containers).
   - JSON Schema (draft 2020-12) export/import for ecosystem interop.

## Phase 4 — Data sources

Goal: `DataSource` becomes functional — schema-validated data files.

1. **Data loading/validation** — Load the JSON file a `DataSource` points at and validate
   it against the referenced class (required members, type conformance, enum values,
   container shapes).
2. **Editor data view** — Show validation results for a data source; stretch: a
   schema-driven data editor grid.
3. **Path resolution** — `RelativeFilePath` needs an anchor; define resolution relative
   to the `.schema.json` location and document it.

## Phase 5 — Release & distribution

Goal: the editor reaches users, not just the library.

1. **Editor packaging** — Publish SchemaEditor per-platform (the winget manifest script
   exists but the editor currently ships nowhere); decide Windows-only vs cross-platform
   support and test on Linux/macOS if the latter.
2. **Schema format stability** — Document the `.schema.json` format, add a format
   version field, and define a migration policy before third parties depend on it.
3. **v2.0 milestone** — Validation + codegen + functional data sources constitutes the
   "complete" project; cut v2.0 when Phases 1–4 land.

## Suggested sequencing

| Order | Work item | Phase | Effort | Rationale |
| ----- | --------------------------------------- | ----- | ------ | --------------------------------------------- |
| 1 | Review/merge PR #6 | 2 | S | Already written; unblocks editor work |
| 2 | Schema validation API | 1 | M | Prerequisite for codegen, data sources, editor diagnostics |
| 3 | Reflection import fixes | 1 | S | Known lossy-mapping bugs |
| 4 | Docs refresh | 1 | S | Actively misleading today |
| 5 | Editor parity (rename/descriptions) | 2 | M | Biggest day-to-day editor gap |
| 6 | Editor file-handling robustness | 2 | S | Data-loss guardrails |
| 7 | Codegen architecture + C# generator | 3 | L | Headline missing feature |
| 8 | Data source validation | 4 | M | Completes the last shell model |
| 9 | Editor packaging + format versioning | 5 | M | Ship it |

## Open questions

These determine scope and ordering; answers will be folded into this document:

1. **Codegen targets** — Is C# the only required output, or is C++ (the historic
   container/key design driver) also in scope?
2. **Data source intent** — Validation of external data files, or full data *editing*
   in SchemaEditor?
3. **Editor platforms** — Is SchemaEditor Windows-first (winget script suggests so) or
   should Linux/macOS be supported and tested?
4. **JSON Schema interop** — Is import/export to standard JSON Schema a goal, or is the
   proprietary format intentional?
