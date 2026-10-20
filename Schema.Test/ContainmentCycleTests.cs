// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the refusal of a class that holds itself by value, which no generator can turn into a
/// type of finite size.
/// </summary>
[TestClass]
public sealed class ContainmentCycleTests
{
	/// <summary>
	/// A class holding itself directly is the commonest way to write this, and it is reported at
	/// the member that does it.
	/// </summary>
	[TestMethod]
	public void AClassHoldingItselfIsAnError()
	{
		Schema schema = new();
		SchemaClass node = schema.AddClass("TreeNode".As<ClassName>())!;
		node.AddMember("Parent".As<MemberName>())!.SetType(ObjectOf("TreeNode"));

		SchemaValidationIssue error = Errors(schema).Single();

		Assert.AreEqual("TreeNode.Parent", error.Path);
		StringAssert.Contains(error.Message, "TreeNode.Parent → TreeNode");
	}

	/// <summary>
	/// A cycle through other classes is one issue, naming every step of it, rather than one per
	/// class on it.
	/// </summary>
	[TestMethod]
	public void ACycleThroughSeveralClassesIsOneErrorNamingTheRoute()
	{
		Schema schema = new();
		schema.AddClass("A".As<ClassName>())!.AddMember("b".As<MemberName>())!.SetType(ObjectOf("B"));
		schema.AddClass("B".As<ClassName>())!.AddMember("c".As<MemberName>())!.SetType(ObjectOf("C"));
		schema.AddClass("C".As<ClassName>())!.AddMember("a".As<MemberName>())!.SetType(ObjectOf("A"));

		SchemaValidationIssue error = Errors(schema).Single();

		Assert.AreEqual("C.a", error.Path);
		StringAssert.Contains(error.Message, "A.b → B.c → C.a → A");
	}

	/// <summary>
	/// An optional and a result hold what they wrap in place, so a cycle through either is still
	/// a cycle.
	/// </summary>
	/// <param name="wrapper">The wrapper the self-reference goes through.</param>
	[TestMethod]
	[DataRow("Optional")]
	[DataRow("Result")]
	public void ACycleThroughAnInlineWrapperIsAnError(string wrapper)
	{
		BaseType element = ObjectOf("TreeNode");
		BaseType type = wrapper == "Optional" ? new Optional { ElementType = element } : new Result { ElementType = element };

		Schema schema = new();
		schema.AddClass("TreeNode".As<ClassName>())!.AddMember("Parent".As<MemberName>())!.SetType(type);

		StringAssert.Contains(Errors(schema).Single(issue => issue.Message.Contains("holds itself", StringComparison.Ordinal)).Message, "holds itself by value");
	}

	/// <summary>
	/// An array and a handle reach their element by indirection, which is how a recursive
	/// structure is meant to be written, so neither is a cycle.
	/// </summary>
	/// <param name="indirection">The indirection the self-reference goes through.</param>
	[TestMethod]
	[DataRow("Array")]
	[DataRow("Handle")]
	public void AReferenceThroughAnIndirectionIsNotACycle(string indirection)
	{
		BaseType element = ObjectOf("TreeNode");
		BaseType type = indirection == "Array" ? new Models.Types.Array { ElementType = element } : new Handle { ElementType = element };

		Schema schema = new();
		schema.AddClass("TreeNode".As<ClassName>())!.AddMember("Children".As<MemberName>())!.SetType(type);

		Assert.IsFalse(
			schema.Validate().Any(i => i.Message.Contains("holds itself by value", StringComparison.Ordinal)),
			string.Join("; ", schema.Validate().Select(i => i.Message)));
	}

	/// <summary>
	/// Two classes holding the same third class is a shared part, not a cycle.
	/// </summary>
	[TestMethod]
	public void ADiamondIsNotACycle()
	{
		Schema schema = new();
		schema.AddClass("Point".As<ClassName>())!.AddMember("X".As<MemberName>())!.SetType(new Float());
		SchemaClass line = schema.AddClass("Line".As<ClassName>())!;
		line.AddMember("Start".As<MemberName>())!.SetType(ObjectOf("Point"));
		line.AddMember("End".As<MemberName>())!.SetType(ObjectOf("Point"));
		schema.AddClass("Shape".As<ClassName>())!.AddMember("Edge".As<MemberName>())!.SetType(ObjectOf("Line"));

		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema).Select(e => e.Message)));
	}

	/// <summary>
	/// A class named by a member but not declared is reported elsewhere, and is not walked into.
	/// </summary>
	[TestMethod]
	public void AnUnresolvedClassIsNotWalked()
	{
		Schema schema = new();
		schema.AddClass("A".As<ClassName>())!.AddMember("b".As<MemberName>())!.SetType(ObjectOf("Missing"));

		Assert.IsFalse(schema.Validate().Any(i => i.Message.Contains("holds itself by value", StringComparison.Ordinal)));
	}

	private static Models.Types.Object ObjectOf(string className) => new() { ClassName = className.As<ClassName>() };

	private static Collection<SchemaValidationIssue> Errors(Schema schema) =>
		[.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];
}
