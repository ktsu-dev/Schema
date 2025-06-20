// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;
using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics;

/// <summary>
/// Equality comparer for schema child elements based on their name property.
/// </summary>
/// <typeparam name="T">The type of schema child elements.</typeparam>
/// <typeparam name="TName">The type of the name used for comparison.</typeparam>
internal class SchemaChildNameComparer<T, TName> : IEqualityComparer<T>
	where T : ISchemaChild<TName>
	where TName : SemanticString<TName>, ISchemaChildName<TName>, new()
{
	private readonly IEqualityComparer<TName> nameComparer = EqualityComparer<TName>.Default;

	/// <inheritdoc/>
	public bool Equals(T? x, T? y)
	{
		if (ReferenceEquals(x, y))
			return true;

		if (x is null || y is null)
			return false;

		return nameComparer.Equals(x.Name, y.Name);
	}

	/// <inheritdoc/>
	public int GetHashCode(T obj)
	{
		ArgumentNullException.ThrowIfNull(obj, nameof(obj));
		return nameComparer.GetHashCode(obj.Name);
	}
} 
