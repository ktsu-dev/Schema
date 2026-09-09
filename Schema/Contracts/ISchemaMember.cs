// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Models.Metadata;
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

	/// <summary>
	/// Gets the unit this member's values are measured in, or null if it measures nothing.
	/// </summary>
	public UnitSymbol? Unit { get; }

	/// <summary>
	/// Gets the range of values this member may take, or null if it is unbounded.
	/// </summary>
	public MemberRange? Range { get; }

	/// <summary>
	/// Gets the value this member takes when none is supplied, or null if it has no default.
	/// </summary>
	public MemberDefault? DefaultValue { get; }

	/// <summary>
	/// Gets how this member should be encoded when sent over a network, or null for no guidance.
	/// </summary>
	public MemberNetwork? Network { get; }

	/// <summary>
	/// Gets how two states of this member may be blended.
	/// </summary>
	public Interpolation Interpolation { get; }

	/// <summary>
	/// Gets a hint about how an editor should present this member, or null for none.
	/// </summary>
	public EditorHint? Editor { get; }
}
