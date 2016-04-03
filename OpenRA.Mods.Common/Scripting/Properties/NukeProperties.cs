#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class NukeProperties : ScriptActorProperties, Requires<NukePowerInfo>
	{
		readonly Actor self;
		readonly NukePower power;

		public NukeProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			this.self = self;
			power = self.Trait<NukePower>();
		}

		[Desc("Activates the NukePower of the actor, launching the nuke on the given 'targetLocation'.",
			"'beaconOwner' specifies which player owns the beacon display on the target location.")]
		public void Nuke(CPos targetLocation, Player beaconOwner)
		{
			power.ActivateNuke(self, targetLocation, beaconOwner);
		}
	}
}
