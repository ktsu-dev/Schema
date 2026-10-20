// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// Rename support for <see cref="Schema"/>.
/// </summary>
/// <remarks>
/// Renaming is the schema's responsibility rather than the individual element's because a name
/// is referenced from elsewhere in the schema: an <see cref="Object"/> names a class, an
/// <see cref="Types.Enum"/> names an enum, a <see cref="DataSource"/> names a class, an
/// <see cref="Array"/> names a key member, a <see cref="Semantic"/> names a semantic type, an
/// <see cref="Interface"/> names an interface, and <see cref="ErrorType"/> names the error enum. A
/// type reference can stand in a member, a function's return type or one of its parameters, and
/// can be nested inside an <see cref="Array"/>, a <see cref="Vector"/> or a
/// <see cref="WrapperType"/> at any depth. <see cref="SchemaChild{TName}.Rename"/> changes
/// only the name and would leave those references dangling, so the operations here cascade the
/// new name to every referrer and reject a name that collides with a sibling.
/// </remarks>
public partial class Schema
{
	/// <summary>
	/// Renames a class and repoints every reference to it.
	/// </summary>
	/// <remarks>
	/// Every object reference to the old name - in a member, a function signature, or nested inside
	/// an array or wrapper - and every data source that named it is updated to the new one, so the
	/// schema stays referentially intact.
	/// </remarks>
	/// <param name="schemaClass">The class to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the class was renamed; false if the name is empty or collides with another class.</returns>
	public bool TryRenameClass(SchemaClass schemaClass, ClassName newName)
	{
		Ensure.NotNull(schemaClass);
		Ensure.NotNull(newName);

		if (!ClassesInternal.Contains(schemaClass) ||
			!CanRename(schemaClass.Name, newName, ClassesInternal.Select(c => c.Name.ToString())))
		{
			return false;
		}

		ClassName oldName = schemaClass.Name;
		schemaClass.Rename(newName);

		foreach (BaseType type in EnumerateMemberTypes())
		{
			if (type is Object objectType && objectType.ClassName == oldName)
			{
				objectType.ClassName = newName;
			}
		}

		foreach (DataSource dataSource in DataSourcesInternal.Where(d => d.ClassName == oldName))
		{
			dataSource.ClassName = newName;
		}

		return true;
	}

	/// <summary>
	/// Renames an enum and repoints every reference to it.
	/// </summary>
	/// <remarks>
	/// Every enum reference to the old name - in a member, a function signature, or nested inside an
	/// array or wrapper - is updated, as is <see cref="ErrorType"/> when it named this enum.
	/// </remarks>
	/// <param name="schemaEnum">The enum to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the enum was renamed; false if the name is empty or collides with another enum.</returns>
	public bool TryRenameEnum(SchemaEnum schemaEnum, EnumName newName)
	{
		Ensure.NotNull(schemaEnum);
		Ensure.NotNull(newName);

		if (!EnumsInternal.Contains(schemaEnum) ||
			!CanRename(schemaEnum.Name, newName, EnumsInternal.Select(e => e.Name.ToString())))
		{
			return false;
		}

		EnumName oldName = schemaEnum.Name;
		schemaEnum.Rename(newName);

		foreach (BaseType type in EnumerateMemberTypes())
		{
			if (type is Types.Enum enumType && enumType.EnumName == oldName)
			{
				enumType.EnumName = newName;
			}
		}

		if (ErrorType == oldName)
		{
			ErrorType = newName;
		}

		return true;
	}

	/// <summary>
	/// Renames a semantic type and repoints every reference to it.
	/// </summary>
	/// <remarks>
	/// Every semantic reference to the old name - in a member, a function signature, nested inside
	/// an array or wrapper, or as the underlying type of another semantic type that refines this one -
	/// is updated, so the schema stays referentially intact.
	/// </remarks>
	/// <param name="semanticType">The semantic type to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the semantic type was renamed; false if the name is empty or collides with another semantic type.</returns>
	public bool TryRenameSemanticType(SchemaSemanticType semanticType, SemanticTypeName newName)
	{
		Ensure.NotNull(semanticType);
		Ensure.NotNull(newName);

		if (!SemanticTypesInternal.Contains(semanticType) ||
			!CanRename(semanticType.Name, newName, SemanticTypesInternal.Select(t => t.Name.ToString())))
		{
			return false;
		}

		SemanticTypeName oldName = semanticType.Name;
		semanticType.Rename(newName);

		foreach (BaseType type in EnumerateMemberTypes())
		{
			if (type is Semantic semantic && semantic.SemanticTypeName == oldName)
			{
				semantic.SemanticTypeName = newName;
			}
		}

		return true;
	}

	/// <summary>
	/// Renames an interface and repoints every reference to it.
	/// </summary>
	/// <remarks>
	/// Every interface reference to the old name - in a member, a function signature, or nested
	/// inside an array or wrapper - is updated.
	/// </remarks>
	/// <param name="schemaInterface">The interface to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the interface was renamed; false if the name is empty or collides with another interface.</returns>
	public bool TryRenameInterface(SchemaInterface schemaInterface, InterfaceName newName)
	{
		Ensure.NotNull(schemaInterface);
		Ensure.NotNull(newName);

		if (!InterfacesInternal.Contains(schemaInterface) ||
			!CanRename(schemaInterface.Name, newName, InterfacesInternal.Select(i => i.Name.ToString())))
		{
			return false;
		}

		InterfaceName oldName = schemaInterface.Name;
		schemaInterface.Rename(newName);

		foreach (BaseType type in EnumerateMemberTypes())
		{
			if (type is Interface interfaceType && interfaceType.InterfaceName == oldName)
			{
				interfaceType.InterfaceName = newName;
			}
		}

		return true;
	}

	/// <summary>
	/// Renames a data source. Nothing in the schema references a data source by name, so there is
	/// nothing to cascade.
	/// </summary>
	/// <param name="dataSource">The data source to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the data source was renamed; false if the name is empty or collides.</returns>
	public bool TryRenameDataSource(DataSource dataSource, DataSourceName newName)
	{
		Ensure.NotNull(dataSource);
		Ensure.NotNull(newName);

		if (!DataSourcesInternal.Contains(dataSource) ||
			!CanRename(dataSource.Name, newName, DataSourcesInternal.Select(d => d.Name.ToString())))
		{
			return false;
		}

		dataSource.Rename(newName);
		return true;
	}

	/// <summary>
	/// Renames a code generator. Nothing in the schema references a code generator by name, so
	/// there is nothing to cascade.
	/// </summary>
	/// <param name="codeGenerator">The code generator to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <returns>True if the code generator was renamed; false if the name is empty or collides.</returns>
	public bool TryRenameCodeGenerator(SchemaCodeGenerator codeGenerator, CodeGeneratorName newName)
	{
		Ensure.NotNull(codeGenerator);
		Ensure.NotNull(newName);

		if (!CodeGeneratorsInternal.Contains(codeGenerator) ||
			!CanRename(codeGenerator.Name, newName, CodeGeneratorsInternal.Select(g => g.Name.ToString())))
		{
			return false;
		}

		codeGenerator.Rename(newName);
		return true;
	}

	/// <summary>
	/// Repoints every array key that named <paramref name="oldKey"/> on the given element class.
	/// </summary>
	/// <param name="elementClassName">The class whose member was renamed.</param>
	/// <param name="oldKey">The member's previous name.</param>
	/// <param name="newKey">The member's new name.</param>
	internal void RepointArrayKeys(ClassName elementClassName, MemberName oldKey, MemberName newKey)
	{
		foreach (BaseType type in EnumerateMemberTypes())
		{
			if (type is Array array &&
				array.Key == oldKey &&
				array.ElementType is Object elementObject &&
				elementObject.ClassName == elementClassName)
			{
				array.Key = newKey;
			}
		}
	}

	/// <summary>
	/// Determines whether a rename is permitted: the new name must be non-empty, and must not be
	/// taken by a sibling unless it is the element's own current name.
	/// </summary>
	private static bool CanRename(string oldName, string newName, IEnumerable<string> siblingNames) =>
		!string.IsNullOrEmpty(newName) &&
		(string.Equals(oldName, newName, StringComparison.Ordinal) ||
			!siblingNames.Any(n => string.Equals(n, newName, StringComparison.Ordinal)));

	/// <summary>
	/// Enumerates every type reference in the schema: each member's type, each interface function's
	/// return type and parameter types, each semantic type's underlying type, and every type nested
	/// inside those.
	/// </summary>
	private IEnumerable<BaseType> EnumerateMemberTypes() =>
		ClassesInternal
			.SelectMany(c => c.Members)
			.Select(m => m.Type)
			.Concat(InterfacesInternal
				.SelectMany(i => i.Functions)
				.SelectMany(f => f.Parameters.Select(p => p.Type).Prepend(f.ReturnType)))
			.Concat(SemanticTypesInternal.Select(t => t.UnderlyingType))
			.SelectMany(ExpandType);

	/// <summary>
	/// Yields a type and every type nested inside it, through arrays, vectors and wrappers.
	/// </summary>
	private static IEnumerable<BaseType> ExpandType(BaseType type)
	{
		yield return type;

		BaseType? nested = type switch
		{
			Array array => array.ElementType,
			Vector vector => vector.ElementType,
			WrapperType wrapper => wrapper.ElementType,
			_ => null,
		};

		if (nested is not null)
		{
			foreach (BaseType inner in ExpandType(nested))
			{
				yield return inner;
			}
		}
	}
}
