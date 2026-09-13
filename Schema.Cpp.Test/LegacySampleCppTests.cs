// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using System.Diagnostics;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Generates C++ from the migrated breadth samples and compiles it.
/// </summary>
/// <remarks>
/// <para>
/// The exemplar tests next door check that one carefully built schema is spelled the way
/// <c>docs/generated-cpp-target.md</c> says. These check something the exemplar cannot: that 45
/// elements written by people with no interest in this generator produce C++ a compiler accepts.
/// Every header is included from one translation unit, so an include a generated file forgot,
/// a name that collides, or a forward reference in the wrong order fails here and nowhere else -
/// the exemplar has too few types to put any of them in the wrong order.
/// </para>
/// <para>
/// The samples name no vector, colour, handle, fallible return or date, so the options are the
/// defaults: everything they contain is either a primitive, a class, an enum or a sequence, and
/// the standard library spells the ones C++ does not have a keyword for.
/// </para>
/// </remarks>
[TestClass]
public sealed class LegacySampleCppTests
{
	/// <summary>
	/// Carbon Monoxide's vocabulary generates C++ that compiles.
	/// </summary>
	[TestMethod]
	public void TheCarbonMonoxideSampleCompiles() => AssertCompiles("carbonmonoxide");

	/// <summary>
	/// So does Dungeoneer's, with its reflection table.
	/// </summary>
	/// <remarks>
	/// With <c>Reflection</c> on, because the table is where a member's kind and representation
	/// have to be written for all 33 classes - and a member the table cannot describe is a
	/// different failure from one the header cannot declare.
	/// </remarks>
	[TestMethod]
	public void TheDungeoneerSampleCompilesWithItsReflectionTable() =>
		AssertCompiles("dungeoneer", new CppGeneratorOptions { Reflection = true });

	/// <summary>
	/// The modernised sample generates C++ that compiles, vocabulary and all.
	/// </summary>
	/// <remarks>
	/// This is the one that exercises the generator rather than merely feeding it. The two
	/// migrated samples are primitives, classes, enums and sequences; this one adds five semantic
	/// types the generator has to emit as classes, built-in vectors and colours the target has to
	/// spell for itself, keyed containers, and seven classes promising to travel as bytes -- which
	/// is where the <c>static_assert(std::is_trivially_copyable_v&lt;T&gt;)</c> beside each of them
	/// has to be true rather than merely written.
	/// </remarks>
	[TestMethod]
	public void TheModernisedSampleCompiles()
	{
		AssertCompiles("modernised", TargetOptions with { Reflection = true }, SampleTypes);
	}

	/// <summary>
	/// One header per element, and nothing else.
	/// </summary>
	/// <remarks>
	/// A generated file per class and per enum is the rule, so the count is derivable from the
	/// schema rather than from what the generator happened to write. This is what would catch a
	/// class silently skipped - which compiling cannot, since a header nobody emits is a header
	/// nobody includes.
	/// </remarks>
	[TestMethod]
	public void EveryElementGetsItsOwnHeader()
	{
		Schema schema = LoadSample("dungeoneer");
		IReadOnlyDictionary<string, string> files = Generate(schema, new CppGeneratorOptions());

		foreach (SchemaClass declared in schema.Classes)
		{
			Assert.Contains($"{declared.Name}.gen.hpp", files.Keys);
		}

		foreach (SchemaEnum declared in schema.Enums)
		{
			Assert.Contains($"{declared.Name}.gen.hpp", files.Keys);
		}

		Assert.HasCount(schema.Classes.Count + schema.Enums.Count, files);
	}

	/// <summary>
	/// The include for a header beside the generated ones, quoted rather than bracketed.
	/// </summary>
	private const string LocalHeader = "\"sample_types.hpp\"";

	/// <summary>
	/// What a target has to say for itself, since standard C++ has no vector and no colour.
	/// </summary>
	private static CppGeneratorOptions TargetOptions { get; } = new()
	{
		Vector2 = new CppTypeSpelling("sample::Vector2", LocalHeader),
		Vector3 = new CppTypeSpelling("sample::Vector3", LocalHeader),
		Vector4 = new CppTypeSpelling("sample::Vector4", LocalHeader),
		ColorRgb = new CppTypeSpelling("sample::ColorRgb", LocalHeader),
		ColorRgba = new CppTypeSpelling("sample::ColorRgba", LocalHeader),
	};

	/// <summary>
	/// The header those five names come from, written beside the generated ones.
	/// </summary>
	/// <remarks>
	/// Deliberately the smallest thing that can be true: a vector of N components and a colour of
	/// three or four channels, each an aggregate of the element type and nothing else. A promising
	/// class holds these, so if any of them were not trivially copyable the assertion the
	/// generator writes beside that class would fail to compile -- which is the point of compiling
	/// this at all.
	/// </remarks>
	private const string SampleTypes = """
		#pragma once

		namespace sample
		{
			template <typename T> struct Vector2 { T x, y; };
			template <typename T> struct Vector3 { T x, y, z; };
			template <typename T> struct Vector4 { T x, y, z, w; };

			struct ColorRgb { float r, g, b; };
			struct ColorRgba { float r, g, b, a; };
		}
		""";

	/// <summary>
	/// Emits a sample and compiles a translation unit that includes every header it wrote.
	/// </summary>
	private static void AssertCompiles(string sample, CppGeneratorOptions? options = null, string? prelude = null)
	{
		Schema schema = LoadSample(sample);
		IReadOnlyDictionary<string, string> files = Generate(schema, options ?? new CppGeneratorOptions());

		string directory = Path.Join(Path.GetTempPath(), $"schema-breadth-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);

		try
		{
			foreach ((string name, string text) in files)
			{
				File.WriteAllText(Path.Join(directory, name), text);
			}

			if (prelude is not null)
			{
				File.WriteAllText(Path.Join(directory, "sample_types.hpp"), prelude);
			}

			string includes = string.Concat(files.Keys.Order(StringComparer.Ordinal)
				.Select(name => $"#include \"{name}\"\n"));

			File.WriteAllText(Path.Join(directory, "all.cpp"), $"{includes}\nint main() {{ return 0; }}\n");

			(int exitCode, string output) = Compile(directory, "all.cpp");

			Assert.AreEqual(0, exitCode, $"the C++ generated from {sample} should compile:\n{output}");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static IReadOnlyDictionary<string, string> Generate(Schema schema, CppGeneratorOptions options) =>
		new CppCodeGenerator(options).Generate(schema, schema.GetCodeGenerator("Cpp".As<CodeGeneratorName>())!);

	/// <summary>
	/// Loads a committed sample and gives it the generator configuration the C++ writer needs.
	/// </summary>
	private static Schema LoadSample(string sample)
	{
		string json = File.ReadAllText(Path.Join(SamplesDirectory, $"{sample}.schema.json"));

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));

		SchemaCodeGenerator configuration = schema!.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Namespace = sample.As<CodeNamespace>();

		return schema;
	}

	private static string SamplesDirectory { get; } = FindSamples();

	private static string FindSamples()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);

		while (directory is not null)
		{
			string candidate = Path.Join(directory.FullName, "samples");

			if (Directory.Exists(candidate))
			{
				return candidate;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("no 'samples' directory above the test assembly");
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
