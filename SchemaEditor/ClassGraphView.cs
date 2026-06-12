// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

using ktsu.ImGuiNodeEditor;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// Renders the relationships between schema classes and enums as an interactive node graph
/// using <c>ktsu.NodeGraph</c> metadata concepts and the <c>ktsu.ImGuiNodeEditor</c> engine.
///
/// Each class and enum becomes a node. A member whose type references another class or enum
/// (directly, or as the element type of an array) becomes a link from the owning class to the
/// referenced node. The force-directed layout in <see cref="NodeEditorEngine"/> arranges the
/// graph automatically, and nodes can be dragged to reposition them.
/// </summary>
internal sealed class ClassGraphView
{
	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();

	// The graph is derived from the schema, so it is only rebuilt when the schema's structure
	// changes. Rebuilding every frame would reset the force-directed layout and node positions.
	private string lastSignature = string.Empty;

	public ClassGraphView() =>
		// The force-directed layout is disabled by default; enable it so the graph self-arranges.
		engine.UpdatePhysicsSettings(engine.PhysicsSettings with { Enabled = true });

	/// <summary>
	/// Draws the class graph for the supplied schema, filling the available content region.
	/// </summary>
	/// <param name="schema">The schema to visualize, or null when no schema is open.</param>
	/// <param name="deltaTime">Seconds elapsed since the previous frame, used to advance physics.</param>
	public void Show(Schema? schema, float deltaTime)
	{
		if (schema is null)
		{
			ImGui.TextUnformatted("No schema is open.");
			return;
		}

		if (schema.Classes.Count == 0 && schema.Enums.Count == 0)
		{
			ImGui.TextUnformatted("This schema has no classes or enums to display.");
			return;
		}

		string signature = ComputeSignature(schema);
		if (signature != lastSignature)
		{
			Rebuild(schema);
			lastSignature = signature;
			engine.InitializeWorldOriginToCentroid();
		}

		// Advance the layout using the drag state captured during the previous frame's render.
		engine.SetDraggedNodes(renderer.CurrentlyDraggedNodes);
		engine.UpdatePhysics(deltaTime);

		Vector2 editorSize = ImGui.GetContentRegionAvail();
		renderer.Render(engine, editorSize);

		// Node transforms must be read back after rendering, while the editor context is active.
		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodePositionUpdates(engine))
		{
			engine.UpdateNodePosition(update.Key, update.Value);
		}

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodeDimensionUpdates(engine))
		{
			engine.UpdateNodeDimensions(update.Key, update.Value);
		}
	}

	/// <summary>
	/// Rebuilds the node graph from the schema. Classes and enums become nodes; member references
	/// between them become links.
	/// </summary>
	private void Rebuild(Schema schema)
	{
		engine.Clear();

		// 1. Collect node descriptors: classes first, then enums.
		List<NodeDescriptor> descriptors = [];
		HashSet<string> knownKeys = [];

		foreach (SchemaClass schemaClass in schema.Classes)
		{
			string key = ClassKey(schemaClass.Name);
			if (knownKeys.Add(key))
			{
				descriptors.Add(new NodeDescriptor(key, schemaClass.Name));
			}
		}

		foreach (SchemaEnum schemaEnum in schema.Enums)
		{
			string key = EnumKey(schemaEnum.Name);
			if (knownKeys.Add(key))
			{
				descriptors.Add(new NodeDescriptor(key, $"{schemaEnum.Name} [enum]"));
			}
		}

		// 2. Collect edges for members that reference another node that actually exists.
		List<Edge> edges = [];
		foreach (SchemaClass schemaClass in schema.Classes)
		{
			string sourceKey = ClassKey(schemaClass.Name);
			foreach (SchemaMember member in schemaClass.Members)
			{
				if (!TryResolveReference(member.Type, out string targetKey, out bool isCollection))
				{
					continue;
				}

				if (!knownKeys.Contains(targetKey))
				{
					continue;
				}

				string outputLabel = isCollection ? $"{member.Name} []" : member.Name;
				string incomingLabel = $"{schemaClass.Name}.{member.Name}";
				edges.Add(new Edge(sourceKey, targetKey, outputLabel, incomingLabel, sourceKey == targetKey));
			}
		}

		// 3. Build per-node pin label lists, recording which pin index each edge maps to.
		//    A class can be referenced many times, and the engine permits only one link per input
		//    pin, so each incoming reference gets its own dedicated input pin.
		Dictionary<string, List<string>> outputLabels = [];
		Dictionary<string, List<string>> inputLabels = [];
		foreach (NodeDescriptor descriptor in descriptors)
		{
			outputLabels[descriptor.Key] = [];
			inputLabels[descriptor.Key] = [];
		}

		foreach (Edge edge in edges)
		{
			List<string> sourceOutputs = outputLabels[edge.SourceKey];
			edge.OutputPinIndex = sourceOutputs.Count;
			sourceOutputs.Add(edge.OutputLabel);

			// Self-references cannot be linked (the engine rejects node-to-itself links), so they
			// are shown as an output pin only and do not consume a target input pin.
			if (!edge.IsSelf)
			{
				List<string> targetInputs = inputLabels[edge.TargetKey];
				edge.InputPinIndex = targetInputs.Count;
				targetInputs.Add(edge.IncomingLabel);
			}
		}

		// 4. Create the nodes, seeded around a circle so the layout has room to spread out.
		Dictionary<string, Node> nodesByKey = [];
		int count = descriptors.Count;
		float radius = MathF.Max(250.0f, count * 45.0f);
		for (int i = 0; i < count; i++)
		{
			NodeDescriptor descriptor = descriptors[i];
			float angle = (i / (float)count) * MathF.Tau;
			Vector2 position = new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
			Node node = engine.CreateNode(position, descriptor.Title, inputLabels[descriptor.Key], outputLabels[descriptor.Key]);
			nodesByKey[descriptor.Key] = node;
		}

		// 5. Link each non-self edge from its owning member's output pin to the referenced node.
		foreach (Edge edge in edges)
		{
			if (edge.IsSelf)
			{
				continue;
			}

			Node sourceNode = nodesByKey[edge.SourceKey];
			Node targetNode = nodesByKey[edge.TargetKey];
			engine.TryCreateLink(
				sourceNode.OutputPins[edge.OutputPinIndex].Id,
				targetNode.InputPins[edge.InputPinIndex].Id);
		}
	}

	/// <summary>
	/// Determines whether a member type references another class or enum, and resolves the node key.
	/// </summary>
	private static bool TryResolveReference(SchemaTypes.BaseType type, out string targetKey, out bool isCollection)
	{
		targetKey = string.Empty;
		isCollection = false;

		switch (type)
		{
			case SchemaTypes.Object objectType:
				targetKey = ClassKey(objectType.ClassName);
				return true;
			case SchemaTypes.Enum enumType:
				targetKey = EnumKey(enumType.EnumName);
				return true;
			case SchemaTypes.Array array when array.ElementType is SchemaTypes.Object elementObject:
				targetKey = ClassKey(elementObject.ClassName);
				isCollection = true;
				return true;
			case SchemaTypes.Array array when array.ElementType is SchemaTypes.Enum elementEnum:
				targetKey = EnumKey(elementEnum.EnumName);
				isCollection = true;
				return true;
			default:
				return false;
		}
	}

	/// <summary>
	/// Builds a compact signature of the schema's structure so the graph is only rebuilt when the
	/// classes, enums, members, or their types change. Member type display names already encode the
	/// referenced class or enum name (for example <c>Array(User)</c> or <c>Enum(Status)</c>).
	/// </summary>
	private static string ComputeSignature(Schema schema)
	{
		StringBuilder builder = new();
		foreach (SchemaClass schemaClass in schema.Classes)
		{
			builder.Append('C').Append((string)schemaClass.Name).Append('{');
			foreach (SchemaMember member in schemaClass.Members)
			{
				builder.Append((string)member.Name).Append(':').Append(member.Type.DisplayName).Append(';');
			}

			builder.Append('}');
		}

		foreach (SchemaEnum schemaEnum in schema.Enums)
		{
			builder.Append('E').Append((string)schemaEnum.Name).Append(';');
		}

		return builder.ToString();
	}

	private static string ClassKey(ClassName name) => $"C:{name}";

	private static string EnumKey(EnumName name) => $"E:{name}";

	private sealed record NodeDescriptor(string Key, string Title);

	/// <summary>
	/// A directed reference from one class to another class or enum, with the pin indices it
	/// maps to once nodes have been created.
	/// </summary>
	private sealed class Edge(string sourceKey, string targetKey, string outputLabel, string incomingLabel, bool isSelf)
	{
		public string SourceKey { get; } = sourceKey;
		public string TargetKey { get; } = targetKey;
		public string OutputLabel { get; } = outputLabel;
		public string IncomingLabel { get; } = incomingLabel;
		public bool IsSelf { get; } = isSelf;
		public int OutputPinIndex { get; set; }
		public int InputPinIndex { get; set; }
	}
}
