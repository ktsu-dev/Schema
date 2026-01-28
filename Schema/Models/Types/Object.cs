// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models.Types;

using ktsu.Schema.Models.Names;

/// <summary>
/// Represents an object type.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We're mimicing the type")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Lazy loading requires backing field")]
public class Object : BaseType
{
	private SchemaClass? internalClass;

	/// <summary>
	/// Gets the schema class associated with the object.
	/// </summary>
	public SchemaClass? Class
	{
		get
		{
			if (!string.IsNullOrEmpty(ClassName) && internalClass?.Name != ClassName)
			{
				ParentMember?.ParentSchema?.TryGetClass(ClassName, out internalClass);
			}

			return internalClass;
		}
	}

	/// <summary>
	/// Gets or sets the class name.
	/// </summary>
	public ClassName ClassName { get; init; } = new();

	/// <summary>
	/// Returns a string representation of the object.
	/// </summary>
	/// <returns>The class name.</returns>
	public override string ToString() => ClassName;
}
