// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a value within an enumeration.
/// This interface maintains two parent references:
/// - ParentSchema (inherited): References the root schema that owns this enum value
/// - ParentEnum: References the immediate parent enumeration that contains this value
/// </summary>
public interface ISchemaEnumValue : ISchemaChild<ISchemaEnumValueName>
{
	/// <summary>
	/// Gets the parent enum that contains this enumeration value.
	/// </summary>
	public ISchemaEnum? ParentEnum { get; }
}
