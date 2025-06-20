// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a set container for schema child elements with name-based uniqueness.
/// </summary>
/// <typeparam name="TValue">The type of schema child elements, must implement ISchemaChild.</typeparam>
/// <typeparam name="TName">The type of the name used for comparison.</typeparam>
public interface ISchemaChildSet<TValue, TName> : ISet<TValue>
	where TValue : ISchemaChild<TName>
	where TName : ISchemaChildName
{
	/// <summary>
	/// Gets the comparer used for name-based uniqueness comparison.
	/// </summary>
	public IEqualityComparer<TName> NameComparer { get; }

	/// <summary>
	/// Tries to get an element by its name.
	/// </summary>
	/// <param name="name">The name of the element to find.</param>
	/// <param name="element">The found element, if any.</param>
	/// <returns>True if an element with the specified name was found, false otherwise.</returns>
	public bool TryGetByName(TName name, out TValue? element);

	/// <summary>
	/// Determines whether the set contains an element with the specified name.
	/// </summary>
	/// <param name="name">The name to check for.</param>
	/// <returns>True if an element with the specified name exists in the set, false otherwise.</returns>
	public bool ContainsByName(TName name);

	/// <summary>
	/// Removes an element by its name.
	/// </summary>
	/// <param name="name">The name of the element to remove.</param>
	/// <returns>True if an element with the specified name was found and removed, false otherwise.</returns>
	public bool RemoveByName(TName name);
}
