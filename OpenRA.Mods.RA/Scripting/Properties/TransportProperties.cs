#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
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

		[Desc("Specifies whether transport has any passengers.")]
		public bool HasPassengers { get { return cargo.Passengers.Any(); } }

		[Desc("Teleport an existing actor inside this transport.")]
		public void LoadPassenger(Actor a) { cargo.Load(Self, a); }

		[Desc("Remove the first actor from the transport.  This actor is not added to the world.")]
		public Actor UnloadPassenger() { return cargo.Unload(Self); }

		[ScriptActorPropertyActivity]
		[Desc("Command transport to unload passengers.")]
		public void UnloadPassengers()
		{
			Self.QueueActivity(new UnloadCargo(Self, true));
		}
	}

	[ScriptPropertyGroup("Transports")]
	public class ParadropPowers : ScriptActorProperties, Requires<CargoInfo>, Requires<ParaDropInfo>
	{
		readonly ParaDrop paradrop;

		public ParadropPowers(ScriptContext context, Actor self)
			: base(context, self)
		{
			paradrop = self.Trait<ParaDrop>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Command transport to paradrop passengers near the target cell.")]
		public void Paradrop(CPos cell)
		{
			paradrop.SetLZ(cell, true);
			Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell)));
			Self.QueueActivity(new FlyOffMap());
			Self.QueueActivity(new RemoveSelf());
		}
	}
}