#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	sealed class ImmobileInfo : TraitInfo, IOccupySpaceInfo
	{
		public readonly bool OccupiesSpace = true;
		public override object Create(ActorInitializer init) { return new Immobile(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return OccupiesSpace ? new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } } :
				new Dictionary<CPos, SubCell>();
		}

		bool IOccupySpaceInfo.SharesCell => false;
	}

	sealed class Immobile : IOccupySpace, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly (CPos, SubCell)[] occupied;

		public Immobile(ActorInitializer init, ImmobileInfo info)
		{
			TopLeft = init.GetValue<LocationInit, CPos>();
			CenterPosition = init.World.Map.CenterOfCell(TopLeft);

			if (info.OccupiesSpace)
				occupied = new[] { (TopLeft, SubCell.FullCell) };
			else
				occupied = Array.Empty<(CPos, SubCell)>();
		}

		[Sync]
		public CPos TopLeft { get; }
		[Sync]
		public WPos CenterPosition { get; }
		public (CPos, SubCell)[] OccupiedCells() { return occupied; }

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
