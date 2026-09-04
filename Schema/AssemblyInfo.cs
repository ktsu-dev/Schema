// Copyright (c) 2023-2026 ktsu-dev contributors

// Both test assemblies are named, rather than only the one that reads this project's internals.
// ktsu.Sdk's KTSU0002 requires a non-test project to expose its internals to the repository's test
// projects, and there are two of them now; which of the two a given project actually needs is not
// what the rule is checking.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.SchemaEditor.Test")]
