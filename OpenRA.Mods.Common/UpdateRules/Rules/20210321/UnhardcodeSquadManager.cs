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
	public class UnhardcodeSquadManager : UpdateRule
	{
		readonly List<MiniYamlNode> addNodes = new List<MiniYamlNode>();

		// Excludes AttackBomber and AttackTDGunboatTurreted as actors with these AttackBase traits aren't supposed to be controlled.
		readonly string[] attackBase = { "AttackLeap", "AttackPopupTurreted", "AttackAircraft", "AttackTesla", "AttackCharges", "AttackFollow", "AttackTurreted", "AttackFrontal", "AttackGarrisoned", "AttackOmni", "AttackSwallow" };
		readonly string[] vipsNames = { "Harvester", "BaseBuilding" };
		readonly string[] buildings = { "Building", "EnergyWall", "D2kBuilding" };
		readonly string[] excludedBuildings = { "LineBuild", "Plug" };

		public override string Name => "SquadManagerBotModule got new fields to configure ground attacks and defensive actions.";

		public override string Description => "AirUnitsTypes and ProtectionTypes were added.";

		public override IEnumerable<string> BeforeUpdateActors(ModData modData, List<MiniYamlNode> resolvedActors)
		{
			var aircraft = new List<string>();
			var vips = new List<string>();

			foreach (var actor in resolvedActors)
			{
				if (actor.Key.StartsWith('^'))
					continue;

				var isVip = false;
				var isBuildable = false;
				var isBuilding = false;
				var isAircraft = false;
				var isExcluded = false;
				var canAttack = false;
				var isKillable = false;

				foreach (var trait in actor.Value.Nodes)
				{
					if (trait.IsRemoval())
						continue;

					if (trait.KeyMatches("Buildable", includeRemovals: false))
					{
						isBuildable = true;
						continue;
					}

					if (trait.KeyMatches("Aircraft", includeRemovals: false))
					{
						isAircraft = true;
						continue;
					}

					if (trait.KeyMatches("Health", includeRemovals: false))
					{
						isKillable = true;
						continue;
					}

					if (vipsNames.Any(v => trait.KeyMatches(v, includeRemovals: false)))
					{
						isVip = true;
						continue;
					}

					if (buildings.Any(b => trait.KeyMatches(b, includeRemovals: false)))
					{
						isBuilding = true;
						continue;
					}

					if (excludedBuildings.Any(eb => trait.KeyMatches(eb, includeRemovals: false)))
					{
						isExcluded = true;
						continue;
					}

					if (attackBase.Any(ab => trait.KeyMatches(ab, includeRemovals: false)))
						canAttack = true;
				}

				if (isAircraft && isBuildable && canAttack && isKillable)
				{
					var name = actor.Key.ToLower();
					if (!aircraft.Contains(name))
						aircraft.Add(name);
				}

				if (isBuildable && isKillable && (isVip || (isBuilding && !isExcluded)))
				{
					var name = actor.Key.ToLower();
					if (!vips.Contains(name))
						vips.Add(name);
				}
			}

			addNodes.Add(new MiniYamlNode("AirUnitsTypes", FieldSaver.FormatValue(aircraft)));
			addNodes.Add(new MiniYamlNode("ProtectionTypes", FieldSaver.FormatValue(vips)));

			yield break;
		}

		bool anyAdded = false;

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var squadManager in actorNode.ChildrenMatching("SquadManagerBotModule", includeRemovals: false))
			{
				foreach (var addNode in addNodes)
				{
					if (!squadManager.ChildrenMatching(addNode.Key, includeRemovals: false).Any())
					{
						squadManager.AddNode(addNode);
						anyAdded = true;
					}
				}
			}

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (anyAdded)
				yield return "`SquadManagerBotModule` was unhardcoded and new fields added: `AirUnitsTypes` and `ProtectionTypes`. Please verify the automated changes.";

			anyAdded = false;
		}
	}
}
