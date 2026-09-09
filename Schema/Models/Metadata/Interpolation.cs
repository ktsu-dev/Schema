// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using System.Text.Json.Serialization;

/// <summary>
/// How two states of a member may be blended.
/// </summary>
/// <remarks>
/// Not every value has a meaningful midpoint. Halfway between two enum values, two identifiers
/// or two strings is not a value of that type at all, so <see cref="None"/> is the default and
/// anything else is a claim the schema author is making.
/// </remarks>
/// <remarks>
/// Written to the file by name rather than by ordinal. An ordinal would mean that inserting a
/// mode into this enum silently changed the meaning of every schema already written — the same
/// reason an enum default is stored as a value's name.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<Interpolation>))]
public enum Interpolation
{
	/// <summary>
	/// The member is not interpolated. A consumer blending between two states should hold the
	/// earlier value until the later one applies.
	/// </summary>
	None,

	/// <summary>
	/// Blended linearly between the two values.
	/// </summary>
	Linear,

	/// <summary>
	/// Blended along the shortest arc. Correct for orientations and for angles that wrap,
	/// where linear blending would take the long way round the discontinuity.
	/// </summary>
	Spherical,

	/// <summary>
	/// Held at the earlier value and changed at the later one, with no intermediate value.
	/// Distinct from <see cref="None"/>: this says stepping is the intended behaviour rather
	/// than that interpolation is meaningless.
	/// </summary>
	Step,
}
