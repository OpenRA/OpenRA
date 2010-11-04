#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingWallInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingWall(init); }
	}

	class RenderBuildingWall : RenderBuilding
	{
		string seqName;
		int adjacentWalls = 0;
		
		public RenderBuildingWall( ActorInitializer init )
			: base(init)
		{
			seqName = "idle";
			anim.PlayFetchIndex(seqName, () => adjacentWalls);
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;

			var bi = self.Info.Traits.Get<BuildingInfo>();

			if (e.DamageState == DamageState.Medium && anim.HasSequence("scratched-idle"))
				seqName = "scratched-idle";
			else if (e.DamageState <= DamageState.Medium)
				seqName = "idle";
			else if (e.DamageState == DamageState.Critical && anim.HasSequence("critical-idle"))
			{
				seqName = "critical-idle";
				if (e.DamageState > e.PreviousDamageState)
					Sound.Play(bi.DamagedSound, self.CenterLocation);
			}
			else if (e.DamageState <= DamageState.Critical)
			{
				seqName = "damaged-idle";
				if (e.DamageState > e.PreviousDamageState)
					Sound.Play(bi.DamagedSound, self.CenterLocation);
			}

			anim.PlayFetchIndex(seqName, () => adjacentWalls);
		}

		bool hasTicked = false;
		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!hasTicked)
			{
				var oneCell = new float2(Game.CellSize, Game.CellSize);
				var adjWalls = self.World.FindUnits(self.CenterLocation - oneCell, self.CenterLocation + oneCell)
					.Where(a => a.Info == self.Info && a != self);

				foreach (var w in adjWalls)
				{
					w.Trait<RenderBuildingWall>().AddAdjacentWall(w.Location, self.Location);
					AddAdjacentWall(self.Location, w.Location);
				}
				hasTicked = true;
			}
		}

		void AddAdjacentWall(int2 location, int2 otherLocation)
		{
			if (otherLocation == location + new int2(0, -1)) adjacentWalls |= 1;
			if (otherLocation == location + new int2(+1, 0)) adjacentWalls |= 2;
			if (otherLocation == location + new int2(0, +1)) adjacentWalls |= 4;
			if (otherLocation == location + new int2(-1, 0)) adjacentWalls |= 8;
		}
	}
}
