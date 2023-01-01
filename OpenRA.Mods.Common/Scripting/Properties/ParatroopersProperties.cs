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
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class ParatroopersProperties : ScriptActorProperties, Requires<ParatroopersPowerInfo>
	{
		readonly ParatroopersPower pp;

		public ParatroopersProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			pp = self.TraitsImplementing<ParatroopersPower>().First();
		}

		[Desc("Activate the actor's Paratroopers Power. Returns the aircraft that will drop the reinforcements.")]
		public Actor[] TargetParatroopers(WPos target, WAngle? facing = null)
		{
			var actors = pp.SendParatroopers(Self, target, facing);
			return actors.Aircraft;
		}
	}
}
