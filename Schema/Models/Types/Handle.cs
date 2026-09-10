// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// An opaque, generation-counted reference to a resource the engine owns.
/// </summary>
/// <remarks>
/// A handle is how ownership stays on one side of a boundary. The caller receives an identifier it
/// can hold indefinitely and pass back; it never receives the resource, so there is nothing for it
/// to free and no question of when. A stale handle is detectable rather than undefined, because the
/// generation counter no longer matches.
/// <para>
/// This is the second half of the reason the schema needs no ownership annotations: anything a
/// caller may keep is a <see cref="Handle"/>, and anything it may not is a <see cref="Span"/>.
/// </para>
/// </remarks>
public class Handle : WrapperType { }
