// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Types;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The same definitions as the legacy set, said in the vocabulary the schema has now.
/// </summary>
/// <remarks>
/// <para>
/// The migrated samples beside this one are a transliteration: they say what the old format said,
/// because that is what makes the migration checkable. The cost is that they exercise almost none
/// of the schema - 45 elements of <c>Int</c>, <c>Float</c>, <c>String</c> and <c>Array</c>, and
/// not one unit, range, default, colour, keyed container or promise about representation.
/// </para>
/// <para>
/// This is the same 33 classes and 8 enums with the same members in the same order, rewritten to
/// say what those members always meant: a colour is a colour rather than a string, a weight
/// carries kilograms, a gradient's stops are keyed by the id they already had, and a class that is
/// only fixed-size numbers says so. <c>SameDefinitionsAsTheLegacySet</c> is what keeps the first
/// half of that sentence true while the second half changes.
/// </para>
/// </remarks>
[TestClass]
public sealed class ModernisedSampleTests
{
	/// <summary>
	/// The four classes that stop being classes, because the schema has them built in.
	/// </summary>
	private static readonly string[] NowBuiltIn = ["Vector2", "Vector3", "IntVector2", "IntVector3"];

	/// <summary>
	/// Same classes, same enums, same members, same order.
	/// </summary>
	/// <remarks>
	/// This is the whole claim of the file. Modernising a schema by hand is an opportunity to
	/// quietly drop a member or reorder two, and member order is load-bearing for any class that
	/// travels as bytes - so the correspondence is asserted against the legacy files themselves
	/// rather than against a copy of them.
	/// </remarks>
	[TestMethod]
	public void SameDefinitionsAsTheLegacySet()
	{
		Schema legacy = LegacySchemaReader.Read(AllLegacyDocuments());
		Schema modern = LegacySampleGenerationTests.LoadSample("modernised");

		List<string> expectedClasses =
		[
			.. legacy.Classes.Select(c => (string)c.Name).Where(name => !NowBuiltIn.Contains(name)),
		];

		Assert.AreSequenceEqual(expectedClasses, [.. modern.Classes.Select(c => (string)c.Name)]);
		Assert.AreSequenceEqual(
			[.. legacy.Enums.Select(e => (string)e.Name)],
			[.. modern.Enums.Select(e => (string)e.Name)]);

		foreach (SchemaEnum declared in modern.Enums)
		{
			Assert.AreSequenceEqual(
				[.. legacy.GetEnum(declared.Name)!.Values.Select(v => (string)v)],
				[.. declared.Values.Select(v => (string)v)],
				$"enum {declared.Name} changed its values");
		}

		foreach (SchemaClass declared in modern.Classes)
		{
			Assert.AreSequenceEqual(
				[.. legacy.GetClass(declared.Name)!.Members.Select(m => (string)m.Name)],
				[.. declared.Members.Select(m => (string)m.Name)],
				$"class {declared.Name} changed its members or their order");
		}
	}

	/// <summary>
	/// It says all of it without a complaint, warnings included.
	/// </summary>
	/// <remarks>
	/// Warnings too, not just errors. Most of what this file adds is only wrong in combination -
	/// a quantisation step wider than the range it encodes, a default outside its own bounds, a
	/// unit on something that measures nothing - and every one of those is a warning or an error
	/// that only appears once the metadata is there to be inconsistent.
	/// </remarks>
	[TestMethod]
	public void SaysAllOfItWithoutAComplaint()
	{
		Schema modern = LegacySampleGenerationTests.LoadSample("modernised");

		List<string> issues =
			[.. modern.Validate().Select(issue => $"{issue.Severity}: {issue.Message}")];

		Assert.IsEmpty(issues, string.Join("\n", issues));
	}

	/// <summary>
	/// It uses the vocabulary the legacy set had no way to reach.
	/// </summary>
	/// <remarks>
	/// Without this the file could decay back into a transliteration one edit at a time and every
	/// other test here would still pass. Each of these is something the old format could only put
	/// in a comment.
	/// </remarks>
	[TestMethod]
	public void UsesWhatTheOldFormatCouldNotSay()
	{
		Schema modern = LegacySampleGenerationTests.LoadSample("modernised");

		List<SchemaMember> members = [.. modern.Classes.SelectMany(c => c.Members)];
		List<BaseType> types = [.. members.Select(m => m.Type)];

		Assert.IsNotEmpty(modern.SemanticTypes, "a weight and a price are not bare numbers");
		Assert.IsNotEmpty(modern.SemanticTypes.Where(s => !string.IsNullOrEmpty(s.Unit)), "a unit lives on the type");
		Assert.IsNotEmpty(types.OfType<Semantic>(), "and a member names it");

		Assert.IsNotEmpty(types.OfType<ColorRGB>().Concat<BaseType>(types.OfType<ColorRGBA>()), "a colour was a string");
		Assert.IsNotEmpty(types.OfType<Models.Types.Vector2>(), "a vector was a class with an x and a y");
		Assert.IsNotEmpty(
			types.OfType<Models.Types.Vector2>().Where(v => v.ElementType is Int),
			"and an integer vector was a second class");

		Assert.IsNotEmpty(
			types.OfType<Models.Types.Array>().Where(a => a.IsKeyed),
			"a list of things with ids is a lookup");

		Assert.IsNotEmpty(modern.Classes.Where(c => c.TravelsAsBytes), "some of these are just numbers");

		Assert.IsNotEmpty(members.Where(m => m.Range is not null), "a probability is not any integer");
		Assert.IsNotEmpty(members.Where(m => m.DefaultValue is not null), "friction starts somewhere");
		Assert.IsNotEmpty(members.Where(m => m.Interpolation != Interpolation.None), "an offset blends");
		Assert.IsNotEmpty(members.Where(m => m.Network is not null), "a position is sent often");
		Assert.IsNotEmpty(members.Where(m => !string.IsNullOrEmpty(m.Editor)), "a script is picked from a file");
	}

	/// <summary>
	/// A class that promises to travel as bytes holds only things that can.
	/// </summary>
	/// <remarks>
	/// <see cref="SaysAllOfItWithoutAComplaint"/> would catch a violation, since validation refuses
	/// a <c>String</c> or an <c>Array</c> in a promising class. This says which classes made the
	/// promise, so removing one is a failure rather than a quiet loss of coverage.
	/// </remarks>
	[TestMethod]
	public void TheClassesThatTravelAsBytesAreTheFixedSizeOnes()
	{
		Schema modern = LegacySampleGenerationTests.LoadSample("modernised");

		Assert.AreSequenceEqual(
			(string[])["Line2D", "Line3D", "Rect", "IntRect", "CRXP", "LevelXP", "Proficiency"],
			[.. modern.Classes.Where(c => c.TravelsAsBytes).Select(c => (string)c.Name)]);
	}

	/// <summary>
	/// It generates C# that compiles, and nothing in it falls through to <c>object?</c>.
	/// </summary>
	[TestMethod]
	public void GeneratesCSharpThatCompiles()
	{
		Schema modern = LegacySampleGenerationTests.LoadSample("modernised");
		SchemaCodeGenerator configuration = CodeGenerationTests.ConfigureGenerator(modern);

		IReadOnlyDictionary<string, string> files = new CSharpCodeGenerator().Generate(modern, configuration);

		foreach ((string file, string source) in files)
		{
			Assert.DoesNotContain("object?", source, $"{file} has a member the C# generator could not spell");
		}

		Assembly assembly = GeneratedSourceCompiler.Compile(files);

		Assert.IsNotEmpty(assembly.GetTypes());
	}

	private static List<string> AllLegacyDocuments() =>
	[
		.. Directory.EnumerateFiles(Path.Join(LegacySchemaMigrationTests.SamplesDirectory, "legacy"), "*.schema.json",
				SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.Ordinal)
			.Select(File.ReadAllText),
	];
}
