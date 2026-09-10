// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Contracts;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Quantities;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a member of a schema class.
/// </summary>
public class SchemaMember : SchemaClassChild<MemberName>, ISchemaMember, ISchemaMetadataCarrier
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

	/// <remarks>
	/// Explicit because the contract exposes the type as <see cref="ISchemaType"/> while the model
	/// exposes the concrete <see cref="BaseType"/>; both are the same object.
	/// </remarks>
	ISchemaType ISchemaMember.Type => Type;

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

	/// <remarks>
	/// Explicit because the contract takes the type as <see cref="ISchemaType"/>. Every type the
	/// library can store derives from <see cref="BaseType"/> — the polymorphic serializer knows no
	/// other — so a type from outside that hierarchy is rejected rather than stored as something
	/// nothing else can read.
	/// </remarks>
	void ISchemaMember.SetType(ISchemaType type) =>
		SetType(type as BaseType ?? throw new ArgumentException($"The type must derive from {nameof(BaseType)}.", nameof(type)));

	/// <summary>
	/// Gets or sets the unit this member's values are measured in, if any.
	/// </summary>
	/// <remarks>
	/// A symbol (<c>m/s</c>) or a unit name (<c>MeterPerSecond</c>), resolved against
	/// <see cref="UnitRegistry"/>. Null on a member that measures nothing — an identifier, a
	/// name, a flag.
	/// <para>
	/// The text is stored rather than a resolved unit so that the file stays readable and the
	/// conversion factors stay owned by <c>ktsu.Semantics.Quantities</c>. Use
	/// <see cref="TryResolveUnit"/> to get the unit itself.
	/// </para>
	/// </remarks>
	public UnitSymbol? Unit { get; set; }

	/// <summary>
	/// Gets or sets the range of values this member may take, if it is bounded.
	/// </summary>
	public MemberRange? Range { get; set; }

	/// <summary>
	/// Gets or sets the value this member takes when none is supplied.
	/// </summary>
	public MemberDefault? DefaultValue { get; set; }

	/// <summary>
	/// Gets or sets how this member should be encoded when sent over a network.
	/// </summary>
	/// <remarks>
	/// Null means no guidance, which a codec should read as "send it verbatim, every time".
	/// </remarks>
	public MemberNetwork? Network { get; set; }

	/// <summary>
	/// Gets or sets how two states of this member may be blended.
	/// </summary>
	public Interpolation Interpolation { get; set; }

	/// <summary>
	/// Gets or sets a hint about how an editor should present this member.
	/// </summary>
	public EditorHint? Editor { get; set; }

	/// <summary>
	/// Resolves <see cref="Unit"/> to a unit from <c>ktsu.Semantics.Quantities</c>.
	/// </summary>
	/// <param name="unit">The resolved unit, or null when the member has no unit or it does
	/// not resolve.</param>
	/// <param name="error">Why it did not resolve. Empty when the member has no unit at all,
	/// since that is not an error.</param>
	/// <returns><see langword="true"/> when the member has a unit and it resolved.</returns>
	public bool TryResolveUnit(out IUnit? unit, out string error)
	{
		unit = null;
		error = string.Empty;
		return Unit is not null && UnitRegistry.TryResolve(Unit, out unit, out error);
	}

	/// <summary>
	/// Tries to remove the schema member from its parent class.
	/// </summary>
	/// <returns>True if the member was successfully removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentClass?.TryRemoveMember(this) ?? false;
}
