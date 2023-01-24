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
	public sealed class DockType { DockType() { } }

	[Desc("A generic dock that services DockClients.")]
	public class DockHostInfo : ConditionalTraitInfo, IDockHostInfo
	{
		[Desc("Docking type.")]
		public readonly BitSet<DockType> Type;

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

	public class DockHost : ConditionalTrait<DockHostInfo>, IDockHost, IDockHostDrag, ITick, INotifySold, INotifyCapture, INotifyOwnerChanged, ISync, INotifyKilled, INotifyActorDisposing
	{
		readonly Actor self;

		public BitSet<DockType> GetDockType => Info.Type;
		public bool IsEnabledAndInWorld => !preventDock && !IsTraitDisabled && !self.IsDead && self.IsInWorld;
		public int ReservationCount => ReservedDockClients.Count;
		public bool CanBeReserved => ReservationCount < Info.MaxQueueLength;
		protected readonly List<DockClientManager> ReservedDockClients = new();

		public WPos DockPosition => self.CenterPosition + Info.DockOffset;
		public int DockWait => Info.DockWait;
		public WAngle DockAngle => Info.DockAngle;

		bool IDockHostDrag.IsDragRequired => Info.IsDragRequired;
		WVec IDockHostDrag.DragOffset => Info.DragOffset;
		int IDockHostDrag.DragLength => Info.DragLength;

		[Sync]
		bool preventDock = false;

		[Sync]
		protected Actor dockedClientActor = null;
		protected DockClientManager dockedClient = null;

		public DockHost(Actor self, DockHostInfo info)
			: base(info)
		{
			this.self = self;
		}

		public virtual bool IsDockingPossible(Actor clientActor, IDockClient client, bool ignoreReservations = false)
		{
			return !IsTraitDisabled && (ignoreReservations || CanBeReserved || ReservedDockClients.Contains(client.DockClientManager));
		}

		public virtual bool Reserve(Actor self, DockClientManager client)
		{
			if (CanBeReserved && !ReservedDockClients.Contains(client))
			{
				ReservedDockClients.Add(client);
				client.ReserveHost(self, this);
				return true;
			}

			return false;
		}

		public virtual void UnreserveAll()
		{
			while (ReservedDockClients.Count > 0)
				Unreserve(ReservedDockClients[0]);
		}

		public virtual void Unreserve(DockClientManager client)
		{
			if (ReservedDockClients.Contains(client))
			{
				ReservedDockClients.Remove(client);
				client.UnreserveHost();
			}
		}

		public virtual void OnDockStarted(Actor self, Actor clientActor, DockClientManager client)
		{
			dockedClientActor = clientActor;
			dockedClient = client;
		}

		public virtual void OnDockCompleted(Actor self, Actor clientActor, DockClientManager client)
		{
			dockedClientActor = null;
			dockedClient = null;
		}

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			// Client was killed during docking.
			if (dockedClientActor != null && (dockedClientActor.IsDead || !dockedClientActor.IsInWorld))
				OnDockCompleted(self, dockedClientActor, dockedClient);
		}

		public virtual bool QueueMoveActivity(Activity moveToDockActivity, Actor self, Actor clientActor, DockClientManager client)
		{
			var move = clientActor.Trait<IMove>();

			// Make sure the actor is at dock, at correct facing, and aircraft are landed.
			// Mobile cannot freely move in WPos, so when we calculate close enough we convert to CPos.
			if ((move is Mobile ? clientActor.Location != clientActor.World.Map.CellContaining(DockPosition) : clientActor.CenterPosition != DockPosition)
				|| move is not IFacing facing || facing.Facing != DockAngle)
			{
				moveToDockActivity.QueueChild(move.MoveOntoTarget(clientActor, Target.FromActor(self), DockPosition - self.CenterPosition, DockAngle));
				return true;
			}

			return false;
		}

		public virtual void QueueDockActivity(Activity moveToDockActivity, Actor self, Actor clientActor, DockClientManager client)
		{
			moveToDockActivity.QueueChild(new GenericDockSequence(clientActor, client, self, this));
		}

		protected override void TraitDisabled(Actor self) { UnreserveAll(); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UnreserveAll(); }

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			// Steal any docked unit too.
			if (dockedClientActor != null && !dockedClientActor.IsDead && dockedClientActor.IsInWorld)
			{
				dockedClientActor.ChangeOwner(newOwner);

				// On capture OnOwnerChanged event is called first, so we need to re-reserve.
				dockedClient.ReserveHost(self, this);
			}
		}

		void INotifySold.Selling(Actor self) { preventDock = true; }

		void INotifySold.Sold(Actor self) { UnreserveAll(); }

		void INotifyKilled.Killed(Actor self, AttackInfo e) { UnreserveAll(); }

		void INotifyActorDisposing.Disposing(Actor self) { preventDock = true; UnreserveAll(); }
	}
}
