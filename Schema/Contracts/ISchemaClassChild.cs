// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a child element of a schema class.
/// </summary>
/// <typeparam name="TName">The type of the name.</typeparam>
public interface ISchemaClassChild<TName> : ISchemaChild<TName> where TName : ISchemaClassChildName
{
	/// <summary>
	/// Gets the parent class of the schema class child.
	/// </summary>
	public ISchemaClass? ParentClass { get; }
}
