// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A schema names an imported declaration by the type's simple name, so two types sharing one
/// must be refused rather than folded into one declaration that describes neither.
/// </summary>
[TestClass]
public class ClrTypeNameCollisionTests
{
	[TestMethod]
	public void SameNamedTypesFromDifferentNamespacesAreRefused()
	{
		Schema schema = new();

		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddClass(typeof(CollisionFixtures.HoldsBothNamespaces)));
		StringAssert.Contains(exception.Message, typeof(CollisionFixtures.Alpha.Item).FullName);
		StringAssert.Contains(exception.Message, typeof(CollisionFixtures.Beta.Item).FullName);
	}

	[TestMethod]
	public void SameNamedNestedTypesAreRefused()
	{
		Schema schema = new();

		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddClass(typeof(CollisionFixtures.HoldsBothNested)));
		StringAssert.Contains(exception.Message, "'Leaf'");
	}

	[TestMethod]
	public void SameNamedEnumsAreRefused()
	{
		Schema schema = new();

		_ = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddClass(typeof(CollisionFixtures.HoldsBothEnums)));
	}

	[TestMethod]
	public void ACollisionAcrossTwoImportsIsRefused()
	{
		Schema schema = new();
		Assert.IsNotNull(schema.AddClass(typeof(CollisionFixtures.HoldsAlpha)));

		_ = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddClass(typeof(CollisionFixtures.HoldsBeta)));
	}

	[TestMethod]
	public void AClosedGenericMemberTypeIsRefused()
	{
		Schema schema = new();

		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddClass(typeof(CollisionFixtures.HoldsBox)));
		StringAssert.Contains(exception.Message, "generic");
		Assert.IsFalse(schema.Classes.Any(schemaClass => schemaClass.Name.ToString().Contains('`', StringComparison.Ordinal)));
	}

	[TestMethod]
	public void AGenericTypeIsRefusedAtTheTop()
	{
		Schema schema = new();

		_ = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddClass(typeof(CollisionFixtures.Box<int>)));
	}

	[TestMethod]
	public void OneTypeReachedTwiceIsOneClass()
	{
		Schema schema = new();
		SchemaClass? imported = schema.AddClass(typeof(CollisionFixtures.HoldsOneTwice));

		Assert.IsNotNull(imported);
		Assert.AreEqual(1, schema.Classes.Count(schemaClass => schemaClass.Name == "Item".As<ClassName>()));
		Assert.IsTrue(imported.Members.All(member => member.Type is Models.Types.Object { ClassName: var name } && name == "Item".As<ClassName>()));
	}

	[TestMethod]
	public void AClassHoldingItselfStillImports()
	{
		Schema schema = new();
		SchemaClass? imported = schema.AddClass(typeof(CollisionFixtures.Node));

		Assert.IsNotNull(imported);
		Assert.IsInstanceOfType<Models.Types.Object>(imported.Members.Single().Type);
	}

	[TestMethod]
	public void AHandDeclaredClassIsStillReused()
	{
		Schema schema = new();
		Assert.IsNotNull(schema.AddClass("Item".As<ClassName>()));

		SchemaClass? imported = schema.AddClass(typeof(CollisionFixtures.HoldsAlpha));

		Assert.IsNotNull(imported);
		Assert.AreEqual(1, schema.Classes.Count(schemaClass => schemaClass.Name == "Item".As<ClassName>()));
	}
}
