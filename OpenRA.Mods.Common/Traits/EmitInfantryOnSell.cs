#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawn new actors when sold.")]
	public class EmitInfantryOnSellInfo : ITraitInfo
	{
		public readonly int ValuePercent = 40;
		public readonly int MinHpPercent = 30;

		[ActorReference]
		[Desc("Be sure to use lowercase. Default value is \"e1\".")]
		public readonly string[] ActorTypes = { "e1" };

		[Desc("Spawns actors only if the selling player's faction is in this list. " +
			"Leave empty to allow all factions by default.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		public object Create(ActorInitializer init) { return new EmitInfantryOnSell(init.Self, this); }
	}

	public class EmitInfantryOnSell : INotifySold
	{
		readonly EmitInfantryOnSellInfo info;
		readonly bool correctFaction;

		public EmitInfantryOnSell(Actor self, EmitInfantryOnSellInfo info)
		{
			this.info = info;
			var factionsList = info.Factions;
			correctFaction = factionsList.Count == 0 || factionsList.Contains(self.Owner.Faction.InternalName);
		}

		public void Selling(Actor self) { }

		void Emit(Actor self)
		{
			if (!correctFaction)
				return;

			var csv = self.Info.TraitInfoOrDefault<CustomSellValueInfo>();
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			var cost = csv != null ? csv.Value : (valued != null ? valued.Cost : 0);

			var health = self.TraitOrDefault<Health>();
			var dudesValue = info.ValuePercent * cost / 100;
			if (health != null)
			{
				if (100 * health.HP >= info.MinHpPercent * health.MaxHP)
					dudesValue = health.HP * dudesValue / health.MaxHP;
				else
					dudesValue = 0;
			}

			var eligibleLocations = FootprintUtils.Tiles(self).ToList();
			var actorTypes = info.ActorTypes.Select(a => new { Name = a, Cost = self.World.Map.Rules.Actors[a].TraitInfo<ValuedInfo>().Cost }).ToList();

			while (eligibleLocations.Count > 0 && actorTypes.Any(a => a.Cost <= dudesValue))
			{
				var at = actorTypes.Where(a => a.Cost <= dudesValue).Random(self.World.SharedRandom);
				var loc = eligibleLocations.Random(self.World.SharedRandom);

				eligibleLocations.Remove(loc);
				dudesValue -= at.Cost;

				self.World.AddFrameEndTask(w => w.CreateActor(at.Name, new TypeDictionary
				{
					new LocationInit(loc),
					new OwnerInit(self.Owner),
				}));
			}
		}

		public void Sold(Actor self) { Emit(self); }
	}
}
