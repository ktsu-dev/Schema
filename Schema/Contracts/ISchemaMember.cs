// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a member of a schema class.
/// </summary>
public interface ISchemaMember : ISchemaClassChild<ISchemaMemberName>
{
	/// <summary>
	/// Gets the type of the schema member.
	/// </summary>
	public ISchemaType Type { get; set; }
}
