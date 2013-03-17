﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IPlaceBuildingDecoration
	{
		void Render(WorldRenderer wr, World w, ActorInfo ai, PPos centerLocation);
	}

	class RenderRangeCircleInfo : TraitInfo<RenderRangeCircle>, IPlaceBuildingDecoration
	{
		public readonly string RangeCircleType = null;

		public void Render(WorldRenderer wr, World w, ActorInfo ai, PPos centerLocation)
		{
			
			wr.DrawRangeCircle(
				GetCircleColor(w),
				centerLocation.ToFloat2(),
				ai.Traits.Get<AttackBaseInfo>().GetMaximumRange());

			foreach (var a in w.ActorsWithTrait<RenderRangeCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					if (a.Actor.Info.Traits.Get<RenderRangeCircleInfo>().RangeCircleType == RangeCircleType)
						a.Trait.RenderBeforeWorld(wr, a.Actor);
		}
		
		public Color GetCircleColor(World w)
		{
			if (w.Map.Tileset == "SNOW")
				return Color.FromArgb(128, Color.Black);
			
			return Color.FromArgb(128, Color.Yellow);
		}
	}

	class RenderRangeCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(WorldRenderer wr, Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;
					
			wr.DrawRangeCircle(
				self.Info.Traits.Get<RenderRangeCircleInfo>().GetCircleColor(wr.world),
				self.CenterLocation.ToFloat2(), self.Trait<AttackBase>().GetMaximumRange());
		}
	}
}
