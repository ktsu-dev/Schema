// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CA1034 // Nested types are what the nested-collision case is about.
#pragma warning disable CA1724 // The fixture namespaces exist to share a type name.

namespace ktsu.Schema.Tests.CollisionFixtures
{
	public class HoldsBothNamespaces
	{
		public Alpha.Item? First { get; set; }

		public Beta.Item? Second { get; set; }
	}

	public class HoldsAlpha
	{
		public Alpha.Item? Value { get; set; }
	}

	public class HoldsBeta
	{
		public Beta.Item? Value { get; set; }
	}

	public class HoldsOneTwice
	{
		public Alpha.Item? First { get; set; }

		public Alpha.Item? Second { get; set; }
	}

	public class HoldsBothNested
	{
		public Left.Leaf? First { get; set; }

		public Right.Leaf? Second { get; set; }
	}

	public static class Left
	{
		public class Leaf
		{
			public int Count { get; set; }
		}
	}

	public static class Right
	{
		public class Leaf
		{
			public string Label { get; set; } = string.Empty;
		}
	}

	public class HoldsBothEnums
	{
		public Alpha.Mode First { get; set; }

		public Beta.Mode Second { get; set; }
	}

	public class Box<T>
	{
		public T? Value { get; set; }
	}

	public class HoldsBox
	{
		public Box<int>? Boxed { get; set; }
	}

	public class Node
	{
		public Node? Next { get; set; }
	}
}

namespace ktsu.Schema.Tests.CollisionFixtures.Alpha
{
	public class Item
	{
		public int Count { get; set; }
	}

	public enum Mode
	{
		On,
		Off,
	}
}

namespace ktsu.Schema.Tests.CollisionFixtures.Beta
{
	public class Item
	{
		public string Label { get; set; } = string.Empty;
	}

	public enum Mode
	{
		Fast,
		Slow,
	}
}
