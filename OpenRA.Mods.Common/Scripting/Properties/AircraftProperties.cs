#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
		readonly bool isPlane;

		public AircraftProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			isPlane = self.Trait<Aircraft>().IsPlane;
		}

		[ScriptActorPropertyActivity]
		[Desc("Fly within the cell grid.")]
		public void Move(CPos cell)
		{
			if (isPlane)
				Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell)));
			else
				Self.QueueActivity(new HeliFly(Self, Target.FromCell(Self.World, cell)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Return to the base, which is either the airfield given, or an auto-selected one otherwise.")]
		public void ReturnToBase(Actor airfield = null)
		{
			if (isPlane)
				Self.QueueActivity(new ReturnToBase(Self, airfield));
			else
				Self.QueueActivity(new HeliReturnToBase(Self));
		}

		[ScriptActorPropertyActivity]
		[Desc("Queues a landing activity on the specififed actor.")]
		public void Land(Actor landOn)
		{
			Self.QueueActivity(new Land(Self, Target.FromActor(landOn)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Starts the resupplying activity when being on a host building.")]
		public void Resupply()
		{
			Self.QueueActivity(new ResupplyAircraft(Self));
		}
	}
}