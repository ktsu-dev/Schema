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
/// Saving through the file browser, which is the only path that answers over later frames.
/// </summary>
/// <remarks>
/// A document with no path cannot be saved without asking where to put it, and the browser answers
/// whenever the user gets round to it. Everything the save was standing in the way of - replacing
/// the document, closing the editor - has to wait for that answer and then happen, which is what
/// the continuation carried through <see cref="SchemaEditor.SaveThen"/> is for. The browser opens
/// on the working directory, so these tests make their scratch directory the working directory
/// rather than clicking their way there.
/// </remarks>
[TestClass]
public sealed class FileBrowserTests
{
	private EditorHarness harness = null!;
	private AbsoluteDirectoryPath scratchDirectory = null!;
	private string previousWorkingDirectory = null!;

	[TestInitialize]
	public void StartEditor()
	{
		// A real directory, not the in-memory one: SchemaFile saves through System.IO directly.
		scratchDirectory = Path.GetTempPath().As<AbsoluteDirectoryPath>() / $"schema-editor-browser-tests-{Guid.NewGuid():N}".As<DirectoryName>();
		Directory.CreateDirectory(scratchDirectory);

		previousWorkingDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(scratchDirectory);

		harness = EditorHarness.Start();
	}

	[TestCleanup]
	public void StopEditor()
	{
		harness.Dispose();
		Directory.SetCurrentDirectory(previousWorkingDirectory);

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
	/// Answers the browser with a file name, which it takes as a file in the directory it is
	/// showing.
	/// </summary>
	private void AnswerBrowserWith(string fileName)
	{
		harness.StepUntil(() => harness.App.Probe.Matches("filesystem-browser/filename").Count > 0, "the save browser appearing");
		harness.TypeInto("filesystem-browser/filename", fileName);
		harness.Click("filesystem-browser/confirm");
		harness.App.Step(2);
	}

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

	[TestMethod]
	public void SavingADocumentWithNoPathWritesItWhereTheBrowserSays()
	{
		OpenDirtyDocument();

		harness.Editor.SaveThen(null);
		AnswerBrowserWith("chosen.schema.json");

		Assert.IsTrue(File.Exists(scratchDirectory / "chosen.schema.json".As<FileName>()), "The document was not written where the browser was pointed.");
		Assert.AreEqual("chosen.schema.json", harness.Editor.DocumentName, "The document did not take the path it was saved to.");
		Assert.IsFalse(harness.Editor.HasUnsavedChanges);
	}

	/// <summary>
	/// The continuation is the whole point of the deferral: what the user was doing when the
	/// unsaved-changes prompt appeared has to happen once the save finally lands.
	/// </summary>
	[TestMethod]
	public void TheActionTheSaveWasGuardingRunsOnceTheBrowserAnswers()
	{
		OpenDirtyDocument();
		bool proceeded = false;

		harness.Editor.SaveThen(() => proceeded = true);
		Assert.IsFalse(proceeded, "The action ran before the browser had been answered.");

		AnswerBrowserWith("guarded.schema.json");

		Assert.IsTrue(proceeded, "The action never ran, though the save succeeded.");
	}

	[TestMethod]
	public void SavingRecordsTheChosenPathAsRecentlyUsed()
	{
		OpenDirtyDocument();

		harness.Editor.SaveThen(null);
		AnswerBrowserWith("recorded.schema.json");

		Assert.AreEqual(scratchDirectory / "recorded.schema.json".As<FileName>(), harness.Editor.Options.RecentFiles[0]);
	}

	/// <summary>
	/// Saving from the unsaved-changes prompt is the case that stacks both deferrals: the prompt
	/// answers on one frame, the browser on a later one, and only then may the document be
	/// replaced.
	/// </summary>
	[TestMethod]
	public void SavingFromTheUnsavedChangesPromptDefersUntilTheBrowserAnswers()
	{
		Schema dirty = OpenDirtyDocument();

		harness.Editor.New();
		harness.Click("prompt/Save");

		Assert.AreSame(dirty, harness.Editor.CurrentSchema, "The document was replaced before it had been saved anywhere.");

		AnswerBrowserWith("stacked.schema.json");

		Assert.AreNotSame(dirty, harness.Editor.CurrentSchema, "The document was never replaced, though the save succeeded.");
		Assert.IsTrue(File.Exists(scratchDirectory / "stacked.schema.json".As<FileName>()));
	}

	/// <summary>
	/// Backing out of the browser leaves the document exactly as it was, rather than half-applying
	/// whatever the save was standing in the way of.
	/// </summary>
	[TestMethod]
	public void CancellingTheBrowserLeavesTheDocumentAlone()
	{
		Schema dirty = OpenDirtyDocument();
		bool proceeded = false;

		harness.Editor.SaveThen(() => proceeded = true);
		harness.StepUntil(() => harness.App.Probe.Matches("filesystem-browser/cancel").Count > 0, "the save browser appearing");
		harness.Click("filesystem-browser/cancel");

		Assert.IsFalse(proceeded);
		Assert.AreSame(dirty, harness.Editor.CurrentSchema);
		Assert.IsTrue(harness.Editor.HasUnsavedChanges);
		Assert.AreEqual(string.Empty, harness.Editor.CurrentSchemaPath.ToString());
	}
}
