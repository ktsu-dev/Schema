// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the commands the schema CLI offers.
/// </summary>
/// <remarks>
/// These run in-process against <see cref="SchemaCommandLine"/> rather than spawning the tool, so
/// they can assert on what each command actually said as well as the code it returned - which is
/// what a build gating on this will care about.
/// </remarks>
[TestClass]
public class SchemaCommandLineTests
{
	private string workingDirectory = string.Empty;

	[TestInitialize]
	public void CreateWorkingDirectory()
	{
		workingDirectory = Path.Combine(Path.GetTempPath(), $"schema-cli-{Guid.NewGuid():N}");
		Directory.CreateDirectory(workingDirectory);
	}

	[TestCleanup]
	public void RemoveWorkingDirectory()
	{
		if (Directory.Exists(workingDirectory))
		{
			Directory.Delete(workingDirectory, recursive: true);
		}
	}

	private sealed record Result(int ExitCode, string Output, string Error);

	private static Result Run(params string[] args)
	{
		using StringWriter output = new();
		using StringWriter error = new();
		int exitCode = SchemaCommandLine.Run(args, output, error);
		return new(exitCode, output.ToString(), error.ToString());
	}

	private string WriteSchema(string name, string json)
	{
		string path = Path.Combine(workingDirectory, name);
		File.WriteAllText(path, json);
		return path;
	}

	private const string ValidSchema = """
	{
	  "formatVersion": 1,
	  "classes": [
	    {
	      "members": [ { "type": { "TypeName": "String" }, "name": "Id", "description": "" } ],
	      "name": "Item",
	      "description": ""
	    }
	  ],
	  "enums": [],
	  "codeGenerators": [
	    { "outputPath": "out", "language": "csharp", "namespace": "Demo", "name": "CSharp", "description": "" }
	  ],
	  "dataSources": []
	}
	""";

	/// <summary>A schema whose object member references a class that is not there.</summary>
	private const string BrokenSchema = """
	{
	  "formatVersion": 1,
	  "classes": [
	    {
	      "members": [ { "type": { "TypeName": "Object", "className": "Missing" }, "name": "Ref", "description": "" } ],
	      "name": "Holder",
	      "description": ""
	    }
	  ],
	  "enums": [],
	  "codeGenerators": [
	    { "outputPath": "out", "language": "csharp", "namespace": "Demo", "name": "CSharp", "description": "" }
	  ],
	  "dataSources": []
	}
	""";

	// ---- usage and dispatch -------------------------------------------------

	[TestMethod]
	public void TestNoArgumentsIsAFailureAndPrintsUsage()
	{
		Result result = Run();

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Output.Contains("Usage:", StringComparison.Ordinal), result.Output);
	}

	[TestMethod]
	[DataRow("--help")]
	[DataRow("-h")]
	[DataRow("-?")]
	[DataRow("help")]
	public void TestHelpSucceedsAndPrintsUsage(string argument)
	{
		Result result = Run(argument);

		Assert.AreEqual(SchemaCommandLine.Success, result.ExitCode);
		Assert.IsTrue(result.Output.Contains("Usage:", StringComparison.Ordinal), result.Output);
		Assert.IsTrue(result.Output.Contains("csharp", StringComparison.Ordinal), "Usage should name the known languages.");
	}

	[TestMethod]
	public void TestUnknownCommandIsAFailure()
	{
		Result result = Run("frobnicate", "x.schema.json");

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("frobnicate", StringComparison.Ordinal), result.Error);
	}

	[TestMethod]
	public void TestCommandsAreCaseInsensitive()
	{
		string path = WriteSchema("case.schema.json", ValidSchema);
		Assert.AreEqual(SchemaCommandLine.Success, Run("VALIDATE", path).ExitCode);
	}

	// ---- loading ------------------------------------------------------------

	[TestMethod]
	public void TestMissingFileArgumentIsAFailure()
	{
		Result result = Run("validate");

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("No schema file", StringComparison.Ordinal), result.Error);
	}

	[TestMethod]
	public void TestNonexistentFileIsAFailure()
	{
		Result result = Run("validate", Path.Combine(workingDirectory, "nope.schema.json"));

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("does not exist", StringComparison.Ordinal), result.Error);
	}

	[TestMethod]
	public void TestUnreadableSchemaIsAFailure()
	{
		string path = WriteSchema("broken.schema.json", "{ not json");
		Result result = Run("validate", path);

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("Could not open", StringComparison.Ordinal), result.Error);
	}

	[TestMethod]
	public void TestSchemaFromANewerVersionSaysSoRatherThanCallingItCorrupt()
	{
		string path = WriteSchema("future.schema.json", ValidSchema.Replace(
			"\"formatVersion\": 1",
			$"\"formatVersion\": {Models.Schema.CurrentFormatVersion + 1}",
			StringComparison.Ordinal));

		Result result = Run("validate", path);

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("newer version", StringComparison.Ordinal), result.Error);
	}

	// ---- validate -----------------------------------------------------------

	[TestMethod]
	public void TestValidateSucceedsForACleanSchema()
	{
		string path = WriteSchema("clean.schema.json", ValidSchema);
		Result result = Run("validate", path);

		Assert.AreEqual(SchemaCommandLine.Success, result.ExitCode);
		Assert.IsTrue(result.Output.Contains("0 error(s)", StringComparison.Ordinal), result.Output);
	}

	[TestMethod]
	public void TestValidateFailsAndListsIssuesForABrokenSchema()
	{
		string path = WriteSchema("dangling.schema.json", BrokenSchema);
		Result result = Run("validate", path);

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Output.Contains("Holder.Ref", StringComparison.Ordinal), result.Output);
		Assert.IsTrue(result.Output.Contains("unknown class", StringComparison.Ordinal), result.Output);
	}

	[TestMethod]
	public void TestValidateDoesNotFailOnWarningsAlone()
	{
		// A code generator with no output path is a warning; an incomplete schema is still a
		// legitimate work in progress and must not break a build.
		const string warningsOnly = """
		{
		  "formatVersion": 1,
		  "classes": [],
		  "enums": [],
		  "codeGenerators": [ { "outputPath": "", "language": "", "namespace": "", "name": "Unset", "description": "" } ],
		  "dataSources": []
		}
		""";

		string path = WriteSchema("warnings.schema.json", warningsOnly);
		Result result = Run("validate", path);

		Assert.AreEqual(SchemaCommandLine.Success, result.ExitCode);
		Assert.IsTrue(result.Output.Contains("0 error(s)", StringComparison.Ordinal), result.Output);
		Assert.IsFalse(result.Output.Contains("0 error(s), 0 warning(s)", StringComparison.Ordinal), "There should be warnings.");
	}

	// ---- generate -----------------------------------------------------------

	[TestMethod]
	public void TestGenerateWritesTheFiles()
	{
		string path = WriteSchema("gen.schema.json", ValidSchema);
		Result result = Run("generate", path);

		Assert.AreEqual(SchemaCommandLine.Success, result.ExitCode, result.Error);
		Assert.IsTrue(File.Exists(Path.Combine(workingDirectory, "out", "Item.g.cs")), result.Output);
		Assert.IsTrue(result.Output.Contains("CSharp:", StringComparison.Ordinal), result.Output);
	}

	[TestMethod]
	public void TestGenerateRefusesABrokenSchemaAndListsWhy()
	{
		string path = WriteSchema("gen-broken.schema.json", BrokenSchema);
		Result result = Run("generate", path);

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("cannot be generated", StringComparison.Ordinal), result.Error);
		Assert.IsTrue(result.Error.Contains("Holder.Ref", StringComparison.Ordinal), result.Error);
		Assert.IsFalse(Directory.Exists(Path.Combine(workingDirectory, "out")), "Nothing should have been written.");
	}

	[TestMethod]
	public void TestGenerateWithNoGeneratorsDeclaredIsAFailure()
	{
		const string noGenerators = """
		{ "formatVersion": 1, "classes": [], "enums": [], "codeGenerators": [], "dataSources": [] }
		""";

		string path = WriteSchema("none.schema.json", noGenerators);
		Result result = Run("generate", path);

		Assert.AreEqual(SchemaCommandLine.Failure, result.ExitCode);
		Assert.IsTrue(result.Error.Contains("no code generators", StringComparison.Ordinal), result.Error);
	}

	[TestMethod]
	public void TestGeneratorOptionSelectsOneByName()
	{
		string path = WriteSchema("filter.schema.json", ValidSchema);

		Result matched = Run("generate", path, "--generator", "CSharp");
		Assert.AreEqual(SchemaCommandLine.Success, matched.ExitCode, matched.Error);

		Result unmatched = Run("generate", path, "--generator", "Cpp");
		Assert.AreEqual(SchemaCommandLine.Failure, unmatched.ExitCode);
		Assert.IsTrue(unmatched.Error.Contains("no code generator named 'Cpp'", StringComparison.Ordinal), unmatched.Error);
	}

	[TestMethod]
	public void TestOptionsDoNotGetMistakenForTheSchemaPath()
	{
		// The path is the first argument that is not an option, whatever order they arrive in.
		string path = WriteSchema("order.schema.json", ValidSchema);
		Result result = Run("generate", "--generator", "CSharp", path);

		Assert.AreEqual(SchemaCommandLine.Success, result.ExitCode, result.Error);
	}
}
