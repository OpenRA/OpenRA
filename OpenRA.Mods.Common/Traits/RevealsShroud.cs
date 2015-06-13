#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RevealsShroudInfo : ITraitInfo
	{
		public readonly WRange Range = WRange.Zero;

		public virtual object Create(ActorInitializer init) { return new RevealsShroud(init.Self, this); }
	}

	public class RevealsShroud : ITick, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		static readonly CPos[] NoCells = { };

		readonly RevealsShroudInfo info;
		readonly bool lobbyShroudFogDisabled;
		[Sync] CPos cachedLocation;
		[Sync] bool cachedDisabled;

		protected Action<Player, CPos[]> addCellsToPlayerShroud;
		protected Action<Player> removeCellsFromPlayerShroud;
		protected Func<bool> isDisabled;

		public RevealsShroud(Actor self, RevealsShroudInfo info)
		{
			this.info = info;
			lobbyShroudFogDisabled = !self.World.LobbyInfo.GlobalSettings.Shroud && !self.World.LobbyInfo.GlobalSettings.Fog;

			addCellsToPlayerShroud = (p, c) => p.Shroud.AddVisibility(self, c);
			removeCellsFromPlayerShroud = p => p.Shroud.RemoveVisibility(self);
			isDisabled = () => false;
		}

		CPos[] Cells(Actor self)
		{
			var range = Range;
			if (range == WRange.Zero)
				return NoCells;

			return Shroud.GetVisOrigins(self)
				.SelectMany(o => Shroud.FindVisibleTiles(self.World, o, range))
				.Distinct().ToArray();
		}

		public void Tick(Actor self)
		{
			if (lobbyShroudFogDisabled || !self.IsInWorld)
				return;

			var location = self.Location;
			var disabled = isDisabled();
			if (cachedLocation == location && cachedDisabled == disabled)
				return;

			cachedLocation = location;
			cachedDisabled = disabled;

			var cells = Cells(self);
			foreach (var p in self.World.Players)
			{
				removeCellsFromPlayerShroud(p);
				addCellsToPlayerShroud(p, cells);
			}
		}

		public void AddedToWorld(Actor self)
		{
			cachedLocation = self.Location;
			cachedDisabled = isDisabled();

			var cells = Cells(self);
			foreach (var p in self.World.Players)
				addCellsToPlayerShroud(p, cells);
		}

		public void RemovedFromWorld(Actor self)
		{
			foreach (var p in self.World.Players)
				removeCellsFromPlayerShroud(p);
		}

		public WRange Range { get { return cachedDisabled ? WRange.Zero : info.Range; } }
	}
}
