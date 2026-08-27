// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;
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
	[JsonIgnore]
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
	/// <remarks>
	/// Settable rather than init-only so <see cref="Schema.TryRenameClass"/> can repoint this
	/// reference when the class it names is renamed.
	/// </remarks>
	public ClassName ClassName { get; set; } = new();

	/// <summary>
	/// Returns a string representation of the object.
	/// </summary>
	/// <returns>The class name.</returns>
	public override string ToString() => ClassName;

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Object otherObject && string.Equals(ClassName, otherObject.ClassName, StringComparison.Ordinal);

	/// <inheritdoc />
	protected override int GetHashCodeCore() => StringComparer.Ordinal.GetHashCode(ClassName.ToString());
}
