// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a child of a schema with a specific name type.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaChild<TName> : ISchemaChild<TName> where TName : SemanticString<TName>, ISchemaChildName, new()
{
	/// <summary>
	/// Gets the name of the schema child.
	/// </summary>
	public TName Name { get; set; } = new();

	/// <summary>
	/// Gets or sets the description of the schema child.
	/// </summary>
	public ISchemaChildDescription Description { get; set; } = new SchemaChildDescription();

	/// <summary>
	/// Gets the parent schema provider of the schema child.
	/// </summary>
	public Schema? ParentSchema { get; private set; }

	/// <summary>
	/// Gets the parent schema provider of the schema child as an interface.
	/// </summary>
	ISchema? ISchemaChild<TName>.ParentSchema => ParentSchema;

	/// <summary>
	/// Gets or sets the summary of this child.
	/// </summary>
	public ISchemaChildSummary Summary { get; set; } = new SchemaChildSummary();

	/// <summary>
	/// Returns the name of the schema child as a string.
	/// </summary>
	/// <returns>The name of the schema child.</returns>
	public override string ToString() => Name;

	/// <summary>
	/// Renames the schema child.
	/// </summary>
	/// <param name="name">The new name for the schema child.</param>
	public void Rename(TName name) => Name = name;

	/// <summary>
	/// Associates the schema child with a parent schema provider.
	/// </summary>
	/// <param name="schemaProvider">The parent schema provider to associate with.</param>
	public void AssociateWith(Schema schemaProvider) => ParentSchema = schemaProvider;

	/// <summary>
	/// Attempts to remove this child from its parent.
	/// </summary>
	/// <returns>True if the child was removed, false otherwise.</returns>
	public abstract bool TryRemove();
}
