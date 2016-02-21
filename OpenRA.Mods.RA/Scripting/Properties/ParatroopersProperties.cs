#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
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

		[Desc("Activate the actor's Paratroopers Power. Returns the dropped units.")]
		public Actor[] SendParatroopers(WPos target, bool randomize = true, int facing = 0)
		{
			return pp.SendParatroopers(Self, target, randomize, facing);
		}
	}
}