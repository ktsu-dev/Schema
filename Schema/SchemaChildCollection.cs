// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using System.Collections.ObjectModel;
using System.Diagnostics;

using ktsu.Semantics;

internal class SchemaChildCollection<TChild, TName> where TChild : SchemaChild<TName> where TName : ISemanticString<TName>, new()
{
	private Collection<TChild> ChildCollection { get; set; } = [];

	public IReadOnlyCollection<TChild> Children => ChildCollection;

	public TChild GetOrCreate(TName name)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));
		if (TryGetChild(name, out TChild? child))
		{
			Debug.Assert(child is not null);
			return child;
		}

		TChild newChild = Activator.CreateInstance<TChild>();
		newChild.Rename(name);
		ChildCollection.Add(newChild);
		return newChild;
	}

	public bool TryGetChild(TName name, out TChild? child)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));
		child = ChildCollection.FirstOrDefault(c => c.Name == name);
		return child is not null;
	}

	public bool TryRemoveChild(TChild child)
	{
		ArgumentNullException.ThrowIfNull(child, nameof(child));
		return ChildCollection.Remove(child);
	}
	public bool TryRemoveChild(TName name)
	{
		ArgumentNullException.ThrowIfNull(name, nameof(name));

		if (TryGetChild(name, out TChild? child))
		{
			Debug.Assert(child is not null);
			return ChildCollection.Remove(child!);
		}

		return false;
	}
}
