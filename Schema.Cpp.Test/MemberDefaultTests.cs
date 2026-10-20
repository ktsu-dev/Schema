// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using System.Diagnostics;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What a member's default becomes in the generated header.
/// </summary>
/// <remarks>
/// A default is held as a <see cref="double"/> whatever the member is stored as, so writing one out
/// is a conversion rather than a copy, and the ways a conversion can be wrong are the ways a
/// generated header can hold a number the schema did not say. These are those ways: a value too
/// small to survive the writing, a value with more significant digits than the writing kept, a
/// whole number that has to read as a float rather than an int, and a value C++ cannot write at
/// all.
/// </remarks>
[TestClass]
public class MemberDefaultTests
{
	private static readonly string[] Compilers = ["g++", "clang++"];

	/// <summary>
	/// A fixed number of decimal places measures from the point rather than from the first digit
	/// that matters, so a small enough value rounds to nothing. A quantisation step is exactly the
	/// kind of number that is that small, and a default that silently becomes zero is a default
	/// nobody can see is wrong.
	/// </summary>
	[TestMethod]
	public void ASmallDefaultSurvives()
	{
		string code = GenerateWithDefault(1e-17);

		Assert.DoesNotContain("= 0.0f", code);
		Assert.Contains("1E-17f", code);
	}

	/// <summary>
	/// The same measurement, one step less extreme: the value survives but its digits do not. The
	/// header used to say 0.0000000000012346 for this, which is a different number.
	/// </summary>
	[TestMethod]
	public void ADefaultKeepsItsSignificantDigits() =>
		Assert.Contains("1.2345678E-12f", GenerateWithDefault(1.2345678e-12));

	/// <summary>
	/// The round-trip form of a whole number has neither a point nor an exponent, and <c>3f</c> is
	/// not a float literal in C++ — it is an integer carrying a suffix no integer takes. So the
	/// point has to be put back.
	/// </summary>
	[TestMethod]
	public void AWholeDefaultIsStillWrittenAsAFloat()
	{
		string code = GenerateWithDefault(3);

		Assert.Contains("3.0f", code);
		Assert.DoesNotContain("= 3f", code);
	}

	/// <summary>
	/// Two of the values a double can hold have no C++ literal. JSON cannot write either, so one
	/// can only arrive from a CLR type whose initialiser held it — and a header that reaches for a
	/// new include to write a default nobody meant is worse than being told.
	/// </summary>
	[TestMethod]
	public void ADefaultCppCannotWriteIsRefusedByName()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateWithDefault(double.NaN));

		Assert.Contains("Speed", refused.Message);
	}

	/// <summary>
	/// A string default containing both a quote and a backslash is written as a C++ string literal
	/// with both escaped.
	/// </summary>
	[TestMethod]
	public void AStringDefaultIsQuotedAndEscaped() =>
		Assert.Contains("std::string name = std::string{ \"anon\\\"ym\\\\ous\" };", GenerateWithDefault("anon\"ym\\ous"));

	/// <summary>
	/// A double's own text turns to an exponent from 1e15 on, and <c>1E+18</c> is a floating literal
	/// a braced initialiser refuses for an integer member. The value has to be written as the
	/// integer it is, on a member stored as one and on a semantic type over one alike.
	/// </summary>
	/// <param name="value">The default.</param>
	/// <param name="expected">The literal it should be written as.</param>
	[TestMethod]
	[DataRow(1e15, "1000000000000000")]
	[DataRow(1e18, "1000000000000000000")]
	[DataRow(-9007199254740992d, "-9007199254740992")]
	[DataRow(-9223372036854775808d, "(-9223372036854775807LL - 1)")]
	public void ALargeIntegerDefaultIsWrittenAsAnInteger(double value, string expected)
	{
		foreach (bool semantic in (bool[])[false, true])
		{
			IReadOnlyDictionary<string, string> files = GenerateIntegerDefault(new Long(), value, semantic);

			Assert.Contains(expected, files["Body.gen.hpp"]);
			Assert.DoesNotContain("E+", files["Body.gen.hpp"]);
			AssertCompiles(files);
		}
	}

	/// <summary>
	/// A 32-bit member is written the same way, and a value no integer of its width holds is
	/// refused by name rather than written for the compiler to refuse.
	/// </summary>
	[TestMethod]
	public void AnIntegerDefaultOutsideItsTypeIsRefusedByName()
	{
		AssertCompiles(GenerateIntegerDefault(new Int(), int.MinValue, semantic: false));

		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateIntegerDefault(new Int(), 1e10, semantic: false));

		Assert.Contains("Count", refused.Message);
		Assert.ThrowsExactly<CppGenerationException>(() => GenerateIntegerDefault(new Long(), 0.5, semantic: false));
	}

	/// <summary>
	/// Builds a one-member schema whose member defaults to the given value, and generates it.
	/// </summary>
	/// <param name="value">The default.</param>
	/// <returns>The generated header.</returns>
	private static string GenerateWithDefault(double value)
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember speed = body.AddMember("Speed".As<MemberName>())!;
		speed.SetType(new Float());
		speed.DefaultValue = new NumberDefault { Value = value };

		return new CppCodeGenerator().Generate(schema, configuration)["Body.gen.hpp"];
	}

	/// <summary>
	/// Builds a one-member schema whose string member defaults to the given value, and generates it.
	/// </summary>
	/// <param name="value">The default.</param>
	/// <returns>The generated header.</returns>
	private static string GenerateWithDefault(string value)
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember name = body.AddMember("Name".As<MemberName>())!;
		name.SetType(new Models.Types.String());
		name.DefaultValue = new TextDefault { Value = value };

		return new CppCodeGenerator().Generate(schema, configuration)["Body.gen.hpp"];
	}

	/// <summary>
	/// Builds a one-member schema whose integer member defaults to the given value, either directly
	/// or through a semantic type over the integer, and generates it.
	/// </summary>
	/// <param name="type">The integer type.</param>
	/// <param name="value">The default.</param>
	/// <param name="semantic">Whether the member is typed by a semantic type over <paramref name="type"/>.</param>
	/// <returns>Every file generated.</returns>
	private static IReadOnlyDictionary<string, string> GenerateIntegerDefault(BaseType type, double value, bool semantic)
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember count = body.AddMember("Count".As<MemberName>())!;

		if (semantic)
		{
			schema.AddSemanticType("Tally".As<SemanticTypeName>())!.SetUnderlyingType(type);
			count.SetType(new Semantic { SemanticTypeName = "Tally".As<SemanticTypeName>() });
		}
		else
		{
			count.SetType(type);
		}

		count.DefaultValue = new NumberDefault { Value = value };

		return new CppCodeGenerator().Generate(schema, configuration);
	}

	/// <summary>
	/// Compiles every generated header as one translation unit.
	/// </summary>
	/// <param name="files">The generated files.</param>
	private static void AssertCompiles(IReadOnlyDictionary<string, string> files)
	{
		string? compiler = Compilers
			.SelectMany(name => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
				.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
				.Select(directory => Path.Join(directory, name)))
			.FirstOrDefault(File.Exists);

		if (compiler is null)
		{
			Assert.Inconclusive("no C++ compiler on PATH, so the generated headers were not compiled.");
		}

		string directory = Path.Join(Path.GetTempPath(), $"schema-default-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);

		try
		{
			foreach ((string name, string text) in files)
			{
				File.WriteAllText(Path.Join(directory, name), text);
			}

			string includes = string.Concat(files.Keys.Order(StringComparer.Ordinal).Select(name => $"#include \"{name}\"\n"));
			File.WriteAllText(Path.Join(directory, "all.cpp"), $"{includes}\nint main() {{ return 0; }}\n");

			using Process process = new()
			{
				StartInfo = new ProcessStartInfo(compiler!)
				{
					WorkingDirectory = directory,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
				},
			};

			foreach (string argument in (string[])["-std=c++20", "-Wall", "-Wextra", "-Werror", "-fsyntax-only", "-I.", "all.cpp"])
			{
				process.StartInfo.ArgumentList.Add(argument);
			}

			process.Start();
			string output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			Assert.AreEqual(0, process.ExitCode, $"the generated C++ should compile:\n{output}");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}
