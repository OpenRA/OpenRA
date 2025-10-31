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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class CarryableHarvesterInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CarryableHarvester(); }
	}

	public class CarryableHarvester : INotifyCreated, INotifyHarvestAction, INotifyDockClientMoving
	{
		ICallForTransport[] transports;

		void INotifyCreated.Created(Actor self)
		{
			transports = self.TraitsImplementing<ICallForTransport>().ToArray();
		}

		void INotifyHarvestAction.MovingToResources(Actor self, CPos targetCell)
		{
			foreach (var t in transports)
				t.RequestTransport(self, targetCell);
		}

		void INotifyHarvestAction.MovementCancelled(Actor self)
		{
			foreach (var t in transports)
				t.MovementCancelled(self);
		}

		void INotifyDockClientMoving.MovingToDock(Actor self, Actor hostActor, IDockHost host)
		{
			foreach (var t in transports)
				t.RequestTransport(self, self.World.Map.CellContaining(host.DockPosition));
		}

		void INotifyDockClientMoving.MovementCancelled(Actor self)
		{
			foreach (var t in transports)
				t.MovementCancelled(self);
		}

		void INotifyHarvestAction.Harvested(Actor self, string resourceType) { }
	}
}
