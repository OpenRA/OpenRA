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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemovedNotifyBuildComplete : UpdateRule
	{
		public override string Name { get { return "Traits are no longer automatically disabled during Building make-animations"; } }
		public override string Description
		{
			get
			{
				return "Traits are no longer force-disabled while the WithMakeAnimation trait is active.\n" +
					"This affects the With*Animation, With*Overlay, *Production, Attack*,\n" +
					"Transforms, Sellable, Gate, ToggleConditionOnOrder, and ConyardChronoReturn traits.\n" +
					"The AnnounceOnBuild trait has been replaced with a new VoiceAnnouncement trait.\n" +
					"Affected actors are listed so that conditions may be manually defined.";
			}
		}

		static readonly string[] Traits =
		{
			"WithAcceptDeliveredCashAnimation",
			"WithBuildingPlacedAnimation",
			"WithBuildingPlacedOverlay",
			"WithChargeAnimation",
			"WithChargeOverlay",
			"WithDockedOverlay",
			"WithIdleAnimation",
			"WithIdleOverlay",
			"WithNukeLaunchAnimation",
			"WithNukeLaunchOverlay",
			"WithProductionDoorOverlay",
			"WithProductionOverlay",
			"WithRepairOverlay",
			"WithResources",
			"WithResupplyAnimation",
			"WithSiloAnimation",
			"WithSpriteTurret",
			"WithVoxelBarrel",
			"WithVoxelTurret",
			"WithDeliveryAnimation",
			"WithCrumbleOverlay",
			"WithDeliveryOverlay",
			"Production",
			"ProductionAirdrop",
			"ProductionFromMapEdge",
			"ProductionParadrop",
			"AttackFrontal",
			"AttackFollow",
			"AttackTurreted",
			"AttackOmni",
			"AttackBomber",
			"AttackPopupTurreted",
			"AttackTesla",
			"Transforms",
			"Sellable",
			"Gate",
			"ConyardChronoReturn",
			"ToggleConditionOnOrder",
			"VoiceAnnouncement"
		};

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "Review the following definitions and, for those that are buildings,\n" +
					"define conditions to disable them while WithMakeAnimation is active:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var announce in actorNode.ChildrenMatching("AnnounceOnBuild"))
			{
				announce.RenameKey("VoiceAnnouncement");
				if (announce.LastChildMatching("Voice") == null)
					announce.AddNode("Voice", "Build");
			}

			var used = new List<string>();
			foreach (var t in Traits)
				if (actorNode.LastChildMatching(t) != null)
					used.Add(t);

			if (used.Any())
			{
				var location = "{0} ({1})".F(actorNode.Key, actorNode.Location.Filename);
				locations[location] = used;
			}

			yield break;
		}
	}
}
