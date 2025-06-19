// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using ktsu.Extensions;
using ktsu.Semantics;

/// <summary>
/// Represents a base type name as a strong string type.
/// </summary>
public sealed record class BaseTypeName : SemanticString<BaseTypeName> { }
/// <summary>
/// Represents a class name as a strong string type.
/// </summary>
public sealed record class ClassName : SemanticString<ClassName> { }
/// <summary>
/// Represents a member name as a strong string type.
/// </summary>
public sealed record class MemberName : SemanticString<MemberName> { }
/// <summary>
/// Represents an enum name as a strong string type.
/// </summary>
public sealed record class EnumName : SemanticString<EnumName> { }
/// <summary>
/// Represents an enum value name as a strong string type.
/// </summary>
public sealed record class EnumValueName : SemanticString<EnumValueName> { }
/// <summary>
/// Represents a container name as a strong string type.
/// </summary>
public sealed record class ContainerName : SemanticString<ContainerName>
{
	/// <summary>
	/// Gets the container name for a vector.
	/// </summary>
	public static ContainerName Vector { get; } = "vector".As<ContainerName>();
}

/// <summary>
/// Represents a data source name as a strong string type.
/// </summary>
public sealed record class DataSourceName : SemanticString<DataSourceName> { }
/// <summary>
/// Represents a code generator name as a strong string type.
/// </summary>
public sealed record class CodeGeneratorName : SemanticString<CodeGeneratorName> { }
