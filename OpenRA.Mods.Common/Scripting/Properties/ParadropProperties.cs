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
	[ScriptPropertyGroup("Transports")]
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
			Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell)));
			Self.QueueActivity(new FlyOffMap(Self));
			Self.QueueActivity(new RemoveSelf());
		}
	}
}
