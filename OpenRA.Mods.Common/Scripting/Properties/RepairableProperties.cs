#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Repairable")]
	public class RepairableProperties : ScriptActorProperties, Requires<RepairableInfo>
	{
		//// readonly Repaira cargo;

		public RepairableProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			// cargo = self.Trait<Cargo>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Command the actor to get fixed at the target repairer actor.")]
		public void RepairAt(Actor host)
		{
			Self.QueueActivity(new Repair(Self, host, new WDist(512)));
		}
	}
}