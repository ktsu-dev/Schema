// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// A named set of functions: a service the engine offers, or one it requires.
/// </summary>
/// <remarks>
/// An interface is where behaviour is declared, as a class is where data is. A generator turns one
/// into the header an implementation is written against — the abstract type in C++, the interface
/// in C# — so the declaration is the contract and the implementation cannot drift from it without
/// failing to compile.
/// </remarks>
public class SchemaInterface : SchemaChild<InterfaceName>
{
	[JsonInclude]
	[JsonPropertyName("functions")]
	internal Collection<SchemaFunction> FunctionsInternal { get; private set; } = [];

	/// <summary>
	/// Gets the interface's functions, in declaration order.
	/// </summary>
	/// <remarks>
	/// Order is preserved for the same reason a class's members are: it is what a reader sees, and
	/// a generated header that reorders itself between runs is a diff nobody can review.
	/// </remarks>
	[JsonIgnore]
	public IReadOnlyList<SchemaFunction> Functions => FunctionsInternal;

	/// <summary>
	/// Declares a function on this interface.
	/// </summary>
	/// <param name="name">The function's name, unique within this interface.</param>
	/// <returns>The new function, or <see langword="null"/> when the name is already taken.</returns>
	/// <remarks>
	/// Uniqueness is by name alone: this schema has no overloading. Two functions differing only
	/// in parameters would be one name in some target languages and two in others, so the schema
	/// refuses the case rather than generating something different per language.
	/// </remarks>
	public SchemaFunction? AddFunction(FunctionName name)
	{
		Ensure.NotNull(name);
		if (FunctionsInternal.Any(function => function.Name == name))
		{
			return null;
		}

		SchemaFunction function = new() { Name = name };
		function.AssociateWith(this);
		FunctionsInternal.Add(function);
		return function;
	}

	/// <summary>
	/// Removes a function from this interface.
	/// </summary>
	/// <param name="function">The function to remove.</param>
	/// <returns><see langword="true"/> when it was present and removed.</returns>
	public bool TryRemoveFunction(SchemaFunction function) => FunctionsInternal.Remove(function);

	/// <summary>
	/// Finds a function by name.
	/// </summary>
	/// <param name="name">The name to look for.</param>
	/// <param name="function">The function, when found.</param>
	/// <returns><see langword="true"/> when a function of that name exists.</returns>
	public bool TryGetFunction(FunctionName name, out SchemaFunction? function)
	{
		function = FunctionsInternal.FirstOrDefault(candidate => candidate.Name == name);
		return function is not null;
	}

	/// <summary>
	/// Re-establishes the parent links this interface's functions lost on deserialization.
	/// </summary>
	internal void Reassociate()
	{
		foreach (SchemaFunction function in FunctionsInternal)
		{
			function.AssociateWith(this);
			if (ParentSchema is not null)
			{
				function.AssociateWith(ParentSchema);
			}

			function.Reassociate();
		}
	}

	/// <inheritdoc />
	public override bool TryRemove() => ParentSchema?.TryRemoveInterface(this) ?? false;
}
