// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SchemaTypes = Models.Types;

/// <summary>
/// Covers the code generation architecture, the C# generator's type mapping, and the round trip
/// that keeps that mapping honest: generate, compile, reimport, compare.
/// </summary>
[TestClass]
public class CodeGenerationTests
{
	private static readonly string[] ExpectedFiles = ["Role.g.cs", "Item.g.cs", "User.g.cs"];

	internal static SchemaCodeGenerator ConfigureGenerator(Schema schema, string codeNamespace = "Generated")
	{
		SchemaCodeGenerator generator = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		generator.Language = LanguageName.CSharp;
		generator.Namespace = codeNamespace.As<CodeNamespace>();
		generator.OutputPath = "generated".As<RelativeDirectoryPath>();
		return generator;
	}

	/// <summary>
	/// A schema exercising every built-in type and both container kinds.
	/// </summary>
	internal static Schema CreateFullSchema()
	{
		Schema schema = new();

		SchemaEnum role = schema.AddEnum("Role".As<EnumName>())!;
		role.Description = "What a user may do".As<SchemaChildDescription>();
		role.TryAddValue("Admin".As<EnumValueName>());
		role.TryAddValue("Member".As<EnumValueName>());

		SchemaClass item = schema.AddClass("Item".As<ClassName>())!;
		item.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.String());

		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		user.Description = "A person with an account".As<SchemaChildDescription>();

		Add(user, "Flag", new SchemaTypes.Bool());
		Add(user, "Count", new SchemaTypes.Int());
		Add(user, "Big", new SchemaTypes.Long());
		Add(user, "Ratio", new SchemaTypes.Float());
		Add(user, "Precise", new SchemaTypes.Double());
		Add(user, "Name", new SchemaTypes.String());
		Add(user, "CreatedAt", new SchemaTypes.DateTime());
		Add(user, "Uptime", new SchemaTypes.TimeSpan());
		Add(user, "Position2", new SchemaTypes.Vector2());
		Add(user, "Position3", new SchemaTypes.Vector3());
		Add(user, "Position4", new SchemaTypes.Vector4());
		Add(user, "Tint", new SchemaTypes.ColorRGB());
		Add(user, "Overlay", new SchemaTypes.ColorRGBA());
		Add(user, "Role", new SchemaTypes.Enum { EnumName = "Role".As<EnumName>() });
		Add(user, "Favourite", new SchemaTypes.Object { ClassName = "Item".As<ClassName>() });
		Add(user, "Tags", new SchemaTypes.Array
		{
			ElementType = new SchemaTypes.String(),
			Container = ContainerName.Vector,
		});
		Add(user, "Items", new SchemaTypes.Array
		{
			ElementType = new SchemaTypes.Object { ClassName = "Item".As<ClassName>() },
			Container = ContainerName.Map,
			Key = "Id".As<MemberName>(),
		});

		return schema;
	}

	private static void Add(SchemaClass schemaClass, string name, SchemaTypes.BaseType type)
	{
		SchemaMember member = schemaClass.AddMember(name.As<MemberName>())!;
		member.SetType(type);
	}

	// ---- refusal ------------------------------------------------------------

	[TestMethod]
	public void TestGenerationIsRefusedForASchemaWithErrors()
	{
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = ConfigureGenerator(schema);

		// Deleting Item leaves User.Favourite and User.Items dangling.
		schema.GetClass("Item".As<ClassName>())!.TryRemove();

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, generator);

		Assert.AreEqual(SchemaGenerationStatus.SchemaInvalid, result.Status);
		Assert.AreEqual(0, result.Files.Count);
		Assert.IsTrue(result.Issues.Count > 0);
		Assert.IsTrue(result.Issues.All(i => i.Severity == SchemaValidationSeverity.Error));

		// The message has to say what is wrong, not just that something is.
		Assert.IsTrue(result.Message.Contains("cannot be generated", StringComparison.Ordinal), result.Message);
		Assert.IsTrue(result.Message.Contains("Favourite", StringComparison.Ordinal) ||
			result.Message.Contains("Items", StringComparison.Ordinal), result.Message);
	}

	[TestMethod]
	public void TestWarningsDoNotRefuseGeneration()
	{
		// A member with no type set is a warning, not an error: an incomplete schema is still
		// generatable, and refusing would make the generator unusable mid-edit.
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = ConfigureGenerator(schema);
		schema.GetClass("User".As<ClassName>())!.AddMember("Untyped".As<MemberName>());

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, generator);
		Assert.AreEqual(SchemaGenerationStatus.Success, result.Status, result.Message);
	}

	[TestMethod]
	public void TestGeneratorWithoutALanguageIsRefused()
	{
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = schema.AddCodeGenerator("Unconfigured".As<CodeGeneratorName>())!;

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, generator);

		Assert.AreEqual(SchemaGenerationStatus.GeneratorNotConfigured, result.Status);
		Assert.IsTrue(result.Message.Contains("target language", StringComparison.Ordinal), result.Message);
	}

	[TestMethod]
	public void TestUnknownLanguageIsRefusedAndListsWhatIsKnown()
	{
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = ConfigureGenerator(schema);
		generator.Language = "cobol".As<LanguageName>();

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, generator);

		Assert.AreEqual(SchemaGenerationStatus.UnknownLanguage, result.Status);
		Assert.IsTrue(result.Message.Contains("cobol", StringComparison.Ordinal), result.Message);
		Assert.IsTrue(result.Message.Contains(LanguageName.CSharpName, StringComparison.Ordinal), result.Message);
	}

	[TestMethod]
	public void TestLanguageMatchingIsCaseInsensitive()
	{
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = ConfigureGenerator(schema);
		generator.Language = "CSharp".As<LanguageName>();

		Assert.AreEqual(SchemaGenerationStatus.Success, SchemaGenerator.Generate(schema, generator).Status);
	}

	// ---- what gets emitted --------------------------------------------------

	[TestMethod]
	public void TestAFileIsEmittedPerClassAndEnum()
	{
		Schema schema = CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, ConfigureGenerator(schema));

		Assert.IsTrue(result.IsSuccess, result.Message);
		CollectionAssert.AreEquivalent(ExpectedFiles, result.Files.Keys.ToArray());
	}

	[TestMethod]
	public void TestDescriptionsBecomeDocComments()
	{
		Schema schema = CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, ConfigureGenerator(schema));

		Assert.IsTrue(result.Files["User.g.cs"].Contains("/// A person with an account", StringComparison.Ordinal), result.Files["User.g.cs"]);
		Assert.IsTrue(result.Files["Role.g.cs"].Contains("/// What a user may do", StringComparison.Ordinal), result.Files["Role.g.cs"]);
	}

	[TestMethod]
	public void TestNamespaceIsEmittedWhenConfigured()
	{
		Schema schema = CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, ConfigureGenerator(schema, "My.Game"));

		Assert.IsTrue(result.Files["User.g.cs"].Contains("namespace My.Game;", StringComparison.Ordinal));
	}

	[TestMethod]
	public void TestNoNamespaceIsEmittedWhenNotConfigured()
	{
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = ConfigureGenerator(schema);
		generator.Namespace = new CodeNamespace();

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, generator);
		Assert.IsFalse(result.Files["User.g.cs"].Contains("namespace ", StringComparison.Ordinal));
	}

	[TestMethod]
	[DataRow("Flag", "public bool Flag")]
	[DataRow("Count", "public int Count")]
	[DataRow("Big", "public long Big")]
	[DataRow("Ratio", "public float Ratio")]
	[DataRow("Precise", "public double Precise")]
	[DataRow("Name", "public string Name")]
	[DataRow("CreatedAt", "public System.DateTime CreatedAt")]
	[DataRow("Uptime", "public System.TimeSpan Uptime")]
	[DataRow("Position2", "public System.Numerics.Vector2 Position2")]
	[DataRow("Position3", "public System.Numerics.Vector3 Position3")]
	[DataRow("Position4", "public System.Numerics.Vector4 Position4")]
	[DataRow("Tint", "public ktsu.Schema.Runtime.ColorRgb Tint")]
	[DataRow("Overlay", "public ktsu.Schema.Runtime.ColorRgba Overlay")]
	[DataRow("Role", "public Role Role")]
	[DataRow("Favourite", "public Item Favourite")]
	[DataRow("Tags", "public System.Collections.Generic.List<string> Tags")]
	[DataRow("Items", "public System.Collections.Generic.Dictionary<string, Item> Items")]
	public void TestEveryTypeMapsAsDocumented(string memberName, string expectedDeclaration)
	{
		Schema schema = CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, ConfigureGenerator(schema));

		string source = result.Files["User.g.cs"];
		Assert.IsTrue(source.Contains(expectedDeclaration, StringComparison.Ordinal),
			$"Expected '{expectedDeclaration}' for member '{memberName}' in:\n{source}");
	}

	[TestMethod]
	public void TestMapKeyTypeComesFromTheKeyMember()
	{
		// Item.Id is a String, so the dictionary is keyed by string. Retype it and the key type
		// follows.
		Schema schema = CreateFullSchema();
		schema.GetClass("Item".As<ClassName>())!.GetMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Int());

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, ConfigureGenerator(schema));

		Assert.IsTrue(result.Files["User.g.cs"].Contains("Dictionary<int, Item> Items", StringComparison.Ordinal),
			result.Files["User.g.cs"]);
	}

	[TestMethod]
	public void TestGenerateToDiskWritesTheFiles()
	{
		string directory = Path.Combine(Path.GetTempPath(), $"schema-gen-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		try
		{
			Schema schema = CreateFullSchema();
			SchemaCodeGenerator generator = ConfigureGenerator(schema);
			schema.SetSourceFile(Path.Combine(directory, "test.schema.json").As<AbsoluteFilePath>());

			SchemaGenerationResult result = SchemaGenerator.GenerateToDisk(schema, generator);

			Assert.IsTrue(result.IsSuccess, result.Message);
			Assert.IsTrue(File.Exists(Path.Combine(directory, "generated", "User.g.cs")));
			Assert.IsTrue(File.Exists(Path.Combine(directory, "generated", "Role.g.cs")));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	public void TestGenerateToDiskNeedsToKnowWhereTheSchemaLives()
	{
		Schema schema = CreateFullSchema();
		SchemaCodeGenerator generator = ConfigureGenerator(schema);

		SchemaGenerationResult result = SchemaGenerator.GenerateToDisk(schema, generator);

		Assert.AreEqual(SchemaGenerationStatus.GeneratorNotConfigured, result.Status);
		Assert.IsTrue(result.Message.Contains("location is unknown", StringComparison.Ordinal), result.Message);
	}
}
