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
		public void Render(WorldRenderer wr, World w, ActorInfo ai, int2 centerLocation)
		{
			wr.DrawRangeCircle(
				Color.FromArgb(128, Color.Cyan),
				centerLocation,
				ai.Traits.Get<CreatesShroudInfo>().Range);

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

			wr.DrawRangeCircle(
				Color.FromArgb(128, Color.Cyan),
				self.CenterLocation, (int)self.Info.Traits.Get<CreatesShroudInfo>().Range);
		}
	}
}

