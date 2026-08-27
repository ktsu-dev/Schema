// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents an array type.
/// </summary>
public class Array : BaseType
{
	/// <summary>
	/// Gets or sets the element type of the array.
	/// </summary>
	public BaseType ElementType { get; init; } = new None();

	/// <summary>
	/// Gets or sets the container name.
	/// </summary>
	public ContainerName Container { get; set; } = new();

	/// <summary>
	/// Gets or sets the key member name.
	/// </summary>
	public MemberName Key { get; set; } = new();

	/// <summary>
	/// Gets a value indicating whether the array is keyed.
	/// </summary>
	[JsonIgnore]
	public bool IsKeyed => ElementType.IsObject && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Container);

	/// <summary>
	/// Tries to get the key member of the array.
	/// </summary>
	/// <param name="keyMember">The key member if found.</param>
	/// <returns>True if the key member is found; otherwise, false.</returns>
	public bool TryGetKeyMember(out SchemaMember? keyMember)
	{
		keyMember = null;
		if (ElementType is Object objectElement)
		{
			objectElement.Class?.TryGetMember(Key, out keyMember);
		}

		return keyMember is not null;
	}

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Array otherArray
			&& ElementType.Equals(otherArray.ElementType)
			&& string.Equals(Container, otherArray.Container, StringComparison.Ordinal)
			&& string.Equals(Key, otherArray.Key, StringComparison.Ordinal);

	/// <inheritdoc />
	public override int GetHashCode() =>
		HashCode.Combine(GetType(), ElementType, Container.ToString(), Key.ToString());
}
