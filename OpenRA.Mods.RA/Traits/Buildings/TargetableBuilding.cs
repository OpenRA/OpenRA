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
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class TargetableBuildingInfo : ITraitInfo, ITargetableInfo, Requires<BuildingInfo>
	{
		public readonly string[] TargetTypes = { };
		public string[] GetTargetTypes() { return TargetTypes; }

		public bool RequiresForceFire = false;

		public object Create(ActorInitializer init) { return new TargetableBuilding(init.self, this); }
	}

	public class TargetableBuilding : ITargetable
	{
		readonly TargetableBuildingInfo info;
		readonly Building building;

		public TargetableBuilding(Actor self, TargetableBuildingInfo info)
		{
			this.info = info;
			building = self.Trait<Building>();
		}

		public string[] TargetTypes { get { return info.TargetTypes; } }
		public bool TargetableBy(Actor self, Actor byActor) { return true; }

		public IEnumerable<WPos> TargetablePositions(Actor self)
		{
			return building.OccupiedCells().Select(c => self.World.Map.CenterOfCell(c.First));
		}

		public bool RequiresForceFire { get { return info.RequiresForceFire; } }
	}
}
