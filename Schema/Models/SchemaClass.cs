// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents a class within a schema.
/// </summary>
public class SchemaClass : SchemaChild<ClassName>
{
	/// <summary>
	/// Gets the internal collection of members.
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("members")]
	internal Collection<SchemaMember> MembersInternal { get; set; } = [];

	/// <summary>
	/// Gets the members of the schema class.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<SchemaMember> Members => MembersInternal;

	/// <summary>
	/// Gets a summary of the schema class.
	/// </summary>
	[JsonIgnore]
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
	/// Restores a previously removed member back into the class.
	/// Used for undo operations where the original object reference is preserved.
	/// </summary>
	/// <param name="member">The member to restore.</param>
	/// <returns>True if the member was restored; false if a member with the same name already exists.</returns>
	public bool RestoreMember(SchemaMember member)
	{
		Ensure.NotNull(member);

		if (MembersInternal.Any(m => m.Name == member.Name))
		{
			return false;
		}

		member.AssociateWith(this);
		MembersInternal.Add(member);
		return true;
	}

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

	/// <summary>
	/// Gets the position of a member in the class's declaration order.
	/// </summary>
	/// <param name="member">The member to locate.</param>
	/// <returns>The member's index, or -1 if it does not belong to this class.</returns>
	public int IndexOfMember(SchemaMember member) => MembersInternal.IndexOf(member);

	/// <summary>
	/// Renames a member, repointing any array key that named it.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="SchemaChild{TName}.Rename"/> this rejects a name already taken by a
	/// sibling member, and fixes up the <see cref="Types.Array.Key"/> of every array elsewhere in
	/// the schema that keys on this class by the member's old name.
	/// </remarks>
	/// <param name="member">The member to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the member was renamed; false if it is not a member of this class, or the name is empty or collides.</returns>
	public bool TryRenameMember(SchemaMember member, MemberName newName)
	{
		Ensure.NotNull(member);
		Ensure.NotNull(newName);

		if (!MembersInternal.Contains(member) || string.IsNullOrEmpty(newName))
		{
			return false;
		}

		if (member.Name != newName && MembersInternal.Any(m => m.Name == newName))
		{
			return false;
		}

		MemberName oldName = member.Name;
		member.Rename(newName);
		ParentSchema?.RepointArrayKeys(Name, oldName, newName);
		return true;
	}

	/// <summary>
	/// Moves a member to a new position in the class's declaration order.
	/// </summary>
	/// <remarks>
	/// Member order is preserved through serialization and is the declaration order any generated
	/// code will use, so it is part of the schema's meaning rather than a display concern.
	/// </remarks>
	/// <param name="member">The member to move.</param>
	/// <param name="newIndex">The zero-based position to move it to.</param>
	/// <returns>True if the member was moved; false if it does not belong to this class or the index is out of range.</returns>
	public bool TryMoveMember(SchemaMember member, int newIndex)
	{
		Ensure.NotNull(member);

		int currentIndex = MembersInternal.IndexOf(member);
		if (currentIndex < 0 || newIndex < 0 || newIndex >= MembersInternal.Count)
		{
			return false;
		}

		if (newIndex == currentIndex)
		{
			return true;
		}

		MembersInternal.RemoveAt(currentIndex);
		MembersInternal.Insert(newIndex, member);
		return true;
	}
}
