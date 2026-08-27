// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Models.Names;

/// <summary>
/// Defines an enumeration in a schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It's representing a custom enumeration")]
public interface ISchemaEnum : ISchemaChild<EnumName>
{
	/// <summary>
	/// Gets the values of the enumeration, in declaration order.
	/// </summary>
	/// <remarks>
	/// An enum value is a name and nothing else — the format stores the values as a list of
	/// strings — so they are exposed as names rather than as child elements.
	/// </remarks>
	public IReadOnlyList<EnumValueName> Values { get; }

	/// <summary>
	/// Adds a value to the enumeration.
	/// </summary>
	/// <param name="name">The value to add.</param>
	/// <returns><see langword="true"/> if the value was added; <see langword="false"/> if it is already present.</returns>
	public bool TryAddValue(EnumValueName name);

	/// <summary>
	/// Removes a value from the enumeration.
	/// </summary>
	/// <param name="name">The value to remove.</param>
	/// <returns><see langword="true"/> if the value was found and removed; otherwise, <see langword="false"/>.</returns>
	public bool TryRemoveValue(EnumValueName name);
}
