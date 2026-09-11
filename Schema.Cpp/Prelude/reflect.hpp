// @BANNER@
//
// The vocabulary a generated reflection table is written in.
//
// Nothing here describes any particular class. `Describe<T>` is specialised once per class in the
// table beside this file, and a validator, a serialiser, a network codec and an editor all read
// that instead of each being a generator with its own copy of the same facts.
//
// Two properties matter more than the rest.
//
// The first is that offsets are not computed here or by the generator. A generated table says
// `offsetof(Class, member)`, so the offset is whatever the compiler chose for this target, this
// ABI, these packing rules. Reflection therefore cannot drift from the layout it describes: it is
// derived from it, by the only thing that knows. A generator that computed offsets itself would be
// a second implementation of the C++ ABI, and would be wrong somewhere eventually.
//
// The second is that all of it is constexpr. A descriptor costs nothing at run time, has no static
// initialisation order to get wrong, and something built on it folds away when its inputs are
// known.
//
// Two of the enumerations below are generated from the schema library's own -- `TypeKind` from the
// types a member can have, `Interpolation` from the ways two values can be blended -- so a type
// added there appears here without an edit. Each is written twice, as the enumeration and as the
// names beside it, from one list: two spellings of one fact that cannot disagree, which is what a
// switch would not have given.

#pragma once

#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>

namespace @NAMESPACE@
{

	// What a member is. Mirrors the schema's own set of types, which is why it is generated rather
	// than written here: the schema is where a type is added, and a hand-kept copy would fall
	// behind it silently.
	enum class TypeKind : std::uint8_t
	{
@TYPEKINDS@
	};

	inline constexpr std::string_view kTypeKindNames[] = {
@TYPEKINDNAMES@
	};

	[[nodiscard]] constexpr std::string_view to_string(TypeKind kind)
	{
		return kTypeKindNames[static_cast<std::size_t>(kind)];
	}

	// Whether a value between two states of a member means anything, and along what. An
	// orientation takes the shortest arc; an identifier is not something to be halfway through.
	enum class Interpolation : std::uint8_t
	{
@INTERPOLATIONS@
	};

	inline constexpr std::string_view kInterpolationNames[] = {
@INTERPOLATIONNAMES@
	};

	[[nodiscard]] constexpr std::string_view to_string(Interpolation interpolation)
	{
		return kInterpolationNames[static_cast<std::size_t>(interpolation)];
	}

	// Exponents over the SI base units, with angle as an eighth axis. A member's C++ type already
	// carries this where it is a semantic type, so this exists for the code that cannot see the
	// type: an editor deciding what suffix to draw, a validator checking a number parsed from a
	// save file.
	struct Dimension
	{
		std::int8_t length = 0;
		std::int8_t mass = 0;
		std::int8_t time = 0;
		std::int8_t angle = 0;
		std::int8_t current = 0;
		std::int8_t temperature = 0;
		std::int8_t amount = 0;
		std::int8_t luminous = 0;

		[[nodiscard]] constexpr bool dimensionless() const
		{
			return length == 0 && mass == 0 && time == 0 && angle == 0 && current == 0 &&
				   temperature == 0 && amount == 0 && luminous == 0;
		}

		[[nodiscard]] friend constexpr bool operator==(const Dimension&, const Dimension&) = default;
	};

	struct Range
	{
		double minimum = 0.0;
		double maximum = 0.0;

		// A wrapping member declares its range as a period rather than a bound: 40 radians on a
		// [0, 2pi) heading is un-normalised, not invalid. Anything checking a range has to read
		// this too, or it rejects every angle nobody has reduced yet.
		bool wrap = false;
	};

	// How a member is quantised on the wire, in the member's own unit.
	struct Network
	{
		double quantise = 0.0;
		bool delta = false;
	};

	struct EnumValue
	{
		std::string_view name;
		std::int64_t value = 0;
	};

	struct MemberInfo
	{
		std::string_view name;
		std::string_view description;

		// The unit as the schema wrote it ("m/s"), for display. `dimension` is the same fact in
		// the form code should branch on.
		std::string_view unit;

		// What the schema declares the member as. A semantic type is `Semantic` here, because that
		// is the word the schema file uses and an editor showing the member wants it.
		TypeKind kind = TypeKind::None;

		// What the bytes at `offset` actually are, with a semantic type followed down its chain of
		// refinement to the type it is stored as. Equal to `kind` for everything else. Anything
		// reading a value out of untyped bytes -- a save file, a packet -- branches on this one;
		// anything showing the member to a person branches on the other.
		TypeKind representation = TypeKind::None;

		Dimension dimension;

		// Both come from the compiler, through offsetof and sizeof in the generated table.
		std::uint32_t offset = 0;
		std::uint32_t size = 0;

		// std::optional is not used for any of these: this is a static table entry that an editor
		// reads every byte of, so a flag beside the value keeps it trivially copyable and
		// dumpable.
		bool has_range = false;
		Range range;

		Interpolation interpolation = Interpolation::None;

		// Free text rather than an enumeration, because the set of useful controls is open: a
		// dial, a colour wheel, a curve, a file picker. An editor that does not recognise a hint
		// falls back to the control the member's type implies, so an unknown one costs nothing.
		std::string_view editor_hint;

		bool has_network = false;
		Network network;

		// Empty unless kind == TypeKind::Enum.
		std::span<const EnumValue> enum_values;
	};

	struct ClassInfo
	{
		std::string_view name;
		std::string_view description;
		std::uint32_t size = 0;
		std::uint32_t alignment = 0;

		// Whether the schema says an instance is copied whole -- across a language boundary, into
		// a save file, onto the wire -- without anyone reading a member on the way. That is what
		// makes the offsets below load-bearing rather than merely true.
		bool travels_as_bytes = false;

		std::span<const MemberInfo> members;

		static constexpr std::size_t npos = static_cast<std::size_t>(-1);

		// Lookup answers an index rather than a pointer, and that is not a style preference. Under
		// -fsanitize=address, GCC stops treating the address of a sanitized global as a constant
		// expression, so a constexpr function returning `&members[i]` compiles everywhere except
		// under the sanitizer -- and the sanitizer build is the one that has to agree with the
		// others. An index has no such problem.
		[[nodiscard]] constexpr std::size_t index_of(std::string_view member_name) const
		{
			for(std::size_t i = 0; i < members.size(); ++i)
			{
				if(members[i].name == member_name)
				{
					return i;
				}
			}
			return npos;
		}

		[[nodiscard]] constexpr bool has(std::string_view member_name) const
		{
			return index_of(member_name) != npos;
		}

		// Throws when the member is absent, which in a constant expression is a compile error
		// naming the line that asked rather than a silent wrong answer.
		[[nodiscard]] constexpr const MemberInfo& member(std::string_view member_name) const
		{
			for(const auto& candidate : members)
			{
				if(candidate.name == member_name)
				{
					return candidate;
				}
			}
			throw "no member of that name on this class";
		}
	};

	// Specialised by the generated table, once per class. Left undefined here so that reflecting
	// on a type nobody generated a table for is a compile error naming that type, rather than a
	// link error or an empty descriptor.
	template <typename T>
	struct Describe;

	template <typename T>
	[[nodiscard]] constexpr const ClassInfo& describe()
	{
		return Describe<T>::info;
	}

	// True when T has a generated descriptor. Lets code ask "is this a schema class?" without
	// instantiating Describe<T> and failing.
	template <typename T>
	concept Reflected = requires { Describe<T>::info; };

} // namespace @NAMESPACE@
