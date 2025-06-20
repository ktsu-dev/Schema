// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a class within a schema.
/// </summary>
public interface ISchemaClass : ISchemaChild<ISchemaClassName>
{
	/// <summary>
	/// Gets the members of the schema class.
	/// </summary>
	public ISchemaChildSet<ISchemaMember, ISchemaMemberName> Members { get; }
}
