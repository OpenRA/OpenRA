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
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RepairsUnitsInfo : ITraitInfo, Requires<DockManagerInfo>
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
		Actor self;
		DockManager dockManager;
		RallyPoint rallyPoint;

		public RepairsUnits(Actor self, RepairsUnitsInfo info)
		{
			this.self = self;
			dockManager = self.Trait<DockManager>();
			rallyPoint = self.TraitOrDefault<RallyPoint>();
		}

		void IAcceptDock.OnUndock(Actor client, Dock dock, Activity parameters)
		{
			client.SetTargetLine(Target.FromCell(self.World, rallyPoint.Location), Color.Green);
			if (rallyPoint != null)
				client.QueueActivity(new AttackMoveActivity(client, client.Trait<IMove>().MoveTo(rallyPoint.Location, 2)));
		}

		void IAcceptDock.QueueDockActivity(Actor client, Dock dock, Activity parameters)
		{
			var air = client.TraitOrDefault<Aircraft>();
			if (air != null)
			{
				client.QueueActivity(new ResupplyAircraft(client));
				return;
			}

			client.Trait<Repairable>().AfterReachActivities(client, self, dock);
		}

		Activity IAcceptDock.ApproachDockActivity(Actor client, Dock dock, Activity parameters)
		{
			var air = client.TraitOrDefault<Aircraft>();
			if (air != null)
			{
				if (air.IsPlane)
				{
					// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
					System.Diagnostics.Debug.Assert(parameters is ReturnToBase, "Wrong parameter for landing");
					var rtb = parameters as ReturnToBase;
					return rtb.LandingProcedure(client, dock);
				}

				var angle = dock.Info.DockAngle;
				if (angle < 0)
					angle = client.Info.TraitInfo<AircraftInfo>().InitialFacing;

				return ActivityUtils.SequenceActivities(
					new HeliFly(client, Target.FromPos(dock.CenterPosition)),
					new Turn(client, angle),
					new HeliLand(client, false));
			}

			return client.Trait<IMove>().MoveTo(dock.Location, self);
		}
	}
}
