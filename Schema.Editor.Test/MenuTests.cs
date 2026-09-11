// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using System;
using System.IO;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

/// <summary>
/// The application menus, driven the way a user reaches them: the menu is opened and the item is
/// clicked.
/// </summary>
/// <remarks>
/// New, Open, Save and Exit have always been reachable two ways - from the File menu and from a
/// keyboard shortcut - and only the second was ever exercised, because a menu item had no name a
/// test could click. So the enabling rules, which live only in the menu, went unchecked: whether
/// Save is offered with no document open, and whether Open Externally is offered with no path.
/// </remarks>
[TestClass]
public sealed class MenuTests
{
	private EditorHarness harness = null!;
	private AbsoluteDirectoryPath scratchDirectory = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();

		// A real directory, not the in-memory one: SchemaFile saves through System.IO directly.
		scratchDirectory = Path.GetTempPath().As<AbsoluteDirectoryPath>() / $"schema-editor-menu-tests-{Guid.NewGuid():N}".As<DirectoryName>();
		Directory.CreateDirectory(scratchDirectory);
	}

	[TestCleanup]
	public void StopEditor()
	{
		harness.Dispose();

		try
		{
			Directory.Delete(scratchDirectory, recursive: true);
		}
		catch (IOException)
		{
			// A leftover temporary directory is not worth failing a passing test over.
		}
	}

	private AbsoluteFilePath ScratchFile(string name) => scratchDirectory / name.As<FileName>();

	/// <summary>
	/// Opens a document and makes an edit through the undo service, which is what the unsaved
	/// marker and the Edit menu both read.
	/// </summary>
	private Schema OpenDirtyDocument()
	{
		Schema schema = new();
		harness.Editor.CurrentSchema = schema;
		harness.Editor.Execute(new DelegateCommand(
			"Add Class",
			() => schema.TryAddClass("User".As<ClassName>()),
			() => schema.RemoveClass("User".As<ClassName>()),
			ChangeType.Insert));

		return schema;
	}

	/// <summary>
	/// Whether a popup the editor raises is on screen, named by the marks its buttons carry.
	/// </summary>
	private bool IsShowing(string item) => harness.App.Probe.Matches(item).Count > 0;

	[TestMethod]
	public void NewFromTheFileMenuReplacesTheDocument()
	{
		Schema first = new();
		harness.Editor.CurrentSchema = first;

		harness.ChooseMenuItem("File", "New");

		Assert.IsNotNull(harness.Editor.CurrentSchema);
		Assert.AreNotSame(first, harness.Editor.CurrentSchema);
	}

	[TestMethod]
	public void NewFromTheFileMenuAsksBeforeDiscardingUnsavedWork()
	{
		Schema dirty = OpenDirtyDocument();

		harness.ChooseMenuItem("File", "New");
		harness.App.Step(3);

		Assert.AreSame(dirty, harness.Editor.CurrentSchema, "The document was replaced without the user being asked.");
		Assert.IsTrue(IsShowing("prompt/Discard"), "The unsaved-changes prompt was not raised.");
	}

	[TestMethod]
	public void OpenFromTheFileMenuRaisesTheFileBrowser()
	{
		harness.Editor.CurrentSchema = new Schema();

		harness.ChooseMenuItem("File", "Open");
		harness.App.Step(4);

		Assert.IsTrue(IsShowing("filesystem-browser/cancel"), "The open browser was not raised.");
	}

	[TestMethod]
	public void SaveFromTheFileMenuWritesTheDocument()
	{
		OpenDirtyDocument();
		AbsoluteFilePath path = ScratchFile("menu-save.schema.json");
		harness.Editor.CurrentSchemaPath = path;

		harness.ChooseMenuItem("File", "Save");

		Assert.IsTrue(File.Exists(path));
		Assert.IsFalse(harness.Editor.HasUnsavedChanges);
	}

	/// <summary>
	/// There is nothing to save with no document open, and the menu says so by disabling the item
	/// rather than by offering one that fails.
	/// </summary>
	[TestMethod]
	public void SaveIsNotOfferedWithNoDocumentOpen()
	{
		Assert.IsNull(harness.Editor.CurrentSchema, "This test is about the menu with no document behind it.");

		harness.ChooseMenuItem("File", "Save");
		harness.App.Step(4);

		Assert.IsFalse(IsShowing("filesystem-browser/cancel"), "A disabled Save asked where to put a document that does not exist.");
	}

	[TestMethod]
	public void SaveAsFromTheFileMenuAsksWhereToPutTheDocument()
	{
		harness.Editor.CurrentSchema = new Schema();

		harness.ChooseMenuItem("File", "Save As...");
		harness.App.Step(4);

		Assert.IsTrue(IsShowing("filesystem-browser/cancel"), "The save browser was not raised.");
	}

	/// <summary>
	/// Opening the schema in whatever the operating system associates with it needs a path, and
	/// the item is disabled until there is one.
	/// </summary>
	/// <remarks>
	/// The two-argument ImGui overload this item used to call takes its flag as the item's checked
	/// state rather than as whether it is enabled, so it stayed clickable with no document open and
	/// handed the shell an empty path, which failed into an error popup.
	/// </remarks>
	[TestMethod]
	public void OpenExternallyIsNotOfferedWithoutAPath()
	{
		Assert.AreEqual(string.Empty, harness.Editor.CurrentSchemaPath.ToString());

		harness.ChooseMenuItem("File", "Open Externally");
		harness.App.Step(4);

		Assert.IsFalse(IsShowing("prompt/OK"), "A disabled Open Externally handed an empty path to the shell.");
	}

	[TestMethod]
	public void ExitFromTheFileMenuAsksAboutUnsavedWork()
	{
		OpenDirtyDocument();

		harness.ChooseMenuItem("File", "Exit");
		harness.App.Step(3);

		Assert.IsTrue(IsShowing("prompt/Discard"), "Exiting offered to throw the document away without asking.");
	}

	[TestMethod]
	public void TheRecentFilesMenuListsADocumentThatIsStillThere()
	{
		AbsoluteFilePath path = ScratchFile("listed.schema.json");
		File.WriteAllText(path, SchemaSerializer.Serialize(new Schema()));
		harness.Editor.Options.RecordRecentFile(path);

		harness.ChooseMenuItem("File", "Open Recent");

		Assert.IsTrue(IsShowing("recent/listed.schema.json"));
	}

	/// <summary>
	/// A file on a drive that is not mounted right now is skipped rather than forgotten, so the
	/// menu lists what can actually be opened and keeps the rest for when it comes back.
	/// </summary>
	[TestMethod]
	public void TheRecentFilesMenuSkipsADocumentThatIsNotThere()
	{
		AbsoluteFilePath missing = ScratchFile("gone.schema.json");
		harness.Editor.Options.RecordRecentFile(missing);

		harness.ChooseMenuItem("File", "Open Recent");

		Assert.IsFalse(IsShowing("recent/gone.schema.json"), "A file that is not on disk was offered.");
		Assert.AreEqual(missing, harness.Editor.Options.RecentFiles[0], "It should still be remembered for when it comes back.");
	}

	[TestMethod]
	public void OpeningARecentFileLoadsIt()
	{
		Schema source = new();
		source.AddClass("Recalled".As<ClassName>());
		AbsoluteFilePath path = ScratchFile("recalled.schema.json");
		File.WriteAllText(path, SchemaSerializer.Serialize(source));
		harness.Editor.Options.RecordRecentFile(path);

		harness.ChooseMenuItem("File", "Open Recent");
		harness.Click("recent/recalled.schema.json");

		Assert.AreEqual(path, harness.Editor.CurrentSchemaPath);
		Assert.AreEqual("Recalled", harness.Editor.CurrentClass?.Name.ToString());
	}

	[TestMethod]
	public void UndoFromTheEditMenuRevertsTheLastEdit()
	{
		Schema schema = OpenDirtyDocument();
		Assert.IsNotNull(schema.GetClass("User".As<ClassName>()));

		harness.ChooseMenuItem("Edit", "Undo");

		Assert.IsNull(schema.GetClass("User".As<ClassName>()));
	}

	[TestMethod]
	public void RedoFromTheEditMenuAppliesTheEditAgain()
	{
		Schema schema = OpenDirtyDocument();
		harness.ChooseMenuItem("Edit", "Undo");

		harness.ChooseMenuItem("Edit", "Redo");

		Assert.IsNotNull(schema.GetClass("User".As<ClassName>()));
	}

	/// <summary>
	/// With nothing on the undo stack there is nothing to undo, and the item is disabled rather
	/// than offered.
	/// </summary>
	[TestMethod]
	public void UndoIsNotOfferedWithNothingToUndo()
	{
		harness.Editor.CurrentSchema = new Schema();
		Assert.IsFalse(harness.Editor.UndoRedo.CanUndo);

		harness.ChooseMenuItem("Edit", "Undo");

		Assert.IsFalse(harness.Editor.UndoRedo.CanRedo, "A disabled Undo ran, leaving something to redo.");
	}
}
