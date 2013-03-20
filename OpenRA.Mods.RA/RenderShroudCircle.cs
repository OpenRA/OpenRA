#region Copyright & License Information
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
	class RenderShroudCircleInfo : TraitInfo<RenderShroudCircle>, IPlaceBuildingDecoration
	{
		public void Render(WorldRenderer wr, World w, ActorInfo ai, PPos centerLocation)
		{
			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, Color.Cyan),
				centerLocation.ToFloat2(),
				ai.Traits.Get<CreatesShroudInfo>().Range,
				Color.FromArgb(96, Color.Black),
				1);

			foreach (var a in w.ActorsWithTrait<RenderShroudCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					a.Trait.RenderBeforeWorld(wr, a.Actor);
		}
	}

	class RenderShroudCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(WorldRenderer wr, Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, Color.Cyan),
				self.CenterLocation.ToFloat2(), self.Info.Traits.Get<CreatesShroudInfo>().Range,
				Color.FromArgb(96, Color.Black),
				1);
		}
	}
}

