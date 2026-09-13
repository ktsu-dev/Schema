# Samples

Two real programs' whole schema vocabulary, migrated from the format they were written in.

| | |
| --- | --- |
| `legacy/carbonmonoxide/` | 8 files as Carbon Monoxide wrote them |
| `legacy/dungeoneer/` | 18 files as Dungeoneer wrote them |
| `carbonmonoxide.schema.json` | the first eight, migrated - 12 classes and 3 enums |
| `dungeoneer.schema.json` | the eighteen plus what they are built against - 33 classes and 8 enums |

## What they are for

Every other test in this repository builds a schema to exercise one rule. These are the opposite:
45 elements written by people who had never heard of this library and were not trying to be
interesting. A generator that handles a hand-built five-member class and falls over on `Monster`
has a bug no rule-shaped test would find, so the suite generates C# from both and compiles it,
generates C++ from both and compiles that, and checks that nothing in either reaches C# as
`object?`.

The legacy files are kept beside the migrated ones and the migration is re-run by
`LegacySchemaMigrationTests` rather than trusted, so a sample cannot drift from the input it came
from. `LegacySchemaReader` is the reader, and it lives in the test project: the format is two
repositories' history, and publishing a reader for it would commit this library to a dialect
nobody else has.

## What the migration could not preserve

**The file boundaries.** A member's `className` names a class in the same schema, and 14 of the
references in the legacy set cross a file - 7 of those reaching out of Dungeoneer into Carbon
Monoxide's shared types. So a set of files that referred to each other becomes one schema or
nothing. `dungeoneer.schema.json` therefore carries the six Carbon Monoxide files it resolves
against (`anchor`, `collision`, `hitbox`, `line`, `rect`, `vector`); `gradient` and `spline` it
never mentions, and `EverySharedFileIsOneDungeoneerActuallyNeeds` is what stops that list quietly
growing into "all of Carbon Monoxide".

The legacy format had its own answer to this and it was never load-bearing: `rect.schema.json`
spells the list `References`, `spline.schema.json` spells it `ReferencedSchemas`, and
`line.schema.json` names `Vector2` while declaring neither. Nothing checked it, because the
generated C++ simply included everything.

## What the migration deliberately did not change

`Vector2`, `Vector3`, `IntVector2` and `IntVector3` stay **classes**, rather than becoming the
built-in `Vector2`/`Vector3` types. The legacy schema declared them as classes with named `x`, `y`
and `z` members, and that is a different thing from a fixed-shape numeric vector whose components
the library knows about: a generated `struct Vector2 { float x; float y; }` is what those programs
compile against today. Mapping them onto the built-ins would be a redesign wearing a migration's
clothes - and it would take the cross-file references with it, which are the most interesting
thing in the set.
