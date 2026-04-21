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

namespace OpenRA.Mods.Common.Traits.Radar
{
	[Desc("This actor enables the radar minimap.")]
	public class ProvidesRadarInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ProvidesRadar(this); }
	}

	public class ProvidesRadar : ConditionalTrait<ProvidesRadarInfo>
	{
		public ProvidesRadar(ProvidesRadarInfo info)
			: base(info) { }
	}
}
