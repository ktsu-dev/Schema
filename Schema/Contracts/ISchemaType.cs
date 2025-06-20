// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Contracts.Names;

/// <summary>
/// Represents a schema type that can be part of a schema member.
/// </summary>
public interface ISchemaType : ISchemaMemberChild<ISchemaTypeName>
{
}
