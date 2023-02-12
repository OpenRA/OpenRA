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
	public class ProximityLinkHostInfo : ConditionalTraitInfo, IDockHostInfo
	{
		[Desc("Linking type.")]
		public readonly BitSet<DockType> Type;

		[Desc("How many clients can this host be reserved for? If set to -1, there is no limit.")]
		public readonly int MaxQueueLength = -1;

		[Desc("How long should the client wait before starting the linking sequence.")]
		public readonly int Wait = 10;

		[Desc("LinkHost position relative to the centre of the actor.")]
		public readonly WVec LinkOffset = WVec.Zero;

		[Desc("From how far away can clients be serviced?")]
		public readonly WDist Range = WDist.FromCells(4);

		[Desc("Does the client need to be occupied when linked?")]
		public readonly bool OccupyClient = false;

		public override object Create(ActorInitializer init) { return new ProximityLinkHost(init.Self, this); }
	}

	public class ProximityLinkHost : ConditionalTrait<ProximityLinkHostInfo>,
		IDockHost, ITick, INotifySold, INotifyOwnerChanged, ISync, INotifyKilled, INotifyActorDisposing
	{
		protected readonly Actor Self;

		[Sync]
		protected bool preventLink = false;

		protected readonly List<DockClientManager> ReservedLinkClients = new();
		protected readonly List<(TraitPair<DockClientManager> Client, long Time)> WaitingClients = new();
		protected readonly List<TraitPair<DockClientManager>> LinkedClients = new();
		protected INotifyDockHost[] notifyLinkHosts;

		public ProximityLinkHost(Actor self, ProximityLinkHostInfo info)
			: base(info)
		{
			Self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			notifyLinkHosts = self.TraitsImplementing<INotifyDockHost>().ToArray();
		}

		#region IDockHost

		public BitSet<DockType> GetDockType => Info.Type;

		public bool IsEnabledAndInWorld => !preventLink && !IsTraitDisabled && !Self.IsDead && Self.IsInWorld;
		public int ReservationCount => ReservedLinkClients.Count;
		public bool CanBeReserved => Info.MaxQueueLength < 0 || ReservationCount < Info.MaxQueueLength;

		public WPos DockPosition => Self.CenterPosition + Info.LinkOffset;

		public virtual bool IsDockingPossible(Actor clientActor, IDockClient client, bool ignoreReservations = false)
		{
			return !IsTraitDisabled && (ignoreReservations || CanBeReserved || ReservedLinkClients.Contains(client.DockClientManager));
		}

		public virtual bool Reserve(Actor self, DockClientManager client)
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

		public virtual void Unreserve(DockClientManager client)
		{
			if (ReservedLinkClients.Remove(client))
				client.UnreserveHost();
		}

		public virtual void OnDockStarted(Actor self, Actor clientActor, DockClientManager client)
		{
			if (Info.OccupyClient || Info.Wait <= 0)
				LinkCreated(self, new TraitPair<DockClientManager>(clientActor, client));
			else
				WaitingClients.Add((new TraitPair<DockClientManager>(clientActor, client), Info.Wait + self.World.WorldTick));
		}

		public virtual void OnDockCompleted(Actor self, Actor clientActor, DockClientManager client)
		{
			if (clientActor != null && !clientActor.IsDead)
			{
				client.OnDockCompleted(clientActor, self, this);

				foreach (var nd in clientActor.TraitsImplementing<INotifyDockClient>())
					nd.Undocked(clientActor, self);
			}

			foreach (var nd in notifyLinkHosts)
				nd.Undocked(self, clientActor);

			LinkedClients.Remove(LinkedClients.First(c => c.Trait == client));
		}

		public virtual bool QueueMoveActivity(Activity moveToLinkHostActivity, Actor self,
			Actor clientActor, DockClientManager client, MoveCooldownHelper moveCooldownHelper)
		{
			if ((clientActor.CenterPosition - DockPosition).HorizontalLengthSquared > Info.Range.LengthSquared)
			{
				moveCooldownHelper.NotifyMoveQueued();

				// TODO: MoveWithinRange doesn't support offsets.
				// TODO: MoveWithinRange considers the whole footprint instead of a point on the actor.
				moveToLinkHostActivity.QueueChild(clientActor.Trait<IMove>().MoveWithinRange(Target.FromActor(self), Info.Range));
				return true;
			}

			return false;
		}

		public virtual void QueueDockActivity(Activity moveToLinkHostActivity, Actor self, Actor clientActor, DockClientManager client)
		{
			if (Info.OccupyClient)
			{
				if (moveToLinkHostActivity == null)
					clientActor.QueueActivity(new ProximityLinkSequence(client, self, this, Info.Range.Length, Info.Wait));
				else
					moveToLinkHostActivity.QueueChild(new ProximityLinkSequence(client, self, this, Info.Range.Length, Info.Wait));
			}
			else
			{
				// Make sure OnLinkStarted is only called once.
				if (!WaitingClients.Any(p => p.Client.Trait == client) && !LinkedClients.Any(p => p.Trait == client))
					OnDockStarted(self, clientActor, client);
			}
		}

		#endregion

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		bool ClientAliveAndInRange(Actor clientActor) => clientActor != null && !clientActor.IsDead && clientActor.IsInWorld
			&& (clientActor.CenterPosition - DockPosition).HorizontalLengthSquared <= Info.Range.LengthSquared;

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
					if (ClientAliveAndInRange(WaitingClients[i].Client.Actor) && client.CanDockAt(self, this, false, true))
						LinkCreated(self, WaitingClients[i].Client);
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
				if (ClientAliveAndInRange(clientActor) && !LinkedClients[i].Trait.OnDockTick(clientActor, self, this))
					continue;

				OnDockCompleted(self, clientActor, LinkedClients[i].Trait);
				i--;
			}
		}

		protected virtual void LinkCreated(Actor self, TraitPair<DockClientManager> client)
		{
			LinkedClients.Add(client);

			foreach (var nd in client.Actor.TraitsImplementing<INotifyDockClient>())
				nd.Docked(client.Actor, self);

			foreach (var ndh in notifyLinkHosts)
				ndh.Docked(self, client.Actor);

			client.Trait.OnDockStarted(client.Actor, self, this);
		}

		public virtual void CancelLink(Actor self)
		{
			// Cancelling will be handled in the ProximityLinkSequence activity.
			if (Info.OccupyClient)
				return;

			while (LinkedClients.Count != 0)
			{
				var pair = LinkedClients[0];
				OnDockCompleted(self, pair.Actor, pair.Trait);
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
