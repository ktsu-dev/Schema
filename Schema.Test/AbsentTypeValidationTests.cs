// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the two types that describe the absence of a value rather than a value, and the one
/// position each of them is allowed to stand in.
/// </summary>
/// <remarks>
/// <see cref="Void"/> is a decision - this function returns nothing - and <see cref="None"/> is an
/// unfinished one. Neither is a type a value can be, so neither can be generated where a value has
/// to be, and a wrapper is not a place one stops being needed: <c>std::optional&lt;void&gt;</c> and
/// <c>std::span&lt;void&gt;</c> are not instantiable and <c>void x{};</c> is not a declaration.
/// </remarks>
[TestClass]
public sealed class AbsentTypeValidationTests
{
	/// <summary>
	/// A member holding nothing is not a member. The editor's type picker offers <c>Void</c> for
	/// return types and the same picker serves members, so this is a reachable edit rather than a
	/// contrived one.
	/// </summary>
	[TestMethod]
	public void AVoidMemberIsRejected()
	{
		Schema schema = HolderOf(new Void());

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error
				&& i.Path == "Holder.Value"
				&& i.Message.Contains("Void carries no value", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// Every wrapper resolves to whatever it wraps, and wrapping nothing yields nothing. Each of
	/// these validated cleanly and then emitted C++ the consumer's compiler refused.
	/// </summary>
	[TestMethod]
	[DataRow("Optional")]
	[DataRow("Span")]
	[DataRow("Handle")]
	[DataRow("Array")]
	public void AVoidInsideAWrapperIsRejected(string wrapper)
	{
		Schema schema = HolderOf(Wrap(wrapper, new Void()));

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error
				&& i.Path == "Holder.Value"
				&& i.Message.Contains("Void carries no value", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// The check is on the way down rather than at the top, so burying it deeper does not get it
	/// past.
	/// </summary>
	[TestMethod]
	public void AVoidTwoWrappersDeepIsRejected()
	{
		Schema schema = HolderOf(new Array
		{
			ElementType = new Optional { ElementType = new Void() },
			Container = Array.VectorContainer.As<ContainerName>(),
		});

		Assert.Contains(
			i => i.Message.Contains("Void carries no value", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A member whose type was never chosen is one unfinished edit, and says so in the words of
	/// the thing that is unfinished. Below a member there was nobody saying anything at all, so an
	/// <c>Optional&lt;None&gt;</c> validated clean and threw at generation time.
	/// </summary>
	[TestMethod]
	[DataRow("Optional")]
	[DataRow("Span")]
	[DataRow("Handle")]
	[DataRow("Array")]
	public void AnUnchosenTypeInsideAWrapperIsRejected(string wrapper)
	{
		Schema schema = HolderOf(Wrap(wrapper, new None()));

		Assert.Contains(
			i => i.Severity == SchemaValidationSeverity.Error
				&& i.Path == "Holder.Value"
				&& i.Message.Contains("No type was chosen for what this carries", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A fallible call that produces nothing is the one thing a wrapper over <c>Void</c> may say,
	/// and it is a type in both target languages - <c>std::expected&lt;void, E&gt;</c> and the
	/// one-argument <c>Result&lt;TError&gt;</c>.
	/// </summary>
	[TestMethod]
	public void ResultOfVoidIsAccepted()
	{
		Schema schema = Fallible();
		SchemaFunction flush = schema.AddInterface("Sink".As<InterfaceName>())!
			.AddFunction("Flush".As<FunctionName>())!;
		flush.SetReturnType(new Result { ElementType = new Void() });

		Assert.IsEmpty(schema.Validate(), string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// The exemption is the <c>Result</c>'s own value and does not carry through a wrapper inside
	/// it: <c>std::expected&lt;std::optional&lt;void&gt;, E&gt;</c> is as ill-formed as
	/// <c>std::optional&lt;void&gt;</c> alone.
	/// </summary>
	[TestMethod]
	public void ResultOfAWrapperOverVoidIsRejected()
	{
		Schema schema = Fallible();
		SchemaFunction flush = schema.AddInterface("Sink".As<InterfaceName>())!
			.AddFunction("Flush".As<FunctionName>())!;
		flush.SetReturnType(new Result { ElementType = new Optional { ElementType = new Void() } });

		Assert.Contains(
			i => i.Message.Contains("Void carries no value", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A <c>Result</c> whose value was never chosen is reported wherever it is written. In a return
	/// position the caller already says it in better words - "use Result&lt;Void&gt;" - so this is
	/// the member position, which said nothing before.
	/// </summary>
	[TestMethod]
	public void ResultOfAnUnchosenTypeIsRejected()
	{
		Schema schema = Fallible();
		SchemaClass holder = schema.AddClass("Holder".As<ClassName>())!;
		holder.AddMember("Value".As<MemberName>())!.SetType(new Result { ElementType = new None() });

		Assert.Contains(
			i => i.Message.Contains("No type was chosen for what this carries", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A function returning nothing is what <c>Void</c> is for, and it is still accepted there.
	/// </summary>
	[TestMethod]
	public void AVoidReturnTypeIsAccepted()
	{
		Schema schema = new();
		schema.AddInterface("Logger".As<InterfaceName>())!
			.AddFunction("Write".As<FunctionName>())!
			.SetReturnType(new Void());

		Assert.IsEmpty(schema.Validate(), string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A <c>Void</c> parameter keeps the message that names it, which says what to do about it.
	/// </summary>
	[TestMethod]
	public void AVoidParameterKeepsItsOwnMessage()
	{
		Schema schema = new();
		SchemaFunction write = schema.AddInterface("Logger".As<InterfaceName>())!
			.AddFunction("Write".As<FunctionName>())!;
		write.SetReturnType(new Void());
		write.AddParameter("nothing".As<ParameterName>())!.SetType(new Void());

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.ContainsSingle(issues, string.Join("; ", issues));
		Assert.Contains("Remove it", issues[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A parameter is only exempt at the top: the wrapper arm is what let one through.
	/// </summary>
	[TestMethod]
	public void AVoidInsideAParameterIsRejected()
	{
		Schema schema = new();
		SchemaFunction draw = schema.AddInterface("Renderer".As<InterfaceName>())!
			.AddFunction("Draw".As<FunctionName>())!;
		draw.SetReturnType(new Void());
		draw.AddParameter("vertices".As<ParameterName>())!.SetType(new Span { ElementType = new Void() });

		Assert.Contains(
			i => i.Message.Contains("Void carries no value", StringComparison.Ordinal),
			schema.Validate(),
			string.Join("; ", schema.Validate()));
	}

	/// <summary>
	/// A member with no type is one unfinished edit, and is told about once. Reporting the absence
	/// again on the way down would make a schema being typed into noisier the less finished it is.
	/// </summary>
	[TestMethod]
	public void AMemberWithoutATypeIsStillOneMessage()
	{
		Schema schema = new();
		schema.AddClass("Holder".As<ClassName>())!.AddMember("Value".As<MemberName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.ContainsSingle(issues, string.Join("; ", issues));
		Assert.AreEqual(SchemaValidationSeverity.Warning, issues[0].Severity);
	}

	/// <summary>
	/// A vector already says what its components have to be, which is more use than being told
	/// that <c>Void</c> carries no value. One mistake, one message.
	/// </summary>
	[TestMethod]
	public void AVectorOfNothingSaysWhatAComponentHasToBe()
	{
		foreach (BaseType component in new BaseType[] { new Void(), new None() })
		{
			Schema schema = HolderOf(new Vector3 { ElementType = component });

			Collection<SchemaValidationIssue> issues = schema.Validate();

			Assert.ContainsSingle(issues, string.Join("; ", issues));
			Assert.Contains("components are numbers", issues[0].Message, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// A colour says the same thing about its channels.
	/// </summary>
	[TestMethod]
	public void AColourOfNothingSaysWhatAChannelHasToBe()
	{
		Schema schema = HolderOf(new ColorRGB { ElementType = new Void() });

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.ContainsSingle(issues, string.Join("; ", issues));
		Assert.Contains("channels are floats", issues[0].Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A schema holding one class whose single member has the given type.
	/// </summary>
	private static Schema HolderOf(BaseType type)
	{
		Schema schema = new();
		schema.AddClass("Holder".As<ClassName>())!
			.AddMember("Value".As<MemberName>())!
			.SetType(type);

		return schema;
	}

	/// <summary>
	/// A schema that can say why a call failed, which a <c>Result</c> anywhere in it needs.
	/// </summary>
	private static Schema Fallible()
	{
		Schema schema = new();
		schema.AddEnum("ErrorCode".As<EnumName>())!.TryAddValue("NotFound".As<EnumValueName>());
		schema.ErrorType = "ErrorCode".As<EnumName>();

		return schema;
	}

	/// <summary>
	/// The four carriers that hold one element, by name, so one case covers all of them.
	/// </summary>
	/// <remarks>
	/// <c>Result</c> is not among them: it is the one whose element may legitimately be
	/// <c>Void</c>, and it has its own cases above.
	/// </remarks>
	private static BaseType Wrap(string wrapper, BaseType element) => wrapper switch
	{
		"Optional" => new Optional { ElementType = element },
		"Span" => new Span { ElementType = element },
		"Handle" => new Handle { ElementType = element },
		"Array" => new Array { ElementType = element, Container = Array.VectorContainer.As<ContainerName>() },
		_ => throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper, "Not a wrapper this test knows."),
	};
}
