// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers what a failure says: the enum the schema names once, rather than each fallible signature
/// choosing its own.
/// </summary>
[TestClass]
public sealed class ErrorTypeTests
{
	/// <summary>
	/// A schema that never fails needs no error type, so declaring none is not a mistake in
	/// itself.
	/// </summary>
	[TestMethod]
	public void ASchemaThatCannotFailNeedsNoErrorType()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		world.AddFunction("Step".As<FunctionName>())!.SetReturnType(new Void());

		Assert.IsEmpty(schema.ErrorType.ToString());
		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// A call that can fail has to be able to say why, and the schema is what says it.
	/// </summary>
	[TestMethod]
	public void AResultWithoutAnErrorTypeIsRefused()
	{
		Schema schema = FallibleSchema();

		Collection<SchemaValidationIssue> errors = Errors(schema);
		Assert.ContainsSingle(errors);
		Assert.Contains("declares no error type", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// Declaring one is what makes the same schema generatable, and it is declared once rather
	/// than at the signature.
	/// </summary>
	[TestMethod]
	public void DeclaringOneSatisfiesEveryFallibleSignature()
	{
		Schema schema = FallibleSchema();
		SchemaEnum errorCode = schema.AddEnum("ErrorCode".As<EnumName>())!;
		errorCode.TryAddValue("NotFound".As<EnumValueName>());
		schema.ErrorType = "ErrorCode".As<EnumName>();

		SchemaFunction second = schema.GetInterface("World".As<InterfaceName>())!.AddFunction("Find".As<FunctionName>())!;
		second.SetReturnType(new Result { ElementType = new Int() });

		Assert.IsEmpty(Errors(schema), string.Join("; ", Errors(schema)));
	}

	/// <summary>
	/// The reported issue names the signature rather than the schema root, because that is the
	/// declaration whose meaning is incomplete.
	/// </summary>
	[TestMethod]
	public void TheIssueIsReportedAtTheSignature()
	{
		Collection<SchemaValidationIssue> errors = Errors(FallibleSchema());

		Assert.ContainsSingle(errors);
		Assert.Contains("Spawn", errors[0].Path, StringComparison.Ordinal);
	}

	/// <summary>
	/// An error type naming an enum the schema does not declare is a dangling reference, reported
	/// the same way every other one is.
	/// </summary>
	[TestMethod]
	public void AnErrorTypeNamingNothingIsRefused()
	{
		Schema schema = new() { ErrorType = "Missing".As<EnumName>() };

		Collection<SchemaValidationIssue> errors = Errors(schema);
		Assert.ContainsSingle(errors);
		Assert.Contains("does not declare as an enum", errors[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A Result nested inside another type is still a call that can fail.
	/// </summary>
	[TestMethod]
	public void ANestedResultIsFoundToo()
	{
		Schema schema = new();
		SchemaClass outcome = schema.AddClass("Outcome".As<ClassName>())!;
		outcome.AddMember("Value".As<MemberName>())!.SetType(new Optional
		{
			ElementType = new Result { ElementType = new Int() },
		});

		Assert.Contains(i => i.Message.Contains("declares no error type", StringComparison.Ordinal), Errors(schema));
	}

	/// <summary>
	/// The error type survives a save and a load, which is the only thing that makes declaring it
	/// once worth anything.
	/// </summary>
	[TestMethod]
	public void TheErrorTypeRoundTrips()
	{
		Schema schema = new();
		schema.AddEnum("ErrorCode".As<EnumName>())!.TryAddValue("NotFound".As<EnumValueName>());
		schema.ErrorType = "ErrorCode".As<EnumName>();

		Assert.IsTrue(SchemaSerializer.TryDeserialize(SchemaSerializer.Serialize(schema), out Schema? reloaded));
		Assert.AreEqual("ErrorCode", reloaded!.ErrorType.ToString());
	}

	private static Schema FallibleSchema()
	{
		Schema schema = new();
		SchemaInterface world = schema.AddInterface("World".As<InterfaceName>())!;
		world.AddFunction("Spawn".As<FunctionName>())!.SetReturnType(new Result { ElementType = new Int() });
		return schema;
	}

	private static Collection<SchemaValidationIssue> Errors(Schema schema) =>
		[.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];
}
