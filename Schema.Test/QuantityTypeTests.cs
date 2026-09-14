// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Quantities;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A member holding one of <c>ktsu.Semantics.Quantities</c>' physical quantities.
/// </summary>
/// <remarks>
/// <para>
/// The difference from <see cref="Semantic"/> is whose vocabulary it is. A semantic type is how a
/// schema says that its own two numbers are different things, and nothing outside the schema has
/// heard of either; a quantity names something both generators already emit, so a member saying
/// <c>Quantity(Mass)</c> reaches a type the target already has rather than asking for a copy of
/// one.
/// </para>
/// <para>
/// What that replaces is a schema declaring a semantic type called <c>Kilograms</c> and writing
/// <c>kg</c> beside every member of it - the unit said twice, once as a name nothing reads and
/// once as text something does. A quantity says the mass and leaves the kilograms to the member,
/// which is the one place the presentation is a fact.
/// </para>
/// </remarks>
[TestClass]
public sealed class QuantityTypeTests
{
	/// <summary>
	/// The whole vocabulary is nameable, and it is the vocabulary the C++ projection emits.
	/// </summary>
	/// <remarks>
	/// 212 is not a number this repository chose: it is what <c>ktsu.Semantics.Cpp</c> writes -
	/// 148 magnitudes, 27 signed scalars and 37 vectors - and the two counts agreeing is what says
	/// a schema can name every quantity a C++ target has. It was 206 while six named vector
	/// overloads went unregistered, which is the case <see cref="ANamedVectorOverloadIsAQuantity"/>
	/// pins directly.
	/// </remarks>
	[TestMethod]
	public void TheVocabularyIsTheOneBothGeneratorsHave() =>
		Assert.HasCount(212, QuantityRegistry.All);

	/// <summary>
	/// A magnitude, a signed scalar and a vector each report their own shape.
	/// </summary>
	[TestMethod]
	public void AQuantityKnowsHowManyComponentsItHas()
	{
		Assert.AreEqual(0, Resolve("Mass").Components);
		Assert.AreEqual(1, Resolve("Heading").Components);
		Assert.AreEqual(3, Resolve("Velocity3D").Components);
	}

	/// <summary>
	/// A vector form's dimension comes from the magnitude it answers with.
	/// </summary>
	/// <remarks>
	/// <c>IVectorN</c> above zero declares components and no dimension, so a <c>Velocity3D</c>
	/// carries none of its own. What it does carry is <c>Magnitude()</c>, and the magnitude of a
	/// velocity is a length over a time - which is not an inference but the relationship the
	/// vocabulary is built on, since the sum of the squares of the components has twice a
	/// component's dimension and the square root halves it again.
	/// </remarks>
	[TestMethod]
	public void AVectorFormTakesItsDimensionFromItsMagnitude()
	{
		DimensionInfo velocity = Resolve("Velocity3D").Dimension;

		Assert.AreEqual("Velocity", velocity.Name);
		Assert.AreEqual(1, velocity.DimensionalFormula["length"]);
		Assert.AreEqual(-1, velocity.DimensionalFormula["time"]);
	}

	/// <summary>
	/// A named overload of a vector form is a quantity, reached through the widening it declares.
	/// </summary>
	/// <remarks>
	/// <c>Position3D</c> is a <c>Displacement3D</c> under another name and the vocabulary gives it
	/// no <c>Magnitude()</c> of its own, so the only thing left to follow is the implicit
	/// conversion onto what it is an overload of. Six quantities are reachable only this way, and
	/// without it a schema naming any of them would be told its own vocabulary does not have it.
	/// </remarks>
	[TestMethod]
	public void ANamedVectorOverloadIsAQuantity()
	{
		foreach (string name in new[]
			{ "Position3D", "Translation3D", "WindVelocity3D", "GravitationalField3D", "WeightVector", "ThrustVector" })
		{
			Assert.AreEqual(3, Resolve(name).Components, name);
			Assert.IsNotNull(Resolve(name).Dimension, name);
		}
	}

	/// <summary>
	/// A logarithmic scale is not a quantity, and is not nameable as one.
	/// </summary>
	/// <remarks>
	/// A decibel does not add and a pH does not scale, which is why <c>ktsu.Semantics</c> emits
	/// them from <c>logarithmic.json</c> rather than as dimensions: they have no dimensional
	/// formula and no vector form. Accepting one here would put a type in the schema whose
	/// arithmetic means nothing, and whose eight exponents a reflection table would have to invent.
	/// </remarks>
	[TestMethod]
	public void ALogarithmicScaleIsNotOne()
	{
		foreach (string name in new[] { "Decibels", "SoundPressureLevel", "Cents", "Semitones", "PH" })
		{
			Assert.IsFalse(QuantityRegistry.TryResolve(name, out _), name);
		}
	}

	/// <summary>
	/// A name the vocabulary does not have is refused, and said to be.
	/// </summary>
	[TestMethod]
	public void AnUnknownQuantityIsReported()
	{
		Schema schema = WithMember(new Quantity { QuantityName = "Squiggles".As<QuantityName>() });

		Assert.ContainsSingle(
			schema.Validate().Where(issue =>
				issue.Severity == SchemaValidationSeverity.Error &&
				issue.Message.Contains("Squiggles", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A quantity is stored in a number, and nothing else.
	/// </summary>
	/// <remarks>
	/// Tighter than the rule a vector's components get, which accept a semantic type over a
	/// number. A quantity is generic under <c>where T : struct, INumber&lt;T&gt;</c> and a
	/// generated semantic type is a record struct over a float implementing no such thing, so
	/// <c>Mass&lt;Kilograms&gt;</c> is not a type anything could write. Refusing it is the schema
	/// saying so rather than the generator emitting C# that will not compile.
	/// </remarks>
	[TestMethod]
	public void AQuantityIsStoredInANumber()
	{
		Schema schema = WithMember(new Quantity
		{
			QuantityName = "Mass".As<QuantityName>(),
			Storage = new Models.Types.String(),
		});

		Assert.ContainsSingle(
			schema.Validate().Where(issue =>
				issue.Severity == SchemaValidationSeverity.Error &&
				issue.Message.Contains("stored in a", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A unit that measures something else is a contradiction, and is reported as one.
	/// </summary>
	/// <remarks>
	/// The check the semantic type this replaces could never make. A unit is text resolved through
	/// <c>UnitRegistry</c> and a type called <c>Kilograms</c> is a name nothing reads, so the two
	/// had no way to disagree; a quantity knows its own eight exponents and so does the unit, so
	/// the contradiction is arithmetic.
	/// </remarks>
	[TestMethod]
	public void AUnitHasToMeasureWhatTheQuantityMeasures()
	{
		Schema schema = WithMember(
			new Quantity { QuantityName = "Mass".As<QuantityName>() },
			member => member.Unit = "m".As<UnitSymbol>());

		Assert.ContainsSingle(
			schema.Validate().Where(issue =>
				issue.Severity == SchemaValidationSeverity.Error &&
				issue.Message.Contains("Mass", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A unit that measures what the quantity measures is accepted.
	/// </summary>
	[TestMethod]
	public void AUnitOfTheRightDimensionIsAccepted()
	{
		Schema schema = WithMember(
			new Quantity { QuantityName = "Mass".As<QuantityName>() },
			member => member.Unit = "kg".As<UnitSymbol>());

		Assert.IsEmpty(schema.Validate());
	}

	/// <summary>
	/// Two names for one set of exponents are both good units for either.
	/// </summary>
	/// <remarks>
	/// 72 of the vocabulary's dimensions share 63 exponent vectors - <c>Torque</c> and
	/// <c>Energy</c> are one vector between two names - so the check compares the exponents and
	/// not the names. A joule and a newton metre are the same eight numbers, and a schema holding
	/// a torque in joules is saying nothing the physics refuses.
	/// </remarks>
	[TestMethod]
	public void ADimensionSharedBetweenTwoNamesIsNotAContradiction()
	{
		Schema schema = WithMember(
			new Quantity { QuantityName = "TorqueMagnitude".As<QuantityName>() },
			member => member.Unit = "J".As<UnitSymbol>());

		Assert.IsEmpty(schema.Validate());
	}

	/// <summary>
	/// A quantity travels as bytes, because its storage does.
	/// </summary>
	[TestMethod]
	public void AClassThatTravelsAsBytesMayHoldOne()
	{
		Schema schema = new();
		SchemaClass promising = schema.AddClass("Body".As<ClassName>())!;
		promising.TravelsAsBytes = true;
		promising.AddMember("Mass".As<MemberName>())!
			.SetType(new Quantity { QuantityName = "Mass".As<QuantityName>() });
		promising.AddMember("Velocity".As<MemberName>())!
			.SetType(new Quantity { QuantityName = "Velocity3D".As<QuantityName>() });

		Assert.IsEmpty(schema.Validate());
	}

	/// <summary>
	/// The storage is written only when it is not the default, so a file gains nothing by saying
	/// what it already meant.
	/// </summary>
	[TestMethod]
	public void TheDefaultStorageIsOmittedFromTheFile()
	{
		string floats = SchemaSerializer.Serialize(
			WithMember(new Quantity { QuantityName = "Mass".As<QuantityName>() }));

		string doubles = SchemaSerializer.Serialize(WithMember(new Quantity
		{
			QuantityName = "Mass".As<QuantityName>(),
			Storage = new Models.Types.Double(),
		}));

		Assert.DoesNotContain("storage", floats, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("storage", doubles, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// A quantity survives being written and read back.
	/// </summary>
	[TestMethod]
	public void ItRoundTripsThroughTheFile()
	{
		Schema original = WithMember(new Quantity
		{
			QuantityName = "Velocity3D".As<QuantityName>(),
			Storage = new Models.Types.Double(),
		});

		Assert.IsTrue(SchemaSerializer.TryDeserialize(SchemaSerializer.Serialize(original), out Schema? reloaded));

		BaseType reloadedType = reloaded!.GetClass("Body".As<ClassName>())!
			.GetMember("Value".As<MemberName>())!.Type;

		Assert.IsInstanceOfType<Quantity>(reloadedType);
		Assert.AreEqual("Velocity3D", ((Quantity)reloadedType).QuantityName.ToString());
		Assert.IsInstanceOfType<Models.Types.Double>(((Quantity)reloadedType).Storage);
	}

	/// <summary>
	/// It reaches C# as the quantity, closed over its storage, with nothing emitted for it.
	/// </summary>
	/// <remarks>
	/// This is the whole of what makes it different from a semantic type on the generated side. A
	/// semantic type is a file the generator writes; a quantity is a name it references, because
	/// the target already has the type.
	/// </remarks>
	[TestMethod]
	public void ItGeneratesAsTheQuantityItNames()
	{
		Schema schema = WithMember(new Quantity
		{
			QuantityName = "Velocity3D".As<QuantityName>(),
			Storage = new Models.Types.Double(),
		});

		IReadOnlyDictionary<string, string> files =
			new CSharpCodeGenerator().Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));

		Assert.ContainsSingle(files);
		Assert.Contains("ktsu.Semantics.Quantities.Velocity3D<double>", files.Values.Single(), StringComparison.Ordinal);
	}

	/// <summary>
	/// Generated, compiled and reimported, it is the quantity it started as.
	/// </summary>
	/// <remarks>
	/// The one type here that reads back with no attribute recording what it is. Everything else
	/// generated C# carries needs one, because a sequential struct or a record struct over a float
	/// is a shape a hand-written type may have for its own reasons - but a <c>Mass&lt;float&gt;</c>
	/// is not something a target happened to write, it is the quantity. That is what it means for
	/// the vocabulary to be shared rather than copied.
	/// </remarks>
	[TestMethod]
	public void ItReimportsFromTheCompiledTypeWithNothingRecordingWhatItIs()
	{
		Schema original = WithMember(new Quantity { QuantityName = "Mass".As<QuantityName>() });
		SchemaGenerationResult result = SchemaGenerator.Generate(original, CodeGenerationTests.ConfigureGenerator(original));

		Assert.IsTrue(result.IsSuccess, result.Message);

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Schema reimported = new();
		reimported.AddClass(assembly.GetType("Generated.Body", throwOnError: true)!);

		BaseType imported = reimported.GetClass("Body".As<ClassName>())!
			.GetMember("Value".As<MemberName>())!.Type;

		Assert.IsInstanceOfType<Quantity>(imported);
		Assert.AreEqual("Mass", ((Quantity)imported).QuantityName.ToString());
		Assert.IsInstanceOfType<Models.Types.Float>(((Quantity)imported).Storage);
	}

	private static QuantityRegistry.QuantityInfo Resolve(string name)
	{
		Assert.IsTrue(QuantityRegistry.TryResolve(name, out QuantityRegistry.QuantityInfo? quantity), name);

		return quantity!;
	}

	/// <summary>
	/// A schema of one class holding one member of the given type.
	/// </summary>
	private static Schema WithMember(BaseType type, Action<SchemaMember>? configure = null)
	{
		Schema schema = new();
		SchemaMember member = schema.AddClass("Body".As<ClassName>())!.AddMember("Value".As<MemberName>())!;

		member.SetType(type);
		configure?.Invoke(member);

		return schema;
	}
}
