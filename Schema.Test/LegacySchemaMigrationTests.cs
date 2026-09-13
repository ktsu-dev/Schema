// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Types;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The breadth suite: 26 schemas written in a previous format, migrated and kept as samples.
/// </summary>
/// <remarks>
/// <para>
/// Every other test in this repository builds a schema to exercise one rule. These are the
/// opposite - two real programs' whole vocabulary, 45 elements, written by people who had never
/// heard of this library and were not trying to be interesting. What they are for is breadth: a
/// generator that handles a hand-built five-member class and falls over on <c>Monster</c> has a
/// bug that no rule-shaped test would have found.
/// </para>
/// <para>
/// The legacy files are committed beside the migrated ones, and the migration is re-run here
/// rather than trusted, so the samples cannot drift from the inputs they came from.
/// </para>
/// </remarks>
[TestClass]
public sealed class LegacySchemaMigrationTests
{
	/// <summary>
	/// The Carbon Monoxide files Dungeoneer's schemas are built against.
	/// </summary>
	/// <remarks>
	/// Six of the eight. Dungeoneer names <c>Vector2</c>, <c>Rect</c>, <c>Line2D</c>,
	/// <c>Anchor</c>, <c>CollisionResponse</c> and <c>Hitbox</c>, and each of those lives in one
	/// of these; <c>gradient</c> and <c>spline</c> it never mentions.
	/// <see cref="EverySharedFileIsOneDungeoneerActuallyNeeds"/> is what keeps this list honest.
	/// </remarks>
	private static readonly string[] SharedFiles =
		["anchor", "collision", "hitbox", "line", "rect", "vector"];

	/// <summary>
	/// Every legacy file parses, and the whole set resolves against itself.
	/// </summary>
	/// <remarks>
	/// The set resolving is the part worth asserting. The legacy format had a per-file reference
	/// list and it was never load-bearing - two spellings across two files, and a third file
	/// naming a type while declaring no reference at all - so nothing checked that the names in
	/// one file could be found in another. The C++ it generated simply included everything.
	/// </remarks>
	[TestMethod]
	public void TheWholeLegacySetResolvesAgainstItself()
	{
		Schema schema = LegacySchemaReader.Read(AllLegacyDocuments());

		Assert.HasCount(37, schema.Classes);
		Assert.HasCount(8, schema.Enums);
		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// Carbon Monoxide's schemas stand on their own.
	/// </summary>
	[TestMethod]
	public void CarbonMonoxideMigratesToTheCommittedSample()
	{
		AssertMatchesSample("carbonmonoxide", LegacySchemaReader.Read(LegacyDocuments("carbonmonoxide")));
	}

	/// <summary>
	/// Dungeoneer's do not, and the sample carries what they are built against.
	/// </summary>
	/// <remarks>
	/// This is the one thing the migration cannot preserve. A <c>className</c> names a class in
	/// the same schema, so a set of files that referred to each other becomes one schema or
	/// nothing - and seven of Dungeoneer's references reach into Carbon Monoxide.
	/// </remarks>
	[TestMethod]
	public void DungeoneerMigratesToTheCommittedSample()
	{
		AssertMatchesSample("dungeoneer", LegacySchemaReader.Read(DungeoneerDocuments()));
	}

	/// <summary>
	/// Each shared file is one Dungeoneer would not resolve without.
	/// </summary>
	/// <remarks>
	/// Without this, <see cref="SharedFiles"/> could quietly become "all of Carbon Monoxide" and
	/// the sample would stop saying anything about what Dungeoneer actually depends on. Dropping
	/// any one of the six has to break it.
	/// </remarks>
	[TestMethod]
	public void EverySharedFileIsOneDungeoneerActuallyNeeds()
	{
		foreach (string omitted in SharedFiles)
		{
			List<string> documents =
			[
				.. LegacyDocuments("dungeoneer"),
				.. SharedFiles.Where(file => file != omitted)
					.Select(file => File.ReadAllText(LegacyPath("carbonmonoxide", file))),
			];

			Assert.ThrowsExactly<InvalidDataException>(
				() => LegacySchemaReader.Read(documents),
				$"dropping '{omitted}' left Dungeoneer resolvable, so it is not one of its dependencies");
		}
	}

	/// <summary>
	/// The breadth is the point, so it is asserted rather than assumed.
	/// </summary>
	/// <remarks>
	/// These are the shapes the 26 files actually contain, counted from the migrated schema. A
	/// change that made one of them stop appearing would leave the suite still passing and no
	/// longer covering what it was added to cover.
	/// </remarks>
	[TestMethod]
	public void TheSuiteCoversTheShapesItWasAddedFor()
	{
		Schema schema = LegacySchemaReader.Read(AllLegacyDocuments());

		List<BaseType> types =
		[
			.. schema.Classes.SelectMany(schemaClass => schemaClass.Members).Select(member => member.Type),
		];

		Assert.IsNotEmpty(types.OfType<Models.Types.String>());
		Assert.IsNotEmpty(types.OfType<Int>());
		Assert.IsNotEmpty(types.OfType<Float>());
		Assert.IsNotEmpty(types.OfType<Bool>());
		Assert.IsNotEmpty(types.OfType<Models.Types.Object>());
		Assert.IsNotEmpty(types.OfType<Models.Types.Enum>());

		List<Models.Types.Array> arrays = [.. types.OfType<Models.Types.Array>()];

		Assert.IsNotEmpty(arrays);
		Assert.IsNotEmpty(arrays.Where(array => array.ElementType is Models.Types.String));
		Assert.IsNotEmpty(arrays.Where(array => array.ElementType is Models.Types.Object));
	}

	/// <summary>
	/// Compares a migrated schema against its committed sample, byte for byte as serialized.
	/// </summary>
	private static void AssertMatchesSample(string name, Schema migrated)
	{
		string expected = File.ReadAllText(SamplePath(name)).ReplaceLineEndings("\n");
		string actual = SchemaSerializer.Serialize(migrated).ReplaceLineEndings("\n");

		Assert.AreEqual(
			expected,
			actual,
			$"samples/{name}.schema.json no longer matches what the legacy files migrate to.");
	}

	/// <summary>
	/// Dungeoneer's own files, plus the Carbon Monoxide ones they are built against.
	/// </summary>
	private static List<string> DungeoneerDocuments() =>
	[
		.. LegacyDocuments("dungeoneer"),
		.. SharedFiles.Select(file => File.ReadAllText(LegacyPath("carbonmonoxide", file))),
	];

	private static List<string> AllLegacyDocuments() =>
		[.. LegacyDocuments("carbonmonoxide"), .. LegacyDocuments("dungeoneer")];

	private static List<string> LegacyDocuments(string project) =>
	[
		.. Directory.EnumerateFiles(Path.Join(SamplesDirectory, "legacy", project), "*.schema.json")
			.OrderBy(path => path, StringComparer.Ordinal)
			.Select(File.ReadAllText),
	];

	private static string LegacyPath(string project, string file) =>
		Path.Join(SamplesDirectory, "legacy", project, $"{file}.schema.json");

	private static string SamplePath(string name) =>
		Path.Join(SamplesDirectory, $"{name}.schema.json");

	/// <summary>
	/// The samples directory, found from the test assembly rather than the working directory.
	/// </summary>
	internal static string SamplesDirectory { get; } = FindSamples();

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

	private static List<string> Errors(Schema schema) =>
		[.. schema.Validate().Where(issue => issue.Severity == SchemaValidationSeverity.Error).Select(issue => issue.Message)];
}
