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
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GpsWatcherInfo : ITraitInfo
	{
		public object Create (ActorInitializer init) { return new GpsWatcher(init.self.Owner); }
	}

	class GpsWatcher : ISync
	{
		[Sync] bool Launched = false;
		[Sync] public bool GrantedAllies = false;
		[Sync] public bool Granted = false;
		Player owner;

		List<Actor> actors = new List<Actor> { };

		public GpsWatcher(Player owner) { this.owner = owner; }

		public void GpsRem(Actor atek)
		{
			actors.Remove(atek);
			RefreshGps(atek);
		}

		public void GpsAdd(Actor atek)
		{
			actors.Add(atek);
			RefreshGps(atek);
		}

		public void Launch(Actor atek, SupportPowerInfo info)
		{
			atek.World.Add(new DelayedAction((info as GpsPowerInfo).RevealDelay * 25,
					() =>
					{
						Launched = true;
						RefreshGps(atek);
					}));
		}

		public void RefreshGps(Actor atek)
		{
			RefreshGranted();
			
			foreach (TraitPair<GpsWatcher> i in atek.World.ActorsWithTrait<GpsWatcher>())
				i.Trait.RefreshGranted();

			if ((Granted || GrantedAllies) && atek.Owner.IsAlliedWith(atek.World.RenderPlayer))
				atek.Owner.Shroud.ExploreAll(atek.World);
		}

		void RefreshGranted()
		{
			Granted = (actors.Count > 0 && Launched);
			GrantedAllies = owner.World.ActorsWithTrait<GpsWatcher>().Any(p => p.Actor.Owner.IsAlliedWith(owner) && p.Trait.Granted);

			if (Granted || GrantedAllies)
				owner.Shroud.ExploreAll(owner.World);
		}
	}

	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(ActorInitializer init) { return new GpsPower(init.self, this); }
	}

	class GpsPower : SupportPower, INotifyKilled, INotifyStanceChanged, INotifySold, INotifyCapture
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

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

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
