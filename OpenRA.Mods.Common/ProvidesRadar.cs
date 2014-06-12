#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class ProvidesRadarInfo : TraitInfo<ProvidesRadar> { }

	public class ProvidesRadar : ITick
	{
		public bool IsActive { get; private set; }

		public void Tick(Actor self) { IsActive = UpdateActive(self); }

		static bool UpdateActive(Actor self)
		{
			// Check if powered
			if (self.IsDisabled()) return false;

			var isJammed = self.World.ActorsWithTrait<JamsRadar>().Any(a => a.Actor.Owner.Stances[self.Owner] != Stance.Ally
				&& (self.Location - a.Actor.Location).Length <= a.Actor.Info.Traits.Get<JamsRadarInfo>().Range);

			return !isJammed;
		}
	}

	class JamsRadarInfo : TraitInfo<JamsRadar> { public readonly int Range = 0; }

	class JamsRadar { }
}
