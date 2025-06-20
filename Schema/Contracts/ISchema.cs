// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Defines a provider for schema definitions that can be injected as a dependency.
/// This interface focuses solely on schema definition and management without serialization or filesystem concerns.
/// </summary>
public interface ISchema
{
	/// <summary>
	/// Gets the collection of schema classes.
	/// </summary>
	public ISchemaChildSet<ISchemaClass, ISchemaClassName> Classes { get; }

	/// <summary>
	/// Gets the collection of schema enums.
	/// </summary>
	public ISchemaChildSet<ISchemaEnum, ISchemaEnumName> Enums { get; }
}
