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
	[Desc("Removes frozen actors and satellite icons from fog, prevents auto-target. Requires fog enabled.")]
	public class CreatesDisruptionFieldInfo : ITraitInfo
	{
		public readonly WRange Range = WRange.Zero;

		public object Create(ActorInitializer init) { return new CreatesDisruptionField(init.Self, this); }
	}

	public class CreatesDisruptionField : ITick, ISync
	{
		public WRange Range { get { return cachedDisabled ? WRange.Zero : info.Range; } }
		readonly CreatesDisruptionFieldInfo info;
		readonly bool lobbyFogDisabled;
		[Sync] CPos cachedLocation;
		[Sync] bool cachedDisabled;

		public CreatesDisruptionField(Actor self, CreatesDisruptionFieldInfo info)
		{
			this.info = info;
			lobbyFogDisabled = !self.World.LobbyInfo.GlobalSettings.Fog;
		}

		public void Tick(Actor self)
		{
			if (lobbyFogDisabled)
				return;

			var disabled = self.IsDisabled();
			if (cachedLocation != self.Location || cachedDisabled != disabled)
			{
				cachedLocation = self.Location;
				cachedDisabled = disabled;
				Shroud.UpdateDisruptionGenerator(self.World.Players.Select(p => p.Shroud), self);
			}
		}
	}
}