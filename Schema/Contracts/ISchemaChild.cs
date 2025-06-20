// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a child element of a schema with a specific name type.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public interface ISchemaChild<TName> where TName : ISchemaChildName
{
	/// <summary>
	/// Gets the name of the schema child.
	/// </summary>
	public TName Name { get; set; }

	/// <summary>
	/// Gets or sets the description of the schema child.
	/// </summary>
	public ISchemaChildDescription Description { get; set; }

	/// <summary>
	/// Gets the parent schema that owns this child element.
	/// All schema children maintain a reference to their root schema.
	/// </summary>
	public ISchema? ParentSchema { get; }

	/// <summary>
	/// Gets or sets the summary of this child.
	/// </summary>
	public ISchemaChildSummary Summary { get; set; }
}
