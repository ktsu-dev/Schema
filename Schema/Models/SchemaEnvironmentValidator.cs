// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using ktsu.Schema.Generation;
using ktsu.Semantics.Paths;

/// <summary>
/// The schema checks that consult something outside the schema: the file system and the generator registry.
/// </summary>
/// <remarks>
/// Kept out of <see cref="Schema"/> because every type the schema reaches counts against its
/// class-coupling budget (CA1506), and these checks reach types nothing else in it needs. It also keeps
/// the schema model from depending on the generators, which already depend on it.
/// </remarks>
internal static class SchemaEnvironmentValidator
{
	/// <summary>
	/// Reports a data source whose file is not there.
	/// </summary>
	/// <param name="issues">Where to add the issues.</param>
	/// <param name="dataSource">A data source of a schema that knows its own location.</param>
	internal static void ReportMissingDataSourceFile(Collection<SchemaValidationIssue> issues, DataSource dataSource)
	{
		if (!dataSource.TryResolveFile(out AbsoluteFilePath resolved))
		{
			return;
		}

		// File.Exists answers false rather than throwing for a path it can't read, so there is nothing to catch.
		if (!File.Exists(resolved))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = dataSource.Name,
				Message = $"Data source file '{dataSource.File}' does not exist (resolved to '{resolved}').",
				Element = dataSource,
			});
		}
	}

	/// <summary>
	/// Reports each element whose generated file would collide with one the generator writes for itself.
	/// </summary>
	/// <remarks>
	/// The element's file and the generator's own would be the same entry in the output, so one
	/// replaces the other on every file system. Matched case-insensitively, because Windows and macOS
	/// file systems ignore case.
	/// </remarks>
	/// <param name="issues">Where to add the issues.</param>
	/// <param name="codeGenerator">The configured generator.</param>
	/// <param name="generatedFileNames">Every element a generator names an output file after, with that name.</param>
	internal static void ReportReservedNames(
		Collection<SchemaValidationIssue> issues,
		SchemaCodeGenerator codeGenerator,
		IEnumerable<(string Name, ISchemaElement Element)> generatedFileNames)
	{
		if (!SchemaGenerator.TryGetGenerator(codeGenerator.Language, out ISchemaCodeGenerator? generator))
		{
			return;
		}

		HashSet<string> reserved = new(generator.ReservedNames(codeGenerator), StringComparer.OrdinalIgnoreCase);
		if (reserved.Count == 0)
		{
			return;
		}

		foreach ((string name, ISchemaElement element) in generatedFileNames.Where(entry => reserved.Contains(entry.Name)))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = name,
				Message = $"'{name}' is the name of a file code generator '{codeGenerator.Name}' writes for itself, so the two would overwrite each other.",
				Element = element,
			});
		}
	}

	/// <summary>
	/// Reports a configured namespace the generator's language cannot write.
	/// </summary>
	/// <remarks>
	/// The namespace goes into every generated file as it is, so one in the other language's
	/// spelling - <c>holo::components</c> for C#, <c>Game.Core</c> for C++ - would otherwise pass
	/// validation and generation and only fail when the output is built. Only a registered
	/// generator knows its language's rules, so a language with none registered is not checked
	/// here.
	/// </remarks>
	/// <param name="issues">Where to add the issues.</param>
	/// <param name="codeGenerator">The configured generator.</param>
	internal static void ReportUnwritableNamespace(Collection<SchemaValidationIssue> issues, SchemaCodeGenerator codeGenerator)
	{
		if (string.IsNullOrEmpty(codeGenerator.Namespace)
			|| !SchemaGenerator.TryGetGenerator(codeGenerator.Language, out ISchemaCodeGenerator? generator))
		{
			return;
		}

		string? problem = generator.NamespaceProblem(codeGenerator.Namespace);
		if (problem is not null)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = codeGenerator.Name,
				Message = $"Code generator namespace '{codeGenerator.Namespace}' cannot be written in {codeGenerator.Language}: {problem}",
				Element = codeGenerator,
			});
		}
	}
}
