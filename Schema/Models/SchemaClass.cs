// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents a class within a schema.
/// </summary>
public class SchemaClass : SchemaChild<ClassName>, ISchemaClass
{
	/// <summary>
	/// Gets the members of the schema class.
	/// </summary>
	public ISchemaChildSet<ISchemaMember, ISchemaMemberName> Members { get; } = new SchemaChildSet<ISchemaMember, ISchemaMemberName>();

	/// <summary>
	/// Returns a summary of the schema class.
	/// </summary>
	/// <returns>A summary of the schema class.</returns>
	public string GetSummary() => $"{Name} ({Members.Count})";

	/// <summary>
	/// Tries to remove this class from its parent schema.
	/// </summary>
	/// <returns>True if the class was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveClass(this) ?? false;
}
