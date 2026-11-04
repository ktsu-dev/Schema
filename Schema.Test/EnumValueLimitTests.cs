// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// An enum is generated one byte wide, so it holds at most 256 values, and validation says so
/// rather than letting both generators emit an enum that does not compile.
/// </summary>
[TestClass]
public class EnumValueLimitTests
{
	[TestMethod]
	public void AnEnumWith257ValuesIsAnErrorAtTheEnum()
	{
		Schema schema = SchemaWithEnum(257);

		SchemaValidationIssue[] errors = [.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];

		Assert.HasCount(1, errors, string.Join("; ", errors.Select(e => e.Message)));
		Assert.AreEqual("Big", errors[0].Path);
		Assert.Contains("256", errors[0].Message);
		Assert.Contains("one-byte", errors[0].Message);
	}

	[TestMethod]
	public void AnEnumWith256ValuesValidates()
	{
		Schema schema = SchemaWithEnum(256);

		Assert.IsFalse(
			schema.Validate().Any(i => i.Severity == SchemaValidationSeverity.Error),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// The limit is the type's, not a guess: the largest enum validation accepts still compiles.
	/// </summary>
	[TestMethod]
	public void AnEnumWith256ValuesCompilesInCSharp()
	{
		Schema schema = SchemaWithEnum(256);

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Type big = assembly.GetType("Generated.Big")!;
		Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(big));
		Assert.HasCount(256, Enum.GetValues(big));
	}

	/// <summary>
	/// Builds a schema holding one enum called <c>Big</c> with the given number of values.
	/// </summary>
	/// <param name="count">How many values the enum has.</param>
	/// <returns>The schema.</returns>
	internal static Schema SchemaWithEnum(int count)
	{
		Schema schema = new();
		SchemaEnum big = schema.AddEnum("Big".As<EnumName>())!;

		for (int i = 0; i < count; i++)
		{
			big.TryAddValue($"Value{i}".As<EnumValueName>());
		}

		return schema;
	}
}
