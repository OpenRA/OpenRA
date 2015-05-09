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
	[Desc("Generates shroud, prevents auto-target. Overridden by CreatesDisruptionField if fog is enabled.")]
	public class CreatesShroudInfo : ITraitInfo
	{
		public readonly WRange Range = WRange.Zero;

		public object Create(ActorInitializer init) { return new CreatesShroud(init.Self, this); }
	}

	public class CreatesShroud : ITick, ISync
	{
		public WRange Range { get { return cachedDisabled ? WRange.Zero : info.Range; } }
		readonly CreatesShroudInfo info;
		readonly bool lobbyShroudDisabled;
		[Sync] CPos cachedLocation;
		[Sync] bool cachedDisabled;

		public CreatesShroud(Actor self, CreatesShroudInfo info)
		{
			this.info = info;
			lobbyShroudDisabled = !self.World.LobbyInfo.GlobalSettings.Shroud;
		}

		public void Tick(Actor self)
		{
			if (lobbyShroudDisabled)
				return;

			var disabled = self.IsDisabled();
			if (cachedLocation != self.Location || cachedDisabled != disabled)
			{
				cachedLocation = self.Location;
				cachedDisabled = disabled;
				Shroud.UpdateShroudGenerator(self.World.Players.Select(p => p.Shroud), self);
			}
		}
	}
}