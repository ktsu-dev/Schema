// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Text inputs that report a value to commit once per editing session rather than once per frame.
/// </summary>
/// <remarks>
/// ImGui's <c>InputText</c> hands back its buffer on every frame the widget exists, so assigning
/// that buffer straight to the model - as this editor used to for the array container field -
/// writes on every frame whether or not anything changed. That allocates a fresh value sixty
/// times a second, and once edits are undoable it would push an undo entry per frame too.
///
/// These helpers keep the in-progress text in a scratch buffer keyed by the widget's id and report
/// a commit only on the frame the widget deactivates after an actual edit. That is one write, and
/// so one undo entry, per editing session - the user typing a name and then clicking away or
/// pressing enter.
/// </remarks>
internal static class EditField
{
	/// <summary>
	/// In-progress text, keyed by the widget's resolved ImGui id.
	/// </summary>
	/// <remarks>
	/// Keyed by <see cref="ImGui.GetID(string)"/> rather than the label string because rows in a
	/// list legitimately share a label - every member row draws "##Name" - and rely on the
	/// surrounding <c>PushID</c> to tell them apart. Keying on the label alone would let one
	/// row's half-typed text appear in another's field.
	/// </remarks>
	private static readonly Dictionary<uint, string> Buffers = [];

	/// <summary>
	/// Draws a single-line text input bound to a model value.
	/// </summary>
	/// <param name="id">The widget id, which also keys the scratch buffer.</param>
	/// <param name="width">The item width.</param>
	/// <param name="modelValue">The value currently held by the model.</param>
	/// <param name="committed">The value to write, valid only when this returns true.</param>
	/// <param name="maxLength">The maximum length accepted by the input.</param>
	/// <returns>True on the frame the user finished editing with a changed value.</returns>
	internal static bool Text(string id, float width, string modelValue, out string committed, int maxLength = 256)
	{
		uint key = ImGui.GetID(id);
		string buffer = Buffers.TryGetValue(key, out string? inProgress) ? inProgress : modelValue;

		ImGui.SetNextItemWidth(width);
		ImGui.InputText(id, ref buffer, (uint)maxLength);
		Mark(id);

		return Resolve(key, buffer, modelValue, out committed);
	}

	/// <summary>
	/// Draws a text input bound to a numeric model value.
	/// </summary>
	/// <remarks>
	/// Text rather than ImGui's own numeric input, for the reason the rest of this class exists:
	/// <c>InputDouble</c> reports its value every frame, so a range bound edited through it would
	/// push an undo entry per keystroke. Text also lets the field hold what a number cannot -
	/// "-", "0.", an empty box mid-edit - without the model seeing any of it.
	/// <para>
	/// Parsed invariantly, matching how the value is written to the schema file. A machine whose
	/// locale writes a decimal comma would otherwise produce a file another machine reads
	/// differently.
	/// </para>
	/// </remarks>
	/// <param name="id">The widget id, which also keys the scratch buffer.</param>
	/// <param name="width">The item width.</param>
	/// <param name="modelValue">The value currently held by the model.</param>
	/// <param name="committed">The value to write, valid only when this returns true.</param>
	/// <returns>True on the frame the user finished editing with a different, parsable number.</returns>
	internal static bool Number(string id, float width, double modelValue, out double committed)
	{
		committed = modelValue;

		string text = modelValue.ToString(CultureInfo.InvariantCulture);
		if (!Text(id, width, text, out string edited))
		{
			return false;
		}

		// An unparsable value is discarded rather than reported: the field has already been
		// deactivated, so the next frame redraws it from the model and the nonsense disappears.
		if (!double.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
		{
			return false;
		}

		committed = parsed;

		// Compared as the text the field is bound to rather than as two doubles, because only a
		// changed number is worth an undo entry and "1.0" and "1" are different text and the same
		// number. Rendering both and comparing the strings answers that without an exact
		// floating-point comparison, which is brittle enough that the analyzers refuse it.
		return !string.Equals(parsed.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
	}

	/// <summary>
	/// Draws a multi-line text input bound to a model value, for descriptions.
	/// </summary>
	/// <param name="id">The widget id, which also keys the scratch buffer.</param>
	/// <param name="size">The input's size.</param>
	/// <param name="modelValue">The value currently held by the model.</param>
	/// <param name="committed">The value to write, valid only when this returns true.</param>
	/// <param name="maxLength">The maximum length accepted by the input.</param>
	/// <returns>True on the frame the user finished editing with a changed value.</returns>
	internal static bool MultilineText(string id, Vector2 size, string modelValue, out string committed, int maxLength = 4096)
	{
		uint key = ImGui.GetID(id);
		string buffer = Buffers.TryGetValue(key, out string? inProgress) ? inProgress : modelValue;

		ImGui.InputTextMultiline(id, ref buffer, (uint)maxLength, size);
		Mark(id);

		return Resolve(key, buffer, modelValue, out committed);
	}

	/// <summary>
	/// Records where the field was drawn, so a test can click into it.
	/// </summary>
	/// <remarks>
	/// Every one of these is a hidden label - an id beginning with "##" so nothing is drawn beside
	/// the box - which leaves a test no text to find it by. The id without its hashes is what the
	/// caller already named the field, and any surrounding probe scope keeps two rows' fields
	/// apart the same way <c>PushID</c> keeps them apart for ImGui.
	/// </remarks>
	/// <param name="id">The widget id the field was drawn with.</param>
	private static void Mark(string id) => ImGuiProbes.MarkItem("field", id.TrimStart('#'));

	/// <summary>
	/// Decides, from the widget state ImGui reports for the item just drawn, whether the buffer
	/// should be kept for the next frame, discarded, or committed to the model.
	/// </summary>
	private static bool Resolve(uint key, string buffer, string modelValue, out string committed)
	{
		// True only on the frame the item loses focus having actually been edited, which is what
		// makes this one undo entry per editing session.
		if (ImGui.IsItemDeactivatedAfterEdit())
		{
			Buffers.Remove(key);
			committed = buffer;
			return !string.Equals(buffer, modelValue, StringComparison.Ordinal);
		}

		if (ImGui.IsItemActive())
		{
			Buffers[key] = buffer;
		}
		else
		{
			// Not being edited, so the model is the source of truth again. Dropping the buffer
			// means an undo that changes the value is reflected the moment the field is redrawn.
			Buffers.Remove(key);
		}

		committed = modelValue;
		return false;
	}

	/// <summary>
	/// Discards every in-progress edit. Called when the open document is replaced, so a buffer
	/// cannot leak from the previous schema into the new one.
	/// </summary>
	internal static void Reset() => Buffers.Clear();
}
