// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

/// <summary>
/// The CLR representation of the schema's <c>ColorRGB</c> type.
/// </summary>
/// <remarks>
/// The vector types map onto <see cref="System.Numerics"/>, but the colour types have no
/// counterpart in the base class library. Generated code needs some type to name, and a generator
/// inventing its own would break the round trip that reimports generated types back into a schema:
/// the reimported member would reference a class rather than the colour type it started as.
/// Providing the type here keeps that round trip exact and keeps every generator agreeing on it.
/// </remarks>
/// <param name="R">The red component.</param>
/// <param name="G">The green component.</param>
/// <param name="B">The blue component.</param>
public readonly record struct ColorRgb(float R, float G, float B);

/// <summary>
/// The CLR representation of the schema's <c>ColorRGBA</c> type.
/// </summary>
/// <remarks>
/// See <see cref="ColorRgb"/> for why these types live in the library.
/// </remarks>
/// <param name="R">The red component.</param>
/// <param name="G">The green component.</param>
/// <param name="B">The blue component.</param>
/// <param name="A">The alpha component.</param>
public readonly record struct ColorRgba(float R, float G, float B, float A);
