#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(ActorInitializer init) { return new GpsPower(init.self, this); }
	}

	class GpsPower : SupportPower, INotifyDamage, ISync, INotifyStanceChanged, INotifySold
	{
		public GpsPower(Actor self, GpsPowerInfo info) : base(self, info) { }

		[Sync]
		public bool Granted;

		public override void Charged(Actor self, string key)
		{
			self.Owner.PlayerActor.Trait<SupportPowerManager>().Powers[key].Activate(new Order());
		}
		
		public override void Activate(Actor self, Order order)
		{
			self.World.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(self.Owner, Info.LaunchSound);

				w.Add(new SatelliteLaunch(self));

                /* there is only one shroud, but it is misleadingly available through Player.Shroud */
				w.Add(new DelayedAction((Info as GpsPowerInfo).RevealDelay * 25,
					() => { Granted = true; RefreshGps(self); }));
			});
		}

		public void Selling(Actor self)
		{
			DisableGps();
		}

		public void Sold(Actor self) { }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
                DisableGps();
			}
		}

		void DisableGps()
		{
			Granted = false;
			RefreshGps(self);
		}

		void RefreshGps(Actor self)
		{
			if (self.World.LocalPlayer != null)
				self.World.LocalShroud.Disabled = self.World.ActorsWithTrait<GpsPower>()
					.Any(p => p.Actor.Owner.Stances[self.World.LocalPlayer] == Stance.Ally &&
						p.Trait.Granted);
		}

		public void StanceChanged(Actor self, Player a, Player b, Stance oldStance, Stance newStance)
		{
			RefreshGps(self);
		}
	}
}
