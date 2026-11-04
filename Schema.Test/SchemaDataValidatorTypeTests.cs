// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the types data validation used to wave through without a look: semantic types,
/// quantities, optionals, vectors of something other than floats, and the types that have no
/// stored form at all.
/// </summary>
[TestClass]
public class SchemaDataValidatorTypeTests
{
	private static SchemaClass ClassWith(Action<Schema, SchemaClass> addMembers)
	{
		Schema schema = new();
		schema.AddSemanticType("EntityId".As<SemanticTypeName>())!.SetUnderlyingType(new Long());
		SchemaClass schemaClass = schema.AddClass("Thing".As<ClassName>())!;
		addMembers(schema, schemaClass);
		return schemaClass;
	}

	private static SchemaClass Member(BaseType type) =>
		ClassWith((_, schemaClass) => schemaClass.AddMember("Value".As<MemberName>())!.SetType(type));

	private static Collection<SchemaValidationIssue> Validate(BaseType type, string value) =>
		SchemaDataValidator.ValidateData(Member(type), $"{{ \"Value\": {value} }}", "data");

	private static readonly string[] ExpectedErrorPaths = ["data.Cell[0]", "data.Cell[1]", "data.Id", "data.Mass", "data.Maybe"];

	private static Semantic EntityId => new() { SemanticTypeName = "EntityId".As<SemanticTypeName>() };

	private static Quantity Quantity(string name, BaseType? storage = null) => new()
	{
		QuantityName = name.As<QuantityName>(),
		Storage = storage ?? new Float(),
	};

	[TestMethod]
	public void TheIssuesPayloadReportsOneErrorPerWrongValue()
	{
		SchemaClass schemaClass = ClassWith((_, c) =>
		{
			c.AddMember("Id".As<MemberName>())!.SetType(EntityId);
			c.AddMember("Mass".As<MemberName>())!.SetType(Quantity("Mass"));
			c.AddMember("Cell".As<MemberName>())!.SetType(new Vector2 { ElementType = new Int() });
			c.AddMember("Maybe".As<MemberName>())!.SetType(new Optional { ElementType = new Int() });
		});

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateData(
			schemaClass,
			"""{ "Id": "not a number", "Mass": {"x":true}, "Cell": [1.5, 2.7], "Maybe": "nope" }""",
			"data");

		string[] paths = [.. issues.Where(i => i.Severity == SchemaValidationSeverity.Error).Select(i => i.Path).Order(StringComparer.Ordinal)];
		Assert.AreSequenceEqual(ExpectedErrorPaths, paths, string.Join("; ", issues));
	}

	[TestMethod]
	public void ASemanticValueIsCheckedAsItsRepresentation()
	{
		Assert.HasCount(0, Validate(EntityId, "42"));
		Assert.HasCount(1, Validate(EntityId, "\"42\""));
		Assert.HasCount(1, Validate(EntityId, "4.5"));
	}

	[TestMethod]
	public void AnUnresolvedSemanticTypeIsLeftToSchemaValidation()
	{
		Semantic missing = new() { SemanticTypeName = "Missing".As<SemanticTypeName>() };

		Assert.HasCount(0, Validate(missing, "\"anything\""));
	}

	[TestMethod]
	public void AMagnitudeIsOneNumber()
	{
		Assert.HasCount(0, Validate(Quantity("Mass"), "75.5"));
		Assert.HasCount(1, Validate(Quantity("Mass"), "[75.5]"));
	}

	[TestMethod]
	public void AQuantityStoredInAWholeNumberRefusesAFraction()
	{
		Assert.HasCount(0, Validate(Quantity("Mass", new Int()), "75"));
		Assert.HasCount(1, Validate(Quantity("Mass", new Int()), "75.5"));
	}

	[TestMethod]
	public void AVectorQuantityIsAnArrayOfItsComponents()
	{
		Assert.HasCount(0, Validate(Quantity("Velocity3D"), "[1, 2, 3]"));

		Collection<SchemaValidationIssue> issues = Validate(Quantity("Velocity3D"), "[1, true]");
		Assert.IsTrue(issues.Any(i => i.Path == "data.Value[1]"), string.Join("; ", issues));
		Assert.IsTrue(issues.Any(i => i.Path == "data.Value" && i.Message.Contains('3', StringComparison.Ordinal)), string.Join("; ", issues));
	}

	[TestMethod]
	public void AnUnknownQuantityIsLeftToSchemaValidation()
	{
		Assert.HasCount(0, Validate(Quantity("Squiggles"), "\"anything\""));
	}

	[TestMethod]
	public void AnOptionalAcceptsNullAndChecksAnythingElse()
	{
		Optional optional = new() { ElementType = new Int() };

		Assert.HasCount(0, Validate(optional, "null"));
		Assert.HasCount(0, Validate(optional, "3"));
		Assert.HasCount(1, Validate(optional, "\"nope\""));
	}

	[TestMethod]
	public void AVectorChecksItsComponentsAgainstItsElementType()
	{
		Assert.HasCount(0, Validate(new Vector3 { ElementType = new Int() }, "[1, 2, 3]"));
		Assert.HasCount(0, Validate(new Vector3(), "[1.5, 2, 3]"));
		Assert.HasCount(1, Validate(new Vector3 { ElementType = EntityId }, "[1, 2.5, 3]"));
	}

	[TestMethod]
	public void ATypeWithNoStoredFormIsReportedAsUnchecked()
	{
		foreach (BaseType type in new BaseType[] { new Handle { ElementType = new Int() }, new Result { ElementType = new Int() }, new Span { ElementType = new Int() }, new Interface() })
		{
			Collection<SchemaValidationIssue> issues = Validate(type, "1");
			Assert.HasCount(1, issues, type.DisplayName);
			Assert.AreEqual(SchemaValidationSeverity.Warning, issues[0].Severity, type.DisplayName);
		}
	}

	// ---- range ----------------------------------------------------------------

	private static Collection<SchemaValidationIssue> ValidateRanged(BaseType type, MemberRange range, string value) =>
		SchemaDataValidator.ValidateData(
			ClassWith((_, c) =>
			{
				SchemaMember member = c.AddMember("Value".As<MemberName>())!;
				member.SetType(type);
				member.Range = range;
			}),
			$"{{ \"Value\": {value} }}",
			"data");

	private static readonly MemberRange Percent = new() { Minimum = 0, Maximum = 100 };

	private static readonly string[] OutOfRangeComponentPaths = ["data.Value[1]", "data.Value[2]"];

	private static readonly string[] OutOfRangeSemanticPaths = ["data.Current", "data.History[1]"];

	[TestMethod]
	[DataRow("50")]
	[DataRow("0")]
	[DataRow("100")]
	public void AValueInsideOrOnTheBoundOfAMembersRangeIsAccepted(string value) =>
		Assert.HasCount(0, ValidateRanged(new Int(), Percent, value));

	[TestMethod]
	[DataRow("500")]
	[DataRow("-7")]
	[DataRow("101")]
	public void AValueOutsideAMembersRangeIsAnErrorThatNamesTheRange(string value)
	{
		Collection<SchemaValidationIssue> issues = ValidateRanged(new Int(), Percent, value);

		Assert.HasCount(1, issues, string.Join("; ", issues));
		Assert.AreEqual(SchemaValidationSeverity.Error, issues[0].Severity);
		Assert.AreEqual("data.Value", issues[0].Path);
		Assert.Contains($"{Percent}", issues[0].Message);
		Assert.Contains("member 'Value'", issues[0].Message);
	}

	[TestMethod]
	public void AFloatJustPastTheBoundIsOutsideTheRange() =>
		Assert.HasCount(1, ValidateRanged(new Double(), new MemberRange { Minimum = 0, Maximum = 1 }, "1.0000001"));

	[TestMethod]
	[DataRow("7")]
	[DataRow("-100")]
	public void AWrappingRangeAcceptsAnyNumber(string value) =>
		Assert.HasCount(0, ValidateRanged(new Double(), new MemberRange { Minimum = 0, Maximum = 6.2831853, Wrap = true }, value));

	[TestMethod]
	public void AVectorIsBoundedComponentByComponent()
	{
		Collection<SchemaValidationIssue> issues = ValidateRanged(new Vector3(), Percent, "[1, 200, -1]");

		string[] paths = [.. issues.Select(i => i.Path)];
		Assert.AreSequenceEqual(OutOfRangeComponentPaths, paths, string.Join("; ", issues));
	}

	[TestMethod]
	public void AValueOfTheWrongTypeIsReportedOnceAndNotAlsoAsOutOfRange() =>
		Assert.HasCount(1, ValidateRanged(new Int(), Percent, "\"fast\""));

	[TestMethod]
	public void ASemanticTypesRangeBoundsEveryValueOfIt()
	{
		SchemaClass schemaClass = ClassWith((schema, c) =>
		{
			SchemaSemanticType health = schema.AddSemanticType("Health".As<SemanticTypeName>())!;
			health.SetUnderlyingType(new Int());
			health.Range = Percent;
			c.AddMember("Current".As<MemberName>())!.SetType(new Semantic { SemanticTypeName = "Health".As<SemanticTypeName>() });
			c.AddMember("History".As<MemberName>())!.SetType(new ktsu.Schema.Models.Types.Array { ElementType = new Semantic { SemanticTypeName = "Health".As<SemanticTypeName>() } });
		});

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateData(
			schemaClass,
			"""{ "Current": 500, "History": [ 50, -7 ] }""",
			"data");

		string[] paths = [.. issues.Select(i => i.Path).Order(StringComparer.Ordinal)];
		Assert.AreSequenceEqual(OutOfRangeSemanticPaths, paths, string.Join("; ", issues));
		Assert.IsTrue(issues.All(i => i.Message.Contains("semantic type 'Health'", StringComparison.Ordinal)), string.Join("; ", issues));
	}

	[TestMethod]
	public void AMembersRangeAndItsSemanticTypesRangeBothApply()
	{
		SchemaClass schemaClass = ClassWith((schema, c) =>
		{
			SchemaSemanticType health = schema.AddSemanticType("Health".As<SemanticTypeName>())!;
			health.SetUnderlyingType(new Int());
			health.Range = Percent;
			SchemaMember member = c.AddMember("Value".As<MemberName>())!;
			member.SetType(new Semantic { SemanticTypeName = "Health".As<SemanticTypeName>() });
			member.Range = new MemberRange { Minimum = 10, Maximum = 1000 };
		});

		Assert.HasCount(0, SchemaDataValidator.ValidateData(schemaClass, """{ "Value": 50 }""", "data"));
		Assert.HasCount(1, SchemaDataValidator.ValidateData(schemaClass, """{ "Value": 5 }""", "data"));
		Assert.HasCount(1, SchemaDataValidator.ValidateData(schemaClass, """{ "Value": 500 }""", "data"));
		Assert.HasCount(2, SchemaDataValidator.ValidateData(schemaClass, """{ "Value": 5000 }""", "data"));
	}

	// ---- floating-point overflow -----------------------------------------------

	[TestMethod]
	[DataRow("1e39")]
	[DataRow("-1e39")]
	public void AFloatPastFloatsRangeIsAnError(string value)
	{
		Collection<SchemaValidationIssue> issues = Validate(new Float(), value);

		Assert.HasCount(1, issues, string.Join("; ", issues));
		Assert.AreEqual(SchemaValidationSeverity.Error, issues[0].Severity);
		Assert.Contains("range of Float", issues[0].Message);
	}

	[TestMethod]
	[DataRow("3.4028234663852886E+38")]
	[DataRow("1e38")]
	[DataRow("0")]
	public void AFloatAtOrInsideFloatsRangeIsAccepted(string value) =>
		Assert.HasCount(0, Validate(new Float(), value));

	[TestMethod]
	public void ADoublePastDoublesRangeIsAnError()
	{
		Assert.HasCount(1, Validate(new Double(), "1e309"));
		Assert.HasCount(0, Validate(new Double(), "1e39"));
	}

	[TestMethod]
	public void AFloatVectorComponentPastFloatsRangeIsAnError()
	{
		Collection<SchemaValidationIssue> issues = Validate(new Vector3(), "[1, 1e39, 2]");

		Assert.HasCount(1, issues, string.Join("; ", issues));
		Assert.AreEqual("data.Value[1]", issues[0].Path);
	}

	[TestMethod]
	public void AColourComponentAndAQuantityPastFloatsRangeAreErrors()
	{
		Assert.HasCount(1, Validate(new ColorRGB(), "[0, 1e39, 0]"));
		Assert.HasCount(1, Validate(Quantity("Mass"), "1e39"));
	}

	[TestMethod]
	public void ASemanticTypeOverAFloatIsHeldToFloatsRange()
	{
		SchemaClass schemaClass = ClassWith((schema, c) =>
		{
			schema.AddSemanticType("Speed".As<SemanticTypeName>())!.SetUnderlyingType(new Float());
			c.AddMember("Value".As<MemberName>())!.SetType(new Semantic { SemanticTypeName = "Speed".As<SemanticTypeName>() });
		});

		Assert.HasCount(1, SchemaDataValidator.ValidateData(schemaClass, """{ "Value": 1e39 }""", "data"));
	}
}
