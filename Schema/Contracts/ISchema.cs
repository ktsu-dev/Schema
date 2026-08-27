// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Contracts;

using ktsu.Schema.Models.Names;

/// <summary>
/// Defines a provider for schema definitions that can be injected as a dependency.
/// This interface focuses solely on schema definition and management without serialization or filesystem concerns.
/// </summary>
/// <remarks>
/// Mutation lives here rather than on the collections so that adding an element can enforce name
/// uniqueness and establish the parent association the element needs to resolve its own references.
/// </remarks>
public interface ISchema
{
	/// <summary>
	/// Gets the collection of schema classes, in declaration order.
	/// </summary>
	public ISchemaChildSet<ISchemaClass, ClassName> Classes { get; }

	/// <summary>
	/// Gets the collection of schema enums, in declaration order.
	/// </summary>
	public ISchemaChildSet<ISchemaEnum, EnumName> Enums { get; }

	/// <summary>
	/// Adds a class to the schema.
	/// </summary>
	/// <param name="name">The name of the class to add.</param>
	/// <returns>The added class, or <see langword="null"/> if the name is already taken.</returns>
	public ISchemaClass? AddClass(ClassName name);

	/// <summary>
	/// Adds an enum to the schema.
	/// </summary>
	/// <param name="name">The name of the enum to add.</param>
	/// <returns>The added enum, or <see langword="null"/> if the name is already taken.</returns>
	public ISchemaEnum? AddEnum(EnumName name);

	/// <summary>
	/// Removes a class from the schema.
	/// </summary>
	/// <param name="name">The name of the class to remove.</param>
	/// <returns><see langword="true"/> if a class with that name was found and removed; otherwise, <see langword="false"/>.</returns>
	public bool RemoveClass(ClassName name);

	/// <summary>
	/// Removes an enum from the schema.
	/// </summary>
	/// <param name="name">The name of the enum to remove.</param>
	/// <returns><see langword="true"/> if an enum with that name was found and removed; otherwise, <see langword="false"/>.</returns>
	public bool RemoveEnum(EnumName name);
}
