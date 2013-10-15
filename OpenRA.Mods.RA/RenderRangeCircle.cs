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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IPlaceBuildingDecoration
	{
		void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition);
	}

	class RenderRangeCircleInfo : ITraitInfo, IPlaceBuildingDecoration
	{
		public readonly string RangeCircleType = null;

		public void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, Color.Yellow), wr.ScreenPxPosition(centerPosition),
				ai.Traits.WithInterface<ArmamentInfo>()
					.Select(a => Rules.Weapons[a.Weapon.ToLowerInvariant()].Range).Max(),
				Color.FromArgb(96, Color.Black));

			foreach (var a in w.ActorsWithTrait<RenderRangeCircle>())
				if (a.Actor.Owner == a.Actor.World.LocalPlayer)
					if (a.Actor.Info.Traits.Get<RenderRangeCircleInfo>().RangeCircleType == RangeCircleType)
						a.Trait.RenderAfterWorld(wr);
		}

		public object Create(ActorInitializer init) { return new RenderRangeCircle(init.self); }
	}

	class RenderRangeCircle : IPostRenderSelection
	{
		Actor self;

		public RenderRangeCircle(Actor self) { this.self = self; }

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			// Hack: Convert world coords to cells
			var pxRange = self.Trait<AttackBase>().GetMaximumRange().Range / 1024f;
			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, Color.Yellow),
				wr.ScreenPxPosition(self.CenterPosition), pxRange,
				Color.FromArgb(96, Color.Black));
		}
	}
}
