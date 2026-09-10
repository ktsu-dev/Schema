// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// A borrowed view over a contiguous sequence of <see cref="WrapperType.ElementType"/>.
/// </summary>
/// <remarks>
/// A span is how a sequence crosses an API boundary without anyone owning it: the callee may read
/// or write through it for the duration of the call and must not retain it. That rule is what lets
/// the schema describe an API with no lifetime annotations at all — there is only one lifetime a
/// borrow can have, so there is nothing to annotate.
/// <para>
/// Contrast <see cref="Array"/>, which is a stored collection a class owns. A span is never a
/// member's type; an array is never a parameter's.
/// </para>
/// </remarks>
public class Span : WrapperType { }
