// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

/// <summary>
/// Legacy schema class for backward compatibility.
/// This is now an alias to SchemaProvider which focuses solely on schema definition.
/// For new projects, use SchemaProvider directly via dependency injection.
/// </summary>
[Obsolete("Use SchemaProvider directly via dependency injection instead of this legacy wrapper.")]
public class Schema : SchemaProvider
{
	/// <summary>
	/// Initializes a new instance of the Schema class.
	/// This constructor is provided for backward compatibility.
	/// </summary>
	public Schema() : base()
	{
	}
}
