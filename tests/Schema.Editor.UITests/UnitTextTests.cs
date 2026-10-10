// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests;

using Hexa.NET.ImGui;

/// <summary>
/// A unit's negative exponent is drawn in a glyph the editor's font has.
/// </summary>
/// <remarks>
/// The bundled font has every superscript digit but not the superscript minus, so a frequency used
/// to draw as <c>s?¹</c>. These ask the font in a real frame, because that is what the editor asks.
/// </remarks>
[TestClass]
public sealed class UnitTextTests
{
	private WidgetHarness harness = null!;

	[TestInitialize]
	public void StartHarness() => harness = WidgetHarness.Start();

	[TestCleanup]
	public void StopHarness() => harness.Dispose();

	[TestMethod]
	public void ANegativeExponentIsDrawnInAGlyphTheFontHas()
	{
		string? drawn = null;
		bool fontHasDrawnMinus = false;
		harness.Draw = () =>
		{
			drawn = UnitText.ForDisplay("s⁻¹");
			char minus = drawn[1];
			fontHasDrawnMinus = ImGui.GetFont().IsGlyphInFont(minus);
		};
		harness.App.Step(2);

		Assert.IsNotNull(drawn);
		Assert.IsTrue(fontHasDrawnMinus, $"'{drawn}' is drawn with a minus the font has no glyph for.");
	}

	[TestMethod]
	public void TextWithoutASuperscriptMinusIsUnchanged()
	{
		string? drawn = null;
		harness.Draw = () => drawn = UnitText.ForDisplay("m/s");
		harness.App.Step(2);

		Assert.AreEqual("m/s", drawn);
	}
}
