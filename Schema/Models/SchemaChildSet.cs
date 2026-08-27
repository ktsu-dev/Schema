// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections;
using System.Collections.ObjectModel;
using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// An order-preserving, name-unique view over a collection of schema child elements.
/// </summary>
/// <remarks>
/// <para>
/// This is a view rather than a store: it wraps the collection the owning element serializes, so
/// there is no second copy to diverge from it and no change to the on-disk format. It owns the
/// name-uniqueness rule that would otherwise be re-implemented as an <c>Any(x =&gt; x.Name == name)</c>
/// check at each call site.
/// </para>
/// <para>
/// Order is the order elements were added, and is preserved through serialization. A name-keyed
/// hash set would not preserve it: <see cref="HashSet{T}"/> makes no ordering guarantee, and reuses
/// freed slots after a removal, so a remove-then-add — what undoing a deletion does — could reorder
/// a class's members.
/// </para>
/// <para>
/// Uniqueness is enforced on the way in, not on the way through. Deserialization writes to the
/// underlying collection directly, so a hand-edited file containing duplicate names still loads
/// with both elements present and is reported by <see cref="Schema.Validate"/>. Silently dropping
/// one at load would turn a diagnosable mistake into data loss.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of schema child elements.</typeparam>
/// <typeparam name="TName">The type of the name used for uniqueness and lookup.</typeparam>
public sealed class SchemaChildSet<T, TName> : ISchemaChildSet<T, TName>, IReadOnlyList<T>
	where T : class, ISchemaChild<TName>
	where TName : SemanticString<TName>, ISchemaChildName, new()
{
	private readonly Collection<T> items;

	/// <summary>
	/// Initializes a new instance of the <see cref="SchemaChildSet{T, TName}"/> class over the specified collection.
	/// </summary>
	/// <param name="items">The collection to present. The set reads and writes it in place; it does not copy.</param>
	public SchemaChildSet(Collection<T> items)
	{
		Ensure.NotNull(items);
		this.items = items;
	}

	/// <inheritdoc/>
	public int Count => items.Count;

	/// <summary>
	/// Gets the element at the specified position in declaration order.
	/// </summary>
	/// <param name="index">The zero-based position.</param>
	/// <returns>The element at that position.</returns>
	public T this[int index] => items[index];

	/// <inheritdoc/>
	public T? GetByName(TName name) => items.FirstOrDefault(item => item.Name == name);

	/// <inheritdoc/>
	public bool ContainsByName(TName name) => GetByName(name) is not null;

	/// <summary>
	/// Tries to get an element by its name.
	/// </summary>
	/// <param name="name">The name of the element to find.</param>
	/// <param name="element">The found element, if any.</param>
	/// <returns><see langword="true"/> if an element with that name was found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetByName(TName name, out T? element)
	{
		element = GetByName(name);
		return element is not null;
	}

	/// <summary>
	/// Adds an element, unless its name is already taken.
	/// </summary>
	/// <param name="element">The element to add.</param>
	/// <returns><see langword="true"/> if the element was added; <see langword="false"/> if an element with the same name is already present.</returns>
	public bool Add(T element)
	{
		Ensure.NotNull(element);

		if (ContainsByName(element.Name))
		{
			return false;
		}

		items.Add(element);
		return true;
	}

	/// <summary>
	/// Removes the specified element.
	/// </summary>
	/// <param name="element">The element to remove.</param>
	/// <returns><see langword="true"/> if the element was found and removed; otherwise, <see langword="false"/>.</returns>
	public bool Remove(T element) => items.Remove(element);

	/// <summary>
	/// Removes the element with the specified name.
	/// </summary>
	/// <param name="name">The name of the element to remove.</param>
	/// <returns><see langword="true"/> if an element with that name was found and removed; otherwise, <see langword="false"/>.</returns>
	public bool RemoveByName(TName name) => GetByName(name) is T element && items.Remove(element);

	/// <summary>
	/// Gets the position of an element in declaration order.
	/// </summary>
	/// <param name="element">The element to locate.</param>
	/// <returns>The element's index, or -1 if it is not in the set.</returns>
	public int IndexOf(T element) => items.IndexOf(element);

	/// <summary>
	/// Moves an element to a new position in declaration order.
	/// </summary>
	/// <param name="element">The element to move.</param>
	/// <param name="newIndex">The zero-based position to move it to.</param>
	/// <returns><see langword="true"/> if the element was moved; <see langword="false"/> if it is not in the set or the index is out of range.</returns>
	public bool Move(T element, int newIndex)
	{
		int currentIndex = IndexOf(element);
		if (currentIndex < 0 || newIndex < 0 || newIndex >= items.Count)
		{
			return false;
		}

		if (newIndex != currentIndex)
		{
			items.RemoveAt(currentIndex);
			items.Insert(newIndex, element);
		}

		return true;
	}

	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
