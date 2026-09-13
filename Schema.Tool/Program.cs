// Copyright (c) 2023-2026 ktsu-dev contributors

// Both test assemblies are named, rather than only the one that reads this project's internals.
// ktsu.Sdk's KTSU0002 requires a non-test project to expose its internals to the repository's test
// projects, and there are two of them now; which of the two a given project actually needs is not
// what the rule is checking.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Editor.Test")]

namespace ktsu.Schema.Tool;

using ktsu.Schema.Cli;
using ktsu.Schema.Cpp;
using ktsu.Schema.Generation;

/// <summary>
/// A command line entry point for validating schemas and running their code generators, so
/// generation can happen in a build rather than only from the editor.
/// </summary>
/// <remarks>
/// Deliberately thin: the commands live in <see cref="SchemaCommandLine"/> in the library and the
/// C++ options in <see cref="CppGeneratorOptionsFile"/> beside the generator, both of which can be
/// tested without spawning a process. This is only the wiring from a real console.
/// </remarks>
internal static class Program
{
	/// <summary>What this is installed as, and the one option it adds to the commands.</summary>
	private static SchemaCommandLineHost Host => new()
	{
		CommandName = "kschema",
		Options = [CppGeneratorOptionsFile.OptionUsage],
	};

	private static int Main(string[] args)
	{
		// The C++ generator cannot ship inside the library - it is built on an AST that publishes
		// no net8.0 assembly and the library does - so a host is what makes it findable by the
		// language a schema names. This is that host.
		//
		// With no options it emits standard C++ and nothing else: a schema reaching for a vector,
		// a handle, a fallible return or a date is refused by name rather than handed a header
		// that will not compile. A target with its own vocabulary for those says how it spells
		// them in the file named by --cpp-options.
		if (!CppGeneratorOptionsFile.TryTake(args, out string[] remaining, out CppGeneratorOptions? options, out string? message))
		{
			Console.Error.WriteLine(message);
			return SchemaCommandLine.Failure;
		}

		SchemaGenerator.Register(new CppCodeGenerator(options));

		return SchemaCommandLine.Run(remaining, Console.Out, Console.Error, Host);
	}
}
