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
	class Building : INotifyDamage, IResolveOrder, ITick
	{
		readonly Actor self;
		public readonly BuildingInfo unitInfo;
		[Sync]
		bool isRepairing = false;
		[Sync]
		bool manuallyDisabled = false;
		public bool ManuallyDisabled { get { return manuallyDisabled; } }
		public bool Disabled { get { return (manuallyDisabled || (unitInfo.Powered && self.Owner.GetPowerState() != PowerState.Normal)); } }
		bool wasDisabled = false;
		
		public Building(Actor self)
		{
			this.self = self;
			unitInfo = (BuildingInfo)self.Info;
			self.CenterLocation = Game.CellSize 
				* ((float2)self.Location + .5f * (float2)unitInfo.Dimensions);
		}
		
		public int GetPowerUsage()
		{
			if (manuallyDisabled)
				return 0;
			
			if (unitInfo.Power > 0)		/* todo: is this how real-ra scales it? */
				return (self.Health * unitInfo.Power) / unitInfo.Strength;
			else
				return unitInfo.Power;
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
				var costPerHp = (Rules.General.URepairPercent * self.Info.Cost) / self.Info.Strength;
				var hpToRepair = Math.Min(Rules.General.URepairStep, self.Info.Strength - self.Health);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.TakeCash(cost))
				{
					remainingTicks = 1;
					return;
				}

				Game.world.AddFrameEndTask(w => w.Add(new RepairIndicator(self)));
				self.InflictDamage(self, -hpToRepair, Rules.WarheadInfo["Super"]);
				if (self.Health == self.Info.Strength)
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
