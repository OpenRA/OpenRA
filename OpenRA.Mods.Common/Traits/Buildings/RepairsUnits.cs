#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Traits;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.Traits
{
	public class RepairsUnitsInfo : ITraitInfo, IAcceptDockInfo, Requires<DockManagerInfo>
	{
		[Desc("Cost in % of the unit value to fully repair the unit.")]
		public readonly int ValuePercentage = 20;
		public readonly int HpPerStep = 10;

		[Desc("Time (in ticks) between two repair steps.")]
		public readonly int Interval = 24;

		[Desc("The sound played when starting to repair a unit.")]
		public readonly string StartRepairingNotification = "Repairing";

		[Desc("The sound played when repairing a unit is done.")]
		public readonly string FinishRepairingNotification = null;

		[Desc("Experience gained by the player owning this actor for repairing an allied unit.")]
		public readonly int PlayerExperience = 0;

		public object Create(ActorInitializer init) { return new RepairsUnits(init.Self, this); }
	}

	public class RepairsUnits : IAcceptDock
	{
		RepairsUnitsInfo info;
		Actor self;
		DockManager dockManager;
		RallyPoint rallyPoint;

		public RepairsUnits(Actor self, RepairsUnitsInfo info)
		{
			this.self = self;
			this.info = info;
			dockManager = self.Trait<DockManager>();
			rallyPoint = self.TraitOrDefault<RallyPoint>();
		}

		// Unused. Repairable.cs takes care of this.
		bool IAcceptDock.AllowDocking
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Unused.
		IEnumerable<CPos> IAcceptDock.DockLocations
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Nothing to do with resources
		bool IAcceptDock.CanGiveResource(int amount)
		{
			throw new NotImplementedException();
		}

		// Nothing to do with resources
		void IAcceptDock.GiveResource(int amount)
		{
			throw new NotImplementedException();
		}

		void IAcceptDock.OnDock(Actor client, Dock dock)
		{
			dockManager.OnArrivalCheck(client, dock);
		}

		void IAcceptDock.OnUndock(Actor client, Dock dock)
		{
			client.SetTargetLine(Target.FromCell(self.World, rallyPoint.Location), Color.Green);
			dockManager.ReleaseAndNext(client, dock);
		}

		void IAcceptDock.QueueOnDockActivity(Actor client, Dock dock)
		{
			client.Trait<Repairable>().AfterReachActivities(client, self, dock);
		}


		void IAcceptDock.QueueUndockActivity(Actor client, Dock dock)
		{
			if (rallyPoint != null)
			{
				client.QueueActivity(new AttackMoveActivity(client, client.Trait<IMove>().MoveTo(rallyPoint.Location, 2)));
			}
		}

		void IAcceptDock.ReserveDock(Actor client)
		{
			dockManager.ReserveDock(self, client);
		}
	}
}
