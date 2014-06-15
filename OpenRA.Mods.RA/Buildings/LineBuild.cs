#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	[Desc("Place the second actor in line to build more of the same at once (used for walls).")]
	public class LineBuildInfo : TraitInfo<LineBuild>
	{
		[Desc("The maximum allowed length of the line.")]
		public readonly int Range = 5;

		[Desc("LineBuildNode 'Types' to attach to.")]
		public readonly string[] NodeTypes = { "wall" };
	}

	public class LineBuild {}
}
