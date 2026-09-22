// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The shortcut table: that a chord selects the command it is meant to, and that the menu's label
/// for a command is the chord that actually fires it.
/// </summary>
/// <remarks>
/// <para>
/// The chords used to be written twice - an <c>if</c>/<c>else if</c> chain for dispatch, and
/// literal strings such as "Ctrl+Shift+S" beside the menu items - with nothing holding the two
/// together. <see cref="MenuLabelIsTheChordThatFiresTheCommand"/> is the test that pins the join:
/// it reads the label the File and Edit menus now draw and checks it against the chord the
/// dispatcher matches, so rebinding a shortcut in one place and advertising the other cannot pass.
/// </para>
/// <para>
/// Frameless, like the rest of this suite. <c>EditorShortcuts</c> takes the pressed key as a name
/// rather than an <c>ImGuiKey</c> precisely so the table can be exercised without a rasterizer;
/// that the editor feeds it ImGui's real key state is covered by
/// <c>Schema.Editor.UITests/ShortcutTests</c>, which presses the chords into a live frame.
/// </para>
/// </remarks>
[TestClass]
public sealed class EditorShortcutTests
{
	private EditorShortcuts shortcuts = null!;

	[TestInitialize]
	public void BuildShortcuts() => shortcuts = new EditorShortcuts();

	/// <summary>
	/// Presses a chord: the named key, with the modifiers the chord carries.
	/// </summary>
	private string? Press(string key, bool ctrl = true, bool shift = false, bool alt = false) =>
		shortcuts.FindCommand(ctrl, shift, alt, pressed => string.Equals(pressed, key, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Every command the menus label, against the chord the menu is expected to show for it.
	/// </summary>
	private static IEnumerable<(string CommandId, string Label)> LabelledCommands =>
	[
		(EditorShortcuts.New, "Ctrl+N"),
		(EditorShortcuts.Open, "Ctrl+O"),
		(EditorShortcuts.Save, "Ctrl+S"),
		(EditorShortcuts.SaveAs, "Ctrl+Shift+S"),
		(EditorShortcuts.Undo, "Ctrl+Z"),
		(EditorShortcuts.Redo, "Ctrl+Y"),
	];

	/// <summary>
	/// The label the menu draws is the chord that fires the command, not a string that merely
	/// resembles it. This is the duplication the shortcut table exists to remove.
	/// </summary>
	[TestMethod]
	public void MenuLabelIsTheChordThatFiresTheCommand()
	{
		foreach ((string commandId, string label) in LabelledCommands)
		{
			Assert.AreEqual(label, shortcuts.Label(commandId), $"The menu label for {commandId} is not the chord bound to it.");
		}
	}

	/// <summary>
	/// And the chord the label names is the one that actually selects that command, so the two
	/// cannot drift apart in the other direction either.
	/// </summary>
	[TestMethod]
	public void TheChordOnTheLabelSelectsThatCommand()
	{
		foreach ((string commandId, _) in LabelledCommands)
		{
			string label = shortcuts.Label(commandId);
			string[] notes = label.Split('+');
			string key = notes[^1];

			string? selected = Press(
				key,
				ctrl: notes.Contains("Ctrl", StringComparer.OrdinalIgnoreCase),
				shift: notes.Contains("Shift", StringComparer.OrdinalIgnoreCase),
				alt: notes.Contains("Alt", StringComparer.OrdinalIgnoreCase));

			Assert.AreEqual(commandId, selected, $"Pressing {label}, the chord the menu shows for {commandId}, did not select it.");
		}
	}

	/// <summary>
	/// Save As shares its key with Save, so the Shift has to be what tells them apart. Under the old
	/// chain this held only because the Save As arm was written first.
	/// </summary>
	[TestMethod]
	public void ShiftDistinguishesSaveAsFromSave()
	{
		Assert.AreEqual(EditorShortcuts.Save, Press("S"));
		Assert.AreEqual(EditorShortcuts.SaveAs, Press("S", shift: true));
	}

	/// <summary>
	/// Undo and Redo share Z the same way.
	/// </summary>
	[TestMethod]
	public void ShiftDistinguishesRedoFromUndo()
	{
		Assert.AreEqual(EditorShortcuts.Undo, Press("Z"));
		Assert.AreEqual(EditorShortcuts.RedoAlternate, Press("Z", shift: true));
	}

	/// <summary>
	/// Ctrl+Y redoes as well, which is why Redo has a second binding rather than a second chord.
	/// </summary>
	[TestMethod]
	public void CtrlYIsTheOtherRedo()
	{
		Assert.AreEqual(EditorShortcuts.Redo, Press("Y"));
		Assert.AreEqual("Ctrl+Y", shortcuts.Label(EditorShortcuts.Redo));
	}

	/// <summary>
	/// A chord has to match in every modifier, not only the ones a branch thought to test.
	/// </summary>
	/// <remarks>
	/// This is the one deliberate behaviour change. The old chain asked <c>ctrl &amp;&amp;
	/// IsKeyPressed(N)</c> and said nothing about Shift or Alt, so Ctrl+Shift+N and Ctrl+Alt+N both
	/// started a new document. Neither is a chord the editor advertises, and neither fires now.
	/// </remarks>
	[TestMethod]
	public void AnExtraModifierIsNotTheSameChord()
	{
		Assert.AreEqual(EditorShortcuts.New, Press("N"));
		Assert.IsNull(Press("N", shift: true), "Ctrl+Shift+N is not a chord the editor binds.");
		Assert.IsNull(Press("N", alt: true), "Ctrl+Alt+N is not a chord the editor binds.");
		Assert.IsNull(Press("O", shift: true), "Ctrl+Shift+O is not a chord the editor binds.");
	}

	/// <summary>
	/// A key with no Ctrl is ordinary typing, not a shortcut.
	/// </summary>
	[TestMethod]
	public void AKeyWithoutItsModifierSelectsNothing()
	{
		Assert.IsNull(Press("N", ctrl: false));
		Assert.IsNull(Press("S", ctrl: false));
	}

	/// <summary>
	/// A key no shortcut is built around never selects anything, whatever is held with it.
	/// </summary>
	[TestMethod]
	public void AnUnboundKeySelectsNothing()
	{
		Assert.IsNull(Press("Q"));
		Assert.IsNull(Press("Q", shift: true));
	}

	/// <summary>
	/// No two commands answer to the same chord, or which one fires would come down to the order
	/// they happen to be registered in - the ambiguity the <c>else if</c> chain had to be read
	/// carefully to rule out.
	/// </summary>
	[TestMethod]
	public void NoChordFiresTwoCommands()
	{
		string[] commandIds =
		[
			EditorShortcuts.New, EditorShortcuts.Open, EditorShortcuts.Save, EditorShortcuts.SaveAs,
			EditorShortcuts.Undo, EditorShortcuts.Redo, EditorShortcuts.RedoAlternate,
		];

		List<string> chords = [.. commandIds.Select(shortcuts.Label)];

		CollectionAssert.AllItemsAreUnique(chords, $"Two commands share a chord: {string.Join(", ", chords)}");
		Assert.IsFalse(chords.Any(string.IsNullOrEmpty), "A command reached the menu with no chord bound to it.");
	}
}
