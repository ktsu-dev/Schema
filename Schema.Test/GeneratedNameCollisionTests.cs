// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Names a schema may legitimately choose that collide with something the generated C# needs.
/// </summary>
/// <remarks>
/// Two kinds. A class or namespace named <c>System</c> or <c>ktsu</c> shadows the namespace a
/// generated type reference starts with, which every reference now avoids by being written from
/// <c>global::</c>. A member named after the type declaring it has no spelling at all in C# or
/// C++, so validation refuses it before a generator is asked.
/// </remarks>
[TestClass]
public sealed class GeneratedNameCollisionTests
{
	/// <summary>
	/// A class called <c>System</c> beside a class whose members are spelled with <c>System</c>
	/// types. Without <c>global::</c> the second class resolved <c>System.Numerics</c> against the
	/// first and did not compile.
	/// </summary>
	[TestMethod]
	[DataRow("System")]
	[DataRow("ktsu")]
	public void AClassNamedAfterARootNamespaceStillCompiles(string name)
	{
		Schema schema = new();
		schema.AddClass(name.As<ClassName>())!.AddMember("Id".As<MemberName>())!.SetType(new Int());
		AddTransform(schema);

		CompileGenerated(schema, "Generated");
	}

	/// <summary>
	/// A generator namespace whose last segment is <c>System</c> shadows the root namespace for
	/// every type declared inside it.
	/// </summary>
	[TestMethod]
	public void ANamespaceEndingInSystemStillCompiles()
	{
		Schema schema = new();
		AddTransform(schema);

		CompileGenerated(schema, "Game.System");
	}

	[TestMethod]
	public void AMemberNamedAfterItsClassIsRefused()
	{
		Schema schema = new();
		schema.AddClass("Score".As<ClassName>())!.AddMember("Score".As<MemberName>())!.SetType(new Int());

		AssertRefused(schema, "Score.Score", "Member 'Score' has the same name as its class");
	}

	[TestMethod]
	public void ASemanticTypeNamedValueIsRefused()
	{
		Schema schema = new();
		schema.AddSemanticType("Value".As<SemanticTypeName>())!.SetUnderlyingType(new Float());

		AssertRefused(schema, "Value", "Semantic type 'Value' has the same name as the 'Value' member");
	}

	[TestMethod]
	public void AFunctionNamedAfterItsInterfaceIsRefused()
	{
		Schema schema = new();
		schema.AddInterface("Draw".As<InterfaceName>())!
			.AddFunction("Draw".As<FunctionName>())!
			.SetReturnType(new Void());

		AssertRefused(schema, "Draw.Draw", "Function 'Draw' has the same name as its interface");
	}

	/// <summary>
	/// The comparison is ordinal, because both languages are case sensitive: <c>score</c> in
	/// <c>Score</c> is a legal declaration and stays accepted.
	/// </summary>
	[TestMethod]
	public void ANameDifferingOnlyInCaseIsAccepted()
	{
		Schema schema = new();
		schema.AddClass("Score".As<ClassName>())!.AddMember("score".As<MemberName>())!.SetType(new Int());

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.IsEmpty(issues, string.Join("; ", issues));
	}

	private static void AddTransform(Schema schema)
	{
		SchemaClass transform = schema.AddClass("Transform".As<ClassName>())!;
		transform.AddMember("Position".As<MemberName>())!.SetType(new Vector3());
		transform.AddMember("Tags".As<MemberName>())!.SetType(new Array
		{
			ElementType = new String(),
			Container = Array.VectorContainer.As<ContainerName>(),
		});
		transform.AddMember("Created".As<MemberName>())!.SetType(new DateTime());
	}

	private static void CompileGenerated(Schema schema, string codeNamespace)
	{
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema, codeNamespace));
		Assert.IsTrue(result.IsSuccess, result.Message);
		GeneratedSourceCompiler.Compile(result.Files);
	}

	private static void AssertRefused(Schema schema, string path, string message)
	{
		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error
				&& i.Path == path
				&& i.Message.Contains(message, System.StringComparison.Ordinal),
			issues,
			string.Join("; ", issues));
	}
}
