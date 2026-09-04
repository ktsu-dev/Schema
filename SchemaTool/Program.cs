// Copyright (c) 2023-2026 ktsu-dev contributors

// Both test assemblies are named, rather than only the one that reads this project's internals.
// ktsu.Sdk's KTSU0002 requires a non-test project to expose its internals to the repository's test
// projects, and there are two of them now; which of the two a given project actually needs is not
// what the rule is checking.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.SchemaEditor.Test")]

namespace ktsu.SchemaTool;

using ktsu.Schema.Cli;

/// <summary>
/// A command line entry point for validating schemas and running their code generators, so
/// generation can happen in a build rather than only from the editor.
/// </summary>
/// <remarks>
/// Deliberately thin: the commands live in <see cref="SchemaCommandLine"/> in the library, where
/// they can be tested without spawning a process. This is only the wiring from a real console.
/// </remarks>
internal static class Program
{
	private static int Main(string[] args) => SchemaCommandLine.Run(args, Console.Out, Console.Error);
}
