// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A value's position in an enumeration is its number in generated code, so putting one back has
/// to put it where it was.
/// </summary>
[TestClass]
public class EnumValueOrderTests
{
	private static readonly string[] RedGreenBlue = ["Red", "Green", "Blue"];

	[TestMethod]
	public void AValueInsertedAtAPositionLandsThere()
	{
		SchemaEnum colour = Colour("Red", "Blue");

		Assert.IsTrue(colour.TryInsertValue(1, "Green".As<EnumValueName>()));

		Assert.AreSequenceEqual(RedGreenBlue, Names(colour));
	}

	[TestMethod]
	public void AValueCanBeInsertedAtEitherEnd()
	{
		SchemaEnum colour = Colour("Green");

		Assert.IsTrue(colour.TryInsertValue(0, "Red".As<EnumValueName>()));
		Assert.IsTrue(colour.TryInsertValue(2, "Blue".As<EnumValueName>()));

		Assert.AreSequenceEqual(RedGreenBlue, Names(colour));
	}

	[TestMethod]
	public void AValueAlreadyPresentIsNotInsertedAgain()
	{
		SchemaEnum colour = Colour("Red", "Green", "Blue");

		Assert.IsFalse(colour.TryInsertValue(0, "Blue".As<EnumValueName>()));

		Assert.AreSequenceEqual(RedGreenBlue, Names(colour));
	}

	[TestMethod]
	public void APositionOutsideTheValuesIsRefused()
	{
		SchemaEnum colour = Colour("Red");

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => colour.TryInsertValue(2, "Green".As<EnumValueName>()));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => colour.TryInsertValue(-1, "Green".As<EnumValueName>()));
	}

	private static SchemaEnum Colour(params string[] values)
	{
		Schema schema = new();
		SchemaEnum colour = schema.AddEnum("Colour".As<EnumName>())!;
		foreach (string value in values)
		{
			colour.TryAddValue(value.As<EnumValueName>());
		}

		return colour;
	}

	private static string[] Names(SchemaEnum schemaEnum) => [.. schemaEnum.Values.Select(v => v.ToString())];
}
