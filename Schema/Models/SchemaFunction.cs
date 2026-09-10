// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// One function of an interface: its parameters, in order, and what it returns.
/// </summary>
/// <remarks>
/// A function carries no <c>throws</c> and no ownership or lifetime annotations. It does not need
/// them: fallibility is <see cref="Types.Result"/> in the return type, a borrow is
/// <see cref="Types.Span"/>, something the caller may keep is <see cref="Types.Handle"/>, and
/// const-ness is <see cref="SchemaParameter.Direction"/>. Each of those is a convention the engine
/// keeps rather than a fact each signature restates, which is what stops the schema growing into a
/// second, worse declaration language.
/// </remarks>
public class SchemaFunction : SchemaInterfaceChild<FunctionName>
{
	[JsonInclude]
	[JsonPropertyName("parameters")]
	internal Collection<SchemaParameter> ParametersInternal { get; private set; } = [];

	/// <summary>
	/// Gets the function's parameters, in declaration order.
	/// </summary>
	/// <remarks>
	/// A list rather than a set because order is the signature. Two parameters may not share a
	/// name, which <see cref="Schema.Validate"/> reports.
	/// </remarks>
	[JsonIgnore]
	public IReadOnlyList<SchemaParameter> Parameters => ParametersInternal;

	/// <summary>
	/// Gets the type the function returns.
	/// </summary>
	/// <remarks>
	/// <see cref="Types.Void"/> for a function that returns nothing — never <see cref="None"/>,
	/// which means the author has not chosen yet and is not generatable.
	/// </remarks>
	[JsonInclude]
	public BaseType ReturnType { get; private set; } = new Void();

	/// <summary>
	/// Sets the type the function returns.
	/// </summary>
	/// <param name="type">The return type to set.</param>
	public void SetReturnType(BaseType type)
	{
		Ensure.NotNull(type);
		ReturnType = type;
		ReturnType.AssociateWith(ParentSchema);
	}

	/// <summary>
	/// Appends a parameter to the signature.
	/// </summary>
	/// <param name="name">The parameter's name, unique within this function.</param>
	/// <returns>The new parameter, or <see langword="null"/> when the name is already taken.</returns>
	public SchemaParameter? AddParameter(ParameterName name)
	{
		Ensure.NotNull(name);
		if (ParametersInternal.Any(parameter => parameter.Name == name))
		{
			return null;
		}

		SchemaParameter parameter = new() { Name = name };
		parameter.AssociateWith(this);
		ParametersInternal.Add(parameter);
		return parameter;
	}

	/// <summary>
	/// Removes a parameter from the signature.
	/// </summary>
	/// <param name="parameter">The parameter to remove.</param>
	/// <returns><see langword="true"/> when it was present and removed.</returns>
	public bool TryRemoveParameter(SchemaParameter parameter) => ParametersInternal.Remove(parameter);

	/// <summary>
	/// Finds a parameter by name.
	/// </summary>
	/// <param name="name">The name to look for.</param>
	/// <param name="parameter">The parameter, when found.</param>
	/// <returns><see langword="true"/> when a parameter of that name exists.</returns>
	public bool TryGetParameter(ParameterName name, out SchemaParameter? parameter)
	{
		parameter = ParametersInternal.FirstOrDefault(candidate => candidate.Name == name);
		return parameter is not null;
	}

	/// <summary>
	/// Re-establishes the parent links this function's parameters lost on deserialization.
	/// </summary>
	internal void Reassociate()
	{
		ReturnType.AssociateWith(ParentSchema);
		foreach (SchemaParameter parameter in ParametersInternal)
		{
			parameter.AssociateWith(this);
			if (ParentSchema is not null)
			{
				parameter.AssociateWith(ParentSchema);
			}
		}
	}

	/// <inheritdoc />
	public override bool TryRemove() => ParentInterface?.TryRemoveFunction(this) ?? false;

	/// <inheritdoc />
	public override string ToString() =>
		$"{ReturnType} {Name}({string.Join(", ", ParametersInternal)})";
}
