// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

/// <summary>
/// Where a target's physical quantities live, and what they are stored in.
/// </summary>
/// <remarks>
/// <para>
/// One entry for 212 types, which is what makes a quantity different from every other spelling
/// this generator is handed. A <see cref="CppTypeSpelling"/> names one type because the target
/// wrote one type; the quantities are a vocabulary <c>ktsu.Semantics.Cpp</c> emits whole, so what
/// a target has to say is where it put them, not what it called each one. It called each one what
/// the schema calls it.
/// </para>
/// <para>
/// <see cref="Storage"/> is the fact the schema and the target can disagree about. C# spells a
/// quantity <c>Mass&lt;float&gt;</c> and closes it per member; the C++ vocabulary is generated
/// over one numeric type and <c>holo::Mass</c> is a class rather than a template, so a member the
/// schema says is a double has no C++ spelling on a target whose quantities are floats. Saying
/// which one the target generated is what lets that be refused rather than emitted as two
/// languages quietly disagreeing about the bytes.
/// </para>
/// </remarks>
/// <param name="Namespace">
/// The namespace the quantities were generated into, unqualified and without trailing colons;
/// empty for a target that generated them at global scope.
/// </param>
/// <param name="Include">
/// The header that declares them. The vocabulary ships an umbrella header, so this is one include
/// rather than one per quantity; written with angle brackets for a system header and quoted
/// otherwise, as every other include here is.
/// </param>
/// <param name="Storage">
/// The schema type the target's quantities are stored in, named as the schema names it -
/// <c>Float</c>, <c>Double</c>, <c>Int</c> or <c>Long</c>. Defaults to <c>Float</c>, which is what
/// <c>ktsu.Semantics.Cpp</c> generates unless told otherwise.
/// </param>
public sealed record CppQuantitySpelling(string Namespace, string Include, string Storage = "Float")
{
	/// <summary>
	/// Gets how a quantity of this vocabulary is written.
	/// </summary>
	/// <param name="name">The quantity's name, as the schema writes it.</param>
	/// <returns>The qualified C++ type name.</returns>
	public string Qualified(string name) =>
		string.IsNullOrEmpty(Namespace) ? name : $"{Namespace}::{name}";
}
