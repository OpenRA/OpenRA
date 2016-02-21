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

using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Movement")]
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
			harvester.ContinueHarvesting(Self);
		}
	}
}