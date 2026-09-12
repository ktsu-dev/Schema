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
	/// The name of the property a generated semantic type holds its value in.
	/// </summary>
	private const string SemanticValueName = "Value";

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
	/// Reads a .NET interface into a schema, with a function for each of its methods.
	/// </summary>
	/// <param name="schema">The schema to add the interface to.</param>
	/// <param name="type">The .NET interface to read.</param>
	/// <returns>The added interface, or null when the type is not one.</returns>
	internal static SchemaInterface? ImportDeclaration(Schema schema, Type type)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(type);

		return type.IsInterface && ImportInterface(schema, type) is Types.Interface reference
			? schema.GetInterface(reference.InterfaceName)
			: null;
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
		ApplyMetadata(member, info);
	}

	/// <summary>
	/// Reads a CLR type as the schema type it stands for.
	/// </summary>
	/// <remarks>
	/// Ordered, and the order is the mapping: the fixed correspondences first, then a collection,
	/// then the declarations a generator emits under a name of their own, then the generic types
	/// that say how a value is carried, and a class last because it is the broadest test. Reading
	/// a value type as a class is what <see cref="IsSchemaClass"/> gates, so the structs above it
	/// are all spoken for before it is reached.
	/// </remarks>
	private static BaseType? GetOrCreateSchemaType(Schema schema, Type type)
	{
		Ensure.NotNull(type);

		type = Nullable.GetUnderlyingType(type) ?? type;

		if (DirectTypeMappings.TryGetValue(type, out Func<BaseType>? create))
		{
			return create();
		}

		if (TryGetCollectionElementType(type, out Type? elementType, out ContainerName? container) &&
			elementType is not null && container is not null)
		{
			return new Array() { ElementType = Element(schema, elementType), Container = container };
		}

		if (type.IsEnum)
		{
			return ImportEnum(schema, type);
		}

		if (type.GetCustomAttribute<Runtime.SchemaSemanticTypeAttribute>() is Runtime.SchemaSemanticTypeAttribute semantic)
		{
			return ImportSemanticType(schema, type, semantic);
		}

		if (type.IsGenericType &&
			GenericTypeMappings.TryGetValue(type.GetGenericTypeDefinition(), out Func<Schema, Type[], BaseType>? build))
		{
			return build(schema, type.GetGenericArguments());
		}

		if (type.IsInterface)
		{
			return ImportInterface(schema, type);
		}

		return IsSchemaClass(type) ? ImportClass(schema, type) : new None();
	}

	/// <summary>
	/// Reads a CLR type as whatever it is the element of, which is <c>None</c> when it is nothing
	/// this recognises.
	/// </summary>
	private static BaseType Element(Schema schema, Type type) =>
		GetOrCreateSchemaType(schema, type) ?? new None();

	/// <summary>
	/// Reads a generated enum back into the schema, declaring it if this is the first thing to
	/// name it.
	/// </summary>
	private static BaseType ImportEnum(Schema schema, Type type)
	{
		EnumName name = type.Name.As<EnumName>();
		SchemaEnum? schemaEnum = schema.GetEnum(name) ?? schema.AddEnum(name);
		if (schemaEnum is null)
		{
			return new None();
		}

		foreach (string value in System.Enum.GetNames(type))
		{
			schemaEnum.TryAddValue(value.As<EnumValueName>());
		}

		return new Enum() { EnumName = name };
	}

	/// <summary>
	/// Reads a generated class back into the schema, declaring it if this is the first thing to
	/// name it.
	/// </summary>
	private static BaseType ImportClass(Schema schema, Type type)
	{
		ClassName name = type.Name.As<ClassName>();
		SchemaClass? schemaClass = schema.GetClass(name) ?? Import(schema, type);

		return schemaClass is not null ? new Object() { ClassName = name } : new None();
	}

	/// <summary>
	/// Reads a generated interface back into the schema, declaring it if this is the first thing
	/// to name it.
	/// </summary>
	/// <remarks>
	/// No attribute is needed to recognise one: a CLR interface is already distinct from a class
	/// and a struct, which is exactly what the schema means by one. The name is taken as written,
	/// the same as a class's, which is why the generator adds no <c>I</c> prefix - one would have
	/// to be stripped here, and stripping cannot tell a prefix from a first letter.
	/// </remarks>
	private static Types.Interface ImportInterface(Schema schema, Type type)
	{
		InterfaceName name = type.Name.As<InterfaceName>();
		Types.Interface reference = new() { InterfaceName = name };

		if (schema.GetInterface(name) is not null)
		{
			return reference;
		}

		SchemaInterface? declaration = schema.AddInterface(name);
		if (declaration is null)
		{
			return reference;
		}

		foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
		{
			ImportFunction(schema, declaration, method);
		}

		return reference;
	}

	/// <summary>
	/// Reads one method into a function of the interface being imported.
	/// </summary>
	private static void ImportFunction(Schema schema, SchemaInterface declaration, MethodInfo method)
	{
		SchemaFunction? function = declaration.AddFunction(method.Name.As<FunctionName>());
		if (function is null)
		{
			return;
		}

		function.IsQuery = method.GetCustomAttribute<Runtime.SchemaQueryAttribute>() is not null;
		function.SetReturnType(Element(schema, method.ReturnType));

		foreach (ParameterInfo parameter in method.GetParameters())
		{
			ImportParameter(schema, function, parameter);
		}
	}

	/// <summary>
	/// Reads one parameter, whose direction is carried by the compiled signature rather than by an
	/// attribute.
	/// </summary>
	/// <remarks>
	/// <c>out</c>, <c>ref</c> and <c>in</c> are all by-reference, so the type is the same three
	/// ways and what tells them apart is which of the two flags the parameter carries. An ordinary
	/// by-value parameter is <see cref="ParameterDirection.In"/>, which is what the generator emits
	/// for one.
	/// </remarks>
	private static void ImportParameter(Schema schema, SchemaFunction function, ParameterInfo parameter)
	{
		SchemaParameter? added = function.AddParameter((parameter.Name ?? string.Empty).As<ParameterName>());
		if (added is null)
		{
			return;
		}

		Type carried = parameter.ParameterType;
		if (carried.IsByRef)
		{
			carried = carried.GetElementType() ?? carried;
			added.Direction = parameter.IsOut
				? ParameterDirection.Out
				: parameter.IsIn ? ParameterDirection.In : ParameterDirection.InOut;
		}
		else if (carried.IsGenericType && carried.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
		{
			// On a view, direction describes the elements rather than the view, so the read-only
			// one is what an In span was emitted as.
			added.Direction = ParameterDirection.In;
		}
		else if (carried.IsGenericType && carried.GetGenericTypeDefinition() == typeof(System.Span<>))
		{
			added.Direction = ParameterDirection.Out;
		}

		added.SetType(Element(schema, carried));
	}

	/// <summary>
	/// Reads a generated semantic type back into the schema, declaring it if this is the first
	/// thing to name it.
	/// </summary>
	/// <remarks>
	/// The struct holds the representation whether or not it refines another semantic type, so the
	/// shape alone cannot say which - the attribute is what carries it, and the chain is rebuilt by
	/// recursing into the type it names. A type already declared is left as it is, which is what
	/// makes two members naming the same semantic type describe one declaration rather than two.
	/// </remarks>
	private static Semantic ImportSemanticType(Schema schema, Type type, Runtime.SchemaSemanticTypeAttribute attribute)
	{
		SemanticTypeName name = type.Name.As<SemanticTypeName>();
		Semantic reference = new() { SemanticTypeName = name };

		if (schema.GetSemanticType(name) is not null)
		{
			return reference;
		}

		SchemaSemanticType? declaration = schema.AddSemanticType(name);
		if (declaration is null)
		{
			return reference;
		}

		BaseType underlying = attribute.Refines is Type refined
			? Element(schema, refined)
			: RepresentationOf(schema, type);

		declaration.SetUnderlyingType(underlying);
		ApplyMetadata(declaration, type);
		return reference;
	}

	/// <summary>
	/// Reads what a generated semantic type is represented as, off the one value it holds.
	/// </summary>
	private static BaseType RepresentationOf(Schema schema, Type type) =>
		type.GetProperty(SemanticValueName, BindingFlags.Public | BindingFlags.Instance) is PropertyInfo value
			? Element(schema, value.PropertyType)
			: new None();

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
	/// Restores semantic metadata from the attributes a generator wrote it into.
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
	/// <para>
	/// A semantic type carries the same six and is restored by the same code, which is what
	/// <see cref="ISchemaMetadataCarrier"/> is for. A <see cref="Type"/> is a
	/// <see cref="MemberInfo"/>, so the two callers differ only in what they hand it.
	/// </para>
	/// </remarks>
	private static void ApplyMetadata(ISchemaMetadataCarrier carrier, MemberInfo info)
	{
		if (info.GetCustomAttribute<Runtime.SchemaUnitAttribute>() is Runtime.SchemaUnitAttribute unit)
		{
			carrier.Unit = unit.Unit.As<UnitSymbol>();
		}

		if (info.GetCustomAttribute<Runtime.SchemaRangeAttribute>() is Runtime.SchemaRangeAttribute range)
		{
			carrier.Range = new MemberRange { Minimum = range.Minimum, Maximum = range.Maximum, Wrap = range.Wrap };
		}

		if (info.GetCustomAttribute<Runtime.SchemaDefaultAttribute>() is Runtime.SchemaDefaultAttribute defaultValue)
		{
			// The constructor the generator chose is what says which kind of default this is, and
			// the boxed value is the only thing that still remembers which one that was.
			carrier.DefaultValue = defaultValue.Value switch
			{
				double number => new NumberDefault { Value = number },
				bool boolean => new BooleanDefault { Value = boolean },
				string text => new TextDefault { Value = text },
				_ => null,
			};
		}

		if (info.GetCustomAttribute<Runtime.SchemaInterpolationAttribute>() is Runtime.SchemaInterpolationAttribute interpolation)
		{
			carrier.Interpolation = interpolation.Mode;
		}

		if (info.GetCustomAttribute<Runtime.SchemaNetworkAttribute>() is Runtime.SchemaNetworkAttribute network)
		{
			carrier.Network = new MemberNetwork { Quantise = network.Quantise, Delta = network.Delta };
		}

		if (info.GetCustomAttribute<Runtime.SchemaEditorHintAttribute>() is Runtime.SchemaEditorHintAttribute editor)
		{
			carrier.Editor = editor.Hint.As<EditorHint>();
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
		[typeof(void)] = () => new Types.Void(),
		[typeof(System.DateTime)] = () => new DateTime(),
		[typeof(System.TimeSpan)] = () => new TimeSpan(),
		[typeof(System.Numerics.Vector2)] = () => new Vector2(),
		[typeof(System.Numerics.Vector3)] = () => new Vector3(),
		[typeof(System.Numerics.Vector4)] = () => new Vector4(),
		[typeof(Runtime.ColorRgb)] = () => new ColorRGB(),
		[typeof(Runtime.ColorRgba)] = () => new ColorRGBA(),
	};

	/// <summary>
	/// The generic types a generator emits, and which schema type each one is.
	/// </summary>
	/// <remarks>
	/// Separate from <see cref="DirectTypeMappings"/> because these say nothing until their
	/// argument is read: a <c>Vector3&lt;double&gt;</c> and a <c>Vector3&lt;Kilograms&gt;</c> are
	/// one entry here and two schema types, so what each maps to is a function of its arguments
	/// rather than a fixed answer.
	/// <para>
	/// Both spans are one schema <c>Span</c>, because which of the two a view is spelled as says
	/// the parameter's direction rather than anything about the type - the direction is restored
	/// from it in <see cref="ImportParameter"/>. Both results are one schema <c>Result</c>, and the
	/// arity is what says whether a successful call produced anything: C# has no <c>void</c> type
	/// argument, so a <c>Result&lt;Void&gt;</c> is the form with no value argument at all.
	/// </para>
	/// </remarks>
	private static readonly Dictionary<Type, Func<Schema, Type[], BaseType>> GenericTypeMappings = new()
	{
		[typeof(Runtime.Vector2<>)] = (schema, arguments) => new Vector2() { ElementType = Element(schema, arguments[0]) },
		[typeof(Runtime.Vector3<>)] = (schema, arguments) => new Vector3() { ElementType = Element(schema, arguments[0]) },
		[typeof(Runtime.Vector4<>)] = (schema, arguments) => new Vector4() { ElementType = Element(schema, arguments[0]) },
		[typeof(Runtime.Handle<>)] = (schema, arguments) => new Handle() { ElementType = Element(schema, arguments[0]) },
		[typeof(Runtime.Optional<>)] = (schema, arguments) => new Optional() { ElementType = Element(schema, arguments[0]) },
		[typeof(System.Span<>)] = (schema, arguments) => new Span() { ElementType = Element(schema, arguments[0]) },
		[typeof(ReadOnlySpan<>)] = (schema, arguments) => new Span() { ElementType = Element(schema, arguments[0]) },
		[typeof(Runtime.Result<>)] = (_, _) => new Result() { ElementType = new Types.Void() },
		[typeof(Runtime.Result<,>)] = (schema, arguments) => new Result() { ElementType = Element(schema, arguments[0]) },
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
