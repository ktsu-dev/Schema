// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Saving a schema somewhere else keeps each relative path pointing at the file it pointed at.
/// </summary>
[TestClass]
public class SchemaPathRebaseTests
{
	private static readonly string Root = Path.Combine(Path.GetTempPath(), "schema-rebase");

	private static AbsoluteFilePath At(params string[] parts) =>
		Path.GetFullPath(Path.Combine([Root, .. parts])).As<AbsoluteFilePath>();

	private static Schema CreateSchema(out DataSource dataSource, out SchemaCodeGenerator generator)
	{
		Schema schema = new();
		dataSource = schema.AddDataSource("Items".As<DataSourceName>())!;
		dataSource.File = "data.json".As<RelativeFilePath>();
		generator = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		generator.OutputPath = "generated".As<RelativeDirectoryPath>();
		schema.SetSourceFile(At("sub", "items.schema.json"));
		return schema;
	}

	[TestMethod]
	public void SavingIntoTheParentKeepsEveryPathOnItsFile()
	{
		Schema schema = CreateSchema(out DataSource dataSource, out SchemaCodeGenerator generator);
		Assert.IsTrue(dataSource.TryResolveFile(out AbsoluteFilePath fileBefore));
		Assert.IsTrue(generator.TryResolveOutputPath(out AbsoluteDirectoryPath outputBefore));

		AbsoluteFilePath moved = At("moved.schema.json");
		SchemaPathRebase rebase = SchemaPathRebase.Plan(schema, moved);
		rebase.Apply();
		schema.SetSourceFile(moved);

		Assert.AreEqual(2, rebase.Count);
		Assert.AreEqual("sub/data.json", dataSource.File.ToString());
		Assert.AreEqual("sub/generated", generator.OutputPath.ToString());
		Assert.IsTrue(dataSource.TryResolveFile(out AbsoluteFilePath fileAfter));
		Assert.IsTrue(generator.TryResolveOutputPath(out AbsoluteDirectoryPath outputAfter));
		Assert.AreEqual(fileBefore.ToString(), fileAfter.ToString());
		Assert.AreEqual(outputBefore.ToString(), outputAfter.ToString());
	}

	[TestMethod]
	public void SavingIntoASiblingClimbsOutOfTheOldDirectory()
	{
		Schema schema = CreateSchema(out DataSource dataSource, out _);

		SchemaPathRebase.Plan(schema, At("other", "items.schema.json")).Apply();

		Assert.AreEqual("../sub/data.json", dataSource.File.ToString());
	}

	[TestMethod]
	public void RevertPutsBackThePathsAsWritten()
	{
		Schema schema = CreateSchema(out DataSource dataSource, out SchemaCodeGenerator generator);

		SchemaPathRebase rebase = SchemaPathRebase.Plan(schema, At("moved.schema.json"));
		rebase.Apply();
		rebase.Revert();

		Assert.AreEqual("data.json", dataSource.File.ToString());
		Assert.AreEqual("generated", generator.OutputPath.ToString());
	}

	[TestMethod]
	public void SavingIntoTheSameDirectoryChangesNothing()
	{
		Schema schema = CreateSchema(out DataSource dataSource, out _);

		SchemaPathRebase rebase = SchemaPathRebase.Plan(schema, At("sub", "renamed.schema.json"));

		Assert.IsTrue(rebase.IsEmpty);
		rebase.Apply();
		Assert.AreEqual("data.json", dataSource.File.ToString());
	}

	[TestMethod]
	public void AnUnanchoredSchemaHasNothingToRebase()
	{
		Schema schema = new();
		DataSource dataSource = schema.AddDataSource("Items".As<DataSourceName>())!;
		dataSource.File = "data.json".As<RelativeFilePath>();

		SchemaPathRebase rebase = SchemaPathRebase.Plan(schema, At("moved.schema.json"));

		Assert.IsTrue(rebase.IsEmpty);
		Assert.AreEqual("data.json", dataSource.File.ToString());
	}

	[TestMethod]
	public void AnEmptyPathStaysEmpty()
	{
		Schema schema = CreateSchema(out DataSource dataSource, out _);
		dataSource.File = new();

		SchemaPathRebase.Plan(schema, At("moved.schema.json")).Apply();

		Assert.AreEqual(string.Empty, dataSource.File.ToString());
	}
}
