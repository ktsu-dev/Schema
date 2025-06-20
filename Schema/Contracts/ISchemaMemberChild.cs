// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a child element of a schema member.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public interface ISchemaMemberChild<TName> : ISchemaChild<TName> where TName : ISchemaMemberChildName
{
	/// <summary>
	/// Gets the parent member of the schema member child.
	/// </summary>
	public ISchemaMember? ParentMember { get; }
}
