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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ImmobileInfo : ITraitInfo, IOccupySpaceInfo
	{
		public readonly bool OccupiesSpace = true;
		public object Create(ActorInitializer init) { return new Immobile(init, this); }
	}

	class Immobile : IOccupySpace, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		[Sync] readonly CPos location;
		readonly IEnumerable<Pair<CPos, SubCell>> occupied;

		public Immobile(ActorInitializer init, ImmobileInfo info)
		{
			this.location = init.Get<LocationInit, CPos>();

			if (info.OccupiesSpace)
				occupied = new [] { Pair.New(TopLeft, SubCell.FullCell) };
			else
				occupied = new Pair<CPos, SubCell>[0];
		}

		public CPos TopLeft { get { return location; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return occupied; }
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
