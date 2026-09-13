// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Runs the C# generator over the migrated samples and compiles what it writes.
/// </summary>
/// <remarks>
/// This is what the breadth suite is for. Every other generator test names one construct and
/// checks its spelling; these two hand it 45 elements nobody chose for their shape - 33 classes
/// deep in references, arrays of objects, arrays of strings, enums declared in one file and used
/// in another - and require the result to be C# a compiler accepts. A mapping that is wrong only
/// in combination has nowhere to hide here.
/// </remarks>
[TestClass]
public sealed class LegacySampleGenerationTests
{
	/// <summary>
	/// Carbon Monoxide's vocabulary generates and compiles.
	/// </summary>
	[TestMethod]
	public void TheCarbonMonoxideSampleCompiles() => AssertCompiles("carbonmonoxide");

	/// <summary>
	/// So does Dungeoneer's, which is the larger of the two and the one with the depth.
	/// </summary>
	[TestMethod]
	public void TheDungeoneerSampleCompiles() => AssertCompiles("dungeoneer");

	/// <summary>
	/// Nothing in either sample reaches C# as <c>object?</c>.
	/// </summary>
	/// <remarks>
	/// <c>object?</c> is what <c>MapType</c> writes when it has no answer, and it compiles - so a
	/// mapping gap in a 45-element schema would pass <see cref="TheDungeoneerSampleCompiles"/>
	/// without anyone noticing. The samples contain no <c>Span</c> outside a signature and no
	/// <c>Void</c> outside a return type, which are the two declarations that legitimately have
	/// none, so every member here must reach C# as a type.
	/// </remarks>
	[TestMethod]
	public void NoMemberOfEitherSampleFallsThroughToObject()
	{
		foreach (string sample in new[] { "carbonmonoxide", "dungeoneer" })
		{
			foreach ((string file, string source) in GenerateCSharp(sample))
			{
				Assert.DoesNotContain(
					"object?",
					source,
					$"{sample}/{file} has a member the C# generator could not spell");
			}
		}
	}

	/// <summary>
	/// Generates a sample and compiles the result, which is the assertion.
	/// </summary>
	private static void AssertCompiles(string sample)
	{
		IReadOnlyDictionary<string, string> files = GenerateCSharp(sample);

		Assert.IsNotEmpty(files);

		Assembly assembly = GeneratedSourceCompiler.Compile(files);

		Assert.IsNotEmpty(assembly.GetTypes());
	}

	/// <summary>
	/// Loads a committed sample and runs the C# generator over it.
	/// </summary>
	private static IReadOnlyDictionary<string, string> GenerateCSharp(string sample)
	{
		Schema schema = LoadSample(sample);
		SchemaCodeGenerator configuration = CodeGenerationTests.ConfigureGenerator(schema);

		return new CSharpCodeGenerator().Generate(schema, configuration);
	}

	/// <summary>
	/// Loads a committed sample from <c>samples/</c>.
	/// </summary>
	internal static Schema LoadSample(string sample)
	{
		string json = File.ReadAllText(
			Path.Join(LegacySchemaMigrationTests.SamplesDirectory, $"{sample}.schema.json"));

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));

		return schema!;
	}
}
