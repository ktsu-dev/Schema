// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Names;

using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a container name as a strong string type.
/// </summary>
public sealed record class ContainerName : SemanticString<ContainerName>, ISchemaTypeName
{
	/// <summary>
	/// Gets the container name for a vector.
	/// </summary>
	public static ContainerName Vector { get; } = "vector".As<ContainerName>();
}
