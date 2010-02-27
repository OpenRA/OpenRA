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
using System.Collections.Generic;
using System.Linq;
using OpenRa.Effects;
using OpenRa.GameRules;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	public class OwnedActorInfo
	{
		public readonly int HP = 0;
		public readonly ArmorType Armor = ArmorType.none;
		public readonly bool Crewed = false;		// replace with trait?
		public readonly int Sight = 0;
		public readonly bool WaterBound = false;
	}

	public class BuildingInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int Power = 0;
		public readonly bool BaseNormal = true;
		public readonly int Adjacent = 2;
		public readonly bool Bib = false;
		public readonly bool Capturable = false;
		public readonly bool Repairable = true;
		public readonly string Footprint = "x";
		public readonly string[] Produces = { };		// does this go somewhere else?
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly bool Unsellable = false;

		public readonly string[] BuildSounds = {"placbldg.aud", "build5.aud"};
		public readonly string[] SellSounds = {"cashturn.aud"};
		
		public object Create(Actor self) { return new Building(self); }
	}

	public class Building : INotifyDamage, IResolveOrder, ITick, IRenderModifier
	{
		readonly Actor self;
		public readonly BuildingInfo Info;
		[Sync]
		bool isRepairing = false;

		public bool Disabled
		{
			get	{ return self.traits.WithInterface<IDisable>().Any(t => t.Disabled); }
		}

		public Building(Actor self)
		{
			this.self = self;
			Info = self.Info.Traits.Get<BuildingInfo>();
			self.CenterLocation = Game.CellSize 
				* ((float2)self.Location + .5f * (float2)Info.Dimensions);
		}
		
		public int GetPowerUsage()
		{
			var modifier = self.traits
				.WithInterface<IPowerModifier>()
				.Select(t => t.GetPowerModifier())
				.Product();
				
			var maxHP = self.Info.Traits.Get<BuildingInfo>().HP;

			if (Info.Power > 0)
				return (int)(modifier*(self.Health * Info.Power) / maxHP);
			else
				return (int)(modifier * Info.Power);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				ScreenShaker.RegisterShakeEffect(10, self.Location.ToFloat2()*new float2(24, 24), 3);
				Sound.Play("kaboom22.aud");
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
			{
				self.CancelActivity();
				self.QueueActivity(new Sell());
			}

			if (order.OrderString == "Repair")
			{
				isRepairing = !isRepairing;
			}
		}

		int remainingTicks;

		public void Tick(Actor self)
		{
			if (!isRepairing) return;

			if (remainingTicks == 0)
			{
				var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
				var buildingValue = csv != null ? csv.Value : self.Info.Traits.Get<BuildableInfo>().Cost;
				var maxHP = self.Info.Traits.Get<BuildingInfo>().HP;
				var costPerHp = (Rules.General.URepairPercent * buildingValue) / maxHP;
				var hpToRepair = Math.Min(Rules.General.URepairStep, maxHP - self.Health);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.TakeCash(cost))
				{
					remainingTicks = 1;
					return;
				}

				self.World.AddFrameEndTask(w => w.Add(new RepairIndicator(self)));
				self.InflictDamage(self, -hpToRepair, Rules.WarheadInfo["Super"]);
				if (self.Health == maxHP)
				{
					isRepairing = false;
					return;
				}
				remainingTicks = (int)(Rules.General.RepairRate * 60 * 25);
			}
			else
				--remainingTicks;
		}
		
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			foreach (var a in r)
			{
				yield return a;
				if (Disabled)
					yield return a.WithPalette("disabled");
			}
		}
	}
}
