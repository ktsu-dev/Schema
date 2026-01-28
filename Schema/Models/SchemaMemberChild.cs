// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a child of a schema member.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaMemberChild<TName> : SchemaClassChild<TName> where TName : SemanticString<TName>, ISchemaClassChildName<TName>, new()
{
	/// <summary>
	/// Gets the parent member of the schema member child.
	/// </summary>
	public SchemaMember? ParentMember { get; private set; }

	/// <summary>
	/// Associates the schema member child with a parent member.
	/// </summary>
	/// <param name="schemaMember">The parent member to associate with.</param>
	public void AssociateWith(SchemaMember schemaMember) => ParentMember = schemaMember;
}
