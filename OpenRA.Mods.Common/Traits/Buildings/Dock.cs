#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Reserve docking places for actors.")]
	public class DockInfo : ConditionalTraitInfo
	{
		[Desc("Docking position relative to the dock actors' center.",
			"Note that ground actors will simply move to the center of the cell containing the offset position.")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Actor types allowed to dock. Leave empty to accept all.")]
		public readonly HashSet<string> ValidActorTypes = new HashSet<string>();

		[Desc("List of blacklisted actor types. If an actor is listed in both lists, ExcludeActorTypes takes priority.")]
		public readonly HashSet<string> ExcludeActorTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new Dock(init.Self, this); }
	}

	public class Dock : ConditionalTrait<DockInfo>, ITick, INotifyOwnerChanged, INotifySold, INotifyActorDisposing
	{
		readonly Actor self;

		public Actor ReservedForActor { get; private set; }
		public Aircraft ReservedForAircraft { get; private set; }

		public Dock(Actor self, DockInfo info)
			: base(info)
		{
			this.self = self;
		}

		void ITick.Tick(Actor self)
		{
			// Nothing to do.
			if (ReservedForActor == null)
				return;

			if (!Target.FromActor(ReservedForActor).IsValidFor(self))
			{
				// Not likely to arrive now.
				if (ReservedForAircraft != null)
					ReservedForAircraft.UnReserve();
				else
					ReservedForActor = null;
			}
		}

		public void Reserve(Actor self, Actor forActor, Aircraft forAircraft = null)
		{
			if (ReservedForAircraft != null && ReservedForAircraft.MayYieldReservation)
				ReservedForAircraft.UnReserve();

			ReservedForActor = forActor;
			ReservedForAircraft = forAircraft;
		}

		public static bool IsReserved(Actor dockActor)
		{
			var docks = dockActor.Docks().ToArray();

			var hasDock = false;
			foreach (var dock in docks)
			{
				// If we got here, we have at least 1 Dock
				hasDock = true;

				// If we have at least one unreserved Dock, we're not reserved
				if (dock.ReservedForActor == null ||
					(dock.ReservedForAircraft != null && dock.ReservedForAircraft.MayYieldReservation))
					return false;
			}

			// If we got here and 'hasDock' is true, it means all of those docks are reserved.
			// If we don't have any docks to begin with, we're never reserved.
			return hasDock;
		}

		public static bool IsAvailableFor(Actor dockActor, Actor forActor)
		{
			var docks = dockActor.Docks().ToArray();

			var hasDock = false;
			foreach (var dock in docks)
			{
				// If we got here, we have at least 1 Dock
				hasDock = true;

				// If we have at least one unreserved (or reserved by requesting actor) Dock, we're available
				if (dock.ReservedForActor == null ||
					dock.ReservedForActor == forActor ||
					(dock.ReservedForAircraft != null && dock.ReservedForAircraft.MayYieldReservation))
					return true;
			}

			// If we got here and 'hasDock' is true, it means none of those docks are available.
			// If we don't have any docks to begin with, we're always available.
			return !hasDock;
		}

		// TODO: The fact that other places can't call this directly and instead have to call Aircraft.UnReserve()
		// which then calls this is a bit crap, there should be a smarter and less round-about way to do this.
		public void UnReserveAircraft()
		{
			ReservedForActor = null;
			ReservedForAircraft = null;
		}

		protected override void TraitDisabled(Actor self)
		{
			if (ReservedForAircraft != null)
				ReservedForAircraft.UnReserve();
			else
				ReservedForActor = null;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (ReservedForAircraft != null)
				ReservedForAircraft.UnReserve();
			else
				ReservedForActor = null;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (ReservedForAircraft != null)
				ReservedForAircraft.UnReserve();
			else
				ReservedForActor = null;
		}

		void INotifySold.Selling(Actor self)
		{
			if (ReservedForAircraft != null)
				ReservedForAircraft.UnReserve();
			else
				ReservedForActor = null;
		}

		void INotifySold.Sold(Actor self)
		{
			if (ReservedForAircraft != null)
				ReservedForAircraft.UnReserve();
			else
				ReservedForActor = null;
		}

		public bool DockEmpty
		{
			get
			{
				return ReservedForActor == null;
			}
		}

		public bool DockCanYield
		{
			get
			{
				return ReservedForAircraft != null && ReservedForAircraft.MayYieldReservation;
			}
		}
	}

	public static class DockExts
	{
		public static Dock FirstDockOrDefault(this Actor actor, string actorType = null, bool onlyValid = true)
		{
			var all = Docks(actor, actorType, onlyValid);
			return all.FirstOrDefault();
		}

		public static IEnumerable<Dock> Docks(this Actor actor, string actorType = null, bool onlyValid = true)
		{
			var all = actor.TraitsImplementing<Dock>()
				.Where(Exts.IsTraitEnabled);

			if (!onlyValid)
				return all;

			if (string.IsNullOrEmpty(actorType))
				return all.Where(e => e.Info.ValidActorTypes.Count == 0);

			return all.Where(e => (e.Info.ValidActorTypes.Count == 0 || e.Info.ValidActorTypes.Contains(actorType))
				&& (e.Info.ExcludeActorTypes.Count == 0 || !e.Info.ExcludeActorTypes.Contains(actorType)));
		}

		public static Dock RandomDockOrDefault(this Actor actor, World world, string actorType = null, Func<Dock, bool> p = null)
		{
			var allOfType = Docks(actor, actorType);
			if (!allOfType.Any())
				return null;

			var shuffled = allOfType.Shuffle(world.SharedRandom);
			return p != null ? shuffled.FirstOrDefault(p) : shuffled.First();
		}
	}
}
