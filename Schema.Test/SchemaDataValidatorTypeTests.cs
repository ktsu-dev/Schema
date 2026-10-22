// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using ktsu.Schema.Models;
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
}
