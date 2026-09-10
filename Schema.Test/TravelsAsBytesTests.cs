// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers what a class promises when it says it travels as raw bytes, and what that promise
/// refuses.
/// </summary>
[TestClass]
public sealed class TravelsAsBytesTests
{
	/// <summary>
	/// A class that says nothing about how it travels is the ordinary case, and nothing about it
	/// is refused.
	/// </summary>
	[TestMethod]
	public void AClassMakesNoSuchPromiseByDefault()
	{
		Schema schema = new();
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		user.AddMember("Name".As<MemberName>())!.SetType(new Models.Types.String());

		Assert.IsFalse(user.TravelsAsBytes);
		Assert.IsEmpty(Errors(schema));
	}

	/// <summary>
	/// Fixed-size values travel as bytes, which is the case the promise exists for.
	/// </summary>
	[TestMethod]
	public void FixedSizeMembersAreAccepted()
	{
		Schema schema = new();
		SchemaEnum kind = schema.AddEnum("BodyKind".As<EnumName>())!;
		kind.TryAddValue("Static".As<EnumValueName>());

		SchemaSemanticType metresPerSecond = schema.AddSemanticType("MetresPerSecond".As<SemanticTypeName>())!;
		metresPerSecond.SetUnderlyingType(new Float());

		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Mass".As<MemberName>())!.SetType(new Float());
		body.AddMember("Ticks".As<MemberName>())!.SetType(new Long());
		body.AddMember("Awake".As<MemberName>())!.SetType(new Bool());
		body.AddMember("Kind".As<MemberName>())!.SetType(new Models.Types.Enum { EnumName = "BodyKind".As<EnumName>() });
		body.AddMember("Velocity".As<MemberName>())!.SetType(new Vector3
		{
			ElementType = new Semantic { SemanticTypeName = "MetresPerSecond".As<SemanticTypeName>() },
		});

		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// A handle is an index and the generation its slot had, so it travels as bytes whatever it
	/// identifies - which is the whole reason a component holds one rather than a reference.
	/// </summary>
	[TestMethod]
	public void AHandleTravelsWhateverItIdentifies()
	{
		Schema schema = new();
		schema.AddClass("Texture".As<ClassName>());

		SchemaClass sprite = schema.AddClass("Sprite".As<ClassName>())!;
		sprite.TravelsAsBytes = true;
		sprite.AddMember("Texture".As<MemberName>())!.SetType(new Handle
		{
			ElementType = new Models.Types.Object { ClassName = "Texture".As<ClassName>() },
		});

		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// Text is held elsewhere and reached by reference, so it would arrive as an address that
	/// means nothing where it landed.
	/// </summary>
	[TestMethod]
	public void TextIsRefused()
	{
		Collection<SchemaValidationIssue> errors = Errors(SchemaWithMember("Name", new Models.Types.String()));

		Assert.ContainsSingle(errors);
		Assert.Contains("is text", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A collection owns its elements somewhere else, however many there are.
	/// </summary>
	[TestMethod]
	public void ACollectionIsRefused()
	{
		Collection<SchemaValidationIssue> errors = Errors(SchemaWithMember(
			"Tags",
			new Models.Types.Array { ElementType = new Int(), Container = "vector".As<ContainerName>() }));

		Assert.Contains(i => i.Message.Contains("is a collection", StringComparison.Ordinal), errors);
	}

	/// <summary>
	/// A view points at memory the class does not own.
	/// </summary>
	[TestMethod]
	public void AViewIsRefused()
	{
		Collection<SchemaValidationIssue> errors = Errors(SchemaWithMember("Pixels", new Span { ElementType = new Int() }));

		Assert.ContainsSingle(errors);
		Assert.Contains("is a view", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// An Optional is trivially copyable in some languages and not in others, and in none of them
	/// is the position of what it wraps something the schema can promise.
	/// </summary>
	[TestMethod]
	public void AnOptionalIsRefusedForItsLayoutRatherThanItsContent()
	{
		Collection<SchemaValidationIssue> errors = Errors(SchemaWithMember("Parent", new Optional { ElementType = new Int() }));

		Assert.ContainsSingle(errors);
		Assert.Contains("layout is the target language's to choose", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A class that travels as bytes may only hold classes that promise the same, since the whole
	/// point is that nobody reads a field on the way past.
	/// </summary>
	[TestMethod]
	public void AClassThatMakesNoSuchPromiseIsRefused()
	{
		Schema schema = new();
		schema.AddClass("Loose".As<ClassName>());

		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Loose".As<MemberName>())!.SetType(new Models.Types.Object { ClassName = "Loose".As<ClassName>() });

		Collection<SchemaValidationIssue> errors = Errors(schema);
		Assert.ContainsSingle(errors);
		Assert.Contains("does not travel as bytes", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// The same class held by one that does make the promise is accepted, which is what makes a
	/// component of components expressible.
	/// </summary>
	[TestMethod]
	public void AClassThatMakesThePromiseIsAccepted()
	{
		Schema schema = new();
		SchemaClass point = schema.AddClass("Point".As<ClassName>())!;
		point.TravelsAsBytes = true;
		point.AddMember("X".As<MemberName>())!.SetType(new Float());

		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Origin".As<MemberName>())!.SetType(new Models.Types.Object { ClassName = "Point".As<ClassName>() });

		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// Two classes holding each other is a schema someone can write, and validating it must not
	/// hang: the promise is read off each class rather than walked through it.
	/// </summary>
	[TestMethod]
	public void TwoClassesHoldingEachOtherDoNotHangValidation()
	{
		Schema schema = new();
		SchemaClass left = schema.AddClass("Left".As<ClassName>())!;
		SchemaClass right = schema.AddClass("Right".As<ClassName>())!;
		left.TravelsAsBytes = true;
		right.TravelsAsBytes = true;
		left.AddMember("Other".As<MemberName>())!.SetType(new Models.Types.Object { ClassName = "Right".As<ClassName>() });
		right.AddMember("Other".As<MemberName>())!.SetType(new Models.Types.Object { ClassName = "Left".As<ClassName>() });

		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// A semantic type refining itself is a schema someone can write, and it is already reported
	/// as a cycle. Reading through it must not recurse without a bottom.
	/// </summary>
	[TestMethod]
	public void ASemanticCycleDoesNotHangValidation()
	{
		Schema schema = new();
		SchemaSemanticType left = schema.AddSemanticType("Left".As<SemanticTypeName>())!;
		SchemaSemanticType right = schema.AddSemanticType("Right".As<SemanticTypeName>())!;
		left.SetUnderlyingType(new Semantic { SemanticTypeName = "Right".As<SemanticTypeName>() });
		right.SetUnderlyingType(new Semantic { SemanticTypeName = "Left".As<SemanticTypeName>() });

		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Spin".As<MemberName>())!.SetType(new Semantic { SemanticTypeName = "Left".As<SemanticTypeName>() });

		// The cycle itself is reported; the member is not refused a second time for it.
		Assert.Contains(i => i.Message.Contains("refines itself", StringComparison.Ordinal), Errors(schema));
		Assert.DoesNotContain(i => i.Message.Contains("travels as bytes, but member", StringComparison.Ordinal), Errors(schema));
	}

	/// <summary>
	/// A semantic type is only as transferable as what it is represented as.
	/// </summary>
	[TestMethod]
	public void ASemanticTypeOverTextIsRefused()
	{
		Schema schema = new();
		SchemaSemanticType tag = schema.AddSemanticType("Tag".As<SemanticTypeName>())!;
		tag.SetUnderlyingType(new Models.Types.String());

		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Tag".As<MemberName>())!.SetType(new Semantic { SemanticTypeName = "Tag".As<SemanticTypeName>() });

		Collection<SchemaValidationIssue> errors = Errors(schema);
		Assert.ContainsSingle(errors);
		Assert.Contains("is text", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A member whose type was never chosen is already reported as one; saying it cannot travel
	/// as bytes as well would be two issues for the same unfinished edit.
	/// </summary>
	[TestMethod]
	public void AnUnfinishedMemberIsNotRefusedTwice()
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember("Unfinished".As<MemberName>());

		Assert.IsEmpty(Errors(schema));
		Assert.ContainsSingle(i => i.Severity == SchemaValidationSeverity.Warning, schema.Validate());
	}

	/// <summary>
	/// The promise survives a save and a load, and a class that does not make it does not grow a
	/// line in the file for saying so.
	/// </summary>
	[TestMethod]
	public void ThePromiseRoundTripsAndIsOmittedWhenNotMade()
	{
		Schema schema = new();
		schema.AddClass("Loose".As<ClassName>());
		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;

		string json = SchemaSerializer.Serialize(schema);
		Assert.ContainsSingle(Occurrences(json, "travelsAsBytes"));

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.IsFalse(reloaded!.GetClass("Loose".As<ClassName>())!.TravelsAsBytes);
		Assert.IsTrue(reloaded.GetClass("RigidBody".As<ClassName>())!.TravelsAsBytes);
	}

	private static IEnumerable<int> Occurrences(string text, string value)
	{
		for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
		{
			yield return index;
		}
	}

	private static Schema SchemaWithMember(string memberName, BaseType type)
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("RigidBody".As<ClassName>())!;
		body.TravelsAsBytes = true;
		body.AddMember(memberName.As<MemberName>())!.SetType(type);
		return schema;
	}

	private static Collection<SchemaValidationIssue> Errors(Schema schema) =>
		[.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];
}
