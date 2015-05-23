#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
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
		public void LoadPassenger(Actor a) { cargo.Load(self, a); }

		[Desc("Remove the first actor from the transport.  This actor is not added to the world.")]
		public Actor UnloadPassenger() { return cargo.Unload(self); }

		[ScriptActorPropertyActivity]
		[Desc("Command transport to unload passengers.")]
		public void UnloadPassengers()
		{
			self.QueueActivity(new UnloadCargo(self, true));
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
			self.QueueActivity(new Fly(self, Target.FromCell(self.World, cell)));
			self.QueueActivity(new FlyOffMap());
			self.QueueActivity(new RemoveSelf());
		}
	}
}