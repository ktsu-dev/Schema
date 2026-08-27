// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;
using ktsu.Semantics.Paths;

/// <summary>
/// Runs the code generators a schema declares.
/// </summary>
/// <remarks>
/// Generation is refused for a schema with error-severity validation issues. Generating from a
/// schema whose references do not resolve would emit code that compiles into something the schema
/// does not describe, or does not compile at all - and the resulting error would point at
/// generated code rather than at the schema mistake that caused it.
/// </remarks>
public static class SchemaGenerator
{
	private static readonly Dictionary<string, ISchemaCodeGenerator> Generators =
		new(StringComparer.OrdinalIgnoreCase)
		{
			[CSharpCodeGenerator.LanguageId] = new CSharpCodeGenerator(),
		};

	/// <summary>
	/// Gets the languages a generator is registered for.
	/// </summary>
	public static IReadOnlyCollection<string> SupportedLanguages => [.. Generators.Keys];

	/// <summary>
	/// Generates source for one of a schema's code generators.
	/// </summary>
	/// <param name="schema">The schema to generate from.</param>
	/// <param name="configuration">The code generator element to generate under.</param>
	/// <returns>The outcome, including the generated files when it succeeded.</returns>
	public static SchemaGenerationResult Generate(Models.Schema schema, SchemaCodeGenerator configuration)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(configuration);

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Collection<SchemaValidationIssue> errors = [.. issues.Where(i => i.Severity == SchemaValidationSeverity.Error)];

		if (errors.Count > 0)
		{
			return new()
			{
				Status = SchemaGenerationStatus.SchemaInvalid,
				Issues = errors,
				Message = $"The schema has {errors.Count} error{(errors.Count == 1 ? string.Empty : "s")} and cannot be generated from. " +
					$"First: {errors[0].Path}: {errors[0].Message}",
			};
		}

		if (string.IsNullOrEmpty(configuration.Language))
		{
			return new()
			{
				Status = SchemaGenerationStatus.GeneratorNotConfigured,
				Message = $"Code generator '{configuration.Name}' does not specify a target language. Known languages: {string.Join(", ", SupportedLanguages)}.",
			};
		}

		if (!Generators.TryGetValue(configuration.Language, out ISchemaCodeGenerator? generator))
		{
			return new()
			{
				Status = SchemaGenerationStatus.UnknownLanguage,
				Message = $"No generator is registered for language '{configuration.Language}'. Known languages: {string.Join(", ", SupportedLanguages)}.",
			};
		}

		IReadOnlyDictionary<string, string> files = generator.Generate(schema, configuration);

		return new()
		{
			Status = SchemaGenerationStatus.Success,
			Files = files,
			Message = $"Generated {files.Count} file{(files.Count == 1 ? string.Empty : "s")} for '{configuration.Name}'.",
		};
	}

	/// <summary>
	/// Generates source for one of a schema's code generators and writes it to that generator's
	/// output directory.
	/// </summary>
	/// <remarks>
	/// The output directory is relative to the schema file, so the schema must know where it was
	/// loaded from. Existing files are overwritten; nothing is deleted, so a file that a previous
	/// run produced and this one did not is left alone rather than being silently removed.
	/// </remarks>
	/// <param name="schema">The schema to generate from.</param>
	/// <param name="configuration">The code generator element to generate under.</param>
	/// <returns>The outcome. On success, the files it reports have been written.</returns>
	public static SchemaGenerationResult GenerateToDisk(Models.Schema schema, SchemaCodeGenerator configuration)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(configuration);

		SchemaGenerationResult result = Generate(schema, configuration);
		if (!result.IsSuccess)
		{
			return result;
		}

		if (!configuration.TryResolveOutputPath(out AbsoluteDirectoryPath outputDirectory))
		{
			return new()
			{
				Status = SchemaGenerationStatus.GeneratorNotConfigured,
				Message = string.IsNullOrEmpty(configuration.OutputPath)
					? $"Code generator '{configuration.Name}' does not specify an output path."
					: $"Code generator '{configuration.Name}' has an output path, but the schema's own location is unknown so it cannot be resolved.",
			};
		}

		foreach (KeyValuePair<string, string> file in result.Files)
		{
			string destination = Path.Combine(outputDirectory, file.Key);
			string? directory = Path.GetDirectoryName(destination);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			File.WriteAllText(destination, file.Value);
		}

		return new()
		{
			Status = SchemaGenerationStatus.Success,
			Files = result.Files,
			Message = $"Wrote {result.Files.Count} file{(result.Files.Count == 1 ? string.Empty : "s")} to '{outputDirectory}'.",
		};
	}
}
