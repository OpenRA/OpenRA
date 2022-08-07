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

	public class CarryableHarvester : INotifyCreated, INotifyHarvesterAction
	{
		ICallForTransport[] transports;

		void INotifyCreated.Created(Actor self)
		{
			transports = self.TraitsImplementing<ICallForTransport>().ToArray();
		}

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell)
		{
			foreach (var t in transports)
				t.RequestTransport(self, targetCell);
		}

		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor)
		{
			var iao = refineryActor.Trait<IAcceptResources>();
			var location = refineryActor.Location + iao.DeliveryOffset;
			foreach (var t in transports)
				t.RequestTransport(self, location);
		}

		void INotifyHarvesterAction.MovementCancelled(Actor self)
		{
			foreach (var t in transports)
				t.MovementCancelled(self);
		}

		void INotifyHarvesterAction.Harvested(Actor self, string resourceType) { }
	}
}
