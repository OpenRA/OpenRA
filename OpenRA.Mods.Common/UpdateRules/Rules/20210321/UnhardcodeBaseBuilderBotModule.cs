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
	public class UnhardcodeBaseBuilderBotModule : UpdateRule
	{
		MiniYamlNode defences;

		// Excludes AttackBomber and AttackTDGunboatTurreted as actors with these AttackBase traits aren't supposed to be controlled.
		readonly string[] attackBase = { "AttackLeap", "AttackPopupTurreted", "AttackAircraft", "AttackTesla", "AttackCharges", "AttackFollow", "AttackTurreted", "AttackFrontal", "AttackGarrisoned", "AttackOmni", "AttackSwallow" };
		readonly string[] buildings = { "Building", "EnergyWall", "D2kBuilding" };

		bool anyAdded;

		public override string Name => "BaseBuilderBotModule got new fields to configure buildings that are defenses.";

		public override string Description => "DefenseTypes were added.";

		public override IEnumerable<string> BeforeUpdateActors(ModData modData, List<MiniYamlNode> resolvedActors)
		{
			var defences = new List<string>();

			foreach (var actor in resolvedActors)
			{
				if (actor.Key.StartsWith('^'))
					continue;

				var isBuildable = false;
				var isBuilding = false;
				var canAttack = false;

				foreach (var trait in actor.Value.Nodes)
				{
					if (trait.IsRemoval())
						continue;

					if (trait.KeyMatches("Buildable", includeRemovals: false))
					{
						isBuildable = true;
						continue;
					}

					if (buildings.Any(v => trait.KeyMatches(v, includeRemovals: false)))
					{
						isBuilding = true;
						continue;
					}

					if (attackBase.Any(ab => trait.KeyMatches(ab, includeRemovals: false)))
						canAttack = true;
				}

				if (isBuildable && isBuilding && canAttack)
				{
					var name = actor.Key.ToLower();
					if (!defences.Contains(name))
						defences.Add(name);
				}
			}

			this.defences = new MiniYamlNode("DefenseTypes", FieldSaver.FormatValue(defences));

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (anyAdded)
				yield return "`BaseBuilderBotModule` was unhardcoded and a new field added: `DefenseTypes`. Please verify the automated changes.";

			anyAdded = false;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var squadManager in actorNode.ChildrenMatching("BaseBuilderBotModule", includeRemovals: false))
			{
				if (!squadManager.ChildrenMatching(defences.Key, includeRemovals: false).Any())
				{
					squadManager.AddNode(defences);
					anyAdded = true;
				}
			}

			yield break;
		}
	}
}
