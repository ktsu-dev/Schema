// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Names;

using ktsu.Semantics.Strings;

/// <summary>
/// Represents the name of a physical quantity as a strong string type.
/// </summary>
/// <remarks>
/// Not an <c>ISchemaRootName</c>, and that is the difference from every other name here: a
/// quantity is not something the schema declares, so there is nothing in the document for the
/// name to be unique against. It names one of <c>ktsu.Semantics.Quantities</c>' 212 quantities,
/// and <c>QuantityRegistry</c> is what resolves it.
/// </remarks>
public sealed record class QuantityName : SemanticString<QuantityName> { }
