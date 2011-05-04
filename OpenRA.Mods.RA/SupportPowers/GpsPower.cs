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
using System.Drawing;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GpsWatcherInfo : TraitInfo<GpsWatcher> { }
	
	class GpsWatcher : ISync
	{
		bool Launched = false;
		List<Actor> actors = new List<Actor> { };
		[Sync]
		public bool GrantedAllies = false;
		[Sync]
		public bool Granted = false;

		public void GpsRem(Actor self)
		{
			actors.Remove(self);
			RefreshGps(self);
		}

		public void GpsAdd(Actor self)
		{
			actors.Add(self);
			RefreshGps(self);
		}

		public void Launch(Actor self, SupportPowerInfo info)
		{
			self.World.Add(new DelayedAction((info as GpsPowerInfo).RevealDelay * 25,
					() =>
					{
						Launched = true;
						RefreshGps(self);
					}));
		}

		public void RefreshGps(Actor self)
		{
			Granted = (actors.Count > 0 && Launched);
			GrantedAllies = self.World.ActorsWithTrait<GpsWatcher>().Any(p =>
					p.Actor.Owner.Stances[self.Owner] == Stance.Ally && p.Trait.Granted);

			if (self.World.LocalPlayer == null)
				return;

			if ((Granted || GrantedAllies) && self.World.LocalPlayer == self.Owner)
			{
				self.World.WorldActor.Trait<Shroud>().ExploreAll(self.World);
			}
		}
	}

	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(ActorInitializer init) { return new GpsPower(init.self, this); }
	}

	class GpsPower : SupportPower, INotifyKilled, ISync, INotifyStanceChanged, INotifySold, INotifyCapture
	{
		GpsWatcher owner;

		public GpsPower(Actor self, GpsPowerInfo info) : base(self, info)
		{
			owner = self.Owner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}

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

				owner.Launch(self, Info);
			});
		}

		public void Killed(Actor self, AttackInfo e) { RemoveGps(self); }

		public void Selling(Actor self) {}
		public void Sold(Actor self) { RemoveGps(self); }

		void RemoveGps(Actor self)
		{
			// Extra function just in case something needs to be added later
			owner.GpsRem(self);
		}

		public void StanceChanged(Actor self, Player a, Player b, Stance oldStance, Stance newStance)
		{
			owner.RefreshGps(self);
		}
		
		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			RemoveGps(self);
			owner = captor.Owner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}
	}
}
