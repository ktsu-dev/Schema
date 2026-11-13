// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <c>value__</c> is the one enumerator name C# refuses outright, even escaped as <c>@value__</c>
/// (CS0076), so validation reports it rather than passing a schema whose C# does not compile.
/// </summary>
[TestClass]
public class ReservedEnumValueNameTests
{
	[TestMethod]
	public void AnEnumValueNamedValueUnderscoresIsAnErrorAtTheValue()
	{
		Schema schema = SchemaWithEnumValue("value__");

		SchemaValidationIssue[] errors = [.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];

		Assert.HasCount(1, errors, string.Join("; ", errors.Select(e => e.Message)));
		Assert.AreEqual("Odd.value__", errors[0].Path);
		Assert.Contains("'value__'", errors[0].Message);
		Assert.Contains("reserved", errors[0].Message);
	}

	/// <summary>
	/// C# identifiers are case-sensitive, so only the exact spelling is reserved, and the others
	/// validate and compile.
	/// </summary>
	[TestMethod]
	[DataRow("Value__")]
	[DataRow("VALUE__")]
	[DataRow("value_")]
	public void ANameThatOnlyResemblesItValidatesAndCompiles(string name)
	{
		Schema schema = SchemaWithEnumValue(name);

		Assert.IsFalse(
			schema.Validate().Any(i => i.Severity == SchemaValidationSeverity.Error),
			string.Join("; ", schema.Validate().Select(i => i.Message)));

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Type odd = assembly.GetType("Generated.Odd")!;
		CollectionAssert.Contains(Enum.GetNames(odd), name);
	}

	private static Schema SchemaWithEnumValue(string name)
	{
		Schema schema = new();
		SchemaEnum schemaEnum = schema.AddEnum("Odd".As<EnumName>())!;
		schemaEnum.TryAddValue("Plain".As<EnumValueName>());
		schemaEnum.TryAddValue(name.As<EnumValueName>());
		return schema;
	}
}
