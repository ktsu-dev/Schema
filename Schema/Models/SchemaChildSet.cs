// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using System.Collections;
using System.Diagnostics;
using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Implementation of a set container for schema child elements with name-based uniqueness.
/// </summary>
/// <typeparam name="T">The type of schema child elements, must implement ISchemaChild.</typeparam>
/// <typeparam name="TName">The type of the name used for comparison.</typeparam>
public class SchemaChildSet<T, TName> : ISchemaChildSet<T, TName>
	where T : class, ISchemaChild<TName>
	where TName : SemanticString<TName>, ISchemaChildName, new()
{
	private readonly HashSet<T> innerSet;
	private readonly SchemaChildNameComparer<T, TName> nameComparer;

	/// <summary>
	/// Initializes a new instance of the SchemaChildSet class.
	/// </summary>
	public SchemaChildSet()
	{
		nameComparer = new SchemaChildNameComparer<T, TName>();
		innerSet = new HashSet<T>(nameComparer);
	}

	/// <summary>
	/// Initializes a new instance of the SchemaChildSet class with the specified capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity of the set.</param>
	public SchemaChildSet(int capacity)
	{
		nameComparer = new SchemaChildNameComparer<T, TName>();
		innerSet = new HashSet<T>(capacity, nameComparer);
	}

	/// <summary>
	/// Initializes a new instance of the SchemaChildSet class with the specified collection.
	/// </summary>
	/// <param name="collection">The collection to initialize the set with.</param>
	public SchemaChildSet(IEnumerable<T> collection)
	{
		nameComparer = new SchemaChildNameComparer<T, TName>();
		innerSet = new HashSet<T>(collection, nameComparer);
	}

	/// <inheritdoc/>
	public IEqualityComparer<TName> NameComparer => EqualityComparer<TName>.Default;

	/// <inheritdoc/>
	public int Count => innerSet.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	public bool Add(T item)
	{
		Ensure.NotNull(item, nameof(item));
		return innerSet.Add(item);
	}

	/// <inheritdoc/>
	void ICollection<T>.Add(T item) => Add(item);

	/// <inheritdoc/>
	public void Clear() => innerSet.Clear();

	/// <inheritdoc/>
	public bool Contains(T item)
	{
		Ensure.NotNull(item, nameof(item));
		return innerSet.Contains(item);
	}

	/// <inheritdoc/>
	public void CopyTo(T[] array, int arrayIndex) => innerSet.CopyTo(array, arrayIndex);

	/// <inheritdoc/>
	public void ExceptWith(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		innerSet.ExceptWith(other);
	}

	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator() => innerSet.GetEnumerator();

	/// <inheritdoc/>
	public void IntersectWith(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		innerSet.IntersectWith(other);
	}

	/// <inheritdoc/>
	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		return innerSet.IsProperSubsetOf(other);
	}

	/// <inheritdoc/>
	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		return innerSet.IsProperSupersetOf(other);
	}

	/// <inheritdoc/>
	public bool IsSubsetOf(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		return innerSet.IsSubsetOf(other);
	}

	/// <inheritdoc/>
	public bool IsSupersetOf(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		return innerSet.IsSupersetOf(other);
	}

	/// <inheritdoc/>
	public bool Overlaps(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		return innerSet.Overlaps(other);
	}

	/// <inheritdoc/>
	public bool Remove(T item)
	{
		Ensure.NotNull(item, nameof(item));
		return innerSet.Remove(item);
	}

	/// <inheritdoc/>
	public bool SetEquals(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		return innerSet.SetEquals(other);
	}

	/// <inheritdoc/>
	public void SymmetricExceptWith(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		innerSet.SymmetricExceptWith(other);
	}

	/// <inheritdoc/>
	public void UnionWith(IEnumerable<T> other)
	{
		Ensure.NotNull(other, nameof(other));
		innerSet.UnionWith(other);
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <inheritdoc/>
	public bool TryGetByName(TName name, out T? element)
	{
		Ensure.NotNull(name, nameof(name));
		element = innerSet.FirstOrDefault(item => NameComparer.Equals(item.Name, name));
		return element is not null;
	}

	/// <inheritdoc/>
	public bool ContainsByName(TName name)
	{
		Ensure.NotNull(name, nameof(name));
		return innerSet.Any(item => NameComparer.Equals(item.Name, name));
	}

	/// <inheritdoc/>
	public bool RemoveByName(TName name)
	{
		Ensure.NotNull(name, nameof(name));
		if (TryGetByName(name, out T? element))
		{
			Debug.Assert(element is not null);
			return innerSet.Remove(element);
		}
		return false;
	}
}
