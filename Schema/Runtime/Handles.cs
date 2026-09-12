// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

using System.Runtime.InteropServices;

/// <summary>
/// The CLR representation of the schema's <c>Handle</c> type: an index and a generation naming
/// something the holder does not own.
/// </summary>
/// <remarks>
/// Here for the same reason <see cref="ColorRgb"/> is - the base class library has nothing that
/// means this, and a generator inventing its own would break the round trip that reimports
/// generated types back into a schema.
/// <para>
/// <typeparamref name="T"/> is carried but never stored, which is the whole point: a handle to a
/// texture and a handle to a mesh are an index and a generation either way, and what stops one
/// being passed where the other belongs is the type parameter rather than anything in the bytes.
/// That is also what keeps it copyable whole whatever it names, which is why a class that travels
/// as bytes may hold one.
/// </para>
/// </remarks>
/// <typeparam name="T">What the handle names.</typeparam>
/// <param name="Index">Where the resource sits in whatever owns it.</param>
/// <param name="Generation">Which occupant of that slot this names, so a stale handle is
/// detectable rather than undefined.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Handle<T>(uint Index, uint Generation);
