// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests.Gallery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ktsu.ImGui.App.Testing;

/// <summary>
/// Photographs the editor for <c>docs/gallery</c>: one picture per <see cref="GalleryCatalog"/>
/// entry, and an index that captions them.
/// </summary>
/// <remarks>
/// <para>
/// These run as tests, so every pull request proves each picture can still be staged: a stage that
/// clicks a control which has moved or been renamed fails here rather than at the next regeneration
/// on main. Pictures are written to a temporary directory unless <c>SCHEMA_GALLERY_OUT</c> names
/// one, which is how the gallery workflow, and anyone regenerating by hand, sends them to
/// <c>docs/gallery</c>.
/// </para>
/// <para>
/// Each entry starts its own editor, so a picture never depends on the one taken before it.
/// </para>
/// </remarks>
[TestClass]
public sealed class EditorGallery
{
	/// <summary>The environment variable naming the directory the gallery is written to.</summary>
	internal const string OutputVariable = "SCHEMA_GALLERY_OUT";

	/// <summary>The display the gallery is drawn at: larger than a test's, so the panels have room.</summary>
	internal static readonly HarnessOptions Display = new() { Width = 1440, Height = 900 };

	private static readonly Lazy<string> TemporaryOutput = new(() =>
		Path.Join(Path.GetTempPath(), $"schema-gallery-{Guid.NewGuid():N}"));

	/// <summary>Gets or sets the context the runner reports through.</summary>
	public TestContext TestContext { get; set; } = null!;

	/// <summary>Gets every entry's name, one test case each.</summary>
	public static IEnumerable<object[]> EntryNames => GalleryCatalog.Entries.Select(entry => new object[] { entry.Name });

	/// <summary>Gets the directory pictures are written to.</summary>
	internal static string OutputDirectory =>
		Environment.GetEnvironmentVariable(OutputVariable) is string output && output.Length > 0
			? Path.GetFullPath(output)
			: TemporaryOutput.Value;

	/// <summary>Removes the temporary directory, when pictures went there rather than to a named one.</summary>
	[ClassCleanup]
	public static void DeleteTemporaryOutput()
	{
		if (TemporaryOutput.IsValueCreated && Directory.Exists(TemporaryOutput.Value))
		{
			Directory.Delete(TemporaryOutput.Value, recursive: true);
		}
	}

	[TestMethod]
	[DynamicData(nameof(EntryNames))]
	public void Photograph(string name)
	{
		GalleryEntry entry = GalleryCatalog.Entries.Single(candidate => candidate.Name == name);

		EditField.Reset();
		using EditorHarness harness = EditorHarness.Start(Display);
		try
		{
			Assert.IsTrue(GalleryFonts.Load(), "ImGuiApp's own font could not be found, so the pictures would not look like the editor.");
			harness.App.Mouse.MoveTo(-100f, -100f);
			harness.App.Step(2);

			entry.Stage(harness);
			harness.App.Step(2);

			Bitmap32 frame = harness.App.Target;
			Rectangle region = entry.Crop?.Invoke(harness) ?? new Rectangle(0, 0, frame.Width, frame.Height);
			Bitmap32 picture = Crop(frame, region);

			Directory.CreateDirectory(OutputDirectory);
			string path = Path.Join(OutputDirectory, entry.Slug + ".png");
			picture.SavePng(path);
			TestContext.WriteLine($"Wrote {path} ({picture.Width}x{picture.Height}).");
		}
		finally
		{
			EditField.Reset();
		}
	}

	[TestMethod]
	public void WriteTheIndex()
	{
		string[] slugs = [.. GalleryCatalog.Entries.Select(entry => entry.Slug)];
		Assert.HasCount(slugs.Length, slugs.Distinct(StringComparer.Ordinal), "Two gallery entries would write the same file.");

		Directory.CreateDirectory(OutputDirectory);
		File.WriteAllText(Path.Join(OutputDirectory, "README.md"), GalleryIndex.Render(GalleryCatalog.Entries));
	}

	/// <summary>Copies a rectangle out of a frame, clamped to its edges.</summary>
	internal static Bitmap32 Crop(Bitmap32 source, Rectangle region)
	{
		int minX = Math.Clamp(region.MinX, 0, source.Width);
		int minY = Math.Clamp(region.MinY, 0, source.Height);
		int maxX = Math.Clamp(region.MaxX, minX, source.Width);
		int maxY = Math.Clamp(region.MaxY, minY, source.Height);
		Assert.IsTrue(maxX > minX && maxY > minY, $"The crop {region} leaves nothing of a {source.Width}x{source.Height} frame.");

		Bitmap32 cropped = new(maxX - minX, maxY - minY);
		int rowBytes = cropped.Width * 4;
		for (int y = minY; y < maxY; y++)
		{
			source.Pixels.Slice(((y * source.Width) + minX) * 4, rowBytes)
				.CopyTo(cropped.Pixels.Slice((y - minY) * rowBytes, rowBytes));
		}

		return cropped;
	}
}
