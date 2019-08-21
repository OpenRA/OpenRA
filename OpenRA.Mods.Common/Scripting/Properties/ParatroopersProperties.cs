#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public Actor[] ActivateParatroopers(WPos target, int facing = -1)
		{
			var actors = pp.SendParatroopers(Self, target, facing);
			return actors.First;
		}

		[Desc("Activate the actor's Paratroopers Power. Returns the dropped units. DEPRECATED! Will be removed.")]
		public Actor[] SendParatroopers(WPos target, bool randomize = true, int facing = 0)
		{
			Game.Debug("SendParatroopers is deprecated. Use ActivateParatroopers instead.");
			var actors = pp.SendParatroopers(Self, target, randomize ? -1 : facing);
			return actors.Second;
		}

		[Desc("Activate the actor's Paratroopers Power. Returns the dropped units. DEPRECATED! Will be removed.")]
		public Actor[] SendParatroopersFrom(CPos from, CPos to)
		{
			Game.Debug("SendParatroopersFrom is deprecated. Use ActivateParatroopers instead.");
			var i = Self.World.Map.CenterOfCell(from);
			var j = Self.World.Map.CenterOfCell(to);

			var actors = pp.SendParatroopers(Self, j, (i - j).Yaw.Facing);
			return actors.Second;
		}
	}
}
