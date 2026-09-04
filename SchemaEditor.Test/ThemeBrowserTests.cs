// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

/// <summary>
/// Picking a theme from the browser the Theme menu opens.
/// </summary>
/// <remarks>
/// The themes named here are ones near the top of the browser's grid. The grid scrolls, and a card
/// below the fold is recorded by the probe at a position that is clipped away, so clicking it hits
/// the modal behind rather than the card.
/// </remarks>
[TestClass]
public sealed class ThemeBrowserTests
{
	private EditorHarness harness = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();
		harness.Editor.Options.ThemeName = string.Empty;
		harness.Editor.OnStart();
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	[TestMethod]
	public void ChoosingAThemeAppliesIt()
	{
		EditorTheme.OpenBrowser();

		harness.Click("theme-card/Dracula");

		Assert.AreEqual("Dracula", EditorTheme.CurrentName);
	}

	/// <summary>
	/// The choice has to reach the settings, or it is gone at the next launch - which is the whole
	/// point of storing it.
	/// </summary>
	[TestMethod]
	public void ChoosingAThemeRemembersIt()
	{
		EditorTheme.OpenBrowser();

		harness.Click("theme-card/Gruvbox Dark");

		Assert.AreEqual("Gruvbox Dark", harness.Editor.Options.ThemeName);
	}

	/// <summary>
	/// A theme chosen now is the theme applied on the next start, which is the round trip the
	/// setting exists for.
	/// </summary>
	[TestMethod]
	public void ARememberedThemeComesBackOnTheNextStart()
	{
		EditorTheme.OpenBrowser();
		harness.Click("theme-card/Dracula");

		EditorTheme.Apply("Nord");
		Assert.AreEqual("Nord", EditorTheme.CurrentName);

		harness.Editor.OnStart();

		Assert.AreEqual("Dracula", EditorTheme.CurrentName);
	}
}
