namespace ktsu.Schema;

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using ktsu.StrongPaths;
using ktsu.ToStringJsonConverter;
using ktsu.StrongStrings;

/// <summary>
/// Represents a document schema.
/// Provides methods for loading, saving, and manipulating the schema.
/// </summary>
public partial class Schema
{
	#region FilePaths
	/// <summary>
	/// Gets the relative paths associated with the schema.
	/// </summary>
	[JsonInclude] public SchemaPaths RelativePaths { get; private set; } = new();

	/// <summary>
	/// Gets the file extension for schema files.
	/// </summary>
	[JsonIgnore] public static FileExtension FileExtension => (FileExtension)".schema.json";

	/// <summary>
	/// Gets the absolute file path of the schema.
	/// </summary>
	[JsonIgnore] public AbsoluteFilePath FilePath { get; private set; } = new();

	/// <summary>
	/// Gets the project root path based on the file path and relative project root path.
	/// </summary>
	[JsonIgnore] public AbsoluteDirectoryPath ProjectRootPath => FilePath.DirectoryPath / RelativePaths.RelativeProjectRootPath;

	/// <summary>
	/// Gets the data source path based on the file path and relative data source path.
	/// </summary>
	[JsonIgnore] public AbsoluteDirectoryPath DataSourcePath => FilePath.DirectoryPath / RelativePaths.RelativeDataSourcePath;
	#endregion

	#region SchemaChildren
	[JsonInclude][JsonPropertyName("Classes")] private Collection<SchemaClass> ClassesInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of schema classes.
	/// </summary>
	public IReadOnlyCollection<SchemaClass> Classes => ClassesInternal;

	[JsonInclude][JsonPropertyName("Enums")] private Collection<SchemaEnum> EnumsInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of schema enums.
	/// </summary>
	public IReadOnlyCollection<SchemaEnum> Enums => EnumsInternal;

	[JsonInclude][JsonPropertyName("DataSources")] private Collection<DataSource> DataSourcesInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of data sources.
	/// </summary>
	public IReadOnlyCollection<DataSource> DataSources => DataSourcesInternal;

	[JsonInclude] private Collection<SchemaCodeGenerator> CodeGeneratorsInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of code generators.
	/// </summary>
	public IReadOnlyCollection<SchemaCodeGenerator> CodeGenerators => CodeGeneratorsInternal;
	#endregion

	/// <summary>
	/// Gets the JSON serializer options for the schema.
	/// </summary>
	internal static JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.General)
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		IgnoreReadOnlyFields = true,
		IgnoreReadOnlyProperties = true,
		IncludeFields = true,
		Converters =
		{
			new JsonStringEnumConverter(),
			new ToStringJsonConverterFactory(),
		}
	};

	/// <summary>
	/// Tries to load a schema from the specified file path.
	/// </summary>
	/// <param name="filePath">The file path to load the schema from.</param>
	/// <param name="schema">The loaded schema, or null if loading failed.</param>
	/// <returns>True if the schema was successfully loaded; otherwise, false.</returns>
	public static bool TryLoad(AbsoluteFilePath filePath, out Schema? schema)
	{
		schema = null;

		if (!string.IsNullOrEmpty(filePath))
		{
			try
			{
				schema = JsonSerializer.Deserialize<Schema>(File.ReadAllBytes(filePath), JsonSerializerOptions);
				if (schema is not null)
				{
					schema.FilePath = filePath;

					// Walk every class and tell each member which class they belong to
					schema.Reassosciate();
				}
			}
			catch (FileNotFoundException)
			{
				//TODO: throw an error popup because the file has dissappeared
			}
			catch (JsonException)
			{
				//TODO: throw an error popup because the file is not well formed
			}
		}

		return schema is not null;
	}

	/// <summary>
	/// Reassociates schema classes and enums with their parent schema.
	/// </summary>
	internal void Reassosciate()
	{
		foreach (var schemaClass in Classes)
		{
			schemaClass.AssosciateWith(this);
			foreach (var member in schemaClass.Members)
			{
				member.AssosciateWith(schemaClass);
				member.Type.AssosciateWith(member);
			}
		}

		foreach (var schemaEnum in Enums)
		{
			schemaEnum.AssosciateWith(this);
		}
	}

	/// <summary>
	/// Ensures that the directory for the specified path exists.
	/// </summary>
	/// <param name="path">The path to ensure the directory exists for.</param>
	public static void EnsureDirectoryExists(string path)
	{
		string? dirName = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dirName))
		{
			Directory.CreateDirectory(dirName);
		}
	}

	/// <summary>
	/// Saves the schema to its file path.
	/// </summary>
	public void Save()
	{
		EnsureDirectoryExists(FilePath);

		string jsonString = JsonSerializer.Serialize(this, JsonSerializerOptions);

		//TODO: hoist this out to some static method called something like WriteTextSafely
		string tmpFilePath = $"{FilePath}.tmp";
		string bkFilePath = $"{FilePath}.bk";
		File.Delete(tmpFilePath);
		File.Delete(bkFilePath);
		File.WriteAllText(tmpFilePath, jsonString);
		try
		{
			File.Move(FilePath, bkFilePath);
		}
		catch (FileNotFoundException) { }

		File.Move(tmpFilePath, FilePath);
		File.Delete(bkFilePath);
	}

	/// <summary>
	/// Changes the file path of the schema.
	/// </summary>
	/// <param name="newFilePath">The new file path.</param>
	public void ChangeFilePath(AbsoluteFilePath newFilePath) => FilePath = newFilePath;

	/// <summary>
	/// Tries to remove a child from a collection.
	/// </summary>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <param name="child">The child to remove.</param>
	/// <param name="collection">The collection to remove the child from.</param>
	/// <returns>True if the child was successfully removed; otherwise, false.</returns>
	public static bool TryRemoveChild<TChild>(TChild child, Collection<TChild> collection)
	{
		ArgumentNullException.ThrowIfNull(child);
		ArgumentNullException.ThrowIfNull(collection);

		return collection.Remove(child);
	}

	/// <summary>
	/// Gets a child from a collection by name.
	/// </summary>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <param name="name">The name of the child.</param>
	/// <param name="collection">The collection to search.</param>
	/// <returns>The child if found; otherwise, null.</returns>
	public static TChild? GetChild<TName, TChild>(TName name, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		return collection.FirstOrDefault(c => c.Name == name);
	}

	/// <summary>
	/// Tries to get a child from a collection by name.
	/// </summary>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <param name="name">The name of the child.</param>
	/// <param name="collection">The collection to search.</param>
	/// <param name="child">The child if found; otherwise, null.</param>
	/// <returns>True if the child was found; otherwise, false.</returns>
	public static bool TryGetChild<TName, TChild>(TName name, Collection<TChild> collection, out TChild? child)
		where TChild : SchemaChild<TName>, new()
		where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		child = null;
		if (name.IsEmpty())
		{
			return false;
		}

		child = collection.FirstOrDefault(c => c.Name == name);
		return child is not null;
	}

	/// <summary>
	/// Tries to get an enum from the schema by name.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <param name="schemaEnum">The enum if found; otherwise, null.</param>
	/// <returns>True if the enum was found; otherwise, false.</returns>
	public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum) => TryGetChild(name, EnumsInternal, out schemaEnum);

	/// <summary>
	/// Tries to get a class from the schema by name.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <param name="schemaClass">The class if found; otherwise, null.</param>
	/// <returns>True if the class was found; otherwise, false.</returns>
	public bool TryGetClass(ClassName name, out SchemaClass? schemaClass) => TryGetChild(name, ClassesInternal, out schemaClass);

	/// <summary>
	/// Gets an enum from the schema by name.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <returns>The enum if found; otherwise, null.</returns>
	public SchemaEnum? GetEnum(EnumName name) => GetChild(name, EnumsInternal);

	/// <summary>
	/// Gets a class from the schema by name.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <returns>The class if found; otherwise, null.</returns>
	public SchemaClass? GetClass(ClassName name) => GetChild(name, ClassesInternal);

	/// <summary>
	/// Gets a data source from the schema by name.
	/// </summary>
	/// <param name="name">The name of the data source.</param>
	/// <returns>The data source if found; otherwise, null.</returns>
	public DataSource? GetDataSource(DataSourceName name) => GetChild(name, DataSourcesInternal);

	/// <summary>
	/// Adds a child to a collection.
	/// </summary>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <param name="name">The name of the child.</param>
	/// <param name="collection">The collection to add the child to.</param>
	/// <returns>The added child if successful; otherwise, null.</returns>
	public TChild? AddChild<TChild, TName>(TName name, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : AnyStrongString, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

		if (!string.IsNullOrEmpty(name) && !collection.Any(c => c.Name == name))
		{
			TChild newChild = new();
			newChild.Rename(name);
			newChild.AssosciateWith(this);
			collection.Add(newChild);

			return newChild;
		}

		return null;
	}


	/// <summary>
	/// Tries to remove an enum from the schema.
	/// </summary>
	/// <param name="schemaEnum">The enum to remove.</param>
	/// <returns>True if the enum was successfully removed; otherwise, false.</returns>
	public bool TryRemoveEnum(SchemaEnum schemaEnum) => TryRemoveChild(schemaEnum, EnumsInternal);

	/// <summary>
	/// Tries to remove a class from the schema.
	/// </summary>
	/// <param name="schemaClass">The class to remove.</param>
	/// <returns>True if the class was successfully removed; otherwise, false.</returns>
	public bool TryRemoveClass(SchemaClass schemaClass) => TryRemoveChild(schemaClass, ClassesInternal);

	/// <summary>
	/// Tries to remove a data source from the schema.
	/// </summary>
	/// <param name="dataSource">The data source to remove.</param>
	/// <returns>True if the data source was successfully removed; otherwise, false.</returns>
	public bool TryRemoveDataSource(DataSource dataSource) => TryRemoveChild(dataSource, DataSourcesInternal);

	/// <summary>
	/// Tries to remove a code generator from the schema.
	/// </summary>
	/// <param name="schemaCodeGenerator">The code generator to remove.</param>
	/// <returns>True if the code generator was successfully removed; otherwise, false.</returns>
	public bool TryRemoveCodeGenerator(SchemaCodeGenerator schemaCodeGenerator) => TryRemoveChild(schemaCodeGenerator, CodeGeneratorsInternal);

	/// <summary>
	/// Tries to add a child to a collection.
	/// </summary>
	/// <typeparam name="TChild">The type of the child.</typeparam>
	/// <typeparam name="TName">The type of the name.</typeparam>
	/// <param name="name">The name of the child.</param>
	/// <param name="collection">The collection to add the child to.</param>
	/// <returns>True if the child was successfully added; otherwise, false.</returns>
	public bool TryAddChild<TChild, TName>(TName name, Collection<TChild> collection)
		where TChild : SchemaChild<TName>, new()
		where TName : AnyStrongString, new()
		=> AddChild(name, collection) is not null;

	/// <summary>
	/// Tries to add an enum to the schema.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <returns>True if the enum was successfully added; otherwise, false.</returns>
	public bool TryAddEnum(EnumName name) => TryAddChild(name, EnumsInternal);

	/// <summary>
	/// Tries to add a class to the schema.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <returns>True if the class was successfully added; otherwise, false.</returns>
	public bool TryAddClass(ClassName name) => TryAddChild(name, ClassesInternal);

	/// <summary>
	/// Tries to add a data source to the schema.
	/// </summary>
	/// <param name="name">The name of the data source.</param>
	/// <returns>True if the data source was successfully added; otherwise, false.</returns>
	public bool TryAddDataSource(DataSourceName name) => TryAddChild(name, DataSourcesInternal);

	/// <summary>
	/// Adds an enum to the schema.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <returns>The added enum if successful; otherwise, null.</returns>
	public SchemaEnum? AddEnum(EnumName name) => AddChild(name, EnumsInternal);

	/// <summary>
	/// Adds a class to the schema.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <returns>The added class if successful; otherwise, null.</returns>
	public SchemaClass? AddClass(ClassName name) => AddChild(name, ClassesInternal);

	/// <summary>
	/// Adds a data source to the schema.
	/// </summary>
	/// <param name="name">The name of the data source.</param>
	/// <returns>The added data source if successful; otherwise, null.</returns>
	public DataSource? AddDataSource(DataSourceName name) => AddChild(name, DataSourcesInternal);

	/// <summary>
	/// Tries to add a class to the schema from a type.
	/// </summary>
	/// <param name="type">The type to add as a class.</param>
	/// <returns>True if the class was successfully added; otherwise, false.</returns>
	public bool TryAddClass(Type type) => AddClass(type) is not null;

	/// <summary>
	/// Adds a class to the schema from a type.
	/// </summary>
	/// <param name="type">The type to add as a class.</param>
	/// <returns>The added class if successful; otherwise, null.</returns>
	public SchemaClass? AddClass(Type type)
	{
		if (type is not null)
		{
			SchemaClass newClass = new();
			newClass.Rename((ClassName)type.Name);
			newClass.AssosciateWith(this);
			foreach (var member in type.GetMembers())
			{
				var memberType = member switch
				{
					PropertyInfo propertyInfo => propertyInfo.PropertyType,
					FieldInfo fieldInfo => fieldInfo.FieldType,
					_ => null
				};

				if (memberType is not null)
				{
					var schemaType = GetOrCreateSchemaType(memberType);
					if (schemaType is not null)
					{
						var newMember = newClass.AddMember((MemberName)member.Name);
						newMember?.SetType(schemaType);
					}
				}
			}

			ClassesInternal.Add(newClass);

			return newClass;
		}

		return null;
	}

	/// <summary>
	/// Gets or creates a schema type from a .NET type.
	/// </summary>
	/// <param name="type">The .NET type to get or create a schema type for.</param>
	/// <returns>The schema type if successful; otherwise, null.</returns>
	private SchemaTypes.BaseType? GetOrCreateSchemaType(Type type)
	{
		bool isEnumerable = type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
		if (type.IsArray || isEnumerable)
		{
			var elementType = type.HasElementType ? type.GetElementType() : type.GetGenericArguments().LastOrDefault();
			if (elementType is not null)
			{
				var schemaType = GetOrCreateSchemaType(elementType);
				if (schemaType is not null)
				{
					return new SchemaTypes.Array { ElementType = schemaType };
				}
			}
		}
		else if (type.IsEnum)
		{
			var enumName = (EnumName)type.Name;
			if (!TryGetEnum(enumName, out var schemaEnum))
			{
				schemaEnum = AddEnum(enumName);
				foreach (string name in Enum.GetNames(type))
				{
					schemaEnum?.TryAddValue((EnumValueName)name);
				}
			}

			if (schemaEnum is not null)
			{
				return new SchemaTypes.Enum { EnumName = schemaEnum.Name };
			}
		}
		else if (type.IsPrimitive || type.FullName == "System.String")
		{
			string typeName = type.Name switch
			{
				"Int32" => "Int",
				"Int64" => "Long",
				"Single" => "Float",
				"Double" => "Double",
				"String" => "String",
				"DateTime" => "DateTime",// ?
				"TimeSpan" => "TimeSpan",// ?
				"Boolean" => "Bool",
				_ => "",
			};

			return SchemaTypes.BaseType.CreateFromString(typeName) as SchemaTypes.BaseType;
		}
		else if (type.IsClass)
		{
			if (!TryGetClass((ClassName)type.Name, out var memberClass))
			{
				memberClass = AddClass(type);
			}

			if (memberClass is not null)
			{
				return new SchemaTypes.Object { ClassName = memberClass.Name };
			}
		}
		return null;
	}

	/// <summary>
	/// Gets the first class in the schema.
	/// </summary>
	public SchemaClass? FirstClass => Classes.FirstOrDefault();

	/// <summary>
	/// Gets the last class in the schema.
	/// </summary>
	public SchemaClass? LastClass => Classes.LastOrDefault();

	private IEnumerable<SchemaTypes.BaseType> GetDiscreteTypes()
	{
		yield return new SchemaTypes.Int();
		yield return new SchemaTypes.Long();
		yield return new SchemaTypes.Float();
		yield return new SchemaTypes.Double();
		yield return new SchemaTypes.String();
		yield return new SchemaTypes.DateTime();
		yield return new SchemaTypes.TimeSpan();
		yield return new SchemaTypes.Bool();

		foreach (var schemaEnum in Enums)
		{
			yield return new SchemaTypes.Enum { EnumName = schemaEnum.Name };
		}

		foreach (var schemaClass in Classes)
		{
			yield return new SchemaTypes.Object { ClassName = schemaClass.Name };
		}
	}

	/// <summary>
	/// Gets the collection of schema types.
	/// </summary>
	/// <returns>An enumerable collection of schema types.</returns>
	public IEnumerable<SchemaTypes.BaseType> GetTypes() =>
		GetDiscreteTypes().Concat(GetDiscreteTypes().Select(t => new SchemaTypes.Array { ElementType = t }));
}
