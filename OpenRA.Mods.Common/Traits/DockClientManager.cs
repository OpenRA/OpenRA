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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Manages DockClients on the actor.")]
	public class DockClientManagerInfo : ConditionalTraitInfo, IDockClientManagerInfo
	{
		[Desc("How long (in ticks) to wait until (re-)checking for a nearby available DockHost.")]
		public readonly int SearchForDockDelay = 125;

		[Desc("The pathfinding cost penalty applied for each dock client waiting to unload at a DockHost.")]
		public readonly int OccupancyCostModifier = 12;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[CursorReference]
		[Desc("Cursor to display when able to dock at target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to dock at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		[Desc("Voice to be played when ordered to dock.")]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line of docking orders.")]
		public readonly Color DockLineColor = Color.Green;

		public override object Create(ActorInitializer init) { return new DockClientManager(init.Self, this); }
	}

	public class DockClientManager : ConditionalTrait<DockClientManagerInfo>, IResolveOrder, IOrderVoice, IIssueOrder, INotifyKilled, INotifyActorDisposing
	{
		readonly Actor self;
		protected IDockClient[] dockClients;
		public Color DockLineColor => Info.DockLineColor;
		public int OccupancyCostModifier => Info.OccupancyCostModifier;
		bool requireForceMove;

		public DockClientManager(Actor self, DockClientManagerInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			dockClients = self.TraitsImplementing<IDockClient>().ToArray();
		}

		public Actor ReservedHostActor { get; protected set; }
		public IDockHost ReservedHost { get; protected set; }

		IDockHost lastReservedDockHost = null;
		public IDockHost LastReservedHost
		{
			get
			{
				if (lastReservedDockHost != null)
				{
					if (!lastReservedDockHost.IsEnabledAndInWorld)
						lastReservedDockHost = null;
					else
						return lastReservedDockHost;
				}

				return ReservedHost;
			}
		}

		public void UnreserveHost()
		{
			if (ReservedHost != null)
			{
				lastReservedDockHost = ReservedHost;
				ReservedHost = null;
				ReservedHostActor = null;
				lastReservedDockHost.Unreserve(this);
			}
		}

		/// <summary>In addition returns true if reservation was successful or we have already been reserved at <paramref name="host"/>.</summary>
		public bool ReserveHost(Actor hostActor, IDockHost host)
		{
			if (host == null)
				return false;

			if (ReservedHost == host)
				return true;

			UnreserveHost();
			if (host.Reserve(hostActor, this))
			{
				ReservedHost = host;
				ReservedHostActor = hostActor;

				// After we have reserved a new Host we want to forget our old host.
				lastReservedDockHost = null;
				return true;
			}

			return false;
		}

		public void OnDockStarted(Actor self, Actor hostActor, IDockHost host)
		{
			foreach (var client in dockClients)
				client.OnDockStarted(self, hostActor, host);
		}

		public bool OnDockTick(Actor self, Actor hostActor, IDockHost host)
		{
			if (IsTraitDisabled)
				return true;

			var cancel = true;
			foreach (var client in dockClients)
				if (!client.OnDockTick(self, hostActor, host))
					cancel = false;

			return cancel;
		}

		public void OnDockCompleted(Actor self, Actor hostActor, IDockHost host)
		{
			foreach (var client in dockClients)
				client.OnDockCompleted(self, hostActor, host);

			UnreserveHost();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new DockActorTargeter(
					6,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					() => requireForceMove,
					CanQueueDockAt,
					(target, forceEnter) => CanDockAt(target, forceEnter, true));
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Dock" || order.OrderString == "ForceDock")
			{
				var target = order.Target;

				// Deliver orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				// TODO: support frozen actors
				if (target.Type != TargetType.Actor)
					return;

				self.QueueActivity(order.Queued, new MoveToDock(
					self,
					target.Actor,
					null,
					order.OrderString == "ForceDock",
					true,
					DockLineColor));

				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.Target.Type != TargetType.Actor || IsTraitDisabled)
				return null;

			if (order.OrderString != "Dock" && order.OrderString != "ForceDock")
				return null;

			if (CanQueueDockAt(order.Target.Actor, order.OrderString == "ForceDock", order.Queued))
				return Info.Voice;

			return null;
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Dock" || order.OrderID == "ForceDock")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}

		/// <summary>Do we have an enabled client with matching <paramref name="type"/>.</summary>
		public bool CanDock(BitSet<DockType> type, bool forceEnter = false)
		{
			return !IsTraitDisabled && dockClients.Any(client => client.CanDock(type, forceEnter));
		}

		/// <summary>Does this <paramref name="target"/> contain at least one enabled <see cref="IDockHost"/> with matching <see cref="DockType"/>.</summary>
		public bool CanDock(Actor target, bool forceEnter = false)
		{
			return !IsTraitDisabled &&
				target.TraitsImplementing<IDockHost>()
					.Any(host => dockClients.Any(client => client.CanDock(host.GetDockType, forceEnter)));
		}

		/// <summary>Can we dock to this <paramref name="host"/>.</summary>
		public bool CanDockAt(Actor hostActor, IDockHost host, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			return !IsTraitDisabled && dockClients.Any(
				client => client.CanDockAt(hostActor, host, forceEnter, ignoreOccupancy));
		}

		/// <summary>Can we dock to this <paramref name="target"/>.</summary>
		public bool CanDockAt(Actor target, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			return !IsTraitDisabled && target.TraitsImplementing<IDockHost>().Any(
				host => dockClients.Any(client => client.CanDockAt(target, host, forceEnter, ignoreOccupancy)));
		}

		/// <summary>Can we dock to this <paramref name="target"/>.</summary>
		public bool CanQueueDockAt(Actor target, bool forceEnter, bool isQueued)
		{
			return !IsTraitDisabled
				&& target.TraitsImplementing<IDockHost>()
				.Any(host => dockClients.Any(client => client.CanQueueDockAt(target, host, forceEnter, isQueued)));
		}

		/// <summary>Find the closest viable <see cref="IDockHost"/>.</summary>
		/// <remarks>If <paramref name="type"/> is not set, scans all clients. Does not check if <see cref="DockClientManager"/> is enabled.</remarks>
		public TraitPair<IDockHost>? ClosestDock(IDockHost ignore, BitSet<DockType> type = default, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			var clients = type.IsEmpty ? dockClients : AvailableDockClients(type, forceEnter);
			return self.World.ActorsWithTrait<IDockHost>()
				.Where(host =>
					host.Trait != ignore &&
					clients.Any(client => client.CanDockAt(host.Actor, host.Trait, forceEnter, ignoreOccupancy)))
				.ClosestDock(self, this);
		}

		/// <summary>Get viable <see cref="IDockHost"/>'s on the <paramref name="target"/>.</summary>
		/// <remarks>If <paramref name="type"/> is not set, checks all clients. Does not check if <see cref="DockClientManager"/> is enabled.</remarks>
		public IEnumerable<TraitPair<IDockHost>> AvailableDockHosts(Actor target, BitSet<DockType> type = default,
			bool forceEnter = false, bool ignoreOccupancy = false)
		{
			var clients = type.IsEmpty ? dockClients : AvailableDockClients(type, forceEnter);
			return target.TraitsImplementing<IDockHost>()
				.Where(host => clients.Any(client => client.CanDockAt(target, host, forceEnter, ignoreOccupancy)))
				.Select(host => new TraitPair<IDockHost>(target, host));
		}

		/// <summary>Get clients of matching <paramref name="type"/>.</summary>
		/// <remarks>Does not check if <see cref="DockClientManager"/> is enabled.</remarks>
		public IEnumerable<IDockClient> AvailableDockClients(BitSet<DockType> type, bool forceEnter = false)
		{
			return dockClients.Where(client => client.CanDock(type, forceEnter));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { UnreserveHost(); }

		void INotifyActorDisposing.Disposing(Actor self) { UnreserveHost(); }
	}

	public class DockActorTargeter : IOrderTargeter
	{
		readonly string enterCursor;
		readonly string enterBlockedCursor;
		readonly Func<bool> requireForceMove;
		readonly Func<Actor, bool, bool, bool> canTarget;
		readonly Func<Actor, bool, bool> useEnterCursor;

		public DockActorTargeter(int priority, string enterCursor, string enterBlockedCursor,
			Func<bool> requireForceMove, Func<Actor, bool, bool, bool> canTarget, Func<Actor, bool, bool> useEnterCursor)
		{
			OrderID = "Dock";
			OrderPriority = priority;
			this.enterCursor = enterCursor;
			this.enterBlockedCursor = enterBlockedCursor;
			this.requireForceMove = requireForceMove;
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; }
		public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
		{
			// TODO: support frozen actors
			if (target.Type != TargetType.Actor)
				return false;

			cursor = enterCursor;
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			var forceEnter = modifiers.HasModifier(TargetModifiers.ForceMove);
			OrderID = forceEnter ? "ForceDock" : "Dock";

			if (requireForceMove() && !forceEnter)
				return false;

			if (!canTarget(target.Actor, forceEnter, IsQueued))
				return false;

			cursor = IsQueued || useEnterCursor(target.Actor, forceEnter)
				? enterCursor
				: enterBlockedCursor;

			return true;
		}

		public virtual bool IsQueued { get; protected set; }
	}

	public static class DockExts
	{
		public static TraitPair<IDockHost>? ClosestDock(this IEnumerable<TraitPair<IDockHost>> docks, Actor clientActor, DockClientManager client)
		{
			var mobile = clientActor.TraitOrDefault<Mobile>();
			if (mobile != null)
			{
				// Overlapping hosts can become hidden.
				var lookup = docks
					.GroupBy(dock => clientActor.World.Map.CellContaining(dock.Trait.DockPosition))
					.ToDictionary(group => group.Key, group => group.First());

				// Start a search from each docks position:
				var path = mobile.PathFinder.FindPathToTargetCell(
					clientActor, lookup.Keys, clientActor.Location, BlockedByActor.None,
					location =>
					{
						if (!lookup.TryGetValue(location, out var dock))
							return 0;

						// Prefer docks with less occupancy (multiplier is to offset distance cost):
						// TODO: add custom weights. E.g. owner vs allied.
						return dock.Trait.ReservationCount * client.OccupancyCostModifier;
					});

				if (path.Count > 0)
					return lookup[path[^1]];
			}
			else
			{
				return docks
					.OrderBy(dock =>
						(clientActor.Location - clientActor.World.Map.CellContaining(dock.Trait.DockPosition)).LengthSquared +
						dock.Trait.ReservationCount * client.OccupancyCostModifier)
					.Cast<TraitPair<IDockHost>?>()
					.FirstOrDefault();
			}

			return null;
		}
	}
}
