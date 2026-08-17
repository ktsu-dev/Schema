// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a member of a schema class.
/// </summary>
public interface ISchemaMember : ISchemaClassChild<ISchemaMemberName>
{
	/// <summary>
	/// Gets the type of the schema member.
	/// </summary>
	public ISchemaType Type { get; set; }
}
