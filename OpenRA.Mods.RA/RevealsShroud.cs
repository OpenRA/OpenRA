#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RevealsShroudInfo : TraitInfo<RevealsShroud> { }

	class RevealsShroud : ITick, IRevealShroud
	{
		int2 previousLocation;

		public void Tick(Actor self)
		{
			if (!self.IsIdle && previousLocation != self.Location)
			{
				previousLocation = self.Location;
				self.World.WorldActor.traits.Get<Shroud>().UpdateActor(self);
			}
		}
	}
}
