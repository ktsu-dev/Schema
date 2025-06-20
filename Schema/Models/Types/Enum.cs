// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models.Types;

using ktsu.Schema.Models.Names;

/// <summary>
/// Represents an enumeration type.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "We're mimicing the type")]
public class Enum : BaseType
{
	/// <summary>
	/// Gets or sets the name of the enumeration.
	/// </summary>
	public EnumName EnumName { get; init; } = new();
}
