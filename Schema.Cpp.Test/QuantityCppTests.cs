// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using System.Diagnostics;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A member holding one of <c>ktsu.Semantics.Quantities</c>' physical quantities, in C++.
/// </summary>
/// <remarks>
/// <para>
/// The C++ side of a quantity is the side where it stops resembling a semantic type. A semantic
/// type is a class this generator writes; a quantity is a class the target already has, because
/// <c>ktsu.Semantics.Cpp</c> emitted all 212 of them into the target's tree. So what a target says
/// is where it put them - one entry for the whole vocabulary, since it named none of them.
/// </para>
/// <para>
/// And the storage is the one thing the two languages can disagree about. C# closes
/// <c>Mass&lt;T&gt;</c> per member; a C++ <c>Mass</c> is a class rather than a template, fixed at
/// whatever the vocabulary was generated over. A member the schema stores in a double therefore
/// has no C++ spelling on a target whose quantities are floats, and saying so is the difference
/// between a refusal and two languages quietly disagreeing about the bytes.
/// </para>
/// </remarks>
[TestClass]
public sealed class QuantityCppTests
{
	private const string VocabularyHeader = "\"quantities.hpp\"";

	/// <summary>
	/// What a target that ran the vocabulary generator says for itself.
	/// </summary>
	private static CppGeneratorOptions TargetOptions { get; } = new()
	{
		Quantities = new CppQuantitySpelling("sample", VocabularyHeader),
	};

	/// <summary>
	/// The smallest thing that can stand in for a generated vocabulary.
	/// </summary>
	/// <remarks>
	/// Three classes over a float and one over three, each an aggregate and nothing else - which
	/// is what the real ones are, and what has to be true for a class promising to travel as bytes
	/// to hold one. If any of these were not trivially copyable the assertion the generator writes
	/// beside that class would fail to compile, which is what makes compiling this worth doing.
	/// </remarks>
	private const string Vocabulary = """
		#pragma once

		namespace sample
		{
			class Mass { public: float value; };
			class Ratio { public: float value; };
			class Heading { public: float value; };
			class Velocity3D { public: float x, y, z; };
		}
		""";

	/// <summary>
	/// A quantity is written where the target's vocabulary put it, and nothing is emitted for it.
	/// </summary>
	[TestMethod]
	public void ItIsNamedWhereTheTargetPutIt()
	{
		IReadOnlyDictionary<string, string> files = Generate(Body(), TargetOptions);

		Assert.ContainsSingle(files, "a quantity is named, not generated, so there is one header and it is the class's");
		Assert.Contains("sample::Mass", files.Values.Single(), StringComparison.Ordinal);
		Assert.Contains($"#include {VocabularyHeader}", files.Values.Single(), StringComparison.Ordinal);
	}

	/// <summary>
	/// A target with no quantities is refused them by name, with the option to set.
	/// </summary>
	/// <remarks>
	/// The same treatment a missing vector gets, and for the same reason: a header naming a type
	/// the target does not have is worse than being told which option would give it one.
	/// </remarks>
	[TestMethod]
	public void ATargetWithNoVocabularyIsRefused()
	{
		CppGenerationException refusal = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate(Body(), new CppGeneratorOptions()));

		Assert.Contains("Mass", refusal.Message, StringComparison.Ordinal);
		Assert.Contains("quantities", refusal.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A storage the target's vocabulary was not generated over is refused.
	/// </summary>
	/// <remarks>
	/// The failure this prevents is the quiet one. Generated C# would say
	/// <c>Mass&lt;double&gt;</c>, generated C++ would say <c>sample::Mass</c>, both would compile,
	/// and a class promising to travel as bytes would be eight bytes in one language and four in
	/// the other - which is the whole class of bug the enum width and the bool marshalling were
	/// already spelled out to prevent.
	/// </remarks>
	[TestMethod]
	public void AStorageTheVocabularyDoesNotHaveIsRefused()
	{
		Schema schema = Body(new Quantity
		{
			QuantityName = "Mass".As<QuantityName>(),
			Storage = new Models.Types.Double(),
		});

		CppGenerationException refusal = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate(schema, TargetOptions));

		Assert.Contains("Double", refusal.Message, StringComparison.Ordinal);
		Assert.Contains("Float", refusal.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A target whose vocabulary is doubles takes the member the float target refused.
	/// </summary>
	[TestMethod]
	public void AVocabularyOfDoublesTakesADoubleMember()
	{
		Schema schema = Body(new Quantity
		{
			QuantityName = "Mass".As<QuantityName>(),
			Storage = new Models.Types.Double(),
		});

		IReadOnlyDictionary<string, string> files = Generate(
			schema,
			new CppGeneratorOptions { Quantities = new CppQuantitySpelling("sample", VocabularyHeader, "Double") });

		Assert.Contains("sample::Mass", files.Values.Single(), StringComparison.Ordinal);
	}

	/// <summary>
	/// A storage that is not a number at all is refused when the file is read, not when a schema
	/// is generated.
	/// </summary>
	[TestMethod]
	public void AVocabularyStoredInNothingIsRefusedByTheOptionsFile()
	{
		Assert.IsFalse(
			CppGeneratorOptionsFile.TryParse(
				"""{ "quantities": { "namespace": "sample", "include": "<q.hpp>", "storage": "flaot" } }""",
				out CppGeneratorOptions? _,
				out string? message));

		Assert.Contains("flaot", message!, StringComparison.Ordinal);
	}

	/// <summary>
	/// The options file carries the whole vocabulary in one entry.
	/// </summary>
	[TestMethod]
	public void TheOptionsFileSaysWhereTheVocabularyIs()
	{
		Assert.IsTrue(
			CppGeneratorOptionsFile.TryParse(
				"""{ "quantities": { "namespace": "holo", "include": "<holotype/quantities/quantities.hpp>" } }""",
				out CppGeneratorOptions? options,
				out string? _));

		Assert.AreEqual("holo::Mass", options!.Quantities!.Qualified("Mass"));
		Assert.AreEqual("Float", options.Quantities.Storage);
	}

	/// <summary>
	/// A class of quantities that promises to travel as bytes compiles, assertion and all.
	/// </summary>
	/// <remarks>
	/// The <c>static_assert(std::is_trivially_copyable_v&lt;T&gt;)</c> the generator writes beside
	/// a promising class is what checks the claim, and it is checked by the compiler rather than
	/// by this test - which is the whole reason the generated headers are compiled instead of
	/// merely inspected.
	/// </remarks>
	[TestMethod]
	public void AClassOfQuantitiesTravelsAsBytes()
	{
		Schema schema = new();
		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		body.TravelsAsBytes = true;

		foreach (string quantity in new[] { "Mass", "Ratio", "Heading", "Velocity3D" })
		{
			body.AddMember(quantity.As<MemberName>())!
				.SetType(new Quantity { QuantityName = quantity.As<QuantityName>() });
		}

		Assert.IsEmpty(schema.Validate());

		AssertCompiles(Configured(schema), TargetOptions with { Reflection = true });
	}

	/// <summary>
	/// The reflection table takes a quantity's dimension from the quantity, with no unit written.
	/// </summary>
	/// <remarks>
	/// This is what the quantity adds that a unit never could. The table's eight exponents used to
	/// come from the member's unit text and from nowhere else, so a member that measured something
	/// but named no unit was written down as dimensionless - indistinguishable from a flag. A
	/// <c>Velocity3D</c> is a length over a time because of what it is, so the table says so
	/// whether or not anyone wrote <c>m/s</c> beside it.
	/// </remarks>
	[TestMethod]
	public void TheTableTakesTheDimensionFromTheQuantity()
	{
		Schema schema = new();
		schema.AddClass("Body".As<ClassName>())!.AddMember("Velocity".As<MemberName>())!
			.SetType(new Quantity { QuantityName = "Velocity3D".As<QuantityName>() });

		IReadOnlyDictionary<string, string> files = Generate(Configured(schema), TargetOptions with { Reflection = true });
		string table = files.Single(file => file.Key.Contains("reflection", StringComparison.Ordinal)).Value;

		// length 1, time -1, and nothing on the other six axes.
		Assert.Contains("{ 1, 0, -1, 0, 0, 0, 0, 0 }", table, StringComparison.Ordinal);
	}

	/// <summary>
	/// A schema of one class holding one quantity.
	/// </summary>
	private static Schema Body(Quantity? quantity = null)
	{
		Schema schema = new();

		schema.AddClass("Body".As<ClassName>())!.AddMember("Value".As<MemberName>())!
			.SetType(quantity ?? new Quantity { QuantityName = "Mass".As<QuantityName>() });

		return Configured(schema);
	}

	private static Schema Configured(Schema schema)
	{
		schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!.Namespace = "sample".As<CodeNamespace>();

		return schema;
	}

	private static IReadOnlyDictionary<string, string> Generate(Schema schema, CppGeneratorOptions options) =>
		new CppCodeGenerator(options).Generate(schema, schema.GetCodeGenerator("Cpp".As<CodeGeneratorName>())!);

	/// <summary>
	/// Emits a schema and compiles every header it wrote as one translation unit.
	/// </summary>
	private static void AssertCompiles(Schema schema, CppGeneratorOptions options)
	{
		IReadOnlyDictionary<string, string> files = Generate(schema, options);
		string directory = Path.Join(Path.GetTempPath(), $"schema-quantity-{Guid.NewGuid():N}");

		Directory.CreateDirectory(directory);

		try
		{
			foreach ((string name, string text) in files)
			{
				File.WriteAllText(Path.Join(directory, name), text);
			}

			File.WriteAllText(Path.Join(directory, "quantities.hpp"), Vocabulary);

			string includes = string.Concat(files.Keys.Order(StringComparer.Ordinal)
				.Select(name => $"#include \"{name}\"\n"));

			File.WriteAllText(Path.Join(directory, "all.cpp"), $"{includes}\nint main() {{ return 0; }}\n");

			(int exitCode, string output) = Compile(directory, "all.cpp");

			Assert.AreEqual(0, exitCode, $"the generated C++ should compile:\n{output}");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static (int ExitCode, string Output) Compile(string directory, string file)
	{
		string? compiler = Find("g++") ?? Find("clang++");

		if (compiler is null)
		{
			Assert.Inconclusive("no C++ compiler on PATH, so the generated headers were not compiled.");
		}

		using Process process = new()
		{
			StartInfo = new ProcessStartInfo(compiler!)
			{
				WorkingDirectory = directory,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			},
		};

		foreach (string argument in (string[])["-std=c++20", "-Wall", "-Wextra", "-fsyntax-only", "-I.", file])
		{
			process.StartInfo.ArgumentList.Add(argument);
		}

		process.Start();
		string output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		return (process.ExitCode, output);
	}

	private static string? Find(string executable) =>
		(Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(directory => Path.Join(directory, Path.GetFileName(executable)))
			.FirstOrDefault(File.Exists);
}
