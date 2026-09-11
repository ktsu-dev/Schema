// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ktsu.Schema.Contracts;
using ktsu.Schema.Contracts.Names;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Provides schema definitions and management functionality.
/// This class focuses solely on schema definition without serialization or filesystem concerns.
/// </summary>
public partial class Schema : ISchema
{
	/// <summary>
	/// The format version this build of the library writes.
	/// </summary>
	/// <remarks>
	/// Version 1 is the first versioned format. A file with no version field predates versioning
	/// and is treated as version <see cref="PreVersioningFormatVersion"/>; see
	/// <c>docs/schema-format.md</c> for the migration path and the compatibility policy.
	/// <para>
	/// Version 2 added the semantic metadata a member can carry: unit, range, default,
	/// interpolation, network encoding and editor hint. Every one of them is optional and
	/// omitted when absent, so a version 2 file with none of them is byte-identical to the
	/// version 1 file it would have been. The version still moves, because a version 1 reader
	/// silently ignores properties it does not know: it would load such a file, drop the
	/// metadata, and write it back without it — which is exactly the information loss the
	/// round-trip contract in <c>docs/schema-format.md</c> forbids. Refusing to read it is
	/// the honest outcome.
	/// </para>
	/// <para>
	/// Version 4 gave a vector a component type. It is omitted when it is the default, so a file
	/// whose vectors are vectors of floats is byte-identical to the version 3 file it would have
	/// been; the version moves for the same reason version 2 did, because a version 3 reader
	/// would load a <c>Vec3</c> of a semantic type, drop what it is three of, and write it back
	/// as three bare floats.
	/// </para>
	/// <para>
	/// Version 5 added two things a schema could not say about the code it describes: that a class
	/// travels as raw bytes, so its member order is load-bearing rather than cosmetic, and which
	/// enum a failed <see cref="Types.Result"/> carries. Both are additive and the first is
	/// omitted when it is false, but a version 4 reader would drop either and go on generating -
	/// a type that quietly stopped promising its layout, or a fallible call with nothing to say
	/// when it fails.
	/// </para>
	/// <para>
	/// Version 6 let a function say a call leaves the receiver unchanged. Additive and omitted
	/// when false, and the version moves for the reason the others did: a version 5 reader would
	/// drop it and generate a signature that promises less than the schema does.
	/// </para>
	/// </remarks>
	public const int CurrentFormatVersion = 6;

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

	/// <summary>
	/// Gets the name of the file this schema was read from, without its directory.
	/// </summary>
	/// <remarks>
	/// Empty for a schema built in memory. Kept beside <see cref="SourceDirectory"/> rather than
	/// derived from it, because the directory is the anchor relative paths resolve against and
	/// this is the name a generated file cites so a reader knows what to edit instead. Not
	/// serialized, for the same reason the directory is not: where the file is is a property of
	/// where it is, not of what is in it.
	/// </remarks>
	[JsonIgnore]
	public string SourceFileName { get; internal set; } = string.Empty;

	/// <summary>
	/// Gets or sets the enum a failed <see cref="Types.Result"/> carries.
	/// </summary>
	/// <remarks>
	/// Held once by the schema rather than restated on every fallible signature, for the same
	/// reason the other four conventions are: a signature that could choose its own error type
	/// would be a signature that has to be read to find out, and every interface language that
	/// allowed that grew per-declaration annotations until it was a worse version of the language
	/// it described. A <see cref="Types.Result"/> says a call can fail; this says what a failure
	/// tells you, everywhere.
	/// <para>
	/// An enum rather than any type, because an error is one of a closed set of reasons - which is
	/// what an enum is - and a caller that has to switch on the reason needs the set to be
	/// enumerable. Empty until a schema declares one, which <see cref="Validate"/> reports only
	/// when something actually returns a <see cref="Types.Result"/>.
	/// </para>
	/// </remarks>
	public EnumName ErrorType { get; set; } = new();

	[JsonInclude]
	[JsonPropertyName("classes")]
	internal Collection<SchemaClass> ClassesInternal { get; set; } = [];

	[JsonInclude]
	[JsonPropertyName("enums")]
	internal Collection<SchemaEnum> EnumsInternal { get; set; } = [];

	[JsonInclude]
	[JsonPropertyName("interfaces")]
	internal Collection<SchemaInterface> InterfacesInternal { get; set; } = [];

	[JsonInclude]
	[JsonPropertyName("semanticTypes")]
	internal Collection<SchemaSemanticType> SemanticTypesInternal { get; set; } = [];

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
	/// Gets the interfaces the schema declares.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<SchemaInterface> Interfaces => InterfacesInternal;

	/// <summary>
	/// Gets the semantic types the schema declares.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyCollection<SchemaSemanticType> SemanticTypes => SemanticTypesInternal;

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
	/// Gets the schema's classes as a name-indexed, order-preserving set.
	/// </summary>
	[JsonIgnore]
	public SchemaChildSet<SchemaClass, ClassName> ClassSet => new(ClassesInternal);

	/// <summary>
	/// Gets the schema's enums as a name-indexed, order-preserving set.
	/// </summary>
	[JsonIgnore]
	public SchemaChildSet<SchemaEnum, EnumName> EnumSet => new(EnumsInternal);

	/// <summary>
	/// Gets an order-preserving, name-unique view over the interfaces.
	/// </summary>
	[JsonIgnore]
	public SchemaChildSet<SchemaInterface, InterfaceName> InterfaceSet => new(InterfacesInternal);

	/// <summary>
	/// Gets an order-preserving, name-unique view over the semantic types.
	/// </summary>
	[JsonIgnore]
	public SchemaChildSet<SchemaSemanticType, SemanticTypeName> SemanticTypeSet => new(SemanticTypesInternal);

	/// <summary>
	/// Gets the schema's data sources as a name-indexed, order-preserving set.
	/// </summary>
	[JsonIgnore]
	public SchemaChildSet<DataSource, DataSourceName> DataSourceSet => new(DataSourcesInternal);

	/// <summary>
	/// Gets the schema's code generators as a name-indexed, order-preserving set.
	/// </summary>
	[JsonIgnore]
	public SchemaChildSet<SchemaCodeGenerator, CodeGeneratorName> CodeGeneratorSet => new(CodeGeneratorsInternal);

	/// <remarks>
	/// Explicit because the contract's element type is <see cref="ISchemaClass"/>; the covariance
	/// of <see cref="ISchemaChildSet{TValue, TName}"/> makes it the same object.
	/// </remarks>
	ISchemaChildSet<ISchemaClass, ClassName> ISchema.Classes => ClassSet;

	/// <remarks>
	/// Explicit because the contract's element type is <see cref="ISchemaEnum"/>; the covariance
	/// of <see cref="ISchemaChildSet{TValue, TName}"/> makes it the same object.
	/// </remarks>
	ISchemaChildSet<ISchemaEnum, EnumName> ISchema.Enums => EnumSet;

	/// <inheritdoc />
	ISchemaClass? ISchema.AddClass(ClassName name) => AddClass(name);

	/// <inheritdoc />
	ISchemaEnum? ISchema.AddEnum(EnumName name) => AddEnum(name);

	/// <summary>
	/// Removes the class with the specified name.
	/// </summary>
	/// <param name="name">The name of the class to remove.</param>
	/// <returns>True if a class with that name was found and removed; otherwise, false.</returns>
	public bool RemoveClass(ClassName name) => GetClass(name) is SchemaClass schemaClass && TryRemoveClass(schemaClass);

	/// <summary>
	/// Removes the enum with the specified name.
	/// </summary>
	/// <param name="name">The name of the enum to remove.</param>
	/// <returns>True if an enum with that name was found and removed; otherwise, false.</returns>
	public bool RemoveEnum(EnumName name) => GetEnum(name) is SchemaEnum schemaEnum && TryRemoveEnum(schemaEnum);

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

		foreach (SchemaInterface schemaInterface in InterfacesInternal)
		{
			schemaInterface.AssociateWith(this);
			schemaInterface.Reassociate();
		}

		foreach (SchemaSemanticType semanticType in SemanticTypesInternal)
		{
			semanticType.AssociateWith(this);
			semanticType.UnderlyingType.AssociateWith(this);
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
	/// Tries to get an interface by name.
	/// </summary>
	/// <param name="name">The interface name.</param>
	/// <param name="schemaInterface">The interface, when found.</param>
	/// <returns><see langword="true"/> when an interface of that name exists.</returns>
	public bool TryGetInterface(InterfaceName name, out SchemaInterface? schemaInterface) => TryGetChild(name, InterfacesInternal, out schemaInterface);

	/// <summary>
	/// Tries to get a semantic type by name.
	/// </summary>
	/// <param name="name">The semantic type name.</param>
	/// <param name="semanticType">The semantic type, when found.</param>
	/// <returns><see langword="true"/> when a semantic type of that name exists.</returns>
	public bool TryGetSemanticType(SemanticTypeName name, out SchemaSemanticType? semanticType) => TryGetChild(name, SemanticTypesInternal, out semanticType);

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
	/// Gets an interface by name.
	/// </summary>
	/// <param name="name">The interface name.</param>
	/// <returns>The interface, or <see langword="null"/> when none of that name exists.</returns>
	public SchemaInterface? GetInterface(InterfaceName name) => GetChild(name, InterfacesInternal);

	/// <summary>
	/// Gets a semantic type by name.
	/// </summary>
	/// <param name="name">The semantic type name.</param>
	/// <returns>The semantic type, or <see langword="null"/> when none of that name exists.</returns>
	public SchemaSemanticType? GetSemanticType(SemanticTypeName name) => GetChild(name, SemanticTypesInternal);

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

		TChild child = new();
		child.Rename(name);
		child.AssociateWith(this);
		return new SchemaChildSet<TChild, TName>(collection).Add(child) ? child : null;
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

		child.AssociateWith(this);
		return new SchemaChildSet<TChild, TName>(collection).Add(child);
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

	/// <summary>
	/// Removes an interface from the schema.
	/// </summary>
	/// <param name="schemaInterface">The interface to remove.</param>
	/// <returns><see langword="true"/> when it was present and removed.</returns>
	internal bool TryRemoveInterface(SchemaInterface schemaInterface) => TryRemoveChild(schemaInterface, InterfacesInternal);

	/// <summary>
	/// Removes a semantic type from the schema.
	/// </summary>
	/// <param name="semanticType">The semantic type to remove.</param>
	/// <returns><see langword="true"/> when it was present and removed.</returns>
	internal bool TryRemoveSemanticType(SchemaSemanticType semanticType) => TryRemoveChild(semanticType, SemanticTypesInternal);

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
	/// Tries to add an interface to the schema.
	/// </summary>
	/// <param name="name">The interface name.</param>
	/// <returns><see langword="true"/> when the name was free and the interface was added.</returns>
	public bool TryAddInterface(InterfaceName name) => TryAddChild(name, InterfacesInternal);

	/// <summary>
	/// Tries to add a semantic type to the schema.
	/// </summary>
	/// <param name="name">The semantic type name.</param>
	/// <returns><see langword="true"/> when the name was free and the type was added.</returns>
	public bool TryAddSemanticType(SemanticTypeName name) => TryAddChild(name, SemanticTypesInternal);

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
	/// Adds an interface to the schema.
	/// </summary>
	/// <param name="name">The interface name.</param>
	/// <returns>The new interface, or <see langword="null"/> when the name is already taken.</returns>
	public SchemaInterface? AddInterface(InterfaceName name) => AddChild(name, InterfacesInternal);

	/// <summary>
	/// Adds a semantic type to the schema.
	/// </summary>
	/// <param name="name">The semantic type name.</param>
	/// <returns>The new semantic type, or <see langword="null"/> when the name is taken.</returns>
	public SchemaSemanticType? AddSemanticType(SemanticTypeName name) => AddChild(name, SemanticTypesInternal);

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
	/// <remarks>
	/// The reflection that reads the type lives in <see cref="ClrTypeImporter"/>, which is the
	/// inverse of what the C# generator emits and is checked against it by the
	/// generate-then-reimport round trip.
	/// </remarks>
	/// <param name="type">The .NET type to add as a schema class.</param>
	/// <returns>The added class if successful, null otherwise.</returns>
	public SchemaClass? AddClass(Type type) => ClrTypeImporter.Import(this, type);

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
