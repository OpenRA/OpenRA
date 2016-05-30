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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class AirstrikeProperties : ScriptActorProperties, Requires<AirstrikePowerInfo>
	{
		readonly AirstrikePower ap;

		public AirstrikeProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			ap = self.TraitsImplementing<AirstrikePower>().First();
		}

		[Desc("Activate the actor's Airstrike Power.")]
		public void SendAirstrike(WPos target, bool randomize = true, int facing = 0)
		{
			ap.SendAirstrike(Self, target, randomize, facing);
		}

		[Desc("Activate the actor's Airstrike Power.")]
		public void SendAirstrikeFrom(CPos from, CPos to)
		{
			var i = Self.World.Map.CenterOfCell(from);
			var j = Self.World.Map.CenterOfCell(to);

			ap.SendAirstrike(Self, j, false, (i - j).Yaw.Facing);
		}
	}
}