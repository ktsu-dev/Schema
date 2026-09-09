// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using System.Linq;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Quantities;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers the semantic metadata a member can carry: its unit, range, default, interpolation
/// and network encoding.
/// </summary>
[TestClass]
public class MemberMetadataTests
{
	private static SchemaMember MemberWith(SchemaTypes.BaseType type, out Schema schema)
	{
		schema = new Schema();
		SchemaClass owner = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember member = owner.AddMember("Value".As<MemberName>())!;
		member.SetType(type);
		return member;
	}

	private static Collection<SchemaValidationIssue> Errors(Schema schema) =>
		new([.. schema.Validate().Where(issue => issue.Severity == SchemaValidationSeverity.Error)]);

	private static void AssertMentions(Collection<SchemaValidationIssue> issues, string fragment)
	{
		Assert.IsTrue(
			issues.Any(issue => issue.Message.Contains(fragment, System.StringComparison.OrdinalIgnoreCase)),
			$"expected an issue mentioning '{fragment}', got: {string.Join(" | ", issues.Select(i => i.Message))}");
	}

	// ------------------------------------------------------------------- units

	[TestMethod]
	public void ASymbolResolvesToItsUnit()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out _);
		member.Unit = "m/s".As<UnitSymbol>();

		Assert.IsTrue(member.TryResolveUnit(out IUnit? unit, out string error), error);
		Assert.AreEqual("MeterPerSecond", unit!.Name);
		Assert.AreEqual(1, unit.Dimension.DimensionalFormula["length"]);
		Assert.AreEqual(-1, unit.Dimension.DimensionalFormula["time"]);
	}

	[TestMethod]
	public void AUnitNameResolvesToo()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out _);
		member.Unit = "MeterPerSecond".As<UnitSymbol>();

		Assert.IsTrue(member.TryResolveUnit(out IUnit? unit, out _));
		Assert.AreEqual("MeterPerSecond", unit!.Name);
	}

	[TestMethod]
	public void AnAmbiguousSymbolIsRefusedAndNamesTheCandidates()
	{
		// 'rad' is the symbol of both Radian and Rad, which are different dimensions
		// entirely -- an angle and an absorbed radiation dose. Picking one silently would
		// be the worst available outcome.
		Assert.IsFalse(UnitRegistry.TryResolve("rad", out IUnit? unit, out string error));
		Assert.IsNull(unit);
		StringAssert.Contains(error, "Radian");
		StringAssert.Contains(error, "Rad");
		StringAssert.Contains(error, "name");
	}

	[TestMethod]
	public void TheNameEscapesTheAmbiguousSymbol()
	{
		Assert.IsTrue(UnitRegistry.TryResolve("Radian", out IUnit? radian, out _));
		Assert.IsTrue(UnitRegistry.TryResolve("Rad", out IUnit? rad, out _));
		Assert.AreNotEqual(radian!.Dimension.Name, rad!.Dimension.Name);
	}

	[TestMethod]
	public void AnUnknownUnitIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Unit = "furlong".As<UnitSymbol>();

		AssertMentions(Errors(schema), "not a known unit");
	}

	[TestMethod]
	public void AUnitOnSomethingThatMeasuresNothingIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.String(), out Schema schema);
		member.Unit = "m".As<UnitSymbol>();

		AssertMentions(Errors(schema), "meaningless");
	}

	[TestMethod]
	public void AVectorMayCarryAUnit()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Vector3(), out Schema schema);
		member.Unit = "m/s".As<UnitSymbol>();

		Assert.AreEqual(0, Errors(schema).Count);
	}

	[TestMethod]
	public void TheTextToWriteForAUnitIsItsSymbolUnlessThatIsShared()
	{
		// What a picker writes when the user has chosen a unit rather than typed one. The symbol
		// reads better and is right for all but the shared two, and the point of choosing per unit
		// is that those two are the only ones that have to be spelled out.
		Assert.IsTrue(UnitRegistry.TryResolve("MeterPerSecond", out IUnit? metersPerSecond, out _));
		Assert.AreEqual("m/s", UnitRegistry.PreferredText(metersPerSecond!));

		Assert.IsTrue(UnitRegistry.TryResolve("Radian", out IUnit? radian, out _));
		Assert.AreEqual("Radian", UnitRegistry.PreferredText(radian!));

		Assert.IsTrue(UnitRegistry.TryResolve("Gram", out IUnit? gram, out _));
		Assert.AreEqual("Gram", UnitRegistry.PreferredText(gram!));
	}

	/// <summary>
	/// The property that matters: whatever text is written for a unit has to name that same unit
	/// when it is read back. Asserted over the whole registry rather than a sample, since the
	/// spellings that do not round-trip are exactly the ones nobody thinks to sample.
	/// </summary>
	[TestMethod]
	public void EveryUnitsPreferredTextResolvesBackToIt()
	{
		foreach (IUnit unit in UnitRegistry.All)
		{
			string text = UnitRegistry.PreferredText(unit);
			Assert.IsTrue(UnitRegistry.TryResolve(text, out IUnit? resolved, out string error), $"'{text}' for {unit.Name}: {error}");
			Assert.AreEqual(unit.Name, resolved!.Name, $"'{text}' was written for {unit.Name}");
		}
	}

	[TestMethod]
	public void TheRegistryIsNotEmpty()
	{
		// Guards the reflection that builds it: a registry that silently found nothing
		// would make every unit unresolvable and every unit test above fail confusingly.
		Assert.IsTrue(UnitRegistry.All.Count > 100, $"registry holds only {UnitRegistry.All.Count} units");
	}

	// ------------------------------------------------------------------ ranges

	[TestMethod]
	public void ABackwardsRangeIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Range = new MemberRange { Minimum = 10.0, Maximum = 1.0 };

		AssertMentions(Errors(schema), "no value satisfies");
	}

	[TestMethod]
	public void ARangeOnAStringIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.String(), out Schema schema);
		member.Range = new MemberRange { Minimum = 0.0, Maximum = 1.0 };

		AssertMentions(Errors(schema), "meaningless");
	}

	[TestMethod]
	public void AZeroWidthWrappingRangeIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Range = new MemberRange { Minimum = 1.0, Maximum = 1.0, Wrap = true };

		AssertMentions(Errors(schema), "wrap into");
	}

	// ---------------------------------------------------------------- defaults

	[TestMethod]
	public void ADefaultOutsideItsRangeIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Range = new MemberRange { Minimum = 0.0, Maximum = 1.0 };
		member.DefaultValue = new NumberDefault { Value = 2.0 };

		AssertMentions(Errors(schema), "outside the member's own range");
	}

	[TestMethod]
	public void ADefaultOutsideAWrappingRangeIsFine()
	{
		// A wrapping range is a period, not a bound: 7 radians is un-normalised rather
		// than wrong. This is the case a range-only reading gets backwards.
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Range = new MemberRange { Minimum = 0.0, Maximum = 6.2831853, Wrap = true };
		member.DefaultValue = new NumberDefault { Value = 7.0 };

		Assert.AreEqual(0, Errors(schema).Count);
	}

	[TestMethod]
	public void AFractionalDefaultOnAnIntegralMemberIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Int(), out Schema schema);
		member.DefaultValue = new NumberDefault { Value = 1.5 };

		AssertMentions(Errors(schema), "not a whole number");
	}

	[TestMethod]
	public void ADefaultOfTheWrongKindIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.DefaultValue = new BooleanDefault { Value = true };

		AssertMentions(Errors(schema), "does not fit");
	}

	[TestMethod]
	public void AnEnumDefaultMustNameAValueOfThatEnum()
	{
		Schema schema = new();
		SchemaEnum kind = schema.AddEnum("Kind".As<EnumName>())!;
		kind.TryAddValue("Sphere".As<EnumValueName>());
		kind.TryAddValue("Box".As<EnumValueName>());

		SchemaClass owner = schema.AddClass("Collider".As<ClassName>())!;
		SchemaMember member = owner.AddMember("Kind".As<MemberName>())!;
		member.SetType(new SchemaTypes.Enum { EnumName = "Kind".As<EnumName>() });

		member.DefaultValue = new TextDefault { Value = "Sphere" };
		Assert.AreEqual(0, Errors(schema).Count);

		member.DefaultValue = new TextDefault { Value = "Capsule" };
		AssertMentions(Errors(schema), "not a value of enum");
	}

	// ----------------------------------------------------------- interpolation

	[TestMethod]
	public void AnEnumCannotBeInterpolated()
	{
		Schema schema = new();
		SchemaEnum kind = schema.AddEnum("Kind".As<EnumName>())!;
		kind.TryAddValue("Sphere".As<EnumValueName>());

		SchemaClass owner = schema.AddClass("Collider".As<ClassName>())!;
		SchemaMember member = owner.AddMember("Kind".As<MemberName>())!;
		member.SetType(new SchemaTypes.Enum { EnumName = "Kind".As<EnumName>() });
		member.Interpolation = Interpolation.Linear;

		AssertMentions(Errors(schema), "no meaningful value between two states");
	}

	[TestMethod]
	public void AVectorMayBeInterpolated()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Vector3(), out Schema schema);
		member.Interpolation = Interpolation.Linear;

		Assert.AreEqual(0, Errors(schema).Count);
	}

	// --------------------------------------------------------------- networking

	[TestMethod]
	public void ANegativeQuantisationStepIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Network = new MemberNetwork { Quantise = -0.5 };

		AssertMentions(Errors(schema), "must not be negative");
	}

	[TestMethod]
	public void QuantisingAStringIsAnError()
	{
		SchemaMember member = MemberWith(new SchemaTypes.String(), out Schema schema);
		member.Network = new MemberNetwork { Quantise = 0.5 };

		AssertMentions(Errors(schema), "no numeric value to quantise");
	}

	[TestMethod]
	public void AStepWiderThanTheRangeIsAWarning()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Float(), out Schema schema);
		member.Range = new MemberRange { Minimum = 0.0, Maximum = 1.0 };
		member.Network = new MemberNetwork { Quantise = 10.0 };

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.AreEqual(0, Errors(schema).Count, "a coarse step is suspicious, not invalid");
		Assert.IsTrue(issues.Any(issue =>
			issue.Severity == SchemaValidationSeverity.Warning &&
			issue.Message.Contains("wider than", System.StringComparison.Ordinal)));
	}

	// ------------------------------------------------------------- round trip

	[TestMethod]
	public void MetadataSurvivesASaveAndLoad()
	{
		// The metadata is polymorphic, and polymorphic JSON is exactly where a private
		// setter or a missing discriminator silently drops data on load rather than
		// failing. So the round trip is asserted rather than assumed.
		Schema schema = new();
		SchemaClass owner = schema.AddClass("RigidBody".As<ClassName>())!;

		SchemaMember velocity = owner.AddMember("Velocity".As<MemberName>())!;
		velocity.SetType(new SchemaTypes.Vector3());
		velocity.Unit = "m/s".As<UnitSymbol>();
		velocity.Interpolation = Interpolation.Linear;
		velocity.Network = new MemberNetwork { Quantise = 0.01, Delta = true };

		SchemaMember heading = owner.AddMember("Heading".As<MemberName>())!;
		heading.SetType(new SchemaTypes.Float());
		heading.Unit = "Radian".As<UnitSymbol>();
		heading.Range = new MemberRange { Minimum = 0.0, Maximum = 6.2831853, Wrap = true };
		heading.DefaultValue = new NumberDefault { Value = 0.0 };
		heading.Editor = "dial".As<EditorHint>();

		string json = SchemaSerializer.Serialize(schema);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? loaded), "the schema did not deserialize");

		SchemaClass reloaded = loaded!.Classes.Single();
		SchemaMember loadedVelocity = reloaded.Members.First(m => m.Name == "Velocity");
		SchemaMember loadedHeading = reloaded.Members.First(m => m.Name == "Heading");

		Assert.AreEqual("m/s", loadedVelocity.Unit?.ToString());
		Assert.AreEqual(Interpolation.Linear, loadedVelocity.Interpolation);
		Assert.AreEqual(0.01, loadedVelocity.Network?.Quantise);
		Assert.IsTrue(loadedVelocity.Network?.Delta);

		Assert.AreEqual("Radian", loadedHeading.Unit?.ToString());
		Assert.AreEqual(6.2831853, loadedHeading.Range?.Maximum);
		Assert.IsTrue(loadedHeading.Range?.Wrap);
		Assert.AreEqual("dial", loadedHeading.Editor?.ToString());

		// The polymorphic case: a NumberDefault must come back as a NumberDefault, not as
		// the abstract base or as null.
		Assert.IsInstanceOfType<NumberDefault>(loadedHeading.DefaultValue);
		Assert.AreEqual(0.0, ((NumberDefault)loadedHeading.DefaultValue!).Value);
	}

	[TestMethod]
	public void TheFileHoldsNoDerivedStateAndNoEnumOrdinals()
	{
		// A round trip cannot see either of these: a derived property written to the file
		// reads back consistently, and an ordinal deserializes to the same value it was
		// written from. Both are still wrong. A derived property is a second, independently
		// editable copy of a fact the file already states, and an ordinal means inserting a
		// mode into the enum silently changes what every existing schema says.
		Schema schema = new();
		SchemaClass owner = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember member = owner.AddMember("Heading".As<MemberName>())!;
		member.SetType(new SchemaTypes.Float());
		member.Range = new MemberRange { Minimum = 0.0, Maximum = 1.0 };
		member.Network = new MemberNetwork { Quantise = 0.25 };
		member.Interpolation = Interpolation.Spherical;

		string json = SchemaSerializer.Serialize(schema);

		StringAssert.Contains(json, "\"interpolation\": \"Spherical\"");
		Assert.IsFalse(json.Contains("isWellFormed", System.StringComparison.OrdinalIgnoreCase), json);
		Assert.IsFalse(json.Contains("isQuantised", System.StringComparison.OrdinalIgnoreCase), json);
	}

	[TestMethod]
	public void AMemberWithNoMetadataStaysThatWay()
	{
		SchemaMember member = MemberWith(new SchemaTypes.Int(), out Schema schema);

		Assert.IsNull(member.Unit);
		Assert.IsNull(member.Range);
		Assert.IsNull(member.DefaultValue);
		Assert.IsNull(member.Network);
		Assert.IsNull(member.Editor);
		Assert.AreEqual(Interpolation.None, member.Interpolation);
		Assert.AreEqual(0, Errors(schema).Count);
	}
}
