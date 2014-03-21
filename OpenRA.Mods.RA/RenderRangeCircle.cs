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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IPlaceBuildingDecoration
	{
		void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition);
	}

	class RenderRangeCircleInfo : ITraitInfo, IPlaceBuildingDecoration, Requires<AttackBaseInfo>
	{
		public readonly string RangeCircleType = null;

		[Desc("Range to draw if no armaments are available")]
		public readonly WRange FallbackRange = WRange.Zero;

		public void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			var armaments = ai.Traits.WithInterface<ArmamentInfo>();
			var range = FallbackRange;

			if (armaments.Any())
				range = armaments.Select(a => Rules.Weapons[a.Weapon.ToLowerInvariant()].Range).Max();

			if (range == WRange.Zero)
				return;

			wr.DrawRangeCircleWithContrast(
				centerPosition,
				range,
				Color.FromArgb(128, Color.Yellow),
				Color.FromArgb(96, Color.Black)
			);

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
		AttackBase attack;

		public RenderRangeCircle(Actor self)
		{
			this.self = self;
			attack = self.Trait<AttackBase>();
		}

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			wr.DrawRangeCircleWithContrast(
				self.CenterPosition,
				attack.GetMaximumRange(),
				Color.FromArgb(128, Color.Yellow),
				Color.FromArgb(96, Color.Black)
			);
		}
	}
}
