// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;

/// <summary>
/// Why a generation run did not produce output, when it did not.
/// </summary>
public enum SchemaGenerationStatus
{
	/// <summary>
	/// Source was generated.
	/// </summary>
	Success,

	/// <summary>
	/// The schema has error-severity validation issues, so generating from it would produce code
	/// that does not reflect a coherent schema.
	/// </summary>
	SchemaInvalid,

	/// <summary>
	/// The code generator element is not configured well enough to generate from.
	/// </summary>
	GeneratorNotConfigured,

	/// <summary>
	/// No generator is registered for the requested language.
	/// </summary>
	UnknownLanguage,
}

/// <summary>
/// The outcome of a generation run.
/// </summary>
public class SchemaGenerationResult
{
	/// <summary>
	/// Gets how the run ended.
	/// </summary>
	public SchemaGenerationStatus Status { get; init; }

	/// <summary>
	/// Gets the generated file contents, keyed by path relative to the output directory. Empty
	/// unless <see cref="Status"/> is <see cref="SchemaGenerationStatus.Success"/>.
	/// </summary>
	public IReadOnlyDictionary<string, string> Files { get; init; } = new Dictionary<string, string>();

	/// <summary>
	/// Gets the validation issues that caused generation to be refused, when it was refused for
	/// that reason.
	/// </summary>
	public Collection<SchemaValidationIssue> Issues { get; init; } = [];

	/// <summary>
	/// Gets a message explaining the outcome, suitable for showing to a user.
	/// </summary>
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Gets a value indicating whether source was generated.
	/// </summary>
	public bool IsSuccess => Status == SchemaGenerationStatus.Success;

	/// <summary>
	/// Returns a string representation of the outcome.
	/// </summary>
	/// <returns>The status and message.</returns>
	public override string ToString() => $"{Status}: {Message}";
}
