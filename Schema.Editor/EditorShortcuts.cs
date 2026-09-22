// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using System;
using System.Collections.Generic;
using System.Linq;

using ktsu.Keybinding.Core.Contracts;
using ktsu.Keybinding.Core.Models;
using ktsu.Keybinding.Core.Services;

/// <summary>
/// The editor's keyboard shortcuts, held once so that dispatch and the menu labels cannot disagree.
/// </summary>
/// <remarks>
/// <para>
/// The chords used to be written down twice: once as an <c>if</c>/<c>else if</c> chain over ImGui's
/// key state, and again as literal strings beside the menu items ("Ctrl+Shift+S" and friends).
/// Nothing tied the two together, so a chord could be rebound in one place and go on being
/// advertised as the other. <see cref="Bindings"/> is now the only place a chord is written, and
/// <see cref="Label"/> renders the menu's text from the same binding that fires the command.
/// </para>
/// <para>
/// The matching itself is <c>ktsu.Keybinding.Core</c>'s, which this project already referenced and
/// did not use. Its <see cref="Chord"/> equality is insensitive to the order the modifiers are
/// written in and to case, and <see cref="Chord.ToString"/> renders a chord in the same spelling
/// the menu used to hard-code - so adopting it leaves the menu reading exactly as it did.
/// </para>
/// <para>
/// Nothing here is persisted. <see cref="KeybindingService"/> is built over an in-memory
/// <see cref="CommandRegistry"/> and <see cref="ProfileManager"/>, so no repository is involved and
/// the editor never reads or writes a keybinding file.
/// </para>
/// <para>
/// One behaviour did change, deliberately. The old chain tested the modifiers it cared about and
/// ignored the rest, so Ctrl+Shift+N started a new document just as Ctrl+N did, and Ctrl+Alt+O
/// opened one. A chord now has to match exactly, so those no longer fire. The chords that were
/// always meant to be distinguished - Ctrl+S against Ctrl+Shift+S, Ctrl+Z against Ctrl+Shift+Z -
/// still are, and no longer depend on the order the <c>else if</c> arms happen to be written in.
/// </para>
/// </remarks>
internal sealed class EditorShortcuts
{
	/// <summary>Start a new document.</summary>
	internal const string New = "editor.file.new";

	/// <summary>Open an existing document.</summary>
	internal const string Open = "editor.file.open";

	/// <summary>Write the open document to its current path.</summary>
	internal const string Save = "editor.file.save";

	/// <summary>Ask where to write a copy of the open document.</summary>
	internal const string SaveAs = "editor.file.save-as";

	/// <summary>Undo the last edit.</summary>
	internal const string Undo = "editor.edit.undo";

	/// <summary>Redo the last undone edit.</summary>
	internal const string Redo = "editor.edit.redo";

	/// <summary>
	/// Redo, under the other chord that has always done it. Kept as its own command because a
	/// binding holds one chord, and Ctrl+Y is the one the Edit menu advertises.
	/// </summary>
	internal const string RedoAlternate = "editor.edit.redo-alternate";

	private const string ProfileId = "default";

	private static readonly string[] ModifierNotes = ["CTRL", "SHIFT", "ALT", "META"];

	/// <summary>
	/// Every shortcut the editor has, and the only place each chord is written.
	/// </summary>
	private static readonly (string CommandId, string Name, string Chord)[] Bindings =
	[
		(New, "New", "Ctrl+N"),
		(Open, "Open", "Ctrl+O"),
		(Save, "Save", "Ctrl+S"),
		(SaveAs, "Save As", "Ctrl+Shift+S"),
		(Undo, "Undo", "Ctrl+Z"),
		(Redo, "Redo", "Ctrl+Y"),
		(RedoAlternate, "Redo", "Ctrl+Shift+Z"),
	];

	private readonly KeybindingService keybindings;

	/// <summary>
	/// The keys a chord can be built around, each named as <c>ktsu.Keybinding.Core</c> spells it.
	/// Derived from <see cref="Bindings"/> so that adding a shortcut needs no second edit here.
	/// </summary>
	private readonly string[] primaryKeys;

	/// <summary>
	/// Registers the editor's commands and binds each to its chord.
	/// </summary>
	internal EditorShortcuts()
	{
		CommandRegistry commands = new();
		ProfileManager profiles = new();
		keybindings = new KeybindingService(commands, profiles);

		keybindings.CreateProfile(ProfileId, "Default", "The editor's built-in shortcuts");
		keybindings.SetActiveProfile(ProfileId);

		foreach ((string commandId, string name, string chord) in Bindings)
		{
			commands.RegisterCommand(new Command(commandId, name, name, "Editor"));
			keybindings.BindChord(commandId, Chord.Parse(chord));
		}

		primaryKeys = [.. Bindings
			.Select(binding => Chord.Parse(binding.Chord))
			.SelectMany(chord => chord.Notes)
			.Select(note => note.ToString())
			.Where(note => !ModifierNotes.Contains(note, StringComparer.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)];
	}

	/// <summary>
	/// The text the menu shows beside the item that <paramref name="commandId"/> names.
	/// </summary>
	/// <param name="commandId">The command whose chord to render.</param>
	/// <returns>The chord's spelling, or an empty string if the command has no chord bound.</returns>
	internal string Label(string commandId) => keybindings.GetChord(commandId)?.ToString() ?? string.Empty;

	/// <summary>
	/// The command whose chord is being pressed, if any.
	/// </summary>
	/// <param name="ctrl">Whether Control is held.</param>
	/// <param name="shift">Whether Shift is held.</param>
	/// <param name="alt">Whether Alt is held.</param>
	/// <param name="isKeyPressed">
	/// Answers whether the named key was pressed on this frame. Named rather than typed as an ImGui
	/// key so that this can be exercised without a frame to press keys into.
	/// </param>
	/// <returns>The command's id, or <see langword="null"/> if no bound chord matches.</returns>
	/// <remarks>
	/// The modifiers are taken as given and the pressed key is looked for among the keys the
	/// bindings actually use, so a chord is only ever built for a key some shortcut wants. Matching
	/// the result is left to <see cref="IKeybindingService.FindCommandByChord(Chord)"/>, which is
	/// what makes the match exact in the modifiers rather than merely in the ones a branch thought
	/// to test.
	/// </remarks>
	internal string? FindCommand(bool ctrl, bool shift, bool alt, Func<string, bool> isKeyPressed)
	{
		Ensure.NotNull(isKeyPressed);

		foreach (string key in primaryKeys)
		{
			if (!isKeyPressed(key))
			{
				continue;
			}

			List<Note> notes = [];

			if (ctrl)
			{
				notes.Add(new Note("CTRL"));
			}

			if (shift)
			{
				notes.Add(new Note("SHIFT"));
			}

			if (alt)
			{
				notes.Add(new Note("ALT"));
			}

			notes.Add(new Note(key));

			string? commandId = keybindings.FindCommandByChord(new Chord(notes));

			if (commandId is not null)
			{
				return commandId;
			}
		}

		return null;
	}
}
