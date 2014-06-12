#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Buildings
{
	[Desc("LineBuild actors attach to LineBuildNodes.")]
	public class LineBuildNodeInfo : TraitInfo<LineBuildNode>
	{
		[Desc("This actor is of LineBuild 'NodeType'...")]
		public readonly string[] Types = { "wall" };
	}

	public class LineBuildNode {}
}
