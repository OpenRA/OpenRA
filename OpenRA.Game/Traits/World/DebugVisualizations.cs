#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	[Desc("Enables visualization commands. Attach this to the world actor.")]
	public class DebugVisualizationsInfo : TraitInfo<DebugVisualizations> { }

	public class DebugVisualizations
	{
		public bool CombatGeometry;
		public bool RenderGeometry;
		public bool ScreenMap;
		public bool DepthBuffer;
		public bool ActorTags;
	}
}
