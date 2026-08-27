// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a child of a schema class.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaClassChild<TName> : SchemaChild<TName>, ISchemaClassChild<TName> where TName : SemanticString<TName>, ISchemaClassChildName, new()
{
	/// <summary>
	/// Gets the parent class of the schema class child.
	/// </summary>
	[JsonIgnore]
	public SchemaClass? ParentClass { get; private set; }

	/// <remarks>
	/// Explicit because the contract exposes the parent as <see cref="ISchemaClass"/> while the
	/// model exposes the concrete <see cref="SchemaClass"/>; both are the same object.
	/// </remarks>
	ISchemaClass? ISchemaClassChild<TName>.ParentClass => ParentClass;

	/// <summary>
	/// Associates the schema class child with a parent class.
	/// </summary>
	/// <param name="schemaClass">The parent class to associate with.</param>
	public void AssociateWith(SchemaClass schemaClass)
	{
		Ensure.NotNull(schemaClass);
		ParentClass = schemaClass;
		if (schemaClass.ParentSchema is not null)
		{
			AssociateWith(schemaClass.ParentSchema);
		}
	}
}
