// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ktsu.Schema.Contracts;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents an enumeration in a schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It's representing a custom enumeration")]
public class SchemaEnum : SchemaChild<EnumName>, ISchemaEnum
{
	/// <summary>
	/// Gets the internal collection of enumeration values.
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("values")]
	private Collection<EnumValueName> ValuesInternal { get; set; } = [];

	/// <summary>
	/// Gets the read-only collection of enumeration values.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyList<EnumValueName> Values => ValuesInternal;

	/// <summary>
	/// Tries to add a new value to the enumeration.
	/// </summary>
	/// <param name="name">The value to add.</param>
	/// <returns>True if the value was added; otherwise, false.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
	public bool TryAddValue(EnumValueName name)
	{
		Ensure.NotNullOrEmpty(name, nameof(name));
		if (!ValuesInternal.Any(v => v == name))
		{
			ValuesInternal.Add(name);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Tries to insert a new value at a position in the enumeration.
	/// </summary>
	/// <remarks>
	/// A value's position is its number in generated code, so putting a deleted value back means
	/// putting it back where it was. Appending it would renumber it and every value after it.
	/// </remarks>
	/// <param name="index">The position to insert at, from 0 to the number of values.</param>
	/// <param name="name">The value to insert.</param>
	/// <returns>True if the value was inserted; false if it is already present.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the values.</exception>
	public bool TryInsertValue(int index, EnumValueName name)
	{
		Ensure.NotNullOrEmpty(name, nameof(name));
		if (index < 0 || index > ValuesInternal.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), index, $"The enumeration has {ValuesInternal.Count} values.");
		}

		if (ValuesInternal.Any(v => v == name))
		{
			return false;
		}

		ValuesInternal.Insert(index, name);
		return true;
	}

	/// <summary>
	/// Tries to remove a value from the enumeration.
	/// </summary>
	/// <param name="name">The value to remove.</param>
	/// <returns>True if the value was removed; otherwise, false.</returns>
	public bool TryRemoveValue(EnumValueName name) => ValuesInternal.Remove(name);

	/// <summary>
	/// Renames a value, keeping its position in the enumeration.
	/// </summary>
	/// <remarks>
	/// A member typed as this enum names its default value by text, so every such default naming
	/// <paramref name="oldValue"/> is repointed to <paramref name="newValue"/>.
	/// </remarks>
	/// <param name="oldValue">The value to rename.</param>
	/// <param name="newValue">The new name.</param>
	/// <returns>True if the value was renamed; false if it is not present, or the new name is empty or already used.</returns>
	public bool TryRenameValue(EnumValueName oldValue, EnumValueName newValue)
	{
		Ensure.NotNull(oldValue);
		Ensure.NotNull(newValue);

		int index = ValuesInternal.IndexOf(oldValue);
		if (index < 0 || string.IsNullOrEmpty(newValue))
		{
			return false;
		}

		if (oldValue != newValue && ValuesInternal.Any(v => v == newValue))
		{
			return false;
		}

		ValuesInternal[index] = newValue;
		ParentSchema?.RepointEnumValueDefaults(Name, oldValue, newValue);
		return true;
	}

	/// <summary>
	/// Tries to remove this enumeration from its parent schema.
	/// </summary>
	/// <returns>True if the enumeration was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveEnum(this) ?? false;

	/// <summary>
	/// Gets a summary of the enumeration.
	/// </summary>
	[JsonIgnore]
	public string EnumSummary => $"{Name} ({ValuesInternal.Count})";
}
