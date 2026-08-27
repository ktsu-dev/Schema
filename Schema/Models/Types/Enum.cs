// Copyright (c) 2023-2026 ktsu-dev contributors

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

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Enum otherEnum && string.Equals(EnumName, otherEnum.EnumName, StringComparison.Ordinal);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(GetType(), EnumName.ToString());
}
