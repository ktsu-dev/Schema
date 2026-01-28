// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Reflection;
using ktsu.Schema.Contracts.Names;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

/// <summary>
/// Provides schema definitions and management functionality.
/// This class focuses solely on schema definition without serialization or filesystem concerns.
/// </summary>
public class Schema
{
	internal Collection<SchemaClass> ClassesInternal { get; set; } = [];
	internal Collection<SchemaEnum> EnumsInternal { get; set; } = [];
	internal Collection<SchemaCodeGenerator> CodeGeneratorsInternal { get; set; } = [];
	internal Collection<DataSource> DataSourcesInternal { get; set; } = [];

	/// <summary>
	/// Gets the collection of schema classes.
	/// </summary>
	public IReadOnlyCollection<SchemaClass> Classes => ClassesInternal;

	/// <summary>
	/// Gets the collection of schema enums.
	/// </summary>
	public IReadOnlyCollection<SchemaEnum> Enums => EnumsInternal;

	/// <summary>
	/// Gets the collection of code generators.
	/// </summary>
	public IReadOnlyCollection<SchemaCodeGenerator> CodeGenerators => CodeGeneratorsInternal;

	/// <summary>
	/// Gets the collection of data sources.
	/// </summary>
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

		// Handle basic types
		if (type == typeof(string))
		{
			return new Types.String();
		}
		else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
		{
			return new Types.Int();
		}
		else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
		{
			return new Types.Float();
		}
		else if (type == typeof(bool))
		{
			return new Types.Bool();
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
				return new Types.Enum() { EnumName = enumName };
			}
		}
		else if (type.IsClass && type != typeof(string))
		{
			ClassName className = type.Name.As<ClassName>();
			SchemaClass? schemaClass = GetClass(className) ?? AddClass(type);
			if (schemaClass is not null)
			{
				return new Types.Object() { ClassName = className };
			}
		}

		return new Types.None();
	}

	/// <summary>
	/// Gets the first class in the schema.
	/// </summary>
	public SchemaClass? FirstClass => ClassesInternal.FirstOrDefault();

	/// <summary>
	/// Gets the last class in the schema.
	/// </summary>
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
}
