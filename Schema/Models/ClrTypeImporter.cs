// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Reflection;

using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

/// <summary>
/// Reads a .NET type into a schema, which is how a schema is built from code rather than from a
/// file.
/// </summary>
/// <remarks>
/// Its own class rather than part of <see cref="Schema"/>: the mapping it holds is the exact
/// inverse of what <see cref="Generation.CSharpCodeGenerator"/> emits, including the attributes
/// carrying everything a CLR type cannot say - a keyed map's key member, and a member's unit,
/// range, default, interpolation and network encoding. Keeping the two mappings as a matched pair
/// is what the generate-then-reimport round-trip test checks, and it reads better beside the
/// generator than buried in the schema's own API.
/// </remarks>
internal static class ClrTypeImporter
{
	/// <summary>
	/// Reads a .NET type into a schema as a class, with a member for each of its properties and
	/// fields.
	/// </summary>
	/// <param name="schema">The schema to add the class to.</param>
	/// <param name="type">The .NET type to read.</param>
	/// <returns>The added class if successful, null otherwise.</returns>
	internal static SchemaClass? Import(Schema schema, Type type)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(type);

		ClassName className = type.Name.As<ClassName>();
		SchemaClass? schemaClass = schema.AddClass(className);
		if (schemaClass is null)
		{
			return null;
		}

		schemaClass.TravelsAsBytes = type.GetCustomAttribute<Runtime.SchemaTravelsAsBytesAttribute>() is not null;

		foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			ImportMember(schema, schemaClass, property, property.PropertyType);
		}

		foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
		{
			ImportMember(schema, schemaClass, field, field.FieldType);
		}

		return schemaClass;
	}

	/// <summary>
	/// Reads one property or field into a member of the class being imported.
	/// </summary>
	/// <remarks>
	/// A property and a field are read the same way, and the only thing that differs is where the
	/// type comes from - which is why the caller supplies it rather than this asking which kind of
	/// member it was handed.
	/// </remarks>
	/// <param name="schema">The schema being built, for the types this member refers to.</param>
	/// <param name="schemaClass">The class the member belongs to.</param>
	/// <param name="info">The property or field to read, and the attributes on it.</param>
	/// <param name="memberType">The CLR type of the value it holds.</param>
	private static void ImportMember(Schema schema, SchemaClass schemaClass, MemberInfo info, Type memberType)
	{
		SchemaMember? member = schemaClass.AddMember(info.Name.As<MemberName>());
		if (member is null)
		{
			return;
		}

		BaseType? schemaType = GetOrCreateSchemaType(schema, memberType);
		if (schemaType is null)
		{
			return;
		}

		ApplySchemaKey(schemaType, info);
		member.SetType(schemaType);
		ApplyMemberMetadata(member, info);
	}

	private static BaseType? GetOrCreateSchemaType(Schema schema, Type type)
	{
		Ensure.NotNull(type);

		type = Nullable.GetUnderlyingType(type) ?? type;

		if (DirectTypeMappings.TryGetValue(type, out Func<BaseType>? create))
		{
			return create();
		}

		if (TryGetCollectionElementType(type, out Type? elementType, out ContainerName? container) && elementType is not null && container is not null)
		{
			BaseType element = GetOrCreateSchemaType(schema, elementType) ?? new None();
			return new Array() { ElementType = element, Container = container };
		}
		else if (type.IsEnum)
		{
			EnumName enumName = type.Name.As<EnumName>();
			SchemaEnum? schemaEnum = schema.GetEnum(enumName) ?? schema.AddEnum(enumName);
			if (schemaEnum is not null)
			{
				// Add enum values
				foreach (string enumValue in System.Enum.GetNames(type))
				{
					schemaEnum.TryAddValue(enumValue.As<EnumValueName>());
				}
				return new Enum() { EnumName = enumName };
			}
		}
		else if (IsSchemaClass(type))
		{
			ClassName className = type.Name.As<ClassName>();
			SchemaClass? schemaClass = schema.GetClass(className) ?? Import(schema, type);
			if (schemaClass is not null)
			{
				return new Object() { ClassName = className };
			}
		}

		return new None();
	}

	/// <summary>
	/// Says whether a CLR type is one a generated schema class would have been emitted as.
	/// </summary>
	/// <remarks>
	/// A reference type that is not a string, as it always was, and now a value type that says it
	/// travels as bytes: <see cref="Generation.CSharpCodeGenerator"/> emits a struct for exactly
	/// those classes and for no other reason, so the promise is what tells a generated struct apart
	/// from a value type this importer simply has no mapping for. Read as "any struct" instead, a
	/// <see cref="Guid"/> would come back as a class with no members - which says the wrong thing
	/// rather than nothing.
	/// <para>
	/// Reached only after the direct mappings and the enum and collection cases, so the value types
	/// that do have a mapping - the vectors, the colours, a date, a duration - are already spoken
	/// for.
	/// </para>
	/// </remarks>
	private static bool IsSchemaClass(Type type) =>
		(type.IsClass && type != typeof(string)) ||
		(type.IsValueType && type.GetCustomAttribute<Runtime.SchemaTravelsAsBytesAttribute>() is not null);

	/// <summary>
	/// Restores an array's key member from the attribute a generator wrote it into.
	/// </summary>
	/// <remarks>
	/// The CLR type of a keyed map carries the key's type but not which member it came from, so
	/// this is the only place that information survives a trip through generated code.
	/// </remarks>
	private static void ApplySchemaKey(BaseType schemaType, MemberInfo member)
	{
		if (schemaType is Array arrayType &&
			member.GetCustomAttribute<Runtime.SchemaKeyAttribute>() is Runtime.SchemaKeyAttribute key)
		{
			arrayType.Key = key.KeyMemberName.As<MemberName>();
		}
	}

	/// <summary>
	/// Restores a member's semantic metadata from the attributes a generator wrote it into.
	/// </summary>
	/// <remarks>
	/// A generated property's type carries none of this: a <c>float</c> measured in metres per
	/// second is the same <c>float</c> as one measured in nothing. The attributes are where it
	/// survives a trip through generated code, so this is the inverse of what the C# generator
	/// emits, and the generate-then-reimport round trip fails if either side changes alone.
	/// <para>
	/// An attribute that is absent leaves the property as it was rather than clearing it, so
	/// reimporting into a member that already carries metadata adds to it instead of erasing it.
	/// </para>
	/// </remarks>
	private static void ApplyMemberMetadata(SchemaMember member, MemberInfo info)
	{
		if (info.GetCustomAttribute<Runtime.SchemaUnitAttribute>() is Runtime.SchemaUnitAttribute unit)
		{
			member.Unit = unit.Unit.As<UnitSymbol>();
		}

		if (info.GetCustomAttribute<Runtime.SchemaRangeAttribute>() is Runtime.SchemaRangeAttribute range)
		{
			member.Range = new MemberRange { Minimum = range.Minimum, Maximum = range.Maximum, Wrap = range.Wrap };
		}

		if (info.GetCustomAttribute<Runtime.SchemaDefaultAttribute>() is Runtime.SchemaDefaultAttribute defaultValue)
		{
			// The constructor the generator chose is what says which kind of default this is, and
			// the boxed value is the only thing that still remembers which one that was.
			member.DefaultValue = defaultValue.Value switch
			{
				double number => new NumberDefault { Value = number },
				bool boolean => new BooleanDefault { Value = boolean },
				string text => new TextDefault { Value = text },
				_ => null,
			};
		}

		if (info.GetCustomAttribute<Runtime.SchemaInterpolationAttribute>() is Runtime.SchemaInterpolationAttribute interpolation)
		{
			member.Interpolation = interpolation.Mode;
		}

		if (info.GetCustomAttribute<Runtime.SchemaNetworkAttribute>() is Runtime.SchemaNetworkAttribute network)
		{
			member.Network = new MemberNetwork { Quantise = network.Quantise, Delta = network.Delta };
		}

		if (info.GetCustomAttribute<Runtime.SchemaEditorHintAttribute>() is Runtime.SchemaEditorHintAttribute editor)
		{
			member.Editor = editor.Hint.As<EditorHint>();
		}
	}

	/// <summary>
	/// The CLR types that map straight onto a schema type, with no further inspection.
	/// </summary>
	/// <remarks>
	/// A table rather than a chain of comparisons: it reads as the mapping it is, and it is the
	/// exact inverse of what a code generator emits, so the two can be checked against each other.
	/// The vector types are <see cref="System.Numerics"/> ones and the colours are the types this
	/// library provides, because the base class library has none - without them, reimporting
	/// generated code would turn a Vector3 member into an object referencing a class called
	/// "Vector3" and the generate-then-reimport round trip would not hold.
	/// </remarks>
	private static readonly Dictionary<Type, Func<BaseType>> DirectTypeMappings = new()
	{
		[typeof(string)] = () => new String(),
		[typeof(int)] = () => new Int(),
		[typeof(short)] = () => new Int(),
		[typeof(byte)] = () => new Int(),
		[typeof(long)] = () => new Long(),
		[typeof(float)] = () => new Float(),
		[typeof(double)] = () => new Double(),
		[typeof(decimal)] = () => new Double(),
		[typeof(bool)] = () => new Bool(),
		[typeof(System.DateTime)] = () => new DateTime(),
		[typeof(System.TimeSpan)] = () => new TimeSpan(),
		[typeof(System.Numerics.Vector2)] = () => new Vector2(),
		[typeof(System.Numerics.Vector3)] = () => new Vector3(),
		[typeof(System.Numerics.Vector4)] = () => new Vector4(),
		[typeof(Runtime.ColorRgb)] = () => new ColorRGB(),
		[typeof(Runtime.ColorRgba)] = () => new ColorRGBA(),
	};

	private static bool TryGetCollectionElementType(Type type, out Type? elementType, out ContainerName? container)
	{
		elementType = null;
		container = null;

		if (type == typeof(string))
		{
			return false;
		}

		if (type.IsArray)
		{
			elementType = type.GetElementType();
			container = Types.Array.VectorContainer.As<ContainerName>();
			return elementType is not null;
		}

		Type? dictionaryInterface = GetGenericInterface(type, typeof(IDictionary<,>));
		if (dictionaryInterface is not null)
		{
			elementType = dictionaryInterface.GetGenericArguments()[1];
			container = Types.Array.MapContainer.As<ContainerName>();
			return true;
		}

		Type? enumerableInterface = GetGenericInterface(type, typeof(IEnumerable<>));
		if (enumerableInterface is not null)
		{
			elementType = enumerableInterface.GetGenericArguments()[0];
			container = Types.Array.VectorContainer.As<ContainerName>();
			return true;
		}

		return false;
	}

	private static Type? GetGenericInterface(Type type, Type genericInterfaceDefinition) =>
		type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == genericInterfaceDefinition
			? type
			: System.Array.Find(type.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceDefinition);
}
