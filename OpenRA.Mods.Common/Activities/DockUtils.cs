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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Drawing;

namespace OpenRA.Mods.Common.Activities
{
	public static class DockUtils
	{
		public static Activity GenericApproachDockActivities(Actor host, Actor client, Dock dock,
			Activity requester, bool goThroughHost=false)
		{
			var air = client.TraitOrDefault<Aircraft>();
			if (air != null)
			{
				if (air.IsPlane)
				{
					// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
					System.Diagnostics.Debug.Assert(requester is ReturnToBase, "Wrong parameter for landing");
					var rtb = requester as ReturnToBase;
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

			if (goThroughHost)
				return client.Trait<IMove>().MoveTo(dock.Location, host);
			else
				return client.Trait<IMove>().MoveTo(dock.Location, 0);
		}

		public static Activity GenericFollowRallyPointActivities(Actor host, Actor client, Dock dock, Activity requester)
		{
			var rp = host.Trait<RallyPoint>();

			var air = client.TraitOrDefault<Aircraft>();
			if (air != null)
			{
				if (air.IsPlane)
				{
					// ResupplyAircraft handles this.
					// Take off and move to RP.
					return ActivityUtils.SequenceActivities(
						new Fly(client, Target.FromCell(client.World, rp.Location)),
						new FlyCircle(client));
				}

				// Don't make helis do attack move, it will waste ammo.
				return client.Trait<IMove>().MoveTo(rp.Location, 2);
			}

			client.SetTargetLine(Target.FromCell(host.World, rp.Location), Color.Green);
			return new AttackMoveActivity(client, client.Trait<IMove>().MoveTo(rp.Location, 2));
		}
	}
}
