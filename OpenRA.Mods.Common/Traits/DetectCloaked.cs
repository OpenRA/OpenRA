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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can reveal Cloak actors in a specified range.")]
	public class DetectCloakedInfo : ITraitInfo
	{
		[Desc("Specific cloak classifications I can reveal.")]
		public readonly string[] CloakTypes = { "Cloak" };

		[Desc("Measured in cells.")]
		public readonly int Range = 5;

		public object Create(ActorInitializer init) { return new DetectCloaked(this); }
	}

	public class DetectCloaked
	{
		public readonly DetectCloakedInfo Info;

		public DetectCloaked(DetectCloakedInfo info)
		{
			Info = info;
		}
	}
}
