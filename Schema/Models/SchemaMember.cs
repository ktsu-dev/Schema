// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// Represents a member of a schema class.
/// </summary>
public class SchemaMember : SchemaClassChild<MemberName>
{
	/// <summary>
	/// Gets the type of the schema member.
	/// </summary>
	public BaseType Type { get; private set; } = new None();

	/// <summary>
	/// Gets or sets the member description text.
	/// </summary>
	public string MemberDescription { get; set; } = string.Empty;

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
