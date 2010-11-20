#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HuskInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new Husk( init ); }
	}

	class Husk : IOccupySpace, IFacing
	{
		[Sync]
		int2 location;
		
		[Sync]
		public int Facing { get; set; }
		public int ROT { get { return 0; } }
		public int InitialFacing { get { return 128; } }

		public Husk(ActorInitializer init)
		{
			this.location = init.Get<LocationInit,int2>();
			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : 128;
		}

		public int2 TopLeft { get { return location; } }

		public IEnumerable<int2> OccupiedCells() { yield return TopLeft; }
		public int2 PxPosition { get { return Util.CenterOfCell( location ); } }
	}
}
