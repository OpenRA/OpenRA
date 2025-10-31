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
using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Transports")]
	public class TransportProperties : ScriptActorProperties, Requires<CargoInfo>
	{
		readonly Cargo cargo;

		public TransportProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			cargo = self.Trait<Cargo>();
		}

		[Desc("Returns references to passengers inside the transport.")]
		public Actor[] Passengers => cargo.Passengers.ToArray();

		[Desc("Specifies whether transport has any passengers.")]
		public bool HasPassengers => cargo.Passengers.Any();

		[Desc("Specifies the amount of passengers.")]
		public int PassengerCount => cargo.Passengers.Count();

		[Desc("Teleport an existing actor inside this transport.")]
		public void LoadPassenger(Actor a)
		{
			if (!a.IsIdle)
				throw new LuaException("LoadPassenger requires the passenger to be idle.");

			cargo.Load(Self, a);
		}

		[Desc("Remove an existing actor (or first actor if none specified) from the transport.  This actor is not added to the world.")]
		public Actor UnloadPassenger(Actor a = null) { return cargo.Unload(Self, a); }

		[ScriptActorPropertyActivity]
		[Desc("Command transport to unload passengers.")]
		public void UnloadPassengers(CPos? cell = null, int unloadRange = 5)
		{
			if (cell.HasValue)
			{
				var destination = Target.FromCell(Self.World, cell.Value);
				Self.QueueActivity(new UnloadCargo(Self, destination, WDist.FromCells(unloadRange)));
			}
			else
				Self.QueueActivity(new UnloadCargo(Self, WDist.FromCells(unloadRange)));
		}
	}
}
