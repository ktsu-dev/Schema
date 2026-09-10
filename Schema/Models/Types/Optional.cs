// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// A value that may be absent.
/// </summary>
/// <remarks>
/// Absence is not failure. <see cref="Optional"/> says a value legitimately may not be there —
/// a lookup that found nothing — where <see cref="Result"/> says the call did not succeed. Using
/// one for the other is what turns "not found" into an error path, or an error into a silent
/// nothing.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
public class Optional : WrapperType { }
