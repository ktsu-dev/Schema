// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// Rename support for <see cref="Schema"/>.
/// </summary>
/// <remarks>
/// Renaming is the schema's responsibility rather than the individual element's because a name
/// is referenced from elsewhere in the schema: an <see cref="Object"/> member names a class, an
/// <see cref="Types.Enum"/> member names an enum, a <see cref="DataSource"/> names a class, and
/// an <see cref="Array"/> names a key member. <see cref="SchemaChild{TName}.Rename"/> changes
/// only the name and would leave those references dangling, so the operations here cascade the
/// new name to every referrer and reject a name that collides with a sibling.
/// </remarks>
public partial class Schema
{
	/// <summary>
	/// Renames a class and repoints every reference to it.
	/// </summary>
	/// <remarks>
	/// Object-typed members and data sources that referenced the old name are updated to the new
	/// one, so the schema stays referentially intact.
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
	/// Renames an enum and repoints every member typed as it.
	/// </summary>
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
	/// Enumerates every member type in the schema, including the element types nested inside arrays.
	/// </summary>
	private IEnumerable<BaseType> EnumerateMemberTypes() =>
		ClassesInternal
			.SelectMany(c => c.Members)
			.SelectMany(m => ExpandType(m.Type));

	private static IEnumerable<BaseType> ExpandType(BaseType type)
	{
		yield return type;

		if (type is Array array)
		{
			foreach (BaseType nested in ExpandType(array.ElementType))
			{
				yield return nested;
			}
		}
	}
}
