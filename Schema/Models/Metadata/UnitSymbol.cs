// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using ktsu.Semantics.Strings;

/// <summary>
/// Names the unit a member's values are measured in, as written in the schema file.
/// </summary>
/// <remarks>
/// Either a unit's symbol (<c>m/s</c>) or its name (<c>MeterPerSecond</c>). Symbols read better
/// and are what a person types; names exist because two symbols in the registry are ambiguous
/// (<c>g</c> is both gram and standard gravity, <c>rad</c> both radian and radiation absorbed
/// dose) and a schema must be able to say which one it meant.
/// <para>
/// The text is what is persisted, not a resolved unit object: it stays readable in a
/// <c>.schema.json</c>, and the conversion factors stay owned by
/// <c>ktsu.Semantics.Quantities</c> rather than being copied into every schema file that uses
/// them. <see cref="UnitRegistry"/> resolves it.
/// </para>
/// </remarks>
public sealed record class UnitSymbol : SemanticString<UnitSymbol> { }
