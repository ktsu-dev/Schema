// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Quantities;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// The metadata panel folded behind each member row: the unit, range, default, interpolation,
/// network encoding and editor hint a member can carry.
/// </summary>
/// <remarks>
/// Driven through the real editor rather than by calling the model, because every one of these is
/// an immediate-mode control whose value only reaches the schema through what the widget reports -
/// which is the part that has no coverage anywhere else.
/// </remarks>
[TestClass]
public sealed class MemberSemanticsTests
{
	private EditorHarness harness = null!;
	private Schema schema = null!;
	private SchemaMember speed = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();
		EditField.Reset();

		schema = new Schema();
		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		speed = body.AddMember("Speed".As<MemberName>())!;
		speed.SetType(new SchemaTypes.Float());

		harness.Editor.CurrentSchema = schema;
		harness.Editor.EditClass(body);
	}

	[TestCleanup]
	public void StopEditor()
	{
		EditField.Reset();
		harness.Dispose();
	}

	/// <summary>
	/// Folds the metadata panel open for the member the tests edit.
	/// </summary>
	private void OpenSemantics() => harness.Click("memberSpeed/ToggleSemantics");

	/// <summary>
	/// Chooses a unit from the picker, narrowing the list first.
	/// </summary>
	/// <remarks>
	/// The registry holds close to two hundred units and the popup draws all of them, so a unit
	/// past the first screenful is recorded at a position outside the popup and cannot be clicked.
	/// Typing into the search box is what a person does too, and the selection is confirmed rather
	/// than applied by the click that makes it.
	/// </remarks>
	private void ChooseUnit(string search, string option)
	{
		harness.Click("memberSpeed/Unit");
		harness.TypeInto("searchable-list/search", search);
		harness.Click($"searchable-list/{option}");
		harness.Click("searchable-list/ok");
	}

	[TestMethod]
	public void TheMetadataIsFoldedAwayUntilItIsAskedFor()
	{
		Assert.IsFalse(harness.IsOnScreen("memberSpeed/Unit"), "the panel was drawn before it was opened");

		OpenSemantics();

		Assert.IsTrue(harness.IsOnScreen("memberSpeed/Unit"));
		Assert.IsTrue(harness.IsOnScreen("memberSpeed/Interpolation"));
	}

	// ------------------------------------------------------------------- units

	[TestMethod]
	public void ChoosingAUnitWritesItsSymbol()
	{
		OpenSemantics();
		ChooseUnit("MeterPerSecond", "MeterPerSecond (m/s)");

		Assert.AreEqual("m/s", speed.Unit?.ToString());
	}

	/// <summary>
	/// The symbol is what a schema usually writes, but two symbols in the registry belong to more
	/// than one unit, and one of the two is not even the same dimension. A picker knows which unit
	/// was chosen, so it must not write text that loses that.
	/// </summary>
	[TestMethod]
	public void AUnitWhoseSymbolIsSharedIsWrittenByName()
	{
		OpenSemantics();
		ChooseUnit("Radian", "Radian (rad)");

		Assert.AreEqual("Radian", speed.Unit?.ToString(), "'rad' is also the symbol of Rad, an absorbed radiation dose");
		Assert.IsTrue(speed.TryResolveUnit(out IUnit? unit, out string error), error);
		Assert.AreEqual("Radian", unit!.Name);
	}

	[TestMethod]
	public void ChoosingAUnitIsUndoable()
	{
		OpenSemantics();
		ChooseUnit("MeterPerSecond", "MeterPerSecond (m/s)");

		harness.Editor.UndoRedo.Undo();

		Assert.IsNull(speed.Unit);
	}

	// ------------------------------------------------------------------ ranges

	[TestMethod]
	public void AMemberIsUnboundedUntilARangeIsAdded()
	{
		OpenSemantics();
		Assert.IsNull(speed.Range);
		Assert.IsFalse(harness.IsOnScreen("memberSpeed/field/RangeMinimum"), "the bounds were drawn without a range");

		harness.Click("memberSpeed/Bounded");

		Assert.IsNotNull(speed.Range);
		Assert.IsTrue(harness.IsOnScreen("memberSpeed/field/RangeMinimum"));
	}

	[TestMethod]
	public void TheBoundsAreWrittenAsTyped()
	{
		OpenSemantics();
		harness.Click("memberSpeed/Bounded");

		harness.Commit("memberSpeed/field/RangeMinimum", "-2.5");
		harness.Commit("memberSpeed/field/RangeMaximum", "10");

		Assert.AreEqual(-2.5, speed.Range?.Minimum);
		Assert.AreEqual(10.0, speed.Range?.Maximum);
	}

	/// <summary>
	/// A half-typed number is not a number, and the field holds one for as long as it takes to type
	/// a negative or a decimal. Nothing reaches the schema until the edit is finished, and text
	/// that is not a number at all leaves the bound where it was.
	/// </summary>
	[TestMethod]
	public void TextThatIsNotANumberLeavesTheBoundAlone()
	{
		OpenSemantics();
		harness.Click("memberSpeed/Bounded");
		harness.Commit("memberSpeed/field/RangeMaximum", "4");

		harness.Commit("memberSpeed/field/RangeMaximum", "fast");

		Assert.AreEqual(4.0, speed.Range?.Maximum);
	}

	[TestMethod]
	public void ARangeCanBeMadeToWrap()
	{
		OpenSemantics();
		harness.Click("memberSpeed/Bounded");
		harness.Commit("memberSpeed/field/RangeMaximum", "6.2831853");

		harness.Click("memberSpeed/Wrap");

		Assert.IsTrue(speed.Range?.Wrap);
		Assert.AreEqual(6.2831853, speed.Range?.Maximum, "wrapping replaced the range rather than editing it");
	}

	/// <summary>
	/// One entry per editing session, not per keystroke: a range is immutable, so each edit
	/// replaces it, and an undo has to put back the range as it stood before that one edit.
	/// </summary>
	[TestMethod]
	public void EditingABoundIsOneUndoEntry()
	{
		OpenSemantics();
		harness.Click("memberSpeed/Bounded");
		harness.Commit("memberSpeed/field/RangeMaximum", "1");
		harness.Commit("memberSpeed/field/RangeMaximum", "2");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual(1.0, speed.Range?.Maximum);
	}

	// ---------------------------------------------------------------- defaults

	[TestMethod]
	public void ADefaultIsGivenAKindBeforeAValue()
	{
		OpenSemantics();
		Assert.IsNull(speed.DefaultValue);

		harness.Click("memberSpeed/DefaultKind");
		harness.Click("default-kind-option/Number");
		harness.Commit("memberSpeed/field/DefaultNumber", "12.5");

		Assert.IsInstanceOfType<NumberDefault>(speed.DefaultValue);
		Assert.AreEqual(12.5, ((NumberDefault)speed.DefaultValue!).Value);
	}

	[TestMethod]
	public void ADefaultCanBeTakenAwayAgain()
	{
		OpenSemantics();
		harness.Click("memberSpeed/DefaultKind");
		harness.Click("default-kind-option/Number");
		Assert.IsNotNull(speed.DefaultValue);

		harness.Click("memberSpeed/DefaultKind");
		harness.Click("default-kind-option/<none>");

		Assert.IsNull(speed.DefaultValue);
	}

	/// <summary>
	/// An enum member's default is offered as the enum's own values, because a name that is not one
	/// of them is the mistake this default invites and the schema already knows which are allowed.
	/// </summary>
	[TestMethod]
	public void AnEnumMemberOffersTheValuesOfItsEnum()
	{
		SchemaEnum kind = schema.AddEnum("Kind".As<EnumName>())!;
		kind.TryAddValue("Sphere".As<EnumValueName>());
		kind.TryAddValue("Box".As<EnumValueName>());

		SchemaClass collider = schema.AddClass("Collider".As<ClassName>())!;
		SchemaMember shape = collider.AddMember("Shape".As<MemberName>())!;
		shape.SetType(new SchemaTypes.Enum { EnumName = "Kind".As<EnumName>() });
		harness.Editor.EditClass(collider);

		harness.Click("memberShape/ToggleSemantics");
		harness.Click("memberShape/DefaultKind");
		harness.Click("default-kind-option/Text");
		harness.Click("memberShape/DefaultEnumValue");
		harness.Click("enum-default-option/Box");

		Assert.AreEqual("Box", shape.DefaultValue?.ToString());
		Assert.AreEqual(0, harness.Editor.CurrentSchema!.Validate().Count(issue => issue.Severity == SchemaValidationSeverity.Error));
	}

	// ----------------------------------------------------------- interpolation

	[TestMethod]
	public void ChoosingAnInterpolationModeSetsIt()
	{
		OpenSemantics();

		harness.Click("memberSpeed/Interpolation");
		harness.Click("interpolation-option/Linear");

		Assert.AreEqual(Interpolation.Linear, speed.Interpolation);

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual(Interpolation.None, speed.Interpolation);
	}

	// ------------------------------------------------------------- networking

	[TestMethod]
	public void AMemberCarriesNoNetworkGuidanceUntilItIsAsked()
	{
		OpenSemantics();
		Assert.IsNull(speed.Network);
		Assert.IsFalse(harness.IsOnScreen("memberSpeed/field/Quantise"), "the step was drawn without an encoding");

		harness.Click("memberSpeed/Encoded");

		Assert.IsNotNull(speed.Network);
		Assert.IsFalse(speed.Network!.IsQuantised, "a new encoding should send full precision");
	}

	[TestMethod]
	public void TheQuantisationStepAndDeltaFlagAreWritten()
	{
		OpenSemantics();
		harness.Click("memberSpeed/Encoded");

		harness.Commit("memberSpeed/field/Quantise", "0.01");
		harness.Click("memberSpeed/Delta");

		Assert.AreEqual(0.01, speed.Network?.Quantise);
		Assert.IsTrue(speed.Network?.Delta);
		Assert.IsTrue(speed.Network?.IsQuantised);
	}

	// ------------------------------------------------------------ editor hint

	[TestMethod]
	public void TheEditorHintIsFreeText()
	{
		OpenSemantics();

		harness.Commit("memberSpeed/field/EditorHint", "dial");

		Assert.AreEqual("dial", speed.Editor?.ToString());
	}

	/// <summary>
	/// Emptying the field removes the hint rather than writing an empty one, so the property leaves
	/// the file instead of sitting in it saying nothing.
	/// </summary>
	[TestMethod]
	public void ClearingTheEditorHintRemovesIt()
	{
		OpenSemantics();
		harness.Commit("memberSpeed/field/EditorHint", "dial");

		harness.Clear("memberSpeed/field/EditorHint");

		Assert.IsNull(speed.Editor);
	}

	// ------------------------------------------------------------ folded state

	/// <summary>
	/// A folded row is the only place most members are ever seen, so it has to say whether there is
	/// anything behind the fold.
	/// </summary>
	[TestMethod]
	public void AFoldedRowSaysWhetherThereIsAnythingBehindIt()
	{
		Assert.IsFalse(MemberSemanticsPanel.HasSemantics(speed));
		StringAssert.Contains(MemberSemanticsPanel.DescribeSemantics(speed), "No unit");

		speed.Unit = "m/s".As<UnitSymbol>();
		speed.Range = new MemberRange { Minimum = 0.0, Maximum = 10.0 };
		speed.Interpolation = Interpolation.Linear;

		Assert.IsTrue(MemberSemanticsPanel.HasSemantics(speed));

		string summary = MemberSemanticsPanel.DescribeSemantics(speed);
		StringAssert.Contains(summary, "in m/s");
		StringAssert.Contains(summary, "[0, 10]");
		StringAssert.Contains(summary, "linear");
	}

	/// <summary>
	/// Interpolation alone counts, because it is the one property whose absence is a value rather
	/// than a null - so asking whether it is set has to mean asking whether it is
	/// <see cref="Interpolation.None"/>.
	/// </summary>
	[TestMethod]
	public void InterpolationAloneIsEnoughToCountAsMetadata()
	{
		speed.Interpolation = Interpolation.Step;

		Assert.IsTrue(MemberSemanticsPanel.HasSemantics(speed));
	}
}
