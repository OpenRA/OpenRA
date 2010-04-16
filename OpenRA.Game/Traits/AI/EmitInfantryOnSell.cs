using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.GameRules;

namespace OpenRA.Traits.AI
{
	class EmitInfantryOnSellInfo : TraitInfo<EmitInfantryOnSell>
	{
		public readonly float ValueFraction = .4f;
		public readonly float MinHpFraction = .3f;
		public readonly string[] ActorTypes = { "e1" };		// todo: cN as well
	}

	class EmitInfantryOnSell : INotifySold, INotifyDamage
	{
		public void Selling(Actor self) { }

		void Emit(Actor self)
		{
			var info = self.Info.Traits.Get<EmitInfantryOnSellInfo>();
			var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
			var cost = csv != null ? csv.Value : self.Info.Traits.Get<ValuedInfo>().Cost;
			var hp = self.Info.Traits.Get<OwnedActorInfo>().HP;
			var hpFraction = Math.Max(info.MinHpFraction, hp / self.GetMaxHP());
			var dudesValue = (int)(hpFraction * info.ValueFraction * cost);
			var eligibleLocations = Footprint.Tiles(self).ToList();
			// todo: fix this for unbuildables in ActorTypes, like civilians.
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
