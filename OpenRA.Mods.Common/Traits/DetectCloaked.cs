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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can reveal Cloak actors in a specified range.")]
	public class DetectCloakedInfo : UpgradableTraitInfo
	{
		[Desc("Specific cloak classifications I can reveal.")]
		public readonly HashSet<string> CloakTypes = new HashSet<string> { "Cloak" };

		public readonly WDist Range = WDist.FromCells(5);

		public override object Create(ActorInitializer init) { return new DetectCloaked(this); }
	}

	public class DetectCloaked : UpgradableTrait<DetectCloakedInfo>
	{
		public DetectCloaked(DetectCloakedInfo info) : base(info) { }
	}
}
