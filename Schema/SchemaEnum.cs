// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using System.Collections.ObjectModel;

/// <summary>
/// Represents an enumeration in a schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It's representing a custom enumeration")]
public class SchemaEnum : SchemaChild<EnumName>
{
	/// <summary>
	/// Gets the internal collection of enumeration values.
	/// </summary>
	private Collection<EnumValueName> ValuesInternal { get; set; } = [];

	/// <summary>
	/// Gets the read-only collection of enumeration values.
	/// </summary>
	public IReadOnlyCollection<EnumValueName> Values => ValuesInternal;

	/// <summary>
	/// Tries to add a new value to the enumeration.
	/// </summary>
	/// <param name="enumValueName">The value to add.</param>
	/// <returns>True if the value was added; otherwise, false.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="enumValueName"/> is null or empty.</exception>
	public bool TryAddValue(EnumValueName enumValueName)
	{
		ArgumentException.ThrowIfNullOrEmpty(enumValueName, nameof(enumValueName));
		if (!ValuesInternal.Any(v => v == enumValueName))
		{
			ValuesInternal.Add(enumValueName);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Tries to remove a value from the enumeration.
	/// </summary>
	/// <param name="enumValueName">The value to remove.</param>
	/// <returns>True if the value was removed; otherwise, false.</returns>
	public bool TryRemoveValue(EnumValueName enumValueName) => ValuesInternal.Remove(enumValueName);

	/// <summary>
	/// Tries to remove this enumeration from its parent schema.
	/// </summary>
	/// <returns>True if the enumeration was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveEnum(this) ?? false;

	/// <summary>
	/// Gets a summary of the enumeration.
	/// </summary>
	/// <returns>A string summary of the enumeration.</returns>
	public override string Summary() => $"{Name} ({ValuesInternal.Count})";
}
