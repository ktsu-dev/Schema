// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using System.Text.Json.Serialization;

/// <summary>
/// The range of values a member is allowed to take.
/// </summary>
/// <remarks>
/// Expressed in the member's own unit when it has one, so a range on a member measured in
/// <c>km</c> is in kilometres, not metres.
/// </remarks>
public sealed class MemberRange
{
	/// <summary>
	/// Gets the smallest allowed value.
	/// </summary>
	public double Minimum { get; init; }

	/// <summary>
	/// Gets the largest allowed value.
	/// </summary>
	public double Maximum { get; init; }

	/// <summary>
	/// Gets a value indicating whether values outside the range wrap into it rather than
	/// being invalid.
	/// </summary>
	/// <remarks>
	/// For a cyclic quantity the range is a period, not a bound: an angle of 7 radians on a
	/// <c>[0, 2π)</c> member is un-normalised rather than wrong, and clamping it to the
	/// maximum would be the one transformation that is certainly incorrect. A validator
	/// should reduce a wrapping value into range; it should reject a non-wrapping one.
	/// </remarks>
	public bool Wrap { get; init; }

	/// <summary>
	/// Gets a value indicating whether the range is satisfiable.
	/// </summary>
	/// <remarks>
	/// Not serialized: it is derived from the two bounds beside it, and writing it would put
	/// a second, independently editable copy of that fact in the file.
	/// </remarks>
	[JsonIgnore]
	public bool IsWellFormed => Minimum <= Maximum;

	/// <summary>
	/// Returns the range in interval notation.
	/// </summary>
	/// <returns>A string such as <c>[0, 1]</c>, suffixed when the range wraps.</returns>
	public override string ToString() =>
		$"[{Minimum}, {Maximum}]{(Wrap ? " (wraps)" : string.Empty)}";
}
