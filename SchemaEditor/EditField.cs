// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

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

		return Resolve(key, buffer, modelValue, out committed);
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

		return Resolve(key, buffer, modelValue, out committed);
	}

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
