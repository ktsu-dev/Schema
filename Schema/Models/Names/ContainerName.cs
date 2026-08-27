// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Names;

using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a container name as a strong string type.
/// </summary>
public sealed record class ContainerName : SemanticString<ContainerName>, ISchemaName
{
	/// <summary>
	/// The container name for an ordered sequence, mapped to a list by a code generator.
	/// </summary>
	public const string VectorName = "vector";

	/// <summary>
	/// The container name for a lookup keyed by a member, mapped to a dictionary by a code generator.
	/// </summary>
	public const string MapName = "map";

	/// <summary>
	/// Gets the container name for a vector.
	/// </summary>
	public static ContainerName Vector { get; } = VectorName.As<ContainerName>();

	/// <summary>
	/// Gets the container name for a map.
	/// </summary>
	public static ContainerName Map { get; } = MapName.As<ContainerName>();
}
