// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a child of a schema function.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public abstract class SchemaFunctionChild<TName> : SchemaChild<TName>
	where TName : SemanticString<TName>, ISchemaFunctionChildName, new()
{
	/// <summary>
	/// Gets the parent function of this child.
	/// </summary>
	[JsonIgnore]
	public SchemaFunction? ParentFunction { get; private set; }

	/// <summary>
	/// Associates this child with a parent function.
	/// </summary>
	/// <param name="schemaFunction">The parent function to associate with.</param>
	public void AssociateWith(SchemaFunction schemaFunction)
	{
		Ensure.NotNull(schemaFunction);
		ParentFunction = schemaFunction;
		if (schemaFunction.ParentSchema is not null)
		{
			AssociateWith(schemaFunction.ParentSchema);
		}
	}
}
