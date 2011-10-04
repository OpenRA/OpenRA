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
	class WaypointInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new Waypoint( init ); }
	}

	class Waypoint : IOccupySpace, ISync
	{
		[Sync] int2 location;

		public Waypoint(ActorInitializer init)
		{
			this.location = init.Get<LocationInit,int2>();
		}

		public int2 TopLeft { get { return location; } }

		public IEnumerable<Pair<int2, SubCell>> OccupiedCells() { yield break; }
		public int2 PxPosition { get { return Util.CenterOfCell( location ); } }
	}
}
