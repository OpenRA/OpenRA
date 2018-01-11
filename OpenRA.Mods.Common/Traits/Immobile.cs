#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class ImmobileInfo : ITraitInfo, IOccupySpaceInfo
	{
		public readonly bool OccupiesSpace = true;
		public object Create(ActorInitializer init) { return new Immobile(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			var occupied = OccupiesSpace ? new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } } :
				new Dictionary<CPos, SubCell>();

			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }
	}

	class Immobile : IOccupySpace, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		[Sync] readonly CPos location;
		[Sync] readonly WPos position;
		readonly Pair<CPos, SubCell>[] occupied;

		public Immobile(ActorInitializer init, ImmobileInfo info)
		{
			location = init.Get<LocationInit, CPos>();
			position = init.World.Map.CenterOfCell(location);

			if (info.OccupiesSpace)
				occupied = new[] { Pair.New(TopLeft, SubCell.FullCell) };
			else
				occupied = new Pair<CPos, SubCell>[0];
		}

		public CPos TopLeft { get { return location; } }
		public WPos CenterPosition { get { return position; } }
		public Pair<CPos, SubCell>[] OccupiedCells() { return occupied; }

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}
	}
}
