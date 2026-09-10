// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers semantic types: a distinct name for something already representable, so that two values
/// sharing a representation stop being interchangeable.
/// </summary>
[TestClass]
public sealed class SchemaSemanticTypeTests
{
	private static readonly string[] ExpectedRefinementChain = ["ForceMagnitude"];

	/// <summary>
	/// Two semantic types over the same underlying type are not the same type.
	/// </summary>
	/// <remarks>
	/// This is the whole point: an entity id and a texture id are both numbers, and adding one to
	/// the other is nonsense that would otherwise compile.
	/// </remarks>
	[TestMethod]
	public void TwoSemanticTypesOverTheSameRepresentationAreNotEqual()
	{
		Semantic entityId = new() { SemanticTypeName = "EntityId".As<SemanticTypeName>() };
		Semantic textureId = new() { SemanticTypeName = "TextureId".As<SemanticTypeName>() };

		Assert.AreNotEqual(entityId, textureId);
		Assert.AreEqual(entityId, new Semantic { SemanticTypeName = "EntityId".As<SemanticTypeName>() });
	}

	/// <summary>
	/// A reference resolves to its declaration, and the declaration knows what it is stored as.
	/// </summary>
	[TestMethod]
	public void AReferenceResolvesToItsDeclaration()
	{
		Schema schema = new();
		SchemaSemanticType entityId = schema.AddSemanticType("EntityId".As<SemanticTypeName>())!;
		entityId.SetUnderlyingType(new Long());

		SchemaClass entity = schema.AddClass("Entity".As<ClassName>())!;
		SchemaMember id = entity.AddMember("Id".As<MemberName>())!;
		id.SetType(new Semantic { SemanticTypeName = "EntityId".As<SemanticTypeName>() });

		Semantic reference = (Semantic)id.Type;
		Assert.IsNotNull(reference.Declaration);
		Assert.IsInstanceOfType<Long>(reference.Declaration!.UnderlyingType);
	}

	/// <summary>
	/// A semantic type may refine another, and the chain reports what it is ultimately stored as.
	/// </summary>
	[TestMethod]
	public void RefinementResolvesToTheUnderlyingRepresentation()
	{
		Schema schema = new();
		SchemaSemanticType force = schema.AddSemanticType("ForceMagnitude".As<SemanticTypeName>())!;
		force.SetUnderlyingType(new Float());

		SchemaSemanticType weight = schema.AddSemanticType("Weight".As<SemanticTypeName>())!;
		weight.SetUnderlyingType(new Semantic { SemanticTypeName = "ForceMagnitude".As<SemanticTypeName>() });

		CollectionAssert.AreEqual(
			ExpectedRefinementChain,
			weight.Refines().Select(t => t.Name.ToString()).ToArray());
		Assert.IsInstanceOfType<Float>(weight.Representation());
	}

	/// <summary>
	/// Metadata intrinsic to the type is declared once on the type rather than on every member.
	/// </summary>
	[TestMethod]
	public void ASemanticTypeCarriesItsOwnMetadata()
	{
		Schema schema = new();
		SchemaSemanticType metres = schema.AddSemanticType("Metres".As<SemanticTypeName>())!;
		metres.SetUnderlyingType(new Float());
		metres.Unit = "m".As<UnitSymbol>();

		Assert.IsTrue(metres.TryResolveUnit(out _, out string error), error);
		Assert.IsEmpty(schema.Validate().Where(i => i.Path.StartsWith("Metres", StringComparison.Ordinal)));
	}

	/// <summary>
	/// The metadata rules are the same ones members get, so a unit on something that measures
	/// nothing is refused wherever it is declared.
	/// </summary>
	[TestMethod]
	public void AUnitOnANonNumericSemanticTypeIsRejected()
	{
		Schema schema = new();
		SchemaSemanticType name = schema.AddSemanticType("PlayerName".As<SemanticTypeName>())!;
		name.SetUnderlyingType(new Models.Types.String());
		name.Unit = "m".As<UnitSymbol>();

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("meaningless", StringComparison.Ordinal)));
	}

	/// <summary>
	/// Everything a semantic type declares survives a save and a load, including the reference
	/// from a member and the resolution behind it.
	/// </summary>
	[TestMethod]
	public void ASemanticTypeRoundTrips()
	{
		Schema schema = new();
		SchemaSemanticType metres = schema.AddSemanticType("Metres".As<SemanticTypeName>())!;
		metres.SetUnderlyingType(new Float());
		metres.Unit = "m".As<UnitSymbol>();
		metres.Interpolation = Interpolation.Linear;

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		body.AddMember("Height".As<MemberName>())!
			.SetType(new Semantic { SemanticTypeName = "Metres".As<SemanticTypeName>() });

		Assert.IsTrue(SchemaSerializer.TryDeserialize(SchemaSerializer.Serialize(schema), out Schema? loaded));

		SchemaSemanticType loadedMetres = loaded!.GetSemanticType("Metres".As<SemanticTypeName>())!;
		Assert.IsInstanceOfType<Float>(loadedMetres.UnderlyingType);
		Assert.AreEqual("m", loadedMetres.Unit!.ToString());
		Assert.AreEqual(Interpolation.Linear, loadedMetres.Interpolation);

		SchemaMember height = loaded.GetClass("Body".As<ClassName>())!.Members.ElementAt(0);
		Assert.IsInstanceOfType<Semantic>(height.Type);
		Assert.IsNotNull(((Semantic)height.Type).Declaration);
	}

	/// <summary>
	/// A class, an interface and an enum are already distinct types, so naming one again buys
	/// nothing and is refused rather than silently generating a redundant wrapper.
	/// </summary>
	[TestMethod]
	public void ASemanticTypeOverAnAlreadyDistinctTypeIsRejected()
	{
		Schema schema = new();
		schema.AddClass("Transform".As<ClassName>());
		SchemaSemanticType shim = schema.AddSemanticType("Placement".As<SemanticTypeName>())!;
		shim.SetUnderlyingType(new Object { ClassName = "Transform".As<ClassName>() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("already a distinct type", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A wrapper says how a value is carried, not what it means, so shimming one crosses two
	/// unrelated axes.
	/// </summary>
	[TestMethod]
	public void ASemanticTypeOverAWrapperIsRejected()
	{
		Schema schema = new();
		SchemaSemanticType shim = schema.AddSemanticType("Samples".As<SemanticTypeName>())!;
		shim.SetUnderlyingType(new Span { ElementType = new Float() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("how a value is carried", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A refinement cycle is reported rather than hung on.
	/// </summary>
	[TestMethod]
	public void ARefinementCycleIsRejected()
	{
		Schema schema = new();
		SchemaSemanticType a = schema.AddSemanticType("A".As<SemanticTypeName>())!;
		SchemaSemanticType b = schema.AddSemanticType("B".As<SemanticTypeName>())!;
		a.SetUnderlyingType(new Semantic { SemanticTypeName = "B".As<SemanticTypeName>() });
		b.SetUnderlyingType(new Semantic { SemanticTypeName = "A".As<SemanticTypeName>() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("refines itself", StringComparison.Ordinal)));
	}

	/// <summary>
	/// Refining a semantic type the schema does not declare is reported rather than dangling.
	/// </summary>
	[TestMethod]
	public void AnUnresolvedRefinementIsRejected()
	{
		Schema schema = new();
		SchemaSemanticType weight = schema.AddSemanticType("Weight".As<SemanticTypeName>())!;
		weight.SetUnderlyingType(new Semantic { SemanticTypeName = "ForceMagnitude".As<SemanticTypeName>() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("does not declare", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A semantic type with no underlying type chosen is not generatable.
	/// </summary>
	[TestMethod]
	public void AnUnchosenUnderlyingTypeIsRejected()
	{
		Schema schema = new();
		schema.AddSemanticType("Undecided".As<SemanticTypeName>());

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("no underlying type chosen", StringComparison.Ordinal)));
	}
}
