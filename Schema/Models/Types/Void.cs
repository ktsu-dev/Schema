// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// Represents the absence of a value: a function that returns nothing.
/// </summary>
/// <remarks>
/// Distinct from <see cref="None"/>, which means no type has been chosen yet — a valid intermediate
/// editing state, and not generatable. <see cref="Void"/> is a decision: this function returns
/// nothing. A generator must emit <c>void</c> for it and refuse <see cref="None"/>.
/// </remarks>
public class Void : BaseType { }
