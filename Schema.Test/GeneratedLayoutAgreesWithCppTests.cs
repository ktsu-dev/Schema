// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;
using System.Runtime.InteropServices;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SchemaTypes = Models.Types;

/// <summary>
/// A class that travels as bytes is the same bytes in C# as in C++, for the two member types where
/// the languages disagree by default.
/// </summary>
/// <remarks>
/// <see cref="SchemaClass.TravelsAsBytes"/> says an instance is copied whole, across a language
/// boundary among others. Sequential layout is what lets C# keep that promise at all, and it is not
/// sufficient on its own, because sequential layout says what order the members go in and nothing
/// about how wide each one is:
/// <list type="bullet">
/// <item><description>
/// An <c>enum</c> is four bytes in C# unless its declaration says otherwise, and one byte in the
/// C++ this repository generates, which writes <c>enum class ... : std::uint8_t</c>.
/// </description></item>
/// <item><description>
/// A <c>bool</c> is one byte in the managed layout and four when marshalled, so the runtime and
/// <see cref="Marshal"/> disagreed with each other before they disagreed with C++.
/// </description></item>
/// </list>
/// <para>
/// Neither is visible from the schema - nothing in a <c>.schema.json</c> says how wide an enum is -
/// so neither would have been caught by comparing schemas. What catches them is measuring the type
/// the generator produced, which is what this does.
/// </para>
/// <para>
/// The C++ half of the pair is asserted in <c>Schema.Cpp.Test</c>'s
/// <c>EnumUnderlyingTypeTests</c>: between them the two widths are written down twice and checked
/// twice, which is the most the two generators can do without a third place to agree with.
/// </para>
/// </remarks>
[TestClass]
public class GeneratedLayoutAgreesWithCppTests
{
	/// <summary>
	/// The C++ this schema generates is three one-byte members, so the C# is too.
	/// </summary>
	/// <remarks>
	/// Deliberately all three one byte wide and in an order no padding can rescue: a bool, an enum
	/// and a bool is 3 bytes when each member is what C++ makes it and 9 when the enum is an
	/// <c>int</c> - and 12 when the bools marshal as four bytes each. One assertion on the size
	/// therefore distinguishes all three cases, and the offsets say which member moved.
	/// </remarks>
	[TestMethod]
	public void TestAPromisingClassIsTheSameBytesAsItsCppCounterpart()
	{
		Type packed = Compile().GetType("Generated.Packed", throwOnError: true)!;

		Assert.AreEqual(3, Marshal.SizeOf(packed), "a bool, a one-byte enum and a bool is three bytes");
		Assert.AreEqual(0, Marshal.OffsetOf(packed, "<Enabled>k__BackingField").ToInt64());
		Assert.AreEqual(1, Marshal.OffsetOf(packed, "<Kind>k__BackingField").ToInt64());
		Assert.AreEqual(2, Marshal.OffsetOf(packed, "<Visible>k__BackingField").ToInt64());
	}

	/// <summary>
	/// The enum is stored in a byte, which is what makes the size above come out.
	/// </summary>
	/// <remarks>
	/// Asserted separately from the layout because it is true of every generated enum rather than
	/// only of one inside a promising class: a schema's enum is a byte wherever it appears, so that
	/// moving it into a component later is not a change to its width.
	/// </remarks>
	[TestMethod]
	public void TestAGeneratedEnumIsStoredInAByte()
	{
		Type kind = Compile().GetType("Generated.Kind", throwOnError: true)!;

		Assert.IsTrue(kind.IsEnum);
		Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(kind));
	}

	private static Assembly Compile()
	{
		Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		configuration.Language = "csharp".As<LanguageName>();
		configuration.Namespace = "Generated".As<CodeNamespace>();

		SchemaEnum kind = schema.AddEnum("Kind".As<EnumName>())!;
		kind.TryAddValue("First".As<EnumValueName>());
		kind.TryAddValue("Second".As<EnumValueName>());

		SchemaClass packed = schema.AddClass("Packed".As<ClassName>())!;
		packed.TravelsAsBytes = true;
		packed.AddMember("Enabled".As<MemberName>())!.SetType(new SchemaTypes.Bool());
		packed.AddMember("Kind".As<MemberName>())!
			.SetType(new SchemaTypes.Enum { EnumName = "Kind".As<EnumName>() });
		packed.AddMember("Visible".As<MemberName>())!.SetType(new SchemaTypes.Bool());

		return GeneratedSourceCompiler.Compile(
			new CSharpCodeGenerator().Generate(schema, configuration));
	}
}
