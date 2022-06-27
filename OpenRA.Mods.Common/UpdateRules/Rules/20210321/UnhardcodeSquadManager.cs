#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class UnhardcodeSquadManager : UpdateRule
	{
		public override string Name => "SquadManagerBotModule got new fields to configure ground attacks and defensive actions.";

		public override string Description => "AirUnitsTypes and ProtectionTypes were added.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var addNodes = new List<MiniYamlNode>();

			var aircraft = modData.DefaultRules.Actors.Values.Where(a => a.HasTraitInfo<AircraftInfo>() && a.HasTraitInfo<AttackBaseInfo>()).Select(a => a.Name);
			var airUnits = new MiniYamlNode("AirUnitsTypes", FieldSaver.FormatValue(aircraft.ToList()));
			addNodes.Add(airUnits);

			var vips = modData.DefaultRules.Actors.Values.Where(a => a.HasTraitInfo<HarvesterInfo>() || a.HasTraitInfo<BaseBuildingInfo>() || (a.HasTraitInfo<BuildingInfo>() && a.HasTraitInfo<BuildableInfo>() && !a.HasTraitInfo<LineBuildInfo>() && !a.HasTraitInfo<PlugInfo>())).Select(a => a.Name);
			var protection = new MiniYamlNode("ProtectionTypes", FieldSaver.FormatValue(vips.ToList()));
			addNodes.Add(protection);

			foreach (var squadManager in actorNode.ChildrenMatching("SquadManagerBotModule"))
				foreach (var addNode in addNodes)
					squadManager.AddNode(addNode);

			yield break;
		}
	}
}
