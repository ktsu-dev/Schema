// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A map is keyed either by one of its elements' members or by a type of its own. The second is
/// what a dictionary of scalars needs, since a string has no member to key it by.
/// </summary>
[TestClass]
public sealed class MapKeyTypeTests
{
	private static ContainerName Map => Array.MapContainer.As<ContainerName>();

	private static Schema HolderOf(Array array)
	{
		Schema schema = new();
		schema.AddClass("Holder".As<ClassName>())!.AddMember("Value".As<MemberName>())!.SetType(array);
		return schema;
	}

	private static Array ImportedMember(System.Type type, string memberName)
	{
		Schema schema = new();
		SchemaClass imported = schema.AddClass(type)!;
		Assert.IsTrue(imported.TryGetMember(memberName.As<MemberName>(), out SchemaMember? member));
		Assert.IsInstanceOfType<Array>(member!.Type);
		Assert.IsEmpty(
			schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error),
			string.Join("; ", schema.Validate()));
		return (Array)member.Type;
	}

	/// <summary>
	/// The import that used to produce a map the validator rejected: nothing on a string to key by.
	/// </summary>
	[TestMethod]
	public void ADictionaryOfScalarsImportsAsAMapWithAKeyType()
	{
		Array map = ImportedMember(typeof(TypeWithScalarMaps), nameof(TypeWithScalarMaps.Labels));

		Assert.AreEqual(Map, map.Container);
		Assert.IsInstanceOfType<String>(map.KeyType);
		Assert.IsInstanceOfType<String>(map.ElementType);
		Assert.IsTrue(map.Key.IsEmpty());
	}

	[TestMethod]
	public void ADictionaryKeyedByIntKeepsItsKeyType()
	{
		Array map = ImportedMember(typeof(TypeWithScalarMaps), nameof(TypeWithScalarMaps.Counts));

		Assert.IsInstanceOfType<Int>(map.KeyType);
		Assert.IsInstanceOfType<Int>(map.ElementType);
	}

	[TestMethod]
	public void ADictionaryOfClassesImportsWithTheKeyType()
	{
		Array map = ImportedMember(typeof(TypeWithCollections), nameof(TypeWithCollections.ItemsById));

		Assert.IsInstanceOfType<String>(map.KeyType);
		Assert.IsInstanceOfType<Object>(map.ElementType);
	}

	[TestMethod]
	public void AMapNamingNeitherAKeyMemberNorAKeyTypeIsRejected()
	{
		Schema schema = HolderOf(new Array { ElementType = new String(), Container = Map });

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error && i.Message.Contains("neither a key member nor a key type", System.StringComparison.Ordinal),
			schema.Validate());
	}

	[TestMethod]
	public void AMapNamingBothAKeyMemberAndAKeyTypeIsRejected()
	{
		Schema schema = HolderOf(new Array
		{
			ElementType = new String(),
			Container = Map,
			Key = "Id".As<MemberName>(),
			KeyType = new String(),
		});

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error && i.Message.Contains("one or the other", System.StringComparison.Ordinal),
			schema.Validate());
	}

	[TestMethod]
	public void AKeyTypeOnAVectorIsRejected()
	{
		Schema schema = HolderOf(new Array
		{
			ElementType = new String(),
			Container = Array.VectorContainer.As<ContainerName>(),
			KeyType = new String(),
		});

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error && i.Message.Contains("does not use the 'map' container", System.StringComparison.Ordinal),
			schema.Validate());
	}

	[TestMethod]
	public void AKeyTypeThatIsNotAScalarIsRejected()
	{
		Schema schema = HolderOf(new Array { ElementType = new String(), Container = Map, KeyType = new Vector3() });

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error && i.Message.Contains("must be a primitive type or an enum", System.StringComparison.Ordinal),
			schema.Validate());
	}

	[TestMethod]
	public void AKeyTypeSurvivesTheFile()
	{
		Schema schema = HolderOf(new Array { ElementType = new String(), Container = Map, KeyType = new Int() });

		string json = SchemaSerializer.Serialize(schema);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));

		Assert.IsTrue(reloaded!.TryGetClass("Holder".As<ClassName>(), out SchemaClass? holder));
		Assert.IsTrue(holder!.TryGetMember("Value".As<MemberName>(), out SchemaMember? member));
		Assert.IsInstanceOfType<Int>(((Array)member!.Type).KeyType);
	}

	/// <summary>
	/// A map keyed by a member says nothing about a key type, so a file written before the property
	/// existed is written exactly as it was.
	/// </summary>
	[TestMethod]
	public void AMapWithoutAKeyTypeWritesNoKeyType()
	{
		Schema schema = HolderOf(new Array { ElementType = new String(), Container = Map, Key = "Id".As<MemberName>() });

		Assert.DoesNotContain("keyType", SchemaSerializer.Serialize(schema), System.StringComparison.OrdinalIgnoreCase);
	}

	[TestMethod]
	public void AMapWithAKeyTypeRoundTripsThroughGeneratedCSharp()
	{
		Schema original = HolderOf(new Array { ElementType = new String(), Container = Map, KeyType = new Int() });

		SchemaGenerationResult result = SchemaGenerator.Generate(original, CodeGenerationTests.ConfigureGenerator(original));
		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Schema reimported = new();
		SchemaClass holder = reimported.AddClass(assembly.GetType("Generated.Holder", throwOnError: true)!)!;
		Assert.IsTrue(holder.TryGetMember("Value".As<MemberName>(), out SchemaMember? member));
		Array map = (Array)member!.Type;

		Assert.AreEqual(Map, map.Container);
		Assert.IsInstanceOfType<Int>(map.KeyType);
		Assert.IsInstanceOfType<String>(map.ElementType);
	}
}

public class TypeWithScalarMaps
{
	public Dictionary<string, string> Labels { get; } = [];
	public Dictionary<int, int> Counts { get; } = [];
}
