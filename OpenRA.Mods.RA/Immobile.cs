#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ImmobileInfo : ITraitInfo, IOccupySpaceInfo
	{
		public object Create(ActorInitializer init) { return new Immobile(init); }
	}

	class Immobile : IOccupySpace, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		[Sync] readonly CPos location;

		public Immobile(ActorInitializer init)
		{
			this.location = init.Get<LocationInit, CPos>();
		}

		public CPos TopLeft { get { return location; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield break; }
		public WPos CenterPosition { get { return location.CenterPosition; } }

		public void AddedToWorld(Actor self)
		{
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self, this);
			self.World.ScreenMap.Add(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self, this);
			self.World.ScreenMap.Remove(self);
		}
	}
}
