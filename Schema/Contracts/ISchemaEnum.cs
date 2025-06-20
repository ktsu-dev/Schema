// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines an enumeration in a schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It's representing a custom enumeration")]
public interface ISchemaEnum : ISchemaChild<ISchemaEnumName>
{
	/// <summary>
	/// Gets the values of the enumeration.
	/// </summary>
	public ISchemaChildSet<ISchemaEnumValue, ISchemaEnumValueName> Values { get; }
}