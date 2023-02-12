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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("A generic link host that services LinkClients at a distance.")]
	public class ProximityLinkHostInfo : ConditionalTraitInfo, ILinkHostInfo
	{
		[Desc("Linking type.")]
		public readonly BitSet<LinkType> Type;

		[Desc("How many clients can this host be reserved for? If set to -1, there is no limit.")]
		public readonly int MaxQueueLength = -1;

		[Desc("How long should the client wait before starting the linking sequence.")]
		public readonly int Wait = 10;

		[Desc("LinkHost position relative to the centre of the actor.")]
		public readonly WVec LinkOffset = WVec.Zero;

		[Desc("From how far away can clients be serviced?")]
		public readonly WDist Range = WDist.FromCells(4);

		[Desc("Does the client need to be preoccupied when linked?")]
		public readonly bool OccupyClient = false;

		public override object Create(ActorInitializer init) { return new ProximityLinkHost(init.Self, this); }
	}

	public class ProximityLinkHost : ConditionalTrait<ProximityLinkHostInfo>, ILinkHost, ITick, INotifySold, INotifyOwnerChanged, ISync, INotifyKilled, INotifyActorDisposing
	{
		protected readonly Actor Self;

		[Sync]
		protected bool preventLink = false;

		protected readonly List<LinkClientManager> ReservedLinkClients = new();
		protected readonly List<(TraitPair<LinkClientManager> Client, long Time)> WaitingClients = new();
		protected readonly List<TraitPair<LinkClientManager>> LinkedClients = new();
		protected INotifyLinkHost[] notifyLinkHosts;

		public ProximityLinkHost(Actor self, ProximityLinkHostInfo info)
			: base(info)
		{
			Self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			notifyLinkHosts = self.TraitsImplementing<INotifyLinkHost>().ToArray();
		}

		#region ILinkHost

		public BitSet<LinkType> GetLinkType => Info.Type;

		public bool IsEnabledAndInWorld => !preventLink && !IsTraitDisabled && !Self.IsDead && Self.IsInWorld;
		public int ReservationCount => ReservedLinkClients.Count;
		public bool CanBeReserved => Info.MaxQueueLength < 0 || ReservationCount < Info.MaxQueueLength;

		public WPos LinkPosition => Self.CenterPosition + Info.LinkOffset;
		public int LinkWait => Info.Wait;

		public WAngle LinkFacing => WAngle.Zero;

		public virtual bool IsLinkingPossible(Actor clientActor, ILinkClient client, bool ignoreReservations = false)
		{
			return !IsTraitDisabled && (ignoreReservations || CanBeReserved || ReservedLinkClients.Contains(client.LinkClientManager));
		}

		public virtual bool Reserve(Actor self, LinkClientManager client)
		{
			if (CanBeReserved && !ReservedLinkClients.Contains(client))
			{
				ReservedLinkClients.Add(client);
				client.ReserveHost(self, this);
				return true;
			}

			return false;
		}

		public virtual void UnreserveAll()
		{
			while (ReservedLinkClients.Count > 0)
				Unreserve(ReservedLinkClients[0]);

			WaitingClients.Clear();
		}

		public virtual void Unreserve(LinkClientManager client)
		{
			if (ReservedLinkClients.Remove(client))
				client.UnreserveHost();
		}

		public virtual void OnLinkStarted(Actor self, Actor clientActor, LinkClientManager client)
		{
			if (Info.OccupyClient || LinkWait <= 0)
				LinkStarted(self, new TraitPair<LinkClientManager>(clientActor, client));
			else
				WaitingClients.Add((new TraitPair<LinkClientManager>(clientActor, client), LinkWait + self.World.WorldTick));
		}

		public virtual void OnLinkCompleted(Actor self, Actor clientActor, LinkClientManager client)
		{
			if (clientActor != null && !clientActor.IsDead)
			{
				client.OnLinkCompleted(clientActor, self, this);

				foreach (var nd in clientActor.TraitsImplementing<INotifyLinkClient>())
					nd.Unlinked(clientActor, self);
			}

			foreach (var nd in notifyLinkHosts)
				nd.Unlinked(self, clientActor);

			LinkedClients.Remove(LinkedClients.First(c => c.Trait == client));
		}

		public virtual bool QueueMoveActivity(Activity moveToLinkHostActivity, Actor self, Actor clientActor, LinkClientManager client)
		{
			if ((clientActor.CenterPosition - LinkPosition).HorizontalLengthSquared > Info.Range.LengthSquared)
			{
				// TODO: MoveWithinRange doesn't support offsets.
				// TODO: MoveWithinRange considers the whole footprint instead of a point on the actor.
				moveToLinkHostActivity.QueueChild(clientActor.Trait<IMove>().MoveWithinRange(Target.FromActor(self), Info.Range));
				return true;
			}

			return false;
		}

		public virtual void QueueLinkActivity(Activity moveToLinkHostActivity, Actor self, Actor clientActor, LinkClientManager client)
		{
			if (Info.OccupyClient)
			{
				if (moveToLinkHostActivity == null)
					clientActor.QueueActivity(new ProximityLinkSequence(client, self, this, Info.Range.Length));
				else
					moveToLinkHostActivity.QueueChild(new ProximityLinkSequence(client, self, this, Info.Range.Length));
			}
			else
			{
				// Make sure OnLinkStarted is only called once.
				if (!WaitingClients.Any(p => p.Client.Trait == client) && !LinkedClients.Any(p => p.Trait == client))
					OnLinkStarted(self, clientActor, client);
			}
		}

		#endregion

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		bool ClientAliveAndInRange(Actor clientActor) => clientActor != null && !clientActor.IsDead && clientActor.IsInWorld
			&& (clientActor.CenterPosition - LinkPosition).HorizontalLengthSquared <= Info.Range.LengthSquared;

		protected virtual void Tick(Actor self)
		{
			if (Info.OccupyClient || IsTraitDisabled)
				return;

			// Track wait time manually.
			for (var i = 0; i < WaitingClients.Count; i++)
			{
				if (WaitingClients[i].Time < self.World.WorldTick)
				{
					var client = WaitingClients[i].Client.Trait;
					if (ClientAliveAndInRange(WaitingClients[i].Client.Actor) && client.CanLinkTo(self, this, false, true))
						LinkStarted(self, WaitingClients[i].Client);
					else
						Unreserve(client);

					WaitingClients.RemoveAt(i);
					i--;
				}
			}

			// Tick clients manually.
			for (var i = 0; i < LinkedClients.Count; i++)
			{
				var clientActor = LinkedClients[i].Actor;
				if (ClientAliveAndInRange(clientActor) && !LinkedClients[i].Trait.OnLinkTick(clientActor, self, this))
					continue;

				OnLinkCompleted(self, clientActor, LinkedClients[i].Trait);
				i--;
			}
		}

		protected virtual void LinkStarted(Actor self, TraitPair<LinkClientManager> client)
		{
			LinkedClients.Add(client);

			foreach (var nd in client.Actor.TraitsImplementing<INotifyLinkClient>())
				nd.Linked(client.Actor, self);

			foreach (var ndh in notifyLinkHosts)
				ndh.Linked(self, client.Actor);

			client.Trait.OnLinkStarted(client.Actor, self, this);
		}

		public virtual void CancelLink(Actor self)
		{
			// Cancelling will be handled in the RemoteLinkSequence activity.
			if (Info.OccupyClient)
				return;

			while (LinkedClients.Count != 0)
			{
				var pair = LinkedClients[0];
				OnLinkCompleted(self, pair.Actor, pair.Trait);
			}
		}

		protected override void TraitDisabled(Actor self) { CancelLink(self); UnreserveAll(); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { CancelLink(self); UnreserveAll(); }

		void INotifySold.Selling(Actor self) { preventLink = true; }

		void INotifySold.Sold(Actor self) { CancelLink(self); UnreserveAll(); }

		void INotifyKilled.Killed(Actor self, AttackInfo e) { CancelLink(self); UnreserveAll(); }

		void INotifyActorDisposing.Disposing(Actor self) { CancelLink(self); UnreserveAll(); preventLink = true; }
	}
}
