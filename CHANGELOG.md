## v1.7.3 (patch)

Changes since v1.7.2:

- Bump the ktsu group with 16 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync global.json ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Bump the ktsu group with 7 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\dependabot.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync icon.png ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitignore ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitattributes ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync global.json ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Bump the ktsu group with 4 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump Polyfill from 10.10.0 to 10.11.0 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump the ktsu group with 3 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump the ktsu group with 10 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))

## v1.7.2 (patch)

Changes since v1.7.1:

- Bump the ktsu group with 3 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))

## v1.7.1 (patch)

Changes since v1.7.0:

- Remove stale files ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.7.0 (minor)

Changes since v1.6.0:

- Fix empty member type picker by offering all available types ([@Claude](https://github.com/Claude))
- Update package refs: remove dev deps, add Hexa.NET.ImGui ([@matt-edmondson](https://github.com/matt-edmondson))
- Move the views tab bar into the right divider zone ([@Claude](https://github.com/Claude))
- Fix editor content rendering over the tab bar ([@Claude](https://github.com/Claude))

## v1.6.2 (patch)

Changes since v1.6.1:

- Update package refs: remove dev deps, add Hexa.NET.ImGui ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.6.1 (patch)

Changes since v1.6.0:

- Move the views tab bar into the right divider zone ([@Claude](https://github.com/Claude))
- Fix editor content rendering over the tab bar ([@Claude](https://github.com/Claude))

## v1.6.0 (minor)

Changes since v1.5.0:

- Update package versions and refactor type references in schema models ([@matt-edmondson](https://github.com/matt-edmondson))
- Add class graph view to SchemaEditor using NodeGraph and ImGuiNodeEditor ([@Claude](https://github.com/Claude))

## v1.5.0 (minor)

Changes since v1.4.0:

- Merge remote-tracking branch 'origin/main' into feature/ktsu-ecosystem-integration ([@Claude](https://github.com/Claude))
- Fix analyzer errors in collection test data classes ([@Claude](https://github.com/Claude))
- Refresh documentation to match the current v1.4 API ([@Claude](https://github.com/Claude))
- Add schema validation API and fix lossy reflection type mappings ([@Claude](https://github.com/Claude))
- Fold owner decisions into roadmap ([@Claude](https://github.com/Claude))
- Add project roadmap based on codebase analysis ([@Claude](https://github.com/Claude))
- Add TAGS.md with NuGet package tags ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove legacy build scripts ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Add keyboard shortcuts for common editor operations ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Integrate ktsu.UndoRedo for undoable schema mutations ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Replace manual debounce with ktsu.IntervalAction ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Add ktsu ecosystem package references ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.4.1 (patch)

Changes since v1.4.0:

- Fix analyzer errors in collection test data classes ([@Claude](https://github.com/Claude))
- Refresh documentation to match the current v1.4 API ([@Claude](https://github.com/Claude))
- Add schema validation API and fix lossy reflection type mappings ([@Claude](https://github.com/Claude))
- Fold owner decisions into roadmap ([@Claude](https://github.com/Claude))
- Add project roadmap based on codebase analysis ([@Claude](https://github.com/Claude))
- Add TAGS.md with NuGet package tags ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.4.0 (minor)

Changes since v1.3.0:

- Refactor Schema Library Documentation and Examples ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Add CodeGenerator tree, DataSource property panel, and use SchemaSerializer in editor ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Add SchemaSerializer, JSON serialization support, and comprehensive test suite ([@matt-edmondson](https://github.com/matt-edmondson))
- [minor] Complete DataSource and CodeGenerator models, fix Reassociate ([@matt-edmondson](https://github.com/matt-edmondson))
- Update README.md with comprehensive project details and usage examples ([@matt-edmondson](https://github.com/matt-edmondson))
- compatibility suppressions ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package versions, refactor classes, and add file persistence functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- migrate to dotnet 10 ([@matt-edmondson](https://github.com/matt-edmondson))
- Add CLAUDE.md for project guidance and documentation ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references and restructure project files for improved organization and clarity ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package versions in Directory.Packages.props, including ktsu.StrongPaths to 1.3.2 and MSTest packages to 3.9.3, while adding XML declaration for improved compatibility. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor schema management by introducing a new Schema class and related models, replacing the previous SchemaProvider and related interfaces. This update enhances type safety and aligns with the latest design principles, removing obsolete classes and improving the overall structure of the schema definitions. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add schema contract interfaces for schema definitions, including ISchema, ISchemaChild, ISchemaClass, ISchemaEnum, and related name interfaces. This introduces a structured approach to schema management without serialization concerns. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update tests to use new SchemaProvider API - remove dependency on obsolete Schema class ([@matt-edmondson](https://github.com/matt-edmondson))
- Complete Schema refactoring to DI SchemaProvider - remove serialization and filesystem concerns ([@matt-edmondson](https://github.com/matt-edmondson))
- Replace StrongStrings and StrongPaths with ktsu.Semantics across the codebase, updating relevant class constraints and references to use ISemanticString. This change enhances type safety and aligns with the latest library standards. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add .cursorignore for SpecStory backup files, update Directory.Packages.props with new package versions, enhance derived-cursor-rules.mdc with project documentation and debugging guidelines, and refactor SchemaEditor for updated ImGuiApp API usage. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project SDK from ktsu.Sdk.App to ktsu.Sdk.ImGuiApp in SchemaEditor.csproj ([@matt-edmondson](https://github.com/matt-edmondson))
- Add .cursorindexingignore to exclude SpecStory auto-save files, update .editorconfig for conditional expression preferences, and modify .gitignore for SpecStory project files. Introduce Directory.Packages.props for centralized package version management and global.json for SDK versioning. Update .runsettings for coverage configuration and enhance documentation structure with new markdown files. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references and add XML declaration in project files ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove Directory.Build.props and Directory.Build.targets files; add copyright notices to schema files; refactor variable declarations for consistency. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project SDK references and README formatting ([@matt-edmondson](https://github.com/matt-edmondson))
- Update packages ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.3.4 (patch)

Changes since v1.3.3:

- compatibility suppressions ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.3.4-pre.7 (prerelease)

Changes since v1.3.4-pre.6:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\update-winget-manifests.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.4-pre.6 (prerelease)

Changes since v1.3.4-pre.5:

- Sync scripts\update-winget-manifests.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync global.json ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.4-pre.5 (prerelease)

Changes since v1.3.4-pre.4:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync global.json ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.4-pre.4 (prerelease)

Changes since v1.3.4-pre.3:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync global.json ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.4-pre.3 (prerelease)

Changes since v1.3.4-pre.2:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync global.json ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync COPYRIGHT.md ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.4-pre.2 (prerelease)

Changes since v1.3.4-pre.1:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.4-pre.1 (prerelease)

No significant changes detected since v1.3.4.

## v1.3.3 (patch)

Changes since v1.3.2:

- Remove .github\workflows\project.yml ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.3.2 (patch)

Changes since v1.3.1:

- Update package versions, refactor classes, and add file persistence functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- migrate to dotnet 10 ([@matt-edmondson](https://github.com/matt-edmondson))
- Add CLAUDE.md for project guidance and documentation ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references and restructure project files for improved organization and clarity ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package versions in Directory.Packages.props, including ktsu.StrongPaths to 1.3.2 and MSTest packages to 3.9.3, while adding XML declaration for improved compatibility. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor schema management by introducing a new Schema class and related models, replacing the previous SchemaProvider and related interfaces. This update enhances type safety and aligns with the latest design principles, removing obsolete classes and improving the overall structure of the schema definitions. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add schema contract interfaces for schema definitions, including ISchema, ISchemaChild, ISchemaClass, ISchemaEnum, and related name interfaces. This introduces a structured approach to schema management without serialization concerns. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update tests to use new SchemaProvider API - remove dependency on obsolete Schema class ([@matt-edmondson](https://github.com/matt-edmondson))
- Complete Schema refactoring to DI SchemaProvider - remove serialization and filesystem concerns ([@matt-edmondson](https://github.com/matt-edmondson))
- Replace StrongStrings and StrongPaths with ktsu.Semantics across the codebase, updating relevant class constraints and references to use ISemanticString. This change enhances type safety and aligns with the latest library standards. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add .cursorignore for SpecStory backup files, update Directory.Packages.props with new package versions, enhance derived-cursor-rules.mdc with project documentation and debugging guidelines, and refactor SchemaEditor for updated ImGuiApp API usage. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project SDK from ktsu.Sdk.App to ktsu.Sdk.ImGuiApp in SchemaEditor.csproj ([@matt-edmondson](https://github.com/matt-edmondson))
- Add .cursorindexingignore to exclude SpecStory auto-save files, update .editorconfig for conditional expression preferences, and modify .gitignore for SpecStory project files. Introduce Directory.Packages.props for centralized package version management and global.json for SDK versioning. Update .runsettings for coverage configuration and enhance documentation structure with new markdown files. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references and add XML declaration in project files ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove Directory.Build.props and Directory.Build.targets files; add copyright notices to schema files; refactor variable declarations for consistency. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project SDK references and README formatting ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.3.2-pre.2 (prerelease)

Changes since v1.3.2-pre.1:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .editorconfig ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .runsettings ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.3.2-pre.1 (prerelease)

No significant changes detected since v1.3.2.

## v1.3.1 (patch)

Changes since v1.3.0:

- Update packages ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.3.0 (minor)

Changes since v1.2.0:

- Add LICENSE template ([@matt-edmondson](https://github.com/matt-edmondson))
- Update packages ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.2.1 (patch)

Changes since v1.2.0:

- Update packages ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.2.1-pre.2 (prerelease)

Changes since v1.2.1-pre.1:

- Sync scripts\make-version.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\make-changelog.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.2.1-pre.1 (prerelease)

No significant changes detected since v1.2.1.

## v1.2.0 (minor)

Changes since v1.1.0:

- Use the .As() extension method on strong strings instead of casting ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.1.0 (minor)

Changes since v1.0.0:

- Add schema editor prototype ([@matt-edmondson](https://github.com/matt-edmondson))
- Dont serialize the readonly collection wrappers ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.0

Changes since v1.0.0-pre.15:

- csharp_style_var_when_type_is_apparent = true:error ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.0-pre.15 (prerelease)

Changes since v1.0.0-pre.14:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))

## v1.0.0-pre.14 (prerelease)

Changes since v1.0.0-pre.13:

- Sync scripts\make-version.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\make-changelog.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.0.0-pre.13 (prerelease)

Changes since v1.0.0-pre.12:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.0.0-pre.12 (prerelease)

Changes since v1.0.0-pre.11:

- Sync scripts\make-version.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\make-changelog.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.0.0-pre.11 (prerelease)

Changes since v1.0.0-pre.10:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.0.0-pre.10 (prerelease)

Changes since v1.0.0-pre.9:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.0.0-pre.9 (prerelease)

Changes since v1.0.0-pre.8:


## v1.0.0-pre.8 (prerelease)

Changes since v1.0.0-pre.7:


## v1.0.0-pre.7 (prerelease)

Changes since v1.0.0-pre.6:

- Bump MSTest from 3.7.2 to 3.7.3 ([@dependabot[bot]](https://github.com/dependabot[bot]))

## v1.0.0-pre.6 (prerelease)

Changes since v1.0.0-pre.5:


## v1.0.0-pre.5 (prerelease)

Changes since v1.0.0-pre.4:


## v1.0.0-pre.4 (prerelease)

Changes since v1.0.0-pre.3:

- Bump MSTest from 3.7.1 to 3.7.2 ([@dependabot[bot]](https://github.com/dependabot[bot]))

## v1.0.0-pre.3 (prerelease)

Changes since v1.0.0-pre.2:

- Bump coverlet.collector from 6.0.3 to 6.0.4 ([@dependabot[bot]](https://github.com/dependabot[bot]))

## v1.0.0-pre.2 (prerelease)

Changes since v1.0.0-pre.1:


## v1.0.0-pre.1 (prerelease)

No significant changes detected since v1.0.0.

## v0.0.1-pre.1 (prerelease)

- Change .editorconfig end_of_line from lf to crlf ([@matt-edmondson](https://github.com/matt-edmondson))
- Delete Schema/SchemaLib.csproj ([@matt-edmondson](https://github.com/matt-edmondson))
- epic strict editorconfig ([@matt-edmondson](https://github.com/matt-edmondson))
- Code style ([@matt-edmondson](https://github.com/matt-edmondson))
- Whitespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Whitespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor Schema class access modifiers and add extensions ([@matt-edmondson](https://github.com/matt-edmondson))
- Whitespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial commit ([@matt-edmondson](https://github.com/matt-edmondson))

