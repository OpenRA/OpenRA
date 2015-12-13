#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Required for GpsPower. Attach this to the player actor.")]
	class GpsWatcherInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new GpsWatcher(init.Self.Owner); }
	}

	class GpsWatcher : ISync, IFogVisibilityModifier
	{
		[Sync] public bool Launched = false;
		[Sync] public bool GrantedAllies = false;
		[Sync] public bool Granted = false;
		public Player Owner;

		List<Actor> actors = new List<Actor> { };

		public GpsWatcher(Player owner) { Owner = owner; }

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
			atek.World.Add(new DelayedAction(((GpsPowerInfo)info).RevealDelay * 25,
					() =>
					{
						Launched = true;
						RefreshGps(atek);
					}));
		}

		public void RefreshGps(Actor atek)
		{
			RefreshGranted();

			foreach (var i in atek.World.ActorsWithTrait<GpsWatcher>())
				i.Trait.RefreshGranted();

			if ((Granted || GrantedAllies) && atek.Owner.IsAlliedWith(Owner))
				atek.Owner.Shroud.ExploreAll(atek.World);
		}

		void RefreshGranted()
		{
			Granted = actors.Count > 0 && Launched;
			GrantedAllies = Owner.World.ActorsHavingTrait<GpsWatcher>(g => g.Granted).Any(p => p.Owner.IsAlliedWith(Owner));

			if (Granted || GrantedAllies)
				Owner.Shroud.ExploreAll(Owner.World);
		}

		public bool HasFogVisibility()
		{
			return Granted || GrantedAllies;
		}

		public bool IsVisible(Actor actor)
		{
			var gpsDot = actor.TraitOrDefault<GpsDot>();
			if (gpsDot == null)
				return false;

			return gpsDot.IsDotVisible(Owner);
		}
	}

	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(ActorInitializer init) { return new GpsPower(init.Self, this); }
	}

	class GpsPower : SupportPower, INotifyKilled, INotifyStanceChanged, INotifySold, INotifyOwnerChanged
	{
		GpsWatcher owner;

		public GpsPower(Actor self, GpsPowerInfo info)
			: base(self, info)
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
				Game.Sound.PlayToPlayer(self.Owner, Info.LaunchSound);

				w.Add(new SatelliteLaunch(self));

				owner.Launch(self, Info);
			});
		}

		public void Killed(Actor self, AttackInfo e) { RemoveGps(self); }

		public void Selling(Actor self) { }
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

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RemoveGps(self);
			owner = newOwner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}
	}
}
