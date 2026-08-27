// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using static ktsu.ImGui.Popups.ImGuiPopups;
using ktsu.Semantics.Paths;

using ktsu.Schema.Models.Types;

internal sealed class Popups
{
	[JsonIgnore] private MessageOK PopupMessageOK { get; init; } = new();
	[JsonIgnore] private Prompt PopupPrompt { get; init; } = new();
	[JsonIgnore] private InputString PopupInputString { get; init; } = new();
	[JsonIgnore] private SearchableList<BaseType> PopupTypeList { get; init; } = new();
	[JsonInclude] private FilesystemBrowser PopupFilesystemBrowser { get; init; } = new();
	[JsonIgnore] private Queue<Action> Queue { get; init; } = [];

	internal void OpenMessageOK(string title, string message) =>
		Queue.Enqueue(() => PopupMessageOK.Open(title, message));

	/// <summary>
	/// Opens a prompt offering a choice between several actions, such as save / discard / cancel.
	/// </summary>
	/// <param name="title">The popup title.</param>
	/// <param name="message">The question to put to the user.</param>
	/// <param name="buttons">Button labels mapped to what each one does.</param>
	internal void OpenPrompt(string title, string message, Dictionary<string, Action?> buttons) =>
		Queue.Enqueue(() => PopupPrompt.Open(title, message, buttons));

	internal void OpenInputString(string title, string message, string defaultValue, Action<string> onConfirm) =>
		Queue.Enqueue(() => PopupInputString.Open(title, message, defaultValue, onConfirm));

	internal void OpenBrowserFileOpen(string title, Action<AbsoluteFilePath> onConfirm, string glob = "*") =>
		Queue.Enqueue(() => PopupFilesystemBrowser.FileOpen(title, onConfirm, glob));

	internal void OpenBrowserFileSave(string title, Action<AbsoluteFilePath> onConfirm, string glob = "*") =>
		Queue.Enqueue(() => PopupFilesystemBrowser.FileSave(title, onConfirm, glob));

	internal void OpenBrowserDirectory(string title, Action<AbsoluteDirectoryPath> onConfirm) =>
		Queue.Enqueue(() => PopupFilesystemBrowser.ChooseDirectory(title, onConfirm));

	internal void OpenTypeList(string title, string label, IEnumerable<BaseType> items, BaseType? defaultItem, Action<BaseType> onConfirm) =>
		Queue.Enqueue(() => PopupTypeList.Open(title, label, items, defaultItem, (t) => t.DisplayName, onConfirm));

	internal void Update()
	{
		while (Queue.Count > 0)
		{
			Action action = Queue.Dequeue();
			action();
		}

		PopupTypeList.ShowIfOpen();
		PopupMessageOK.ShowIfOpen();
		PopupPrompt.ShowIfOpen();
		PopupInputString.ShowIfOpen();
		PopupFilesystemBrowser.ShowIfOpen();
	}
}
