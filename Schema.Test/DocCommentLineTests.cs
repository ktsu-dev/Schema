// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What a description spanning several lines becomes in generated C#.
/// </summary>
/// <remarks>
/// A description is written behind <c>///</c>, and a doc comment ends where the line does. The C#
/// lexer ends a line at any of six terminators, not only <c>\n</c>, so a description that carries
/// any of them has to be written as one comment line per line of text, or whatever follows the
/// terminator is read as code.
/// </remarks>
[TestClass]
public class DocCommentLineTests
{
	/// <summary>
	/// Every terminator the lexer recognises, each followed by text that is not valid C#.
	/// </summary>
	private const string Description =
		"first line\nsecond = line\r\nthird ( line\rfourth } line\u0085fifth ; line\u2028sixth , line\u2029seventh line";

	/// <summary>
	/// The generated source compiles, which it only does if no line of the description escaped its
	/// comment.
	/// </summary>
	[TestMethod]
	public void AMultiLineDescriptionCompiles()
	{
		SchemaGenerationResult result = Generate();

		Assert.IsTrue(result.IsSuccess, result.Message);
		Assert.IsNotNull(GeneratedSourceCompiler.Compile(result.Files).GetType("Generated.Body"));
	}

	/// <summary>
	/// Each line of the description becomes its own comment line, on the class and on its member,
	/// and none of the terminators survives into the file.
	/// </summary>
	[TestMethod]
	public void EachLineOfTheDescriptionIsItsOwnCommentLine()
	{
		string text = Generate().Files["Body.g.cs"];

		foreach (string line in (string[])["first line", "second = line", "third ( line", "fourth } line", "fifth ; line", "sixth , line", "seventh line"])
		{
			Assert.AreEqual(2, CountOccurrences(text, $"/// {line}"), $"'{line}' should be a comment line on the class and on the member");
		}

		foreach (char terminator in (char[])['\r', '\u0085', '\u2028', '\u2029'])
		{
			Assert.DoesNotContain(terminator.ToString(), text);
		}
	}

	private static int CountOccurrences(string text, string value)
	{
		int count = 0;

		for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
		{
			count++;
		}

		return count;
	}

	private static SchemaGenerationResult Generate()
	{
		Schema schema = new();

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		body.Description = Description.As<SchemaChildDescription>();

		SchemaMember speed = body.AddMember("Speed".As<MemberName>())!;
		speed.SetType(new Float());
		speed.Description = Description.As<SchemaChildDescription>();

		return SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
	}
}
