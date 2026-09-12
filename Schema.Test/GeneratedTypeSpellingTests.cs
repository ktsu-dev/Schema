// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;
using System.Runtime.InteropServices;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SchemaTypes = Models.Types;

/// <summary>
/// The three types the C# generator could not spell - a handle, a semantic type, and a vector of
/// anything but floats - and what spelling them is worth.
/// </summary>
/// <remarks>
/// Each of them used to be emitted as <c>object?</c>, which compiles and says nothing. Inside a
/// class that travels as bytes it says something worse than nothing: a reference field, in the one
/// kind of class whose whole promise is that it has none. Validation accepts all three there,
/// because they travel as bytes in a language that can say them - so the refusal belonged in the
/// generator, and the fix is for the generator to say them.
/// </remarks>
[TestClass]
public class GeneratedTypeSpellingTests
{
	private static readonly string[] ExpectedRefinement = ["ForceMagnitude"];
	private static readonly string[] ExpectedSemanticFiles = ["ForceMagnitude.g.cs", "Weight.g.cs"];

	/// <summary>
	/// The proof the hole is closed, and the one assertion the others exist to make meaningful: a
	/// class that promises to travel as bytes, holding all three, can be pinned.
	/// </summary>
	/// <remarks>
	/// Pinning is refused for a type holding a reference anywhere in it, so this fails outright on
	/// the <c>object?</c> these three used to be emitted as. It is the C# counterpart of the
	/// <c>static_assert(std::is_trivially_copyable_v&lt;T&gt;)</c> the C++ generator emits beside
	/// the same class, and the two together are what make the promise checkable in both languages.
	/// </remarks>
	[TestMethod]
	public void TestAPromisingClassHoldingAllThreeIsBlittable()
	{
		Type body = Compile().GetType("Generated.Body", throwOnError: true)!;
		object instance = Activator.CreateInstance(body)!;

		GCHandle pinned = GCHandle.Alloc(instance, GCHandleType.Pinned);
		try
		{
			Assert.AreNotEqual(IntPtr.Zero, pinned.AddrOfPinnedObject());
		}
		finally
		{
			pinned.Free();
		}
	}

	/// <summary>
	/// A handle is generic over what it names, and holds an index and a generation whatever that
	/// is.
	/// </summary>
	/// <remarks>
	/// The type argument is carried but never stored, which is why the schema lets a class that
	/// travels as bytes hold a handle to one that does not: <c>Texture</c> is a reference type and
	/// a <c>Handle&lt;Texture&gt;</c> is still two integers. Naming it in the type is what keeps a
	/// handle to a texture from being passed where one to a mesh belongs.
	/// </remarks>
	[TestMethod]
	public void TestAHandleIsGeneratedOverWhatItNames()
	{
		Assembly assembly = Compile();
		Type body = assembly.GetType("Generated.Body", throwOnError: true)!;
		Type skin = body.GetProperty("Skin")!.PropertyType;

		Assert.AreEqual(typeof(Runtime.Handle<>), skin.GetGenericTypeDefinition());
		Assert.AreEqual(assembly.GetType("Generated.Texture"), skin.GetGenericArguments()[0]);
		Assert.IsFalse(assembly.GetType("Generated.Texture", throwOnError: true)!.IsValueType,
			"The point of the phantom parameter is that what a handle names need not travel as bytes itself.");
	}

	/// <summary>
	/// A vector of floats is still the <see cref="System.Numerics"/> one; a vector of anything else
	/// is generic over its component.
	/// </summary>
	/// <remarks>
	/// Two spellings for one family of schema types, because <see cref="System.Numerics.Vector3"/>
	/// holds floats and nothing else. Keeping it for the float case is what stops this from
	/// changing the type of every vector member that already existed.
	/// </remarks>
	[TestMethod]
	public void TestAVectorIsGeneratedOverItsComponent()
	{
		Type body = Compile().GetType("Generated.Body", throwOnError: true)!;

		Type precise = body.GetProperty("Precise")!.PropertyType;
		Assert.AreEqual(typeof(Runtime.Vector3<>), precise.GetGenericTypeDefinition());
		Assert.AreEqual(typeof(double), precise.GetGenericArguments()[0]);

		Assert.AreEqual(typeof(System.Numerics.Vector3), body.GetProperty("Ordinary")!.PropertyType);
	}

	/// <summary>
	/// A semantic type is a sequentially laid out struct holding the one value it is represented
	/// as, so it is the same bytes as the thing it shims.
	/// </summary>
	[TestMethod]
	public void TestASemanticTypeIsAStructOverItsRepresentation()
	{
		Type weight = Compile().GetType("Generated.Weight", throwOnError: true)!;

		Assert.IsTrue(weight.IsValueType);
		Assert.AreEqual(LayoutKind.Sequential, weight.StructLayoutAttribute!.Value);
		Assert.AreEqual(typeof(float), weight.GetProperty("Value")!.PropertyType);
		Assert.AreEqual(sizeof(float), Marshal.SizeOf(weight));
	}

	/// <summary>
	/// Crossing into or out of the representation is explicit in both directions, which is the
	/// convention the schema holds rather than each declaration restating it.
	/// </summary>
	/// <remarks>
	/// The point of a semantic type is what it refuses. An implicit conversion from <c>float</c>
	/// would make a bare number a <c>Weight</c> by accident, which is the thing the type exists to
	/// stop - so the absence of one is as much the assertion here as the presence of the explicit
	/// pair.
	/// </remarks>
	[TestMethod]
	public void TestCrossingIntoAndOutOfASemanticTypeIsExplicit()
	{
		Type weight = Compile().GetType("Generated.Weight", throwOnError: true)!;

		Assert.IsNotNull(Conversion(weight, "op_Explicit", typeof(float), weight));
		Assert.IsNotNull(Conversion(weight, "op_Explicit", weight, typeof(float)));

		Assert.IsNull(Conversion(weight, "op_Implicit", typeof(float), weight),
			"A bare number became a Weight by accident.");
		Assert.IsNull(Conversion(weight, "op_Implicit", weight, typeof(float)),
			"A Weight stopped being one by accident.");
	}

	/// <summary>
	/// A type refining another widens to it implicitly and narrows back explicitly: a
	/// <c>Weight</c> is a <c>ForceMagnitude</c>, but not every force is a weight.
	/// </summary>
	/// <remarks>
	/// Invoked rather than merely found, because the direction is the whole claim and reflection
	/// on an operator's name would pass just as well if the two were the wrong way round.
	/// </remarks>
	[TestMethod]
	public void TestASemanticTypeWidensImplicitlyAndNarrowsExplicitly()
	{
		Assembly assembly = Compile();
		Type weight = assembly.GetType("Generated.Weight", throwOnError: true)!;
		Type force = assembly.GetType("Generated.ForceMagnitude", throwOnError: true)!;

		object aWeight = Conversion(weight, "op_Explicit", typeof(float), weight)!.Invoke(null, [9.81f])!;

		object widened = Conversion(weight, "op_Implicit", weight, force)!.Invoke(null, [aWeight])!;
		Assert.AreEqual(9.81f, force.GetProperty("Value")!.GetValue(widened));

		object narrowed = Conversion(weight, "op_Explicit", force, weight)!.Invoke(null, [widened])!;
		Assert.AreEqual(aWeight, narrowed);

		Assert.IsNull(Conversion(weight, "op_Implicit", force, weight),
			"Every ForceMagnitude became a Weight.");
	}

	/// <summary>
	/// All three come back as the types the schema named, which is what they could not do while
	/// they were emitted as <c>object?</c>.
	/// </summary>
	/// <remarks>
	/// The generator's mapping and the importer's are inverses, and <c>object?</c> has no inverse -
	/// it read back as a member with no type at all, so a schema that named a handle got one that
	/// named nothing.
	/// </remarks>
	[TestMethod]
	public void TestAllThreeRoundTripAsTheTypesTheSchemaNamed()
	{
		Schema reimported = Reimport();
		SchemaClass body = reimported.GetClass("Body".As<ClassName>())!;

		Assert.AreEqual(
			new SchemaTypes.Semantic { SemanticTypeName = "Weight".As<SemanticTypeName>() },
			body.GetMember("Pull".As<MemberName>())!.Type);

		Assert.AreEqual(
			new SchemaTypes.Handle { ElementType = new SchemaTypes.Object { ClassName = "Texture".As<ClassName>() } },
			body.GetMember("Skin".As<MemberName>())!.Type);

		Assert.AreEqual(
			new SchemaTypes.Vector3 { ElementType = new SchemaTypes.Double() },
			body.GetMember("Precise".As<MemberName>())!.Type);

		Assert.AreEqual(
			new SchemaTypes.Vector3(),
			body.GetMember("Ordinary".As<MemberName>())!.Type);
	}

	/// <summary>
	/// The chain of refinement is rebuilt on reimport, down to what the whole chain is represented
	/// as.
	/// </summary>
	/// <remarks>
	/// The struct holds the representation whether or not it refines anything, so <c>Weight</c> and
	/// <c>ForceMagnitude</c> both hold a <c>float</c> and the shape alone cannot say that one is
	/// the other narrowed. That is what the attribute carries, and this is what says it carried it.
	/// </remarks>
	[TestMethod]
	public void TestARefinementChainIsRebuiltOnReimport()
	{
		Schema reimported = Reimport();

		SchemaSemanticType weight = reimported.GetSemanticType("Weight".As<SemanticTypeName>())!;
		Assert.AreEqual(
			new SchemaTypes.Semantic { SemanticTypeName = "ForceMagnitude".As<SemanticTypeName>() },
			weight.UnderlyingType);

		Assert.AreSequenceEqual(
			ExpectedRefinement,
			weight.Refines().Select(refined => refined.Name.ToString()));

		Assert.AreEqual(new SchemaTypes.Float(), weight.Representation());
	}

	/// <summary>
	/// What a semantic type says about its values survives the trip, the same as a member's does.
	/// </summary>
	/// <remarks>
	/// A unit on the type is the reason a semantic type carries metadata at all: <c>Newtons</c> is
	/// newtons everywhere, stated once rather than on every member. Losing it on reimport would
	/// give back a schema that said it nowhere.
	/// </remarks>
	[TestMethod]
	public void TestASemanticTypesOwnMetadataSurvivesTheRoundTrip()
	{
		SchemaSemanticType force = Reimport().GetSemanticType("ForceMagnitude".As<SemanticTypeName>())!;

		Assert.AreEqual("N", force.Unit?.ToString());
		Assert.AreEqual(0.0, force.Range?.Minimum);
		Assert.AreEqual(1000.0, force.Range?.Maximum);
		Assert.AreEqual(Interpolation.Linear, force.Interpolation);
	}

	/// <summary>
	/// A semantic type is a file of its own, beside the classes and the enums.
	/// </summary>
	[TestMethod]
	public void TestASemanticTypeIsGeneratedAsItsOwnFile()
	{
		Schema schema = SemanticSchema();
		IReadOnlyDictionary<string, string> files =
			new CSharpCodeGenerator().Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));

		CollectionAssert.IsSubsetOf(ExpectedSemanticFiles, files.Keys.ToArray());
	}

	/// <summary>
	/// Finds a conversion operator by the types it converts between, since the name alone says
	/// nothing about the direction.
	/// </summary>
	private static MethodInfo? Conversion(Type declaring, string name, Type from, Type to) =>
		declaring.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(method =>
				string.Equals(method.Name, name, StringComparison.Ordinal) &&
				method.ReturnType == to &&
				method.GetParameters() is [ParameterInfo parameter] &&
				parameter.ParameterType == from);

	private static Assembly Compile()
	{
		Schema schema = SemanticSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		Assert.IsTrue(result.IsSuccess, result.Message);

		return GeneratedSourceCompiler.Compile(result.Files);
	}

	private static Schema Reimport()
	{
		Schema reimported = new();
		reimported.AddClass(Compile().GetType("Generated.Body", throwOnError: true)!);
		return reimported;
	}

	/// <summary>
	/// A class that travels as bytes holding one of each of the three, plus an ordinary vector of
	/// floats so the spelling that already worked is held to as well.
	/// </summary>
	private static Schema SemanticSchema()
	{
		Schema schema = new();

		SchemaSemanticType force = schema.AddSemanticType("ForceMagnitude".As<SemanticTypeName>())!;
		force.SetUnderlyingType(new SchemaTypes.Float());
		force.Unit = "N".As<UnitSymbol>();
		force.Range = new MemberRange { Minimum = 0.0, Maximum = 1000.0 };
		force.Interpolation = Interpolation.Linear;

		SchemaSemanticType weight = schema.AddSemanticType("Weight".As<SemanticTypeName>())!;
		weight.SetUnderlyingType(new SchemaTypes.Semantic { SemanticTypeName = "ForceMagnitude".As<SemanticTypeName>() });

		// Not promising, which is the point of the handle: what one names need not travel as bytes.
		SchemaClass texture = schema.AddClass("Texture".As<ClassName>())!;
		texture.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Long());

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Pull".As<MemberName>())!.SetType(
			new SchemaTypes.Semantic { SemanticTypeName = "Weight".As<SemanticTypeName>() });
		body.AddMember("Skin".As<MemberName>())!.SetType(
			new SchemaTypes.Handle { ElementType = new SchemaTypes.Object { ClassName = "Texture".As<ClassName>() } });
		body.AddMember("Precise".As<MemberName>())!.SetType(
			new SchemaTypes.Vector3 { ElementType = new SchemaTypes.Double() });
		body.AddMember("Ordinary".As<MemberName>())!.SetType(new SchemaTypes.Vector3());

		return schema;
	}
}
