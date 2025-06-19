// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using ktsu.Semantics;

/// <summary>
/// Represents a child of a schema.
/// </summary>
public abstract class SchemaChild
{
	/// <summary>
	/// Attempts to remove this child from its parent.
	/// </summary>
	/// <returns>True if the child was removed, false otherwise.</returns>
	public abstract bool TryRemove();

	/// <summary>
	/// Returns a summary of this child.
	/// </summary>
	/// <returns>A summary of this child.</returns>
	public abstract string Summary();
}

/// <summary>
/// Represents a child of a schema with a specific name type.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaChild<TName> : SchemaChild where TName : SemanticString<TName>, new()
{
	/// <summary>
	/// Gets the name of the schema child.
	/// </summary>
	public TName Name { get; private set; } = new();

	/// <summary>
	/// Gets the parent schema provider of the schema child.
	/// </summary>
	public SchemaProvider? ParentSchema { get; private set; }

	/// <summary>
	/// Returns the name of the schema child as a string.
	/// </summary>
	/// <returns>The name of the schema child.</returns>
	public override string ToString() => Name;

	/// <summary>
	/// Returns a summary of the schema child.
	/// </summary>
	/// <returns>A summary of the schema child.</returns>
	public override string Summary() => Name;

	/// <summary>
	/// Renames the schema child.
	/// </summary>
	/// <param name="name">The new name for the schema child.</param>
	public void Rename(TName name) => Name = name;

	/// <summary>
	/// Associates the schema child with a parent schema provider.
	/// </summary>
	/// <param name="schemaProvider">The parent schema provider to associate with.</param>
	public void AssociateWith(SchemaProvider schemaProvider) => ParentSchema = schemaProvider;
}

/// <summary>
/// Represents a child of a schema class.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaClassChild<TName> : SchemaChild<TName> where TName : SemanticString<TName>, new()
{
	/// <summary>
	/// Gets the parent class of the schema class child.
	/// </summary>
	public SchemaClass? ParentClass { get; private set; }

	/// <summary>
	/// Associates the schema class child with a parent class.
	/// </summary>
	/// <param name="schemaClass">The parent class to associate with.</param>
	/// <exception cref="ArgumentNullException">Thrown when the provided schema class is null.</exception>
	public void AssociateWith(SchemaClass schemaClass)
	{
		ArgumentNullException.ThrowIfNull(schemaClass);

		ParentClass = schemaClass;
		if (schemaClass.ParentSchema is not null)
		{
			AssociateWith(schemaClass.ParentSchema);
		}
	}
}

/// <summary>
/// Represents a child of a schema member.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaMemberChild<TName> : SchemaClassChild<TName> where TName : SemanticString<TName>, new()
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
