using OpenRa.Game.GameRules;
using OpenRa.Game.Traits.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Effects;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class OwnedActorInfo
	{
		public readonly int HP = 0;
		public readonly ArmorType Armor = ArmorType.none;
		public readonly bool Crewed = false;		// replace with trait?
		public readonly int InitialFacing = 128;
		public readonly int Sight = 0;
	}

	class BuildingInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int Power = 0;
		public readonly bool RequiresPower = false;
		public readonly bool BaseNormal = true;
		public readonly int Adjacent = 1;
		public readonly bool Bib = false;
		public readonly bool Capturable = false;
		public readonly bool Repairable = true;
		public readonly string Footprint = "x";
		public readonly string[] Produces = { };		// does this go somewhere else?
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly bool WaterBound = false;
		public readonly bool Unsellable = false;

		public object Create(Actor self) { return new Building(self); }
	}

	class Building : INotifyDamage, IResolveOrder, ITick
	{
		readonly Actor self;
		[Obsolete] public readonly LegacyBuildingInfo unitInfo;
		public readonly BuildingInfo Info;
		[Sync]
		bool isRepairing = false;
		[Sync]
		bool manuallyDisabled = false;
		public bool ManuallyDisabled { get { return manuallyDisabled; } }
		public bool Disabled { get { return (manuallyDisabled || (Info.RequiresPower && self.Owner.GetPowerState() != PowerState.Normal)); } }
		bool wasDisabled = false;
		
		public Building(Actor self)
		{
			this.self = self;
			Info = self.Info.Traits.Get<BuildingInfo>();
			unitInfo = (LegacyBuildingInfo)self.LegacyInfo;
			self.CenterLocation = Game.CellSize 
				* ((float2)self.Location + .5f * (float2)Info.Dimensions);
		}
		
		public int GetPowerUsage()
		{
			if (manuallyDisabled)
				return 0;

			var maxHP = self.Info.Traits.Get<BuildingInfo>().HP;

			if (Info.Power > 0)
				return (self.Health * Info.Power) / maxHP;
			else
				return Info.Power;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Sound.Play("kaboom22.aud");
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
			
			if (order.OrderString == "PowerDown")
			{
				manuallyDisabled = !manuallyDisabled;
				Sound.Play((manuallyDisabled) ? "bleep12.aud" : "bleep11.aud");
			}
		}

		int remainingTicks;

		public void Tick(Actor self)
		{
			// If the disabled state has changed since the last frame
			if (Disabled ^ wasDisabled 
				&& (wasDisabled = Disabled)) // Yes, I mean assignment
					Game.world.AddFrameEndTask(w => w.Add(new PowerDownIndicator(self)));
			
			if (!isRepairing) return;

			if (remainingTicks == 0)
			{
				var maxHP = self.Info.Traits.Get<BuildingInfo>().HP;
				var costPerHp = (Rules.General.URepairPercent * self.Info.Traits.Get<BuildableInfo>().Cost) / maxHP;
				var hpToRepair = Math.Min(Rules.General.URepairStep, maxHP - self.Health);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.TakeCash(cost))
				{
					remainingTicks = 1;
					return;
				}

				Game.world.AddFrameEndTask(w => w.Add(new RepairIndicator(self)));
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
	}
}
