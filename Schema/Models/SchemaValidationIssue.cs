// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
	/// Returns a string representation of the issue.
	/// </summary>
	/// <returns>The severity, path, and message of the issue.</returns>
	public override string ToString() => $"{Severity}: {Path}: {Message}";
}
