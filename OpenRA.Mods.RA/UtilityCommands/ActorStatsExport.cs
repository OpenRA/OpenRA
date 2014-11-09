#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.UtilityCommands
{
	public static class ActorStatsExport
	{
		public static DataTable GenerateTable()
		{
			var rules = Game.modData.RulesetCache.LoadDefaultRules();

			var table = new DataTable();
			table.Columns.Add("Name", typeof(string));
			table.Columns.Add("Cost", typeof(int));
			table.Columns.Add("HitPoints", typeof(int));
			table.Columns.Add("Armor", typeof(string));
			table.Columns.Add("Damage /s", typeof(float));

			var armorList = new List<string>();
			foreach (var actorInfo in rules.Actors.Values)
			{
				var armor = actorInfo.Traits.GetOrDefault<ArmorInfo>();
				if (armor != null)
					if (!armorList.Contains(armor.Type))
						armorList.Add(armor.Type);
			}

			armorList.Sort();
			foreach (var armorType in armorList)
				table.Columns.Add("vs. " + armorType, typeof(float));

			foreach (var actorInfo in rules.Actors.Values)
			{
				if (actorInfo.Name.StartsWith("^"))
					continue;

				var buildable = actorInfo.Traits.GetOrDefault<BuildableInfo>();
				if (buildable == null)
					continue;

				var row = table.NewRow();
				var tooltip = actorInfo.Traits.GetOrDefault<TooltipInfo>();
				row["Name"] = tooltip != null ? tooltip.Name : actorInfo.Name;

				var value = actorInfo.Traits.GetOrDefault<ValuedInfo>();
				row["Cost"] = value != null ? value.Cost : 0;

				var health = actorInfo.Traits.GetOrDefault<HealthInfo>();
				row["HitPoints"] = health != null ? health.HP : 0;

				var armor = actorInfo.Traits.GetOrDefault<ArmorInfo>();
				row["Armor"] = armor != null ? armor.Type : "";

				var armaments = actorInfo.Traits.WithInterface<ArmamentInfo>();
				if (armaments.Any())
				{
					var weapons = armaments.Select(a => rules.Weapons[a.Weapon.ToLowerInvariant()]);
					foreach (var weapon in weapons)
					{
						var warhead = weapon.Warheads.FirstOrDefault(w => (w is DamageWarhead)) as DamageWarhead;
						if (warhead != null)
						{
							var rateOfFire = weapon.ReloadDelay > 1 ? weapon.ReloadDelay : 1;
							var burst = weapon.Burst;
							var delay = weapon.BurstDelay;
							var damage = warhead.Damage;
							var damagePerSecond = (1000f / Game.Timestep) * (damage * burst) / (delay + burst * rateOfFire);
							row["Damage /s"] = Math.Round(damagePerSecond, 1, MidpointRounding.AwayFromZero);

							foreach (var armorType in armorList)
							{
								var vs = warhead.Versus.ContainsKey(armorType) ? warhead.Versus[armorType] : 100;
								row["vs. " + armorType] = Math.Round(damagePerSecond * vs / 100, 1, MidpointRounding.AwayFromZero);
							}
						}
					}
				}

				table.Rows.Add(row);
			}

			return table;
		}
	}
}