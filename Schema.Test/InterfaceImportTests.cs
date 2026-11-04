// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What reading a hand-written type back into a schema takes from it, and what it leaves out:
/// an indexer is not a member, a property accessor is not a function, and a method a base
/// interface declares is still one of the interface's functions.
/// </summary>
[TestClass]
public class InterfaceImportTests
{
	private static readonly string[] OnlyName = ["Name"];
	private static readonly string[] PingAndPong = ["Ping", "Pong"];

	[TestMethod]
	public void AnIndexerIsNotImportedAsAMember()
	{
		Schema schema = new();
		SchemaClass? imported = schema.AddClass(typeof(Indexed));

		Assert.IsNotNull(imported);
		string[] names = [.. imported.Members.Select(member => member.Name.ToString())];
		Assert.AreSequenceEqual(OnlyName, names);
	}

	[TestMethod]
	public void APropertyAccessorIsNotImportedAsAFunction()
	{
		Schema schema = new();
		SchemaInterface? imported = schema.AddInterface(typeof(IDerived));

		Assert.IsNotNull(imported);
		Assert.IsFalse(imported.Functions.Any(function => function.Name.ToString().StartsWith("get_", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void AMethodInheritedFromABaseInterfaceIsImported()
	{
		Schema schema = new();
		SchemaInterface? imported = schema.AddInterface(typeof(IDerived));

		Assert.IsNotNull(imported);
		string[] names = [.. imported.Functions.Select(function => function.Name.ToString()).Order(StringComparer.Ordinal)];
		Assert.AreSequenceEqual(PingAndPong, names);
	}

	[TestMethod]
	public void AMethodReachedByTwoPathsIsImportedOnce()
	{
		Schema schema = new();
		SchemaInterface? imported = schema.AddInterface(typeof(IDiamond));

		Assert.IsNotNull(imported);
		Assert.AreEqual(1, imported.Functions.Count(function => function.Name == "Ping".As<FunctionName>()));
	}

	[TestMethod]
	public void TwoInheritedMethodsWithOneNameAreReported()
	{
		Schema schema = new();

		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => schema.AddInterface(typeof(ICollides)));
		StringAssert.Contains(exception.Message, "Ping");
	}

	public class Indexed
	{
		public string Name { get; set; } = string.Empty;

		public string this[int index] => Name;
	}

	public interface IBase
	{
		public void Ping();
	}

	public interface IDerived : IBase
	{
		public int Count { get; }

		public void Pong();
	}

	public interface ILeft : IBase;

	public interface IRight : IBase;

	public interface IDiamond : ILeft, IRight;

	public interface IOtherBase
	{
		public int Ping(int times);
	}

	public interface ICollides : IBase, IOtherBase;
}
