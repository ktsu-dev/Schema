// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents an enumeration in a schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It's representing a custom enumeration")]
public class SchemaEnum : SchemaChild<EnumName>
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
	public IReadOnlyCollection<EnumValueName> Values => ValuesInternal;

	/// <summary>
	/// Tries to add a new value to the enumeration.
	/// </summary>
	/// <param name="enumValueName">The value to add.</param>
	/// <returns>True if the value was added; otherwise, false.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="enumValueName"/> is null or empty.</exception>
	public bool TryAddValue(EnumValueName enumValueName)
	{
		Ensure.NotNullOrEmpty(enumValueName, nameof(enumValueName));
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
	/// Renames a value, keeping its position in the enumeration.
	/// </summary>
	/// <remarks>
	/// Nothing else in the schema references an enum value by name, so there is nothing to cascade.
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
