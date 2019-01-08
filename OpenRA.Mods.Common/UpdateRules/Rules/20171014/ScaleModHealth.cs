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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ScaleModHealth : UpdateRule
	{
		public override string Name { get { return "Scale health and damage in the default OpenRA mods"; } }
		public override string Description
		{
			get
			{
				return "All health and damage values are increased by a factor of 100 (ra, cnc, ts) or 10 (d2k)\n"
					+ "in order to reduce numerical inaccuracies in damage calculations.";
			}
		}

		static readonly Dictionary<string, Pair<string, string>[]> TraitMapping = new Dictionary<string, Pair<string, string>[]>()
		{
			{ "Health", new[] { Pair.New("HP", string.Empty) } },
			{ "SelfHealing", new[] { Pair.New("Step", "500") } },
			{ "RepairsUnits", new[] { Pair.New("HpPerStep", "1000") } },
			{ "RepairableBuilding", new[] { Pair.New("RepairStep", "700") } },
			{ "Burns", new[] { Pair.New("Damage", "100") } },
			{ "DamagedByTerrain", new[] { Pair.New("Damage", string.Empty) } },
		};

		static readonly Dictionary<string, string> WarheadMapping = new Dictionary<string, string>()
		{
			{ "SpreadDamage", "Damage" },
			{ "TargetDamage", "Damage" },
		};

		public ScaleModHealth(int scale = 100)
		{
			this.scale = scale;
		}

		protected int scale;

		bool updated;

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			updated = false;
			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (updated)
				yield return "Health and damage values have been muliplied by a factor of {0}.\n".F(scale)
					+ "The increased calculation precision will affect game balance and may need to be manually adjusted.\n";
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var kv in TraitMapping)
			{
				foreach (var trait in actorNode.ChildrenMatching(kv.Key))
				{
					foreach (var parameter in kv.Value)
					{
						var node = trait.LastChildMatching(parameter.First);
						if (node != null)
							node.ReplaceValue((scale * node.NodeValue<int>()).ToString());
						else if (!string.IsNullOrEmpty(parameter.Second))
							trait.AddNode(parameter.First, parameter.Second);

						updated = true;
					}
				}
			}

			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var warheadNode in weaponNode.ChildrenMatching("Warhead"))
			{
				var name = warheadNode.NodeValue<string>();
				if (name == null)
					continue;

				string parameterName;
				if (!WarheadMapping.TryGetValue(name, out parameterName))
					continue;

				foreach (var node in warheadNode.ChildrenMatching(parameterName))
				{
					node.ReplaceValue((scale * node.NodeValue<int>()).ToString());
					updated = true;
				}
			}

			yield break;
		}
	}
}
