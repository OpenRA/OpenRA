#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class EmitInfantryOnSellInfo : TraitInfo<EmitInfantryOnSell>
	{
		public readonly float ValueFraction = .4f;
		public readonly float MinHpFraction = .3f;

		[ActorReference]
		public readonly string[] ActorTypes = { "e1" };
	}

	class EmitInfantryOnSell : INotifySold, INotifyDamage
	{
		public void Selling(Actor self) { }

		void Emit(Actor self)
		{
			var info = self.Info.Traits.Get<EmitInfantryOnSellInfo>();
			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			var valued = self.Info.Traits.GetOrDefault<ValuedInfo>();
			var cost = csv != null ? csv.Value : (valued != null ? valued.Cost : 0);
			var hp = self.Info.Traits.Get<OwnedActorInfo>().HP;
			var hpFraction = Math.Max(info.MinHpFraction, hp / self.GetMaxHP());
			var dudesValue = (int)(hpFraction * info.ValueFraction * cost);
			var eligibleLocations = Footprint.Tiles(self).ToList();
			var actorTypes = info.ActorTypes.Select(a => new { Name = a, Cost = Rules.Info[a].Traits.Get<ValuedInfo>().Cost }).ToArray();

			while (eligibleLocations.Count > 0 && actorTypes.Any(a => a.Cost <= dudesValue))
			{
				var at = actorTypes.Where(a => a.Cost <= dudesValue).Random(self.World.SharedRandom);
				var loc = eligibleLocations.Random(self.World.SharedRandom);

				eligibleLocations.Remove(loc);
				dudesValue -= at.Cost;

				self.World.AddFrameEndTask(w => w.CreateActor(at.Name, loc, self.Owner));
			}
		}

		public void Sold(Actor self) { Emit(self); }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageStateChanged && e.DamageState == DamageState.Dead)
				Emit(self);
		}
	}
}
