// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// A reference to an interface declared in this schema.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="Object"/> for a class: it names the interface rather than holding
/// it, and resolves the name against the owning schema on demand so a rename repoints rather than
/// dangles.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "We're mimicing the type")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Lazy loading requires backing field")]
public class Interface : BaseType
{
	private SchemaInterface? internalInterface;

	/// <summary>
	/// Gets the interface this type names, or <see langword="null"/> when it does not resolve.
	/// </summary>
	/// <remarks>
	/// Named <c>Declaration</c> rather than <c>Interface</c> because a member may not share its
	/// enclosing type's name. <see cref="Object.Class"/> has no such collision and so keeps the
	/// referent's own word.
	/// </remarks>
	[JsonIgnore]
	public SchemaInterface? Declaration
	{
		get
		{
			if (!string.IsNullOrEmpty(InterfaceName) && internalInterface?.Name != InterfaceName)
			{
				ParentSchema?.TryGetInterface(InterfaceName, out internalInterface);
			}

			return internalInterface;
		}
	}

	/// <summary>
	/// Gets or sets the name of the interface being referenced.
	/// </summary>
	/// <remarks>
	/// Settable rather than init-only so a rename can repoint this reference, matching
	/// <see cref="Object.ClassName"/>.
	/// </remarks>
	public InterfaceName InterfaceName { get; set; } = new();

	/// <inheritdoc />
	public override string ToString() => InterfaceName;

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Interface otherInterface
			&& string.Equals(InterfaceName, otherInterface.InterfaceName, StringComparison.Ordinal);

	/// <inheritdoc />
	protected override int GetHashCodeCore() =>
		StringComparer.Ordinal.GetHashCode(InterfaceName.ToString());
}
