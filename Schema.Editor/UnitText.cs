// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using Hexa.NET.ImGui;

/// <summary>
/// Spells a unit's text in characters the editor's font can draw.
/// </summary>
/// <remarks>
/// <para>
/// Unit symbols come from ktsu.Semantics.Quantities and write a negative exponent with the
/// superscript minus, U+207B: <c>s⁻¹</c>, <c>m·s⁻²</c>. The font ImGuiApp ships, JetBrains Mono
/// Nerd Font, has every superscript digit and the superscript plus but not that one character, so
/// it drew as the fallback glyph and a frequency read <c>s?¹</c>. Widening the glyph ranges cannot
/// help, because the glyph is missing from the font rather than from the ranges.
/// </para>
/// <para>
/// Where the font has no glyph for it, the minus is drawn as a macron, U+00AF, which sits at the
/// height of the superscript digits beside it and is the usual stand-in for a raised minus. This is
/// display only: what is written to the schema file is untouched, and as soon as the font gains the
/// real glyph it is drawn instead.
/// </para>
/// </remarks>
internal static class UnitText
{
	/// <summary>The superscript minus unit symbols use for negative exponents.</summary>
	internal const char SuperscriptMinus = '⁻';

	/// <summary>The raised bar drawn in its place when the font has no superscript minus.</summary>
	internal const char RaisedBar = '¯';

	/// <summary>
	/// Returns <paramref name="text"/> as it should be drawn in the current font.
	/// </summary>
	/// <remarks>Call during a frame: whether the font has the glyph is asked of the font in use.</remarks>
	/// <param name="text">A unit's symbol, or text containing one.</param>
	/// <returns>The text, with the superscript minus replaced when the font cannot draw it.</returns>
	internal static string ForDisplay(string text) =>
		text.Contains(SuperscriptMinus) && !FontHasSuperscriptMinus()
			? text.Replace(SuperscriptMinus, RaisedBar)
			: text;

	private static bool FontHasSuperscriptMinus()
	{
		ImFontPtr font = ImGui.GetFont();
		return !font.IsNull && font.IsGlyphInFont(SuperscriptMinus);
	}
}
