// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The files this generator writes for itself, which no element may be named after.
/// </summary>
[TestClass]
public sealed class GeneratorOwnFileTests
{
	private static SchemaCodeGenerator Configuration()
	{
		Models.Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		return configuration;
	}

	[TestMethod]
	public void TheReflectionFilesAreReservedWhenReflectionIsOn()
	{
		CppCodeGenerator generator = new(new CppGeneratorOptions { Reflection = true });

		IReadOnlyCollection<string> reserved = generator.ReservedNames(Configuration());

		Assert.Contains("reflect", reserved);
		Assert.Contains("reflection", reserved);
	}

	/// <summary>
	/// Without reflection neither file is written, so an element may take either name.
	/// </summary>
	[TestMethod]
	public void NothingIsReservedWithoutReflection()
	{
		CppCodeGenerator generator = new();

		Assert.IsEmpty(generator.ReservedNames(Configuration()));
	}

	/// <summary>
	/// Each reserved name is a file the generator really writes, so the list cannot drift from the
	/// output.
	/// </summary>
	[TestMethod]
	public void EveryReservedNameIsAFileTheGeneratorWrites()
	{
		CppCodeGenerator generator = new(new CppGeneratorOptions { Reflection = true });
		SchemaCodeGenerator configuration = Configuration();

		string extension = generator.Options.HeaderExtension;
		IEnumerable<string> written = generator.Generate(configuration.ParentSchema!, configuration).Keys
			.Select(file => file.EndsWith(extension, StringComparison.Ordinal) ? file[..^extension.Length] : file);

		foreach (string name in generator.ReservedNames(configuration))
		{
			Assert.Contains(name, written);
		}
	}
}
