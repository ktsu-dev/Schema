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
/// <para>
/// Settable, because reading is not the only thing done to both alike:
/// <see cref="ClrTypeImporter"/> restores all six from the attributes a generator wrote them into,
/// and a member and a semantic type are restored identically. The read-only view of a member is
/// <see cref="Contracts.ISchemaMember"/>, which is the abstraction seam; this is the models' own.
/// </para>
/// </remarks>
public interface ISchemaMetadataCarrier
{
	/// <summary>
	/// Gets or sets the unit the value is measured in.
	/// </summary>
	public UnitSymbol? Unit { get; set; }

	/// <summary>
	/// Gets or sets the range the value is bounded to, if any.
	/// </summary>
	public MemberRange? Range { get; set; }

	/// <summary>
	/// Gets or sets the value taken when none is supplied.
	/// </summary>
	public MemberDefault? DefaultValue { get; set; }

	/// <summary>
	/// Gets or sets how the value should be encoded when sent over a network.
	/// </summary>
	public MemberNetwork? Network { get; set; }

	/// <summary>
	/// Gets or sets how two states of the value may be blended.
	/// </summary>
	public Interpolation Interpolation { get; set; }

	/// <summary>
	/// Gets or sets a hint about how an editor should present the value.
	/// </summary>
	public EditorHint? Editor { get; set; }
}
