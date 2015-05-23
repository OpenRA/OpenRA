#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits
{
	public class RevealsShroudInfo : ITraitInfo
	{
		public readonly WRange Range = WRange.Zero;
		public object Create(ActorInitializer init) { return new RevealsShroud(this); }
	}

	public class RevealsShroud : ITick, ISync
	{
		RevealsShroudInfo info;
		[Sync] CPos cachedLocation;

		public RevealsShroud(RevealsShroudInfo info)
		{
			this.info = info;
		}

		public void Tick(Actor self)
		{
			if (cachedLocation != self.Location)
			{
				cachedLocation = self.Location;
				Shroud.UpdateVisibility(self.World.Players.Select(p => p.Shroud), self);
			}
		}

		public WRange Range { get { return info.Range; } }
	}
}
