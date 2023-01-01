#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddPipDecorationTraits : UpdateRule
	{
		public override string Name => "Add decoration traits for selection pips.";

		public override string Description =>
			"The AmmoPool, Cargo, Harvester, and StoresResources traits no longer\n" +
			"automatically add pips to the selection box. New traits WithAmmoPipsDecoration,\n" +
			"WithCargoPipsDecoration, WithHarvesterPipsDecoration,\n" +
			"WithResourceStoragePipsDecoration are added to provide the same functionality.\n\n" +
			"Passenger.PipType has been replaced with CustomPipType, which now references a\n" +
			"sequence defined in WithCargoDecoration.CustomPipTypeSequences.\n\n" +
			"ResourceType.PipColor has been removed and resource pip colours are now defined\n" +
			"in WithHarvesterPipsDecoration.ResourceSequences.";

		static readonly Dictionary<string, string> PipReplacements = new Dictionary<string, string>
		{
			{ "transparent", "pip-empty" },
			{ "green", "pip-green" },
			{ "yellow", "pip-yellow" },
			{ "red", "pip-red" },
			{ "gray", "pip-gray" },
			{ "blue", "pip-blue" },
			{ "ammo", "pip-ammo" },
			{ "ammoempty", "pip-ammoempty" },
		};

		bool customPips;
		readonly List<string> locations = new List<string>();
		readonly List<string> cargoPipLocations = new List<string>();
		readonly HashSet<string> cargoCustomPips = new HashSet<string>();
		readonly List<string> harvesterPipLocations = new List<string>();
		readonly Dictionary<string, string> harvesterCustomPips = new Dictionary<string, string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (customPips && locations.Count > 0)
				yield return "Custom pip Images and Palettes are now defined on the individual With*PipsDecoration traits.\n" +
					"You should review the following definitions and manually define the Image and Palette properties as required:\n" +
					UpdateUtils.FormatMessageList(locations);

			if (cargoCustomPips.Count > 0 && cargoPipLocations.Count > 0)
				yield return "Some passenger types define custom cargo pips. Review the following definitions:\n" +
					UpdateUtils.FormatMessageList(cargoPipLocations) +
					"\nand, if required, add the following to the WithCargoPipsDecoration traits:\n" +
					"CustomPipSequences:\n" + cargoCustomPips.Select(p => $"\t{p}: {PipReplacements[p]}").JoinWith("\n");

			if (harvesterCustomPips.Count > 0 && harvesterPipLocations.Count > 0)
				yield return "Review the following definitions:\n" +
				             UpdateUtils.FormatMessageList(harvesterPipLocations) +
				             "\nand, if required, add the following to the WithHarvesterPipsDecoration traits:\n" +
				             "ResourceSequences:\n" + harvesterCustomPips.Select(kv => $"\t{kv.Key}: {PipReplacements[kv.Value]}").JoinWith("\n");

			customPips = false;
			locations.Clear();
			cargoPipLocations.Clear();
			harvesterPipLocations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var addNodes = new List<MiniYamlNode>();

			foreach (var selectionDecorations in actorNode.ChildrenMatching("SelectionDecorations"))
			{
				customPips |= selectionDecorations.RemoveNodes("Palette") > 0;
				customPips |= selectionDecorations.RemoveNodes("Image") > 0;
			}

			foreach (var ammoPool in actorNode.ChildrenMatching("AmmoPool"))
			{
				var ammoPips = new MiniYamlNode("WithAmmoPipsDecoration", "");
				ammoPips.AddNode("Position", "BottomLeft");
				ammoPips.AddNode("RequiresSelection", "true");

				var pipCountNode = ammoPool.LastChildMatching("PipCount");
				if (pipCountNode != null)
				{
					ammoPool.RemoveNode(pipCountNode);
					var pipCount = pipCountNode.NodeValue<int>();
					if (pipCount == 0)
					{
						addNodes.Add(new MiniYamlNode("-" + ammoPips.Key, ""));
						continue;
					}

					var ammoNode = ammoPool.LastChildMatching("Ammo");
					var maxAmmo = ammoNode != null ? ammoNode.NodeValue<int>() : 0;
					if (pipCount != maxAmmo)
						ammoPips.AddNode("PipCount", pipCount);
				}

				var pipTypeNode = ammoPool.LastChildMatching("PipType");
				if (pipTypeNode != null)
				{
					ammoPool.RemoveNode(pipTypeNode);

					if (PipReplacements.TryGetValue(pipTypeNode.Value.Value.ToLowerInvariant(), out var sequence))
						ammoPips.AddNode("FullSequence", sequence);
				}

				var pipTypeEmptyNode = ammoPool.LastChildMatching("PipTypeEmpty");
				if (pipTypeEmptyNode != null)
				{
					ammoPool.RemoveNode(pipTypeEmptyNode);

					if (PipReplacements.TryGetValue(pipTypeEmptyNode.Value.Value.ToLowerInvariant(), out var sequence))
						ammoPips.AddNode("EmptySequence", sequence);
				}

				addNodes.Add(ammoPips);
				locations.Add($"{actorNode.Key}: {ammoPips.Key} ({actorNode.Location.Filename})");
			}

			foreach (var cargo in actorNode.ChildrenMatching("Cargo"))
			{
				var cargoPips = new MiniYamlNode("WithCargoPipsDecoration", "");
				cargoPips.AddNode("Position", "BottomLeft");
				cargoPips.AddNode("RequiresSelection", "true");

				var pipCountNode = cargo.LastChildMatching("PipCount");
				if (pipCountNode != null)
				{
					cargo.RemoveNode(pipCountNode);

					var pipCount = pipCountNode.NodeValue<int>();
					if (pipCount == 0)
					{
						addNodes.Add(new MiniYamlNode("-" + cargoPips.Key, ""));
						continue;
					}

					var maxWeightNode = cargo.LastChildMatching("MaxWeight");
					var maxWeight = maxWeightNode != null ? maxWeightNode.NodeValue<int>() : 0;
					if (pipCount != maxWeight)
						cargoPips.AddNode("PipCount", pipCount);
				}
				else
					continue;

				addNodes.Add(cargoPips);
				locations.Add($"{actorNode.Key}: {cargoPips.Key} ({actorNode.Location.Filename})");
				cargoPipLocations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
			}

			foreach (var passenger in actorNode.ChildrenMatching("Passenger"))
			{
				var pipTypeNode = passenger.LastChildMatching("PipType");
				if (pipTypeNode != null)
				{
					pipTypeNode.RenameKey("CustomPipType");
					pipTypeNode.Value.Value = pipTypeNode.Value.Value.ToLowerInvariant();
					cargoCustomPips.Add(pipTypeNode.Value.Value);
				}
			}

			foreach (var harvester in actorNode.ChildrenMatching("Harvester"))
			{
				var harvesterPips = new MiniYamlNode("WithHarvesterPipsDecoration", "");
				harvesterPips.AddNode("Position", "BottomLeft");
				harvesterPips.AddNode("RequiresSelection", "true");

				// Harvester hardcoded a default PipCount > 0 so we can't use that to determine whether
				// this is a definition or an override. Resources isn't ideal either, but is better than nothing
				var resourcesNode = harvester.LastChildMatching("Resources");
				if (resourcesNode == null)
					continue;

				var pipCountNode = harvester.LastChildMatching("PipCount");
				if (pipCountNode != null)
				{
					harvester.RemoveNode(pipCountNode);

					var pipCount = pipCountNode.NodeValue<int>();
					if (pipCount == 0)
					{
						addNodes.Add(new MiniYamlNode("-" + harvesterPips.Key, ""));
						continue;
					}

					harvesterPips.AddNode("PipCount", pipCount);
				}
				else
					harvesterPips.AddNode("PipCount", 7);

				addNodes.Add(harvesterPips);
				locations.Add($"{actorNode.Key}: {harvesterPips.Key} ({actorNode.Location.Filename})");
				harvesterPipLocations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
			}

			foreach (var resourceType in actorNode.ChildrenMatching("ResourceType"))
			{
				var pipColor = "yellow";
				var pipCountNode = resourceType.LastChildMatching("PipColor");
				if (pipCountNode != null)
				{
					pipColor = pipCountNode.Value.Value.ToLowerInvariant();
					resourceType.RemoveNode(pipCountNode);
				}

				var typeNode = resourceType.LastChildMatching("Type");
				if (typeNode != null)
					harvesterCustomPips.Add(typeNode.Value.Value, pipColor);
			}

			foreach (var storesResources in actorNode.ChildrenMatching("StoresResources"))
			{
				var storagePips = new MiniYamlNode("WithResourceStoragePipsDecoration", "");
				storagePips.AddNode("Position", "BottomLeft");
				storagePips.AddNode("RequiresSelection", "true");

				var pipCountNode = storesResources.LastChildMatching("PipCount");
				if (pipCountNode != null)
				{
					storesResources.RemoveNode(pipCountNode);
					var pipCount = pipCountNode.NodeValue<int>();
					if (pipCount == 0)
					{
						addNodes.Add(new MiniYamlNode("-" + storagePips.Key, ""));
						continue;
					}

					storagePips.AddNode("PipCount", pipCount);
				}
				else
					continue;

				// Default pip color changed from yellow to green for consistency with other pip traits
				var pipColorNode = storesResources.LastChildMatching("PipColor");
				if (pipColorNode != null)
				{
					storesResources.RemoveNode(pipColorNode);

					var type = pipColorNode.Value.Value.ToLowerInvariant();
					if (type != "green" && PipReplacements.TryGetValue(type, out var sequence))
						storagePips.AddNode("FullSequence", sequence);
				}
				else
					storagePips.AddNode("FullSequence", PipReplacements["yellow"]);

				addNodes.Add(storagePips);
				locations.Add($"{actorNode.Key}: {storagePips.Key} ({actorNode.Location.Filename})");
			}

			foreach (var addNode in addNodes)
				actorNode.AddNode(addNode);

			yield break;
		}
	}
}
