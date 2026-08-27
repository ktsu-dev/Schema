// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;
using ktsu.Schema.Models;

/// <summary>
/// Defines a child element of a schema with a specific name type.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public interface ISchemaChild<TName> where TName : ISchemaChildName
{
	/// <summary>
	/// Gets or sets the name of the schema child.
	/// </summary>
	public TName Name { get; set; }

	/// <summary>
	/// Gets or sets the description of the schema child.
	/// </summary>
	public SchemaChildDescription Description { get; set; }

	/// <summary>
	/// Gets the parent schema that owns this child element.
	/// All schema children maintain a reference to their root schema.
	/// </summary>
	public ISchema? ParentSchema { get; }
}
