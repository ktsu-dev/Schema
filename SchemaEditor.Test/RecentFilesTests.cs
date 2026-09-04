// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// The recent-files list. Pure logic - no ImGui context needed - which is why it is the first
/// thing here.
/// </summary>
[TestClass]
public sealed class RecentFilesTests
{
	// AppData is disposable and saves on dispose if a save is outstanding, so even these
	// filesystem-free tests are pointed at an in-memory store rather than the real settings of
	// whoever runs the suite.
	[TestInitialize]
	public void RedirectSettings() =>
		ktsu.AppDataStorage.AppData.ConfigureForTesting(() => new System.IO.Abstractions.TestingHelpers.MockFileSystem());

	[TestCleanup]
	public void RestoreSettings() => ktsu.AppDataStorage.AppData.ResetFileSystem();

	private static AbsoluteFilePath Path(string name) =>
		$"{(OperatingSystem.IsWindows() ? "C:\\schemas\\" : "/schemas/")}{name}".As<AbsoluteFilePath>();

	[TestMethod]
	public void RecordingAFilePutsItFirst()
	{
		using AppData options = new();

		options.RecordRecentFile(Path("a.schema.json"));
		options.RecordRecentFile(Path("b.schema.json"));

		Assert.AreEqual(2, options.RecentFiles.Count);
		Assert.AreEqual(Path("b.schema.json"), options.RecentFiles[0]);
		Assert.AreEqual(Path("a.schema.json"), options.RecentFiles[1]);
	}

	[TestMethod]
	public void RecordingAFileAgainMovesItRatherThanDuplicatingIt()
	{
		using AppData options = new();

		options.RecordRecentFile(Path("a.schema.json"));
		options.RecordRecentFile(Path("b.schema.json"));
		options.RecordRecentFile(Path("a.schema.json"));

		Assert.AreEqual(2, options.RecentFiles.Count);
		Assert.AreEqual(Path("a.schema.json"), options.RecentFiles[0]);
		Assert.AreEqual(Path("b.schema.json"), options.RecentFiles[1]);
	}

	[TestMethod]
	public void RecordingIsBoundedAndDropsTheOldest()
	{
		using AppData options = new();

		for (int index = 0; index < AppData.MaxRecentFiles + 5; index++)
		{
			options.RecordRecentFile(Path($"file{index}.schema.json"));
		}

		Assert.AreEqual(AppData.MaxRecentFiles, options.RecentFiles.Count);

		// Newest first, so the most recently recorded file leads and the oldest survivor is the
		// one recorded MaxRecentFiles ago.
		Assert.AreEqual(Path($"file{AppData.MaxRecentFiles + 4}.schema.json"), options.RecentFiles[0]);
		Assert.AreEqual(Path("file5.schema.json"), options.RecentFiles[^1]);
		Assert.IsFalse(options.RecentFiles.Contains(Path("file0.schema.json")));
	}

	[TestMethod]
	public void RecordingAnEmptyPathIsIgnored()
	{
		using AppData options = new();

		options.RecordRecentFile(new AbsoluteFilePath());

		Assert.AreEqual(0, options.RecentFiles.Count);
	}

	/// <summary>
	/// A list that already holds duplicates - written by an older build, or hand-edited - must not
	/// keep one of them behind when the file is recorded again.
	/// </summary>
	[TestMethod]
	public void RecordingRemovesEveryEarlierCopy()
	{
		using AppData options = new();
		options.RecentFiles.Add(Path("a.schema.json"));
		options.RecentFiles.Add(Path("b.schema.json"));
		options.RecentFiles.Add(Path("a.schema.json"));

		options.RecordRecentFile(Path("a.schema.json"));

		Assert.AreEqual(2, options.RecentFiles.Count);
		Assert.AreEqual(1, options.RecentFiles.Count(p => p == Path("a.schema.json")));
	}
}
