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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Place the second actor in line to build more of the same at once (used for walls).")]
	public class LineBuildInfo : TraitInfo<LineBuild>
	{
		[Desc("The maximum allowed length of the line.")]
		public readonly int Range = 5;

		[Desc("LineBuildNode 'Types' to attach to.")]
		public readonly HashSet<string> NodeTypes = new HashSet<string> { "wall" };
	}

	public class LineBuild { }
}
