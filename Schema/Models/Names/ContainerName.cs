// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models.Names;

using ktsu.Extensions;
using ktsu.Schema.Contracts.Names;
using ktsu.Semantics;

/// <summary>
/// Represents a container name as a strong string type.
/// </summary>
public sealed record class ContainerName : SemanticString<ContainerName>, ISchemaTypeName<ContainerName>
{
	/// <summary>
	/// Gets the container name for a vector.
	/// </summary>
	public static ContainerName Vector { get; } = "vector".As<ContainerName>();
}
