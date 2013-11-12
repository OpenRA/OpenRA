#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		public override object Create(ActorInitializer init) { return new RenderBuildingWall( init, this ); }
	}

	class RenderBuildingWall : RenderBuilding, INotifyBuildComplete
	{
		string seqName;
		int adjacentWalls = 0;

		public RenderBuildingWall( ActorInitializer init, RenderBuildingInfo info )
			: base(init, info)
		{
			seqName = "idle";
		}

		public void BuildingComplete( Actor self )
		{
			anim.PlayFetchIndex(seqName, () => adjacentWalls);
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			anim.PlayFetchIndex(NormalizeSequence(anim, e.DamageState, "idle"), () => adjacentWalls);
		}

		bool hasTicked = false;
		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!hasTicked)
			{
				var vec = new CVec(1, 1);
				var adjWalls = self.World.FindActorsInBox(self.Location - vec, self.Location + vec)
					.Where(a => a.Info == self.Info && a != self);

				foreach (var w in adjWalls)
				{
					w.Trait<RenderBuildingWall>().AddAdjacentWall(w.Location, self.Location);
					AddAdjacentWall(self.Location, w.Location);
				}
				hasTicked = true;
			}
		}

		void AddAdjacentWall(CPos location, CPos otherLocation)
		{
			if (otherLocation == location + new CVec(0, -1)) adjacentWalls |= 1;
			if (otherLocation == location + new CVec(+1, 0)) adjacentWalls |= 2;
			if (otherLocation == location + new CVec(0, +1)) adjacentWalls |= 4;
			if (otherLocation == location + new CVec(-1, 0)) adjacentWalls |= 8;
		}
	}
}
