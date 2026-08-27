// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

/// <summary>
/// A named schema element that a <see cref="SchemaValidationIssue"/> can point at.
/// </summary>
/// <remarks>
/// <see cref="SchemaChild{TName}"/> is generic in its name type, so it offers no common type a
/// consumer can hold a reference to. This non-generic view exists so a validation issue can carry
/// the element it was reported against, letting a tool navigate to it directly instead of parsing
/// the issue's dotted path back into a lookup.
/// </remarks>
public interface ISchemaElement
{
	/// <summary>
	/// Gets the element's name.
	/// </summary>
	public string ElementName { get; }
}
