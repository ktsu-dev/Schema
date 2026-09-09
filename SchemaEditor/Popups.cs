// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using static ktsu.ImGui.Popups.ImGuiPopups;
using ktsu.Semantics.Paths;

using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Quantities;

internal sealed class Popups
{
	[JsonIgnore] private MessageOK PopupMessageOK { get; init; } = new();
	[JsonIgnore] private Prompt PopupPrompt { get; init; } = new();
	[JsonIgnore] private InputString PopupInputString { get; init; } = new();
	[JsonIgnore] private SearchableList<BaseType> PopupTypeList { get; init; } = new();
	[JsonIgnore] private SearchableList<IUnit> PopupUnitList { get; init; } = new();
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

	/// <summary>
	/// Opens the type picker, with the type the caller already holds selected.
	/// </summary>
	/// <remarks>
	/// The types are materialised, and the current one resolved to the instance in that list,
	/// because the picker redraws from the same sequence every frame and tracks its selection by
	/// reference. <c>Schema.GetAvailableTypes</c> is lazy and builds fresh instances on each
	/// enumeration, so passing it straight through gives the picker a different object every frame
	/// and nothing ever draws as selected.
	/// </remarks>
	/// <param name="title">The popup title.</param>
	/// <param name="label">The label above the list.</param>
	/// <param name="items">The types that can be chosen from.</param>
	/// <param name="current">The type held now, or null when there is none.</param>
	/// <param name="onConfirm">Applies the chosen type.</param>
	internal void OpenTypeList(string title, string label, IEnumerable<BaseType> items, BaseType? current, Action<BaseType> onConfirm) =>
		Queue.Enqueue(() =>
		{
			List<BaseType> types = [.. items];
			BaseType? selected = types.Find(type => type == current);
			PopupTypeList.Open(title, label, types, selected, (t) => t.DisplayName, onConfirm);
		});

	/// <summary>
	/// Opens the unit picker, with the unit the member already names selected.
	/// </summary>
	/// <remarks>
	/// Searchable rather than a menu because the registry holds close to two hundred units, which
	/// is more than a list is worth scrolling. Each is offered as its name and its symbol
	/// together: the symbol is what a schema usually writes, and the name is what tells two units
	/// sharing a symbol apart.
	/// <para>
	/// The member's unit is text and may be either spelling, or something that resolves to no unit
	/// at all, so the selection is resolved through <see cref="UnitRegistry"/> rather than matched
	/// on the text.
	/// </para>
	/// </remarks>
	/// <param name="title">The popup title.</param>
	/// <param name="label">The label above the list.</param>
	/// <param name="units">The units that can be chosen from.</param>
	/// <param name="current">The unit text the member holds now, or null when it has none.</param>
	/// <param name="onConfirm">Applies the chosen unit. Null clears the member's unit.</param>
	internal void OpenUnitList(string title, string label, IEnumerable<IUnit> units, UnitSymbol? current, Action<IUnit?> onConfirm) =>
		Queue.Enqueue(() =>
		{
			List<IUnit> choices = [.. units];
			IUnit? selected = UnitRegistry.TryResolve(current, out IUnit? resolved, out _) ? resolved : null;
			PopupUnitList.Open(title, label, choices, selected, unit => $"{unit.Name} ({unit.Symbol})", onConfirm!);
		});

	internal void Update()
	{
		while (Queue.Count > 0)
		{
			Action action = Queue.Dequeue();
			action();
		}

		PopupTypeList.ShowIfOpen();
		PopupUnitList.ShowIfOpen();
		PopupMessageOK.ShowIfOpen();
		PopupPrompt.ShowIfOpen();
		PopupInputString.ShowIfOpen();
		PopupFilesystemBrowser.ShowIfOpen();
	}
}
