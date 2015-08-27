#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TargetableBuildingInfo : ITraitInfo, ITargetableInfo, Requires<BuildingInfo>
	{
		[FieldLoader.Require]
		public readonly string[] TargetTypes = { };
		public string[] GetTargetTypes() { return TargetTypes; }

		public bool RequiresForceFire = false;

		public object Create(ActorInitializer init) { return new TargetableBuilding(init.Self, this); }
	}

	public class TargetableBuilding : ITargetable
	{
		readonly TargetableBuildingInfo info;
		readonly Building building;
		protected Cloak cloak;

		public TargetableBuilding(Actor self, TargetableBuildingInfo info)
		{
			this.info = info;
			building = self.Trait<Building>();
			cloak = self.TraitOrDefault<Cloak>();
		}

		public bool TargetableBy(Actor self, Actor viewer)
		{
			if (cloak == null || (!viewer.IsDead && viewer.HasTrait<IgnoresCloak>()))
				return true;

			return cloak.IsVisible(self, viewer.Owner);
		}

		public string[] TargetTypes { get { return info.TargetTypes; } }

		public IEnumerable<WPos> TargetablePositions(Actor self)
		{
			return building.OccupiedCells().Select(c => self.World.Map.CenterOfCell(c.First));
		}

		public bool RequiresForceFire { get { return info.RequiresForceFire; } }
	}
}
