// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System;
using System.IO;
using System.Linq;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// The code generator panel: configuring one, and running it.
/// </summary>
/// <remarks>
/// Until this panel existed a code generator could be created and deleted but never configured, so
/// every one in a saved schema tripped the "does not specify an output path" warning. What it is
/// for is therefore the fields - the language, the namespace, the output path - and the button that
/// runs the generator with them.
/// </remarks>
[TestClass]
public sealed class CodeGeneratorPanelTests
{
	private EditorHarness harness = null!;
	private Schema schema = null!;
	private SchemaCodeGenerator generator = null!;
	private AbsoluteDirectoryPath scratchDirectory = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();

		// A real directory, not the in-memory one: generation writes through System.IO directly.
		scratchDirectory = Path.GetTempPath().As<AbsoluteDirectoryPath>() / $"schema-editor-generator-tests-{Guid.NewGuid():N}".As<DirectoryName>();
		Directory.CreateDirectory(scratchDirectory);

		schema = new Schema();
		generator = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		harness.Editor.CurrentSchema = schema;
		harness.Editor.EditCodeGenerator(generator);
	}

	[TestCleanup]
	public void StopEditor()
	{
		harness.Dispose();

		try
		{
			Directory.Delete(scratchDirectory, recursive: true);
		}
		catch (IOException)
		{
			// A leftover temporary directory is not worth failing a passing test over.
		}
	}

	/// <summary>
	/// Anchors the schema to a file on disk, which is what output paths are resolved against.
	/// </summary>
	private void SaveSchemaTo(string name)
	{
		AbsoluteFilePath path = scratchDirectory / name.As<FileName>();
		File.WriteAllText(path, SchemaSerializer.Serialize(schema));
		schema.SetSourceFile(path);
		harness.Editor.CurrentSchemaPath = path;
		harness.App.Step(2);
	}

	private bool IsShowing(string item) => harness.App.Probe.Matches(item).Count > 0;

	[TestMethod]
	public void RenamingACodeGeneratorFromItsPanelRenamesIt()
	{
		harness.Commit("field/CodeGeneratorNameCSharp", "Pocos");

		Assert.AreEqual("Pocos", generator.Name.ToString());
	}

	[TestMethod]
	public void SettingTheNamespaceRecordsIt()
	{
		harness.Commit("field/CodeGeneratorNamespaceCSharp", "Contoso.Model");

		Assert.AreEqual("Contoso.Model", generator.Namespace.ToString());
	}

	[TestMethod]
	public void SettingTheNamespaceIsUndoable()
	{
		harness.Commit("field/CodeGeneratorNamespaceCSharp", "Contoso.Model");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual(string.Empty, generator.Namespace.ToString());
	}

	[TestMethod]
	public void SettingTheOutputPathRecordsIt()
	{
		harness.Commit("field/CodeGeneratorOutputCSharp", "generated");

		Assert.AreEqual("generated", generator.OutputPath.ToString());
	}

	[TestMethod]
	public void BrowsingForAnOutputPathAsksForADirectory()
	{
		harness.Click("browse/CSharp");
		harness.App.Step(4);

		Assert.IsTrue(IsShowing("filesystem-browser/cancel"), "The directory browser was not raised.");
	}

	[TestMethod]
	public void ChoosingALanguageSetsIt()
	{
		harness.Click("language-selector/CSharp");
		harness.Click($"language-option/{SchemaGenerator.SupportedLanguages.First()}");

		Assert.AreEqual(SchemaGenerator.SupportedLanguages.First(), generator.Language.ToString());
	}

	[TestMethod]
	public void ChoosingALanguageIsUndoable()
	{
		harness.Click("language-selector/CSharp");
		harness.Click($"language-option/{SchemaGenerator.SupportedLanguages.First()}");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual(string.Empty, generator.Language.ToString());
	}

	[TestMethod]
	public void EditingTheDescriptionRecordsIt()
	{
		harness.TypeInto("field/CodeGeneratorDescriptionCSharp", "Plain classes for the model.");
		harness.Click("field/CodeGeneratorNameCSharp");

		Assert.AreEqual("Plain classes for the model.", generator.Description.ToString());
	}

	/// <summary>
	/// Output paths are relative to the schema file, so there is nothing to resolve them against
	/// until the schema has been saved. The panel says so rather than offering a button that would
	/// fail.
	/// </summary>
	[TestMethod]
	public void GeneratingIsNotOfferedUntilTheSchemaHasBeenSaved()
	{
		harness.App.Step(2);

		Assert.IsFalse(schema.CanResolvePaths, "This test is about a schema that has never been saved.");
		Assert.IsFalse(IsShowing("generate/CSharp"), "Generating was offered with nothing to resolve output paths against.");
	}

	[TestMethod]
	public void GeneratingIsOfferedOnceTheSchemaHasBeenSaved()
	{
		SaveSchemaTo("offered.schema.json");

		Assert.IsTrue(IsShowing("generate/CSharp"));
	}

	[TestMethod]
	public void GeneratingWritesTheOutputAndSaysSo()
	{
		schema.AddClass("User".As<ClassName>())!.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Int());
		generator.Language = SchemaGenerator.SupportedLanguages.First().As<LanguageName>();
		generator.Namespace = "Contoso.Model".As<CodeNamespace>();
		generator.OutputPath = "generated".As<RelativeDirectoryPath>();
		SaveSchemaTo("generating.schema.json");

		harness.Click("generate/CSharp");
		harness.App.Step(3);

		Assert.IsTrue(Directory.Exists(scratchDirectory / "generated".As<DirectoryName>()), "Nothing was written.");
		Assert.IsTrue(IsShowing("prompt/OK"), "Generating said nothing about what it had done.");
	}

	/// <summary>
	/// A generator that cannot run says why. A schema with errors in it is the case worth pointing
	/// at the diagnostics tab, since that is where the errors can be acted on.
	/// </summary>
	[TestMethod]
	public void GeneratingAnInvalidSchemaIsRefusedWithAReason()
	{
		// A member pointing at a class that is not there: an error, so generation is refused.
		schema.AddClass("User".As<ClassName>())!
			.AddMember("Owner".As<MemberName>())!
			.SetType(new SchemaTypes.Object() { ClassName = "Missing".As<ClassName>() });
		generator.Language = SchemaGenerator.SupportedLanguages.First().As<LanguageName>();
		generator.OutputPath = "generated".As<RelativeDirectoryPath>();
		SaveSchemaTo("invalid.schema.json");

		harness.Click("generate/CSharp");
		harness.App.Step(3);

		Assert.IsFalse(Directory.Exists(scratchDirectory / "generated".As<DirectoryName>()), "A schema with errors in it was generated anyway.");
		Assert.IsTrue(IsShowing("prompt/OK"), "The refusal was not reported.");
	}
}
