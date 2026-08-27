// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

/// <summary>
/// Indicates the severity of a schema validation issue.
/// </summary>
public enum SchemaValidationSeverity
{
	/// <summary>
	/// The schema is usable but something is incomplete or suspicious.
	/// </summary>
	Warning,

	/// <summary>
	/// The schema contains a broken reference or invalid construct.
	/// </summary>
	Error,
}

/// <summary>
/// Describes a single issue found while validating a schema.
/// </summary>
public class SchemaValidationIssue
{
	/// <summary>
	/// Gets the severity of the issue.
	/// </summary>
	public SchemaValidationSeverity Severity { get; init; }

	/// <summary>
	/// Gets the dotted path to the schema element the issue relates to, e.g. "User.Role".
	/// </summary>
	public string Path { get; init; } = string.Empty;

	/// <summary>
	/// Gets a human-readable description of the issue.
	/// </summary>
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Gets the schema element the issue was reported against, when a single element owns it.
	/// </summary>
	/// <remarks>
	/// <see cref="Path"/> identifies the element for a human reader, but parsing it back into a
	/// lookup is lossy - a name can contain a dot, and a duplicate-name issue names no single
	/// element. This reference lets a tool navigate straight to the offending element instead.
	/// It is null for issues that are not about one element, such as a duplicate name.
	/// </remarks>
	public ISchemaElement? Element { get; init; }

	/// <summary>
	/// Returns a string representation of the issue.
	/// </summary>
	/// <returns>The severity, path, and message of the issue.</returns>
	public override string ToString() => $"{Severity}: {Path}: {Message}";
}
