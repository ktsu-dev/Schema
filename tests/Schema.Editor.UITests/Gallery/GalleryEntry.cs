// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests.Gallery;

using System;
using System.Text;

using ktsu.ImGui.App.Testing;

/// <summary>One picture in the editor gallery: how to stage it, and what to say about it.</summary>
/// <param name="Name">The caption, which also names the picture's file.</param>
/// <param name="Description">One or two sentences under the picture in the gallery's index.</param>
/// <param name="Stage">
/// Drives the editor into the state worth photographing, starting from the sample schema open and
/// settled. It works the way a user would where it can - menus, rows and buttons clicked by the names
/// the editor records for them - so the picture is of a path through the editor rather than of
/// state set behind its back.
/// </param>
internal sealed record GalleryEntry(string Name, string Description, Action<EditorHarness> Stage)
{
	/// <summary>
	/// Gets the part of the window to keep, or null for all of it. Asked after <see cref="Stage"/>
	/// has run, so it can measure what the stage put on screen.
	/// </summary>
	public Func<EditorHarness, Rectangle?>? Crop { get; init; }

	/// <summary>Gets the file name the picture is written under, without its extension.</summary>
	public string Slug => MakeSlug(Name);

	/// <inheritdoc/>
	public override string ToString() => Name;

	/// <summary>Turns a caption into a lower-case, hyphenated file name.</summary>
	internal static string MakeSlug(string text)
	{
		StringBuilder slug = new(text.Length);
		foreach (char character in text)
		{
			if (char.IsAsciiLetterOrDigit(character))
			{
				slug.Append(char.ToLowerInvariant(character));
			}
			else if (slug.Length > 0 && slug[^1] != '-')
			{
				slug.Append('-');
			}
		}

		return slug.ToString().TrimEnd('-');
	}
}
