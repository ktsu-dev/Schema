// Copyright (c) 2023-2026 ktsu-dev contributors

// Every test assembly in the repository is named, for the reason Schema/AssemblyInfo.cs records:
// KTSU0002 asks a non-test project to expose its internals to the repository's test projects
// rather than to the one that happens to need them.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.SchemaEditor.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Cpp.Test")]
