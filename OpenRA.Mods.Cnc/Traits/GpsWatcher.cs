#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Required for `GpsPower`. Attach this to the player actor.")]
	class GpsWatcherInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new GpsWatcher(init.Self.Owner); }
	}

	interface IOnGpsRefreshed { void OnGpsRefresh(Actor self, Player player); }

	class GpsWatcher : ISync, IPreventsShroudReset
	{
		[Sync]
		public bool Launched { get; private set; }

		[Sync]
		public bool GrantedAllies { get; private set; }

		[Sync]
		public bool Granted { get; private set; }

		// Whether this watcher has explored the terrain (by becoming Launched, or an ally becoming Launched)
		[Sync]
		bool explored;

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
			RefreshGps(atek.Owner);
		}

		public void GpsAdd(Actor atek)
		{
			actors.Add(atek);
			RefreshGps(atek.Owner);
		}

		public void ReachedOrbit(Player launcher)
		{
			Launched = true;
			RefreshGps(launcher);
		}

		public void RefreshGps(Player launcher)
		{
			RefreshGranted();

			foreach (var i in launcher.World.ActorsWithTrait<GpsWatcher>())
				i.Trait.RefreshGranted();
		}

		void RefreshGranted()
		{
			var wasGranted = Granted;
			var wasGrantedAllies = GrantedAllies;
			var allyWatchers = owner.World.ActorsWithTrait<GpsWatcher>().Where(kv => kv.Actor.Owner.IsAlliedWith(owner));

			Granted = actors.Count > 0 && Launched;
			GrantedAllies = allyWatchers.Any(w => w.Trait.Granted);

			if (!explored && (Launched || allyWatchers.Any(w => w.Trait.Launched)))
			{
				explored = true;
				owner.Shroud.ExploreAll();
			}

			if (wasGranted != Granted || wasGrantedAllies != GrantedAllies)
				foreach (var tp in notifyOnRefresh.ToList())
					tp.Trait.OnGpsRefresh(tp.Actor, owner);
		}

		bool IPreventsShroudReset.PreventShroudReset(Actor self)
		{
			return Granted || GrantedAllies;
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
