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
	[Desc("Manages LinkClients on the actor.")]
	public class LinkClientManagerInfo : ConditionalTraitInfo, ILinkClientManagerInfo
	{
		[Desc("How long (in ticks) to wait until (re-)checking for a nearby available" + nameof(ILinkHost) + ".")]
		public readonly int SearchForLinkDelay = 125;

		[Desc("The pathfinding cost penalty applied for each link client waiting to unload at a " + nameof(ILinkHost) + ".")]
		public readonly int OccupancyCostModifier = 12;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[CursorReference]
		[Desc("Cursor to display when able to link to target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to link to target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		[Desc("Voice to be played when ordered to link.")]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line of linking orders.")]
		public readonly Color LinkLineColor = Color.Green;

		public override object Create(ActorInitializer init) { return new LinkClientManager(init.Self, this); }
	}

	public class LinkClientManager : ConditionalTrait<LinkClientManagerInfo>, IResolveOrder, IOrderVoice, IIssueOrder, INotifyKilled, INotifyActorDisposing
	{
		readonly Actor self;
		protected ILinkClient[] linkClients;
		public Color LinkLineColor => Info.LinkLineColor;
		public int OccupancyCostModifier => Info.OccupancyCostModifier;
		bool requireForceMove;

		public LinkClientManager(Actor self, LinkClientManagerInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			linkClients = self.TraitsImplementing<ILinkClient>().ToArray();
		}

		public Actor ReservedHostActor { get; protected set; }
		public ILinkHost ReservedHost { get; protected set; }

		ILinkHost lastReservedHost = null;
		public ILinkHost LastReservedHost
		{
			get
			{
				if (lastReservedHost != null)
				{
					if (!lastReservedHost.IsEnabledAndInWorld)
						lastReservedHost = null;
					else
						return lastReservedHost;
				}

				return ReservedHost;
			}
		}

		public void UnreserveHost()
		{
			if (ReservedHost != null)
			{
				lastReservedHost = ReservedHost;
				ReservedHost = null;
				ReservedHostActor = null;
				lastReservedHost.Unreserve(this);
			}
		}

		/// <summary>In addition returns true if reservation was succesful or we have already been reserved at <paramref name="host"/>.</summary>
		public bool ReserveHost(Actor hostActor, ILinkHost host)
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
				lastReservedHost = null;
				return true;
			}

			return false;
		}

		public void OnLinkStarted(Actor self, Actor hostActor, ILinkHost host)
		{
			foreach (var client in linkClients)
				client.OnLinkStarted(self, hostActor, host);
		}

		public bool OnLinkTick(Actor self, Actor hostActor, ILinkHost host)
		{
			if (IsTraitDisabled)
				return true;

			var cancel = true;
			foreach (var client in linkClients)
				if (!client.OnLinkTick(self, hostActor, host))
					cancel = false;

			return cancel;
		}

		public void OnLinkCompleted(Actor self, Actor hostActor, ILinkHost host)
		{
			foreach (var client in linkClients)
				client.OnLinkCompleted(self, hostActor, host);

			UnreserveHost();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new LinkActorTargeter(
					6,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					() => requireForceMove,
					LinkingPossible,
					(target, forceEnter) => CanLinkTo(target, forceEnter));
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Link" || order.OrderString == "ForceLink")
			{
				var target = order.Target;

				// Deliver orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				// TODO: support frozen actors
				if (target.Type != TargetType.Actor)
					return;

				if (IsTraitDisabled)
					return;

				var link = AvailableLinkHosts(target.Actor, false, order.OrderString == "Link").ClosestLinkHost(self, this);
				if (!link.HasValue)
					return;

				self.QueueActivity(order.Queued, new MoveToDock(self, link.Value.Actor, link.Value.Trait));
				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.Target.Type != TargetType.Actor || IsTraitDisabled)
				return null;

			if (order.OrderString == "Link" && CanLinkTo(order.Target.Actor, false, true))
				return Info.Voice;
			else if (order.OrderString == "ForceLink" && CanLinkTo(order.Target.Actor, true, true))
				return Info.Voice;

			return null;
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Link" || order.OrderID == "ForceLink")
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
		public bool LinkingPossible(BitSet<LinkType> type, bool forceEnter = false)
		{
			return !IsTraitDisabled && linkClients.Any(client => client.IsLinkingPossible(type, forceEnter));
		}

		/// <summary>Does this <paramref name="target"/> contain at least one enabled <see cref="ILinkHost"/> with maching <see cref="LinkType"/>.</summary>
		public bool LinkingPossible(Actor target, bool forceEnter = false)
		{
			return !IsTraitDisabled && target.TraitsImplementing<ILinkHost>().Any(host => linkClients.Any(client => client.IsLinkingPossible(host.GetLinkType, forceEnter)));
		}

		/// <summary>Can we lonk to this <paramref name="host"/>.</summary>
		public bool CanLinkTo(Actor hostActor, ILinkHost host, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			return !IsTraitDisabled && linkClients.Any(client => client.CanLinkTo(hostActor, host, forceEnter, ignoreOccupancy));
		}

		/// <summary>Can we link to this <paramref name="target"/>.</summary>
		public bool CanLinkTo(Actor target, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			return !IsTraitDisabled && target.TraitsImplementing<ILinkHost>().Any(host => linkClients.Any(client => client.CanLinkTo(target, host, forceEnter, ignoreOccupancy)));
		}

		/// <summary>Find the closest viable <see cref="ILinkHost"/>.</summary>
		/// <remarks>If <paramref name="type"/> is not set, scans all clients. Does not check if <see cref="LinkClientManager"/> is enabled.</remarks>
		public TraitPair<ILinkHost>? ClosestLinkHost(ILinkHost ignore, BitSet<LinkType> type = default, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			var clients = type.IsEmpty ? linkClients : AvailableLinkClients(type);
			return self.World.ActorsWithTrait<ILinkHost>()
				.Where(host => host.Trait != ignore && clients.Any(client => client.CanLinkTo(host.Actor, host.Trait, forceEnter, ignoreOccupancy)))
				.ClosestLinkHost(self, this);
		}

		/// <summary>Get viable <see cref="ILinkHost"/>'s on the <paramref name="target"/>.</summary>
		/// <remarks>Does not check if <see cref="LinkClientManager"/> is enabled.</remarks>
		public IEnumerable<TraitPair<ILinkHost>> AvailableLinkHosts(Actor target, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			return target.TraitsImplementing<ILinkHost>()
				.Where(host => linkClients.Any(client => client.CanLinkTo(target, host, forceEnter, ignoreOccupancy)))
				.Select(host => new TraitPair<ILinkHost>(target, host));
		}

		/// <summary>Get clients of matching <paramref name="type"/>.</summary>
		/// <remarks>Does not check if <see cref="LinkClientManager"/> is enabled.</remarks>
		public IEnumerable<ILinkClient> AvailableLinkClients(BitSet<LinkType> type)
		{
			return linkClients.Where(client => client.IsLinkingPossible(type));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { UnreserveHost(); }

		void INotifyActorDisposing.Disposing(Actor self) { UnreserveHost(); }
	}

	public class LinkActorTargeter : IOrderTargeter
	{
		readonly string enterCursor;
		readonly string enterBlockedCursor;
		readonly Func<bool> requireForceMove;
		readonly Func<Actor, bool, bool> canTarget;
		readonly Func<Actor, bool, bool> useEnterCursor;

		public LinkActorTargeter(int priority, string enterCursor, string enterBlockedCursor, Func<bool> requireForceMove, Func<Actor, bool, bool> canTarget, Func<Actor, bool, bool> useEnterCursor)
		{
			OrderID = "Link";
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
			OrderID = forceEnter ? "ForceLink" : "Link";

			if (requireForceMove() && !forceEnter)
				return false;

			if (!self.Owner.IsAlliedWith(target.Actor.Owner) || !canTarget(target.Actor, forceEnter))
				return false;

			cursor = useEnterCursor(target.Actor, forceEnter) ? enterCursor : enterBlockedCursor;
			return true;
		}

		public virtual bool IsQueued { get; protected set; }
	}

	public static class LinkExts
	{
		public static TraitPair<ILinkHost>? ClosestLinkHost(this IEnumerable<TraitPair<ILinkHost>> linkHosts, Actor clientActor, LinkClientManager client)
		{
			var mobile = clientActor.TraitOrDefault<Mobile>();
			if (mobile != null)
			{
				// Overlapping hosts can become hidden.
				var lookup = linkHosts.ToDictionary(host => clientActor.World.Map.CellContaining(host.Trait.LinkPosition));

				// Start a search from each host position:
				var path = mobile.PathFinder.FindPathToTargetCell(
					clientActor, lookup.Keys, clientActor.Location, BlockedByActor.None,
					location =>
					{
						if (!lookup.TryGetValue(location, out var host))
							return 0;

						// Prefer docks with less occupancy (multiplier is to offset distance cost):
						// TODO: add custom wieghts. E.g. owner vs allied.
						return host.Trait.ReservationCount * client.OccupancyCostModifier;
					});

				if (path.Count > 0)
					return lookup[path.Last()];
			}
			else
			{
				return linkHosts
					.OrderBy(host => (clientActor.Location - clientActor.World.Map.CellContaining(host.Trait.LinkPosition)).LengthSquared + host.Trait.ReservationCount * client.OccupancyCostModifier)
					.FirstOrDefault();
			}

			return null;
		}
	}
}
