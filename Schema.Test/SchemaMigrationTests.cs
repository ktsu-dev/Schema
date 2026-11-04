// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers the three parts of migrating bound data between schema versions: the rename history a
/// schema records, the diff read over it, and the data migrated across the diff.
/// </summary>
[TestClass]
public class SchemaMigrationTests
{
	private const string PlayerData = """
		[
			{ "Name": "Ada", "Hp": 10, "Class": "Wizard", "Level": "3", "Notes": "kept", "Home": { "X": 1 } },
			{ "Name": "Bo", "Hp": 7, "Class": "Rogue", "Level": "4.5", "Home": { "X": 2 } }
		]
		""";

	private static Schema CreatePrevious()
	{
		Schema schema = new();

		SchemaEnum job = schema.AddEnum("Job".As<EnumName>())!;
		job.TryAddValue("Wizard".As<EnumValueName>());
		job.TryAddValue("Rogue".As<EnumValueName>());

		SchemaClass place = schema.AddClass("Place".As<ClassName>())!;
		place.AddMember("X".As<MemberName>())!.SetType(new SchemaTypes.Int());

		SchemaClass player = schema.AddClass("Player".As<ClassName>())!;
		player.AddMember("Name".As<MemberName>())!.SetType(new SchemaTypes.String());
		player.AddMember("Hp".As<MemberName>())!.SetType(new SchemaTypes.Int());
		player.AddMember("Class".As<MemberName>())!.SetType(new SchemaTypes.Enum { EnumName = "Job".As<EnumName>() });
		player.AddMember("Level".As<MemberName>())!.SetType(new SchemaTypes.String());
		player.AddMember("Home".As<MemberName>())!.SetType(new SchemaTypes.Object { ClassName = "Place".As<ClassName>() });
		return schema;
	}

	private static Schema Copy(Schema schema) => SchemaSerializer.Load(SchemaSerializer.Serialize(schema)).Schema!;

	/// <summary>
	/// The previous version with every kind of change applied: a class, member and enum value
	/// renamed, a member retyped, one removed and one added with a default.
	/// </summary>
	private static Schema CreateCurrent(Schema previous)
	{
		Schema schema = Copy(previous);

		SchemaClass player = schema.GetClass("Player".As<ClassName>())!;
		Assert.IsTrue(schema.TryRenameClass(player, "Hero".As<ClassName>()));
		Assert.IsTrue(player.TryRenameMember(player.Members.First(m => m.Name == "Hp"), "Health".As<MemberName>()));
		Assert.IsTrue(player.TryRenameMember(player.Members.First(m => m.Name == "Class"), "Job".As<MemberName>()));
		player.Members.First(m => m.Name == "Level").SetType(new SchemaTypes.Int());
		Assert.IsTrue(player.RemoveMember("Name".As<MemberName>()));
		SchemaMember gold = player.AddMember("Gold".As<MemberName>())!;
		gold.SetType(new SchemaTypes.Int());
		gold.DefaultValue = new NumberDefault { Value = 50 };

		SchemaEnum job = schema.GetEnum("Job".As<EnumName>())!;
		Assert.IsTrue(job.TryRenameValue("Wizard".As<EnumValueName>(), "Mage".As<EnumValueName>()));
		return schema;
	}

	[TestMethod]
	public void RenamesAreRecordedInTheOrderTheyWereMade()
	{
		Schema current = CreateCurrent(CreatePrevious());

		string[] recorded = [.. current.RenameHistory.Select(rename => rename.ToString())];

		Assert.HasCount(4, recorded, string.Join("; ", recorded));
		Assert.AreEqual(SchemaRenameKind.Class, current.RenameHistory[0].Kind);
		Assert.AreEqual(SchemaRenameKind.EnumValue, current.RenameHistory[3].Kind);
		Assert.AreEqual("Hero", current.RenameHistory[1].Owner, "A member rename names its class as it was called at the time.");
	}

	[TestMethod]
	public void RenamingBackUndoesTheRenameRatherThanRecordingTwo()
	{
		Schema schema = CreatePrevious();
		SchemaClass player = schema.GetClass("Player".As<ClassName>())!;

		schema.TryRenameClass(player, "Hero".As<ClassName>());
		schema.TryRenameClass(player, "Player".As<ClassName>());

		Assert.HasCount(0, schema.RenameHistory);
	}

	[TestMethod]
	public void AnEmptyHistoryIsLeftOutOfTheFile()
	{
		string json = SchemaSerializer.Serialize(CreatePrevious());

		Assert.DoesNotContain("\"renames\"", json);
	}

	[TestMethod]
	public void TheHistorySurvivesARoundTrip()
	{
		Schema current = CreateCurrent(CreatePrevious());

		Schema reloaded = Copy(current);

		Assert.AreSequenceEqual(current.RenameHistory, reloaded.RenameHistory);
	}

	[TestMethod]
	public void TheDiffReportsEveryChangeThatAffectsData()
	{
		Schema previous = CreatePrevious();
		SchemaDiff diff = SchemaDiff.Compare(previous, CreateCurrent(previous));

		SchemaChangeKind[] kinds = [.. diff.Changes.Select(change => change.Kind).Order()];
		SchemaChangeKind[] expected =
		[
			SchemaChangeKind.ClassRenamed,
			SchemaChangeKind.MemberRenamed,
			SchemaChangeKind.MemberRenamed,
			SchemaChangeKind.MemberRemoved,
			SchemaChangeKind.MemberAdded,
			SchemaChangeKind.MemberRetyped,
			SchemaChangeKind.EnumValueRenamed,
		];

		Assert.AreSequenceEqual(expected.Order(), kinds, string.Join("; ", diff.Changes));
		Assert.IsTrue(diff.HasDataChanges);
		Assert.HasCount(0, diff.Warnings);
	}

	[TestMethod]
	public void ARenameIsNotReportedAsARemovalAndAnAddition()
	{
		Schema previous = CreatePrevious();
		SchemaDiff diff = SchemaDiff.Compare(previous, CreateCurrent(previous));

		Assert.IsFalse(diff.Changes.Any(change => change.Kind == SchemaChangeKind.ClassRemoved));
		Assert.IsFalse(diff.Changes.Any(change => change.Kind == SchemaChangeKind.EnumValueRemoved));
	}

	[TestMethod]
	public void TwoIdenticalVersionsHaveNoChanges()
	{
		Schema previous = CreatePrevious();

		SchemaDiff diff = SchemaDiff.Compare(previous, Copy(previous));

		Assert.HasCount(0, diff.Changes);
		Assert.IsFalse(diff.HasDataChanges);
	}

	[TestMethod]
	public void AHistoryThatDivergedIsWarnedAbout()
	{
		Schema previous = CreatePrevious();
		Schema current = Copy(previous);
		previous.TryRenameClass(previous.GetClass("Place".As<ClassName>())!, "Town".As<ClassName>());

		SchemaDiff diff = SchemaDiff.Compare(previous, current);

		Assert.HasCount(1, diff.Warnings);
	}

	[TestMethod]
	public void MigratedDataValidatesCleanAgainstTheCurrentSchema()
	{
		Schema previous = CreatePrevious();
		Schema current = CreateCurrent(previous);
		SchemaDiff diff = SchemaDiff.Compare(previous, current);

		SchemaMigrationResult result = SchemaDataMigrator.Migrate(diff, previous.GetClass("Player".As<ClassName>())!, PlayerData, "players");

		Assert.IsTrue(result.Succeeded, string.Join("; ", result.Issues));
		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateData(current.GetClass("Hero".As<ClassName>())!, result.Json, "players");
		Assert.IsFalse(issues.Any(issue => issue.Severity == SchemaValidationSeverity.Error), string.Join("; ", issues));
	}

	[TestMethod]
	public void MigrationCarriesRenamedValuesAndConvertsRetypedOnes()
	{
		Schema previous = CreatePrevious();
		SchemaDiff diff = SchemaDiff.Compare(previous, CreateCurrent(previous));

		SchemaMigrationResult result = SchemaDataMigrator.Migrate(diff, previous.GetClass("Player".As<ClassName>())!, PlayerData, "players");
		JsonArray players = JsonNode.Parse(result.Json)!.AsArray();
		JsonObject ada = players[0]!.AsObject();
		JsonObject bo = players[1]!.AsObject();

		Assert.AreEqual(10, ada["Health"]!.GetValue<int>());
		Assert.AreEqual("Mage", ada["Job"]!.GetValue<string>(), "A renamed enum value is rewritten.");
		Assert.AreEqual("Rogue", bo["Job"]!.GetValue<string>());
		Assert.AreEqual(3, ada["Level"]!.GetValue<int>(), "A whole number written as text converts.");
		Assert.AreEqual(0, bo["Level"]!.GetValue<int>(), "A fraction cannot become an Int, so it takes the default.");
		Assert.AreEqual(50, ada["Gold"]!.GetValue<int>(), "An added member takes its declared default.");
		Assert.IsFalse(ada.ContainsKey("Name"), "A removed member's value is dropped.");
		Assert.AreEqual("kept", ada["Notes"]!.GetValue<string>(), "A property neither version models is left alone.");
		Assert.AreEqual(1, ada["Home"]!["X"]!.GetValue<int>());
	}

	[TestMethod]
	public void EveryValueGivenUpIsReported()
	{
		Schema previous = CreatePrevious();
		SchemaDiff diff = SchemaDiff.Compare(previous, CreateCurrent(previous));

		SchemaMigrationResult result = SchemaDataMigrator.Migrate(diff, previous.GetClass("Player".As<ClassName>())!, PlayerData, "players");
		string[] paths = [.. result.Issues.Select(issue => issue.Path).Order(StringComparer.Ordinal)];
		string[] expected = ["players[0].Gold", "players[0].Name", "players[1].Gold", "players[1].Level", "players[1].Name"];

		Assert.AreSequenceEqual(expected, paths, string.Join("; ", result.Issues));
	}

	[TestMethod]
	public void DataBoundToARemovedClassIsRefused()
	{
		Schema previous = CreatePrevious();
		Schema current = Copy(previous);
		Assert.IsTrue(current.GetClass("Place".As<ClassName>())!.TryRemove());
		SchemaDiff diff = SchemaDiff.Compare(previous, current);

		SchemaMigrationResult result = SchemaDataMigrator.Migrate(diff, previous.GetClass("Place".As<ClassName>())!, """{ "X": 1 }""", "place");

		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual("""{ "X": 1 }""", result.Json);
	}

	[TestMethod]
	public void AKeyedMapIsRekeyedByTheMigratedKey()
	{
		Schema previous = new();
		SchemaClass item = previous.AddClass("Item".As<ClassName>())!;
		item.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.String());
		SchemaClass bag = previous.AddClass("Bag".As<ClassName>())!;
		bag.AddMember("Items".As<MemberName>())!.SetType(new SchemaTypes.Array
		{
			ElementType = new SchemaTypes.Object { ClassName = "Item".As<ClassName>() },
			Container = SchemaTypes.Array.MapContainer.As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		});

		Schema current = Copy(previous);
		SchemaClass currentItem = current.GetClass("Item".As<ClassName>())!;
		Assert.IsTrue(currentItem.TryRenameMember(currentItem.Members[0], "Key".As<MemberName>()));
		SchemaDiff diff = SchemaDiff.Compare(previous, current);

		SchemaMigrationResult result = SchemaDataMigrator.Migrate(diff, bag, """{ "Items": { "a": { "Id": "a" } } }""", "bag");

		Assert.AreEqual("a", JsonNode.Parse(result.Json)!["Items"]!["a"]!["Key"]!.GetValue<string>(), result.Json);
		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateData(current.GetClass("Bag".As<ClassName>())!, result.Json, "bag");
		Assert.HasCount(0, issues, string.Join("; ", issues));
	}
}
