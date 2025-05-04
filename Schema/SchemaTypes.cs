// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using System.Text.Json.Serialization;

/// <summary>
/// Contains various schema types used in the application.
/// </summary>
public static class SchemaTypes
{
	/// <summary>
	/// Gets the type qualifier string.
	/// </summary>
	public static string TypeQualifier => $"{typeof(SchemaTypes).FullName}+";

	/// <summary>
	/// Represents a type with no value.
	/// </summary>
	public class None : BaseType { }

	/// <summary>
	/// Represents an integer type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
	public class Int : BaseType { }

	/// <summary>
	/// Represents a long integer type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
	public class Long : BaseType { }

	/// <summary>
	/// Represents a floating-point type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
	public class Float : BaseType { }

	/// <summary>
	/// Represents a double-precision floating-point type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
	public class Double : BaseType { }

	/// <summary>
	/// Represents a string type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
	public class String : BaseType { }

	/// <summary>
	/// Represents a date and time type.
	/// </summary>
	public class DateTime : BaseType { }

	/// <summary>
	/// Represents a time span type.
	/// </summary>
	public class TimeSpan : BaseType { }

	/// <summary>
	/// Represents a boolean type.
	/// </summary>
	public class Bool : BaseType { }

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
	}

	/// <summary>
	/// Represents an object type.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
	public class Object : BaseType
	{
		private SchemaClass? internalClass;

		/// <summary>
		/// Gets the schema class associated with the object.
		/// </summary>
		public SchemaClass? Class
		{
			get
			{
				if (!string.IsNullOrEmpty(ClassName) && internalClass?.Name != ClassName)
				{
					ParentMember?.ParentSchema?.TryGetClass(ClassName, out internalClass);
				}

				return internalClass;
			}
		}

		/// <summary>
		/// Gets or sets the class name.
		/// </summary>
		public ClassName ClassName { get; init; } = new();

		/// <summary>
		/// Returns a string representation of the object.
		/// </summary>
		/// <returns>The class name.</returns>
		public override string ToString() => ClassName;
	}

	/// <summary>
	/// Represents a system object type.
	/// </summary>
	public class SystemObject : Object { }

	/// <summary>
	/// Represents a vector type.
	/// </summary>
	public class Vector : SystemObject { }

	/// <summary>
	/// Represents a 2D vector type.
	/// </summary>
	public class Vector2 : Vector { }

	/// <summary>
	/// Represents a 3D vector type.
	/// </summary>
	public class Vector3 : Vector { }

	/// <summary>
	/// Represents a 4D vector type.
	/// </summary>
	public class Vector4 : Vector { }

	/// <summary>
	/// Represents an RGB color type.
	/// </summary>
	public class ColorRGB : Vector3 { }

	/// <summary>
	/// Represents an RGBA color type.
	/// </summary>
	public class ColorRGBA : Vector4 { }

	/// <summary>
	/// Gets a set of built-in types.
	/// </summary>
	public static HashSet<Type> BuiltIn =>
	[
		typeof(Int),
		typeof(Long),
		typeof(Float),
		typeof(Double),
		typeof(String),
		typeof(DateTime),
		typeof(TimeSpan),
		typeof(Bool),
		typeof(Enum),
		typeof(Array),
		typeof(Vector2),
		typeof(Vector3),
		typeof(Vector4),
		typeof(ColorRGB),
		typeof(ColorRGBA),
	];

	/// <summary>
	/// Gets a set of primitive types.
	/// </summary>
	public static HashSet<Type> Primitives =>
	[
		typeof(Int),
		typeof(Long),
		typeof(Float),
		typeof(Double),
		typeof(String),
		typeof(DateTime),
		typeof(TimeSpan),
		typeof(Bool),
	];

	/// <summary>
	/// Gets a dictionary mapping schema types to system types.
	/// </summary>
	public static Dictionary<Type, Type> SystemTypes => new()
	{
		{ typeof(Vector2), typeof(System.Numerics.Vector2) },
		{ typeof(Vector3), typeof(System.Numerics.Vector3) },
		{ typeof(Vector4), typeof(System.Numerics.Vector4) },
		{ typeof(ColorRGB), typeof(System.Numerics.Vector3) },
		{ typeof(ColorRGBA), typeof(System.Numerics.Vector4) },
	};

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
	public abstract class BaseType : SchemaMemberChild<BaseTypeName>, IEquatable<BaseType?>
	{
		/// <summary>
		/// Throws an InvalidOperationException as types cannot be removed from a member.
		/// </summary>
		/// <returns>Always throws an exception.</returns>
		public override bool TryRemove() => throw new InvalidOperationException("Cannot remove a type from a member");

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="other">The object to compare with the current object.</param>
		/// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
		public bool Equals(BaseType? other) => ReferenceEquals(this, other) || ((other?.GetType()) == GetType() && other.ToString() != ToString());

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

			var type = typeof(SchemaTypes).GetNestedTypes().FirstOrDefault(t => t.Name == str);
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

		/// <summary>
		/// Gets a value indicating whether the type is built-in.
		/// </summary>
		public bool IsBuiltIn => BuiltIn.Contains(GetType());

		/// <summary>
		/// Gets a value indicating whether the type is primitive.
		/// </summary>
		public bool IsPrimitive => Primitives.Contains(GetType());

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
}
