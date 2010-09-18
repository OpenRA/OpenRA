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
	public class BuildingInfo : ITraitInfo
	{
		public readonly int Power = 0;
		public readonly bool BaseNormal = true;
		public readonly bool WaterBound = false;
		public readonly int Adjacent = 2;
		public readonly bool Capturable = false;
		public readonly string Footprint = "x";
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly bool Unsellable = false;
		public readonly float RefundPercent = 0.5f;
		
		public readonly string[] BuildSounds = {"placbldg.aud", "build5.aud"};
		public readonly string[] SellSounds = {"cashturn.aud"};
		public readonly string DamagedSound = "kaboom1.aud";
		public readonly string DestroyedSound = "kaboom22.aud";

		public object Create(ActorInitializer init) { return new Building(init); }
	}

	public class Building : INotifyDamage, IResolveOrder, IRenderModifier, IOccupySpace
	{
		readonly Actor self;
		public readonly BuildingInfo Info;
		[Sync]
		readonly int2 topLeft;
		
		readonly PowerManager PlayerPower;

		public bool Disabled
		{
			get	{ return self.TraitsImplementing<IDisable>().Any(t => t.Disabled); }
		}

		public Building(ActorInitializer init)
		{
			this.self = init.self;
			this.topLeft = init.Get<LocationInit,int2>();
			Info = self.Info.Traits.Get<BuildingInfo>();
			self.CenterLocation = Game.CellSize 
				* ((float2)topLeft + .5f * (float2)Info.Dimensions);
			
			PlayerPower = init.self.Owner.PlayerActor.Trait<PowerManager>();
		}
		
		public int GetPowerUsage()
		{
			var modifier = self
				.TraitsImplementing<IPowerModifier>()
				.Select(t => t.GetPowerModifier())
				.Product();
			
			if (Info.Power > 0)
			{
				var health = self.TraitOrDefault<Health>();
				var healthFraction = (health == null) ? 1f : health.HPFraction;
				return (int)(modifier * healthFraction * Info.Power);
			}
			else
				return (int)(modifier * Info.Power);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// Power plants lose power with damage
			if (Info.Power > 0)
			{
				var modifier = self
					.TraitsImplementing<IPowerModifier>()
					.Select(t => t.GetPowerModifier())
					.Product();
					
				var health = self.TraitOrDefault<Health>();
				var healthFraction = (health == null) ? 1f : health.HPFraction;
				PlayerPower.UpdateActor(self, (int)(modifier * healthFraction * Info.Power));
			}
			
			if (e.DamageState == DamageState.Dead)
			{
				self.World.WorldActor.Trait<ScreenShaker>().AddEffect(10, self.CenterLocation, 1);
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
	}
}
