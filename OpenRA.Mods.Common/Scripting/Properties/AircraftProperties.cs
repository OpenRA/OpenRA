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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class AircraftProperties : ScriptActorProperties, Requires<AircraftInfo>
	{
		readonly Aircraft aircraft;

		public AircraftProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			aircraft = self.Trait<Aircraft>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Fly within the cell grid.")]
		public void Move(CPos cell)
		{
			Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Return to the base, which is either the destination given, or an auto-selected one otherwise.")]
		public void ReturnToBase(Actor destination = null)
		{
			Self.QueueActivity(new ReturnToBase(Self, destination, true));
		}

		[ScriptActorPropertyActivity]
		[Desc("Queues a landing activity on the specified actor.")]
		public void Land(Actor landOn)
		{
			Self.QueueActivity(new Land(Self, Target.FromActor(landOn)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Starts the resupplying activity when being on a host building.")]
		public void Resupply()
		{
			var atLandAltitude = Self.World.Map.DistanceAboveTerrain(Self.CenterPosition) == aircraft.Info.LandAltitude;
			var host = aircraft.GetActorBelow();
			if (atLandAltitude && host != null)
				Self.QueueActivity(new Resupply(Self, host, WDist.Zero));
		}
	}
}
