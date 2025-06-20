// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics;

/// <summary>
/// Represents a child of a schema class.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaClassChild<TName> : SchemaChild<TName>, ISchemaClassChild<TName> where TName : SemanticString<TName>, ISchemaClassChildName<TName>, new()
{
	/// <summary>
	/// Gets the parent class of the schema class child.
	/// </summary>
	public SchemaClass? ParentClass { get; private set; }

	/// <summary>
	/// Gets the parent class of the schema class child as an interface.
	/// </summary>
	ISchemaClass? ISchemaClassChild<TName>.ParentClass => ParentClass;

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
