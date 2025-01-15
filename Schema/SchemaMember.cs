namespace ktsu.Schema;

using System.Text.Json.Serialization;
using ktsu.Extensions;

/// <summary>
/// Represents a member of a schema class.
/// </summary>
public class SchemaMember : SchemaClassChild<MemberName>
{
	/// <summary>
	/// Gets the type of the schema member.
	/// </summary>
	[JsonInclude] public SchemaTypes.BaseType Type { get; private set; } = new SchemaTypes.None();

	/// <summary>
	/// Gets or sets the description of the schema member.
	/// </summary>
	[JsonInclude] public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Sets the type of the schema member and associates it with this member.
	/// </summary>
	/// <param name="type">The type to set.</param>
	public void SetType(SchemaTypes.BaseType type)
	{
		Type = type;
		Type.AssosciateWith(this);
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
	private MemberName Root { get; } = nameof(Root).As<MemberName>();

	/// <summary>
	/// Gets the name of the root schema member.
	/// </summary>
	[JsonInclude] public new MemberName Name => Root;

	/// <summary>
	/// Throws a NotSupportedException as renaming is not supported on the root schema member.
	/// </summary>
	/// <param name="_">The new name (not used).</param>
	/// <exception cref="NotSupportedException">Always thrown as renaming is not supported on the root schema member.</exception>
	[Obsolete("Not supported on the root schema member", true)]
	public new void Rename(MemberName _) => throw new NotSupportedException("Not supported on the root schema member");
}
