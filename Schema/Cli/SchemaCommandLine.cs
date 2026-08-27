// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cli;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// The commands a schema command line offers: validating a schema and running its code generators.
/// </summary>
/// <remarks>
/// The implementation lives in the library rather than in the tool so it can be tested without
/// spawning a process, and so anything else that wants to drive generation - an MSBuild task, for
/// instance - can call it directly. The tool itself is only a <c>Main</c> that hands
/// <see cref="Console"/> to <see cref="Run"/>.
///
/// Output goes to injected writers for the same reason: a test can read what a command said, not
/// merely what it returned.
/// </remarks>
public static class SchemaCommandLine
{
	/// <summary>
	/// The exit code for a command that did what was asked.
	/// </summary>
	public const int Success = 0;

	/// <summary>
	/// The exit code for a command that did not.
	/// </summary>
	public const int Failure = 1;

	/// <summary>
	/// The option that limits <c>generate</c> to a single named code generator.
	/// </summary>
	private const string GeneratorOption = "--generator";

	/// <summary>
	/// Runs a command.
	/// </summary>
	/// <param name="args">The command and its arguments.</param>
	/// <param name="output">Where ordinary output goes.</param>
	/// <param name="error">Where errors go.</param>
	/// <returns><see cref="Success"/> or <see cref="Failure"/>.</returns>
	public static int Run(string[] args, TextWriter output, TextWriter error)
	{
		Ensure.NotNull(args);
		Ensure.NotNull(output);
		Ensure.NotNull(error);

		if (args.Length == 0 || IsHelp(args[0]))
		{
			WriteUsage(output);
			return args.Length == 0 ? Failure : Success;
		}

		return args[0].ToLowerInvariant() switch
		{
			"generate" => Generate([.. args.Skip(1)], output, error),
			"validate" => Validate([.. args.Skip(1)], output, error),
			_ => UnknownCommand(args[0], output, error),
		};
	}

	private static bool IsHelp(string argument) =>
		argument is "--help" or "-h" or "-?" or "help";

	private static void WriteUsage(TextWriter output)
	{
		output.WriteLine("Usage: schema <command> <schema-file> [options]");
		output.WriteLine();
		output.WriteLine("Commands:");
		output.WriteLine("  generate <schema-file> [--generator <name>]   Run the schema's code generators.");
		output.WriteLine("  validate <schema-file>                        Report the schema's validation issues.");
		output.WriteLine();
		output.WriteLine("Output paths are relative to the schema file's own directory.");
		output.WriteLine($"Known languages: {string.Join(", ", SchemaGenerator.SupportedLanguages)}");
	}

	private static int UnknownCommand(string command, TextWriter output, TextWriter error)
	{
		error.WriteLine($"Unknown command '{command}'.");
		WriteUsage(output);
		return Failure;
	}

	private static int Generate(string[] args, TextWriter output, TextWriter error)
	{
		if (!TryLoad(args, error, out Models.Schema? schema, out string? schemaPath))
		{
			return Failure;
		}

		string? only = GetOption(args, GeneratorOption);
		List<SchemaCodeGenerator> generators = [.. schema!.CodeGenerators
			.Where(g => only is null || string.Equals(g.Name, only, StringComparison.OrdinalIgnoreCase))];

		if (generators.Count == 0)
		{
			error.WriteLine(only is null
				? $"'{schemaPath}' declares no code generators."
				: $"'{schemaPath}' has no code generator named '{only}'.");
			return Failure;
		}

		bool allSucceeded = true;
		foreach (SchemaCodeGenerator generator in generators)
		{
			SchemaGenerationResult result = SchemaGenerator.GenerateToDisk(schema, generator);

			if (result.IsSuccess)
			{
				output.WriteLine($"{generator.Name}: {result.Message}");
				continue;
			}

			allSucceeded = false;
			error.WriteLine($"{generator.Name}: {result.Message}");

			foreach (SchemaValidationIssue issue in result.Issues)
			{
				error.WriteLine($"  {issue}");
			}
		}

		return allSucceeded ? Success : Failure;
	}

	private static int Validate(string[] args, TextWriter output, TextWriter error)
	{
		if (!TryLoad(args, error, out Models.Schema? schema, out _))
		{
			return Failure;
		}

		List<SchemaValidationIssue> issues = [.. schema!.Validate()];

		foreach (SchemaValidationIssue issue in issues)
		{
			output.WriteLine(issue.ToString());
		}

		int errors = issues.Count(i => i.Severity == SchemaValidationSeverity.Error);
		output.WriteLine($"{errors} error(s), {issues.Count - errors} warning(s).");

		// A warning is not a failure: an incomplete schema is still a legitimate work in progress.
		return errors == 0 ? Success : Failure;
	}

	private static bool TryLoad(string[] args, TextWriter error, out Models.Schema? schema, out string? schemaPath)
	{
		schema = null;
		schemaPath = FindSchemaPath(args);

		if (string.IsNullOrEmpty(schemaPath))
		{
			error.WriteLine("No schema file was given.");
			return false;
		}

		if (!File.Exists(schemaPath))
		{
			error.WriteLine($"'{schemaPath}' does not exist.");
			return false;
		}

		AbsoluteFilePath fullPath = Path.GetFullPath(schemaPath).As<AbsoluteFilePath>();
		SchemaLoadResult result = SchemaSerializer.Load(File.ReadAllText(fullPath), fullPath);

		if (!result.IsSuccess)
		{
			error.WriteLine($"Could not open '{schemaPath}': {result.Message}");
			return false;
		}

		schema = result.Schema;
		return true;
	}

	/// <summary>
	/// Finds the schema path among the arguments.
	/// </summary>
	/// <remarks>
	/// The value that follows an option is not a positional argument, and is otherwise
	/// indistinguishable from one: without this, "generate --generator CSharp game.schema.json"
	/// would take "CSharp" as the schema to open.
	/// </remarks>
	private static string? FindSchemaPath(string[] args)
	{
		for (int index = 0; index < args.Length; index++)
		{
			if (!args[index].StartsWith('-'))
			{
				return args[index];
			}

			if (TakesAValue(args[index]))
			{
				index++;
			}
		}

		return null;
	}

	private static bool TakesAValue(string option) =>
		string.Equals(option, GeneratorOption, StringComparison.OrdinalIgnoreCase);

	private static string? GetOption(string[] args, string name)
	{
		int index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
		return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
	}
}
