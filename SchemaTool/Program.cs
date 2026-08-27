// Copyright (c) 2023-2026 ktsu-dev contributors

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]

namespace ktsu.SchemaTool;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// A command line entry point for running a schema's code generators, so generation can happen in
/// a build rather than only from the editor.
/// </summary>
internal static class Program
{
	private const int Success = 0;
	private const int Failure = 1;

	private static int Main(string[] args)
	{
		if (args.Length == 0 || IsHelp(args[0]))
		{
			WriteUsage();
			return args.Length == 0 ? Failure : Success;
		}

		return args[0].ToLowerInvariant() switch
		{
			"generate" => Generate([.. args.Skip(1)]),
			"validate" => Validate([.. args.Skip(1)]),
			_ => UnknownCommand(args[0]),
		};
	}

	private static bool IsHelp(string argument) =>
		argument is "--help" or "-h" or "-?" or "help";

	private static void WriteUsage()
	{
		Console.WriteLine("Usage: schema <command> <schema-file> [options]");
		Console.WriteLine();
		Console.WriteLine("Commands:");
		Console.WriteLine("  generate <schema-file> [--generator <name>]   Run the schema's code generators.");
		Console.WriteLine("  validate <schema-file>                        Report the schema's validation issues.");
		Console.WriteLine();
		Console.WriteLine("Output paths are relative to the schema file's own directory.");
		Console.WriteLine($"Known languages: {string.Join(", ", SchemaGenerator.SupportedLanguages)}");
	}

	private static int UnknownCommand(string command)
	{
		Console.Error.WriteLine($"Unknown command '{command}'.");
		WriteUsage();
		return Failure;
	}

	private static int Generate(string[] args)
	{
		if (!TryLoad(args, out Schema? schema, out string? schemaPath))
		{
			return Failure;
		}

		string? only = GetOption(args, "--generator");
		List<SchemaCodeGenerator> generators = [.. schema!.CodeGenerators
			.Where(g => only is null || string.Equals(g.Name, only, StringComparison.OrdinalIgnoreCase))];

		if (generators.Count == 0)
		{
			Console.Error.WriteLine(only is null
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
				Console.WriteLine($"{generator.Name}: {result.Message}");
				continue;
			}

			allSucceeded = false;
			Console.Error.WriteLine($"{generator.Name}: {result.Message}");

			foreach (SchemaValidationIssue issue in result.Issues)
			{
				Console.Error.WriteLine($"  {issue}");
			}
		}

		return allSucceeded ? Success : Failure;
	}

	private static int Validate(string[] args)
	{
		if (!TryLoad(args, out Schema? schema, out _))
		{
			return Failure;
		}

		List<SchemaValidationIssue> issues = [.. schema!.Validate()];

		foreach (SchemaValidationIssue issue in issues)
		{
			Console.WriteLine(issue.ToString());
		}

		int errors = issues.Count(i => i.Severity == SchemaValidationSeverity.Error);
		Console.WriteLine($"{errors} error(s), {issues.Count - errors} warning(s).");

		// A warning is not a failure: an incomplete schema is still a legitimate work in progress.
		return errors == 0 ? Success : Failure;
	}

	private static bool TryLoad(string[] args, out Schema? schema, out string? schemaPath)
	{
		schema = null;
		schemaPath = args.FirstOrDefault(a => !a.StartsWith('-'));

		if (string.IsNullOrEmpty(schemaPath))
		{
			Console.Error.WriteLine("No schema file was given.");
			WriteUsage();
			return false;
		}

		if (!File.Exists(schemaPath))
		{
			Console.Error.WriteLine($"'{schemaPath}' does not exist.");
			return false;
		}

		AbsoluteFilePath fullPath = Path.GetFullPath(schemaPath).As<AbsoluteFilePath>();
		SchemaLoadResult result = SchemaSerializer.Load(File.ReadAllText(fullPath), fullPath);

		if (!result.IsSuccess)
		{
			Console.Error.WriteLine($"Could not open '{schemaPath}': {result.Message}");
			return false;
		}

		schema = result.Schema;
		return true;
	}

	private static string? GetOption(string[] args, string name)
	{
		int index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
		return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
	}
}
