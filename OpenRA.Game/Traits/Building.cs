#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits.Activities;

namespace OpenRA.Traits
{
	public class OwnedActorInfo
	{
		public readonly int HP = 0;
		public readonly ArmorType Armor = ArmorType.none;
		public readonly bool Crewed = false;		// replace with trait?
		public readonly int Sight = 0;
		public readonly bool WaterBound = false;
		public readonly string TargetType = "Ground";
	}

	public class BuildingInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int Power = 0;
		public readonly bool BaseNormal = true;
		public readonly int Adjacent = 2;
		public readonly bool Capturable = false;
		public readonly bool Repairable = true;
		public readonly string Footprint = "x";
		public readonly string[] Produces = { };		// does this go somewhere else?
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly bool Unsellable = false;

		public readonly string[] BuildSounds = {"placbldg.aud", "build5.aud"};
		public readonly string[] SellSounds = {"cashturn.aud"};
		public readonly string DamagedSound = "kaboom1.aud";
		public readonly string DestroyedSound = "kaboom22.aud";

		public object Create(ActorInitializer init) { return new Building(init); }
	}

	public class Building : INotifyDamage, IResolveOrder, ITick, IRenderModifier, IOccupySpace, IRadarSignature, IRevealShroud
	{
		readonly Actor self;
		public readonly BuildingInfo Info;
		[Sync]
		readonly int2 topLeft;
		[Sync]
		bool isRepairing = false;

		public bool Disabled
		{
			get	{ return self.traits.WithInterface<IDisable>().Any(t => t.Disabled); }
		}

		public Building(ActorInitializer init)
		{
			this.self = init.self;
			this.topLeft = init.location;
			Info = self.Info.Traits.Get<BuildingInfo>();
			self.CenterLocation = Game.CellSize 
				* ((float2)topLeft + .5f * (float2)Info.Dimensions);
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
				self.World.WorldActor.traits.Get<ScreenShaker>().AddEffect(10, self.CenterLocation, 1);
				Sound.Play(Info.DestroyedSound, self.CenterLocation);
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
				var buildingValue = csv != null ? csv.Value : self.Info.Traits.Get<ValuedInfo>().Cost;
				var maxHP = self.Info.Traits.Get<BuildingInfo>().HP;
				var costPerHp = (self.World.Defaults.RepairPercent * buildingValue) / maxHP;
				var hpToRepair = Math.Min(self.World.Defaults.RepairStep, maxHP - self.Health);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.PlayerActor.traits.Get<PlayerResources>().TakeCash(cost))
				{
					remainingTicks = 1;
					return;
				}

				self.World.AddFrameEndTask(w => w.Add(new RepairIndicator(self)));
				self.InflictDamage(self, -hpToRepair, null);
				if (self.Health == maxHP)
				{
					isRepairing = false;
					return;
				}
				remainingTicks = (int)(self.World.Defaults.RepairRate * 60 * 25);
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

		public int2 TopLeft
		{
			get { return topLeft; }
		}

		public IEnumerable<int2> OccupiedCells()
		{
			return Footprint.UnpathableTiles( self.Info.Name, Info, TopLeft );
		}
		
		public IEnumerable<int2> RadarSignatureCells(Actor self)
		{
			foreach (var mod in self.traits.WithInterface<IRadarVisibilityModifier>())
				if (!mod.VisibleOnRadar(self))
					return new int2[] {};
				
			return Footprint.Tiles(self);
		}
		
		public Color RadarSignatureColor(Actor self)
		{
			var mod = self.traits.WithInterface<IRadarColorModifier>().FirstOrDefault();
			if (mod != null)
				return mod.RadarColorOverride(self);
			
			return self.Owner.Color;
		}
	}
}
