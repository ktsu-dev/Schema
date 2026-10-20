// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A path in a schema is relative to the schema file, and an absolute one is refused as a file the
/// model cannot hold rather than thrown out of <see cref="SchemaSerializer.Load(string)"/>.
/// </summary>
[TestClass]
public class AbsolutePathLoadTests
{
	internal const string AbsoluteOutputPath = """
		{ "formatVersion": 1, "codeGenerators": [ { "name": "G", "language": "CSharp", "outputPath": "/tmp/out" } ] }
		""";

	internal const string AbsoluteDataSourceFile = """
		{ "formatVersion": 1, "dataSources": [ { "name": "D", "file": "/tmp/data.json", "className": "C" } ] }
		""";

	[TestMethod]
	public void AnAbsoluteOutputPathIsRefusedAsInvalid()
	{
		SchemaLoadResult result = SchemaSerializer.Load(AbsoluteOutputPath);

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
		Assert.Contains("codeGenerators[0].outputPath", result.Message);
		Assert.Contains("relative to the schema file", result.Message);
	}

	[TestMethod]
	public void AnAbsoluteDataSourceFileIsRefusedAsInvalid()
	{
		SchemaLoadResult result = SchemaSerializer.Load(AbsoluteDataSourceFile);

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
		Assert.Contains("dataSources[0].file", result.Message);
		Assert.Contains("relative to the schema file", result.Message);
	}

	[TestMethod]
	public void ARelativeOutputPathStillLoads()
	{
		SchemaLoadResult result = SchemaSerializer.Load(AbsoluteOutputPath.Replace("/tmp/out", "out", System.StringComparison.Ordinal));

		Assert.AreEqual(SchemaLoadStatus.Success, result.Status, result.Message);
	}
}
