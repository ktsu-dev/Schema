// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

/// <summary>
/// Defines a name-indexed, order-preserving view over a set of schema child elements.
/// </summary>
/// <remarks>
/// <para>
/// <typeparamref name="TValue"/> is covariant so that a set of concrete elements can be consumed
/// through the contracts: a <c>SchemaChildSet&lt;SchemaClass, ClassName&gt;</c> is an
/// <c>ISchemaChildSet&lt;ISchemaClass, ClassName&gt;</c>. That rules out the mutating members of
/// <see cref="ISet{T}"/> — <c>Add</c> would put <typeparamref name="TValue"/> in an input position
/// and make the interface invariant — so mutation lives on the owning element instead, where it can
/// also establish parent association. For the same reason the lookup returns its result rather than
/// using an <see langword="out"/> parameter, which C# treats as an invariant position.
/// </para>
/// <para>
/// <typeparamref name="TName"/> is invariant, so implementations and consumers name the same
/// concrete name type. Name types are values rather than entities: abstracting a semantic string
/// behind a further interface buys nothing and is what makes the variance unworkable.
/// </para>
/// <para>
/// Enumeration order is the order elements were added, and is preserved through serialization.
/// </para>
/// </remarks>
/// <typeparam name="TValue">The type of the schema child elements.</typeparam>
/// <typeparam name="TName">The type of the name used to look elements up.</typeparam>
public interface ISchemaChildSet<out TValue, in TName> : IReadOnlyCollection<TValue>
	where TValue : class
{
	/// <summary>
	/// Gets the element with the specified name.
	/// </summary>
	/// <param name="name">The name of the element to find.</param>
	/// <returns>The element with that name, or <see langword="null"/> if the set contains no such element.</returns>
	public TValue? GetByName(TName name);

	/// <summary>
	/// Determines whether the set contains an element with the specified name.
	/// </summary>
	/// <param name="name">The name to check for.</param>
	/// <returns><see langword="true"/> if an element with that name exists in the set; otherwise, <see langword="false"/>.</returns>
	public bool ContainsByName(TName name);
}
