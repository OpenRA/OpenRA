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
		public bool Active { get { return active || allyActive; } }

		readonly Player owner;

		readonly List<Actor> sources = new List<Actor>();
		readonly HashSet<TraitPair<IOnGpsRefreshed>> notifyOnRefresh = new HashSet<TraitPair<IOnGpsRefreshed>>();

		// Whether this watcher has been enabled (by launching a satellite)
		[Sync] bool enabled;

		// Whether this watcher has explored the terrain (by becoming enabled, or an ally becoming enabled)
		[Sync] bool explored;

		// Whether this or an ally's watcher is currently active (> 0 sources and enabled)
		[Sync] bool active;
		[Sync] bool allyActive;

		public GpsWatcher(Player owner)
		{
			this.owner = owner;
		}

		public void RemoveSource(Actor source)
		{
			sources.Remove(source);
			Refresh();
		}

		public void AddSource(Actor source)
		{
			sources.Add(source);
			Refresh();
		}

		public void Launch(Actor source, GpsPowerInfo info)
		{
			source.World.Add(new DelayedAction(info.RevealDelay * 25, () =>
			{
				enabled = true;
				Refresh();
			}));
		}

		void Refresh()
		{
			RefreshInner();

			// Refresh the state of all allied players (including ourselves, again...)
			foreach (var i in owner.World.ActorsWithTrait<GpsWatcher>().Where(kv => kv.Actor.Owner.IsAlliedWith(owner)))
				i.Trait.RefreshInner();
		}

		void RefreshInner()
		{
			var wasActive = Active;
			var allyWatchers = owner.World.ActorsWithTrait<GpsWatcher>().Where(kv => kv.Actor.Owner.IsAlliedWith(owner));

			active = sources.Count > 0 && enabled;
			allyActive = allyWatchers.Any(w => w.Trait.active);

			// Shroud is only explored once, when the watcher is first enabled
			var allyEnabled = allyWatchers.Any(w => w.Trait.enabled);
			if ((enabled || allyEnabled) && !explored)
			{
				explored = true;
				owner.Shroud.ExploreAll();
			}

			if (wasActive != Active)
				foreach (var tp in notifyOnRefresh.ToList())
					tp.Trait.OnGpsRefresh(tp.Actor, owner);
		}

		public bool HasFogVisibility()
		{
			return Active;
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
