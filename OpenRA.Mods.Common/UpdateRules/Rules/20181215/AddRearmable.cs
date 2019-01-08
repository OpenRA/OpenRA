#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddRearmable : UpdateRule
	{
		public override string Name { get { return "Added Rearmable trait and move RearmBuildings properties there"; } }
		public override string Description
		{
			get
			{
				return "Added Rearmable trait and replaced Aircraft.RearmBuildings and\n" +
					"Minelayer.RearmBuildings with Rearmable.RearmActors.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraftNodes = actorNode.ChildrenMatching("Aircraft");
			var minelayerNodes = actorNode.ChildrenMatching("Minelayer");
			var ammoPoolNodes = actorNode.ChildrenMatching("AmmoPool");
			var addNodes = new List<MiniYamlNode>();

			var ammoPoolNames = new List<string>() { "primary" };
			foreach (var ap in ammoPoolNodes)
			{
				var poolName = ap.LastChildMatching("Name");
				if (poolName != null && poolName.NodeValue<string>() != "primary")
					ammoPoolNames.Add(poolName.NodeValue<string>());
			}

			var rearmableAdded = false;
			foreach (var aircraftNode in aircraftNodes)
			{
				var rearmBuildings = aircraftNode.LastChildMatching("RearmBuildings");
				if (rearmBuildings != null)
				{
					if (!rearmableAdded)
					{
						var rearmableNode = new MiniYamlNode("Rearmable", "");
						rearmBuildings.MoveAndRenameNode(aircraftNode, rearmableNode, "RearmActors");

						// If the list has more than one entry, at least one of them won't be "primary"
						if (ammoPoolNames.Count > 1)
						{
							var ammoPools = new MiniYamlNode("AmmoPools", string.Join(", ", ammoPoolNames));
							rearmableNode.AddNode(ammoPools);
						}

						addNodes.Add(rearmableNode);
						rearmableAdded = true;
					}
					else
						aircraftNode.RemoveNodes("RearmBuildings");
				}
			}

			// If it's a minelayer, it won't be an aircraft and rearmableAdded should still be false, so we can use it here
			foreach (var minelayerNode in minelayerNodes)
			{
				var rearmableNode = new MiniYamlNode("Rearmable", "");

				var rearmBuildings = minelayerNode.LastChildMatching("RearmBuildings");
				if (!rearmableAdded)
				{
					if (rearmBuildings != null)
						rearmBuildings.MoveAndRenameNode(minelayerNode, rearmableNode, "RearmActors");
					else
						rearmableNode.AddNode(new MiniYamlNode("RearmActors", "fix"));

					// If the list has more than one entry, at least one of them won't be "primary"
					if (ammoPoolNames.Count > 1)
					{
						var ammoPools = new MiniYamlNode("AmmoPools", string.Join(", ", ammoPoolNames));
						rearmableNode.AddNode(ammoPools);
					}

					addNodes.Add(rearmableNode);
					rearmableAdded = true;
				}
				else if (rearmableAdded && rearmBuildings != null)
					minelayerNode.RemoveNodes("RearmBuildings");
			}

			foreach (var node in addNodes)
				actorNode.AddNode(node);

			yield break;
		}
	}
}
