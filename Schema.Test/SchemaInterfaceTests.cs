// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the API half of the schema: interfaces, functions, parameters, and the four conventions
/// that let a signature carry no ownership, lifetime or error annotations.
/// </summary>
[TestClass]
public sealed class SchemaInterfaceTests
{
	/// <summary>
	/// A function's parameters keep the order they were declared in. Order is the signature, so
	/// this is a correctness property rather than a cosmetic one.
	/// </summary>
	[TestMethod]
	public void ParametersKeepDeclarationOrder()
	{
		SchemaFunction function = NewFunction();
		function.AddParameter("first".As<ParameterName>());
		function.AddParameter("second".As<ParameterName>());
		function.AddParameter("third".As<ParameterName>());

		CollectionAssert.AreEqual(
			ExpectedParameterOrder,
			function.Parameters.Select(p => p.Name.ToString()).ToArray());
	}

	/// <summary>
	/// There is no overloading: a name identifies a function within its interface.
	/// </summary>
	[TestMethod]
	public void AddFunction_RejectsADuplicateName()
	{
		Schema schema = new();
		SchemaInterface renderer = schema.AddInterface("Renderer".As<InterfaceName>())!;

		Assert.IsNotNull(renderer.AddFunction("Submit".As<FunctionName>()));
		Assert.IsNull(renderer.AddFunction("Submit".As<FunctionName>()));
	}

	/// <summary>
	/// A parameter name is unique within its function.
	/// </summary>
	[TestMethod]
	public void AddParameter_RejectsADuplicateName()
	{
		SchemaFunction function = NewFunction();

		Assert.IsNotNull(function.AddParameter("count".As<ParameterName>()));
		Assert.IsNull(function.AddParameter("count".As<ParameterName>()));
	}

	/// <summary>
	/// A class named in a signature resolves to the class, the same way a member's type does.
	/// </summary>
	/// <remarks>
	/// This is what the schema association on <see cref="BaseType"/> exists for. Before it,
	/// resolution walked the parent member, and a parameter has none — so every class reference in
	/// an API would have silently resolved to nothing.
	/// </remarks>
	[TestMethod]
	public void AClassNamedInASignatureResolves()
	{
		Schema schema = new();
		schema.AddClass("Transform".As<ClassName>());
		SchemaInterface scene = schema.AddInterface("Scene".As<InterfaceName>())!;
		SchemaFunction spawn = scene.AddFunction("Spawn".As<FunctionName>())!;
		SchemaParameter parameter = spawn.AddParameter("transform".As<ParameterName>())!;

		parameter.SetType(new Object { ClassName = "Transform".As<ClassName>() });

		Object objectType = (Object)parameter.Type;
		Assert.IsNotNull(objectType.Class);
		Assert.AreEqual("Transform", objectType.Class!.Name.ToString());
	}

	/// <summary>
	/// A class named inside a wrapper resolves too, so <c>Span&lt;Transform&gt;</c> is as usable
	/// as a bare <c>Transform</c>.
	/// </summary>
	[TestMethod]
	public void AClassInsideAWrapperResolves()
	{
		Schema schema = new();
		schema.AddClass("Transform".As<ClassName>());
		SchemaInterface scene = schema.AddInterface("Scene".As<InterfaceName>())!;
		SchemaFunction update = scene.AddFunction("Update".As<FunctionName>())!;
		SchemaParameter parameter = update.AddParameter("transforms".As<ParameterName>())!;

		parameter.SetType(new Span { ElementType = new Object { ClassName = "Transform".As<ClassName>() } });

		Object element = (Object)((Span)parameter.Type).ElementType;
		Assert.IsNotNull(element.Class);
	}

	/// <summary>
	/// Interfaces, functions, parameters, directions and the wrapper types all survive a save and
	/// a load.
	/// </summary>
	[TestMethod]
	public void AnInterfaceRoundTrips()
	{
		Schema schema = new();
		schema.AddClass("Texture".As<ClassName>());
		SchemaInterface renderer = schema.AddInterface("Renderer".As<InterfaceName>())!;
		SchemaFunction load = renderer.AddFunction("LoadTexture".As<FunctionName>())!;
		load.SetReturnType(new Result { ElementType = new Handle { ElementType = new Object { ClassName = "Texture".As<ClassName>() } } });

		SchemaParameter path = load.AddParameter("path".As<ParameterName>())!;
		path.SetType(new String());

		SchemaParameter pixels = load.AddParameter("pixels".As<ParameterName>())!;
		pixels.SetType(new Span { ElementType = new Int() });
		pixels.Direction = ParameterDirection.Out;

		string json = SchemaSerializer.Serialize(schema);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? loaded));

		SchemaInterface loadedRenderer = loaded!.GetInterface("Renderer".As<InterfaceName>())!;
		Assert.IsTrue(loadedRenderer.TryGetFunction("LoadTexture".As<FunctionName>(), out SchemaFunction? loadedLoad));

		Assert.IsInstanceOfType<Result>(loadedLoad!.ReturnType);
		Assert.IsInstanceOfType<Handle>(((Result)loadedLoad.ReturnType).ElementType);

		Assert.AreEqual(2, loadedLoad.Parameters.Count);
		Assert.AreEqual("path", loadedLoad.Parameters[0].Name.ToString());
		Assert.AreEqual(ParameterDirection.In, loadedLoad.Parameters[0].Direction);
		Assert.AreEqual("pixels", loadedLoad.Parameters[1].Name.ToString());
		Assert.AreEqual(ParameterDirection.Out, loadedLoad.Parameters[1].Direction);
		Assert.IsInstanceOfType<Span>(loadedLoad.Parameters[1].Type);
	}

	/// <summary>
	/// A reference in a signature still resolves after a round trip, which is what
	/// <c>Reassociate</c> is for.
	/// </summary>
	[TestMethod]
	public void ASignatureReferenceResolvesAfterALoad()
	{
		Schema schema = new();
		schema.AddClass("Transform".As<ClassName>());
		SchemaInterface scene = schema.AddInterface("Scene".As<InterfaceName>())!;
		SchemaFunction spawn = scene.AddFunction("Spawn".As<FunctionName>())!;
		spawn.AddParameter("transform".As<ParameterName>())!
			.SetType(new Object { ClassName = "Transform".As<ClassName>() });

		Assert.IsTrue(SchemaSerializer.TryDeserialize(SchemaSerializer.Serialize(schema), out Schema? loaded));

		SchemaParameter parameter = loaded!
			.GetInterface("Scene".As<InterfaceName>())!
			.Functions[0].Parameters[0];

		Assert.IsNotNull(((Object)parameter.Type).Class);
	}

	/// <summary>
	/// A valid interface produces no validation issues.
	/// </summary>
	[TestMethod]
	public void AWellFormedInterfaceValidatesClean()
	{
		Schema schema = new();
		SchemaInterface clock = schema.AddInterface("Clock".As<InterfaceName>())!;
		SchemaFunction now = clock.AddFunction("Now".As<FunctionName>())!;
		now.SetReturnType(new Double());

		Assert.IsEmpty(schema.Validate().Where(i => i.Path.StartsWith("Clock", StringComparison.Ordinal)));
	}

	/// <summary>
	/// An Array may not cross a boundary: a borrowed sequence is a Span, and that is what keeps
	/// ownership unambiguous without an annotation saying so.
	/// </summary>
	[TestMethod]
	public void AnArrayParameterIsRejected()
	{
		Schema schema = new();
		SchemaInterface mesh = schema.AddInterface("Mesh".As<InterfaceName>())!;
		SchemaFunction upload = mesh.AddFunction("Upload".As<FunctionName>())!;
		upload.AddParameter("vertices".As<ParameterName>())!
			.SetType(new Array { ElementType = new Float() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("Span", StringComparison.Ordinal)));
	}

	/// <summary>
	/// Fallibility describes a call, not an argument to one.
	/// </summary>
	[TestMethod]
	public void AResultParameterIsRejected()
	{
		Schema schema = new();
		SchemaInterface loader = schema.AddInterface("Loader".As<InterfaceName>())!;
		SchemaFunction load = loader.AddFunction("Load".As<FunctionName>())!;
		load.AddParameter("outcome".As<ParameterName>())!
			.SetType(new Result { ElementType = new Int() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("fallible", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A function whose return type was never chosen is not generatable, and says so distinctly
	/// from one that deliberately returns nothing.
	/// </summary>
	[TestMethod]
	public void AnUnchosenReturnTypeIsRejectedAndVoidIsNot()
	{
		Schema schema = new();
		SchemaInterface logger = schema.AddInterface("Logger".As<InterfaceName>())!;

		SchemaFunction undecided = logger.AddFunction("Undecided".As<FunctionName>())!;
		undecided.SetReturnType(new None());

		SchemaFunction write = logger.AddFunction("Write".As<FunctionName>())!;
		write.SetReturnType(new Void());

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("no return type chosen", StringComparison.Ordinal)));
		Assert.IsFalse(schema.Validate().Any(i => i.Path.Contains("Write", StringComparison.Ordinal)));
	}

	/// <summary>
	/// A signature naming an interface that does not exist is reported rather than dangling.
	/// </summary>
	[TestMethod]
	public void AnUnresolvedInterfaceReferenceIsRejected()
	{
		Schema schema = new();
		SchemaInterface engine = schema.AddInterface("Engine".As<InterfaceName>())!;
		SchemaFunction audio = engine.AddFunction("Audio".As<FunctionName>())!;
		audio.SetReturnType(new Interface { InterfaceName = "AudioDevice".As<InterfaceName>() });

		Assert.IsTrue(schema.Validate().Any(i => i.Message.Contains("does not declare", StringComparison.Ordinal)));
	}

	private static readonly string[] ExpectedParameterOrder = ["first", "second", "third"];

	private static SchemaFunction NewFunction()
	{
		Schema schema = new();
		SchemaInterface schemaInterface = schema.AddInterface("Surface".As<InterfaceName>())!;
		return schemaInterface.AddFunction("Call".As<FunctionName>())!;
	}
}
