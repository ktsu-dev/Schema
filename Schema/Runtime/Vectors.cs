// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

using System.Runtime.InteropServices;

/// <summary>
/// The CLR representation of the schema's <c>Vector2</c> over a component that is not a
/// <c>float</c>.
/// </summary>
/// <remarks>
/// <see cref="System.Numerics.Vector2"/> holds floats and nothing else, so it says a vector of
/// floats and only that. A vector of anything else has no counterpart in the base class library,
/// which is the same position the colours were in - so it is provided here for the same reason,
/// and the round trip stays exact either way: a vector of floats is still the
/// <see cref="System.Numerics"/> one.
/// <para>
/// The components are fields of <typeparamref name="T"/> rather than a phantom parameter, so
/// whether one of these travels as bytes is whatever is true of its component - which is what the
/// schema says about it too.
/// </para>
/// </remarks>
/// <typeparam name="T">What each component is.</typeparam>
/// <param name="X">The first component.</param>
/// <param name="Y">The second component.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vector2<T>(T X, T Y);

/// <summary>
/// The CLR representation of the schema's <c>Vector3</c> over a component that is not a
/// <c>float</c>.
/// </summary>
/// <remarks>
/// See <see cref="Vector2{T}"/> for why these types live in the library.
/// </remarks>
/// <typeparam name="T">What each component is.</typeparam>
/// <param name="X">The first component.</param>
/// <param name="Y">The second component.</param>
/// <param name="Z">The third component.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vector3<T>(T X, T Y, T Z);

/// <summary>
/// The CLR representation of the schema's <c>Vector4</c> over a component that is not a
/// <c>float</c>.
/// </summary>
/// <remarks>
/// See <see cref="Vector2{T}"/> for why these types live in the library.
/// </remarks>
/// <typeparam name="T">What each component is.</typeparam>
/// <param name="X">The first component.</param>
/// <param name="Y">The second component.</param>
/// <param name="Z">The third component.</param>
/// <param name="W">The fourth component.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vector4<T>(T X, T Y, T Z, T W);
