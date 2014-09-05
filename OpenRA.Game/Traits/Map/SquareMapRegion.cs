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
	[Desc("Tracks actors within a specific map region.", "This goes on the world actor.")]
	public class SquareMapRegionInfo : ITraitInfo, Requires<MovementAnnouncerInfo>
	{
		[Desc("Name of this region.", "Should be unique.")]
		public readonly string Name = "Unnamed";

		[Desc("Position of the upper North-West corner.")]
		public readonly WPos UpperNW = new WPos(0,0,0);

		[Desc("Position of the lower South-East corner.")]
		public readonly WPos LowerSE = new WPos(0,0,0);

		public object Create(ActorInitializer init) { return new SquareMapRegion(init.world, this); }
	}

	public class SquareMapRegion : MovementListener
	{
		public readonly SquareMapRegionInfo Info;
		public readonly WPos UpperNW;
		public readonly WPos LowerSE;
		public readonly List<Actor> Occupants = new List<Actor>();
		readonly World world;

		public SquareMapRegion(World world, SquareMapRegionInfo info)
			: base(world)
		{
			Info = info;
			UpperNW = Info.UpperNW;
			LowerSE = Info.LowerSE;
			this.world = world;
		}

		public bool PositionWithinRegion(WPos pos)
		{
			return (pos.X >= UpperNW.X && pos.Y >= UpperNW.Y && pos.Z <= UpperNW.Z &&
					pos.X <= LowerSE.X && pos.Y <= LowerSE.Y && pos.Z >= LowerSE.Z);
		}

		public override void PositionMovementAnnouncement(HashSet<Actor> movedActors)
		{
			foreach (var actor in movedActors)
			{
				if (PositionWithinRegion(actor.CenterPosition) &&
					!Occupants.Contains(actor))
				{
					Occupants.Add(actor);
					foreach (var inrt in world.Actors.SelectMany(a => a.TraitsImplementing<INotifyRegionTrigger>()))
						inrt.EnteredRegion(actor.Owner, actor, Info.Name);
				}

				if (!PositionWithinRegion(actor.CenterPosition) &&
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
