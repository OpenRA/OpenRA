#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class ImproveProductionAirdrop : UpdateRule
	{
		public override string Name { get { return "Add more options to ProductionAirdrop."; } }

		public override string Description
		{
			get
			{
				return "The ProductionAirdrop trait received some new properties to allow for better control over the delivery aircraft's behaviour.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var productionAirdropTraits = actorNode.ChildrenMatching("ProductionAirdrop");

			foreach (var productionAirdropTrait in productionAirdropTraits)
			{
				var facingNode = productionAirdropTrait.ChildrenMatching("Facing").FirstOrDefault();
				if (facingNode != null)
					facingNode.Key = "LandingFacing";
				else
					productionAirdropTrait.AddNode("LandingFacing", 64);    // The previously hardcoded value was 64.

				var baselineSpawnNode = productionAirdropTrait.ChildrenMatching("BaselineSpawn").FirstOrDefault();
				if (baselineSpawnNode != null)
				{
					if (baselineSpawnNode.Value.Value == "true")
					{
						// Values here are designed to keep the same behaviour.
						productionAirdropTrait.AddNode("EntryType", "PlayerSpawnClosestEdge");
						productionAirdropTrait.AddNode("ExitType", "SameAsEntry");
					}

					productionAirdropTrait.RemoveNode(baselineSpawnNode);
				}
			}

			yield break;
		}
	}
}
