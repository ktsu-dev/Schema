// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using ktsu.Semantics.Strings;

/// <summary>
/// Suggests how an editor should present a member.
/// </summary>
/// <remarks>
/// Free text on purpose. The set of useful controls is open — a dial, a colour wheel, a curve,
/// a file picker — and an enum here would have to be extended in this library every time a
/// consumer invented one. An editor that does not recognise a hint falls back to the control
/// the member's type implies, so an unknown hint costs nothing.
/// </remarks>
public sealed record class EditorHint : SemanticString<EditorHint> { }
