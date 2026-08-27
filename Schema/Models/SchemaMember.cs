// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a member of a schema class.
/// </summary>
public class SchemaMember : SchemaClassChild<MemberName>
{
	/// <summary>
	/// Gets the type of the schema member.
	/// </summary>
	/// <remarks>
	/// <see cref="JsonIncludeAttribute"/> is required because the setter is private: without it
	/// System.Text.Json writes the type on save but silently ignores it on load, so every member
	/// in a loaded schema came back as <see cref="None"/>.
	/// </remarks>
	[JsonInclude]
	public BaseType Type { get; private set; } = new None();

	/// <summary>
	/// Reads the description written by versions that stored it under "memberDescription".
	/// </summary>
	/// <remarks>
	/// A member inherited <see cref="SchemaChild{TName}.Description"/> and also carried its own
	/// <c>MemberDescription</c>, of a different type, with nothing documenting which one meant
	/// what. The two are now collapsed onto <see cref="SchemaChild{TName}.Description"/>. This
	/// property exists only so an older file's value is migrated onto that field instead of being
	/// dropped: the getter returns null, and the serializer ignores nulls when writing, so the
	/// legacy property is read but never written again.
	/// </remarks>
	[JsonInclude]
	[JsonPropertyName("memberDescription")]
	internal string? LegacyMemberDescription
	{
		get => null;
		set
		{
			if (!string.IsNullOrEmpty(value) && string.IsNullOrEmpty(Description))
			{
				Description = value.As<SchemaChildDescription>();
			}
		}
	}

	/// <summary>
	/// Sets the type of the schema member and associates it with this member.
	/// </summary>
	/// <param name="type">The type to set.</param>
	public void SetType(BaseType type)
	{
		Type = type;
		Type.AssociateWith(this);
	}

	/// <summary>
	/// Tries to remove the schema member from its parent class.
	/// </summary>
	/// <returns>True if the member was successfully removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentClass?.TryRemoveMember(this) ?? false;
}

/// <summary>
/// Represents the root member of a schema.
/// </summary>
public class RootSchemaMember : SchemaMember
{
	/// <summary>
	/// Throws a NotSupportedException as renaming is not supported on the root schema member.
	/// </summary>
	/// <param name="_">The new name (not used).</param>
	/// <exception cref="NotSupportedException">Always thrown as renaming is not supported on the root schema member.</exception>
	[Obsolete("Not supported on the root schema member", true)]
	public new void Rename(MemberName _) => throw new NotSupportedException("Not supported on the root schema member");
}
