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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class LinkType { LinkType() { } }

	[Desc("A generic dock that services DockClients.")]
	public class DockHostInfo : ConditionalTraitInfo, ILinkHostInfo
	{
		[Desc("Link type.")]
		public readonly BitSet<LinkType> Type;

		[Desc("How many clients can this dock be reserved for?")]
		public readonly int MaxQueueLength = 3;

		[Desc("How long should the client wait before starting the docking sequence.")]
		public readonly int DockWait = 10;

		[Desc("Actual client facing when docking.")]
		public readonly WAngle DockAngle = WAngle.Zero;

		[Desc("Docking cell relative to the centre of the actor.")]
		public readonly WVec DockOffset = WVec.Zero;

		[Desc("Does client need to be dragged in?")]
		public readonly bool IsDragRequired = false;

		[Desc("Vector by which the client will be dragged when docking.")]
		public readonly WVec DragOffset = WVec.Zero;

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 0;

		public override object Create(ActorInitializer init) { return new DockHost(init.Self, this); }
	}

	public class DockHost : ConditionalTrait<DockHostInfo>, ILinkHost, ILinkHostDrag, ITick, INotifySold, INotifyCapture, INotifyOwnerChanged, ISync, INotifyKilled, INotifyActorDisposing
	{
		readonly Actor self;

		public BitSet<LinkType> GetLinkType => Info.Type;
		public bool IsEnabledAndInWorld => !preventLink && !IsTraitDisabled && !self.IsDead && self.IsInWorld;
		public int ReservationCount => ReservedLinkClients.Count;
		public bool CanBeReserved => ReservationCount < Info.MaxQueueLength;
		protected readonly List<LinkClientManager> ReservedLinkClients = new();

		public WPos LinkPosition => self.CenterPosition + Info.DockOffset;
		public int LinkWait => Info.DockWait;
		public WAngle LinkFacing => Info.DockAngle;

		bool ILinkHostDrag.IsDragRequired => Info.IsDragRequired;
		WVec ILinkHostDrag.DragOffset => Info.DragOffset;
		int ILinkHostDrag.DragLength => Info.DragLength;

		[Sync]
		bool preventLink = false;

		[Sync]
		protected Actor linkedClientActor = null;
		protected LinkClientManager linkedClient = null;

		public DockHost(Actor self, DockHostInfo info)
			: base(info)
		{
			this.self = self;
		}

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
		}

		public virtual void Unreserve(LinkClientManager client)
		{
			if (ReservedLinkClients.Remove(client))
				client.UnreserveHost();
		}

		public virtual void OnLinkStarted(Actor self, Actor clientActor, LinkClientManager client)
		{
			linkedClientActor = clientActor;
			linkedClient = client;
		}

		public virtual void OnLinkCompleted(Actor self, Actor clientActor, LinkClientManager client)
		{
			linkedClientActor = null;
			linkedClient = null;
		}

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			// Client was killed during docking.
			if (linkedClientActor != null && (linkedClientActor.IsDead || !linkedClientActor.IsInWorld))
				OnLinkCompleted(self, linkedClientActor, linkedClient);
		}

		public virtual bool QueueMoveActivity(Activity moveToLinkHostActivity, Actor self, Actor clientActor, LinkClientManager client)
		{
			var move = clientActor.Trait<IMove>();

			// Make sure the actor is at dock, at correct facing, and aircraft are landed.
			// Mobile cannot freely move in WPos, so when we calculate close enough we convert to CPos.
			if ((move is Mobile ? clientActor.Location != clientActor.World.Map.CellContaining(LinkPosition) : clientActor.CenterPosition != LinkPosition)
				|| move is not IFacing facing || facing.Facing != LinkFacing)
			{
				moveToLinkHostActivity.QueueChild(move.MoveOntoTarget(clientActor, Target.FromActor(self), LinkPosition - self.CenterPosition, LinkFacing));
				return true;
			}

			return false;
		}

		public virtual void QueueLinkActivity(Activity moveToLinkHostActivity, Actor self, Actor clientActor, LinkClientManager client)
		{
			moveToLinkHostActivity.QueueChild(new GenericDockSequence(clientActor, client, self, this));
		}

		protected override void TraitDisabled(Actor self) { UnreserveAll(); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UnreserveAll(); }

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			// Steal any linked unit too.
			if (linkedClientActor != null && !linkedClientActor.IsDead && linkedClientActor.IsInWorld)
			{
				linkedClientActor.ChangeOwner(newOwner);

				// On capture OnOwnerChanged event is called first, so we need to re-reserve.
				linkedClient.ReserveHost(self, this);
			}
		}

		void INotifySold.Selling(Actor self) { preventLink = true; }

		void INotifySold.Sold(Actor self) { UnreserveAll(); }

		void INotifyKilled.Killed(Actor self, AttackInfo e) { UnreserveAll(); }

		void INotifyActorDisposing.Disposing(Actor self) { preventLink = true; UnreserveAll(); }
	}
}
