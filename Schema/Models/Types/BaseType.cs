// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the base type for all schema types.
/// </summary>
/// <remarks>
/// This class is used as a base for all other schema types and provides common functionality.
/// </remarks>
[JsonDerivedType(typeof(None), nameof(None))]
[JsonDerivedType(typeof(Int), nameof(Int))]
[JsonDerivedType(typeof(Long), nameof(Long))]
[JsonDerivedType(typeof(Float), nameof(Float))]
[JsonDerivedType(typeof(Double), nameof(Double))]
[JsonDerivedType(typeof(String), nameof(String))]
[JsonDerivedType(typeof(DateTime), nameof(DateTime))]
[JsonDerivedType(typeof(TimeSpan), nameof(TimeSpan))]
[JsonDerivedType(typeof(Bool), nameof(Bool))]
[JsonDerivedType(typeof(Enum), nameof(Enum))]
[JsonDerivedType(typeof(Array), nameof(Array))]
[JsonDerivedType(typeof(Vector2), nameof(Vector2))]
[JsonDerivedType(typeof(Vector3), nameof(Vector3))]
[JsonDerivedType(typeof(Vector4), nameof(Vector4))]
[JsonDerivedType(typeof(ColorRGB), nameof(ColorRGB))]
[JsonDerivedType(typeof(ColorRGBA), nameof(ColorRGBA))]
[JsonDerivedType(typeof(Object), nameof(Object))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "TypeName")]
public abstract class BaseType : IEquatable<BaseType?>
{
	/// <summary>
	/// Gets or sets the parent member of the schema type.
	/// </summary>
	public SchemaMember? ParentMember { get; private set; }

	/// <summary>
	/// Associates this type with a schema member.
	/// </summary>
	/// <param name="schemaMember">The schema member to associate with.</param>
	public void AssociateWith(SchemaMember schemaMember) => ParentMember = schemaMember;

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="other">The object to compare with the current object.</param>
	/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
	public bool Equals(BaseType? other) => ReferenceEquals(this, other) || ((other?.GetType() == GetType()) && (other.ToString() != ToString()));

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object? obj) => Equals(obj as BaseType);

	/// <summary>
	/// Serves as the default hash function.
	/// </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode() => HashCode.Combine(ToString());

	/// <summary>
	/// Returns a string representation of the type.
	/// </summary>
	/// <returns>The name of the type.</returns>
	public override string ToString() => GetType().Name ?? string.Empty;

	/// <summary>
	/// Creates an instance of a type from its string representation.
	/// </summary>
	/// <param name="str">The string representation of the type.</param>
	/// <returns>An instance of the type if found; otherwise, null.</returns>
	public static object? CreateFromString(string? str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return null;
		}

		Type? type = typeof(BaseType).Assembly.GetTypes()
			.FirstOrDefault(t => t.IsSubclassOf(typeof(BaseType)) && t.Name == str);
		return type is null ? null : Activator.CreateInstance(type);
	}

	/// <summary>
	/// Gets the display name of the type.
	/// </summary>
	public string DisplayName
	{
		get
		{
			if (this is Array array)
			{
				return $"{nameof(Array)}({array.ElementType.DisplayName})";
			}
			else if (this is Enum enumType)
			{
				return $"{nameof(Enum)}({enumType.EnumName})";
			}

			return ToString();
		}
	}

	private static readonly HashSet<Type> PrimitiveTypes =
	[
		typeof(Int),
		typeof(Long),
		typeof(Float),
		typeof(Double),
		typeof(String),
		typeof(Bool),
	];

	private static readonly HashSet<Type> BuiltInTypes =
	[
		typeof(None),
		typeof(Int),
		typeof(Long),
		typeof(Float),
		typeof(Double),
		typeof(String),
		typeof(DateTime),
		typeof(TimeSpan),
		typeof(Bool),
		typeof(Vector2),
		typeof(Vector3),
		typeof(Vector4),
		typeof(ColorRGB),
		typeof(ColorRGBA),
	];

	/// <summary>
	/// Gets a value indicating whether the type is built-in.
	/// </summary>
	public bool IsBuiltIn => BuiltInTypes.Contains(GetType());

	/// <summary>
	/// Gets a value indicating whether the type is primitive.
	/// </summary>
	public bool IsPrimitive => PrimitiveTypes.Contains(GetType());

	/// <summary>
	/// Gets a value indicating whether the type is integral.
	/// </summary>
	public bool IsIntegral => this switch
	{
		Int => true,
		Long => true,
		_ => false,
	};

	/// <summary>
	/// Gets a value indicating whether the type is decimal.
	/// </summary>
	public bool IsDecimal => this switch
	{
		Float => true,
		Double => true,
		_ => false,
	};

	/// <summary>
	/// Gets a value indicating whether the type is numeric.
	/// </summary>
	public bool IsNumeric => IsIntegral || IsDecimal;

	/// <summary>
	/// Gets a value indicating whether the type is a container.
	/// </summary>
	public bool IsContainer => this switch
	{
		Array => true,
		_ => false,
	};

	/// <summary>
	/// Gets a value indicating whether the type is an object.
	/// </summary>
	public bool IsObject => this is Object;

	/// <summary>
	/// Gets a value indicating whether the type is a system object.
	/// </summary>
	public bool IsSystemObject => this is SystemObject;

	/// <summary>
	/// Gets a value indicating whether the type is an array.
	/// </summary>
	public bool IsArray => this is Array;

	/// <summary>
	/// Gets a value indicating whether the type is a complex array.
	/// </summary>
	public bool IsComplexArray => this is Array array && array.ElementType.IsObject;

	/// <summary>
	/// Gets a value indicating whether the type is a primitive array.
	/// </summary>
	public bool IsPrimitiveArray => this is Array array && array.ElementType.IsPrimitive;
}
