// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json.Serialization;
using ktsu.Schema.Contracts.Names;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Provides schema definitions and management functionality.
/// This class focuses solely on schema definition without serialization or filesystem concerns.
/// </summary>
public partial class Schema
{
	/// <summary>
	/// The format version this build of the library writes.
	/// </summary>
	/// <remarks>
	/// Version 1 is the first versioned format. A file with no version field predates versioning
	/// and is treated as version <see cref="PreVersioningFormatVersion"/>; see
	/// <c>docs/schema-format.md</c> for the migration path and the compatibility policy.
	/// </remarks>
	public const int CurrentFormatVersion = 1;

	/// <summary>
	/// The version attributed to a file written before the format carried a version field.
	/// </summary>
	public const int PreVersioningFormatVersion = 0;

	/// <summary>
	/// Gets the format version of this schema.
	/// </summary>
	/// <remarks>
	/// Declared first so it is the first property in the serialized file, where a reader looking
	/// for it does not have to scan the whole document. Defaults to
	/// <see cref="CurrentFormatVersion"/> for a schema built in memory; a schema loaded from a
	/// file carries the version that file declared until it is migrated.
	/// </remarks>
	[JsonInclude]
	[JsonPropertyName("formatVersion")]
	public int FormatVersion { get; internal set; } = CurrentFormatVersion;

	/// <summary>
	/// Gets the directory the schema was loaded from, which relative paths in it resolve against.
	/// </summary>
	/// <remarks>
	/// Empty for a schema built in memory or parsed from a string with no anchor supplied, in
	/// which case its relative paths cannot be resolved - see <see cref="CanResolvePaths"/>.
	///
	/// Not serialized: a schema's own location is a property of where the file is, not of what is
	/// in it. Writing it into the file would break the moment the file moved, which is precisely
	/// what anchoring relative paths to the file is meant to survive.
	/// </remarks>
	[JsonIgnore]
	public AbsoluteDirectoryPath SourceDirectory { get; internal set; } = new();

	[JsonInclude]
	[JsonPropertyName("classes")]
	internal Collection<SchemaClass> ClassesInternal { get; set; } = [];

	[JsonInclude]
	[JsonPropertyName("enums")]
	internal Collection<SchemaEnum> EnumsInternal { get; set; } = [];

	[JsonInclude]
	[JsonPropertyName("codeGenerators")]
	internal Collection<SchemaCodeGenerator> CodeGeneratorsInternal { get; set; } = [];

	[JsonInclude]
	[JsonPropertyName("dataSources")]
	internal Collection<DataSource> DataSourcesInternal { get; set; } = [];

	/// <summary>
	/// Gets the collection of schema classes.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<SchemaClass> Classes => ClassesInternal;

	/// <summary>
	/// Gets the collection of schema enums.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<SchemaEnum> Enums => EnumsInternal;

	/// <summary>
	/// Gets the collection of code generators.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<SchemaCodeGenerator> CodeGenerators => CodeGeneratorsInternal;

	/// <summary>
	/// Gets the collection of data sources.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<DataSource> DataSources => DataSourcesInternal;

	/// <summary>
	/// Initializes a new instance of the Schema class.
	/// </summary>
	public Schema() => Reassociate();

	/// <summary>
	/// Reassociates schema classes and enums with their parent schema provider.
	/// Call this after deserializing a schema to re-establish parent-child relationships.
	/// </summary>
	public void Reassociate()
	{
		foreach (SchemaClass schemaClass in ClassesInternal)
		{
			schemaClass.AssociateWith(this);
			foreach (SchemaMember member in schemaClass.Members)
			{
				member.AssociateWith(schemaClass);
				member.Type.AssociateWith(member);
			}
		}

		foreach (SchemaEnum schemaEnum in EnumsInternal)
		{
			schemaEnum.AssociateWith(this);
		}

		foreach (DataSource dataSource in DataSourcesInternal)
		{
			dataSource.AssociateWith(this);
		}

		foreach (SchemaCodeGenerator codeGenerator in CodeGeneratorsInternal)
		{
			codeGenerator.AssociateWith(this);
		}
	}

	/// <summary>
	/// Tries to remove a child from a collection.
	/// </summary>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <param name="child">The child to remove.</param>
	/// <param name="collection">The collection to remove the child from.</param>
	/// <returns>True if the child was successfully removed; otherwise, false.</returns>
	public static bool TryRemoveChild<TChild>(TChild child, Collection<TChild> collection)
		where TChild : class
	{
		Ensure.NotNull(child);
		Ensure.NotNull(collection);

		return collection.Remove(child);
	}

	/// <summary>
	/// Gets a child from a collection by name.
	/// </summary>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <param name="name">The name of the child to get.</param>
	/// <param name="collection">The collection to search in.</param>
	/// <returns>The child if found; otherwise, null.</returns>
	public static TChild? GetChild<TName, TChild>(TName name, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : SemanticString<TName>, ISchemaChildName, new()
	{
		Ensure.NotNull(name);
		Ensure.NotNull(collection);

		foreach (TChild child in collection)
		{
			if (child.Name == name)
			{
				return child;
			}
		}

		return null;
	}

	/// <summary>
	/// Tries to get a child from a collection by name.
	/// </summary>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <param name="name">The name of the child to get.</param>
	/// <param name="collection">The collection to search in.</param>
	/// <param name="child">The found child, if any.</param>
	/// <returns>True if the child was found; otherwise, false.</returns>
	public static bool TryGetChild<TName, TChild>(TName name, Collection<TChild> collection, out TChild? child)
		where TChild : SchemaChild<TName>, new()
		where TName : SemanticString<TName>, ISchemaChildName, new()
	{
		child = GetChild(name, collection);
		return child is not null;
	}

	/// <summary>
	/// Tries to get an enum by name.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <param name="schemaEnum">The found enum, if any.</param>
	/// <returns>True if found; otherwise, false.</returns>
	public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum) => TryGetChild(name, EnumsInternal, out schemaEnum);

	/// <summary>
	/// Tries to get a class by name.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <param name="schemaClass">The found class, if any.</param>
	/// <returns>True if found; otherwise, false.</returns>
	public bool TryGetClass(ClassName name, out SchemaClass? schemaClass) => TryGetChild(name, ClassesInternal, out schemaClass);

	/// <summary>
	/// Gets an enum by name.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <returns>The enum if found, null otherwise.</returns>
	public SchemaEnum? GetEnum(EnumName name) => GetChild(name, EnumsInternal);

	/// <summary>
	/// Gets a class by name.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <returns>The class if found, null otherwise.</returns>
	public SchemaClass? GetClass(ClassName name) => GetChild(name, ClassesInternal);

	/// <summary>
	/// Adds a child to a collection.
	/// </summary>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <param name="name">The name of the child to add.</param>
	/// <param name="collection">The collection to add the child to.</param>
	/// <returns>The added child, or null if a child with the same name already exists.</returns>
	public TChild? AddChild<TChild, TName>(TName name, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : SemanticString<TName>, ISchemaChildName, new()
	{
		Ensure.NotNull(name);
		Ensure.NotNull(collection);

		if (GetChild(name, collection) is null)
		{
			TChild child = new();
			child.Rename(name);
			child.AssociateWith(this);
			collection.Add(child);
			return child;
		}

		return null;
	}

	/// <summary>
	/// Restores a previously removed child back into a collection.
	/// Used for undo operations where the original object reference is preserved.
	/// </summary>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <param name="child">The child to restore.</param>
	/// <param name="collection">The collection to restore the child into.</param>
	/// <returns>True if the child was restored; false if a child with the same name already exists.</returns>
	public bool RestoreChild<TChild, TName>(TChild child, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : SemanticString<TName>, ISchemaChildName, new()
	{
		Ensure.NotNull(child);
		Ensure.NotNull(collection);

		if (GetChild(child.Name, collection) is not null)
		{
			return false;
		}

		child.AssociateWith(this);
		collection.Add(child);
		return true;
	}

	/// <summary>
	/// Restores a previously removed class back into the schema.
	/// </summary>
	/// <param name="schemaClass">The class to restore.</param>
	/// <returns>True if restored; false if a class with the same name already exists.</returns>
	public bool RestoreClass(SchemaClass schemaClass) => RestoreChild<SchemaClass, ClassName>(schemaClass, ClassesInternal);

	/// <summary>
	/// Restores a previously removed enum back into the schema.
	/// </summary>
	/// <param name="schemaEnum">The enum to restore.</param>
	/// <returns>True if restored; false if an enum with the same name already exists.</returns>
	public bool RestoreEnum(SchemaEnum schemaEnum) => RestoreChild<SchemaEnum, EnumName>(schemaEnum, EnumsInternal);

	/// <summary>
	/// Restores a previously removed data source back into the schema.
	/// </summary>
	/// <param name="dataSource">The data source to restore.</param>
	/// <returns>True if restored; false if a data source with the same name already exists.</returns>
	public bool RestoreDataSource(DataSource dataSource) => RestoreChild<DataSource, DataSourceName>(dataSource, DataSourcesInternal);

	/// <summary>
	/// Restores a previously removed code generator back into the schema.
	/// </summary>
	/// <param name="codeGenerator">The code generator to restore.</param>
	/// <returns>True if restored; false if a code generator with the same name already exists.</returns>
	public bool RestoreCodeGenerator(SchemaCodeGenerator codeGenerator) => RestoreChild<SchemaCodeGenerator, CodeGeneratorName>(codeGenerator, CodeGeneratorsInternal);

	internal bool TryRemoveEnum(SchemaEnum schemaEnum) => TryRemoveChild(schemaEnum, EnumsInternal);

	internal bool TryRemoveClass(SchemaClass schemaClass) => TryRemoveChild(schemaClass, ClassesInternal);

	internal bool TryRemoveCodeGenerator(SchemaCodeGenerator schemaCodeGenerator) => TryRemoveChild(schemaCodeGenerator, CodeGeneratorsInternal);

	internal bool TryRemoveDataSource(DataSource dataSource) => TryRemoveChild(dataSource, DataSourcesInternal);

	internal bool TryAddChild<TChild, TName>(TName name, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : SemanticString<TName>, ISchemaChildName, new()
		=> AddChild(name, collection) is not null;

	/// <summary>
	/// Tries to add an enum.
	/// </summary>
	/// <param name="name">The name of the enum to add.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddEnum(EnumName name) => TryAddChild(name, EnumsInternal);

	/// <summary>
	/// Tries to add a class.
	/// </summary>
	/// <param name="name">The name of the class to add.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddClass(ClassName name) => TryAddChild(name, ClassesInternal);

	/// <summary>
	/// Adds an enum.
	/// </summary>
	/// <param name="name">The name of the enum to add.</param>
	/// <returns>The added enum if successful, null otherwise.</returns>
	public SchemaEnum? AddEnum(EnumName name) => AddChild(name, EnumsInternal);

	/// <summary>
	/// Adds a class.
	/// </summary>
	/// <param name="name">The name of the class to add.</param>
	/// <returns>The added class if successful, null otherwise.</returns>
	public SchemaClass? AddClass(ClassName name) => AddChild(name, ClassesInternal);

	/// <summary>
	/// Tries to add a data source.
	/// </summary>
	/// <param name="name">The name of the data source to add.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddDataSource(DataSourceName name) => TryAddChild(name, DataSourcesInternal);

	/// <summary>
	/// Adds a data source.
	/// </summary>
	/// <param name="name">The name of the data source to add.</param>
	/// <returns>The added data source if successful, null otherwise.</returns>
	public DataSource? AddDataSource(DataSourceName name) => AddChild(name, DataSourcesInternal);

	/// <summary>
	/// Gets a data source by name.
	/// </summary>
	/// <param name="name">The name of the data source.</param>
	/// <returns>The data source if found, null otherwise.</returns>
	public DataSource? GetDataSource(DataSourceName name) => GetChild(name, DataSourcesInternal);

	/// <summary>
	/// Tries to get a data source by name.
	/// </summary>
	/// <param name="name">The name of the data source.</param>
	/// <param name="dataSource">The found data source, if any.</param>
	/// <returns>True if found; otherwise, false.</returns>
	public bool TryGetDataSource(DataSourceName name, out DataSource? dataSource) => TryGetChild(name, DataSourcesInternal, out dataSource);

	/// <summary>
	/// Tries to add a code generator.
	/// </summary>
	/// <param name="name">The name of the code generator to add.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddCodeGenerator(CodeGeneratorName name) => TryAddChild(name, CodeGeneratorsInternal);

	/// <summary>
	/// Adds a code generator.
	/// </summary>
	/// <param name="name">The name of the code generator to add.</param>
	/// <returns>The added code generator if successful, null otherwise.</returns>
	public SchemaCodeGenerator? AddCodeGenerator(CodeGeneratorName name) => AddChild(name, CodeGeneratorsInternal);

	/// <summary>
	/// Gets a code generator by name.
	/// </summary>
	/// <param name="name">The name of the code generator.</param>
	/// <returns>The code generator if found, null otherwise.</returns>
	public SchemaCodeGenerator? GetCodeGenerator(CodeGeneratorName name) => GetChild(name, CodeGeneratorsInternal);

	/// <summary>
	/// Tries to get a code generator by name.
	/// </summary>
	/// <param name="name">The name of the code generator.</param>
	/// <param name="codeGenerator">The found code generator, if any.</param>
	/// <returns>True if found; otherwise, false.</returns>
	public bool TryGetCodeGenerator(CodeGeneratorName name, out SchemaCodeGenerator? codeGenerator) => TryGetChild(name, CodeGeneratorsInternal, out codeGenerator);

	/// <summary>
	/// Tries to add a class based on a .NET Type.
	/// </summary>
	/// <param name="type">The .NET type to add as a schema class.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddClass(Type type) => AddClass(type) is not null;

	/// <summary>
	/// Adds a class based on a .NET Type.
	/// </summary>
	/// <param name="type">The .NET type to add as a schema class.</param>
	/// <returns>The added class if successful, null otherwise.</returns>
	public SchemaClass? AddClass(Type type)
	{
		Ensure.NotNull(type);

		ClassName className = type.Name.As<ClassName>();
		SchemaClass? schemaClass = AddClass(className);
		if (schemaClass is not null)
		{
			// Add properties as members
			foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				MemberName memberName = property.Name.As<MemberName>();
				SchemaMember? member = schemaClass.AddMember(memberName);
				if (member is not null)
				{
					BaseType? schemaType = GetOrCreateSchemaType(property.PropertyType);
					if (schemaType is not null)
					{
						ApplySchemaKey(schemaType, property);
						member.SetType(schemaType);
					}
				}
			}

			// Add fields as members
			foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				MemberName memberName = field.Name.As<MemberName>();
				SchemaMember? member = schemaClass.AddMember(memberName);
				if (member is not null)
				{
					BaseType? schemaType = GetOrCreateSchemaType(field.FieldType);
					if (schemaType is not null)
					{
						ApplySchemaKey(schemaType, field);
						member.SetType(schemaType);
					}
				}
			}
		}

		return schemaClass;
	}

	private BaseType? GetOrCreateSchemaType(Type type)
	{
		Ensure.NotNull(type);

		type = Nullable.GetUnderlyingType(type) ?? type;

		if (DirectTypeMappings.TryGetValue(type, out Func<BaseType>? create))
		{
			return create();
		}

		if (TryGetCollectionElementType(type, out Type? elementType, out ContainerName? container) && elementType is not null && container is not null)
		{
			BaseType element = GetOrCreateSchemaType(elementType) ?? new None();
			return new Array() { ElementType = element, Container = container };
		}
		else if (type.IsEnum)
		{
			EnumName enumName = type.Name.As<EnumName>();
			SchemaEnum? schemaEnum = GetEnum(enumName) ?? AddEnum(enumName);
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
		else if (type.IsClass && type != typeof(string))
		{
			ClassName className = type.Name.As<ClassName>();
			SchemaClass? schemaClass = GetClass(className) ?? AddClass(type);
			if (schemaClass is not null)
			{
				return new Object() { ClassName = className };
			}
		}

		return new None();
	}

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

	/// <summary>
	/// Gets the first class in the schema.
	/// </summary>
	[JsonIgnore]
	public SchemaClass? FirstClass => ClassesInternal.FirstOrDefault();

	/// <summary>
	/// Gets the last class in the schema.
	/// </summary>
	[JsonIgnore]
	public SchemaClass? LastClass => ClassesInternal.LastOrDefault();

	private IEnumerable<BaseType> GetDiscreteTypes()
	{
		foreach (SchemaClass schemaClass in ClassesInternal)
		{
			foreach (SchemaMember member in schemaClass.Members)
			{
				yield return member.Type;
			}
		}
	}

	/// <summary>
	/// Gets all types defined in the schema.
	/// </summary>
	/// <returns>Collection of all schema types.</returns>
	public IEnumerable<BaseType> GetTypes() =>
		GetDiscreteTypes().GroupBy(t => t.GetType()).Select(g => g.First());

	/// <summary>
	/// Gets every type a member can be assigned, suitable for populating a type picker.
	/// This includes the built-in types, an <see cref="Enum"/> for each defined enum,
	/// an <see cref="Object"/> for each defined class, and an <see cref="Array"/> of each
	/// of those element types.
	/// </summary>
	/// <returns>Collection of all selectable schema types.</returns>
	public IEnumerable<BaseType> GetAvailableTypes()
	{
		yield return new None();

		foreach (BaseType elementType in GetSelectableElementTypes())
		{
			yield return elementType;
		}

		foreach (BaseType elementType in GetSelectableElementTypes())
		{
			yield return new Array() { ElementType = elementType };
		}
	}

	private IEnumerable<BaseType> GetSelectableElementTypes()
	{
		foreach (BaseType builtInType in BaseType.GetBuiltInTypes())
		{
			if (builtInType is not None)
			{
				yield return builtInType;
			}
		}

		foreach (SchemaEnum schemaEnum in EnumsInternal)
		{
			yield return new Enum() { EnumName = schemaEnum.Name };
		}

		foreach (SchemaClass schemaClass in ClassesInternal)
		{
			yield return new Object() { ClassName = schemaClass.Name };
		}
	}
}
