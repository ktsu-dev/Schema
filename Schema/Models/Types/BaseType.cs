// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;
using ktsu.Schema.Contracts;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

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
public abstract class BaseType : IEquatable<BaseType?>, ISchemaType
{
	/// <summary>
	/// Gets or sets the parent member of the schema type.
	/// </summary>
	[JsonIgnore]
	public SchemaMember? ParentMember { get; private set; }

	/// <summary>
	/// Gets the name identifying which type this is.
	/// </summary>
	/// <remarks>
	/// Derived from the CLR type name, which is exactly what the <c>[JsonDerivedType]</c>
	/// discriminators above are declared as, so this always matches the <c>TypeName</c> written
	/// to the file. A test asserts that correspondence.
	/// </remarks>
	[JsonIgnore]
	public BaseTypeName TypeName => GetType().Name.As<BaseTypeName>();

	/// <remarks>
	/// Explicit because the contract exposes the parent as <see cref="ISchemaMember"/> while the
	/// model exposes the concrete <see cref="SchemaMember"/>; both are the same object.
	/// </remarks>
	ISchemaMember? ISchemaType.ParentMember => ParentMember;

	/// <summary>
	/// Associates this type with a schema member.
	/// </summary>
	/// <remarks>
	/// Virtual so a type that contains another type can pass the association on. Without that, an
	/// <see cref="Array"/>'s element type has no parent member and therefore no route back to the
	/// schema, so <see cref="Object.Class"/> and <see cref="Array.TryGetKeyMember"/> silently
	/// resolve to nothing for anything nested inside an array.
	/// </remarks>
	/// <param name="schemaMember">The schema member to associate with.</param>
	public virtual void AssociateWith(SchemaMember schemaMember) => ParentMember = schemaMember;

	/// <summary>
	/// Determines whether the specified type is equal to the current type.
	/// </summary>
	/// <remarks>
	/// Two types are equal when they are the same CLR type and their type-specific state
	/// matches. Types that carry no state - the primitives, the vectors and colors - are equal
	/// to any other instance of the same type; the types that do carry state compare it in
	/// <see cref="EqualsCore(BaseType)"/>.
	/// </remarks>
	/// <param name="other">The type to compare with the current type.</param>
	/// <returns>True if the specified type is equal to the current type; otherwise, false.</returns>
	public bool Equals(BaseType? other) =>
		ReferenceEquals(this, other) || (other is not null && other.GetType() == GetType() && EqualsCore(other));

	/// <summary>
	/// Compares the type-specific state of two instances that are already known to be of the
	/// same CLR type.
	/// </summary>
	/// <remarks>
	/// The default implementation returns true, which is correct for every stateless type.
	/// Derived types that carry state - <see cref="Object"/>, <see cref="Enum"/> and
	/// <see cref="Array"/> - override this and must keep <see cref="GetHashCode"/> in step.
	/// </remarks>
	/// <param name="other">The instance to compare against; guaranteed to be the same CLR type as this one.</param>
	/// <returns>True if the type-specific state matches; otherwise, false.</returns>
	protected virtual bool EqualsCore(BaseType other) => true;

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object? obj) => Equals(obj as BaseType);

	/// <summary>
	/// Serves as the default hash function.
	/// </summary>
	/// <remarks>
	/// Mirrors <see cref="Equals(BaseType?)"/>: the CLR type identifies the type, and
	/// <see cref="GetHashCodeCore"/> contributes whatever state
	/// <see cref="EqualsCore(BaseType)"/> compares. Derived types override that hook rather
	/// than this method, so equality and hashing cannot drift apart.
	/// </remarks>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode() => HashCode.Combine(GetType(), GetHashCodeCore());

	/// <summary>
	/// Contributes the type-specific state to the hash code.
	/// </summary>
	/// <remarks>
	/// The default returns a constant, which is correct for every stateless type. A derived
	/// type that overrides <see cref="EqualsCore(BaseType)"/> must override this too, hashing
	/// exactly the state that method compares.
	/// </remarks>
	/// <returns>A hash of the type-specific state.</returns>
	protected virtual int GetHashCodeCore() => 0;

	/// <summary>
	/// Determines whether two types are equal.
	/// </summary>
	/// <param name="left">The first type to compare.</param>
	/// <param name="right">The second type to compare.</param>
	/// <returns>True if the types are equal; otherwise, false.</returns>
	public static bool operator ==(BaseType? left, BaseType? right) =>
		left is null ? right is null : left.Equals(right);

	/// <summary>
	/// Determines whether two types are not equal.
	/// </summary>
	/// <param name="left">The first type to compare.</param>
	/// <param name="right">The second type to compare.</param>
	/// <returns>True if the types are not equal; otherwise, false.</returns>
	public static bool operator !=(BaseType? left, BaseType? right) => !(left == right);

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
	[JsonIgnore]
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
	/// Creates fresh instances of every built-in type.
	/// </summary>
	/// <returns>A new instance of each built-in type.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Allocates new instances on each call, which is not appropriate for a property.")]
	public static IEnumerable<BaseType> GetBuiltInTypes() =>
	[
		new None(),
		new Bool(),
		new Int(),
		new Long(),
		new Float(),
		new Double(),
		new String(),
		new DateTime(),
		new TimeSpan(),
		new Vector2(),
		new Vector3(),
		new Vector4(),
		new ColorRGB(),
		new ColorRGBA(),
	];

	/// <summary>
	/// Gets a value indicating whether the type is built-in.
	/// </summary>
	[JsonIgnore]
	public bool IsBuiltIn => BuiltInTypes.Contains(GetType());

	/// <summary>
	/// Gets a value indicating whether the type is primitive.
	/// </summary>
	[JsonIgnore]
	public bool IsPrimitive => PrimitiveTypes.Contains(GetType());

	/// <summary>
	/// Gets a value indicating whether the type is integral.
	/// </summary>
	[JsonIgnore]
	public bool IsIntegral => this switch
	{
		Int => true,
		Long => true,
		_ => false,
	};

	/// <summary>
	/// Gets a value indicating whether the type is decimal.
	/// </summary>
	[JsonIgnore]
	public bool IsDecimal => this switch
	{
		Float => true,
		Double => true,
		_ => false,
	};

	/// <summary>
	/// Gets a value indicating whether the type is numeric.
	/// </summary>
	[JsonIgnore]
	public bool IsNumeric => IsIntegral || IsDecimal;

	/// <summary>
	/// Gets a value indicating whether the type is a container.
	/// </summary>
	[JsonIgnore]
	public bool IsContainer => this switch
	{
		Array => true,
		_ => false,
	};

	/// <summary>
	/// Gets a value indicating whether the type is an object.
	/// </summary>
	[JsonIgnore]
	public bool IsObject => this is Object;

	/// <summary>
	/// Gets a value indicating whether the type is a system object.
	/// </summary>
	[JsonIgnore]
	public bool IsSystemObject => this is SystemObject;

	/// <summary>
	/// Gets a value indicating whether the type is an array.
	/// </summary>
	[JsonIgnore]
	public bool IsArray => this is Array;

	/// <summary>
	/// Gets a value indicating whether the type is a complex array.
	/// </summary>
	[JsonIgnore]
	public bool IsComplexArray => this is Array array && array.ElementType.IsObject;

	/// <summary>
	/// Gets a value indicating whether the type is a primitive array.
	/// </summary>
	[JsonIgnore]
	public bool IsPrimitiveArray => this is Array array && array.ElementType.IsPrimitive;
}
