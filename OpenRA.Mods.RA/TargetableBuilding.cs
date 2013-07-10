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
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TargetableBuildingInfo : ITraitInfo, Requires<BuildingInfo>
	{
		public readonly string[] TargetTypes = { };

		public object Create( ActorInitializer init ) { return new TargetableBuilding( this ); }
	}

	class TargetableBuilding : ITargetable
	{
		readonly TargetableBuildingInfo info;

		public TargetableBuilding( TargetableBuildingInfo info ) { this.info = info; }

		public string[] TargetTypes { get { return info.TargetTypes; } }
		public bool TargetableBy(Actor self, Actor byActor) { return true; }

		public IEnumerable<WPos> TargetablePositions(Actor self)
		{
			return self.Trait<Building>().OccupiedCells().Select(c => c.First.CenterPosition);
		}
	}
}
