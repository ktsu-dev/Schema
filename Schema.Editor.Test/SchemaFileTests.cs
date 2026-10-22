// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using System;
using System.IO;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Reading and writing the document on disk, and what each failure is reported as.
/// </summary>
/// <remarks>
/// The reason a load failed is not decoration: a file written by a newer build of the library is
/// not a broken file, and the editor titles its message differently for each. Nothing here needs a
/// frame - this is the one part of the editor that was always plain logic.
/// </remarks>
[TestClass]
public sealed class SchemaFileTests
{
	private AbsoluteDirectoryPath scratchDirectory = null!;

	[TestInitialize]
	public void CreateScratchDirectory()
	{
		scratchDirectory = Path.GetTempPath().As<AbsoluteDirectoryPath>() / $"schema-file-tests-{Guid.NewGuid():N}".As<DirectoryName>();
		Directory.CreateDirectory(scratchDirectory);
	}

	[TestCleanup]
	public void DeleteScratchDirectory()
	{
		try
		{
			Directory.Delete(scratchDirectory, recursive: true);
		}
		catch (IOException)
		{
			// A leftover temporary directory is not worth failing a passing test over.
		}
	}

	private AbsoluteFilePath ScratchFile(string name) => scratchDirectory / name.As<FileName>();

	[TestMethod]
	public void ASchemaRoundTripsThroughAFile()
	{
		Schema source = new();
		source.AddClass("User".As<ClassName>());
		AbsoluteFilePath path = ScratchFile("round-trip.schema.json");

		Assert.IsTrue(SchemaFile.TrySave(source, path));
		Assert.IsTrue(SchemaFile.TryLoad(path, out Schema? loaded));

		Assert.IsNotNull(loaded);
		Assert.IsNotNull(loaded.GetClass("User".As<ClassName>()));
	}

	/// <summary>
	/// Saving creates the directories the path names, so a schema can be saved into a folder that
	/// does not exist yet.
	/// </summary>
	[TestMethod]
	public void SavingCreatesTheDirectoryTheFileGoesIn()
	{
		AbsoluteFilePath path = scratchDirectory / "nested".As<DirectoryName>() / "made.schema.json".As<FileName>();

		Assert.IsTrue(SchemaFile.TrySave(new Schema(), path));
		Assert.IsTrue(File.Exists(path));
	}

	/// <summary>
	/// A write that fails partway leaves the previous save as it was. Writing straight over the target
	/// truncated it first, so a full disk or a dropped share destroyed the only saved copy.
	/// </summary>
	[TestMethod]
	public void AFailedWriteLeavesThePreviousSaveIntact()
	{
		AbsoluteFilePath path = ScratchFile("kept.schema.json");
		Schema original = new();
		original.AddClass("User".As<ClassName>());
		Assert.IsTrue(SchemaFile.TrySave(original, path));
		byte[] before = File.ReadAllBytes(path);

		Schema edited = new();
		edited.AddClass("Order".As<ClassName>());
		bool saved = SchemaFile.TrySave(edited, path, static (target, content) =>
		{
			File.WriteAllText(target, content[..(content.Length / 2)]);
			throw new IOException("The disk is full.");
		});

		Assert.IsFalse(saved);
		Assert.AreSequenceEqual(before, File.ReadAllBytes(path));
	}

	/// <summary>
	/// The temporary file a failed save wrote into is removed, so failures do not litter the folder
	/// the schema lives in.
	/// </summary>
	[TestMethod]
	public void AFailedWriteLeavesNoTemporaryFileBehind()
	{
		AbsoluteFilePath path = ScratchFile("tidy.schema.json");

		bool saved = SchemaFile.TrySave(new Schema(), path, static (target, content) =>
		{
			File.WriteAllText(target, content);
			throw new IOException("The share went away.");
		});

		Assert.IsFalse(saved);
		Assert.HasCount(0, Directory.GetFiles(scratchDirectory));
	}

	/// <summary>
	/// A file the user may not write - read-only, or in a directory they do not own - is refused
	/// with <see cref="UnauthorizedAccessException"/>, which is not an <see cref="IOException"/>.
	/// It used to escape the save and close the editor with the work unsaved.
	/// </summary>
	[TestMethod]
	public void AWriteTheUserMayNotMakeFailsAndSaysWhy()
	{
		AbsoluteFilePath path = ScratchFile("locked.schema.json");

		bool saved = SchemaFile.TrySave(new Schema(), path, static (_, _) =>
			throw new UnauthorizedAccessException("Access to the path is denied."), out string? failure);

		Assert.IsFalse(saved);
		Assert.AreEqual("Access to the path is denied.", failure);
		Assert.HasCount(0, Directory.GetFiles(scratchDirectory));
	}

	/// <summary>
	/// A save that succeeds reports no reason, so a caller can show one whenever there is one.
	/// </summary>
	[TestMethod]
	public void ASuccessfulSaveReportsNoFailure()
	{
		Assert.IsTrue(SchemaFile.TrySave(new Schema(), ScratchFile("fine.schema.json"), out string? failure));
		Assert.IsNull(failure);
	}

	/// <summary>
	/// Saving over an existing file replaces its content and leaves only that file in the folder.
	/// </summary>
	[TestMethod]
	public void SavingOverAnExistingFileReplacesIt()
	{
		AbsoluteFilePath path = ScratchFile("replaced.schema.json");
		Schema first = new();
		first.AddClass("User".As<ClassName>());
		Schema second = new();
		second.AddClass("Order".As<ClassName>());

		Assert.IsTrue(SchemaFile.TrySave(first, path));
		Assert.IsTrue(SchemaFile.TrySave(second, path));
		Assert.IsTrue(SchemaFile.TryLoad(path, out Schema? loaded));

		Assert.IsNotNull(loaded);
		Assert.IsNotNull(loaded.GetClass("Order".As<ClassName>()));
		Assert.IsNull(loaded.GetClass("User".As<ClassName>()));
		Assert.HasCount(1, Directory.GetFiles(scratchDirectory));
	}

	[TestMethod]
	public void SavingWithNoPathFails() =>
		Assert.IsFalse(SchemaFile.TrySave(new Schema(), new AbsoluteFilePath()));

	/// <summary>
	/// A path whose parent is an existing file rather than a directory cannot be created, which is
	/// the failure the editor turns into its "failed to save" message.
	/// </summary>
	[TestMethod]
	public void SavingWhereADirectoryCannotBeMadeFails()
	{
		const string blockerName = "blocker";
		File.WriteAllText(ScratchFile(blockerName), "not a directory");
		AbsoluteFilePath path = scratchDirectory / blockerName.As<DirectoryName>() / "nested.schema.json".As<FileName>();

		Assert.IsFalse(SchemaFile.TrySave(new Schema(), path));
	}

	[TestMethod]
	public void LoadingWithNoPathSaysSo()
	{
		SchemaLoadResult result = SchemaFile.Load(new AbsoluteFilePath());

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNull(result.Schema);
	}

	[TestMethod]
	public void LoadingAFileThatIsNotThereSaysSo()
	{
		SchemaLoadResult result = SchemaFile.Load(ScratchFile("absent.schema.json"));

		Assert.IsFalse(result.IsSuccess);
		Assert.IsTrue(result.Message.Contains("does not exist", StringComparison.Ordinal), $"The reason was '{result.Message}'.");
	}

	[TestMethod]
	public void LoadingSomethingThatIsNotASchemaSaysSo()
	{
		AbsoluteFilePath path = ScratchFile("nonsense.schema.json");
		File.WriteAllText(path, "{ this is not json");

		SchemaLoadResult result = SchemaFile.Load(path);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
	}

	/// <summary>
	/// The failing load must not be reported through the out parameter as a schema, or the editor
	/// would replace the open document with nothing.
	/// </summary>
	[TestMethod]
	public void TryLoadReportsFailureWithoutASchema()
	{
		Assert.IsFalse(SchemaFile.TryLoad(ScratchFile("absent.schema.json"), out Schema? schema));
		Assert.IsNull(schema);
	}
}
