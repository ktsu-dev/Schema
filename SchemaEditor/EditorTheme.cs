// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using ktsu.ImGui.Styler;
using ktsu.Schema.Models;
using ktsu.ThemeProvider;

/// <summary>
/// The application-wide look, as a ktsu.ThemeProvider theme.
/// </summary>
/// <remarks>
/// <para>
/// A theme is applied once and then left alone. It is not the same thing as
/// <see cref="Theme.FromColor"/>, which pushes one colour around one widget and is what marks an
/// element carrying an error or a warning. The editor used to wrap every frame in
/// <c>FromColor(Palette.Semantic.Primary)</c>, which tinted the whole interface with the primary
/// colour and left an ordinary button indistinguishable from an emphasised one.
/// </para>
/// <para>
/// Separate from <see cref="SchemaEditor"/> so that the registry and its types do not count
/// against that class's coupling budget, which they otherwise push past the analyzer's limit.
/// </para>
/// </remarks>
internal static class EditorTheme
{
	/// <summary>
	/// The theme applied when the settings name none, or name one no longer registered.
	/// </summary>
	private const string DefaultThemeName = "VSCode Dark";

	/// <summary>
	/// Gets the name of the theme currently applied, or empty when none is.
	/// </summary>
	internal static string CurrentName => Theme.CurrentThemeName ?? string.Empty;

	/// <summary>
	/// Applies a theme by name, falling back when the name resolves to nothing.
	/// </summary>
	/// <remarks>
	/// The default is resolved against the registry rather than hard-wired, so a theme leaving
	/// ktsu.ThemeProvider degrades to another dark one. A registry with no themes at all leaves
	/// ImGui's own styling in place, which is a readable neutral rather than something to fail on.
	/// </remarks>
	/// <param name="themeName">The theme to apply, or empty for the default.</param>
	internal static void Apply(string themeName) =>
		Theme.CurrentThemeName = Resolve(themeName) ?? Resolve(DefaultThemeName) ?? FirstDarkThemeName();

	private static string? Resolve(string themeName) =>
		!string.IsNullOrEmpty(themeName) && ThemeRegistry.FindTheme(themeName) is ThemeRegistry.ThemeInfo found
			? found.Name
			: null;

	private static string? FirstDarkThemeName()
	{
		IReadOnlyList<ThemeRegistry.ThemeInfo> dark = ThemeRegistry.DarkThemes;
		return dark.Count > 0 ? dark[0].Name : null;
	}

	/// <summary>
	/// Scopes the colour that marks an element carrying a validation issue.
	/// </summary>
	/// <remarks>
	/// One definition rather than the same severity ternary written out at each of the places an
	/// issue is drawn - the tree row, the diagnostics list, the summary and the member row - so
	/// they cannot drift apart.
	/// </remarks>
	/// <param name="severity">The severity to colour for.</param>
	/// <returns>A scope that reverts the colour when disposed.</returns>
	internal static ScopedThemeColor Severity(SchemaValidationSeverity severity) =>
		severity == SchemaValidationSeverity.Error ? Error() : Warning();

	/// <summary>
	/// Scopes the colour for something that is wrong.
	/// </summary>
	internal static ScopedThemeColor Error() => Theme.FromColor(Palette.Semantic.Error);

	/// <summary>
	/// Scopes the colour for something that needs attention but is not wrong.
	/// </summary>
	internal static ScopedThemeColor Warning() => Theme.FromColor(Palette.Semantic.Warning);

	/// <summary>
	/// Draws the theme menu. Returns true when the user picked a different theme.
	/// </summary>
	internal static bool ShowMenu() => Theme.RenderThemeSelectorMenu();

	/// <summary>
	/// Draws the theme browser if it is open. Returns true when the user picked a different theme.
	/// </summary>
	internal static bool ShowBrowser() => Theme.RenderThemeSelector();
}
