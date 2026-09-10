// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// A reference to a semantic type declared in this schema.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="Object"/> for a class and <see cref="Interface"/> for an
/// interface: it names the semantic type rather than holding it, and resolves the name against the
/// owning schema on demand so a rename repoints rather than dangles.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Lazy loading requires backing field")]
public class Semantic : BaseType
{
	private SchemaSemanticType? internalSemanticType;

	/// <summary>
	/// Gets the semantic type this names, or <see langword="null"/> when it does not resolve.
	/// </summary>
	[JsonIgnore]
	public SchemaSemanticType? Declaration
	{
		get
		{
			if (!string.IsNullOrEmpty(SemanticTypeName) && internalSemanticType?.Name != SemanticTypeName)
			{
				ParentSchema?.TryGetSemanticType(SemanticTypeName, out internalSemanticType);
			}

			return internalSemanticType;
		}
	}

	/// <summary>
	/// Gets or sets the name of the semantic type being referenced.
	/// </summary>
	public SemanticTypeName SemanticTypeName { get; set; } = new();

	/// <inheritdoc />
	public override string ToString() => SemanticTypeName;

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Semantic otherSemantic
			&& string.Equals(SemanticTypeName, otherSemantic.SemanticTypeName, StringComparison.Ordinal);

	/// <inheritdoc />
	protected override int GetHashCodeCore() =>
		StringComparer.Ordinal.GetHashCode(SemanticTypeName.ToString());
}
