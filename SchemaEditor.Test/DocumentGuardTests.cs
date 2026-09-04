// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System;
using System.IO;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

/// <summary>
/// The guard that stands between an action and the open document's unsaved work, and the
/// save-then-continue sequence it starts.
/// </summary>
/// <remarks>
/// These run through the prompt as a user does - the popup is drawn into a real frame and the
/// button is clicked by name - because the sequencing being tested is precisely what the popup's
/// callbacks do, and a test that invoked those callbacks directly would not be testing it.
/// </remarks>
[TestClass]
public sealed class DocumentGuardTests
{
	private EditorHarness harness = null!;
	private AbsoluteDirectoryPath scratchDirectory = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();

		// A real directory, not the in-memory one: SchemaFile saves through System.IO directly.
		scratchDirectory = Path.GetTempPath().As<AbsoluteDirectoryPath>() / $"schema-editor-tests-{Guid.NewGuid():N}".As<DirectoryName>();
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

	/// <summary>
	/// A path inside this test's scratch directory.
	/// </summary>
	/// <remarks>
	/// Composed with the '/' operator from ktsu.Semantics.Paths rather than
	/// <c>Path.Combine</c>, which silently discards everything before a rooted segment - so a
	/// name that was not a leaf would put the file somewhere other than the scratch directory,
	/// and so outside what <see cref="StopEditor"/> deletes. A <see cref="FileName"/> cannot be
	/// rooted, which is what makes the composition safe rather than merely intended to be.
	/// </remarks>
	/// <param name="name">The file name.</param>
	private AbsoluteFilePath ScratchFile(string name) => scratchDirectory / name.As<FileName>();

	/// <summary>
	/// Opens a document and makes an edit through the undo service, which is what
	/// <see cref="SchemaEditor.HasUnsavedChanges"/> reads.
	/// </summary>
	private void OpenDirtyDocument()
	{
		Schema schema = new();
		harness.Editor.CurrentSchema = schema;
		harness.Editor.Execute(new DelegateCommand(
			"Add Class",
			() => schema.TryAddClass("User".As<ClassName>()),
			() => schema.RemoveClass("User".As<ClassName>()),
			ChangeType.Insert));

		Assert.IsTrue(harness.Editor.HasUnsavedChanges, "The document should be dirty after an edit.");
	}

	/// <summary>
	/// Advances frames until the prompt has been drawn, then clicks one of its buttons.
	/// </summary>
	/// <remarks>
	/// The extra frames after the button first appears are not padding. A modal sizes itself from
	/// its contents on the frame it appears and is centred on the next, so the rectangle recorded
	/// for a button on its first frame is not where that button ends up. Clicking there hits the
	/// background instead, and the prompt sits unanswered.
	/// </remarks>
	private void AnswerPrompt(string button)
	{
		harness.StepUntil(() => harness.App.Probe.Matches(button).Count > 0, $"the '{button}' button appearing");
		harness.App.Step(3);
		harness.App.Click(button);
		harness.App.Step(2);
	}

	[TestMethod]
	public void AnUnmodifiedDocumentIsDiscardedWithoutAsking()
	{
		harness.Editor.CurrentSchema = new Schema();
		bool proceeded = false;

		harness.Editor.WithUnsavedChangesGuard(() => proceeded = true);

		Assert.IsTrue(proceeded, "Nothing was at risk, so the action should have run straight away.");
	}

	[TestMethod]
	public void AModifiedDocumentIsNotDiscardedWithoutAsking()
	{
		OpenDirtyDocument();
		bool proceeded = false;

		harness.Editor.WithUnsavedChangesGuard(() => proceeded = true);
		harness.App.Step(3);

		Assert.IsFalse(proceeded, "The action ran before the user was asked about unsaved work.");
		Assert.IsTrue(harness.App.Probe.Matches("Discard").Count > 0, "The unsaved-changes prompt was not raised.");
	}

	[TestMethod]
	public void DiscardingRunsTheAction()
	{
		OpenDirtyDocument();
		bool proceeded = false;

		harness.Editor.WithUnsavedChangesGuard(() => proceeded = true);
		AnswerPrompt("Discard");

		Assert.IsTrue(proceeded);
	}

	[TestMethod]
	public void CancellingRunsNeitherTheActionNorNothing()
	{
		OpenDirtyDocument();
		bool proceeded = false;
		bool cancelled = false;

		harness.Editor.WithUnsavedChangesGuard(() => proceeded = true, () => cancelled = true);
		AnswerPrompt("Cancel");

		Assert.IsFalse(proceeded);
		Assert.IsTrue(cancelled);
	}

	[TestMethod]
	public void SavingWritesTheDocumentAndThenRunsTheAction()
	{
		OpenDirtyDocument();
		AbsoluteFilePath path = ScratchFile("saved.schema.json");
		harness.Editor.CurrentSchemaPath = path;
		bool proceeded = false;

		harness.Editor.WithUnsavedChangesGuard(() => proceeded = true);
		AnswerPrompt("Save");

		Assert.IsTrue(File.Exists(path), "The document should have been written before the action ran.");
		Assert.IsTrue(proceeded);
		Assert.IsFalse(harness.Editor.HasUnsavedChanges, "Saving should have cleared the unsaved marker.");
	}

	/// <summary>
	/// A document with no path cannot be saved without asking where to put it, and the file browser
	/// answers over later frames. The continuation must wait for that answer rather than running as
	/// though the save had happened.
	/// </summary>
	[TestMethod]
	public void SavingADocumentWithNoPathDefersTheActionToTheFileBrowser()
	{
		harness.Editor.CurrentSchema = new Schema();
		Assert.AreEqual(string.Empty, harness.Editor.CurrentSchemaPath.ToString());
		bool proceeded = false;

		harness.Editor.SaveThen(() => proceeded = true);
		harness.App.Step(3);

		Assert.IsFalse(proceeded, "The action ran without the document having been saved anywhere.");
	}

	[TestMethod]
	public void SavingADocumentWithAPathRunsTheActionOnceItIsWritten()
	{
		harness.Editor.CurrentSchema = new Schema();
		AbsoluteFilePath path = ScratchFile("direct.schema.json");
		harness.Editor.CurrentSchemaPath = path;
		bool proceeded = false;

		harness.Editor.SaveThen(() => proceeded = true);

		Assert.IsTrue(File.Exists(path));
		Assert.IsTrue(proceeded);
	}

	/// <summary>
	/// A save that fails must not run the continuation: the work it was guarding is still at risk.
	/// </summary>
	[TestMethod]
	public void AFailedSaveDoesNotRunTheAction()
	{
		harness.Editor.CurrentSchema = new Schema();

		// A path whose parent is an existing file rather than a directory cannot be created.
		const string blockerName = "blocker";
		AbsoluteFilePath blocker = ScratchFile(blockerName);
		File.WriteAllText(blocker, "not a directory");
		harness.Editor.CurrentSchemaPath =
			scratchDirectory / blockerName.As<DirectoryName>() / "nested.schema.json".As<FileName>();

		bool proceeded = false;
		harness.Editor.SaveThen(() => proceeded = true);

		Assert.IsFalse(proceeded);
	}

	[TestMethod]
	public void SavingRecordsTheDocumentAsRecentlyUsed()
	{
		harness.Editor.CurrentSchema = new Schema();
		AbsoluteFilePath path = ScratchFile("recent.schema.json");
		harness.Editor.CurrentSchemaPath = path;

		harness.Editor.SaveThen(null);

		Assert.AreEqual(path, harness.Editor.Options.RecentFiles[0]);
	}

	[TestMethod]
	public void OpeningADocumentRecordsItAsRecentlyUsedAndSelectsItsFirstClass()
	{
		Schema source = new();
		source.AddClass("First".As<ClassName>());
		source.AddClass("Second".As<ClassName>());
		AbsoluteFilePath path = ScratchFile("opened.schema.json");
		File.WriteAllText(path, SchemaSerializer.Serialize(source));

		harness.Editor.LoadFrom(path);

		Assert.IsNotNull(harness.Editor.CurrentSchema);
		Assert.AreEqual(path, harness.Editor.CurrentSchemaPath);
		Assert.AreEqual("First", harness.Editor.CurrentClass?.Name.ToString());
		Assert.AreEqual(path, harness.Editor.Options.RecentFiles[0]);
		Assert.AreEqual("opened.schema.json", harness.Editor.DocumentName);
	}

	/// <summary>
	/// A failed open must leave the document that is already open alone, rather than half-replacing
	/// it with nothing.
	/// </summary>
	[TestMethod]
	public void AFailedOpenLeavesTheCurrentDocumentAlone()
	{
		Schema open = new();
		harness.Editor.CurrentSchema = open;
		AbsoluteFilePath path = ScratchFile("kept.schema.json");
		harness.Editor.CurrentSchemaPath = path;

		harness.Editor.LoadFrom(ScratchFile("missing.schema.json"));

		Assert.AreSame(open, harness.Editor.CurrentSchema);
		Assert.AreEqual(path, harness.Editor.CurrentSchemaPath);
	}

	[TestMethod]
	public void ANewDocumentReplacesAnUnmodifiedOne()
	{
		Schema first = new();
		harness.Editor.CurrentSchema = first;

		harness.Editor.New();

		Assert.IsNotNull(harness.Editor.CurrentSchema);
		Assert.AreNotSame(first, harness.Editor.CurrentSchema);
		Assert.AreEqual(string.Empty, harness.Editor.CurrentSchemaPath.ToString());
	}

	[TestMethod]
	public void ANewDocumentAsksBeforeReplacingAModifiedOne()
	{
		OpenDirtyDocument();
		Schema dirty = harness.Editor.CurrentSchema!;

		harness.Editor.New();
		harness.App.Step(3);

		Assert.AreSame(dirty, harness.Editor.CurrentSchema);

		AnswerPrompt("Discard");

		Assert.AreNotSame(dirty, harness.Editor.CurrentSchema);
	}

	[TestMethod]
	public void ClosingIsAllowedWhenNothingWouldBeLost()
	{
		harness.Editor.CurrentSchema = new Schema();

		Assert.IsTrue(harness.Editor.ShouldClose());
	}

	[TestMethod]
	public void ClosingIsRefusedWhileWorkWouldBeLost()
	{
		OpenDirtyDocument();

		Assert.IsFalse(harness.Editor.ShouldClose());
	}

	/// <summary>
	/// The refused close has to raise its prompt from the render loop, since the close callback
	/// returns before another frame is drawn.
	/// </summary>
	[TestMethod]
	public void ARefusedCloseRaisesThePromptOnALaterFrame()
	{
		OpenDirtyDocument();

		Assert.IsFalse(harness.Editor.ShouldClose());
		harness.StepUntil(() => harness.App.Probe.Matches("Discard").Count > 0, "the close prompt appearing");
	}

	/// <summary>
	/// Hitting the close button repeatedly must not stack a prompt per press, which would leave the
	/// user dismissing the same question several times.
	/// </summary>
	[TestMethod]
	public void RepeatedCloseAttemptsRaiseOnlyOnePrompt()
	{
		OpenDirtyDocument();

		Assert.IsFalse(harness.Editor.ShouldClose());
		Assert.IsFalse(harness.Editor.ShouldClose());
		harness.App.Step(3);
		harness.Editor.ProcessCloseRequest();
		harness.App.Step(3);

		Assert.IsFalse(harness.App.Probe.IsAmbiguous("Discard"), "More than one unsaved-changes prompt was raised.");
	}
}
