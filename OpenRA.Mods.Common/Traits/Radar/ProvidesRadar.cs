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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Radar
{
	[Desc("This actor enables the radar minimap.")]
	public class ProvidesRadarInfo : TraitInfo<ProvidesRadar> { }

	public class ProvidesRadar : ITick
	{
		public bool IsActive { get; private set; }

		public void Tick(Actor self) { IsActive = UpdateActive(self); }

		static bool UpdateActive(Actor self)
		{
			// Check if powered
			if (self.IsDisabled()) return false;

			return self.World.ActorsWithTrait<JamsRadar>().All(a => a.Actor.Owner.Stances[self.Owner] == Stance.Ally
				|| (self.CenterPosition - a.Actor.CenterPosition).HorizontalLengthSquared
					> a.Actor.Info.TraitInfo<JamsRadarInfo>().Range.LengthSquared);
		}
	}
}
