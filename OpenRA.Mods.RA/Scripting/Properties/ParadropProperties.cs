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
using OpenRA.Mods.RA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Paradrop")]
	public class ParadropProperties : ScriptActorProperties, Requires<CargoInfo>, Requires<ParaDropInfo>
	{
		readonly ParaDrop paradrop;

		public ParadropProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			paradrop = self.Trait<ParaDrop>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Command transport to paradrop passengers near the target cell.")]
		public void Paradrop(CPos cell)
		{
			paradrop.SetLZ(cell, true);
			Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell), Self.Trait<Plane>()));
			Self.QueueActivity(new FlyOffMap(Self));
			Self.QueueActivity(new RemoveSelf());
		}
	}
}