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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("LineBuild actors attach to LineBuildNodes.")]
	public class LineBuildNodeInfo : TraitInfo<LineBuildNode>
	{
		[Desc("This actor is of LineBuild 'NodeType'...")]
		public readonly HashSet<string> Types = new HashSet<string> { "wall" };

		[Desc("Cells (outside the footprint) that contain cells that can connect to this actor.")]
		public readonly CVec[] Connections = new[] { new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0), new CVec(0, -1) };
	}

	public class LineBuildNode { }
}
