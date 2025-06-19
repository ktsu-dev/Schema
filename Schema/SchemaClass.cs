// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using System.Collections.ObjectModel;

/// <summary>
/// Represents a class within a schema.
/// </summary>
public class SchemaClass : SchemaChild<ClassName>
{
	/// <summary>
	/// Gets or sets the internal collection of schema members.
	/// </summary>
	private Collection<SchemaMember> MembersInternal { get; set; } = [];

	/// <summary>
	/// Gets the read-only collection of schema members.
	/// </summary>
	public IReadOnlyCollection<SchemaMember> Members => MembersInternal;

	/// <summary>
	/// Adds a member to the schema class.
	/// </summary>
	/// <param name="memberName">The name of the member to add.</param>
	/// <returns>The added schema member.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the schema class does not belong to a schema provider.</exception>
	public SchemaMember AddMember(MemberName memberName) => ParentSchema?.AddChild(memberName, MembersInternal) ?? throw new InvalidOperationException("You can not add members to a SchemaClass that doesn't belong to a SchemaProvider");

	/// <summary>
	/// Tries to get a member from the schema class.
	/// </summary>
	/// <param name="memberName">The name of the member to get.</param>
	/// <param name="schemaMember">The retrieved schema member, if found.</param>
	/// <returns>True if the member was found, false otherwise.</returns>
	public bool TryGetMember(MemberName memberName, out SchemaMember? schemaMember) => SchemaProvider.TryGetChild(memberName, MembersInternal, out schemaMember);

	/// <summary>
	/// Tries to remove a member from the schema class.
	/// </summary>
	/// <param name="schemaMember">The schema member to remove.</param>
	/// <returns>True if the member was removed, false otherwise.</returns>
	public bool TryRemoveMember(SchemaMember schemaMember) => SchemaProvider.TryRemoveChild(schemaMember, MembersInternal);

	/// <summary>
	/// Attempts to remove this schema class from its parent schema provider.
	/// </summary>
	/// <returns>True if the schema class was removed, false otherwise.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the schema class does not belong to a schema provider.</exception>
	public override bool TryRemove() => ParentSchema?.TryRemoveClass(this) ?? throw new InvalidOperationException("You can not remove a SchemaClass that doesn't belong to a SchemaProvider");

	/// <summary>
	/// Returns a summary of the schema class.
	/// </summary>
	/// <returns>A summary of the schema class.</returns>
	public override string Summary() => $"{Name} ({MembersInternal.Count})";
}
