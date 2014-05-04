#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class EmitInfantryOnSellInfo : TraitInfo<EmitInfantryOnSell>
	{
		public readonly float ValuePercent = 40;
		public readonly float MinHpPercent = 30;

		[ActorReference]
		public readonly string[] ActorTypes = { "e1" };
	}

	class EmitInfantryOnSell : INotifySold, INotifyKilled
	{
		public void Selling(Actor self) { }

		void Emit(Actor self)
		{
			var info = self.Info.Traits.Get<EmitInfantryOnSellInfo>();
			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			var valued = self.Info.Traits.GetOrDefault<ValuedInfo>();
			var cost = csv != null ? csv.Value : (valued != null ? valued.Cost : 0);

			var health = self.TraitOrDefault<Health>();
			var dudesValue = info.ValuePercent * cost;
			if (health != null)
				dudesValue = dudesValue*health.HP / health.MaxHP;
			dudesValue /= 100;

			var eligibleLocations = FootprintUtils.Tiles(self).ToList();
			var actorTypes = info.ActorTypes.Select(a => new { Name = a, Cost = self.World.Map.Rules.Actors[a].Traits.Get<ValuedInfo>().Cost }).ToArray();

			while (eligibleLocations.Count > 0 && actorTypes.Any(a => a.Cost <= dudesValue))
			{
				var at = actorTypes.Where(a => a.Cost <= dudesValue).Random(self.World.SharedRandom);
				var loc = eligibleLocations.Random(self.World.SharedRandom);

				eligibleLocations.Remove(loc);
				dudesValue -= at.Cost;

				self.World.AddFrameEndTask(w => w.CreateActor(at.Name, new TypeDictionary
				{
					new LocationInit( loc ),
					new OwnerInit( self.Owner ),
				}));
			}
		}

		public void Sold(Actor self) { Emit(self); }

		public void Killed(Actor self, AttackInfo e)
		{
			Emit(self);
		}
	}
}
