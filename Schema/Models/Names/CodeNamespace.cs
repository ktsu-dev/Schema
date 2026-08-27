// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Names;

using ktsu.Semantics.Strings;

/// <summary>
/// Represents the namespace generated code is emitted into, as a strong string type.
/// </summary>
public sealed record class CodeNamespace : SemanticString<CodeNamespace>;
