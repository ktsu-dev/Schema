// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Models.Names;

/// <summary>
/// Represents a schema type that can be part of a schema member.
/// </summary>
/// <remarks>
/// A type is not a named child of the schema the way a class or member is: it has no name or
/// description of its own, and exists only as the type of the member that holds it. It is
/// identified by which type it is, which is what <see cref="TypeName"/> reports.
/// </remarks>
public interface ISchemaType
{
	/// <summary>
	/// Gets the name identifying which type this is.
	/// </summary>
	/// <remarks>
	/// This is the discriminator written to and read from the schema file's <c>TypeName</c>
	/// property, so it is stable across versions in the way the file format is.
	/// </remarks>
	public BaseTypeName TypeName { get; }

	/// <summary>
	/// Gets the member this type belongs to, if it has been associated with one.
	/// </summary>
	public ISchemaMember? ParentMember { get; }
}
