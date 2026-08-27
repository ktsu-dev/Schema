// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

/// <summary>
/// Records which member of the element type a generated dictionary is keyed by.
/// </summary>
/// <remarks>
/// A schema's keyed map knows the member it keys on; the <c>Dictionary&lt;TKey, TValue&gt;</c> a
/// generator emits for it does not - the key is just a type. Without this attribute, reimporting
/// generated code would produce an array with no key, and the schema would come back subtly
/// different from the one that produced it. The attribute is what makes that round trip exact.
/// </remarks>
/// <param name="keyMemberName">The name of the element type's member that supplies the key.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SchemaKeyAttribute(string keyMemberName) : Attribute
{
	/// <summary>
	/// Gets the name of the element type's member that supplies the key.
	/// </summary>
	public string KeyMemberName { get; } = keyMemberName;
}
