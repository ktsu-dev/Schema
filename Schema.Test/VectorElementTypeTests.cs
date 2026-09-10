// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers what a vector is a vector of: a velocity is three metres per second, not three floats.
/// </summary>
[TestClass]
public sealed class VectorElementTypeTests
{
	/// <summary>
	/// A vector that says nothing about its components is what a vector has always been.
	/// </summary>
	[TestMethod]
	public void AVectorsComponentsDefaultToFloat()
	{
		Assert.IsInstanceOfType<Float>(new Vector2().ElementType);
		Assert.IsInstanceOfType<Float>(new Vector3().ElementType);
		Assert.IsInstanceOfType<Float>(new Vector4().ElementType);
		Assert.IsInstanceOfType<Float>(new ColorRGB().ElementType);
		Assert.IsInstanceOfType<Float>(new ColorRGBA().ElementType);
	}

	/// <summary>
	/// Two vectors of the same shape but different components are different types, which is the
	/// whole reason the component is part of the type rather than a comment beside it.
	/// </summary>
	[TestMethod]
	public void VectorsOfDifferentComponentsAreNotEqual()
	{
		Vector3 velocity = new() { ElementType = new Semantic { SemanticTypeName = "MetresPerSecond".As<SemanticTypeName>() } };
		Vector3 position = new() { ElementType = new Semantic { SemanticTypeName = "Metres".As<SemanticTypeName>() } };

		Assert.AreNotEqual(velocity, position);
		Assert.AreNotEqual(velocity, new Vector3());
		Assert.AreEqual(velocity, new Vector3 { ElementType = new Semantic { SemanticTypeName = "MetresPerSecond".As<SemanticTypeName>() } });
	}

	/// <summary>
	/// A vector of the same shape is still a different type from a vector of another shape, even
	/// when the components match.
	/// </summary>
	[TestMethod]
	public void ShapeStillSeparatesTwoVectorsOfTheSameComponent()
	{
		Vector2 two = new() { ElementType = new Long() };
		Vector3 three = new() { ElementType = new Long() };

		Assert.AreNotEqual<BaseType>(two, three);
	}

	/// <summary>
	/// The component appears in the type's own text, so an error message and an inspector row say
	/// what the vector is three of.
	/// </summary>
	[TestMethod]
	public void TheComponentIsPartOfHowAVectorReads()
	{
		Assert.AreEqual("Vector3", new Vector3().ToString());
		Assert.AreEqual("Vector3<MetresPerSecond>", new Vector3
		{
			ElementType = new Semantic { SemanticTypeName = "MetresPerSecond".As<SemanticTypeName>() },
		}.ToString());
	}

	/// <summary>
	/// A semantic component resolves against the owning schema, exactly as one named directly by
	/// a member does.
	/// </summary>
	[TestMethod]
	public void ASemanticComponentResolvesAgainstTheSchema()
	{
		Schema schema = BuildSchemaWithVelocity();

		Vector3 velocity = (Vector3)schema.Classes.Single().Members.Single().Type;
		Semantic component = (Semantic)velocity.ElementType;

		Assert.IsNotNull(component.Declaration);
		Assert.IsInstanceOfType<Float>(component.Declaration!.Representation());
	}

	/// <summary>
	/// A vector of a number, or of a semantic type over one, is what a vector is.
	/// </summary>
	[TestMethod]
	public void ANumericComponentValidates()
	{
		Schema schema = BuildSchemaWithVelocity();

		Assert.AreEqual(0, schema.Validate().Count);
	}

	/// <summary>
	/// A vector of things that are not numbers is a collection of things rather than one value
	/// with components, which is what an Array is for.
	/// </summary>
	[TestMethod]
	public void ANonNumericComponentIsRefused()
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember tags = body.AddMember("Tags".As<MemberName>())!;
		tags.SetType(new Vector3 { ElementType = new Models.Types.String() });

		SchemaValidationIssue issue = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);
		Assert.Contains("components are numbers", issue.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A semantic type that is not stored as a number is refused the same way the bare type is.
	/// </summary>
	[TestMethod]
	public void ASemanticComponentOverANonNumberIsRefused()
	{
		Schema schema = new();
		SchemaSemanticType tag = schema.AddSemanticType("Tag".As<SemanticTypeName>())!;
		tag.SetUnderlyingType(new Models.Types.String());

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember tags = body.AddMember("Tags".As<MemberName>())!;
		tags.SetType(new Vector3 { ElementType = new Semantic { SemanticTypeName = "Tag".As<SemanticTypeName>() } });

		SchemaValidationIssue issue = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);
		Assert.Contains("components are numbers", issue.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A component naming a semantic type the schema does not declare is one mistake, and is
	/// reported once.
	/// </summary>
	[TestMethod]
	public void AnUnresolvedComponentIsReportedOnce()
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember velocity = body.AddMember("Velocity".As<MemberName>())!;
		velocity.SetType(new Vector3 { ElementType = new Semantic { SemanticTypeName = "MetresPerSecnod".As<SemanticTypeName>() } });

		SchemaValidationIssue issue = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);
		Assert.Contains("MetresPerSecnod", issue.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A colour's components are its channels, and every consumer of one reads them as floats.
	/// </summary>
	[TestMethod]
	public void AColourOfAnythingButFloatsIsRefused()
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember tint = body.AddMember("Tint".As<MemberName>())!;
		tint.SetType(new ColorRGBA { ElementType = new Int() });

		SchemaValidationIssue issue = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);
		Assert.Contains("channels are floats", issue.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// The component survives a save and a load, which is the only thing that makes it worth
	/// declaring.
	/// </summary>
	[TestMethod]
	public void TheComponentSurvivesARoundTrip()
	{
		string json = SchemaSerializer.Serialize(BuildSchemaWithVelocity());

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Vector3 velocity = (Vector3)reloaded!.Classes.Single().Members.Single().Type;

		Assert.AreEqual("MetresPerSecond", ((Semantic)velocity.ElementType).SemanticTypeName.ToString());
		Assert.IsNotNull(((Semantic)velocity.ElementType).Declaration);
	}

	/// <summary>
	/// A vector of floats is written as it always was, so a file that has nothing to say about
	/// its components does not grow four lines under every one of them.
	/// </summary>
	[TestMethod]
	public void AVectorOfFloatsWritesNoComponent()
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember position = body.AddMember("Position".As<MemberName>())!;
		position.SetType(new Vector3());

		string json = SchemaSerializer.Serialize(schema);

		Assert.DoesNotContain("elementType", json, StringComparison.Ordinal);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.IsInstanceOfType<Float>(((Vector3)reloaded!.Classes.Single().Members.Single().Type).ElementType);
	}

	private static Schema BuildSchemaWithVelocity()
	{
		Schema schema = new();
		SchemaSemanticType metresPerSecond = schema.AddSemanticType("MetresPerSecond".As<SemanticTypeName>())!;
		metresPerSecond.SetUnderlyingType(new Float());

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember velocity = body.AddMember("Velocity".As<MemberName>())!;
		velocity.SetType(new Vector3
		{
			ElementType = new Semantic { SemanticTypeName = "MetresPerSecond".As<SemanticTypeName>() },
		});

		return schema;
	}
}
