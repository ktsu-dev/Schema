// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Shows a tooltip whose text is drawn exactly as given.
/// </summary>
/// <remarks>
/// <see cref="ImGui.SetTooltip(string)"/> hands its argument to Dear ImGui as a printf format
/// string, so a description, a path or a validation message carrying a <c>%</c> is formatted
/// rather than shown: <c>%s</c> reads an argument that was never passed, and <c>% o</c> prints a
/// number. Every tooltip showing text the user or the schema supplied goes through here instead,
/// and only a constant string may go to <see cref="ImGui.SetTooltip(string)"/>.
/// </remarks>
internal static class EditorTooltip
{
	/// <summary>
	/// The name the tooltip's text is recorded under for a probe.
	/// </summary>
	internal const string TextProbeName = "tooltip-text";

	/// <summary>
	/// Shows <paramref name="text"/> in a tooltip beside the pointer, with no formatting applied.
	/// </summary>
	/// <param name="text">The text to show.</param>
	internal static void Show(string text)
	{
		ImGui.BeginTooltip();
		ImGui.TextUnformatted(text);
		ImGuiProbes.MarkItem(TextProbeName);
		ImGui.EndTooltip();
	}
}
