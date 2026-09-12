// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

using ktsu.Schema.Models.Metadata;

/// <summary>
/// Records the unit a generated member's values are measured in.
/// </summary>
/// <remarks>
/// The family of attributes in this file exists for the reason
/// <see cref="SchemaKeyAttribute"/> does: a C# type says what a value <em>is</em> and nothing
/// about what it means, so a <c>float</c> emitted for a member measured in metres per second is
/// indistinguishable from one measured in nothing at all. Reimporting generated code would drop
/// every one of these and give back a schema subtly different from the one that produced it.
/// <para>
/// The text is the same spelling the schema file holds - a symbol or a unit name - and resolves
/// through <see cref="UnitRegistry"/>.
/// </para>
/// </remarks>
/// <param name="unit">The unit's symbol or name.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaUnitAttribute(string unit) : Attribute
{
	/// <summary>
	/// Gets the unit's symbol or name.
	/// </summary>
	public string Unit { get; } = unit;
}

/// <summary>
/// Records the range a generated member's values may take.
/// </summary>
/// <param name="minimum">The smallest allowed value, in the member's own unit.</param>
/// <param name="maximum">The largest allowed value, in the member's own unit.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaRangeAttribute(double minimum, double maximum) : Attribute
{
	/// <summary>
	/// Gets the smallest allowed value.
	/// </summary>
	public double Minimum { get; } = minimum;

	/// <summary>
	/// Gets the largest allowed value.
	/// </summary>
	public double Maximum { get; } = maximum;

	/// <summary>
	/// Gets or sets a value indicating whether values outside the range wrap into it rather than
	/// being invalid.
	/// </summary>
	/// <remarks>
	/// A named argument rather than a third constructor parameter, because it is absent from most
	/// ranges and a bare <c>true</c> at a call site says nothing about which flag it is.
	/// </remarks>
	public bool Wrap { get; set; }
}

/// <summary>
/// Records the value a generated member takes when none is supplied.
/// </summary>
/// <remarks>
/// One attribute with a constructor per kind of value rather than three attributes, because a
/// member has one default: the overload chosen at the call site is what says which kind it is,
/// and <see cref="Value"/> hands back what that overload was given.
/// <para>
/// Distinct from the property initialiser a generator also emits. The initialiser makes an
/// instance start at the default; this is what lets the default be read back off the type.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaDefaultAttribute : Attribute
{
	/// <summary>
	/// Records a numeric default, for an integral, decimal or vector member.
	/// </summary>
	/// <param name="value">The default value.</param>
	public SchemaDefaultAttribute(double value) => Value = value;

	/// <summary>
	/// Records a boolean default.
	/// </summary>
	/// <param name="value">The default value.</param>
	public SchemaDefaultAttribute(bool value) => Value = value;

	/// <summary>
	/// Records a textual default: a string member's contents, or the name of an enum value.
	/// </summary>
	/// <param name="value">The default value.</param>
	public SchemaDefaultAttribute(string value) => Value = value;

	/// <summary>
	/// Gets the default value, as a <see cref="double"/>, <see cref="bool"/> or
	/// <see cref="string"/> according to the constructor used.
	/// </summary>
	public object Value { get; }
}

/// <summary>
/// Records how two states of a generated member may be blended.
/// </summary>
/// <param name="mode">The interpolation mode.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaInterpolationAttribute(Interpolation mode) : Attribute
{
	/// <summary>
	/// Gets the interpolation mode.
	/// </summary>
	public Interpolation Mode { get; } = mode;
}

/// <summary>
/// Records how a generated member should be encoded when sent over a network.
/// </summary>
/// <param name="quantise">The smallest change worth transmitting. Zero means full precision.</param>
/// <param name="delta">Whether to send the member only when it differs from the last acknowledged state.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaNetworkAttribute(double quantise, bool delta) : Attribute
{
	/// <summary>
	/// Gets the smallest change worth transmitting, in the member's own unit.
	/// </summary>
	public double Quantise { get; } = quantise;

	/// <summary>
	/// Gets a value indicating whether the member is sent only when it has changed.
	/// </summary>
	public bool Delta { get; } = delta;
}

/// <summary>
/// Records how an editor should present a generated member.
/// </summary>
/// <param name="hint">The hint, which is free text.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaEditorHintAttribute(string hint) : Attribute
{
	/// <summary>
	/// Gets the hint.
	/// </summary>
	public string Hint { get; } = hint;
}

/// <summary>
/// Records that instances of a generated type travel as raw bytes, so its member order is part of
/// what it means.
/// </summary>
/// <remarks>
/// Here for the same reason the rest of this file is: nothing about a CLR type says whether it is
/// copied whole without anyone reading a field on the way, so reimporting generated code would give
/// back a schema whose classes had quietly stopped promising it. A class attribute rather than a
/// member one, because the promise is the type's.
/// <para>
/// It records the promise; the generated type keeps it. A promising class is emitted as a
/// <c>struct</c> with a sequential layout, which is what makes its member order load-bearing in C#
/// as well. Sequential layout is a fact about a type rather than a statement of intent, though, and
/// a hand-written struct may have it for reasons of its own - so the promise cannot be read back
/// off the shape. That is what this is for, and it is also how
/// <see cref="Models.ClrTypeImporter"/> tells a generated struct from any other value type.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class SchemaTravelsAsBytesAttribute : Attribute
{
}
