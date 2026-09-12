// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;

using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Types;

/// <summary>
/// The vocabulary a generated reflection table is written in, and the two enumerations of it that
/// come from the schema library rather than from C++.
/// </summary>
/// <remarks>
/// The header itself is shipped rather than generated, for the same reason
/// <c>ktsu.Semantics.Cpp</c> ships its <c>Quantity</c>: almost none of it is derived from anything,
/// and the parts that are cannot be said by the AST at all. <c>template &lt;typename T&gt; struct
/// Describe;</c> declares a type parameter, which the AST deliberately does not model — a generator
/// names a generic type, it never declares one — and a concept is not a node either.
/// <para>
/// Two lists are derived, though, and they are why the file is substituted rather than copied:
/// <see cref="TypeKinds"/> is every type a member can have and <see cref="Interpolations"/> is
/// every way two values can be blended, both read off the schema library. A type added there
/// appears here with no edit in this file and none in the target's tree, which is the whole reason
/// the C++ side of this stack emits its own vocabulary instead of sharing Holotype's.
/// </para>
/// <para>
/// Each list is written into the header twice, as the enumeration and as the names beside it, and
/// that is deliberate rather than redundant: the alternative is a <c>switch</c>, which the AST
/// cannot say and which nothing would then keep in step with the enumeration. Two spellings
/// generated from one list cannot disagree.
/// </para>
/// </remarks>
internal static class CppReflection
{
	/// <summary>The namespace the vocabulary and the tables are generated into, under the target's own.</summary>
	internal const string Namespace = "reflect";

	/// <summary>The name of the shipped header, without an extension.</summary>
	internal const string Header = "reflect";

	/// <summary>The name of the generated header, without an extension.</summary>
	internal const string TablesHeader = "reflection";

	private const string NamespaceToken = "@NAMESPACE@";
	private const string BannerToken = "@BANNER@";
	private const string TypeKindsToken = "@TYPEKINDS@";
	private const string TypeKindNamesToken = "@TYPEKINDNAMES@";
	private const string InterpolationsToken = "@INTERPOLATIONS@";
	private const string InterpolationNamesToken = "@INTERPOLATIONNAMES@";

	/// <summary>
	/// Every type a member can have, named as the schema file names it.
	/// </summary>
	/// <remarks>
	/// Read off <see cref="BaseType"/>'s polymorphic discriminators, which is the same list
	/// <see cref="BaseType.TypeName"/> answers from — so the enumerator and the <c>TypeName</c>
	/// written to a <c>.schema.json</c> are the same word, and a consumer holding one can compare
	/// it to the other.
	/// <para>
	/// Sorted rather than left in declaration order. Attribute order is not something the runtime
	/// promises, and a generated enumeration whose numbering depends on it would be stable only by
	/// luck.
	/// </para>
	/// </remarks>
	internal static IReadOnlyList<string> TypeKinds { get; } = new ReadOnlyCollection<string>(
	[
		.. typeof(BaseType).GetCustomAttributes<JsonDerivedTypeAttribute>()
			.Select(derived => derived.TypeDiscriminator?.ToString() ?? derived.DerivedType.Name)
			.OrderBy(name => name, StringComparer.Ordinal),
	]);

	/// <summary>Every way two states of a member can be blended.</summary>
	internal static IReadOnlyList<string> Interpolations { get; } = new ReadOnlyCollection<string>(
		[.. System.Enum.GetNames<Interpolation>()]);

	/// <summary>
	/// Reads the shipped header and fills in what the target and the schema library decide.
	/// </summary>
	/// <param name="containing">The namespace the vocabulary is generated into.</param>
	/// <param name="banner">The line the file opens with.</param>
	/// <returns>The header, with line endings normalised.</returns>
	internal static string Prelude(string containing, string banner)
	{
		Assembly assembly = typeof(CppReflection).Assembly;
		string resource = assembly.GetManifestResourceNames()
			.Single(name => name.EndsWith($".{Header}.hpp", StringComparison.Ordinal));

		using Stream stream = assembly.GetManifestResourceStream(resource)!;
		using StreamReader reader = new(stream);

		return reader.ReadToEnd()
			.Replace(NamespaceToken, containing, StringComparison.Ordinal)
			.Replace(BannerToken, banner, StringComparison.Ordinal)
			.Replace(TypeKindsToken, Enumerators(TypeKinds), StringComparison.Ordinal)
			.Replace(TypeKindNamesToken, Names(TypeKinds), StringComparison.Ordinal)
			.Replace(InterpolationsToken, Enumerators(Interpolations), StringComparison.Ordinal)
			.Replace(InterpolationNamesToken, Names(Interpolations), StringComparison.Ordinal)
			.ReplaceLineEndings("\n");
	}

	/// <summary>
	/// How a member's declared type is named in the table.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The enumerator, qualified by the enumeration.</returns>
	internal static string Kind(BaseType type) =>
		$"TypeKind::{Ensure.NotNull(type).TypeName}";

	/// <summary>
	/// How the bytes a member occupies are named in the table.
	/// </summary>
	/// <remarks>
	/// A semantic type is followed down its chain of refinement to the type it is stored as, which
	/// is the whole reason the table carries this beside the declared kind: <c>Kilograms</c> tells
	/// a person what the member is and tells a codec nothing about how to read it.
	/// <para>
	/// A chain that reaches nothing real answers with the declared kind, the same way
	/// <c>Schema.Validation</c> treats an unresolved semantic type — already reported elsewhere,
	/// rather than walked further from here.
	/// </para>
	/// </remarks>
	/// <param name="type">The type.</param>
	/// <returns>The enumerator, qualified by the enumeration.</returns>
	internal static string Representation(BaseType type) =>
		Ensure.NotNull(type) is Semantic semantic && semantic.Declaration?.Representation() is BaseType stored
			? Kind(stored)
			: Kind(type);

	/// <summary>
	/// How a member's interpolation is named in the table.
	/// </summary>
	/// <param name="interpolation">The interpolation.</param>
	/// <returns>The enumerator, qualified by the enumeration.</returns>
	internal static string Blend(Interpolation interpolation) =>
		$"Interpolation::{interpolation}";

	private static string Enumerators(IReadOnlyList<string> names) =>
		string.Join("\n", names.Select(name => $"\t\t{name},"));

	private static string Names(IReadOnlyList<string> names) =>
		string.Join("\n", names.Select(name => $"\t\t\"{name}\","));
}
