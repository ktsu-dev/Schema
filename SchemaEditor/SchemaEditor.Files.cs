// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.SchemaEditor;

using System;
using System.Collections.Generic;
using System.IO;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.Schema.Models;
using ktsu.Semantics.Paths;

/// <summary>
/// Opening, saving and closing the schema document, and the unsaved-changes guard around each.
/// </summary>
public partial class SchemaEditor
{
	/// <summary>
	/// Gets a value indicating whether the open schema has edits that have not been saved.
	/// </summary>
	/// <remarks>
	/// The undo service already tracked this - <see cref="Save"/> has always called
	/// <c>MarkAsSaved</c> - but nothing read it, so New and Open threw unsaved work away without
	/// asking.
	/// </remarks>
	internal bool HasUnsavedChanges => CurrentSchema is not null && UndoRedo.HasUnsavedChanges;

	/// <summary>
	/// Gets the name of the open document for display, without its directory.
	/// </summary>
	internal string DocumentName =>
		string.IsNullOrEmpty(CurrentSchemaPath)
			? "Untitled schema"
			: Path.GetFileName(CurrentSchemaPath);

	/// <summary>
	/// Draws the open document's name and unsaved marker, plus the validation summary, at the end
	/// of the application menu bar.
	/// </summary>
	/// <remarks>
	/// This is where the window title would normally carry the document name and a dirty marker.
	/// <c>ImGuiAppConfig.Title</c> is init-only and ImGuiApp reads it once when it creates the
	/// window, and the window itself is internal to that package, so the title cannot be changed
	/// after startup from here. The menu bar is always visible, so the same information lives
	/// there instead.
	/// </remarks>
	private void ShowDocumentStatus()
	{
		if (CurrentSchema is null)
		{
			return;
		}

		ImGui.Separator();
		ImGui.TextUnformatted($"{DocumentName}{(HasUnsavedChanges ? "*" : string.Empty)}");

		ShowValidationSummary();
	}

	private void ShowRecentFilesMenu()
	{
		IReadOnlyList<AbsoluteFilePath> recent = [.. Options.RecentFiles];

		if (!ImGui.BeginMenu("Open Recent", recent.Count > 0))
		{
			return;
		}

		bool anyShown = false;
		foreach (AbsoluteFilePath path in recent)
		{
			// Skipped rather than pruned: a file on a drive that is not mounted right now should
			// come back when it is, instead of being forgotten.
			if (!File.Exists(path))
			{
				continue;
			}

			anyShown = true;
			if (ImGui.MenuItem(path))
			{
				AbsoluteFilePath captured = path;
				WithUnsavedChangesGuard(() => LoadFrom(captured));
			}
		}

		if (!anyShown)
		{
			ImGui.TextDisabled("No recent files are available.");
		}

		ImGui.EndMenu();
	}

	/// <summary>
	/// Runs an action that would discard the open document, asking what to do first if it has
	/// unsaved changes.
	/// </summary>
	/// <param name="proceed">What to do once it is safe to discard the document.</param>
	private void WithUnsavedChangesGuard(Action proceed)
	{
		if (!HasUnsavedChanges)
		{
			proceed();
			return;
		}

		Popups.OpenPrompt(
			"Unsaved Changes",
			$"{DocumentName} has unsaved changes.",
			new Dictionary<string, Action?>
			{
				["Save"] = () => SaveThen(proceed),
				["Discard"] = proceed,
				["Cancel"] = null,
			});
	}

	private void New() => WithUnsavedChangesGuard(NewInternal);

	private void NewInternal()
	{
		Reset();
		CurrentSchema = new Schema();
		RequestValidation();
		QueueSaveOptions();
	}

	private void Open() => WithUnsavedChangesGuard(OpenInternal);

	private void OpenInternal() =>
		Popups.OpenBrowserFileOpen("Open Schema", LoadFrom, "*.schema.json");

	private void LoadFrom(AbsoluteFilePath filePath)
	{
		SchemaLoadResult result = SchemaFile.Load(filePath);

		if (!result.IsSuccess || result.Schema is null)
		{
			// The reason matters: a file from a newer build of the library is not a broken file,
			// and telling the user it is would send them looking for the wrong problem.
			string title = result.Status == SchemaLoadStatus.UnsupportedFutureVersion
				? "Schema Is Too New"
				: "Error";

			Popups.OpenMessageOK(title, $"Could not open '{filePath}'.\n\n{result.Message}");
			return;
		}

		Reset();
		CurrentSchema = result.Schema;
		CurrentSchemaPath = filePath;
		CurrentClass = CurrentSchema.FirstClass;
		Options.RecordRecentFile(filePath);
		RequestValidation();
		QueueSaveOptions();
	}

	private void Reset()
	{
		CurrentSchema = null;
		CurrentSchemaPath = new();
		ClearSelection();
		UndoRedo.Clear();

		// Any half-typed field belonged to the document being closed.
		EditField.Reset();
		Diagnostics = [];
	}

	private void Save() => SaveThen(null);

	/// <summary>
	/// Saves the schema, then runs a continuation if the save succeeded.
	/// </summary>
	/// <remarks>
	/// The continuation exists because saving may need to ask for a path first, and the file
	/// browser answers asynchronously. Whatever the user was doing when the unsaved-changes prompt
	/// appeared has to wait for that answer rather than running immediately.
	/// </remarks>
	/// <param name="continuation">What to do after a successful save, if anything.</param>
	private void SaveThen(Action? continuation)
	{
		if (string.IsNullOrEmpty(CurrentSchemaPath))
		{
			SaveAs(continuation);
			return;
		}

		if (SaveToCurrentPath())
		{
			continuation?.Invoke();
		}
	}

	private void SaveAs(Action? continuation = null) =>
		Popups.OpenBrowserFileSave("Save Schema", (filePath) =>
		{
			CurrentSchemaPath = filePath;
			if (SaveToCurrentPath())
			{
				continuation?.Invoke();
			}
		}, "*.schema.json");

	private bool SaveToCurrentPath()
	{
		if (CurrentSchema is null || !SchemaFile.TrySave(CurrentSchema, CurrentSchemaPath))
		{
			Popups.OpenMessageOK("Error", $"Failed to save schema to '{CurrentSchemaPath}'.");
			return false;
		}

		UndoRedo.MarkAsSaved();
		Options.RecordRecentFile(CurrentSchemaPath);
		QueueSaveOptions();
		return true;
	}

	/// <summary>
	/// Quits, asking about unsaved changes first.
	/// </summary>
	/// <remarks>
	/// ImGuiApp exposes no cancellable close hook, so clicking the window's own close button
	/// cannot be intercepted from here. This menu item is the route out that can ask first.
	/// </remarks>
	private void ExitWithUnsavedChangesGuard() =>
		WithUnsavedChangesGuard(() =>
		{
			SaveOptionsInternal();
			ImGuiApp.Stop();
		});
}
