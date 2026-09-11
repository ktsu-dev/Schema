// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using System;
using System.IO;

using Hexa.NET.ImGui;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

/// <summary>
/// The keyboard shortcuts, pressed into a live frame rather than by calling what they invoke.
/// </summary>
/// <remarks>
/// The shortcuts are read from ImGui's own key state during the update callback, so what is being
/// checked here is not only that Ctrl+S saves: it is that the editor sees the key at all, that
/// Ctrl+Shift+S is not also taken as Ctrl+S, and that a shortcut does not fire while the key is
/// going into a text field the user is typing in.
/// </remarks>
[TestClass]
public sealed class ShortcutTests
{
	private EditorHarness harness = null!;
	private AbsoluteDirectoryPath scratchDirectory = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();

		// A real directory, not the in-memory one: SchemaFile saves through System.IO directly.
		scratchDirectory = Path.GetTempPath().As<AbsoluteDirectoryPath>() / $"schema-editor-shortcut-tests-{Guid.NewGuid():N}".As<DirectoryName>();
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
	/// Presses a shortcut and draws the frames that carry out what it started.
	/// </summary>
	private void Press(ImGuiKey key, bool shift = false)
	{
		harness.App.Keyboard.Press(key, ctrl: true, shift: shift);
		harness.App.Step(3);
	}

	/// <summary>
	/// Opens a document and makes one undoable edit, so there is something to undo and something
	/// to lose.
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

	private bool IsShowing(string item) => harness.App.Probe.Matches(item).Count > 0;

	[TestMethod]
	public void CtrlNStartsANewDocument()
	{
		Schema first = new();
		harness.Editor.CurrentSchema = first;

		Press(ImGuiKey.N);

		Assert.IsNotNull(harness.Editor.CurrentSchema);
		Assert.AreNotSame(first, harness.Editor.CurrentSchema);
	}

	[TestMethod]
	public void CtrlOAsksWhichDocumentToOpen()
	{
		harness.Editor.CurrentSchema = new Schema();

		Press(ImGuiKey.O);
		harness.App.Step(3);

		Assert.IsTrue(IsShowing("filesystem-browser/cancel"), "The open browser was not raised.");
	}

	[TestMethod]
	public void CtrlSWritesTheDocument()
	{
		OpenDirtyDocument();
		AbsoluteFilePath path = ScratchFile("shortcut-save.schema.json");
		harness.Editor.CurrentSchemaPath = path;

		Press(ImGuiKey.S);

		Assert.IsTrue(File.Exists(path));
		Assert.IsFalse(harness.Editor.HasUnsavedChanges);
	}

	/// <summary>
	/// Save As has to be recognised before Save, or the shared S would save over the open file
	/// instead of asking where the copy should go.
	/// </summary>
	[TestMethod]
	public void CtrlShiftSAsksWhereToPutTheDocumentEvenWhenItHasAPath()
	{
		harness.Editor.CurrentSchema = new Schema();
		AbsoluteFilePath path = ScratchFile("not-written.schema.json");
		harness.Editor.CurrentSchemaPath = path;

		Press(ImGuiKey.S, shift: true);
		harness.App.Step(3);

		Assert.IsTrue(IsShowing("filesystem-browser/cancel"), "The save browser was not raised.");
		Assert.IsFalse(File.Exists(path), "Save As wrote to the open document's path without asking.");
	}

	[TestMethod]
	public void CtrlZUndoesTheLastEdit()
	{
		Schema schema = OpenDirtyDocument();

		Press(ImGuiKey.Z);

		Assert.IsNull(schema.GetClass("User".As<ClassName>()));
	}

	[TestMethod]
	public void CtrlShiftZRedoesIt()
	{
		Schema schema = OpenDirtyDocument();
		Press(ImGuiKey.Z);

		Press(ImGuiKey.Z, shift: true);

		Assert.IsNotNull(schema.GetClass("User".As<ClassName>()));
	}

	[TestMethod]
	public void CtrlYRedoesIt()
	{
		Schema schema = OpenDirtyDocument();
		Press(ImGuiKey.Z);

		Press(ImGuiKey.Y);

		Assert.IsNotNull(schema.GetClass("User".As<ClassName>()));
	}

	/// <summary>
	/// A shortcut must not fire while the key is going into a field, or typing an N into a class
	/// name would throw the document away.
	/// </summary>
	[TestMethod]
	public void ShortcutsAreIgnoredWhileAFieldIsBeingTypedInto()
	{
		Schema schema = new();
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		harness.Editor.CurrentSchema = schema;
		harness.Editor.EditClass(user);

		harness.Click("field/ClassNameUser");

		Press(ImGuiKey.N);

		Assert.AreSame(schema, harness.Editor.CurrentSchema, "Typing into a field started a new document.");
	}
}
