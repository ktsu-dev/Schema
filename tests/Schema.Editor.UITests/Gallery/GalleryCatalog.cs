// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests.Gallery;

using System.Collections.Generic;

using ktsu.ImGui.App.Testing;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>Every picture in the editor gallery, in the order the index shows them.</summary>
/// <remarks>
/// Each stage after the first starts with <see cref="GallerySchema"/> open and drives the editor by
/// the same names the other tests click. A new view earns a picture by adding an entry here;
/// nothing else needs to change, because the runner, the index and the workflow all read this list.
/// </remarks>
internal static class GalleryCatalog
{
	/// <summary>The class graph's physics runs this many frames before it is photographed.</summary>
	private const int GraphSettleFrames = 300;

	/// <summary>Gets the entries.</summary>
	internal static IReadOnlyList<GalleryEntry> Entries { get; } =
	[
		new(
			"Starting a schema",
			"The editor opens on whatever was open last, or on nothing. The File menu creates, opens and saves `.schema.json` files and keeps the recent ones to hand.",
			harness =>
			{
				harness.OpenMenu("File");
				Park(harness);
			})
		{
			Crop = _ => new Rectangle(0, 0, 720, 450),
		},
		new(
			"Editing a class",
			"The trees on the left hold the schema's classes, enums, data sources and code generators. Choosing a class opens its members as a grid: each one named, typed and described, and reordered or removed from its row.",
			harness =>
			{
				Open(harness);
				Park(harness);
			}),
		new(
			"Member metadata",
			"Behind each member is what its type cannot say: the unit it is measured in, the range it may take, its default, how it is interpolated, and how it travels over a network.",
			harness =>
			{
				Open(harness);
				harness.Click($"member{GallerySchema.Speed}/ToggleSemantics");
				Park(harness);
			}),
		new(
			"Choosing a type",
			"A member's type is picked from the built-in types and everything the schema declares, searchable by name.",
			harness =>
			{
				Open(harness);
				harness.Click("memberInventory/Type");
				harness.StepUntil(() => harness.IsOnScreen("searchable-list/search"), "the type picker opening");
				Park(harness);
			}),
		new(
			"Choosing a unit",
			"Units come from ktsu.Semantics.Quantities rather than a list kept here, each offered by name and symbol.",
			harness =>
			{
				Open(harness);
				harness.Click($"member{GallerySchema.Speed}/ToggleSemantics");
				harness.Click($"member{GallerySchema.Speed}/Unit");
				harness.TypeInto("searchable-list/search", "PerSecond");
				harness.App.Step(2);
				Park(harness);
			}),
		new(
			"Editing an enum",
			"An enum is a named set of values any class can use as a member's type.",
			harness =>
			{
				Load(harness);
				harness.SelectTree(TreeSchema.EnumsTab);
				harness.Click("BtnRarity");
				Park(harness);
			}),
		new(
			"Code generators",
			"A code generator writes the schema out as source in a target language, into a directory relative to the schema file.",
			harness =>
			{
				Load(harness);
				harness.SelectTree(TreeSchema.CodeGeneratorsTab);
				harness.Click("BtnCSharp");
				Park(harness);
			}),
		new(
			"Class graph",
			"Every class and enum as a node, with an edge for each member that refers to another, laid out by a force simulation.",
			harness =>
			{
				Open(harness);
				harness.Click("main-tab/Class Graph");
				harness.App.Step(GraphSettleFrames);
				harness.Editor.ClassGraph.RequestFitToView();
				Park(harness);
			}),
		new(
			"Diagnostics",
			"The schema is validated as it is edited. The tab carries the counts, and each issue names the element it is about; clicking one selects that element.",
			harness =>
			{
				Schema schema = Open(harness);
				schema.GetClass(GallerySchema.Player.As<ClassName>())!.AddMember("Score".As<MemberName>());
				schema.AddDataSource("Players".As<DataSourceName>());
				Validate(harness);
				harness.Click("main-tab/Diagnostics");
				Park(harness);
			}),
		new(
			"Themes",
			"The editor takes its colours from ktsu.ThemeProvider, and remembers the theme chosen in the browser.",
			harness =>
			{
				Open(harness);
				EditorTheme.OpenBrowser();
				harness.StepUntil(() => harness.IsOnScreen("theme-card/Dracula"), "the theme browser opening");
				harness.App.Step(3);
				Park(harness);
			}),
	];

	/// <summary>Opens the sample schema with its main class selected, as a user would find it.</summary>
	private static Schema Open(EditorHarness harness)
	{
		Schema schema = Load(harness);
		harness.SelectTree(TreeSchema.ClassesTab);
		harness.Click($"Btn{GallerySchema.Player}");
		return schema;
	}

	/// <summary>
	/// Opens the sample schema with nothing selected.
	/// </summary>
	/// <remarks>
	/// For a picture of something other than a class. Opening the class tree first would leave the
	/// probe knowing two rows called <c>BtnRarity</c>, the enum and the item's member of that type,
	/// and a name that matches two rows is one a click refuses to guess between.
	/// </remarks>
	private static Schema Load(EditorHarness harness)
	{
		Schema schema = GallerySchema.Build();
		harness.Editor.CurrentSchema = schema;
		harness.Editor.CurrentSchemaPath = GallerySchema.Path;
		Validate(harness);
		return schema;
	}

	/// <summary>Validates now rather than after the editor's debounce, and draws the result.</summary>
	private static void Validate(EditorHarness harness)
	{
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);
		harness.App.Step(2);
	}

	/// <summary>Moves the pointer off the window, so nothing in the picture is hovered.</summary>
	private static void Park(EditorHarness harness)
	{
		harness.App.Mouse.MoveTo(-100f, -100f);
		harness.App.Step(3);
	}
}
