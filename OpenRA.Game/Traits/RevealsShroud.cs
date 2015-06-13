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

		public object Create(ActorInitializer init) { return new RevealsShroud(init.Self, this); }
	}

	public class RevealsShroud : ITick, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly RevealsShroudInfo info;
		readonly bool lobbyShroudFogDisabled;
		[Sync] CPos cachedLocation;

		public RevealsShroud(Actor self, RevealsShroudInfo info)
		{
			this.info = info;
			lobbyShroudFogDisabled = !self.World.LobbyInfo.GlobalSettings.Shroud && !self.World.LobbyInfo.GlobalSettings.Fog;
		}

		public void Tick(Actor self)
		{
			if (lobbyShroudFogDisabled)
				return;

			if (cachedLocation != self.Location)
			{
				cachedLocation = self.Location;

				CPos[] visible = null;
				foreach (var p in self.World.Players)
				{
					p.Shroud.RemoveVisibility(self);
					p.Shroud.AddVisibility(self, ref visible);
				}
			}
		}

		public void AddedToWorld(Actor self)
		{
			CPos[] visible = null;
			foreach (var p in self.World.Players)
				p.Shroud.AddVisibility(self, ref visible);
		}

		public void RemovedFromWorld(Actor self)
		{
			foreach (var p in self.World.Players)
				p.Shroud.RemoveVisibility(self);
		}

		public WRange Range { get { return info.Range; } }
	}
}
