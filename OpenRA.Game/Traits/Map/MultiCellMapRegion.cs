#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Scripting;

namespace OpenRA.Traits
{
	[Desc("Tracks actors within a number of map regions.", "This goes on the world actor.")]
	public class MultiCellMapRegionInfo : ITraitInfo, Requires<MovementAnnouncerInfo>
	{
		[Desc("Name of this region.", "Should be unique.")]
		public readonly string Name = "Unnamed";

		[Desc("The cells (CPos) that makes up the region.")]
		public readonly CPos[] Cells;

		public object Create(ActorInitializer init) { return new MultiCellMapRegion(init.world, this); }
	}

	public class MultiCellMapRegion : MovementListener
	{
		public readonly MultiCellMapRegionInfo Info;
		public readonly List<Actor> Occupants = new List<Actor>();
		readonly World world;

		public MultiCellMapRegion(World world, MultiCellMapRegionInfo info)
			: base(world)
		{
			Info = info;
			this.world = world;
		}

		public bool PositionWithinRegion(WPos pos)
		{
			var checkCell = world.Map.CellContaining(pos);
			foreach (var cell in Info.Cells)
				if (checkCell == cell)
					return true;

			return false;
		}

		public override void PositionMovementAnnouncement(HashSet<Actor> movedActors)
		{
			foreach (var actor in movedActors)
			{
				var isInRegion = PositionWithinRegion(actor.CenterPosition);
				if (isInRegion &&
					!Occupants.Contains(actor))
				{
					Occupants.Add(actor);
					foreach (var inrt in world.Actors.SelectMany(a => a.TraitsImplementing<INotifyRegionTrigger>()))
						inrt.EnteredRegion(actor.Owner, actor, Info.Name);
				}

				if (!isInRegion &&
					Occupants.Contains(actor))
				{
					Occupants.Remove(actor);
					foreach (var inrt in world.Actors.SelectMany(a => a.TraitsImplementing<INotifyRegionTrigger>()))
						inrt.LeftRegion(actor.Owner, actor, Info.Name);
				}
			}
		}
	}
}
