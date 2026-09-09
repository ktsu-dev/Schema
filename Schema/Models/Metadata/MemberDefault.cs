// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using System.Globalization;
using System.Text.Json.Serialization;

/// <summary>
/// The value a member takes when none is supplied.
/// </summary>
/// <remarks>
/// Polymorphic rather than a single string, matching how <see cref="Types.BaseType"/> is
/// modelled: an editor can then offer a number box for a number and a picker for an enum
/// without parsing text to find out which it has, and a malformed default is a load-time
/// failure rather than something every consumer rediscovers.
/// <para>
/// A default is not the same as a zeroed value, and the distinction matters wherever zeroed
/// memory is a valid instance: <c>default(T)</c> in C# and a value-initialised struct in C++
/// are all-zero bytes, which is rarely what a schema means by "the default".
/// </para>
/// </remarks>
[JsonDerivedType(typeof(NumberDefault), nameof(NumberDefault))]
[JsonDerivedType(typeof(BooleanDefault), nameof(BooleanDefault))]
[JsonDerivedType(typeof(TextDefault), nameof(TextDefault))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "DefaultKind")]
public abstract class MemberDefault
{
	/// <summary>
	/// Returns the value in a form suitable for display.
	/// </summary>
	/// <returns>The default value as text.</returns>
	public abstract override string ToString();
}

/// <summary>
/// A numeric default, for integral, decimal and vector members.
/// </summary>
public sealed class NumberDefault : MemberDefault
{
	/// <summary>
	/// Gets the value, in the member's own unit.
	/// </summary>
	/// <remarks>
	/// Held as a <see cref="double"/> whatever the member's numeric type, so that one class
	/// covers every numeric member. A consumer narrows it to the member's type, and
	/// validation is what checks the value survives that narrowing.
	/// </remarks>
	public double Value { get; init; }

	/// <inheritdoc />
	public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// A boolean default.
/// </summary>
public sealed class BooleanDefault : MemberDefault
{
	/// <summary>
	/// Gets the value.
	/// </summary>
	public bool Value { get; init; }

	/// <inheritdoc />
	public override string ToString() => Value ? "true" : "false";
}

/// <summary>
/// A textual default: the contents of a string member, or the name of an enum value.
/// </summary>
public sealed class TextDefault : MemberDefault
{
	/// <summary>
	/// Gets the value.
	/// </summary>
	/// <remarks>
	/// For an enum member this is the value's name rather than its ordinal, so reordering an
	/// enum cannot silently change what the default means.
	/// </remarks>
	public string Value { get; init; } = string.Empty;

	/// <inheritdoc />
	public override string ToString() => Value;
}
