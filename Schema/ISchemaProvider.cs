// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

/// <summary>
/// Defines a provider for schema definitions that can be injected as a dependency.
/// This interface focuses solely on schema definition and management without serialization or filesystem concerns.
/// </summary>
public interface ISchemaProvider
{
	/// <summary>
	/// Gets the collection of schema classes.
	/// </summary>
	public IReadOnlyCollection<SchemaClass> Classes { get; }

	/// <summary>
	/// Gets the collection of schema enums.
	/// </summary>
	public IReadOnlyCollection<SchemaEnum> Enums { get; }

	/// <summary>
	/// Gets the collection of code generators.
	/// </summary>
	public IReadOnlyCollection<SchemaCodeGenerator> CodeGenerators { get; }

	/// <summary>
	/// Gets the collection of data sources.
	/// </summary>
	public IReadOnlyCollection<DataSource> DataSources { get; }

	/// <summary>
	/// Tries to get an enum by name.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <param name="schemaEnum">The found enum, if any.</param>
	/// <returns>True if found, false otherwise.</returns>
	public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum);

	/// <summary>
	/// Tries to get a class by name.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <param name="schemaClass">The found class, if any.</param>
	/// <returns>True if found, false otherwise.</returns>
	public bool TryGetClass(ClassName name, out SchemaClass? schemaClass);

	/// <summary>
	/// Gets an enum by name.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	/// <returns>The enum if found, null otherwise.</returns>
	public SchemaEnum? GetEnum(EnumName name);

	/// <summary>
	/// Gets a class by name.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	/// <returns>The class if found, null otherwise.</returns>
	public SchemaClass? GetClass(ClassName name);

	/// <summary>
	/// Tries to add a new enum.
	/// </summary>
	/// <param name="name">The name of the enum to add.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddEnum(EnumName name);

	/// <summary>
	/// Tries to add a new class.
	/// </summary>
	/// <param name="name">The name of the class to add.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddClass(ClassName name);

	/// <summary>
	/// Adds a new enum.
	/// </summary>
	/// <param name="name">The name of the enum to add.</param>
	/// <returns>The added enum if successful, null otherwise.</returns>
	public SchemaEnum? AddEnum(EnumName name);

	/// <summary>
	/// Adds a new class.
	/// </summary>
	/// <param name="name">The name of the class to add.</param>
	/// <returns>The added class if successful, null otherwise.</returns>
	public SchemaClass? AddClass(ClassName name);

	/// <summary>
	/// Tries to add a class based on a .NET Type.
	/// </summary>
	/// <param name="type">The .NET type to add as a schema class.</param>
	/// <returns>True if added successfully, false otherwise.</returns>
	public bool TryAddClass(Type type);

	/// <summary>
	/// Adds a class based on a .NET Type.
	/// </summary>
	/// <param name="type">The .NET type to add as a schema class.</param>
	/// <returns>The added class if successful, null otherwise.</returns>
	public SchemaClass? AddClass(Type type);

	/// <summary>
	/// Gets all types defined in the schema.
	/// </summary>
	/// <returns>Collection of all schema types.</returns>
	public IEnumerable<SchemaTypes.BaseType> GetTypes();

	/// <summary>
	/// Gets the first class in the schema.
	/// </summary>
	public SchemaClass? FirstClass { get; }

	/// <summary>
	/// Gets the last class in the schema.
	/// </summary>
	public SchemaClass? LastClass { get; }
}
