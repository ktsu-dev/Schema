// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a child of a schema interface.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaInterfaceChild<TName> : SchemaChild<TName>
	where TName : SemanticString<TName>, ISchemaInterfaceChildName, new()
{
	/// <summary>
	/// Gets the parent interface of this child.
	/// </summary>
	[JsonIgnore]
	public SchemaInterface? ParentInterface { get; private set; }

	/// <summary>
	/// Associates this child with a parent interface.
	/// </summary>
	/// <param name="schemaInterface">The parent interface to associate with.</param>
	public void AssociateWith(SchemaInterface schemaInterface)
	{
		Ensure.NotNull(schemaInterface);
		ParentInterface = schemaInterface;
		if (schemaInterface.ParentSchema is not null)
		{
			AssociateWith(schemaInterface.ParentSchema);
		}
	}
}
