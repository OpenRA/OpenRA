#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawn new actors when sold.")]
	public class EmitInfantryOnSellInfo : ITraitInfo
	{
		public readonly float ValuePercent = 40;
		public readonly float MinHpPercent = 30;

		[ActorReference]
		[Desc("Be sure to use lowercase. Default value is \"e1\".")]
		public readonly string[] ActorTypes = { "e1" };

		[Desc("Spawns actors only if the selling player's race is in this list." +
			"Leave empty to allow all races by default.")]
		public readonly string[] Races = { };

		public object Create(ActorInitializer init) { return new EmitInfantryOnSell(init.Self, this); }
	}

	public class EmitInfantryOnSell : INotifySold
	{
		readonly EmitInfantryOnSellInfo info;
		readonly bool correctRace = false;

		public EmitInfantryOnSell(Actor self, EmitInfantryOnSellInfo info)
		{
			this.info = info;
			var raceList = info.Races;
			correctRace = raceList.Length == 0 || raceList.Contains(self.Owner.Country.Race);
		}

		public void Selling(Actor self) { }

		void Emit(Actor self)
		{
			if (!correctRace)
				return;

			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			var valued = self.Info.Traits.GetOrDefault<ValuedInfo>();
			var cost = csv != null ? csv.Value : (valued != null ? valued.Cost : 0);

			var health = self.TraitOrDefault<Health>();
			var dudesValue = info.ValuePercent * cost;
			if (health != null)
				dudesValue = dudesValue * health.HP / health.MaxHP;
			dudesValue /= 100;

			var eligibleLocations = FootprintUtils.Tiles(self).ToList();
			var actorTypes = info.ActorTypes.Select(a => new { Name = a, Cost = self.World.Map.Rules.Actors[a].Traits.Get<ValuedInfo>().Cost }).ToList();

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
