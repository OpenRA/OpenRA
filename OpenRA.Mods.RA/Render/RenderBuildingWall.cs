#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingWallInfo : RenderBuildingInfo
	{
		public readonly string Type = "wall";
		public readonly string Sequence = "idle";

		public override object Create(ActorInitializer init) { return new RenderBuildingWall(init, this); }
	}

	class RenderBuildingWall : RenderBuilding, INotifyBuildComplete, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly RenderBuildingWallInfo info;
		int adjacent = 0;
		bool dirty = true;

		public RenderBuildingWall(ActorInitializer init, RenderBuildingWallInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public void BuildingComplete(Actor self)
		{
			DefaultAnimation.PlayFetchIndex(info.Sequence, () => adjacent);
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			DefaultAnimation.PlayFetchIndex(NormalizeSequence(DefaultAnimation, e.DamageState, info.Sequence), () => adjacent);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!dirty)
				return;

			// Update connection to neighbours
			var vec = new CVec(1, 1);
			var adjacentActors = self.World.FindActorsInBox(self.Location - vec, self.Location + vec);

			adjacent = 0;
			foreach (var a in adjacentActors)
			{
				var rb = a.TraitOrDefault<RenderBuildingWall>();
				if (rb == null || rb.info.Type != info.Type)
					continue;

				var location = self.Location;
				var otherLocation = a.Location;

				if (otherLocation == location + new CVec(0, -1))
					adjacent |= 1;
				else if (otherLocation == location + new CVec(+1, 0))
					adjacent |= 2;
				else if (otherLocation == location + new CVec(0, +1))
					adjacent |= 4;
				else if (otherLocation == location + new CVec(-1, 0))
					adjacent |= 8;
			}

			dirty = false;
		}

		static void UpdateNeighbours(Actor self)
		{
			var vec = new CVec(1, 1);
			var neighbours = self.World.FindActorsInBox(self.Location - vec, self.Location + vec)
				.Select(a => a.TraitOrDefault<RenderBuildingWall>())
				.Where(a => a != null);

			foreach (var rb in neighbours)
				rb.dirty = true;
		}

		public void AddedToWorld(Actor self)
		{
			UpdateNeighbours(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			UpdateNeighbours(self);
		}
	}
}
