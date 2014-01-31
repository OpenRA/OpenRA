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
	class RenderShroudCircleInfo : ITraitInfo, IPlaceBuildingDecoration
	{
		public void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			wr.DrawRangeCircleWithContrast(
				centerPosition,
				WRange.FromCells(ai.Traits.Get<CreatesShroudInfo>().Range),
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black)
			);

			foreach (var a in w.ActorsWithTrait<RenderShroudCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					a.Trait.RenderAfterWorld(wr);
		}

		public object Create(ActorInitializer init) { return new RenderShroudCircle(init.self); }
	}

	class RenderShroudCircle : IPostRenderSelection
	{
		Actor self;

		public RenderShroudCircle(Actor self) { this.self = self; }

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			wr.DrawRangeCircleWithContrast(
				self.CenterPosition,
				WRange.FromCells(self.Info.Traits.Get<CreatesShroudInfo>().Range),
				Color.FromArgb(128, Color.Cyan),
				Color.FromArgb(96, Color.Black)
			);
		}
	}
}

