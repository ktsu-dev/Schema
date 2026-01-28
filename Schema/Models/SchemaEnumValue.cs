// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;

/// <summary>
/// Represents a value within an enumeration.
/// This class maintains two parent references:
/// - ParentSchema (inherited): References the root schema that owns this enum value
/// - ParentEnum: References the immediate parent enumeration that contains this value
/// </summary>
public class SchemaEnumValue : SchemaChild<EnumValueName>
{
	/// <summary>
	/// Gets the parent enum that contains this enumeration value.
	/// </summary>
	public SchemaEnum? ParentEnum { get; private set; }

	/// <summary>
	/// Associates the schema enum value with a parent enum.
	/// </summary>
	/// <param name="schemaEnum">The parent enum to associate with.</param>
	/// <exception cref="ArgumentNullException">Thrown when the provided schema enum is null.</exception>
	public void AssociateWith(SchemaEnum schemaEnum)
	{
		Ensure.NotNull(schemaEnum);

		ParentEnum = schemaEnum;
		if (schemaEnum.ParentSchema is not null)
		{
			AssociateWith(schemaEnum.ParentSchema);
		}
	}

	/// <summary>
	/// Tries to remove this enum value from its parent enum.
	/// </summary>
	/// <returns>True if the enum value was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentEnum?.TryRemoveValue(Name) ?? false;
}