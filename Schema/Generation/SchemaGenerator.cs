// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

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
	// Concurrent because registration can overlap: a plain dictionary written from two threads at
	// once can lose one of the writes, which is how a parallel test run once found a registered
	// language missing.
	private static readonly ConcurrentDictionary<string, ISchemaCodeGenerator> Generators =
		new(StringComparer.OrdinalIgnoreCase)
		{
			[CSharpCodeGenerator.LanguageId] = new CSharpCodeGenerator(),
		};

	/// <summary>
	/// Gets the languages a generator is registered for.
	/// </summary>
	public static IReadOnlyCollection<string> SupportedLanguages => [.. Generators.Keys];

	/// <summary>
	/// Adds a generator, or replaces the one already registered for its language.
	/// </summary>
	/// <param name="generator">The generator to register.</param>
	/// <exception cref="ArgumentException"><paramref name="generator"/> names no language.</exception>
	/// <remarks>
	/// This library targets frameworks that some generators' own dependencies do not, so a generator
	/// cannot always live beside the ones built in — the C++ generator is one, because the AST it
	/// builds on ships no <c>net8.0</c> assembly. Registration is what lets one live in its own
	/// project and still be found by language the way a schema names it.
	/// <para>
	/// Meant to be called once while a program starts, before anything generates. It is safe to
	/// call from several threads, and a generation running at the same time sees either the old
	/// generator or the new one.
	/// </para>
	/// </remarks>
	public static void Register(ISchemaCodeGenerator generator)
	{
		Ensure.NotNull(generator);

		if (string.IsNullOrEmpty(generator.Language))
		{
			throw new ArgumentException("A generator must name the language it emits.", nameof(generator));
		}

		Generators[generator.Language] = generator;
	}

	/// <summary>
	/// Reports whether a language has a generator registered for it.
	/// </summary>
	/// <param name="language">The language to look for, matched case-insensitively.</param>
	/// <returns>True when one is registered.</returns>
	public static bool IsRegistered(string language) =>
		!string.IsNullOrEmpty(language) && Generators.ContainsKey(language);

	/// <summary>
	/// Finds the generator registered for a language.
	/// </summary>
	/// <param name="language">The language to look for, matched case-insensitively.</param>
	/// <param name="generator">The generator, when one is registered.</param>
	/// <returns>True when one is registered.</returns>
	internal static bool TryGetGenerator(string language, [NotNullWhen(true)] out ISchemaCodeGenerator? generator)
	{
		generator = null;
		return !string.IsNullOrEmpty(language) && Generators.TryGetValue(language, out generator);
	}

	/// <summary>
	/// Reports whether an error stops the schema being generated from.
	/// </summary>
	/// <remarks>
	/// Generation is refused so that nothing compiles into something the schema does not
	/// describe. A data source describes where data lives, not a type, and no generator reads
	/// one, so its errors cannot cause that. A data file that does not exist yet, and a data
	/// source naming a class that does not, are both still errors to <c>kschema validate</c> and to
	/// data validation, which is where they mean something. Holding generation hostage to them
	/// made the same schema generate in memory and fail once saved, on a fresh clone, or wherever
	/// the data is git-ignored or written by a later build step.
	/// </remarks>
	/// <param name="issue">The issue to classify.</param>
	/// <returns>False for an issue about a data source; true otherwise.</returns>
	internal static bool BlocksGeneration(SchemaValidationIssue issue) => issue.Element is not DataSource;

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
		Collection<SchemaValidationIssue> errors = [.. issues.Where(i => i.Severity == SchemaValidationSeverity.Error && BlocksGeneration(i))];

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

		IReadOnlyDictionary<string, string> files;

		try
		{
			files = generator.Generate(schema, configuration);
		}
		catch (SchemaGenerationException exception)
		{
			// A refusal is an outcome rather than a fault: the schema named something the target
			// has no type for, and the generator said which and what it would need. Reported as a
			// result so a caller prints that sentence, rather than leaving it to be read off the
			// top of a stack trace.
			return new()
			{
				Status = SchemaGenerationStatus.TargetCannotExpress,
				Message = exception.Message,
			};
		}

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
	/// loaded from. Existing files are overwritten.
	/// <para>
	/// A file an earlier run wrote and this one did not - the header of a class since renamed or
	/// deleted - is removed, because left in place it goes on compiling a type the schema no longer
	/// declares. Each run records what it wrote in a manifest in the output directory, named for the
	/// generator, and only files that manifest lists are ever removed, so hand-written files beside
	/// the generated ones, and another generator's files in the same directory, are never touched.
	/// The first run after upgrading has no manifest and removes nothing. Stale files are removed
	/// before the new ones are written, so a rename that changes only the case of a file name lands
	/// on a case-insensitive file system rather than being deleted after it was written. A file that
	/// cannot be removed is reported in the message and kept in the manifest, so the next run tries
	/// again.
	/// </para>
	/// </remarks>
	/// <param name="schema">The schema to generate from.</param>
	/// <param name="configuration">The code generator element to generate under.</param>
	/// <returns>
	/// The outcome. On success, the files it reports have been written. A file that cannot be
	/// written is reported as <see cref="SchemaGenerationStatus.WriteFailed"/> rather than thrown.
	/// </returns>
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

		// Every destination is checked before anything is written, so a generator naming one file
		// outside the output directory leaves none of its others behind. A key is whatever the
		// generator returned, which for a registered one is code this library did not write, and
		// a name like "../x" or a rooted path would otherwise put a file anywhere the process can.
		string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(outputDirectory)) + Path.DirectorySeparatorChar;
		foreach (string key in result.Files.Keys)
		{
			string resolved = Path.GetFullPath(Path.Combine(outputDirectory, key));
			if (!resolved.StartsWith(root, StringComparison.Ordinal))
			{
				return new()
				{
					Status = SchemaGenerationStatus.WriteFailed,
					Message = $"Code generator '{configuration.Name}' produced '{key}', which is outside its output directory '{outputDirectory}'. Nothing was written.",
				};
			}
		}

		string manifestPath = Path.Combine(outputDirectory, GeneratedFileManifest.FileNameFor(configuration.Name));
		List<string> removed = [];
		List<string> notRemoved = [];
		foreach (string stale in GeneratedFileManifest.Read(manifestPath).Where(path => !result.Files.ContainsKey(path)))
		{
			if (!GeneratedFileManifest.TryResolve(outputDirectory, stale, out string stalePath) || !File.Exists(stalePath))
			{
				continue;
			}

			try
			{
				File.Delete(stalePath);
				removed.Add(stale);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				notRemoved.Add(stale);
			}
		}

		try
		{
			Directory.CreateDirectory(outputDirectory);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			return new()
			{
				Status = SchemaGenerationStatus.WriteFailed,
				Message = $"Could not write '{outputDirectory}' for '{configuration.Name}': {exception.Message}",
			};
		}
		foreach (KeyValuePair<string, string> file in result.Files)
		{
			string destination = Path.Combine(outputDirectory, file.Key);

			// A write that fails is an outcome, like a refusal: a read-only directory or a file
			// standing where a directory is needed says nothing about the schema, and a caller
			// running several generators should report it and carry on rather than unwind.
			try
			{
				string? directory = Path.GetDirectoryName(destination);
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				File.WriteAllText(destination, file.Value);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				return new()
				{
					Status = SchemaGenerationStatus.WriteFailed,
					Message = $"Could not write '{destination}' for '{configuration.Name}': {exception.Message}",
				};
			}
		}

		try
		{
			GeneratedFileManifest.Write(manifestPath, [.. result.Files.Keys, .. notRemoved]);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			return new()
			{
				Status = SchemaGenerationStatus.WriteFailed,
				Message = $"Wrote the files for '{configuration.Name}' but could not record them in '{manifestPath}': {exception.Message}",
			};
		}

		string message = $"Wrote {result.Files.Count} file{(result.Files.Count == 1 ? string.Empty : "s")} to '{outputDirectory}'";
		if (removed.Count > 0)
		{
			message += $" and removed {removed.Count} stale file{(removed.Count == 1 ? string.Empty : "s")}";
		}

		message += ".";
		if (notRemoved.Count > 0)
		{
			message += $" Could not remove: {string.Join(", ", notRemoved)}.";
		}

		return new()
		{
			Status = SchemaGenerationStatus.Success,
			Files = result.Files,
			RemovedFiles = removed.AsReadOnly(),
			Message = message,
		};
	}
}
