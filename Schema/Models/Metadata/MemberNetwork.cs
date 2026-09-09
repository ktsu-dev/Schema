// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using System.Text.Json.Serialization;

/// <summary>
/// How a member should be encoded when its value is sent over a network.
/// </summary>
/// <remarks>
/// Neither of these is expressible in the member's type, which is why they belong to the
/// schema: the type says what a value <em>is</em>, and this says what a peer needs to know
/// about it. Both are advisory — a codec that ignores them is still correct, just larger.
/// </remarks>
public sealed class MemberNetwork
{
	/// <summary>
	/// Gets the smallest change worth transmitting, in the member's own unit.
	/// </summary>
	/// <remarks>
	/// Zero means send the value at full precision. A velocity good to a centimetre per
	/// second does not need thirty-two bits, and the quantised value is what a codec puts on
	/// the wire, so the round trip is accurate to half of this and no better.
	/// </remarks>
	public double Quantise { get; init; }

	/// <summary>
	/// Gets a value indicating whether the member should be sent only when it differs from
	/// the last state the peer acknowledged.
	/// </summary>
	public bool Delta { get; init; }

	/// <summary>
	/// Gets a value indicating whether the member is quantised.
	/// </summary>
	/// <remarks>
	/// Not serialized: it is derived from <see cref="Quantise"/>.
	/// </remarks>
	[JsonIgnore]
	public bool IsQuantised => Quantise > 0.0;

	/// <summary>
	/// Returns the encoding as text.
	/// </summary>
	/// <returns>A description of the quantisation and delta settings.</returns>
	public override string ToString() =>
		$"{(IsQuantised ? $"quantised to {Quantise}" : "full precision")}{(Delta ? ", delta encoded" : string.Empty)}";
}
