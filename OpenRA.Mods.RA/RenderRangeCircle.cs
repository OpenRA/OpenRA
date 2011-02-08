#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IPlaceBuildingDecoration
	{
		void Render(WorldRenderer wr, World w, ActorInfo ai, int2 centerLocation);
	}

	class RenderRangeCircleInfo : TraitInfo<RenderRangeCircle>, IPlaceBuildingDecoration
	{
		public readonly string RangeCircleType;

		public void Render(WorldRenderer wr, World w, ActorInfo ai, int2 centerLocation)
		{
			wr.DrawRangeCircle(
				Color.FromArgb(128, Color.Yellow),
				centerLocation, 3	/* hack: get this from the ActorInfo, but it's nontrivial currently */);
		}
	}

	class RenderRangeCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(WorldRenderer wr, Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;
			
			wr.DrawRangeCircle(
				Color.FromArgb(128, Color.Yellow),
				self.CenterLocation, (int)self.Trait<AttackBase>().GetMaximumRange());
		}
	}
}
