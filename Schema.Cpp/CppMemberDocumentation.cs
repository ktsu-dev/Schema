// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Globalization;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;

/// <summary>
/// Writes what a generated member means, above the declaration that cannot say it.
/// </summary>
/// <remarks>
/// A C++ type says what a value <em>is</em>. Six things the schema knows about a member - its
/// unit, its bounds, whether those bounds wrap, how two values blend, how it is quantised on the
/// wire, and how an editor should present it - are not expressible in any of them, and a generated
/// file that dropped them would look complete while having lost the only facts worth knowing about
/// the field.
/// <para>
/// So they are comments, in a fixed order, one per line. Not because comments are a good place for
/// data, but because the alternative is a field whose unit lives only in the schema file the reader
/// does not have open.
/// </para>
/// </remarks>
internal static class CppMemberDocumentation
{
	/// <summary>
	/// Writes the lines that go above a member.
	/// </summary>
	/// <param name="member">The member.</param>
	/// <returns>The lines, description first, in the order the exemplar fixed.</returns>
	public static IEnumerable<string> For(SchemaMember member)
	{
		Ensure.NotNull(member);

		if (!string.IsNullOrEmpty(member.Description))
		{
			yield return member.Description.ToString();
		}

		foreach (string line in ForCarrier(member))
		{
			yield return line;
		}
	}

	/// <summary>
	/// Writes the lines for anything carrying the six semantic properties, which is a member or a
	/// semantic type.
	/// </summary>
	/// <param name="carrier">What carries them.</param>
	/// <returns>The lines.</returns>
	public static IEnumerable<string> ForCarrier(ISchemaMetadataCarrier carrier)
	{
		Ensure.NotNull(carrier);

		if (carrier.Unit is not null)
		{
			yield return $"unit: {carrier.Unit}";
		}

		if (carrier.Range is MemberRange range)
		{
			string wraps = range.Wrap ? " (wraps)" : string.Empty;
			yield return $"range: [{Number(range.Minimum)}, {Number(range.Maximum)}]{wraps}";
		}

		if (carrier.Interpolation != Interpolation.None)
		{
			yield return carrier.Interpolation == Interpolation.Linear
				? "interpolated between states"
				: $"interpolated between states, {carrier.Interpolation.ToString().ToLowerInvariant()}";
		}

		if (carrier.Network is MemberNetwork network && network.IsQuantised)
		{
			string delta = network.Delta ? ", delta encoded" : string.Empty;
			yield return $"network: quantised to {Number(network.Quantise)}{delta}";
		}

		if (carrier.Editor is not null)
		{
			yield return $"editor: {carrier.Editor}";
		}
	}

	/// <summary>
	/// Writes a number the way the schema file holds it.
	/// </summary>
	/// <remarks>
	/// Invariant and round-trip, so a bound of a million reads as <c>1000000</c> rather than in
	/// exponent form, and one of a thousandth keeps its digits. A comment a reader compares
	/// against the schema by eye has to say the same thing the schema does.
	/// </remarks>
	private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
