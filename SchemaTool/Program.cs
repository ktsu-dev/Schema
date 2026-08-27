// Copyright (c) 2023-2026 ktsu-dev contributors

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]

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
