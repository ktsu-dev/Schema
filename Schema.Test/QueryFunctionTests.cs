// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the fifth convention: whether calling something changes the thing it is called on.
/// </summary>
[TestClass]
public sealed class QueryFunctionTests
{
	/// <summary>
	/// A function that says nothing about it is a command, which is what every function a schema
	/// already declares has always been.
	/// </summary>
	[TestMethod]
	public void AFunctionIsACommandByDefault()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;

		Assert.IsFalse(world.AddFunction("Step".As<FunctionName>())!.IsQuery);
	}

	/// <summary>
	/// A query that answers something is an ordinary signature and nothing is reported about it.
	/// </summary>
	[TestMethod]
	public void AQueryThatAnswersIsAccepted()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		SchemaFunction find = world.AddFunction("Find".As<FunctionName>())!;
		find.SetReturnType(new Int());
		find.IsQuery = true;

		Assert.IsEmpty(schema.Validate(), string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A query that returns nothing changes nothing and answers nothing, so calling it cannot be
	/// observed at all.
	/// </summary>
	[TestMethod]
	public void AQueryThatReturnsNothingIsReported()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		SchemaFunction step = world.AddFunction("Step".As<FunctionName>())!;
		step.SetReturnType(new Void());
		step.IsQuery = true;

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.ContainsSingle(issues);
		Assert.AreEqual(SchemaValidationSeverity.Warning, issues[0].Severity);
		Assert.Contains("cannot be observed", issues[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A command returning nothing is the ordinary case and is not reported.
	/// </summary>
	[TestMethod]
	public void ACommandThatReturnsNothingIsFine()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		world.AddFunction("Step".As<FunctionName>())!.SetReturnType(new Void());

		Assert.IsEmpty(schema.Validate(), string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A fallible call that produces nothing still answers whether it succeeded, which is
	/// something - so it is exempt.
	/// </summary>
	[TestMethod]
	public void AQueryReturningResultOfVoidIsExempt()
	{
		Schema schema = new();
		schema.AddEnum("ErrorCode".As<EnumName>())!.TryAddValue("NotFound".As<EnumValueName>());
		schema.ErrorType = "ErrorCode".As<EnumName>();

		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		SchemaFunction check = world.AddFunction("Check".As<FunctionName>())!;
		check.SetReturnType(new Result { ElementType = new Void() });
		check.IsQuery = true;

		Assert.IsEmpty(schema.Validate(), string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// The convention survives a save and a load, and a command does not grow a line in the file
	/// for being one.
	/// </summary>
	[TestMethod]
	public void ItRoundTripsAndIsOmittedForACommand()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		world.AddFunction("Step".As<FunctionName>())!.SetReturnType(new Void());

		SchemaFunction find = world.AddFunction("Find".As<FunctionName>())!;
		find.SetReturnType(new Int());
		find.IsQuery = true;

		string json = SchemaSerializer.Serialize(schema);
		Assert.AreEqual(1, Occurrences(json, "isQuery"));

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		SchemaInterface reloadedWorld = reloaded!.GetInterface("World".As<InterfaceName>())!;

		Assert.IsTrue(reloadedWorld.TryGetFunction("Step".As<FunctionName>(), out SchemaFunction? step));
		Assert.IsTrue(reloadedWorld.TryGetFunction("Find".As<FunctionName>(), out SchemaFunction? reloadedFind));
		Assert.IsFalse(step!.IsQuery);
		Assert.IsTrue(reloadedFind!.IsQuery);
	}

	private static int Occurrences(string text, string value)
	{
		int count = 0;
		for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
		{
			count++;
		}

		return count;
	}
}
