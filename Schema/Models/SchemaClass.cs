// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents a class within a schema.
/// </summary>
public class SchemaClass : SchemaChild<ClassName>
{
	/// <summary>
	/// Gets the internal collection of members.
	/// </summary>
	internal Collection<SchemaMember> MembersInternal { get; set; } = [];

	/// <summary>
	/// Gets the members of the schema class.
	/// </summary>
	public IReadOnlyCollection<SchemaMember> Members => MembersInternal;

	/// <summary>
	/// Gets a summary of the schema class.
	/// </summary>
	public string ClassSummary => $"{Name} ({Members.Count})";

	/// <summary>
	/// Tries to remove this class from its parent schema.
	/// </summary>
	/// <returns>True if the class was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveClass(this) ?? false;

	/// <summary>
	/// Adds a member to the schema class.
	/// </summary>
	/// <param name="name">The name of the member to add.</param>
	/// <returns>The added member if successful, null otherwise.</returns>
	public SchemaMember? AddMember(MemberName name)
	{
		Ensure.NotNull(name);

		if (MembersInternal.Any(m => m.Name == name))
		{
			return null;
		}

		SchemaMember member = new();
		member.Rename(name);
		member.AssociateWith(this);
		MembersInternal.Add(member);
		return member;
	}

	/// <summary>
	/// Tries to remove a member from the schema class.
	/// </summary>
	/// <param name="member">The member to remove.</param>
	/// <returns>True if the member was removed; otherwise, false.</returns>
	internal bool TryRemoveMember(SchemaMember member) => MembersInternal.Remove(member);

	/// <summary>
	/// Tries to get a member by name.
	/// </summary>
	/// <param name="name">The name of the member to find.</param>
	/// <param name="member">The found member, if any.</param>
	/// <returns>True if the member was found; otherwise, false.</returns>
	public bool TryGetMember(MemberName name, out SchemaMember? member)
	{
		member = MembersInternal.FirstOrDefault(m => m.Name == name);
		return member is not null;
	}

	/// <summary>
	/// Gets a member by name.
	/// </summary>
	/// <param name="name">The name of the member.</param>
	/// <returns>The member if found, null otherwise.</returns>
	public SchemaMember? GetMember(MemberName name) => MembersInternal.FirstOrDefault(m => m.Name == name);
}
