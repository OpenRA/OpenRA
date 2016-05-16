#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
	[Desc("Required for `GpsPower`. Attach this to the player actor.")]
	class GpsWatcherInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new GpsWatcher(init.Self.Owner); }
	}

	interface IOnGpsRefreshed { void OnGpsRefresh(Actor self, Player player); }

	class GpsWatcher : ISync, IFogVisibilityModifier
	{
		[Sync] public bool Launched { get; private set; }
		[Sync] public bool GrantedAllies { get; private set; }
		[Sync] public bool Granted { get; private set; }

		readonly Player owner;

		readonly List<Actor> actors = new List<Actor>();
		readonly HashSet<TraitPair<IOnGpsRefreshed>> notifyOnRefresh = new HashSet<TraitPair<IOnGpsRefreshed>>();

		public GpsWatcher(Player owner)
		{
			this.owner = owner;
		}

		public void GpsRemove(Actor atek)
		{
			actors.Remove(atek);
			RefreshGps(atek);
		}

		public void GpsAdd(Actor atek)
		{
			actors.Add(atek);
			RefreshGps(atek);
		}

		public void Launch(Actor atek, GpsPowerInfo info)
		{
			atek.World.Add(new DelayedAction(info.RevealDelay * 25,
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

			if ((Granted || GrantedAllies) && atek.Owner.IsAlliedWith(owner))
				atek.Owner.Shroud.ExploreAll();
		}

		void RefreshGranted()
		{
			var wasGranted = Granted;
			var wasGrantedAllies = GrantedAllies;

			Granted = actors.Count > 0 && Launched;
			GrantedAllies = owner.World.ActorsHavingTrait<GpsWatcher>(g => g.Granted).Any(p => p.Owner.IsAlliedWith(owner));

			if (Granted || GrantedAllies)
				owner.Shroud.ExploreAll();

			if (wasGranted != Granted || wasGrantedAllies != GrantedAllies)
				foreach (var tp in notifyOnRefresh.ToList())
					tp.Trait.OnGpsRefresh(tp.Actor, owner);
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

			return gpsDot.IsDotVisible(owner);
		}

		public void RegisterForOnGpsRefreshed(Actor actor, IOnGpsRefreshed toBeNotified)
		{
			notifyOnRefresh.Add(new TraitPair<IOnGpsRefreshed>(actor, toBeNotified));
		}

		public void UnregisterForOnGpsRefreshed(Actor actor, IOnGpsRefreshed toBeNotified)
		{
			notifyOnRefresh.Remove(new TraitPair<IOnGpsRefreshed>(actor, toBeNotified));
		}
	}
}
