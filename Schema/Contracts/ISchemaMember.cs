// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Models.Names;

/// <summary>
/// Defines a member of a schema class.
/// </summary>
public interface ISchemaMember : ISchemaClassChild<MemberName>
{
	/// <summary>
	/// Gets the type of the schema member.
	/// </summary>
	public ISchemaType Type { get; }

	/// <summary>
	/// Sets the type of the schema member.
	/// </summary>
	/// <remarks>
	/// A method rather than a settable property because setting a type also associates it with
	/// this member, which is what gives the type a route back to the schema. A plain setter
	/// invites assigning a type that resolves none of its own references.
	/// </remarks>
	/// <param name="type">The type to set.</param>
	public void SetType(ISchemaType type);
}
