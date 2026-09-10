// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Metadata;

/// <summary>
/// Something that carries the six semantic properties a value can have.
/// </summary>
/// <remarks>
/// A member carries them because a use of a value is where a unit or a range is often decided. A
/// semantic type carries them because for some values those facts belong to the type itself:
/// <c>Metres</c> is always metres, and a normalised ratio is always in [0, 1] wherever it appears.
/// The rules for whether a given combination makes sense are identical in both cases, so this is
/// the shape validation reads rather than each carrier growing its own copy of them.
/// </remarks>
public interface ISchemaMetadataCarrier
{
	/// <summary>
	/// Gets the unit the value is measured in.
	/// </summary>
	public UnitSymbol? Unit { get; }

	/// <summary>
	/// Gets the range the value is bounded to, if any.
	/// </summary>
	public MemberRange? Range { get; }

	/// <summary>
	/// Gets the value taken when none is supplied.
	/// </summary>
	public MemberDefault? DefaultValue { get; }

	/// <summary>
	/// Gets how the value should be encoded when sent over a network.
	/// </summary>
	public MemberNetwork? Network { get; }

	/// <summary>
	/// Gets how two states of the value may be blended.
	/// </summary>
	public Interpolation Interpolation { get; }

	/// <summary>
	/// Gets a hint about how an editor should present the value.
	/// </summary>
	public EditorHint? Editor { get; }
}
