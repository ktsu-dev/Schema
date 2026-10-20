# Multi-schema references: design

Status: proposed, for issue #302. Nothing here is implemented yet. It is written down first because
three pieces of work meet at this boundary: references between schemas (#302), generation leaving
earlier output behind (#288), and generated migrations (#127). Each of them needs the same answer to
one question: which elements a schema *owns*, and which it only *refers to*.

## The file format

A schema names the schemas it builds on in a new top-level property:

```json
{
  "formatVersion": 8,
  "references": [
    { "path": "../carbonmonoxide/carbonmonoxide.schema.json", "alias": "CarbonMonoxide" }
  ],
  "classes": [
    {
      "name": "Dungeon",
      "members": [
        { "name": "Spawn", "type": { "TypeName": "Object", "className": "CarbonMonoxide.SpawnPoint" } }
      ]
    }
  ]
}
```

- `path` is a `RelativeFilePath` resolved against the referencing schema's own directory. That is how
  `dataSources[].file` and `codeGenerators[].outputPath` already resolve (`Schema.TryResolvePath`), so
  a schema can still be moved together with its neighbours.
- `alias` is optional. Without one, a referenced name is written bare (`SpawnPoint`). With one, a
  name may also be written qualified (`CarbonMonoxide.SpawnPoint`). Qualification exists so that a
  schema can still name both types when two referenced schemas declare the same name. It is not
  required otherwise, which keeps the migration of the legacy samples a one-line change per file.
- `references` is omitted when empty, so a file that references nothing is written exactly as it is
  today. Because the property is additive, `formatVersion` goes up by one (from 7 to 8), following the existing
  migration policy in `docs/schema-format.md`. An older reader that meets the property refuses the
  file rather than silently dropping the references.

## Resolution

`Schema.GetClass`, `GetEnum`, `GetSemanticType` and `GetInterface`, and the lookup behind
`Schema.ErrorType`, search in this order:

1. The schema's own declarations.
2. Each referenced schema, in declaration order. The search is not transitive: a reference does
   not bring in the references of the referenced schema. A schema has to say what it uses, which is
   also what keeps a generated file's imports predictable.

A qualified name (`Alias.Name`) skips step 1 and looks only in the aliased schema.

Loading becomes `SchemaSerializer.Load(path)`, which loads the referenced files as well, through a
`SchemaSet` that caches by absolute path. Two schemas that both reference `shared.schema.json` share
one instance of it, so object identity can be used for "is this the same class".
`SchemaSerializer.TryDeserialize(string)` has no anchor directory. It keeps loading a single schema,
and that schema reports any reference it cannot resolve.

## Validation

All of these are reported as errors, at the path of the reference that caused them:

| Case | Message names |
|---|---|
| The referenced file does not exist, or does not parse | The resolved path and the reason |
| The referenced file has errors of its own | The file, and the count of its errors (they are not repeated) |
| A cycle (`a -> b -> a`) | The whole cycle, in order |
| A name declared locally and in a referenced schema | Both declarations |
| A bare name declared in two referenced schemas | Both schemas, and the qualified spellings to use instead |
| An unknown alias in a qualified name | The alias |

**A cycle is refused, not handled.** It could be handled: the cache makes loading terminate. But a
cycle means neither schema can be generated or migrated without the other, so they are one schema,
and saying so is more useful than supporting them.

**Shadowing is an error, not a precedence rule.** Step 1 above wins only when the name is unique.
A silent shadow is the case where a referenced schema adds a type and changes, without any notice,
what an existing member of the referencing schema means.

`kschema validate` exits non-zero on all of these. A missing file is the one case that does not need
the referenced schema to load, which is why it is named first.

## Ownership

Every element has exactly one owning schema, and that is the one it was declared in.

- **Editing.** `Schema.Rename` (`Schema.Rename.cs`) cascades only through the owning schema. In the
  editor, referenced elements are listed under their source file, read-only, with an action that
  opens that file. A rename in a shared schema does not rewrite the schemas that reference it. Each
  of those then reports the old name as dangling, which is the honest outcome when another file was
  changed on its own.
- **Generation (#288).** A generator emits only the elements the schema owns. A reference is spelled
  through its owner's generated output: C# writes the owner's `codeGenerators[].namespace` qualified
  (`global::Contoso.Shared.SpawnPoint`), and C++ `#include`s the owner's header by its relative path.
  The owner's namespace and output path are read from the code generator **with the same language**
  in the referenced schema. If that schema has none, the generator refuses with
  `TargetCannotExpress` and says which schema needs which language configured, rather than guessing a
  namespace. Since no file is emitted twice, two generated outputs only overlap if they are pointed
  at one directory. That is the case #288's manifest of written files has to detect.
- **Migrations (#127).** A diff between two versions of a schema compares only owned elements. A
  referenced type that changed belongs to the migration of the schema that owns it. The referencing
  schema's migration records the version of each referenced schema it was generated against, so it
  can require that migration to have run first.

## The acceptance case

`samples/dungeoneer.schema.json` drops its copies of the six Carbon Monoxide files and gains one
reference to `carbonmonoxide.schema.json`. When that happens:

- `EverySharedFileIsOneDungeoneerActuallyNeeds` is deleted, because the reference is now what keeps
  that set exact.
- The 14 cross-file references listed in `samples/README.md` resolve.
- `LegacySampleGenerationTests` and `LegacySampleCppTests` compile both schemas' output together.

## Order of work

1. The format, loading through `SchemaSet`, and resolution, with every validation case above. This
   is useful on its own, because validation is what tells the user whether their references work.
2. Ownership in generation (C#, then C++). This is where #288's written-files manifest should land
   too, since both change what a run writes.
3. The editor's read-only view of referenced elements.
4. #127 builds on the ownership rule from step 2.
