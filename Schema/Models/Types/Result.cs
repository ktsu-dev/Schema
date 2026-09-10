// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// The outcome of a call that can fail: either <see cref="WrapperType.ElementType"/> or an error.
/// </summary>
/// <remarks>
/// Fallibility is part of the return type rather than a separate declaration, so there is no
/// <c>throws</c> for a schema to express and no way for a signature to be silently fallible. A
/// function returns <see cref="Result"/> or it cannot fail.
/// <para>
/// A function that can fail and returns nothing is <c>Result&lt;Void&gt;</c>, which reads oddly
/// and is correct: the call still succeeds or does not.
/// </para>
/// </remarks>
public class Result : WrapperType { }
