// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SchemaTypes = Models.Types;

/// <summary>
/// The four types left over once a handle, a semantic type and a vector were spelled - a
/// <c>Span</c>, a <c>Result</c>, an <c>Optional</c> and an <c>Interface</c> - and the interfaces
/// that are where three of them live.
/// </summary>
/// <remarks>
/// These were not a gap in the type mapping the way the other three were. Each is a decision about
/// the generated C# API: which of two span types a view is, what a fallible call returns, how an
/// absent value is spelled, and - the one that had to come first - that an interface is emitted at
/// all, since until it was, an <c>Interface</c> member named a type nothing produced.
/// </remarks>
[TestClass]
public class GeneratedSignatureSpellingTests
{
	/// <summary>
	/// An interface is emitted under the name the schema gave it, with no <c>I</c> prefix.
	/// </summary>
	/// <remarks>
	/// The prefix is the C# convention and it is not available: the compiled name is what the
	/// importer reads back, so a prefix would have to be stripped again, and stripping cannot tell
	/// a prefix from a first letter - an interface called <c>Item</c> would come back as
	/// <c>tem</c>. The classes and the enums follow the same rule.
	/// </remarks>
	[TestMethod]
	public void TestAnInterfaceIsGeneratedUnderTheNameTheSchemaGaveIt()
	{
		Type renderer = Compile().GetType("Generated.Renderer", throwOnError: true)!;

		Assert.IsTrue(renderer.IsInterface);
		Assert.AreEqual("Renderer", renderer.Name);
	}

	/// <summary>
	/// On a view, direction describes the elements rather than the view: an <c>In</c> span is a
	/// <see cref="ReadOnlySpan{T}"/> and the others are a <see cref="Span{T}"/>.
	/// </summary>
	/// <remarks>
	/// The same reading the C++ generator gives it, where the difference is a <c>const</c> on the
	/// element. C# needs two types where C++ needs one qualifier, which is why this is the
	/// generator's decision rather than something the schema says.
	/// </remarks>
	[TestMethod]
	public void TestAViewIsReadOnlyExactlyWhenItsElementsAreIn()
	{
		Type renderer = Compile().GetType("Generated.Renderer", throwOnError: true)!;

		Assert.AreEqual(
			typeof(ReadOnlySpan<float>),
			renderer.GetMethod("Draw")!.GetParameters()[0].ParameterType);

		Assert.AreEqual(
			typeof(Span<int>),
			renderer.GetMethod("Count")!.GetParameters()[0].ParameterType);
	}

	/// <summary>
	/// Everywhere but a view, direction is the parameter modifier - and <c>In</c> is no modifier at
	/// all, because an ordinary by-value parameter is already one the caller supplies and the
	/// callee does not modify.
	/// </summary>
	[TestMethod]
	public void TestDirectionIsTheParameterModifier()
	{
		Type renderer = Compile().GetType("Generated.Renderer", throwOnError: true)!;

		ParameterInfo tally = renderer.GetMethod("Count")!.GetParameters()[1];
		Assert.IsTrue(tally.ParameterType.IsByRef, "An InOut parameter is not by reference.");
		Assert.IsFalse(tally.IsOut);

		ParameterInfo path = renderer.GetMethod("Load")!.GetParameters()[0];
		Assert.IsFalse(path.ParameterType.IsByRef, "An In parameter should be an ordinary one.");
	}

	/// <summary>
	/// A fallible call returns a result carrying the schema's error enum - the schema's, once,
	/// rather than one this signature chose.
	/// </summary>
	[TestMethod]
	public void TestAFallibleReturnCarriesTheSchemasErrorType()
	{
		Assembly assembly = Compile();
		Type renderer = assembly.GetType("Generated.Renderer", throwOnError: true)!;
		Type returned = renderer.GetMethod("Load")!.ReturnType;

		Assert.AreEqual(typeof(Runtime.Result<,>), returned.GetGenericTypeDefinition());
		Assert.AreEqual(assembly.GetType("Generated.Failure"), returned.GetGenericArguments()[1]);
	}

	/// <summary>
	/// A call that can fail and produces nothing is the other arity, because C# has no <c>void</c>
	/// type argument to close the value-carrying form over.
	/// </summary>
	[TestMethod]
	public void TestAFallibleCallProducingNothingIsTheOtherArity()
	{
		Type renderer = Compile().GetType("Generated.Renderer", throwOnError: true)!;

		Assert.AreEqual(
			typeof(Runtime.Result<>),
			renderer.GetMethod("Flush")!.ReturnType.GetGenericTypeDefinition());
	}

	/// <summary>
	/// A function returning nothing is <c>void</c>, which is a type C# has in this one position and
	/// nowhere else.
	/// </summary>
	[TestMethod]
	public void TestAFunctionReturningNothingIsVoid()
	{
		Type renderer = Compile().GetType("Generated.Renderer", throwOnError: true)!;

		Assert.AreEqual(typeof(void), renderer.GetMethod("Draw")!.ReturnType);
	}

	/// <summary>
	/// A query is recorded with an attribute, because C# cannot say that calling a method leaves
	/// the receiver alone.
	/// </summary>
	/// <remarks>
	/// The one of the five conventions the language has no syntax for - C++ writes it as a trailing
	/// <c>const</c>. Without the attribute it would be dropped by the reimport, so a schema would
	/// come back with every query turned into a command.
	/// </remarks>
	[TestMethod]
	public void TestAQueryIsRecordedWithAnAttribute()
	{
		Type renderer = Compile().GetType("Generated.Renderer", throwOnError: true)!;

		Assert.IsNotNull(renderer.GetMethod("Count")!.GetCustomAttribute<Runtime.SchemaQueryAttribute>());
		Assert.IsNull(renderer.GetMethod("Draw")!.GetCustomAttribute<Runtime.SchemaQueryAttribute>());
	}

	/// <summary>
	/// An absent value is an <c>Optional&lt;T&gt;</c> of what it wraps.
	/// </summary>
	/// <remarks>
	/// <c>T?</c> would be idiomatic and means two different things: over a value type it is a type,
	/// and over a reference type it is an annotation that is not part of the type at all. A
	/// generator writing it would round-trip an <c>Optional&lt;Int&gt;</c> and lose an
	/// <c>Optional&lt;Item&gt;</c>, which is the asymmetry the mapping cannot afford.
	/// </remarks>
	[TestMethod]
	public void TestAnAbsentValueIsAnOptionalOfWhatItWraps()
	{
		Assembly assembly = Compile();
		Type chosen = assembly.GetType("Generated.Shelf", throwOnError: true)!.GetProperty("Chosen")!.PropertyType;

		Assert.AreEqual(typeof(Runtime.Optional<>), chosen.GetGenericTypeDefinition());
		Assert.AreEqual(assembly.GetType("Generated.Texture"), chosen.GetGenericArguments()[0]);
	}

	/// <summary>
	/// Every signature comes back as the schema declared it: the types, the directions, and whether
	/// the call answers or acts.
	/// </summary>
	/// <remarks>
	/// The generator's mapping and the importer's are inverses, and this is the half of that claim
	/// interfaces had no part in before - nothing emitted an interface, so nothing read one back.
	/// </remarks>
	[TestMethod]
	public void TestEverySignatureRoundTripsAsTheSchemaDeclaredIt()
	{
		SchemaInterface renderer = Reimport().GetInterface("Renderer".As<InterfaceName>())!;

		CollectionAssert.AreEqual(ExpectedSignatures, renderer.Functions.Select(Describe).ToArray());
	}

	/// <summary>
	/// A member holding an interface comes back naming it, and the interface is declared by having
	/// been named.
	/// </summary>
	[TestMethod]
	public void TestAMemberHoldingAnInterfaceRoundTripsAsTheInterfaceItNames()
	{
		Schema reimported = Reimport();
		SchemaMember backend = reimported.GetClass("Shelf".As<ClassName>())!.GetMember("Backend".As<MemberName>())!;

		Assert.AreEqual(
			new SchemaTypes.Interface { InterfaceName = "Renderer".As<InterfaceName>() },
			backend.Type);

		Assert.IsNotNull(reimported.GetInterface("Renderer".As<InterfaceName>()));
	}

	/// <summary>
	/// An absent value comes back as one too, which <c>T?</c> could not have managed over a class.
	/// </summary>
	[TestMethod]
	public void TestAnAbsentValueRoundTripsAsAnOptional()
	{
		SchemaMember chosen = Reimport().GetClass("Shelf".As<ClassName>())!.GetMember("Chosen".As<MemberName>())!;

		Assert.AreEqual(
			new SchemaTypes.Optional { ElementType = new SchemaTypes.Object { ClassName = "Texture".As<ClassName>() } },
			chosen.Type);
	}

	/// <summary>
	/// A result carries what a successful call produced, or why it failed, and never both.
	/// </summary>
	[TestMethod]
	public void TestAResultCarriesEitherAValueOrAnError()
	{
		Runtime.Result<int, DayOfWeek> produced = Runtime.Result<int, DayOfWeek>.Ok(7);
		Assert.IsTrue(produced.IsSuccess);
		Assert.AreEqual(7, produced.Value);

		Runtime.Result<int, DayOfWeek> failed = Runtime.Result<int, DayOfWeek>.Fail(DayOfWeek.Friday);
		Assert.IsFalse(failed.IsSuccess);
		Assert.AreEqual(DayOfWeek.Friday, failed.Error);
		Assert.IsFalse(failed.TryGetValue(out _));
		Assert.ThrowsExactly<InvalidOperationException>(() => _ = failed.Value);

		Assert.IsTrue(Runtime.Result<DayOfWeek>.Ok().IsSuccess);
		Assert.AreEqual(DayOfWeek.Monday, Runtime.Result<DayOfWeek>.Fail(DayOfWeek.Monday).Error);
	}

	/// <summary>
	/// An optional is absent by default, which is what makes a default-constructed one honest.
	/// </summary>
	[TestMethod]
	public void TestAnOptionalIsAbsentUntilItIsGivenAValue()
	{
		Runtime.Optional<string> nothing = default;
		Assert.IsFalse(nothing.HasValue);
		Assert.AreEqual(Runtime.Optional<string>.None, nothing);
		Assert.ThrowsExactly<InvalidOperationException>(() => _ = nothing.Value);

		Runtime.Optional<string> something = Runtime.Optional<string>.Some("here");
		Assert.IsTrue(something.TryGetValue(out string? value));
		Assert.AreEqual("here", value);
	}

	/// <summary>
	/// Everything about a signature the schema declares, in one line: what it answers with, what it
	/// takes and which way each travels, and whether it answers rather than acts.
	/// </summary>
	private static readonly string[] ExpectedSignatures =
	[
		"Void Draw(IN Span<Float> Vertices)",
		"Result<Handle<Texture>> Load(IN String Path)",
		"Result<Void> Flush()",
		"query Int Count(OUT Span<Int> Scratch, INOUT Int Tally)",
	];

	private static string Describe(SchemaFunction function)
	{
		string parameters = string.Join(", ", function.Parameters.Select(parameter => parameter.ToString()));
		string query = function.IsQuery ? "query " : string.Empty;

		return $"{query}{function.ReturnType} {function.Name}({parameters})";
	}

	private static Assembly Compile()
	{
		Schema schema = SignatureSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		Assert.IsTrue(result.IsSuccess, result.Message);

		return GeneratedSourceCompiler.Compile(result.Files);
	}

	private static Schema Reimport()
	{
		Assembly assembly = Compile();
		Schema reimported = new();
		reimported.AddInterface(assembly.GetType("Generated.Renderer", throwOnError: true)!);
		reimported.AddClass(assembly.GetType("Generated.Shelf", throwOnError: true)!);
		return reimported;
	}

	/// <summary>
	/// An interface exercising each of the four, plus a class holding the two of them that can be
	/// a member.
	/// </summary>
	private static Schema SignatureSchema()
	{
		Schema schema = new() { ErrorType = "Failure".As<EnumName>() };

		SchemaEnum failure = schema.AddEnum("Failure".As<EnumName>())!;
		failure.TryAddValue("NotFound".As<EnumValueName>());

		SchemaClass texture = schema.AddClass("Texture".As<ClassName>())!;
		texture.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Long());

		SchemaClass shelf = schema.AddClass("Shelf".As<ClassName>())!;
		shelf.AddMember("Chosen".As<MemberName>())!.SetType(new SchemaTypes.Optional
		{
			ElementType = new SchemaTypes.Object { ClassName = "Texture".As<ClassName>() },
		});
		shelf.AddMember("Backend".As<MemberName>())!.SetType(new SchemaTypes.Interface
		{
			InterfaceName = "Renderer".As<InterfaceName>(),
		});

		AddSignatures(schema.AddInterface("Renderer".As<InterfaceName>())!);
		return schema;
	}

	private static void AddSignatures(SchemaInterface renderer)
	{
		SchemaFunction draw = renderer.AddFunction("Draw".As<FunctionName>())!;
		draw.SetReturnType(new SchemaTypes.Void());
		draw.AddParameter("Vertices".As<ParameterName>())!.SetType(
			new SchemaTypes.Span { ElementType = new SchemaTypes.Float() });

		SchemaFunction load = renderer.AddFunction("Load".As<FunctionName>())!;
		load.SetReturnType(new SchemaTypes.Result
		{
			ElementType = new SchemaTypes.Handle
			{
				ElementType = new SchemaTypes.Object { ClassName = "Texture".As<ClassName>() },
			},
		});
		load.AddParameter("Path".As<ParameterName>())!.SetType(new SchemaTypes.String());

		SchemaFunction flush = renderer.AddFunction("Flush".As<FunctionName>())!;
		flush.SetReturnType(new SchemaTypes.Result { ElementType = new SchemaTypes.Void() });

		SchemaFunction count = renderer.AddFunction("Count".As<FunctionName>())!;
		count.IsQuery = true;
		count.SetReturnType(new SchemaTypes.Int());

		SchemaParameter scratch = count.AddParameter("Scratch".As<ParameterName>())!;
		scratch.SetType(new SchemaTypes.Span { ElementType = new SchemaTypes.Int() });
		scratch.Direction = ParameterDirection.Out;

		SchemaParameter tally = count.AddParameter("Tally".As<ParameterName>())!;
		tally.SetType(new SchemaTypes.Int());
		tally.Direction = ParameterDirection.InOut;
	}
}
