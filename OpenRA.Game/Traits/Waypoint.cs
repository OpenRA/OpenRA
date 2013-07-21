#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	class WaypointInfo : ITraitInfo, IOccupySpaceInfo
	{
		public object Create( ActorInitializer init ) { return new Waypoint( init ); }
	}

	class Waypoint : IOccupySpace, ISync
	{
		[Sync] CPos location;

		public Waypoint(ActorInitializer init)
		{
			this.location = init.Get<LocationInit, CPos>();
		}

		public CPos TopLeft { get { return location; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield break; }
		public WPos CenterPosition { get { return location.CenterPosition; } }
	}
}
