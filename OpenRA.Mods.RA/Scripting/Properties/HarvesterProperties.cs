#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Scripting;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Harvester")]
	public class HarvesterProperties : ScriptActorProperties, Requires<HarvesterInfo>
	{
		readonly Harvester harvester;

		public HarvesterProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			harvester = self.Trait<Harvester>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Search for nearby resources and begin harvesting.")]
		public void FindResources()
		{
			harvester.ContinueHarvesting(self);
		}
	}
}