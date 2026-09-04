// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

/// <summary>
/// Which ktsu.ThemeProvider theme the editor runs under, and where that choice comes from.
/// </summary>
/// <remarks>
/// The editor used to have no theme at all. It wrapped every frame in
/// <c>Theme.FromColor(Palette.Semantic.Primary)</c> - a scoped colour meant for one widget -
/// which tinted the entire interface with the primary colour and left an ordinary button looking
/// the same as one marked with an error.
/// </remarks>
[TestClass]
public sealed class ThemeTests
{
	private EditorHarness harness = null!;

	[TestInitialize]
	public void StartEditor() => harness = EditorHarness.Start();

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	/// <summary>
	/// Settings that name no theme still get one, rather than falling back to unstyled ImGui.
	/// </summary>
	[TestMethod]
	public void SettingsWithNoThemeGetTheDefault()
	{
		EditorTheme.Apply(string.Empty);

		Assert.AreEqual("VSCode Dark", EditorTheme.CurrentName);
	}

	[TestMethod]
	public void ANamedThemeIsApplied()
	{
		EditorTheme.Apply("Nord");

		Assert.AreEqual("Nord", EditorTheme.CurrentName);
	}

	/// <summary>
	/// A theme that has left the registry - renamed upstream, or dropped - must not leave the
	/// editor unstyled, because the name is read from settings written by an older build.
	/// </summary>
	[TestMethod]
	public void AThemeThatIsNoLongerRegisteredFallsBack()
	{
		EditorTheme.Apply("A Theme That Does Not Exist");

		Assert.AreEqual("VSCode Dark", EditorTheme.CurrentName);
	}

	[TestMethod]
	public void TheSavedThemeIsAppliedWhenTheEditorStarts()
	{
		harness.Editor.Options.ThemeName = "Gruvbox Dark";

		harness.Editor.OnStart();

		Assert.AreEqual("Gruvbox Dark", EditorTheme.CurrentName);
	}

	/// <summary>
	/// Starting with whatever the previous test left applied must still end with a theme, which is
	/// the property that stops the blanket-tint approach coming back as "no theme at all".
	/// </summary>
	[TestMethod]
	public void TheEditorAlwaysRunsUnderSomeTheme()
	{
		harness.Editor.Options.ThemeName = string.Empty;

		harness.Editor.OnStart();
		harness.App.Step(3);

		Assert.IsFalse(string.IsNullOrEmpty(EditorTheme.CurrentName), "The editor drew a frame with no theme applied.");
	}
}
