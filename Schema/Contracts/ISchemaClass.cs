// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Models.Names;

/// <summary>
/// Defines a class within a schema.
/// </summary>
public interface ISchemaClass : ISchemaChild<ClassName>
{
	/// <summary>
	/// Gets the members of the schema class, in declaration order.
	/// </summary>
	/// <remarks>
	/// Member order is the declaration order generated code will use, so it is part of the
	/// schema's meaning rather than a display concern.
	/// </remarks>
	public ISchemaChildSet<ISchemaMember, MemberName> Members { get; }

	/// <summary>
	/// Adds a member to the schema class.
	/// </summary>
	/// <param name="name">The name of the member to add.</param>
	/// <returns>The added member, or <see langword="null"/> if the name is already taken.</returns>
	public ISchemaMember? AddMember(MemberName name);

	/// <summary>
	/// Removes a member from the schema class.
	/// </summary>
	/// <param name="name">The name of the member to remove.</param>
	/// <returns><see langword="true"/> if a member with that name was found and removed; otherwise, <see langword="false"/>.</returns>
	public bool RemoveMember(MemberName name);
}
