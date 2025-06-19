// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using System.Collections.ObjectModel;
using System.Reflection;
using ktsu.Extensions;
using ktsu.Semantics;

/// <summary>
/// Provides schema definitions and management functionality.
/// This class focuses solely on schema definition without serialization or filesystem concerns.
/// </summary>
public class SchemaProvider : ISchemaProvider
{
	#region SchemaChildren
	internal Collection<SchemaClass> ClassesInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of schema classes.
	/// </summary>
	public IReadOnlyCollection<SchemaClass> Classes => ClassesInternal;

	internal Collection<SchemaEnum> EnumsInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of schema enums.
	/// </summary>
	public IReadOnlyCollection<SchemaEnum> Enums => EnumsInternal;

	internal Collection<SchemaCodeGenerator> CodeGeneratorsInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of code generators.
	/// </summary>
	public IReadOnlyCollection<SchemaCodeGenerator> CodeGenerators => CodeGeneratorsInternal;

	internal Collection<DataSource> DataSourcesInternal { get; set; } = [];
	/// <summary>
	/// Gets the collection of data sources.
	/// </summary>
	public IReadOnlyCollection<DataSource> DataSources => DataSourcesInternal;
	#endregion

	/// <summary>
	/// Initializes a new instance of the SchemaProvider class.
	/// </summary>
	public SchemaProvider() => Reassociate();

	/// <summary>
	/// Reassociates schema classes and enums with their parent schema provider.
	/// </summary>
	internal void Reassociate()
	{
		foreach (SchemaClass schemaClass in Classes)
		{
			schemaClass.AssociateWith(this);
			foreach (SchemaMember member in schemaClass.Members)
			{
				member.AssociateWith(schemaClass);
				member.Type.AssociateWith(member);
			}
		}

		foreach (SchemaEnum schemaEnum in Enums)
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
		ArgumentNullException.ThrowIfNull(child);
		ArgumentNullException.ThrowIfNull(collection);

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
		where TName : SemanticString<TName>, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

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
		where TName : SemanticString<TName>, new()
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
		where TName : SemanticString<TName>, new()
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(collection);

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
		where TName : SemanticString<TName>, new()
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
		ArgumentNullException.ThrowIfNull(type);

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
					SchemaTypes.BaseType? schemaType = GetOrCreateSchemaType(property.PropertyType);
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
					SchemaTypes.BaseType? schemaType = GetOrCreateSchemaType(field.FieldType);
					if (schemaType is not null)
					{
						member.SetType(schemaType);
					}
				}
			}
		}

		return schemaClass;
	}

	private SchemaTypes.BaseType? GetOrCreateSchemaType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		// Handle basic types
		if (type == typeof(string))
		{
			return new SchemaTypes.String();
		}
		else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
		{
			return new SchemaTypes.Int();
		}
		else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
		{
			return new SchemaTypes.Float();
		}
		else if (type == typeof(bool))
		{
			return new SchemaTypes.Bool();
		}
		else if (type.IsEnum)
		{
			EnumName enumName = type.Name.As<EnumName>();
			SchemaEnum? schemaEnum = GetEnum(enumName) ?? AddEnum(enumName);
			if (schemaEnum is not null)
			{
				// Add enum values
				foreach (string enumValue in Enum.GetNames(type))
				{
					schemaEnum.TryAddValue(enumValue.As<EnumValueName>());
				}
				return new SchemaTypes.Enum() { EnumName = enumName };
			}
		}
		else if (type.IsClass && type != typeof(string))
		{
			ClassName className = type.Name.As<ClassName>();
			SchemaClass? schemaClass = GetClass(className) ?? AddClass(type);
			if (schemaClass is not null)
			{
				return new SchemaTypes.Object() { ClassName = className };
			}
		}

		return new SchemaTypes.None();
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
		foreach (SchemaClass schemaClass in Classes)
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
	public IEnumerable<SchemaTypes.BaseType> GetTypes() =>
		GetDiscreteTypes().GroupBy(t => t.GetType()).Select(g => g.First());
}
