## v1.3.2 (patch)

Changes since v1.3.1:

- Update project SDK from ktsu.Sdk.App to ktsu.Sdk.ImGuiApp in SchemaEditor.csproj ([@matt-edmondson](https://github.com/matt-edmondson))
- Replace StrongStrings and StrongPaths with ktsu.Semantics across the codebase, updating relevant class constraints and references to use ISemanticString. This change enhances type safety and aligns with the latest library standards. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update tests to use new SchemaProvider API - remove dependency on obsolete Schema class ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references and add XML declaration in project files ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor schema management by introducing a new Schema class and related models, replacing the previous SchemaProvider and related interfaces. This update enhances type safety and aligns with the latest design principles, removing obsolete classes and improving the overall structure of the schema definitions. ([@matt-edmondson](https://github.com/matt-edmondson))
- Complete Schema refactoring to DI SchemaProvider - remove serialization and filesystem concerns ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove Directory.Build.props and Directory.Build.targets files; add copyright notices to schema files; refactor variable declarations for consistency. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references and restructure project files for improved organization and clarity ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project SDK references and README formatting ([@matt-edmondson](https://github.com/matt-edmondson))
- Add schema contract interfaces for schema definitions, including ISchema, ISchemaChild, ISchemaClass, ISchemaEnum, and related name interfaces. This introduces a structured approach to schema management without serialization concerns. ([@matt-edmondson](https://github.com/matt-edmondson))
