// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Represents a schema type that can be part of a schema member.
/// </summary>
public interface ISchemaType : ISchemaMemberChild<ISchemaTypeName>
{
}
