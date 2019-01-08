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

using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can reveal Cloak actors in a specified range.")]
	public class DetectCloakedInfo : ConditionalTraitInfo
	{
		[Desc("Specific cloak classifications I can reveal.")]
		public readonly BitSet<CloakType> CloakTypes = new BitSet<CloakType>("Cloak");

		public readonly WDist Range = WDist.FromCells(5);

		public override object Create(ActorInitializer init) { return new DetectCloaked(this); }
	}

	public class DetectCloaked : ConditionalTrait<DetectCloakedInfo>
	{
		public DetectCloaked(DetectCloakedInfo info) : base(info) { }
	}
}
