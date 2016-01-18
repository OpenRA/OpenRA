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
	}
}