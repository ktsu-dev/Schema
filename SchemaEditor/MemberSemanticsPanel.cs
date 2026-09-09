// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System.Globalization;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Semantics.Quantities;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

using SchemaTypes = Schema.Models.Types;

/// <summary>
/// The panel for the semantic metadata a member carries beside its type: what its values are
/// measured in, the range they may take, the value it starts at, how two of its states blend, how
/// it goes on the wire, and how an editor should present it.
/// </summary>
/// <remarks>
/// Folded away behind a toggle rather than shown in the member row. The row is already five
/// controls wide, and all six of these are absent on most members - an identifier measures
/// nothing, has no range and blends with nothing - so a row that always drew them would spend
/// most of its width saying "none" six times.
/// <para>
/// Its own class rather than another part of <see cref="SchemaEditor"/>, so that the six kinds of
/// metadata and the unit registry behind them are coupled to the panel that draws them rather than
/// to the editor as a whole.
/// </para>
/// </remarks>
/// <param name="schemaEditor">The editor this panel draws inside, which owns the undo stack.</param>
internal sealed class MemberSemanticsPanel(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	private static float FieldWidth => SchemaEditor.FieldWidth;
	/// <summary>
	/// The width of the label column inside the metadata panel, in ems, so the controls line up
	/// under each other whatever the font size.
	/// </summary>
	private const float MetadataLabelEms = 7.0f;

	/// <summary>
	/// What the kind picker calls each kind of default, and the order it offers them in.
	/// </summary>
	/// <remarks>
	/// Names rather than types, because "no default at all" is one of the choices and has no type
	/// to name it with.
	/// </remarks>
	private static readonly string[] DefaultKinds = [SchemaEditor.NoneOption, "Number", "Boolean", "Text"];

	/// <summary>
	/// Draws the metadata editor for a member, and the controls that set it.
	/// </summary>
	/// <param name="schema">The schema the member belongs to, for resolving an enum default.</param>
	/// <param name="member">The member being edited.</param>
	internal void Show(Schema schema, SchemaMember member)
	{
		ImGui.Indent();

		ShowUnitField(member);
		ShowRangeFields(member);
		ShowDefaultFields(schema, member);
		ShowInterpolationField(member);
		ShowNetworkFields(member);
		ShowEditorHintField(member);

		ImGui.Unindent();
	}

	/// <summary>
	/// Whether a member carries any metadata at all, which is what the folded row reports.
	/// </summary>
	/// <param name="member">The member to ask about.</param>
	/// <returns>True when at least one of the six properties is set.</returns>
	internal static bool HasSemantics(SchemaMember member)
	{
		Ensure.NotNull(member);

		return member.Unit is not null
			|| member.Range is not null
			|| member.DefaultValue is not null
			|| member.Network is not null
			|| member.Editor is not null
			|| member.Interpolation is not Interpolation.None;
	}

	/// <summary>
	/// Draws a label and leaves the cursor where the row's first control goes.
	/// </summary>
	private static void ShowMetadataLabel(string label)
	{
		float column = ImGui.GetFontSize() * MetadataLabelEms;
		float start = ImGui.GetCursorPosX();
		ImGui.TextUnformatted(label);
		ImGui.SameLine();
		ImGui.SetCursorPosX(start + column);
	}

	/// <summary>
	/// Draws the unit picker.
	/// </summary>
	/// <remarks>
	/// A picker over the registry rather than a text field, because a unit typed by hand is only
	/// discovered to be wrong when the schema is next validated, and the registry is the thing
	/// that knows which spellings exist.
	/// </remarks>
	private void ShowUnitField(SchemaMember member)
	{
		ShowMetadataLabel("Unit:");

		bool clicked = ImGui.Button(member.Unit is null ? SchemaEditor.NoneOption : member.Unit.ToString(), new Vector2(FieldWidth, 0));
		ImGuiProbes.MarkItem("Unit");

		if (member.Unit is not null && ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(UnitRegistry.TryResolve(member.Unit, out IUnit? resolved, out string error)
				? $"{resolved!.Name} ({resolved.Symbol})"
				: error);
		}

		if (clicked)
		{
			SchemaMember captured = member;
			Popups.OpenUnitList("Select Unit", "Unit", UnitRegistry.All, captured.Unit, unit => SetUnit(captured, unit));
		}
	}

	/// <summary>
	/// Applies a chosen unit, or clears it.
	/// </summary>
	/// <remarks>
	/// What is written is <see cref="UnitRegistry.PreferredText"/> rather than the symbol, so the
	/// two units whose symbols are shared are written by name and the file always resolves back to
	/// the unit that was actually picked.
	/// </remarks>
	private void SetUnit(SchemaMember member, IUnit? unit)
	{
		UnitSymbol? previous = member.Unit;
		UnitSymbol? next = unit is null ? null : UnitRegistry.PreferredText(unit).As<UnitSymbol>();

		schemaEditor.Execute(new DelegateCommand(
			unit is null ? "Clear Unit" : $"Set Unit '{next}'",
			() => member.Unit = next,
			() => member.Unit = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Draws the range: whether the member has one, its bounds, and whether it wraps.
	/// </summary>
	private void ShowRangeFields(SchemaMember member)
	{
		ShowMetadataLabel("Range:");

		bool bounded = member.Range is not null;
		if (ImGui.Checkbox("##Bounded", ref bounded))
		{
			SetRange(member, bounded ? new MemberRange() : null, bounded ? "Add Range" : "Remove Range");
		}

		ImGuiProbes.MarkItem("Bounded");

		if (member.Range is not MemberRange range)
		{
			return;
		}

		ImGui.SameLine();
		if (EditField.Number("##RangeMinimum", FieldWidth * 0.5f, range.Minimum, out double minimum))
		{
			SetRange(member, new MemberRange { Minimum = minimum, Maximum = range.Maximum, Wrap = range.Wrap }, "Set Range Minimum");
		}

		ImGui.SameLine();
		if (EditField.Number("##RangeMaximum", FieldWidth * 0.5f, range.Maximum, out double maximum))
		{
			SetRange(member, new MemberRange { Minimum = range.Minimum, Maximum = maximum, Wrap = range.Wrap }, "Set Range Maximum");
		}

		ImGui.SameLine();
		ImGui.TextUnformatted("wrap");
		ImGui.SameLine();

		// A hidden label, like every other control here: a checkbox's visible label sits inside the
		// item's rectangle without being clickable, so a caption here would leave the middle of
		// the control - where a test aims - hitting nothing.
		bool wrap = range.Wrap;
		if (ImGui.Checkbox("##Wrap", ref wrap))
		{
			SetRange(member, new MemberRange { Minimum = range.Minimum, Maximum = range.Maximum, Wrap = wrap }, wrap ? "Wrap Range" : "Unwrap Range");
		}

		ImGuiProbes.MarkItem("Wrap");

		if (ImGui.IsItemHovered())
		{
			// The one thing about a range that is easy to read backwards, so it is said where the
			// question is asked rather than only in the format documentation.
			ImGui.SetTooltip("A wrapping range is a period rather than a bound: a value outside it is un-normalised, not invalid.");
		}
	}

	/// <summary>
	/// Replaces a member's range, recording one undo entry.
	/// </summary>
	/// <remarks>
	/// A range is immutable, so every edit to one is a replacement. That is what makes an undo of
	/// a bound a single assignment rather than a field-by-field restoration.
	/// </remarks>
	private void SetRange(SchemaMember member, MemberRange? next, string description)
	{
		MemberRange? previous = member.Range;
		schemaEditor.Execute(new DelegateCommand(
			description,
			() => member.Range = next,
			() => member.Range = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Draws the default: which kind of value it is, and the value itself.
	/// </summary>
	private void ShowDefaultFields(Schema schema, SchemaMember member)
	{
		ShowMetadataLabel("Default:");

		string kind = member.DefaultValue switch
		{
			NumberDefault => "Number",
			BooleanDefault => "Boolean",
			TextDefault => "Text",
			_ => SchemaEditor.NoneOption,
		};

		ImGui.Button(kind, new Vector2(FieldWidth * 0.6f, 0));
		ImGuiProbes.MarkItem("DefaultKind");
		ShowDefaultKindMenu(member);

		switch (member.DefaultValue)
		{
			case NumberDefault number:
				ImGui.SameLine();
				if (EditField.Number("##DefaultNumber", FieldWidth * 0.5f, number.Value, out double committed))
				{
					SetDefault(member, new NumberDefault { Value = committed }, "Set Default");
				}

				return;

			case BooleanDefault boolean:
				ImGui.SameLine();
				bool value = boolean.Value;
				if (ImGui.Checkbox("##DefaultBoolean", ref value))
				{
					SetDefault(member, new BooleanDefault { Value = value }, "Set Default");
				}

				ImGuiProbes.MarkItem("DefaultBoolean");
				return;

			case TextDefault text:
				ImGui.SameLine();
				ShowTextDefault(schema, member, text);
				return;

			default:
				return;
		}
	}

	/// <summary>
	/// Offers the kinds of default a member can have.
	/// </summary>
	private void ShowDefaultKindMenu(SchemaMember member)
	{
		if (!ImGui.BeginPopupContextItem("##DefaultKind", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		foreach (string kind in DefaultKinds)
		{
			bool chosen = ImGui.Selectable(kind);
			ImGuiProbes.MarkItem("default-kind-option", kind);
			if (chosen)
			{
				SetDefault(member, NewDefault(kind), kind == SchemaEditor.NoneOption ? "Remove Default" : $"Set Default Kind '{kind}'");
			}
		}

		ImGui.EndPopup();
	}

	/// <summary>
	/// Builds an empty default of the chosen kind.
	/// </summary>
	/// <remarks>
	/// Zero, false and the empty string: the kind is what the user asked for, and the value is the
	/// next thing they will type. A default is not the same as a zeroed value, but zero is the
	/// least surprising thing to offer while the field is still being filled in.
	/// </remarks>
	private static MemberDefault? NewDefault(string kind) => kind switch
	{
		"Number" => new NumberDefault(),
		"Boolean" => new BooleanDefault(),
		"Text" => new TextDefault(),
		_ => null,
	};

	/// <summary>
	/// Draws the value of a textual default: a picker of the enum's values on an enum member, and
	/// a text field on anything else.
	/// </summary>
	/// <remarks>
	/// A picker on an enum member because a name that is not one of the enum's values is the
	/// mistake this default invites, and the schema already knows which names are allowed.
	/// </remarks>
	private void ShowTextDefault(Schema schema, SchemaMember member, TextDefault text)
	{
		if (member.Type is not SchemaTypes.Enum enumType || !schema.TryGetEnum(enumType.EnumName, out SchemaEnum? target) || target is null)
		{
			if (EditField.Text("##DefaultText", FieldWidth, text.Value, out string committed))
			{
				SetDefault(member, new TextDefault { Value = committed }, "Set Default");
			}

			return;
		}

		ImGui.Button(string.IsNullOrEmpty(text.Value) ? SchemaEditor.NoneOption : text.Value, new Vector2(FieldWidth, 0));
		ImGuiProbes.MarkItem("DefaultEnumValue");

		if (!ImGui.BeginPopupContextItem("##DefaultEnumValue", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		foreach (string value in target.Values.Select(value => value.ToString()))
		{
			bool chosen = ImGui.Selectable(value);
			ImGuiProbes.MarkItem("enum-default-option", value);
			if (chosen)
			{
				SetDefault(member, new TextDefault { Value = value }, $"Set Default '{value}'");
			}
		}

		ImGui.EndPopup();
	}

	/// <summary>
	/// Replaces a member's default, recording one undo entry.
	/// </summary>
	private void SetDefault(SchemaMember member, MemberDefault? next, string description)
	{
		MemberDefault? previous = member.DefaultValue;
		schemaEditor.Execute(new DelegateCommand(
			description,
			() => member.DefaultValue = next,
			() => member.DefaultValue = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Draws the interpolation picker.
	/// </summary>
	private void ShowInterpolationField(SchemaMember member)
	{
		ShowMetadataLabel("Blends:");

		ImGui.Button(member.Interpolation.ToString(), new Vector2(FieldWidth, 0));
		ImGuiProbes.MarkItem("Interpolation");

		if (!ImGui.BeginPopupContextItem("##Interpolation", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		foreach (Interpolation mode in System.Enum.GetValues<Interpolation>())
		{
			bool chosen = ImGui.Selectable(mode.ToString());
			ImGuiProbes.MarkItem("interpolation-option", mode.ToString());
			if (chosen)
			{
				SetInterpolation(member, mode);
			}
		}

		ImGui.EndPopup();
	}

	private void SetInterpolation(SchemaMember member, Interpolation mode)
	{
		Interpolation previous = member.Interpolation;
		schemaEditor.Execute(new DelegateCommand(
			$"Set Interpolation '{mode}'",
			() => member.Interpolation = mode,
			() => member.Interpolation = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Draws the network encoding: whether the member carries any guidance, its quantisation step
	/// and whether it is sent as a delta.
	/// </summary>
	private void ShowNetworkFields(SchemaMember member)
	{
		ShowMetadataLabel("Network:");

		bool encoded = member.Network is not null;
		if (ImGui.Checkbox("##Encoded", ref encoded))
		{
			SetNetwork(member, encoded ? new MemberNetwork() : null, encoded ? "Add Network Encoding" : "Remove Network Encoding");
		}

		ImGuiProbes.MarkItem("Encoded");

		if (member.Network is not MemberNetwork network)
		{
			return;
		}

		ImGui.SameLine();
		ImGui.TextUnformatted("step");
		ImGui.SameLine();
		if (EditField.Number("##Quantise", FieldWidth * 0.5f, network.Quantise, out double quantise))
		{
			SetNetwork(member, new MemberNetwork { Quantise = quantise, Delta = network.Delta }, "Set Quantisation Step");
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("The smallest change worth transmitting, in the member's own unit. Zero sends full precision.");
		}

		ImGui.SameLine();
		ImGui.TextUnformatted("delta");
		ImGui.SameLine();
		bool delta = network.Delta;
		if (ImGui.Checkbox("##Delta", ref delta))
		{
			SetNetwork(member, new MemberNetwork { Quantise = network.Quantise, Delta = delta }, delta ? "Send As Delta" : "Send In Full");
		}

		ImGuiProbes.MarkItem("Delta");
	}

	/// <summary>
	/// Replaces a member's network encoding, recording one undo entry.
	/// </summary>
	private void SetNetwork(SchemaMember member, MemberNetwork? next, string description)
	{
		MemberNetwork? previous = member.Network;
		schemaEditor.Execute(new DelegateCommand(
			description,
			() => member.Network = next,
			() => member.Network = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Draws the editor hint, which is free text because the set of useful controls is open.
	/// </summary>
	private void ShowEditorHintField(SchemaMember member)
	{
		ShowMetadataLabel("Editor:");

		string current = member.Editor?.ToString() ?? string.Empty;
		if (!EditField.Text("##EditorHint", FieldWidth, current, out string committed))
		{
			return;
		}

		EditorHint? previous = member.Editor;

		// Empty means no hint rather than an empty one, so clearing the field removes the property
		// from the file instead of writing "" into it.
		EditorHint? next = string.IsNullOrWhiteSpace(committed) ? null : committed.As<EditorHint>();

		schemaEditor.Execute(new DelegateCommand(
			next is null ? "Clear Editor Hint" : $"Set Editor Hint '{next}'",
			() => member.Editor = next,
			() => member.Editor = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Summarises a member's metadata for the folded row's tooltip.
	/// </summary>
	/// <param name="member">The member to describe.</param>
	/// <returns>A one-line summary, or an invitation to add some when there is none.</returns>
	internal static string DescribeSemantics(SchemaMember member)
	{
		Ensure.NotNull(member);

		if (!HasSemantics(member))
		{
			return "No unit, range, default, interpolation or network encoding.";
		}

		List<string> parts = [];

		if (member.Unit is not null)
		{
			parts.Add($"in {member.Unit}");
		}

		if (member.Range is not null)
		{
			parts.Add(member.Range.ToString());
		}

		if (member.DefaultValue is not null)
		{
			parts.Add($"defaults to {member.DefaultValue}");
		}

		if (member.Interpolation is not Interpolation.None)
		{
			parts.Add(member.Interpolation.ToString().ToLower(CultureInfo.InvariantCulture));
		}

		if (member.Network is not null)
		{
			parts.Add(member.Network.ToString());
		}

		if (member.Editor is not null)
		{
			parts.Add($"shown as {member.Editor}");
		}

		return string.Join(", ", parts);
	}
}
